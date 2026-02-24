using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 变量监视管理器。[InitializeOnLoad] 单例，线程安全。
    /// </summary>
    [InitializeOnLoad]
    public static class WatchManager
    {
        // ─── Constants ───────────────────────────────────────
        public const int DefaultHistoryDepth = 300;
        public const int MaxHistoryDepth = 10000;
        public const int DefaultMaxEntries = 1000;

        // ─── EditorPrefs Keys ────────────────────────────────
        private const string PrefHistoryDepth = "EnhancedConsole_WatchHistoryDepth";
        private const string PrefMaxEntries = "EnhancedConsole_WatchMaxEntries";
        private const string PrefAutoUpdateInterval = "EnhancedConsole_WatchAutoUpdateInterval";
        private const string PrefPersistToFile = "EnhancedConsole_WatchPersistToFile";
        private const string PersistFileName = "EnhancedConsole_Watch.log";

        // ─── State ───────────────────────────────────────────
        private static readonly Dictionary<string, WatchEntry> _entries = new Dictionary<string, WatchEntry>();
        private static readonly List<string> _orderedKeys = new List<string>();
        private static readonly object _lock = new object();
        private static int _mainThreadId;
        private static int _historyDepth;
        private static int _maxEntries;
        private static double _lastAutoUpdateTime;
        private static float _autoUpdateInterval;

        // ─── Events ──────────────────────────────────────────
        public static event Action OnChanged;

        // ─── Properties ──────────────────────────────────────
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        public static int EntryCount { get { lock (_lock) { return _entries.Count; } } }
        public static int HistoryDepth => _historyDepth;
        public static int MaxEntries => _maxEntries;

        // ─── Static Constructor ──────────────────────────────
        static WatchManager()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            LoadPrefs();
            LoadFromFile();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
        }

        private static void LoadPrefs()
        {
            _historyDepth = Mathf.Clamp(
                EditorPrefs.GetInt(PrefHistoryDepth, DefaultHistoryDepth), 1, MaxHistoryDepth);
            _maxEntries = Mathf.Clamp(
                EditorPrefs.GetInt(PrefMaxEntries, DefaultMaxEntries), 1, 100000);
            _autoUpdateInterval = EditorPrefs.GetFloat(PrefAutoUpdateInterval, 0f);
        }

        // ─── Read Access (for UI) ───────────────────────────

        /// <summary>
        /// 在锁内访问所有条目。回调中不应执行耗时操作。
        /// </summary>
        public static void ReadEntries(Action<IReadOnlyDictionary<string, WatchEntry>, IReadOnlyList<string>> reader)
        {
            if (reader == null) return;
            lock (_lock)
            {
                reader(_entries, _orderedKeys);
            }
        }

        /// <summary>
        /// 获取所有条目的快照列表（按注册顺序）
        /// </summary>
        public static List<WatchEntry> ReadEntries()
        {
            lock (_lock)
            {
                var list = new List<WatchEntry>(_orderedKeys.Count);
                foreach (var key in _orderedKeys)
                {
                    if (_entries.TryGetValue(key, out var entry))
                        list.Add(entry);
                }
                return list;
            }
        }

        /// <summary>
        /// 获取指定名称的条目快照（浅拷贝引用）
        /// </summary>
        public static WatchEntry GetEntry(string name)
        {
            lock (_lock)
            {
                return _entries.TryGetValue(name, out var entry) ? entry : null;
            }
        }

        /// <summary>
        /// 获取所有条目名称的快照
        /// </summary>
        public static List<string> GetOrderedKeysCopy()
        {
            lock (_lock)
            {
                return new List<string>(_orderedKeys);
            }
        }

        // ─── Editor Update ───────────────────────────────────

        private static void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            if (_autoUpdateInterval > 0 && (now - _lastAutoUpdateTime) < _autoUpdateInterval)
                return;
            _lastAutoUpdateTime = now;

            bool changed = false;
            lock (_lock)
            {
                var deadKeys = (List<string>)null;
                foreach (var kvp in _entries)
                {
                    var entry = kvp.Value;
                    if (entry.Getter == null) continue;
                    if (entry.IsPaused) continue;

                    // Check owner is alive
                    if (entry.Owner != null)
                    {
                        if (!entry.Owner.IsAlive)
                        {
                            (deadKeys ?? (deadKeys = new List<string>())).Add(kvp.Key);
                            continue;
                        }
                        if (entry.Owner.Target is UnityEngine.Object uobj && uobj == null)
                        {
                            (deadKeys ?? (deadKeys = new List<string>())).Add(kvp.Key);
                            continue;
                        }
                    }

                    try
                    {
                        object newVal = entry.Getter();
                        UpdateEntryValue(entry, newVal);
                        changed = true;
                    }
                    catch (Exception)
                    {
                        // Getter threw — remove this auto-watch
                        (deadKeys ?? (deadKeys = new List<string>())).Add(kvp.Key);
                    }
                }

                if (deadKeys != null)
                {
                    foreach (var key in deadKeys)
                    {
                        _entries.Remove(key);
                        _orderedKeys.Remove(key);
                    }
                    changed = true;
                }
            }

            if (changed)
                OnChanged?.Invoke();
        }

        // ─── Value Setting (called from Watch.cs) ───────────

        public static void SetValue(string name, object value, WatchValueType typeHint, string format)
        {
            if (string.IsNullOrEmpty(name)) return;
            lock (_lock)
            {
                var entry = GetOrCreateEntry(name, format);
                if (entry.IsPaused) return;
                entry.ValueType = typeHint != WatchValueType.Object ? typeHint : DetectValueType(value);
                UpdateEntryValue(entry, value);
            }
            OnChanged?.Invoke();
        }

        public static void SetFloat(string name, float value, string format)
        {
            if (string.IsNullOrEmpty(name)) return;
            lock (_lock)
            {
                var entry = GetOrCreateEntry(name, format);
                if (entry.IsPaused) return;
                entry.ValueType = WatchValueType.Float;
                string formatted = format != null ? value.ToString(format) : value.ToString();
                entry.CurrentValue = value;
                entry.RecordHistory(formatted, value, true);
                entry.FormattedValue = formatted;
            }
            OnChanged?.Invoke();
        }

        public static void SetInt(string name, int value, string format)
        {
            if (string.IsNullOrEmpty(name)) return;
            lock (_lock)
            {
                var entry = GetOrCreateEntry(name, format);
                if (entry.IsPaused) return;
                entry.ValueType = WatchValueType.Integer;
                string formatted = format != null ? value.ToString(format) : value.ToString();
                entry.CurrentValue = value;
                entry.RecordHistory(formatted, value, true);
                entry.FormattedValue = formatted;
            }
            OnChanged?.Invoke();
        }

        public static void SetBool(string name, bool value)
        {
            if (string.IsNullOrEmpty(name)) return;
            lock (_lock)
            {
                var entry = GetOrCreateEntry(name, null);
                if (entry.IsPaused) return;
                entry.ValueType = WatchValueType.Boolean;
                string formatted = value.ToString();
                entry.CurrentValue = value;
                entry.RecordHistory(formatted, value ? 1.0 : 0.0, true);
                entry.FormattedValue = formatted;
            }
            OnChanged?.Invoke();
        }

        // ─── Auto-watch ─────────────────────────────────────

        public static void RegisterAuto(string name, Func<object> getter, UnityEngine.Object owner, string format)
        {
            if (string.IsNullOrEmpty(name) || getter == null) return;
            lock (_lock)
            {
                var entry = GetOrCreateEntry(name, format);
                entry.Getter = getter;
                entry.Owner = owner != null ? new WeakReference(owner) : null;
                try
                {
                    object val = getter();
                    entry.ValueType = DetectValueType(val);
                    UpdateEntryValue(entry, val);
                }
                catch (Exception)
                {
                    // Getter failed during registration — entry remains with null value
                }
            }
            OnChanged?.Invoke();
        }

        // ─── Remove ─────────────────────────────────────────

        public static void RemoveEntry(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            bool removed;
            lock (_lock)
            {
                removed = _entries.Remove(name);
                if (removed) _orderedKeys.Remove(name);
            }
            if (removed) OnChanged?.Invoke();
        }

        public static void RemoveByOwner(UnityEngine.Object owner)
        {
            if (owner == null) return;
            bool removed = false;
            lock (_lock)
            {
                var toRemove = new List<string>();
                foreach (var kvp in _entries)
                {
                    if (kvp.Value.Owner?.Target == owner)
                        toRemove.Add(kvp.Key);
                }
                foreach (var key in toRemove)
                {
                    _entries.Remove(key);
                    _orderedKeys.Remove(key);
                }
                removed = toRemove.Count > 0;
            }
            if (removed) OnChanged?.Invoke();
        }

        public static void ClearAll()
        {
            lock (_lock)
            {
                _entries.Clear();
                _orderedKeys.Clear();
            }
            OnChanged?.Invoke();
        }

        public static void SetEntryPaused(string name, bool paused)
        {
            bool changed = false;
            lock (_lock)
            {
                if (_entries.TryGetValue(name, out var entry))
                {
                    if (entry.IsPaused != paused)
                    {
                        entry.IsPaused = paused;
                        changed = true;
                    }
                }
            }
            if (changed) OnChanged?.Invoke();
        }

        // ─── Helpers ─────────────────────────────────────────

        private static WatchEntry GetOrCreateEntry(string name, string format)
        {
            if (_entries.TryGetValue(name, out var existing))
            {
                if (format != null) existing.Format = format;
                return existing;
            }

            // Enforce max entries — remove oldest
            while (_entries.Count >= _maxEntries && _orderedKeys.Count > 0)
            {
                var oldest = _orderedKeys[0];
                _entries.Remove(oldest);
                _orderedKeys.RemoveAt(0);
            }

            var (group, displayName) = WatchEntry.ParseName(name);
            var entry = new WatchEntry
            {
                Name = name,
                Group = group,
                DisplayName = displayName,
                Format = format,
                History = new WatchHistoryEntry[_historyDepth],
                HistoryHead = 0,
                HistoryCount = 0,
                ChangeCount = 0
            };
            _entries[name] = entry;
            _orderedKeys.Add(name);
            return entry;
        }

        private static void UpdateEntryValue(WatchEntry entry, object value)
        {
            entry.CurrentValue = value;
            string formatted = FormatValue(value, entry.Format, entry.ValueType);
            bool hasNumeric = TryGetNumeric(value, out double numericValue);
            entry.RecordHistory(formatted, numericValue, hasNumeric);
            entry.FormattedValue = formatted;
        }

        private static string FormatValue(object value, string format, WatchValueType type)
        {
            if (value == null) return "null";
            try
            {
                switch (type)
                {
                    case WatchValueType.Float:
                        if (value is float f)
                            return format != null ? f.ToString(format, CultureInfo.InvariantCulture) : f.ToString(CultureInfo.InvariantCulture);
                        if (value is double d)
                            return format != null ? d.ToString(format, CultureInfo.InvariantCulture) : d.ToString(CultureInfo.InvariantCulture);
                        return value.ToString();

                    case WatchValueType.Integer:
                        if (value is int i)
                            return format != null ? i.ToString(format, CultureInfo.InvariantCulture) : i.ToString(CultureInfo.InvariantCulture);
                        if (value is long l)
                            return format != null ? l.ToString(format, CultureInfo.InvariantCulture) : l.ToString(CultureInfo.InvariantCulture);
                        return value.ToString();

                    case WatchValueType.Boolean:
                        return value.ToString();

                    case WatchValueType.Vector:
                        if (value is Vector3 v3)
                            return format != null
                                ? $"({v3.x.ToString(format, CultureInfo.InvariantCulture)}, {v3.y.ToString(format, CultureInfo.InvariantCulture)}, {v3.z.ToString(format, CultureInfo.InvariantCulture)})"
                                : v3.ToString();
                        if (value is Vector2 v2)
                            return format != null
                                ? $"({v2.x.ToString(format, CultureInfo.InvariantCulture)}, {v2.y.ToString(format, CultureInfo.InvariantCulture)})"
                                : v2.ToString();
                        if (value is Vector4 v4)
                            return format != null
                                ? $"({v4.x.ToString(format, CultureInfo.InvariantCulture)}, {v4.y.ToString(format, CultureInfo.InvariantCulture)}, {v4.z.ToString(format, CultureInfo.InvariantCulture)}, {v4.w.ToString(format, CultureInfo.InvariantCulture)})"
                                : v4.ToString();
                        return value.ToString();

                    case WatchValueType.Color:
                        if (value is Color c)
                            return $"({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
                        if (value is Color32 c32)
                            return $"({c32.r}, {c32.g}, {c32.b}, {c32.a})";
                        return value.ToString();

                    default:
                        return value.ToString();
                }
            }
            catch
            {
                return value.ToString();
            }
        }

        private static WatchValueType DetectValueType(object value)
        {
            if (value == null) return WatchValueType.Object;
            if (value is float || value is double) return WatchValueType.Float;
            if (value is int || value is long || value is short || value is byte) return WatchValueType.Integer;
            if (value is bool) return WatchValueType.Boolean;
            if (value is Vector3 || value is Vector2 || value is Vector4) return WatchValueType.Vector;
            if (value is Color || value is Color32) return WatchValueType.Color;
            if (value is string) return WatchValueType.String;
            return WatchValueType.Object;
        }

        private static bool TryGetNumeric(object value, out double result)
        {
            result = 0;
            if (value == null) return false;
            if (value is float f) { result = f; return true; }
            if (value is double d) { result = d; return true; }
            if (value is int i) { result = i; return true; }
            if (value is long l) { result = l; return true; }
            if (value is short s) { result = s; return true; }
            if (value is byte b) { result = b; return true; }
            if (value is bool bo) { result = bo ? 1 : 0; return true; }
            return false;
        }

        // ─── Lifecycle ───────────────────────────────────────

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            // Auto-watches with destroyed owners will be cleaned up in OnEditorUpdate
        }

        private static void OnBeforeReload()
        {
            SaveToFile();
            lock (_lock)
            {
                _entries.Clear();
                _orderedKeys.Clear();
            }
        }

        // ─── Settings ────────────────────────────────────────

        public static void SetHistoryDepth(int depth)
        {
            depth = Mathf.Clamp(depth, 1, MaxHistoryDepth);
            _historyDepth = depth;
            EditorPrefs.SetInt(PrefHistoryDepth, depth);
            lock (_lock)
            {
                foreach (var entry in _entries.Values)
                {
                    if (entry.History.Length == depth) continue;
                    var ordered = entry.GetHistoryOrdered().ToArray();
                    entry.History = new WatchHistoryEntry[depth];
                    entry.HistoryHead = 0;
                    entry.HistoryCount = 0;
                    int copyCount = Mathf.Min(ordered.Length, depth);
                    int startIdx = ordered.Length - copyCount;
                    for (int i = 0; i < copyCount; i++)
                    {
                        entry.History[i] = ordered[startIdx + i];
                    }
                    entry.HistoryHead = copyCount % depth;
                    entry.HistoryCount = copyCount;
                }
            }
        }

        public static void SetMaxEntries(int max)
        {
            max = Mathf.Clamp(max, 1, 100000);
            _maxEntries = max;
            EditorPrefs.SetInt(PrefMaxEntries, max);
        }

        public static void SetAutoUpdateInterval(float interval)
        {
            _autoUpdateInterval = Mathf.Max(0f, interval);
            EditorPrefs.SetFloat(PrefAutoUpdateInterval, _autoUpdateInterval);
        }

        // ─── Persistence ─────────────────────────────────────

        public static bool PersistToFile
        {
            get => EditorPrefs.GetBool(PrefPersistToFile, false);
            set => EditorPrefs.SetBool(PrefPersistToFile, value);
        }

        public static string PersistFilePath =>
            Path.Combine(Application.temporaryCachePath, PersistFileName);

        /// <summary>
        /// 将当前所有监视条目快照写入 JSON 文件。
        /// </summary>
        public static void SaveToFile()
        {
            if (!PersistToFile) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"timestamp\": \"" + DateTime.Now.ToString("O") + "\",");
                sb.AppendLine("  \"entries\": [");

                List<string> keys;
                lock (_lock) { keys = new List<string>(_orderedKeys); }

                bool firstEntry = true;
                for (int i = 0; i < keys.Count; i++)
                {
                    WatchEntry entry;
                    lock (_lock)
                    {
                        if (!_entries.TryGetValue(keys[i], out entry)) continue;
                    }

                    if (!firstEntry) sb.AppendLine(",");
                    firstEntry = false;

                    sb.Append("    { \"name\": ");
                    sb.Append(EscapeJsonString(entry.Name));
                    sb.Append(", \"group\": ");
                    sb.Append(EscapeJsonString(entry.Group ?? ""));
                    sb.Append(", \"value\": ");
                    sb.Append(EscapeJsonString(entry.FormattedValue ?? ""));
                    sb.Append(", \"type\": \"");
                    sb.Append(entry.ValueType);
                    sb.Append("\", \"changes\": ");
                    sb.Append(entry.ChangeCount);
                    sb.Append(", \"paused\": ");
                    sb.Append(entry.IsPaused ? "true" : "false");

                    // History snapshot (last 50 entries max for file size)
                    sb.Append(", \"history\": [");
                    int histIdx = 0;
                    foreach (var h in entry.GetHistoryOrdered())
                    {
                        if (histIdx >= 50) break;
                        if (histIdx > 0) sb.Append(", ");
                        sb.Append("{ \"t\": ");
                        sb.Append(h.Timestamp.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                        sb.Append(", \"f\": ");
                        sb.Append(h.FrameCount);
                        sb.Append(", \"v\": ");
                        sb.Append(EscapeJsonString(h.FormattedValue ?? ""));
                        sb.Append(" }");
                        histIdx++;
                    }
                    sb.Append("]");

                    sb.Append(" }");
                }

                sb.AppendLine();
                sb.AppendLine("  ]");
                sb.AppendLine("}");

                File.WriteAllText(PersistFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WatchManager] SaveToFile failed: " + ex.Message);
            }
        }

        /// <summary>
        /// 从持久化文件加载监视数据（仅恢复名称和最后值，不恢复 Auto-Watch）。
        /// </summary>
        public static void LoadFromFile()
        {
            if (!PersistToFile) return;

            var path = PersistFilePath;
            if (!File.Exists(path)) return;

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                // 简易解析：逐行查找 "name": 和 "value": 对
                // 完整 JSON 解析不引入额外依赖，用正则提取
                var namePattern = new System.Text.RegularExpressions.Regex("\"name\":\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
                var valuePattern = new System.Text.RegularExpressions.Regex("\"value\":\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");

                var nameMatches = namePattern.Matches(json);
                var valueMatches = valuePattern.Matches(json);

                int count = Mathf.Min(nameMatches.Count, valueMatches.Count);
                for (int i = 0; i < count; i++)
                {
                    string name = UnescapeJsonString(nameMatches[i].Groups[1].Value);
                    string value = UnescapeJsonString(valueMatches[i].Groups[1].Value);
                    SetValue(name, value, WatchValueType.String, null);
                }

                if (count > 0)
                    Debug.Log($"[WatchManager] Loaded {count} entries from {PersistFileName}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WatchManager] LoadFromFile failed: " + ex.Message);
            }
        }

        private static string EscapeJsonString(string s)
        {
            if (s == null) return "\"\"";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static string UnescapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\\"", "\"")
                    .Replace("\\\\", "\\")
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t");
        }

    }
}
