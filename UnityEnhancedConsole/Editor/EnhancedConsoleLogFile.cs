using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private static readonly List<string> _writeBuffer = new List<string>();
        private static double _lastFlushTime;

        private const string LogFileName = "EnhancedConsole.log";
        private const int WriteBufferFlushCount = 50;
        private const double WriteBufferFlushIntervalSec = 1.5;
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
            _lastFlushTime = EditorApplication.timeSinceStartup;
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
                    frameCount = Application.isPlaying ? Time.frameCount : 0;    
                }
                string line = SerializeEntry((int)type, timestamp, condition ?? "", stackTrace ?? "", frameCount);
                lock (FileLock)
                {
                    _writeBuffer.Add(line + Environment.NewLine);
                    if (_writeBuffer.Count == 1)
                        _lastFlushTime = EditorApplication.timeSinceStartup;
                    if (_writeBuffer.Count >= WriteBufferFlushCount)
                        FlushBufferInternal();
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
            if (_writeBuffer.Count == 0) return;
            try
            {
                string path = GetLogFilePath();
                string merged = string.Concat(_writeBuffer);
                File.AppendAllText(path, merged, Encoding.UTF8);
                _writeBuffer.Clear();
                _lastFlushTime = EditorApplication.timeSinceStartup;
            }
            catch (Exception e)
            {
                Debug.LogWarning("EnhancedConsole: Failed to flush log buffer: " + e.Message);
            }
        }

        /// <summary>
        /// 定时检查：达到间隔则 flush（主线程每帧调用）。
        /// </summary>
        private static void FlushCheck()
        {
            lock (FileLock)
            {
                if (_writeBuffer.Count == 0) return;
                if (EditorApplication.timeSinceStartup - _lastFlushTime < WriteBufferFlushIntervalSec) return;
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

        private static bool TryDeserializeEntry(string line, out LogEntry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(line)) return false;
            string[] parts = line.Split(new[] { '|' }, 5, StringSplitOptions.None);
            if (parts.Length < 4) return false;
            if (!int.TryParse(parts[0], out int type)) return false;
            string timestamp = parts[1];
            string condition;
            string stackTrace;
            try
            {
                condition = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
                stackTrace = Encoding.UTF8.GetString(Convert.FromBase64String(parts[3]));
            }
            catch
            {
                return false;
            }
            int frameCount = 0;
            if (parts.Length >= 5) int.TryParse(parts[4], out frameCount);
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
        /// 从日志文件流式加载条目。若文件不存在或为空则返回空列表。
        /// </summary>
        /// <param name="maxEntries">最多加载条数，超过时只保留最近 N 条（滑动窗口）；0 表示不限制（大文件可能 OOM）。</param>
        public static List<LogEntry> LoadEntries(int maxEntries = 50000)
        {
            FlushBuffer();
            string path = GetLogFilePath();
            if (!File.Exists(path)) return new List<LogEntry>();
            try
            {
                if (maxEntries > 0)
                {
                    var queue = new Queue<LogEntry>(Math.Min(maxEntries, 1024));
                    using (var sr = new StreamReader(path, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (!TryDeserializeEntry(line, out LogEntry entry)) continue;
                            queue.Enqueue(entry);
                            if (queue.Count > maxEntries)
                                queue.Dequeue();
                        }
                    }
                    return new List<LogEntry>(queue);
                }
                else
                {
                    var list = new List<LogEntry>();
                    using (var sr = new StreamReader(path, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (TryDeserializeEntry(line, out LogEntry entry))
                                list.Add(entry);
                        }
                    }
                    return list;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("EnhancedConsole: Failed to load log file: " + e.Message);
            }
            return new List<LogEntry>();
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
