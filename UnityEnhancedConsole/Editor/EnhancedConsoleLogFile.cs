using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug=UnityEngine.Debug;
namespace UnityEnhancedConsole
{
    /// <summary>
    /// Unity 启动时即监听日志并写入文件；提供从文件加载、清空文件。
    /// 文件格式：每行一条日志 TYPE|TIMESTAMP|BASE64(condition)|BASE64(stackTrace)
    /// </summary>
    [InitializeOnLoad]
    public static class EnhancedConsoleLogFile
    {
        private static readonly object FileLock = new object();
        private static string _logFilePath;
        private static bool _initialized;
        private static readonly StringBuilder _writeBuffer = new StringBuilder(64 * 1024);
        private static int _writeBufferLineCount;
        private static DateTime _lastFlushTime = DateTime.UtcNow;

        private const string LogFileName = "EnhancedConsole.log";
        private const int WriteBufferFlushCount = 50;
        private const double WriteBufferFlushIntervalSec = 1.5;
        private const long MaxLogFileSizeBytes = 500L * 1024 * 1024; // 500MB
        private const long MaxTotalLogSizeBytes = 10L * 1024 * 1024 * 1024; // 10GB
        private  static int mainThreadId = 0;

        static EnhancedConsoleLogFile()
        {
            mainThreadId= System.Threading.Thread.CurrentThread.ManagedThreadId;            
            if (_initialized) return;
            _initialized = true;
            _logFilePath = GetLogFilePath();
            RotateLogFileOnStartup();
            // Application.logMessageReceived += AppendLogToFile;
            Application.logMessageReceivedThreaded += AppendLogToFileThreaded;
            _lastFlushTime = DateTime.UtcNow;
            EditorApplication.update += FlushCheck;
            AssemblyReloadEvents.beforeAssemblyReload += FlushBuffer;
            EditorApplication.quitting += FlushBuffer;
            AssemblyReloadEvents.afterAssemblyReload += FlushBuffer;
        }

        /// <summary>
        /// Unity 打开项目时（新进程）：若存在当前日志文件则改名为带时间戳的备份，然后从空文件重新开始。
        /// 域重载（脚本重编译等同进程）时不轮转，避免同一会话内重复轮转。
        /// </summary>
        private static void RotateLogFileOnStartup()
        {
            try
            {
                int currentPid = Process.GetCurrentProcess().Id;
                string pidPath = GetRotateSessionPidPath();
                if (File.Exists(pidPath) && int.TryParse(File.ReadAllText(pidPath).Trim(), out int storedPid) && storedPid == currentPid)
                    return; // 同进程（域重载），不轮转
                string path = GetLogFilePath();
                if (!File.Exists(path))
                {
                    WriteRotateSessionPid(pidPath, currentPid);
                    return;
                }
                string dir = Path.GetDirectoryName(path);
                string backupName = "EnhancedConsole_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
                string backupPath = Path.Combine(dir, backupName);
                File.Move(path, backupPath);
                WriteRotateSessionPid(pidPath, currentPid);
            }
            catch (Exception e)
            {
                Debug.LogWarning("EnhancedConsole: Failed to rotate log file on startup: " + e.Message);
            }
        }

        private static string GetRotateSessionPidPath()
        {
            string dir = Path.Combine(Application.dataPath, "..", "Library");
            dir = Path.GetFullPath(dir);
            return Path.Combine(dir, "EnhancedConsole_rotate_pid.txt");
        }

        private static void WriteRotateSessionPid(string pidPath, int pid)
        {
            try
            {
                string dir = Path.GetDirectoryName(pidPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(pidPath, pid.ToString(), Encoding.UTF8);
            }
            catch { /* 忽略，仅用于优化轮转时机 */ }
        }

        /// <summary>
        /// 获取日志文件路径（项目下 Logs/EnhancedConsole.log）
        /// </summary>
        public static string GetLogFilePath()
        {
            if (!string.IsNullOrEmpty(_logFilePath)) return _logFilePath;
            string dir = Path.Combine(Application.dataPath, "..", "Logs");
            dir = Path.GetFullPath(dir);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return Path.Combine(dir, LogFileName);
        }

        /// <summary>
        /// 主线程日志回调：追加写入文件
        /// </summary>
        private static void AppendLogToFile(string condition, string stackTrace, LogType type)
        {
            WriteEntryToFile(condition, stackTrace, type);
        }

        /// <summary>
        /// 子线程日志回调：追加写入文件（WriteEntryToFile 内加锁）
        /// </summary>
        private static void AppendLogToFileThreaded(string condition, string stackTrace, LogType type)
        {
            WriteEntryToFile(condition, stackTrace, type);
        }

        private static void WriteEntryToFile(string condition, string stackTrace, LogType type)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                int frameCount = 0;
                if (System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId)
                {
                    try
                    {
                        if (Application.isPlaying)
                            frameCount = Time.frameCount;
                    }
                    catch (UnityException)
                    {
                        // 序列化等阶段不允许调用 isPlaying/frameCount，保持 0
                    }
                }
                string line = SerializeEntry((int)type, timestamp, condition ?? "", stackTrace ?? "", frameCount);
                lock (FileLock)
                {
                    _writeBuffer.Append(line).Append(Environment.NewLine);
                    _writeBufferLineCount++;
                    if (_writeBufferLineCount == 1)
                        _lastFlushTime = DateTime.UtcNow;
                    if (_writeBufferLineCount >= WriteBufferFlushCount)
                        FlushBufferInternal();
                    CheckRotateBySize();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 将缓冲内容一次性写入文件。必须在 FileLock 内调用。
        /// </summary>
        private static void FlushBufferInternal()
        {
            if (_writeBufferLineCount == 0) return;
            try
            {
                string path = GetLogFilePath();
                File.AppendAllText(path, _writeBuffer.ToString(), Encoding.UTF8);
                _writeBuffer.Clear();
                _writeBufferLineCount = 0;
                _lastFlushTime = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                Debug.LogWarning("EnhancedConsole: Failed to flush log buffer: " + e.Message);
            }
        }

        /// <summary>
        /// 获取所有日志文件（当前+历史），按修改时间从新到旧排序。
        /// </summary>
        private static List<string> GetLogFilesOrdered()
        {
            string dir = Path.GetDirectoryName(GetLogFilePath());
            if (!Directory.Exists(dir)) return new List<string>();
            var files = Directory.GetFiles(dir, "EnhancedConsole*.log")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => f.FullName)
                .ToList();
            string current = GetLogFilePath();
            if (files.Contains(current))
            {
                files.Remove(current);
                files.Insert(0, current);
            }
            return files;
        }

        /// <summary>
        /// 检查当前日志文件大小，超过阈值时执行轮转（必须在 FileLock 内调用）。
        /// </summary>
        private static void CheckRotateBySize()
        {
            try
            {
                string path = GetLogFilePath();
                if (!File.Exists(path)) return;
                var fi = new FileInfo(path);
                if (fi.Length < MaxLogFileSizeBytes) return;
                string dir = Path.GetDirectoryName(path);
                string backupName = "EnhancedConsole_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
                string backupPath = Path.Combine(dir, backupName);
                File.Move(path, backupPath);
                EnforceTotalSizeLimit();
            }
            catch (Exception e)
            {
                Debug.LogWarning("EnhancedConsole: Failed to rotate log file by size: " + e.Message);
            }
        }

        /// <summary>
        ///  enforcement 总日志大小上限，超出时删除最旧的历史文件（保留当前活跃文件）。
        /// </summary>
        private static void EnforceTotalSizeLimit()
        {
            try
            {
                string dir = Path.GetDirectoryName(GetLogFilePath());
                if (!Directory.Exists(dir)) return;
                var files = Directory.GetFiles(dir, "EnhancedConsole*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();
                long totalSize = files.Sum(f => f.Length);
                while (totalSize > MaxTotalLogSizeBytes && files.Count > 1)
                {
                    var oldest = files[files.Count - 1];
                    totalSize -= oldest.Length;
                    try { File.Delete(oldest.FullName); } catch { }
                    files.RemoveAt(files.Count - 1);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("EnhancedConsole: Failed to enforce total log size limit: " + e.Message);
            }
        }

        /// <summary>
        /// 定时检查：达到间隔则 flush（主线程每帧调用）。
        /// </summary>
        private static void FlushCheck()
        {
            lock (FileLock)
            {
                if (_writeBufferLineCount == 0) return;
                if ((DateTime.UtcNow - _lastFlushTime).TotalSeconds < WriteBufferFlushIntervalSec) return;
                FlushBufferInternal();
            }
        }

        /// <summary>
        /// 立即将缓冲写入文件（LoadEntries/Clear/Reload 前调用）。
        /// </summary>
        private static void FlushBuffer()
        {
            lock (FileLock)
            {
                FlushBufferInternal();
            }
        }

        private static string SerializeEntry(int type, string timestamp, string condition, string stackTrace, int frameCount)
        {
            string c = Convert.ToBase64String(Encoding.UTF8.GetBytes(condition));
            string s = Convert.ToBase64String(Encoding.UTF8.GetBytes(stackTrace));
            return type + "|" + timestamp + "|" + c + "|" + s + "|" + frameCount;
        }

        // 解析路径上的复用缓冲（线程局部，避免在加载 5w+ 行时反复分配 char[]）
        [ThreadStatic] private static char[] _tlDecodeChars;

        private static bool TryDecodeBase64(string source, int start, int length, out string result)
        {
            result = null;
            if (length == 0) { result = ""; return true; }
            try
            {
                var chars = _tlDecodeChars;
                if (chars == null || chars.Length < length) { chars = new char[Math.Max(length, 256)]; _tlDecodeChars = chars; }
                source.CopyTo(start, chars, 0, length);
                byte[] bytes = Convert.FromBase64CharArray(chars, 0, length);
                result = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryDeserializeEntry(string line, out LogEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(line)) return false;
            // 手写 IndexOf 切片，避免 string.Split 分配 string[] + 4 次 Substring
            int p0 = line.IndexOf('|'); if (p0 < 0) return false;
            int p1 = line.IndexOf('|', p0 + 1); if (p1 < 0) return false;
            int p2 = line.IndexOf('|', p1 + 1); if (p2 < 0) return false;
            int p3 = line.IndexOf('|', p2 + 1); // 可选 frameCount

            // type
            int type = 0;
            for (int i = 0; i < p0; i++)
            {
                char ch = line[i];
                if (ch < '0' || ch > '9') return false;
                type = type * 10 + (ch - '0');
            }
            // timestamp 直接 substring（短，无法避免）
            string timestamp = line.Substring(p0 + 1, p1 - p0 - 1);

            int condStart = p1 + 1;
            int condLen = p2 - condStart;
            int stackStart = p2 + 1;
            int stackLen = (p3 < 0 ? line.Length : p3) - stackStart;

            if (!TryDecodeBase64(line, condStart, condLen, out string condition)) return false;
            if (!TryDecodeBase64(line, stackStart, stackLen, out string stackTrace)) return false;

            int frameCount = 0;
            if (p3 >= 0)
            {
                for (int i = p3 + 1; i < line.Length; i++)
                {
                    char ch = line[i];
                    if (ch < '0' || ch > '9') break;
                    frameCount = frameCount * 10 + (ch - '0');
                }
            }
            entry = new LogEntry
            {
                LogType = (LogType)type,
                TimeStamp = timestamp,
                Condition = condition,
                StackTrace = stackTrace,
                Count = 1,
                FrameCount = frameCount
            };
            return true;
        }

        /// <summary>
        /// 从所有日志文件（当前+历史）中惰性加载条目，按时间从新到旧读取，直到满足 maxEntries。
        /// 对大文件自动使用尾部读取优化，避免读取整个文件。
        /// </summary>
        /// <param name="maxEntries">最多加载条数，超过时只保留最近 N 条；0 表示不限制（大文件可能 OOM）。</param>
        public static List<LogEntry> LoadEntries(int maxEntries = 50000)
        {
            FlushBuffer();
            if (maxEntries <= 0) maxEntries = int.MaxValue;
            var allFiles = GetLogFilesOrdered();
            const int EstAvgLineBytes = 350;

            // 文件按修改时间从新到旧（当前文件在最前）。从新到旧累计直至够 maxEntries。
            // 用 List<List<LogEntry>> 暂存各文件的尾部（从早到晚），最后一次性按从旧到新顺序拼接。
            var perFile = new List<List<LogEntry>>(allFiles.Count);
            int total = 0;
            foreach (var filePath in allFiles)
            {
                if (!File.Exists(filePath)) continue;
                var fi = new FileInfo(filePath);
                if (fi.Length == 0) continue;

                int need = maxEntries - total;
                if (need <= 0) break;

                var entries = TailReadEntries(filePath, fi.Length, need, EstAvgLineBytes);
                if (entries == null || entries.Count == 0) continue;
                perFile.Add(entries);
                total += entries.Count;
                if (total >= maxEntries) break;
            }

            // perFile 顺序为"从新到旧"。最终按"从旧到新"拼接：倒序遍历。
            var result = new List<LogEntry>(Math.Min(total, maxEntries));
            for (int i = perFile.Count - 1; i >= 0; i--)
                result.AddRange(perFile[i]);

            // 仅保留最后 maxEntries 条（多读到的部分位于头部）
            if (result.Count > maxEntries)
            {
                int keep = maxEntries;
                int skip = result.Count - keep;
                // 用尾段构造新 List，避免头部 RemoveRange 的 O(N) 移动 + 后续访问开销
                var trimmed = new List<LogEntry>(keep);
                for (int i = skip; i < result.Count; i++) trimmed.Add(result[i]);
                result = trimmed;
            }

            return result;
        }

        /// <summary>
        /// 从文件尾部估算位置开始读取，避免读取整个大文件。
        /// 使用 FileStream.Seek 跳过前面的内容，仅解析尾部所需行数。
        /// </summary>
        private static List<LogEntry> TailReadEntries(string path, long fileLength, int maxEntries, int avgLineBytes)
        {
            // 多读 20% 余量，确保有足够条目
            long startPos = Math.Max(0, fileLength - (long)maxEntries * avgLineBytes * 12 / 10);
            try
            {
                // 用环形 Queue 收集尾部 maxEntries 条；避免 List 头部 RemoveRange 的 O(N) 移动
                var queue = new Queue<LogEntry>(maxEntries);
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(startPos, SeekOrigin.Begin);
                    using (var sr = new StreamReader(fs, Encoding.UTF8, true, 8192, true))
                    {
                        if (startPos > 0)
                            sr.ReadLine(); // 跳过被截断的第一行

                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (TryDeserializeEntry(line, out LogEntry entry))
                            {
                                if (queue.Count >= maxEntries) queue.Dequeue();
                                queue.Enqueue(entry);
                            }
                        }
                    }
                }
                var entries = new List<LogEntry>(queue.Count);
                while (queue.Count > 0) entries.Add(queue.Dequeue());
                return entries;
            }
            catch
            {
                return null; // 出错时回退到全量读取
            }
        }

        /// <summary>
        /// 清空日志文件内容（Clear 时调用）。先 flush 缓冲再清空文件。
        /// </summary>
        public static void ClearFile()
        {
            try
            {
                string path = GetLogFilePath();
                lock (FileLock)
                {
                    FlushBufferInternal();
                    File.WriteAllText(path, "", Encoding.UTF8);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("EnhancedConsole: Failed to clear log file: " + e.Message);
            }
        }

        /// <summary>
        /// 用指定条目重写日志文件（Clear 时保留编译错误条目用）。先 flush 缓冲，再写入仅包含这些条目的内容。
        /// </summary>
        public static void RewriteFileWithEntries(List<LogEntry> entries)
        {
            if (entries == null) entries = new List<LogEntry>();
            try
            {
                string path = GetLogFilePath();
                lock (FileLock)
                {
                    FlushBufferInternal();
                    var sb = new StringBuilder();
                    foreach (var e in entries)
                    {
                        string line = SerializeEntry(
                            (int)e.LogType,
                            e.TimeStamp ?? "",
                            e.Condition ?? "",
                            e.StackTrace ?? "",
                            e.FrameCount);
                        sb.Append(line).Append(Environment.NewLine);
                    }
                    File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("EnhancedConsole: Failed to rewrite log file: " + e.Message);
            }
        }
    }
}
