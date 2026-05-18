using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// ????Console ????????Console ??????????????????
    /// </summary>
    public partial class EnhancedConsoleWindow : EditorWindow
    {
        #region UI Toolkit ????

        private const string UxmlPath = "Assets/AUnityLocal/UnityEnhancedConsole/Editor/EnhancedConsoleWindow.uxml";
        private const string UssPath = "Assets/AUnityLocal/UnityEnhancedConsole/Editor/EnhancedConsoleWindow.uss";
        #endregion
        
        #region ??????

        internal const string PrefCollapse = "EnhancedConsole.Collapse";
        internal const string PrefCollapseGlobal = "EnhancedConsole.CollapseGlobal";
        internal const string PrefClearOnPlay = "EnhancedConsole.ClearOnPlay";
        internal const string PrefClearOnBuild = "EnhancedConsole.ClearOnBuild";
        internal const string PrefErrorPause = "EnhancedConsole.ErrorPause";
        internal const string PrefShowLog = "EnhancedConsole.ShowLog";
        internal const string PrefShowWarning = "EnhancedConsole.ShowWarning";
        internal const string PrefShowError = "EnhancedConsole.ShowError";
        internal const string PrefEntryLines = "EnhancedConsole.EntryLines";
        internal const string PrefStackTraceLog = "EnhancedConsole.StackTraceLog";
        internal const string PrefStackTraceWarning = "EnhancedConsole.StackTraceWarning";
        internal const string PrefStackTraceError = "EnhancedConsole.StackTraceError";
        internal const string PrefDetailHeight = "EnhancedConsole.DetailHeight";
        internal const string PrefShowTimestamp = "EnhancedConsole.ShowTimestamp";
        internal const string PrefShowFrameCount = "EnhancedConsole.ShowFrameCount";
        internal const string PrefShowStackTrace = "EnhancedConsole.ShowStackTrace";
        internal const string PrefSearchRegex = "EnhancedConsole.SearchRegex";
        internal const string PrefSearchText = "EnhancedConsole.SearchText";
        internal const string PrefShowMessageNumber = "EnhancedConsole.ShowMessageNumber";
        internal const string PrefSearchHistoryPrefix = "EnhancedConsole.SearchHistory.";
        internal const int MaxSearchHistory = 20;
        internal const string PrefTagsEnabled = "EnhancedConsole.TagsEnabled";
        internal const string PrefSelectedTags = "EnhancedConsole.SelectedTags";
        internal const string PrefExcludedTags = "EnhancedConsole.ExcludedTags";
        internal const string PrefTagSortMode = "EnhancedConsole.TagSortMode";
        internal const string PrefTagSortDesc = "EnhancedConsole.TagSortDesc";
        internal const string PrefTagSearchText = "EnhancedConsole.TagSearchText";
        internal const string PrefMaxEntries = "EnhancedConsole.MaxEntries";
        internal const string PrefMaxLoadEntries = "EnhancedConsole.MaxLoadEntries";
        internal const string PrefViewLocked = "EnhancedConsole.ViewLocked";
        internal const string PrefFilterTimeRange = "EnhancedConsole.FilterTimeRange";
        internal const string PrefFilterNumberRange = "EnhancedConsole.FilterNumberRange";
        internal const string PrefFilterFrameRange = "EnhancedConsole.FilterFrameRange";
        internal const string PrefFilterTimeMin = "EnhancedConsole.FilterTimeMin";
        internal const string PrefFilterTimeMax = "EnhancedConsole.FilterTimeMax";
        internal const string PrefFilterNumberMin = "EnhancedConsole.FilterNumberMin";
        internal const string PrefFilterNumberMax = "EnhancedConsole.FilterNumberMax";
        internal const string PrefFilterFrameMin = "EnhancedConsole.FilterFrameMin";
        internal const string PrefFilterFrameMax = "EnhancedConsole.FilterFrameMax";
        private const float SplitterHeight = 5f;
        private const float TimeFrameGap = 0f;
        private const float MinListHeight = 60f;
        private const float MinDetailHeight = 60f;
        private const double MinFilterRebuildIntervalMs = 200;
        /// <summary> ????????????????????N ???</summary>
        private const int DefaultMaxEntries = 20000;
        /// <summary> ??????????????????????????????N ????</summary>
        private const int DefaultMaxLoadEntries = 50000;
        /// <summary> ?????????????????</summary>
        private const int ListVirtualBufferRows = 10;
        /// <summary> ??????AddEntry ??Repaint ???????????????</summary>
        private const double MinRepaintIntervalMs = 150;
        /// <summary> ?????? / ???????????????????????????</summary>
        private const int MaxCopyLines = 100000;

        private static List<LogEntry> PendingEntries = new List<LogEntry>();
        private static List<LogEntry> PendingEntriesBackBuffer = new List<LogEntry>();
        private static readonly object PendingLock = new object();

        /// <summary> ????????????????Clear ???????????????????</summary>
        private static bool _hasCompilationErrors;
        private static bool _currentCycleHasErrors;
        private static readonly int MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

        // ?????? (at path:line) ??in path:line
        private static readonly Regex StackLineRegex = new Regex(@"\s*\(at\s+(.+):(\d+)\)|\s+in\s+(.+):(\d+)", RegexOptions.Compiled);

        /// <summary> ???????Unity ?????????????????????????</summary>
        private static readonly Color[] TagColors = new[]
        {
            new Color(0.26f, 0.42f, 0.62f, 0.92f),  // ??
            new Color(0.22f, 0.48f, 0.48f, 0.92f),  // ??
            new Color(0.22f, 0.52f, 0.38f, 0.92f),  // ??
            new Color(0.48f, 0.48f, 0.28f, 0.92f),  // ??
            new Color(0.58f, 0.42f, 0.24f, 0.92f),  // ??
            new Color(0.58f, 0.32f, 0.34f, 0.92f),  // ??
            new Color(0.42f, 0.34f, 0.54f, 0.92f),  // ??
            new Color(0.52f, 0.32f, 0.48f, 0.92f), // ??
        };

        private static readonly Dictionary<string, string> _shortStringPool = new Dictionary<string, string>();
        private const int ShortStringPoolMaxSize = 5000;
        private static readonly Dictionary<string, string> _longStringPool = new Dictionary<string, string>();
        private const int LongStringPoolMaxSize = 2000;
        private const int LongStringMaxLen = 16 * 1024;
        private static readonly object _stringPoolLock = new object();

        private static string ShareShortString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length <= 256)
            {
                lock (_stringPoolLock)
                {
                    if (_shortStringPool.TryGetValue(s, out var existing)) return existing;
                    if (_shortStringPool.Count >= ShortStringPoolMaxSize)
                    {
                        int target = ShortStringPoolMaxSize * 3 / 4;
                        var keys = new List<string>(_shortStringPool.Keys);
                        int remove = _shortStringPool.Count - target;
                        for (int i = 0; i < remove && i < keys.Count; i++)
                            _shortStringPool.Remove(keys[i]);
                    }
                    _shortStringPool[s] = s;
                    return s;
                }
            }
            if (s.Length <= LongStringMaxLen)
            {
                lock (_stringPoolLock)
                {
                    if (_longStringPool.TryGetValue(s, out var existing)) return existing;
                    if (_longStringPool.Count >= LongStringPoolMaxSize)
                    {
                        int target = LongStringPoolMaxSize * 3 / 4;
                        var keys = new List<string>(_longStringPool.Keys);
                        int remove = _longStringPool.Count - target;
                        for (int i = 0; i < remove && i < keys.Count; i++)
                            _longStringPool.Remove(keys[i]);
                    }
                    _longStringPool[s] = s;
                    return s;
                }
            }
            return s;
        }

        private static Color GetTagColor(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return TagColors[0];
            int i = (tag.GetHashCode() & 0x7FFFFFFF) % TagColors.Length;
            return TagColors[i];
        }

        private static string ClampCount(int count, int cap)
        {
            return count > cap ? cap + "+" : count.ToString();
        }

        /// <summary> ??????????????</summary>
        public static bool HasCompilationErrors => _hasCompilationErrors;

        /// <summary> ????????????????Clear ?????Unity C# ??????????"error CS" ??": error "??</summary>
        private static bool IsCompilationErrorLog(LogEntry e)
        {
            if (e == null) return false;
            if (e.LogType != LogType.Error && e.LogType != LogType.Exception) return false;
            if (string.IsNullOrEmpty(e.Condition)) return false;
            return e.Condition.IndexOf("error CS", StringComparison.OrdinalIgnoreCase) >= 0
                   || e.Condition.IndexOf(": error ", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static EnhancedConsoleWindow()
        {
            CompilationPipeline.compilationStarted += _ => _currentCycleHasErrors = false;
            CompilationPipeline.assemblyCompilationFinished += (_, messages) =>
            {
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        if (m.type == CompilerMessageType.Error)
                        {
                            _currentCycleHasErrors = true;
                            break;
                        }
                    }
                }
            };
            CompilationPipeline.compilationFinished += _ =>
            {
                _hasCompilationErrors = _currentCycleHasErrors;
                foreach (var w in Resources.FindObjectsOfTypeAll<EnhancedConsoleWindow>())
                {
                    if (w == null) continue;
                    w.RefreshUI();
                }
            };
        }

        #endregion

        #region ???

        private readonly List<LogEntry> _entries = new List<LogEntry>();
        private int _selectedIndex = -1;
        private string _search = "";
        private bool _searchRegex;
        private bool _collapse;
        private bool _collapseGlobal = true;
        private bool _clearOnPlay;
        private bool _clearOnBuild;
        private bool _errorPause;
        private bool _showLog = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private bool _showTimestamp;
        private bool _showFrameCount;
        private bool _showMessageNumber;
        private bool _showStackTrace = true;
        private int _entryLines = 2;
        private int _maxEntries = DefaultMaxEntries;
        private int _maxLoadEntries = DefaultMaxLoadEntries;
        private int _nextMessageNumber;
        private StackTraceLogType _stackTraceLog = StackTraceLogType.ScriptOnly;
        private StackTraceLogType _stackTraceWarning = StackTraceLogType.ScriptOnly;
        private StackTraceLogType _stackTraceError = StackTraceLogType.ScriptOnly;
        private float _detailHeight = 120f;
        private readonly HashSet<string> _selectedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _tagsCollapsed = true;
        private readonly HashSet<string> _excludedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _searchHistory = new List<string>();
        private bool _tagsEnabled = true;
        private TagSortMode _tagSortMode = TagSortMode.Name;
        private bool _tagSortDesc;
        private string _tagSearch = "";
        private double _lastFilterRebuildTime;
        private bool _filterTimeRange;
        private bool _filterNumberRange;
        private bool _filterFrameRange;
        private string _filterTimeMin = "";
        private string _filterTimeMax = "";
        private int _filterNumberMin = 1;
        private int _filterNumberMax = int.MaxValue;
        private int _filterFrameMin = 0;
        private int _filterFrameMax = int.MaxValue;
        private Regex _cachedSearchRegex;
        private string _cachedSearchPattern;
        private List<FilteredRow> _cachedFilteredRows;
        private Dictionary<int, CollapseGroupInfo> _cachedCollapseGroupInfo;
        private bool _filterDirty = true;
        // 增量过滤：仅当上次完整过滤后只有 _entries 末尾追加时为 true，可走增量路径。
        // 任何会破坏 entryIndex 稳定性 / 改变筛选条件 / 改变 _entries 中已存在元素的操作都必须设为 false。
        private bool _filterAppendOnly = false;
        private int _lastFilteredEntriesEnd = 0;
        // collapse 全局模式下的分组缓存（key→该 group 的 list 行索引）
        private Dictionary<LogKey, int> _collapseRowIndexByKey;
        private System.Threading.CancellationTokenSource _filterCts;
        private bool _filterRebuildPending;
        // 每次过滤条件（搜索/类型/tag/折叠/范围 等）改变时由 InvalidateFilterCriteria() 递增。
        // 后台过滤任务用此版本号判断"我跑出的结果是否还匹配当前条件"，不匹配则丢弃。
        private int _filterCriteriaVersion;
        // 行渲染缓存版本：showMessageNumber/showTimestamp/showFrameCount/entryLines/searchApplied/searchRegex 任一变化即 ++
        // bindItem 用此判断 LogEntry.CachedDisplayText 是否仍然有效。
        private int _displayCacheVersion;
        private int _lastDisplayParamsHash = -1;

        /// <summary>
        /// 标记所有"行显示文本缓存"已过期。任何会改变行 msg 输出（前缀/折行/搜索高亮）的状态变化都应调用此方法。
        /// </summary>
        private void InvalidateDisplayCache() { _displayCacheVersion++; }
        private bool _listViewNeedsRebuild;
        private List<FilteredRow> _currentFilteredRowsForBinding;
        private Dictionary<string, TagInfo> _cachedTagInfo;
        private bool _tagCountsDirty = true;
        private string _searchApplied = "";
        private double _searchInputLastChangeTime;
        private double _lastRepaintTime;
        private bool _repaintScheduled; // legacy, kept for binary serialization compat; no longer used
        private bool _viewLocked;
        private readonly List<LogEntry> _flushBuffer = new List<LogEntry>();
        private int _deferredTagComputeIndex;
        private bool _deferredTagComputing;
        private int _unfilteredLogCount;
        private int _unfilteredWarnCount;
        private int _unfilteredErrCount;
        private Button _remoteButton;

        /* UI Toolkit ?? */
        private ListView _logListView;
        private bool _logListContextBound;
        private TextField _detailField;
        // private VisualElement _detailLinks; // removed (double-click to open)
        private TextField _searchField;
        private TwoPaneSplitView _mainSplit;
        private VisualElement _tagBarContainer;
        private readonly Dictionary<string, Button> _tagButtonPool = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _lastTagBarOrder = new List<string>();
        private Texture2D _iconLog;
        private Texture2D _iconWarning;
        private Texture2D _iconError;
        private Vector2 _detailMouseDownPos;
        private bool _detailMouseDownTracking;


        #endregion

        [MenuItem("Window/General/Enhanced Console %#d", false, 2000)]
        public static void Open()
        {
            var existing = Resources.FindObjectsOfTypeAll<EnhancedConsoleWindow>();
            if (existing != null && existing.Length > 0)
            {
                var focused = EditorWindow.focusedWindow as EnhancedConsoleWindow;
                if (focused != null)
                {
                    focused.Close();
                    return;
                }
                existing[0].Close();
                return;
            }
            var w = GetWindow<EnhancedConsoleWindow>("Enhanced Console", true);
            w.minSize = new Vector2(300, 200);
            w.Focus();
        }

        // 异步加载历史日志的 token：OnDisable 时取消
        private System.Threading.CancellationTokenSource _loadHistoryCts;
        private bool _historyLoading;

        private void OnEnable()
        {
            LoadPrefs();
            _entries.Clear();
            _nextMessageNumber = 0;
            _cachedFilteredRows = null;
            _cachedTagInfo = null;
            _filterAppendOnly = false;
            _filterDirty = true;
            _filterCriteriaVersion++;
            _tagCountsDirty = true;

            // 阶段 A：先注册回调，让历史加载期间发生的新日志先进 PendingEntries（lock 保护）
            EnhancedConsoleTagLogic.PrimeCachesMainThread();
            Application.logMessageReceivedThreaded += HandleLogThreaded;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            LoadIcons();
            BuildUI();
            ApplyStackTraceSettings();
            EditorApplication.update += ApplySearchDebounced;
            EditorApplication.update += FlushPendingEntries;
            EditorApplication.update += TickRefreshUI;

            // Auto-start remote server if configured
            RemoteConsoleServer.OnClientCountChanged += OnRemoteClientCountChanged;
            if (RemoteConsoleServer.GetAutoStart() && !RemoteConsoleServer.IsRunning)
                RemoteConsoleServer.Start();

            // 阶段 B/C：异步读文件，完成后回主线程 swap
            _loadHistoryCts?.Cancel();
            _loadHistoryCts = new System.Threading.CancellationTokenSource();
            var cts = _loadHistoryCts;
            int maxLoad = _maxLoadEntries;
            _historyLoading = true;
            Task.Run(() =>
            {
                List<LogEntry> loaded = null;
                try { loaded = EnhancedConsoleLogFile.LoadEntries(maxLoad); }
                catch { loaded = new List<LogEntry>(); }
                if (cts.IsCancellationRequested) return;
                EditorApplication.delayCall += () =>
                {
                    if (cts.IsCancellationRequested) return;
                    OnHistoryLoaded(loaded);
                };
            });
        }

        private void OnHistoryLoaded(List<LogEntry> loaded)
        {
            _historyLoading = false;
            if (loaded == null || loaded.Count == 0)
            {
                _nextMessageNumber = _entries.Count;
                CleanupStaleTags();
                RecalculateUnfilteredCounts();
                _filterAppendOnly = false;
                _filterDirty = true;
                _filterCriteriaVersion++;
                _tagCountsDirty = true;
                RefreshUI();
                return;
            }
            // 历史日志放在前面，已收到的新日志（_entries 已有内容）顺延到末尾
            // 重排序号：1..N
            int total = loaded.Count + _entries.Count;
            // 先把 loaded 拷贝到一个新列表，再 append 已有 _entries
            var existing = new List<LogEntry>(_entries);
            _entries.Clear();
            _entries.AddRange(loaded);
            _entries.AddRange(existing);
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e == null) continue;
                if (string.IsNullOrEmpty(e.FirstTimeStamp)) e.FirstTimeStamp = e.TimeStamp;
                if (string.IsNullOrEmpty(e.LastTimeStamp)) e.LastTimeStamp = e.TimeStamp;
                e.MessageNumber = i + 1;
            }
            _nextMessageNumber = _entries.Count;
            TrimEntriesToMax();

            _deferredTagComputeIndex = 0;
            _deferredTagComputing = _entries.Count > 0;
            if (_deferredTagComputing)
                StartDeferredComputeTags();

            CleanupStaleTags();
            RecalculateUnfilteredCounts();
            _filterAppendOnly = false;
            _filterDirty = true;
            _filterCriteriaVersion++;
            _tagCountsDirty = true;
            RefreshUI();
        }

        /// <summary>
        /// 搜索 fast-path：若新 search 是旧 search 的超集（new.StartsWith(old)，非 regex），
        /// 且非 collapse 模式且 cache 存在 → 在 _cachedFilteredRows 上原地缩减，避免全量重扫 _entries。
        /// 返回 true 表示 fast-path 已处理，调用方无需 invalidate filter。
        /// </summary>
        private bool TrySearchShrinkInPlace(string prevApplied, string newApplied)
        {
            if (_searchRegex) return false;
            if (_collapse) return false; // collapse 分组依赖整组，简化起见跳过
            if (_cachedFilteredRows == null) return false;
            if (string.IsNullOrEmpty(prevApplied)) return false; // 旧无搜索 → 无法保证新结果是旧的子集
            if (string.IsNullOrEmpty(newApplied)) return false;
            if (!newApplied.StartsWith(prevApplied, StringComparison.OrdinalIgnoreCase)) return false;
            // 原地保留满足新搜索的行
            var rows = _cachedFilteredRows;
            int write = 0;
            for (int read = 0; read < rows.Count; read++)
            {
                int ei = rows[read].entryIndex;
                if (ei < 0 || ei >= _entries.Count) continue;
                var cond = _entries[ei].Condition;
                if (cond != null && cond.IndexOf(newApplied, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (write != read) rows[write] = rows[read];
                    write++;
                }
            }
            if (write < rows.Count) rows.RemoveRange(write, rows.Count - write);
            _collapseRowIndexByKey = null; // 索引可能因行删除而失效
            return true;
        }

        /// <summary>
        /// ????????? 0.35s ???????????
        /// </summary>
        private void ApplySearchDebounced()
        {
            if (_search == _searchApplied) return;
            if ((EditorApplication.timeSinceStartup - _searchInputLastChangeTime) < 0.35) return;
            string prev = _searchApplied;
            _searchApplied = _search;
            // 显示缓存必失效（高亮变化）
            InvalidateDisplayCache();
            // fast-path：搜索仅在原结果上收紧 → 跳过 filter task
            if (TrySearchShrinkInPlace(prev, _searchApplied))
            {
                _filterCriteriaVersion++;
                _filterDirty = false;
                _filterAppendOnly = false;
                _lastFilteredEntriesEnd = _entries.Count;
                SavePrefs();
                RefreshUI();
                return;
            }
            _filterAppendOnly = false;
            _filterDirty = true;
            _filterCriteriaVersion++;
            _tagCountsDirty = true;
            SavePrefs();
            RefreshUI();
        }

        private void LoadIcons()
        {
            _iconLog = GetIconTexture("console.infoicon") ?? GetIconTexture("d_console.infoicon");
            _iconWarning = GetIconTexture("console.warnicon") ?? GetIconTexture("d_console.warnicon");
            _iconError = GetIconTexture("console.erroricon") ?? GetIconTexture("d_console.erroricon");
        }

        private static Texture2D GetIconTexture(string iconName)
        {
            try
            {
                var content = EditorGUIUtility.IconContent(iconName);
                return content?.image as Texture2D;
            }
            catch { return null; }
        }

        private void OnDisable()
        {
            _filterCts?.Cancel();
            _loadHistoryCts?.Cancel();
            _deferredTagComputing = false;
            EditorApplication.update -= ApplySearchDebounced;
            EditorApplication.update -= FlushPendingEntries;
            EditorApplication.update -= TickRefreshUI;
            Application.logMessageReceivedThreaded -= HandleLogThreaded;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            RemoteConsoleServer.OnClientCountChanged -= OnRemoteClientCountChanged;
            SavePrefs();
        }

        private void OnDetailPaneGeometryChanged(GeometryChangedEvent evt)
        {
            float h = evt.newRect.height;
            if (h >= MinDetailHeight && Mathf.Abs(h - _detailHeight) > 0.5f)
            {
                _detailHeight = h;
                EditorPrefs.SetFloat(PrefDetailHeight, _detailHeight);
            }
        }

        private void StartDeferredComputeTags()
        {
            if (!_deferredTagComputing) return;
            var entriesSnapshot = _entries.ToList();
            Task.Run(() =>
            {
                const int batch = 500;
                for (int idx = 0; idx < entriesSnapshot.Count; idx += batch)
                {
                    if (!_deferredTagComputing) break;
                    int end = Mathf.Min(idx + batch, entriesSnapshot.Count);
                    for (int i = idx; i < end; i++)
                    {
                        EnhancedConsoleTagLogic.ComputeTags(entriesSnapshot[i]);
                    }
                }
                _deferredTagComputing = false;
                EditorApplication.delayCall += () =>
                {
                    _tagCountsDirty = true;
                    RefreshUI();
                };
            });
        }

        private void HandleLogThreaded(string condition, string stackTrace, LogType type)
        {
            var entry = CreateEntry(condition, stackTrace, type);
            // 在后台线程预先计算 tag，避免主线程 Flush 时阻塞
            try { EnhancedConsoleTagLogic.ComputeTags(entry); } catch { }
            lock (PendingLock)
            {
                PendingEntries.Add(entry);
            }
        }

        /// <summary>
        /// 由 RemoteConsoleServer 调用：直接将远程日志注入 pending 队列，保留远程 stackTrace。
        /// 不走 Debug.Log，避免 Unity 内置控制台双写 + stack 被本地堆栈覆盖。
        /// </summary>
        internal static void EnqueueRemoteLog(string condition, string stackTrace, LogType type)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var entry = new LogEntry
            {
                Condition = ShareShortString(condition ?? ""),
                StackTrace = ShareShortString(stackTrace ?? ""),
                LogType = type,
                Count = 1,
                TimeStamp = timeStamp,
                FirstTimeStamp = timeStamp,
                LastTimeStamp = timeStamp,
                FrameCount = 0,
                Tags = null // 由后台线程 ComputeTags 填充；主线程 Flush 时 EnsureTagsComputed 兜底
            };
            try { EnhancedConsoleTagLogic.ComputeTags(entry); } catch { }
            lock (PendingLock)
            {
                PendingEntries.Add(entry);
            }
        }

        private LogEntry CreateEntry(string condition, string stackTrace, LogType type)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var entry = new LogEntry
            {
                Condition = ShareShortString(condition ?? ""),
                StackTrace = _showStackTrace ? ShareShortString(stackTrace ?? "") : "",
                LogType = type,
                Count = 1,
                TimeStamp = timeStamp,
                FirstTimeStamp = timeStamp,
                LastTimeStamp = timeStamp,
                FrameCount = GetFrameCountSafe(),
                Tags = null
            };
            return entry;
        }

        /// <summary>
        /// 线程安全地获取 FrameCount：仅在主线程且播放模式下调用 Unity API
        /// </summary>
        private static int GetFrameCountSafe()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != MainThreadId)
                return 0;
            try
            {
                return Application.isPlaying ? Time.frameCount : 0;
            }
            catch
            {
                return 0;
            }
        }

        private void AddEntry(string condition, string stackTrace, LogType type)
        {
            var entry = CreateEntry(condition, stackTrace, type);

            if (_collapse && _entries.Count > 0)
            {
                var last = _entries[_entries.Count - 1];
                if (last.IsSameContent(entry))
                {
                    bool tagCacheActive = _cachedTagInfo != null && !_tagCountsDirty;
                    last.Count++;
                    if (string.IsNullOrEmpty(last.FirstTimeStamp)) last.FirstTimeStamp = last.TimeStamp;
                    last.LastTimeStamp = entry.TimeStamp;
                    // 优先尝试 O(1) 行更新，避免全量重建
                    if (!TryFastCollapseHitUpdate(last, 1, entry.TimeStamp))
                    {
                        _filterDirty = true;
                        _filterAppendOnly = false;
                    }
                    UpdateTagCacheForEntry(last, 1, entry.TimeStamp);
                    if (!tagCacheActive) _tagCountsDirty = true;
                    if (_errorPause && (type == LogType.Error || type == LogType.Exception) && EditorApplication.isPlaying)
                        EditorApplication.isPaused = true;
                    RepaintThrottled();
                    return;
                }
            }
            EnhancedConsoleTagLogic.ComputeTags(entry);
            entry.MessageNumber = GetAndAdvanceNextMessageNumber();
            _entries.Add(entry);
            bool tagCacheActive2 = _cachedTagInfo != null && !_tagCountsDirty;
            UpdateTagCacheForEntry(entry, entry.Count, entry.TimeStamp);
            // 末尾追加：如已无前置 dirty 则可走增量
            if (!_filterDirty || _filterAppendOnly)
            {
                _filterAppendOnly = true;
            }
            _filterDirty = true;
            if (!tagCacheActive2) _tagCountsDirty = true;
            TrimEntriesToMax();

            if (_errorPause && (type == LogType.Error || type == LogType.Exception))
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.isPaused = true;
            }

            RepaintThrottled();
        }

        private const int MaxFlushEntriesPerFrame = 1000;

        private void FlushPendingEntries()
        {
            lock (PendingLock)
            {
                int count = PendingEntries.Count;
                if (count == 0) return;
                if (count <= MaxFlushEntriesPerFrame)
                {
                    // 快速路径：整桶交换，O(1)，避免 RemoveRange(0,N) 的搬移开销
                    var swap = PendingEntries;
                    PendingEntries = PendingEntriesBackBuffer;
                    PendingEntriesBackBuffer = swap;
                    // _flushBuffer 应是空的（上一帧 Clear 过）；直接 AddRange 取过来
                    _flushBuffer.AddRange(swap);
                    swap.Clear();
                }
                else
                {
                    int take = MaxFlushEntriesPerFrame;
                    for (int i = 0; i < take; i++)
                        _flushBuffer.Add(PendingEntries[i]);
                    PendingEntries.RemoveRange(0, take);
                }
            }
            // 提前确保规则 / 过滤策略缓存已被 LoadRules/LoadFilterSettings prime（主线程读取 EditorPrefs）
            EnhancedConsoleTagLogic.PrimeCachesMainThread();

            foreach (var e in _flushBuffer)
            {
                if (_collapse && _entries.Count > 0)
                {
                    var last = _entries[_entries.Count - 1];
                    if (last.IsSameContent(e))
                    {
                        bool tagCacheActive = _cachedTagInfo != null && !_tagCountsDirty;
                        last.Count++;
                        if (string.IsNullOrEmpty(last.FirstTimeStamp)) last.FirstTimeStamp = last.TimeStamp;
                        last.LastTimeStamp = e.TimeStamp;
                        if (!TryFastCollapseHitUpdate(last, 1, e.TimeStamp))
                        {
                            _filterDirty = true;
                            _filterAppendOnly = false;
                        }
                        UpdateUnfilteredCounts(last, 1);
                        UpdateTagCacheForEntry(last, 1, e.TimeStamp);
                        if (!tagCacheActive) _tagCountsDirty = true;
                        if (_errorPause && (e.LogType == LogType.Error || e.LogType == LogType.Exception) && EditorApplication.isPlaying)
                            EditorApplication.isPaused = true;
                        continue;
                    }
                }
                EnhancedConsoleTagLogic.EnsureTagsComputed(e);
                e.MessageNumber = GetAndAdvanceNextMessageNumber();
                _entries.Add(e);
                UpdateUnfilteredCounts(e, e.Count);
                bool tagCacheActive2 = _cachedTagInfo != null && !_tagCountsDirty;
                UpdateTagCacheForEntry(e, e.Count, e.TimeStamp);
                if (!_filterDirty || _filterAppendOnly)
                {
                    _filterAppendOnly = true;
                }
                _filterDirty = true;
                if (!tagCacheActive2) _tagCountsDirty = true;
                if (_errorPause && (e.LogType == LogType.Error || e.LogType == LogType.Exception))
                {
                    if (EditorApplication.isPlaying)
                        EditorApplication.isPaused = true;
                }
            }
            _flushBuffer.Clear();
            TrimEntriesToMax();
            if (!_viewLocked)
                RepaintThrottled();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!_clearOnPlay) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
                Clear();
        }

        private void LoadPrefs()
        {
            _collapse = EditorPrefs.GetBool(PrefCollapse, false);
            _collapseGlobal = EditorPrefs.GetBool(PrefCollapseGlobal, true);
            _clearOnPlay = EditorPrefs.GetBool(PrefClearOnPlay, false);
            _clearOnBuild = EditorPrefs.GetBool(PrefClearOnBuild, false);
            _errorPause = EditorPrefs.GetBool(PrefErrorPause, false);
            _showLog = EditorPrefs.GetBool(PrefShowLog, true);
            _showWarning = EditorPrefs.GetBool(PrefShowWarning, true);
            _showError = EditorPrefs.GetBool(PrefShowError, true);
            _entryLines = EditorPrefs.GetInt(PrefEntryLines, 2);
            _entryLines = Mathf.Clamp(_entryLines, 1, 10);
            _maxEntries = Mathf.Max(100, EditorPrefs.GetInt(PrefMaxEntries, DefaultMaxEntries));
            _maxLoadEntries = Mathf.Max(_maxEntries, EditorPrefs.GetInt(PrefMaxLoadEntries, DefaultMaxLoadEntries));
            _stackTraceLog = (StackTraceLogType)EditorPrefs.GetInt(PrefStackTraceLog, (int)StackTraceLogType.ScriptOnly);
            _stackTraceWarning = (StackTraceLogType)EditorPrefs.GetInt(PrefStackTraceWarning, (int)StackTraceLogType.ScriptOnly);
            _stackTraceError = (StackTraceLogType)EditorPrefs.GetInt(PrefStackTraceError, (int)StackTraceLogType.ScriptOnly);
            _detailHeight = Mathf.Max(MinDetailHeight, EditorPrefs.GetFloat(PrefDetailHeight, 120f));
            _showTimestamp = EditorPrefs.GetBool(PrefShowTimestamp, false);
            _showFrameCount = EditorPrefs.GetBool(PrefShowFrameCount, false);
            _showStackTrace = EditorPrefs.GetBool(PrefShowStackTrace, true);
            _searchRegex = EditorPrefs.GetBool(PrefSearchRegex, false);
            _search = EditorPrefs.GetString(PrefSearchText, "");
            _searchApplied = _search;
            _showMessageNumber = EditorPrefs.GetBool(PrefShowMessageNumber, false);
            _tagsEnabled = EditorPrefs.GetBool(PrefTagsEnabled, true);
            _tagSortMode = (TagSortMode)EditorPrefs.GetInt(PrefTagSortMode, (int)TagSortMode.Name);
            _tagSortDesc = EditorPrefs.GetBool(PrefTagSortDesc, false);
            _tagSearch = EditorPrefs.GetString(PrefTagSearchText, "");
            _viewLocked = EditorPrefs.GetBool(PrefViewLocked, false);
            _filterTimeRange = EditorPrefs.GetBool(PrefFilterTimeRange, false);
            _filterNumberRange = EditorPrefs.GetBool(PrefFilterNumberRange, false);
            _filterFrameRange = EditorPrefs.GetBool(PrefFilterFrameRange, false);
            _filterTimeMin = EditorPrefs.GetString(PrefFilterTimeMin, "");
            _filterTimeMax = EditorPrefs.GetString(PrefFilterTimeMax, "");
            _filterNumberMin = EditorPrefs.GetInt(PrefFilterNumberMin, 1);
            _filterNumberMax = EditorPrefs.GetInt(PrefFilterNumberMax, int.MaxValue);
            _filterFrameMin = EditorPrefs.GetInt(PrefFilterFrameMin, 0);
            _filterFrameMax = EditorPrefs.GetInt(PrefFilterFrameMax, int.MaxValue);
            _selectedTags.Clear();
            foreach (var t in DeserializeStringList(EditorPrefs.GetString(PrefSelectedTags, "")))
                if (!string.IsNullOrEmpty(t)) _selectedTags.Add(t);
            _excludedTags.Clear();
            foreach (var t in DeserializeStringList(EditorPrefs.GetString(PrefExcludedTags, "")))
                if (!string.IsNullOrEmpty(t)) _excludedTags.Add(t);
            LoadSearchHistory();
        }

        private void LoadSearchHistory()
        {
            _searchHistory.Clear();
            for (int i = 0; i < MaxSearchHistory; i++)
            {
                string s = EditorPrefs.GetString(PrefSearchHistoryPrefix + i, "");
                if (!string.IsNullOrEmpty(s))
                    _searchHistory.Add(s);
            }
        }

        private void SaveSearchHistory()
        {
            for (int i = 0; i < MaxSearchHistory; i++)
            {
                string key = PrefSearchHistoryPrefix + i;
                if (i < _searchHistory.Count)
                    EditorPrefs.SetString(key, _searchHistory[i]);
                else
                    EditorPrefs.DeleteKey(key);
            }
        }

        /// <summary>
        /// ??????????????????????MaxSearchHistory ????
        /// </summary>
        private void PushSearchHistory(string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return;
            string s = search.Trim();
            _searchHistory.RemoveAll(x => string.Equals(x, s, StringComparison.Ordinal));
            _searchHistory.Insert(0, s);
            while (_searchHistory.Count > MaxSearchHistory)
                _searchHistory.RemoveAt(_searchHistory.Count - 1);
            SaveSearchHistory();
        }

        private static string SerializeStringList(IEnumerable<string> items)
        {
            var list = new List<string>();
            if (items != null)
            {
                foreach (var s in items)
                {
                    if (!string.IsNullOrEmpty(s))
                        list.Add(s);
                }
            }
            var wrapper = new StringListWrapper { items = list };
            return JsonUtility.ToJson(wrapper);
        }

        private static List<string> DeserializeStringList(string json)
        {
            if (string.IsNullOrEmpty(json)) return new List<string>();
            try
            {
                var wrapper = JsonUtility.FromJson<StringListWrapper>(json);
                return wrapper?.items ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static string GetEntryFirstTime(LogEntry e)
        {
            if (e == null) return null;
            return !string.IsNullOrEmpty(e.FirstTimeStamp) ? e.FirstTimeStamp : e.TimeStamp;
        }

        private static string GetEntryLastTime(LogEntry e)
        {
            if (e == null) return null;
            return !string.IsNullOrEmpty(e.LastTimeStamp) ? e.LastTimeStamp : e.TimeStamp;
        }

        public static void NotifySettingsChanged()
        {
            var windows = Resources.FindObjectsOfTypeAll<EnhancedConsoleWindow>();
            if (windows != null)
            {
                foreach (var w in windows)
                {
                    if (w == null) continue;
                    w.LoadPrefs();
                    w._filterAppendOnly = false;
                    w._filterDirty = true;
                    w._tagCountsDirty = true;
                    w.RefreshUI();
                }
            }
        }

        private static void UpdateGroupTimeRange(ref CollapseGroupState state, LogEntry e)
        {
            string first = GetEntryFirstTime(e);
            string last = GetEntryLastTime(e);
            if (string.IsNullOrEmpty(state.firstTime) || (!string.IsNullOrEmpty(first) && string.Compare(first, state.firstTime, StringComparison.Ordinal) < 0))
                state.firstTime = first;
            if (string.IsNullOrEmpty(state.lastTime) || (!string.IsNullOrEmpty(last) && string.Compare(last, state.lastTime, StringComparison.Ordinal) > 0))
                state.lastTime = last;
        }

        private void SavePrefs()
        {
            EditorPrefs.SetBool(PrefCollapse, _collapse);
            EditorPrefs.SetBool(PrefCollapseGlobal, _collapseGlobal);
            EditorPrefs.SetBool(PrefClearOnPlay, _clearOnPlay);
            EditorPrefs.SetBool(PrefClearOnBuild, _clearOnBuild);
            EditorPrefs.SetBool(PrefErrorPause, _errorPause);
            EditorPrefs.SetBool(PrefShowLog, _showLog);
            EditorPrefs.SetBool(PrefShowWarning, _showWarning);
            EditorPrefs.SetBool(PrefShowError, _showError);
            EditorPrefs.SetInt(PrefEntryLines, _entryLines);
            EditorPrefs.SetInt(PrefMaxEntries, _maxEntries);
            EditorPrefs.SetInt(PrefMaxLoadEntries, _maxLoadEntries);
            EditorPrefs.SetInt(PrefStackTraceLog, (int)_stackTraceLog);
            EditorPrefs.SetInt(PrefStackTraceWarning, (int)_stackTraceWarning);
            EditorPrefs.SetInt(PrefStackTraceError, (int)_stackTraceError);
            EditorPrefs.SetFloat(PrefDetailHeight, _detailHeight);
            EditorPrefs.SetBool(PrefShowTimestamp, _showTimestamp);
            EditorPrefs.SetBool(PrefShowFrameCount, _showFrameCount);
            EditorPrefs.SetBool(PrefShowStackTrace, _showStackTrace);
            EditorPrefs.SetBool(PrefSearchRegex, _searchRegex);
            EditorPrefs.SetString(PrefSearchText, _searchApplied ?? "");
            EditorPrefs.SetBool(PrefShowMessageNumber, _showMessageNumber);
            EditorPrefs.SetBool(PrefTagsEnabled, _tagsEnabled);
            EditorPrefs.SetInt(PrefTagSortMode, (int)_tagSortMode);
            EditorPrefs.SetBool(PrefTagSortDesc, _tagSortDesc);
            EditorPrefs.SetString(PrefTagSearchText, _tagSearch ?? "");
            EditorPrefs.SetBool(PrefViewLocked, _viewLocked);
            EditorPrefs.SetBool(PrefFilterTimeRange, _filterTimeRange);
            EditorPrefs.SetBool(PrefFilterNumberRange, _filterNumberRange);
            EditorPrefs.SetBool(PrefFilterFrameRange, _filterFrameRange);
            EditorPrefs.SetString(PrefFilterTimeMin, _filterTimeMin);
            EditorPrefs.SetString(PrefFilterTimeMax, _filterTimeMax);
            EditorPrefs.SetInt(PrefFilterNumberMin, _filterNumberMin);
            EditorPrefs.SetInt(PrefFilterNumberMax, _filterNumberMax);
            EditorPrefs.SetInt(PrefFilterFrameMin, _filterFrameMin);
            EditorPrefs.SetInt(PrefFilterFrameMax, _filterFrameMax);
            EditorPrefs.SetString(PrefSelectedTags, SerializeStringList(_selectedTags));
            EditorPrefs.SetString(PrefExcludedTags, SerializeStringList(_excludedTags));
        }

        private void ApplyStackTraceSettings()
        {
            Application.SetStackTraceLogType(LogType.Log, (UnityEngine.StackTraceLogType)_stackTraceLog);
            Application.SetStackTraceLogType(LogType.Warning, (UnityEngine.StackTraceLogType)_stackTraceWarning);
            Application.SetStackTraceLogType(LogType.Error, (UnityEngine.StackTraceLogType)_stackTraceError);
            Application.SetStackTraceLogType(LogType.Exception, (UnityEngine.StackTraceLogType)_stackTraceError);
        }

        /// <summary>
        /// ?????????GetFilteredRows ??????????????????
        /// Collapse ??displayCount ?? &gt;1?????????????
        /// </summary>
        private (int log, int warn, int err) CountByType()
        {
            var rows = GetFilteredRows();
            int log = 0, warn = 0, err = 0;
            foreach (var row in rows)
            {
                var e = _entries[row.entryIndex];
                int c = row.displayCount;
                switch (e.LogType)
                {
                    case LogType.Log:
                    case LogType.Assert: log += c; break;
                    case LogType.Warning: warn += c; break;
                    case LogType.Error:
                    case LogType.Exception: err += c; break;
                }
            }
            return (log, warn, err);
        }

        /// <summary>
        /// ????????????????????????????????? Toggle ????????
        /// Collapse ?? e.Count ????
        /// </summary>
        private (int log, int warn, int err) CountByTypeUnfiltered()
        {
            return (_unfilteredLogCount, _unfilteredWarnCount, _unfilteredErrCount);
        }

        private void UpdateUnfilteredCounts(LogEntry e, int delta)
        {
            if (e == null) return;
            switch (e.LogType)
            {
                case LogType.Log:
                case LogType.Assert: _unfilteredLogCount += delta; break;
                case LogType.Warning: _unfilteredWarnCount += delta; break;
                case LogType.Error:
                case LogType.Exception: _unfilteredErrCount += delta; break;
            }
        }

        private void RecalculateUnfilteredCounts()
        {
            _unfilteredLogCount = 0;
            _unfilteredWarnCount = 0;
            _unfilteredErrCount = 0;
            foreach (var e in _entries)
            {
                if (e == null) continue;
                int c = e.Count;
                switch (e.LogType)
                {
                    case LogType.Log:
                    case LogType.Assert: _unfilteredLogCount += c; break;
                    case LogType.Warning: _unfilteredWarnCount += c; break;
                    case LogType.Error:
                    case LogType.Exception: _unfilteredErrCount += c; break;
                }
            }
        }

        private bool ShowType(LogType type)
        {
            switch (type)
            {
                case LogType.Log:
                case LogType.Assert: return _showLog;
                case LogType.Warning: return _showWarning;
                case LogType.Error:
                case LogType.Exception: return _showError;
                default: return true;
            }
        }

        /// <summary>
        /// ?????????????????????? _search ???????????????????
        /// </summary>
        private Regex GetOrCreateSearchRegex()
        {
            if (!_searchRegex || string.IsNullOrEmpty(_searchApplied))
            {
                _cachedSearchRegex = null;
                _cachedSearchPattern = null;
                return null;
            }
            if (_cachedSearchPattern == _searchApplied && _cachedSearchRegex != null)
                return _cachedSearchRegex;
            try
            {
                _cachedSearchRegex = new Regex(_searchApplied, RegexOptions.Compiled);
                _cachedSearchPattern = _searchApplied;
                return _cachedSearchRegex;
            }
            catch
            {
                _cachedSearchRegex = null;
                _cachedSearchPattern = null;
                return null;
            }
        }

        private bool EntryMatchesSearch(LogEntry e, Regex compiledRegex)
        {
            if (string.IsNullOrEmpty(_searchApplied)) return true;
            if (e?.Condition == null) return false;
            if (compiledRegex != null)
            {
                try { return compiledRegex.IsMatch(e.Condition); }
                catch { return false; }
            }
            return e.Condition.IndexOf(_searchApplied, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool EntryMatchesTimeRange(LogEntry e)
        {
            if (!_filterTimeRange) return true;
            if (e == null || string.IsNullOrEmpty(e.TimeStamp)) return string.IsNullOrEmpty(_filterTimeMin) && string.IsNullOrEmpty(_filterTimeMax);
            if (!string.IsNullOrEmpty(_filterTimeMin) && string.Compare(e.TimeStamp, _filterTimeMin, StringComparison.Ordinal) < 0) return false;
            if (!string.IsNullOrEmpty(_filterTimeMax) && string.Compare(e.TimeStamp, _filterTimeMax, StringComparison.Ordinal) > 0) return false;
            return true;
        }

        private bool EntryMatchesNumberRange(LogEntry e)
        {
            if (!_filterNumberRange) return true;
            if (e == null) return false;
            return e.MessageNumber >= _filterNumberMin && e.MessageNumber <= _filterNumberMax;
        }

        private bool EntryMatchesFrameRange(LogEntry e)
        {
            if (!_filterFrameRange) return true;
            if (e == null) return false;
            return e.FrameCount >= _filterFrameMin && e.FrameCount <= _filterFrameMax;
        }

        private bool EntryMatchesFiltersExceptTag(LogEntry e, Regex searchRegex)
        {
            if (!EntryMatchesSearch(e, searchRegex)) return false;
            if (!EntryMatchesTimeRange(e)) return false;
            if (!EntryMatchesNumberRange(e)) return false;
            if (!EntryMatchesFrameRange(e)) return false;
            if (!ShowType(e.LogType)) return false;
            return true;
        }

        private void UpdateTagCacheForEntry(LogEntry e, int addCount, string lastTime)
        {
            // 仅维护 tag 名集合（不再统计 count / lastTime），与 GetAllTagsFromRowsWithoutTagFilter 一致
            if (_cachedTagInfo == null) return;
            if (e == null) return;
            var tags = e.TagsOrEmpty;
            for (int i = 0; i < tags.Length; i++)
            {
                var t = tags[i];
                if (string.IsNullOrEmpty(t)) continue;
                if (!_cachedTagInfo.ContainsKey(t))
                    _cachedTagInfo[t] = default(TagInfo);
            }
        }

        /// <summary>
        /// ?????????????? Condition ????????????????????????????????MaxCopyLines ???
        /// </summary>
        private void CopyMatchedResultsToClipboard()
        {
            var rows = GetFilteredRows();
            if (rows.Count == 0)
            {
                EditorUtility.DisplayDialog("Copy Results", "No matched entries.", "OK");
                return;
            }
            int take = Mathf.Min(rows.Count, MaxCopyLines);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < take; i++)
            {
                var e = _entries[rows[i].entryIndex];
                if (e?.Condition != null)
                    sb.AppendLine(e.Condition);
            }
            string text = sb.ToString();
            if (text.Length > 0 && text.EndsWith("\r\n"))
                text = text.Substring(0, text.Length - 2);
            else if (text.Length > 0 && text.EndsWith("\n"))
                text = text.Substring(0, text.Length - 1);
            if (rows.Count > MaxCopyLines)
                text += "\n(Truncated, only first " + MaxCopyLines + " items copied)";
            EditorGUIUtility.systemCopyBuffer = text;
        }

        /// <summary>
        /// ????????????????????? Condition ???????????????????????????????????????MaxCopyLines ?????
        /// </summary>
        private void CopyRegexMatchPartsToClipboard()
        {
            if (string.IsNullOrEmpty(_searchApplied))
            {
                EditorUtility.DisplayDialog("Copy Matches", "Please enter a search term.", "OK");
                return;
            }
            var rows = GetFilteredRows();
            if (rows.Count == 0)
            {
                EditorUtility.DisplayDialog("Copy Matches", "No matched entries.", "OK");
                return;
            }
            var parts = new List<string>();
            bool truncated = false;
            var processedEntries = new HashSet<int>();
            if (_searchRegex)
            {
                try
                {
                    var regex = GetOrCreateSearchRegex() ?? new Regex(_searchApplied);
                    for (int i = 0; i < rows.Count; i++)
                    {
                        if (parts.Count >= MaxCopyLines) { truncated = true; break; }
                        int ei = rows[i].entryIndex;
                        if (!processedEntries.Add(ei)) continue;
                        var e = _entries[ei];
                        if (e?.Condition == null) continue;
                        foreach (Match m in regex.Matches(e.Condition))
                        {
                            if (m.Success && m.Length > 0)
                            {
                                parts.Add(m.Value);
                                if (parts.Count >= MaxCopyLines) { truncated = true; break; }
                            }
                        }
                        if (truncated) break;
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Copy Matches", "Invalid regex: " + ex.Message, "OK");
                    return;
                }
            }
            else
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    if (parts.Count >= MaxCopyLines) { truncated = true; break; }
                    int ei = rows[i].entryIndex;
                    if (!processedEntries.Add(ei)) continue;
                    var e = _entries[ei];
                    if (e?.Condition == null) continue;
                    int idx = e.Condition.IndexOf(_searchApplied, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                        parts.Add(e.Condition.Substring(idx, _searchApplied.Length));
                    if (truncated) break;
                }
            }
            if (parts.Count == 0)
            {
                EditorUtility.DisplayDialog("Copy Matches", "No match parts were found in matched entries.", "OK");
                return;
            }
            string text = string.Join("\n", parts);
            if (truncated)
                text += "\n(Truncated, only first " + MaxCopyLines + " items copied)";
            EditorGUIUtility.systemCopyBuffer = text;
        }

        /// <summary>
        /// ?????? maxLines ???????????????????? Condition ??Split('\n') ????
        /// ????Unity ??????????'\n' ????????
        /// </summary>

        private static readonly Regex RichTextTagRegex = new Regex(@"</?[a-zA-Z][^>]*>", RegexOptions.Compiled);

        private static string StripRichTextTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return RichTextTagRegex.Replace(text, "");
        }

        private void OpenStackLinkFromSelectionOrCaret()
        {
            if (_detailField == null) return;
            string raw = _detailField.value ?? "";
            string plain = StripRichTextTags(raw);
            if (string.IsNullOrEmpty(plain)) return;

            string selected = GetSelectedText(_detailField);
            if (!string.IsNullOrEmpty(selected))
            {
                string selPlain = StripRichTextTags(selected);
                var selLink = TryParseStackLine(selPlain);
                if (selLink.HasValue)
                {
                    OpenFileAtLine(selLink.Value.path, selLink.Value.lineNum);
                    return;
                }
            }

            int caret = GetCaretIndex(_detailField);
            if (caret < 0) return;
            if (caret > plain.Length) caret = plain.Length;

            int lineStart = caret > 0 ? plain.LastIndexOf("\n", caret - 1, caret) : -1;
            int lineEnd = caret < plain.Length ? plain.IndexOf("\n", caret, plain.Length - caret) : -1;
            if (lineStart < 0) lineStart = 0; else lineStart += 1;
            if (lineEnd < 0) lineEnd = plain.Length;
            if (lineEnd <= lineStart) return;

            string line = plain.Substring(lineStart, lineEnd - lineStart);
            var link = TryParseStackLine(line);
            if (!link.HasValue) return;

            int pathIndex = line.IndexOf(link.Value.path, StringComparison.Ordinal);
            if (pathIndex < 0) return;
            int start = pathIndex;
            int end = pathIndex + link.Value.path.Length;
            string lineNumToken = ":" + link.Value.lineNum;
            int lineNumIndex = line.IndexOf(lineNumToken, end, StringComparison.Ordinal);
            if (lineNumIndex >= 0) end = lineNumIndex + lineNumToken.Length;

            int caretInLine = caret - lineStart;
            if (caretInLine < start || caretInLine > end) return;

            OpenFileAtLine(link.Value.path, link.Value.lineNum);
        }

        private static int GetCaretIndex(TextField tf)
        {
            if (tf == null) return -1;
            try
            {
                var prop = tf.GetType().GetProperty("cursorIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null) return (int)prop.GetValue(tf);
            }
            catch { }
            try
            {
                var selProp = tf.GetType().GetProperty("textSelection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var sel = selProp != null ? selProp.GetValue(tf) : null;
                if (sel != null)
                {
                    var cp = sel.GetType().GetProperty("cursorIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (cp != null) return (int)cp.GetValue(sel);
                    var ap = sel.GetType().GetProperty("selectionAnchorPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (ap != null) return (int)ap.GetValue(sel);
                }
            }
            catch { }
            return -1;
        }

        private static void SetCaretCollapsed(TextField tf, int caret)
        {
            if (tf == null) return;
            try
            {
                var prop = tf.GetType().GetProperty("cursorIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null) prop.SetValue(tf, caret);
            }
            catch { }
            try
            {
                var selProp = tf.GetType().GetProperty("textSelection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var sel = selProp != null ? selProp.GetValue(tf) : null;
                if (sel != null)
                {
                    var cp = sel.GetType().GetProperty("cursorIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (cp != null) cp.SetValue(sel, caret);
                    var ap = sel.GetType().GetProperty("selectionAnchorPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (ap != null) ap.SetValue(sel, caret);
                }
            }
            catch { }
        }

        private static void DisableTextFieldAutoSelect(TextField field)
        {
            if (field == null) return;
            try
            {
                var t = field.GetType();
                var prop = t.GetProperty("selectAllOnFocus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.PropertyType == typeof(bool)) prop.SetValue(field, false);
                prop = t.GetProperty("selectAllOnMouseUp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.PropertyType == typeof(bool)) prop.SetValue(field, false);
            }
            catch { }
        }

        private static VisualElement GetDetailInputElement(TextField field)
        {
            if (field == null) return null;
            return field.Q<VisualElement>(className: "unity-text-input") ??
                   field.Q<VisualElement>(className: "unity-text-input__input") ??
                   field;
        }

        private void ClearDetailSelectionKeepCaret()
        {
            if (_detailField == null) return;
            int caret = GetCaretIndex(_detailField);
            if (caret < 0) caret = 0;
            SetCaretCollapsed(_detailField, caret);
        }

        private static string GetSelectedText(TextField tf)
        {
            if (tf == null) return null;
            try
            {
                var selProp = tf.GetType().GetProperty("textSelection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var sel = selProp != null ? selProp.GetValue(tf) : null;
                if (sel != null)
                {
                    var st = sel.GetType().GetProperty("selectedText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (st != null) return st.GetValue(sel) as string;
                }
            }
            catch { }
            return null;
        }
        private void CopySelectedMessagesToClipboard()
        {
            CopySelectedMessagesToClipboard(false);
        }

        private void CopySelectedMessagesToClipboard(bool includeTimestamp)
        {
            if (_logListView == null) return;
            var selected = _logListView.selectedIndices.OrderBy(i => i).ToList();
            if (selected.Count == 0) return;
            var rows = GetFilteredRows();
            var sb = new System.Text.StringBuilder();
            int take = Math.Min(selected.Count, MaxCopyLines);
            for (int i = 0; i < take; i++)
            {
                int idxRow = selected[i];
                if (idxRow < 0 || idxRow >= rows.Count) continue;
                var e = _entries[rows[idxRow].entryIndex];
                if (e?.Condition != null)
                {
                    var content = StripStackFromCondition(e.Condition);
                    if (includeTimestamp && !string.IsNullOrEmpty(e.TimeStamp))
                        content = "[" + e.TimeStamp + "] " + content;
                    sb.AppendLine(content);
                }
            }
            string text = sb.ToString();
            if (text.Length > 0 && text.EndsWith("\r\n"))
                text = text.Substring(0, text.Length - 2);
            else if (text.Length > 0 && text.EndsWith("\n"))
                text = text.Substring(0, text.Length - 1);
            EditorGUIUtility.systemCopyBuffer = text;
        }

        private static string StripStackFromCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return "";
            // Some logs include stack lines appended to condition. Remove stack-like lines.
            var lines = condition.Replace("\r\n", "\n").Split('\n');
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (IsStackLine(line)) break;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(line);
            }
            return sb.ToString();
        }

        private static bool IsStackLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return false;
            if (StackLineRegex.IsMatch(line)) return true;
            var t = line.TrimStart();
            return t.StartsWith("at ") || t.StartsWith("in ");
        }

        private bool IsClickInsideListView(VisualElement target)
        {
            if (_logListView == null || target == null) return false;
            var ve = target as VisualElement;
            return ve != null && (_logListView == ve || _logListView.Contains(ve));
        }

        private int GetListViewIndexFromTarget(VisualElement target)
        {
            if (_logListView == null || target == null) return -1;
            var ve = target;
            while (ve != null && ve != _logListView)
            {
                if (ve.name == "log-row" && ve.userData is RowChildren rc2) return rc2.RowIndex;
                ve = ve.parent as VisualElement;
            }
            return -1;
        }

        private bool IsClickInsideDetailPane(VisualElement target)
        {
            if (target == null) return false;
            var ve = target as VisualElement;
            if (ve == null) return false;
            if (_detailField != null && (_detailField == ve || _detailField.Contains(ve))) return true;
            var detailPane = rootVisualElement?.Q<VisualElement>("detailScroll");
            return detailPane != null && (detailPane == ve || detailPane.Contains(ve));
        }

        private static bool HasClassInHierarchy(VisualElement ve, string className)
        {
            for (var cur = ve; cur != null; cur = cur.parent)
            {
                if (cur.ClassListContains(className)) return true;
            }
            return false;
        }

        private bool IsClickInsideSplitViewDivider(VisualElement target)
        {
            if (target == null) return false;
            var ve = target as VisualElement;
            if (ve == null) return false;
            // Unity's TwoPaneSplitView divider/dragline internal classes
            return HasClassInHierarchy(ve, "unity-two-pane-split-view__divider") ||
                   HasClassInHierarchy(ve, "unity-two-pane-split-view__dragline");
        }

        private static string GetFirstLines(string text, int maxLines)
        {
            if (string.IsNullOrEmpty(text) || maxLines <= 0) return "";
            if (maxLines == 1)
            {
                int i = text.IndexOf('\n');
                return i >= 0 ? text.Substring(0, i) : text;
            }
            int pos = -1;
            for (int i = 0; i < maxLines; i++)
            {
                int next = text.IndexOf('\n', pos + 1);
                if (next < 0) return text;
                pos = next;
            }
            return text.Substring(0, pos + 1);
        }

        /// <summary>
        /// ???Collapse?????????????????????????????
        /// ?? Collapse??????????????????????
        /// </summary>
        private struct FilteredRow { public int entryIndex; public int displayCount; }

        // ListView 行的子节点引用缓存。makeItem 时填充，bindItem 直接读，避免反复 Q<>。
        private sealed class RowChildren
        {
            public int RowIndex;
            public Image Icon;
            public Label Message;
            public VisualElement Tags;
            public Label Count;
            public List<Label> TagLabels; // 复用的 tag 子标签池
            // 上一次绑定记录，用于跳过完全相同的 bind
            public LogEntry LastEntry;
            public int LastDisplayCount;
            public int LastDisplayCacheVersion;
            public bool LastCollapseShowing;
        }

        private struct CollapseGroupInfo
        {
            public int totalCount;
            public string firstTime;
            public string lastTime;
        }

        private struct TagInfo
        {
            public int count;
            public string lastTime;
        }

        internal enum TagSortMode
        {
            Name = 0,
            Count = 1,
            Recent = 2
        }

        private struct CollapseGroupState
        {
            public int firstIndex;
            public int totalCount;
            public string firstTime;
            public string lastTime;
        }

        private readonly struct LogKey : IEquatable<LogKey>
        {
            private readonly string _condition;
            private readonly string _stackTrace;
            private readonly LogType _logType;
            private readonly int _hash;

            public LogKey(LogEntry entry)
            {
                _condition = entry?.Condition ?? "";
                _stackTrace = entry?.StackTrace ?? "";
                _logType = entry != null ? entry.LogType : LogType.Log;
                if (entry != null)
                {
                    int h = entry.CachedKeyHash;
                    if (h == 0)
                    {
                        unchecked
                        {
                            h = (int)_logType;
                            h = (h * 397) ^ (_condition != null ? _condition.GetHashCode() : 0);
                            h = (h * 397) ^ (_stackTrace != null ? _stackTrace.GetHashCode() : 0);
                            if (h == 0) h = 1; // 0 用作"未计算"标记
                        }
                        entry.CachedKeyHash = h;
                    }
                    _hash = h;
                }
                else
                {
                    _hash = 0;
                }
            }

            public bool Equals(LogKey other)
            {
                if (_hash != other._hash) return false;
                if (_logType != other._logType) return false;
                // 引用相等快路径（ShareShortString 已 intern 短串）
                if (ReferenceEquals(_condition, other._condition) && ReferenceEquals(_stackTrace, other._stackTrace))
                    return true;
                return string.Equals(_condition, other._condition, StringComparison.Ordinal) &&
                       string.Equals(_stackTrace, other._stackTrace, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is LogKey other && Equals(other);
            }

            public override int GetHashCode() => _hash;
        }

        [Serializable]
        private class StringListWrapper
        {
            public List<string> items = new List<string>();
        }

        private struct FilterJobInput
        {
            // 使用数组快照，避免后台过滤与主线程修改 _entries 产生竞态
            public LogEntry[] Entries;
            public int EntriesCount;
            public Regex SearchRegexObj;
            public string SearchApplied;
            public bool Collapse;
            public bool CollapseGlobal;
            public bool ShowLog;
            public bool ShowWarning;
            public bool ShowError;
            public bool TagsEnabled;
            public HashSet<string> ExcludedTags;
            public HashSet<string> SelectedTags;
            public bool FilterTimeRange;
            public bool FilterNumberRange;
            public bool FilterFrameRange;
            public string FilterTimeMin;
            public string FilterTimeMax;
            public int FilterNumberMin;
            public int FilterNumberMax;
            public int FilterFrameMin;
            public int FilterFrameMax;
        }

        private struct FilterJobOutput
        {
            public List<FilteredRow> Rows;
            public Dictionary<int, CollapseGroupInfo> CollapseInfo;
        }

        private static bool MatchesFilters(LogEntry e, FilterJobInput input)
        {
            if (!string.IsNullOrEmpty(input.SearchApplied))
            {
                if (e?.Condition == null) return false;
                bool searchMatched;
                if (input.SearchRegexObj != null)
                {
                    try { searchMatched = input.SearchRegexObj.IsMatch(e.Condition); }
                    catch { searchMatched = false; }
                }
                else
                {
                    searchMatched = e.Condition.IndexOf(input.SearchApplied, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                if (!searchMatched) return false;
            }

            if (input.FilterTimeRange)
            {
                if (e == null || string.IsNullOrEmpty(e.TimeStamp))
                {
                    if (!string.IsNullOrEmpty(input.FilterTimeMin) || !string.IsNullOrEmpty(input.FilterTimeMax)) return false;
                }
                else
                {
                    if (!string.IsNullOrEmpty(input.FilterTimeMin) && string.Compare(e.TimeStamp, input.FilterTimeMin, StringComparison.Ordinal) < 0) return false;
                    if (!string.IsNullOrEmpty(input.FilterTimeMax) && string.Compare(e.TimeStamp, input.FilterTimeMax, StringComparison.Ordinal) > 0) return false;
                }
            }

            if (input.FilterNumberRange)
            {
                if (e == null) return false;
                if (e.MessageNumber < input.FilterNumberMin || e.MessageNumber > input.FilterNumberMax) return false;
            }

            if (input.FilterFrameRange)
            {
                if (e == null) return false;
                if (e.FrameCount < input.FilterFrameMin || e.FrameCount > input.FilterFrameMax) return false;
            }

            switch (e.LogType)
            {
                case LogType.Log:
                case LogType.Assert: if (!input.ShowLog) return false; break;
                case LogType.Warning: if (!input.ShowWarning) return false; break;
                case LogType.Error:
                case LogType.Exception: if (!input.ShowError) return false; break;
            }

            if (input.TagsEnabled && input.ExcludedTags != null && input.ExcludedTags.Count > 0 && e.HasAnyTag(input.ExcludedTags)) return false;
            if (input.TagsEnabled && input.SelectedTags != null && input.SelectedTags.Count > 0 && !e.HasAnyTag(input.SelectedTags)) return false;

            return true;
        }

        private static FilterJobOutput BuildFilteredRowsSync(FilterJobInput input)
        {
            var list = new List<FilteredRow>();
            Dictionary<int, CollapseGroupInfo> infoMap = null;

            if (input.Collapse && input.CollapseGlobal)
            {
                var groups = new Dictionary<LogKey, CollapseGroupState>();
                var order = new List<LogKey>();
                int n = input.EntriesCount;
                for (int i = 0; i < n; i++)
                {
                    var e = input.Entries[i];
                    if (!MatchesFilters(e, input)) continue;
                    var key = new LogKey(e);
                    if (groups.TryGetValue(key, out var state))
                    {
                        state.totalCount += e.Count;
                        UpdateGroupTimeRange(ref state, e);
                        groups[key] = state;
                    }
                    else
                    {
                        state = new CollapseGroupState
                        {
                            firstIndex = i,
                            totalCount = e.Count,
                            firstTime = GetEntryFirstTime(e),
                            lastTime = GetEntryLastTime(e)
                        };
                        groups.Add(key, state);
                        order.Add(key);
                    }
                }
                infoMap = new Dictionary<int, CollapseGroupInfo>(order.Count);
                foreach (var key in order)
                {
                    var g = groups[key];
                    list.Add(new FilteredRow { entryIndex = g.firstIndex, displayCount = g.totalCount });
                    infoMap[g.firstIndex] = new CollapseGroupInfo { totalCount = g.totalCount, firstTime = g.firstTime, lastTime = g.lastTime };
                }
            }
            else if (input.Collapse)
            {
                int n = input.EntriesCount;
                for (int i = 0; i < n; i++)
                {
                    var e = input.Entries[i];
                    if (!MatchesFilters(e, input)) continue;
                    list.Add(new FilteredRow { entryIndex = i, displayCount = e.Count });
                }
            }
            else
            {
                int n = input.EntriesCount;
                for (int i = 0; i < n; i++)
                {
                    var e = input.Entries[i];
                    if (!MatchesFilters(e, input)) continue;
                    if (e.Count == 1)
                    {
                        list.Add(new FilteredRow { entryIndex = i, displayCount = 1 });
                    }
                    else
                    {
                        for (int k = 0; k < e.Count; k++)
                            list.Add(new FilteredRow { entryIndex = i, displayCount = 1 });
                    }
                }
            }

            return new FilterJobOutput { Rows = list, CollapseInfo = infoMap };
        }

        /// <summary>
        /// 在已有 cached rows 上做"仅追加"增量过滤：从 startIndex 扫描到 _entries 末尾。
        /// collapse 全局模式下命中已有分组时更新该 row 的 displayCount / lastTime。
        /// 仅当 _filterAppendOnly==true 且 cache 已存在时调用。
        /// </summary>
        private void AppendIncrementalFilter(int startIndex)
        {
            if (_cachedFilteredRows == null) return;
            int n = _entries.Count;
            if (startIndex >= n) return;

            var input = BuildFilterJobInput(forBackgroundJob: false);

            if (input.Collapse && input.CollapseGlobal)
            {
                // 重建 key→rowIndex 索引（首次或被清失效时）
                if (_collapseRowIndexByKey == null)
                {
                    _collapseRowIndexByKey = new Dictionary<LogKey, int>(_cachedFilteredRows.Count);
                    for (int r = 0; r < _cachedFilteredRows.Count; r++)
                    {
                        var er = input.Entries[_cachedFilteredRows[r].entryIndex];
                        _collapseRowIndexByKey[new LogKey(er)] = r;
                    }
                }
                if (_cachedCollapseGroupInfo == null)
                    _cachedCollapseGroupInfo = new Dictionary<int, CollapseGroupInfo>(_cachedFilteredRows.Count);

                for (int i = startIndex; i < n; i++)
                {
                    var e = input.Entries[i];
                    if (!MatchesFilters(e, input)) continue;
                    var key = new LogKey(e);
                    if (_collapseRowIndexByKey.TryGetValue(key, out int rowIdx))
                    {
                        var row = _cachedFilteredRows[rowIdx];
                        row.displayCount += e.Count;
                        _cachedFilteredRows[rowIdx] = row;
                        if (_cachedCollapseGroupInfo.TryGetValue(row.entryIndex, out var info))
                        {
                            info.totalCount = row.displayCount;
                            string lt = GetEntryLastTime(e);
                            if (!string.IsNullOrEmpty(lt) && (string.IsNullOrEmpty(info.lastTime) || string.Compare(lt, info.lastTime, StringComparison.Ordinal) > 0))
                                info.lastTime = lt;
                            _cachedCollapseGroupInfo[row.entryIndex] = info;
                        }
                    }
                    else
                    {
                        var row = new FilteredRow { entryIndex = i, displayCount = e.Count };
                        int newRowIdx = _cachedFilteredRows.Count;
                        _cachedFilteredRows.Add(row);
                        _collapseRowIndexByKey[key] = newRowIdx;
                        _cachedCollapseGroupInfo[i] = new CollapseGroupInfo
                        {
                            totalCount = e.Count,
                            firstTime = GetEntryFirstTime(e),
                            lastTime = GetEntryLastTime(e)
                        };
                    }
                }
            }
            else if (input.Collapse)
            {
                for (int i = startIndex; i < n; i++)
                {
                    var e = input.Entries[i];
                    if (!MatchesFilters(e, input)) continue;
                    _cachedFilteredRows.Add(new FilteredRow { entryIndex = i, displayCount = e.Count });
                }
            }
            else
            {
                for (int i = startIndex; i < n; i++)
                {
                    var e = input.Entries[i];
                    if (!MatchesFilters(e, input)) continue;
                    if (e.Count == 1)
                    {
                        _cachedFilteredRows.Add(new FilteredRow { entryIndex = i, displayCount = 1 });
                    }
                    else
                    {
                        for (int k = 0; k < e.Count; k++)
                            _cachedFilteredRows.Add(new FilteredRow { entryIndex = i, displayCount = 1 });
                    }
                }
            }
            _lastFilteredEntriesEnd = n;
        }

        /// <summary>
        /// 标记筛选条件已改变，必须做全量重建。
        /// </summary>
        private void InvalidateFilterFull()
        {
            _filterDirty = true;
            _filterAppendOnly = false;
        }

        /// <summary>
        /// 拷贝 _entries 到指定数组（必要时扩容），避免后台过滤与主线程修改冲突。
        /// reuseBuffer=true 时复用内部 buffer（仅同步主线程使用，且后续不再被读取）。
        /// reuseBuffer=false 时分配新数组（用于后台 Task，buffer 生命周期独立）。
        /// </summary>
        private LogEntry[] _entriesSnapshotBuffer;
        private int SnapshotEntries(bool reuseBuffer, out LogEntry[] arr)
        {
            int n = _entries.Count;
            if (!reuseBuffer)
            {
                arr = new LogEntry[n];
                if (n > 0) _entries.CopyTo(0, arr, 0, n);
                return n;
            }
            if (_entriesSnapshotBuffer == null || _entriesSnapshotBuffer.Length < n)
            {
                int cap = Math.Max(64, _entriesSnapshotBuffer == null ? n : _entriesSnapshotBuffer.Length * 2);
                while (cap < n) cap *= 2;
                _entriesSnapshotBuffer = new LogEntry[cap];
            }
            if (n > 0) _entries.CopyTo(0, _entriesSnapshotBuffer, 0, n);
            arr = _entriesSnapshotBuffer;
            return n;
        }

        private FilterJobInput BuildFilterJobInput(bool forBackgroundJob)
        {
            int count = SnapshotEntries(!forBackgroundJob, out var arr);
            return new FilterJobInput
            {
                Entries = arr,
                EntriesCount = count,
                SearchRegexObj = GetOrCreateSearchRegex(),
                SearchApplied = _searchApplied,
                Collapse = _collapse,
                CollapseGlobal = _collapseGlobal,
                ShowLog = _showLog,
                ShowWarning = _showWarning,
                ShowError = _showError,
                TagsEnabled = _tagsEnabled,
                ExcludedTags = _excludedTags,
                SelectedTags = _selectedTags,
                FilterTimeRange = _filterTimeRange,
                FilterNumberRange = _filterNumberRange,
                FilterFrameRange = _filterFrameRange,
                FilterTimeMin = _filterTimeMin,
                FilterTimeMax = _filterTimeMax,
                FilterNumberMin = _filterNumberMin,
                FilterNumberMax = _filterNumberMax,
                FilterFrameMin = _filterFrameMin,
                FilterFrameMax = _filterFrameMax,
            };
        }

        /// <summary>
        /// collapse 命中已有分组时，尝试在 cached rows 上做 O(1) 行更新，避免触发全量重建。
        /// 仅 collapse + collapseGlobal + cache 已存在时有效；命中返回 true。
        /// </summary>
        private bool TryFastCollapseHitUpdate(LogEntry last, int addedCount, string newLastTime)
        {
            if (_cachedFilteredRows == null) return false;
            if (!_collapse || !_collapseGlobal) return false;

            // 懒构建 key→rowIndex 索引
            if (_collapseRowIndexByKey == null)
            {
                _collapseRowIndexByKey = new Dictionary<LogKey, int>(_cachedFilteredRows.Count);
                for (int r = 0; r < _cachedFilteredRows.Count; r++)
                {
                    int ei = _cachedFilteredRows[r].entryIndex;
                    if (ei < 0 || ei >= _entries.Count) continue;
                    _collapseRowIndexByKey[new LogKey(_entries[ei])] = r;
                }
            }

            var key = new LogKey(last);
            if (!_collapseRowIndexByKey.TryGetValue(key, out int rowIdx)) return false;

            var row = _cachedFilteredRows[rowIdx];
            row.displayCount += addedCount;
            _cachedFilteredRows[rowIdx] = row;
            if (_cachedCollapseGroupInfo != null && _cachedCollapseGroupInfo.TryGetValue(row.entryIndex, out var info))
            {
                info.totalCount = row.displayCount;
                if (!string.IsNullOrEmpty(newLastTime) && (string.IsNullOrEmpty(info.lastTime) || string.Compare(newLastTime, info.lastTime, StringComparison.Ordinal) > 0))
                    info.lastTime = newLastTime;
                _cachedCollapseGroupInfo[row.entryIndex] = info;
            }
            return true;
        }

        private void TryScheduleBackgroundFilter()
        {
            if (!_filterDirty || _cachedFilteredRows == null || _filterRebuildPending) return;

            _filterCts?.Cancel();
            _filterCts = new System.Threading.CancellationTokenSource();
            var cts = _filterCts;
            var input = BuildFilterJobInput(forBackgroundJob: true);
            int snapshotCount = input.EntriesCount;
            int scheduledVersion = _filterCriteriaVersion;
            _filterRebuildPending = true;

            Task.Run(() =>
            {
                var output = BuildFilteredRowsSync(input);
                if (cts.IsCancellationRequested) return;

                EditorApplication.delayCall += () =>
                {
                    if (cts.IsCancellationRequested) return;
                    _filterRebuildPending = false;
                    // 调度期间用户改了过滤条件（如 selectedTags / search / show-type 等），结果已过期 → 丢弃，重新调度
                    if (_filterCriteriaVersion != scheduledVersion)
                    {
                        // 保持 _filterDirty / _filterAppendOnly 当前值（已被改条件的代码设为 dirty + non-append）
                        TryScheduleBackgroundFilter();
                        return;
                    }
                    _cachedFilteredRows = output.Rows;
                    _cachedCollapseGroupInfo = output.CollapseInfo;
                    _collapseRowIndexByKey = null;
                    _lastFilteredEntriesEnd = snapshotCount;
                    // 后台过滤期间 _entries 可能继续追加：保留 append-only dirty
                    if (_entries.Count > snapshotCount)
                    {
                        _filterDirty = true;
                        _filterAppendOnly = true;
                    }
                    else
                    {
                        _filterDirty = false;
                        _filterAppendOnly = false;
                    }
                    RefreshUI();
                };
            });
        }

        private List<FilteredRow> GetFilteredRows()
        {
            if (!_filterDirty && _cachedFilteredRows != null)
                return _cachedFilteredRows;

            // 增量路径：仅当筛选条件未变、只有末尾追加时
            if (_filterDirty && _filterAppendOnly && _cachedFilteredRows != null && _lastFilteredEntriesEnd <= _entries.Count)
            {
                AppendIncrementalFilter(_lastFilteredEntriesEnd);
                _filterDirty = false;
                _filterAppendOnly = false;
                _lastFilterRebuildTime = EditorApplication.timeSinceStartup;
                return _cachedFilteredRows;
            }

            if (_filterRebuildPending && _cachedFilteredRows != null)
                return _cachedFilteredRows;

            double now = EditorApplication.timeSinceStartup;
            if (_filterDirty && _cachedFilteredRows != null && (now - _lastFilterRebuildTime) * 1000 < MinFilterRebuildIntervalMs)
                return _cachedFilteredRows;
            _lastFilterRebuildTime = now;

            var input = BuildFilterJobInput(forBackgroundJob: false);
            var output = BuildFilteredRowsSync(input);
            _cachedFilteredRows = output.Rows;
            _cachedCollapseGroupInfo = output.CollapseInfo;
            _collapseRowIndexByKey = null; // 让增量路径在下次需要时重建
            _filterDirty = false;
            _filterAppendOnly = false;
            _filterRebuildPending = false;
            _lastFilteredEntriesEnd = input.EntriesCount;
            return output.Rows;
        }

        /// <summary>
        /// ????????????????????????????????????????????????????0 ???????
        /// </summary>
        /// <summary>
        /// 返回所有出现过的 tag（不含 tag 自身过滤），仅维护"tag 集合"，不再统计计数/最近时间。
        /// 性能优先：只在 cache 为空时做一次 O(N) 扫描；新增 entry 由 UpdateTagCacheForEntry 增量加入；
        /// 删除（Trim/Clear）走 CleanupStaleTags 偶尔做一次重建。
        /// </summary>
        private Dictionary<string, TagInfo> GetAllTagsFromRowsWithoutTagFilter()
        {
            if (_cachedTagInfo != null && !_tagCountsDirty)
                return _cachedTagInfo;

            // dirty 但已存在：保留旧字典，避免每次 toggle 都重建（仅在首次 / Clear 后真正重建）
            if (_cachedTagInfo != null)
            {
                _tagCountsDirty = false;
                return _cachedTagInfo;
            }

            var allTags = new Dictionary<string, TagInfo>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                var tags = e.TagsOrEmpty;
                for (int j = 0; j < tags.Length; j++)
                {
                    var t = tags[j];
                    if (string.IsNullOrEmpty(t)) continue;
                    if (!allTags.ContainsKey(t))
                        allTags[t] = default(TagInfo);
                }
            }
            _cachedTagInfo = allTags;
            _tagCountsDirty = false;
            return allTags;
        }

        /// <summary>
        /// ???????? (at path:line) ??in path:line????(before, path, lineNum, after)??????????
        /// </summary>
        private (string before, string path, int lineNum, string after)? TryParseStackLine(string line)
        {
            var m = StackLineRegex.Match(line);
            if (!m.Success) return null;
            string path = null;
            int lineNum = 0;
            if (m.Groups[1].Success)
            {
                path = m.Groups[1].Value.Trim();
                int.TryParse(m.Groups[2].Value, out lineNum);
            }
            else if (m.Groups[3].Success)
            {
                path = m.Groups[3].Value.Trim();
                int.TryParse(m.Groups[4].Value, out lineNum);
            }
            if (string.IsNullOrEmpty(path) || lineNum <= 0) return null;
            string before = line.Substring(0, m.Index);
            string after = line.Substring(m.Index + m.Length);
            return (before, path, lineNum, after);
        }

        /// <summary>
        /// ????????????(path, lineNum)???????????
        /// </summary>
        private List<(string path, int lineNum)> ParseStackTraceLinks(string stackTrace)
        {
            var list = new List<(string path, int lineNum)>();
            if (string.IsNullOrEmpty(stackTrace)) return list;
            var lines = stackTrace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var link = TryParseStackLine(line);
                if (link != null)
                    list.Add((link.Value.path, link.Value.lineNum));
            }
            return list;
        }

        private static void OpenFileAtLine(string path, int line)
        {
            path = path.Replace('\\', '/');
            string projectPath = Application.dataPath;
            string projectRoot = Path.GetDirectoryName(projectPath).Replace('\\', '/');
            string relative = path;
            if (path.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                relative = path.Substring(projectRoot.Length).TrimStart('/');
            else if (path.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase) is int i && i >= 0)
                relative = path.Substring(i);

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relative);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset, line);
                return;
            }
            if (File.Exists(path))
            {
                var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditorInternal.InternalEditorUtility");
                if (type != null)
                {
                    var method = type.GetMethod("OpenFileAtLineExternal", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(int) }, null);
                    method?.Invoke(null, new object[] { path, line });
                }
            }
        }



        public void RecomputeAllTags()
        {
            foreach (var e in _entries)
                EnhancedConsoleTagLogic.ComputeTags(e);
            _filterAppendOnly = false;
            _filterDirty = true;
            _filterCriteriaVersion++;
            _tagCountsDirty = true;
            _cachedTagInfo = null;
            InvalidateDisplayCache();
            RefreshUI();
        }

        internal void JumpToMessageNumber(int targetNumber)
        {
            if (targetNumber <= 0)
            {
                ShowNotification(new GUIContent("请输入有效的日志编号"), 2f);
                return;
            }

            int entryIndex = FindEntryIndexByMessageNumber(targetNumber);
            if (entryIndex < 0)
            {
                ShowNotification(new GUIContent($"日志编号 {targetNumber} 不存在"), 2f);
                return;
            }

            _selectedIndex = entryIndex;

            var filtered = GetFilteredRows();
            int filteredIndex = -1;
            for (int i = 0; i < filtered.Count; i++)
            {
                if (filtered[i].entryIndex == entryIndex)
                {
                    filteredIndex = i;
                    break;
                }
            }

            if (filteredIndex < 0)
            {
                ShowNotification(new GUIContent($"日志编号 {targetNumber} 被当前过滤条件隐藏"), 2f);
                return;
            }

            _logListView.SetSelectionWithoutNotify(new[] { filteredIndex });
            _logListView.ScrollToItem(filteredIndex);
            UpdateDetailPanel();
        }

        private int FindEntryIndexByMessageNumber(int number)
        {
            int lo = 0, hi = _entries.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int midNum = _entries[mid].MessageNumber;
                if (midNum == number) return mid;
                if (midNum < number) lo = mid + 1;
                else hi = mid - 1;
            }
            return -1;
        }

        private void SetStackTrace(LogType type, StackTraceLogType stackType)
        {
            if (type == LogType.Log) _stackTraceLog = stackType;
            else if (type == LogType.Warning) _stackTraceWarning = stackType;
            else _stackTraceError = stackType;
            ApplyStackTraceSettings();
            SavePrefs();
        }

        private static void OpenEditorLog()
        {
            string path = Application.consoleLogPath;
            if (string.IsNullOrEmpty(path))
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log");
            if (File.Exists(path))
                EditorUtility.RevealInFinder(path);
            else
                Debug.LogWarning("Editor log file not found: " + path);
        }

        private static void OpenPlayerLog()
        {
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
            root = Path.GetFullPath(root);
            string company = PlayerSettings.companyName;
            string product = PlayerSettings.productName;
            string path = Path.Combine(root, company, product, "Player.log");
            path = Path.GetFullPath(path);
            if (File.Exists(path))
                EditorUtility.RevealInFinder(path);
            else
                Debug.LogWarning("Player log file not found: " + path);
        }

        public void Clear()
        {
            if (HasCompilationErrors)
            {
                // ????????????????????
                for (int i = _entries.Count - 1; i >= 0; i--)
                {
                    if (!IsCompilationErrorLog(_entries[i]))
                        _entries.RemoveAt(i);
                }
                if (_entries.Count > 0)
                {
                    for (int i = 0; i < _entries.Count; i++)
                        _entries[i].MessageNumber = i + 1;
                    _nextMessageNumber = _entries.Count + 1;
                    EnhancedConsoleLogFile.RewriteFileWithEntries(_entries);
                }
                else
                {
                    _nextMessageNumber = 0;
                    EnhancedConsoleLogFile.ClearFile();
                }
            }
            else
            {
                _entries.Clear();
                _nextMessageNumber = 0;
                EnhancedConsoleLogFile.ClearFile();
            }
            _selectedTags.Clear();
            _excludedTags.Clear();
            _tagButtonPool.Clear();
            _lastTagBarOrder.Clear();
            _cachedFilteredRows = null;
            _cachedTagInfo = null;
            _cachedCollapseGroupInfo = null;
            _collapseRowIndexByKey = null;
            _filterAppendOnly = false;
            _filterDirty = true;
            _listViewNeedsRebuild = true;
            _tagCountsDirty = true;
            _selectedIndex = -1;
            RecalculateUnfilteredCounts();
            SavePrefs();
            RefreshUI();
        }

        private int GetAndAdvanceNextMessageNumber()
        {
            _nextMessageNumber++;
            if (_nextMessageNumber > int.MaxValue)
                _nextMessageNumber = 1;
            return _nextMessageNumber;
        }

        /// <summary>
        /// ?????? MaxEntries???????????????N ??????_selectedIndex??
        /// </summary>
        private void TrimEntriesToMax()
        {
            int threshold = (int)(_maxEntries * 1.2);
            if (_entries.Count <= threshold) return;
            int removeCount = _entries.Count - _maxEntries;
            for (int i = 0; i < removeCount; i++)
            {
                var e = _entries[i];
                if (e == null) continue;
                int c = e.Count;
                switch (e.LogType)
                {
                    case LogType.Log:
                    case LogType.Assert: _unfilteredLogCount -= c; break;
                    case LogType.Warning: _unfilteredWarnCount -= c; break;
                    case LogType.Error:
                    case LogType.Exception: _unfilteredErrCount -= c; break;
                }
            }
            _entries.RemoveRange(0, removeCount);
            if (_selectedIndex >= 0)
            {
                _selectedIndex -= removeCount;
                if (_selectedIndex < 0) _selectedIndex = -1;
            }
            _cachedTagInfo = null;
            _tagCountsDirty = true;
            // entryIndex 已整体偏移，必须全量重建
            _filterAppendOnly = false;
            _cachedFilteredRows = null;
            _cachedCollapseGroupInfo = null;
            _collapseRowIndexByKey = null;
            _filterDirty = true;
            CleanupStaleTags();
        }

        private void CleanupStaleTags()
        {
            if (_selectedTags.Count == 0 && _excludedTags.Count == 0) return;
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in _entries)
            {
                if (e?.Tags == null) continue;
                foreach (var t in e.Tags)
                    if (!string.IsNullOrEmpty(t)) existing.Add(t);
            }
            _selectedTags.RemoveWhere(t => !existing.Contains(t));
            _excludedTags.RemoveWhere(t => !existing.Contains(t));
        }

        /// <summary>
        /// ??????AddEntry ??????Repaint ?? N ms ???? Repaint????????????
        /// </summary>
        /// <summary>
        /// 节流：高频路径（AddEntry/Flush/远程接收）调用此方法只置 dirty 标志，由 EditorApplication.update tick 合并执行 RefreshUI。
        /// 替代以前的 EditorApplication.delayCall 链，避免同帧多次注册回调。
        /// </summary>
        private void RepaintThrottled()
        {
            if (_viewLocked) return;
            _uiRefreshRequested = true;
        }

        private bool _uiRefreshRequested;

        private void TickRefreshUI()
        {
            if (!_uiRefreshRequested) return;
            if (_viewLocked) { _uiRefreshRequested = false; return; }
            double now = EditorApplication.timeSinceStartup;
            if ((now - _lastRepaintTime) * 1000 < MinRepaintIntervalMs) return;
            _uiRefreshRequested = false;
            _lastRepaintTime = now;
            try { RefreshUI(); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
        }

        #region UI Toolkit
        

        private void BuildUI()
        {
            var root = rootVisualElement;
            root.Clear();
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (tree == null) return;
            tree.CloneTree(root);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            var innerRoot = root.Q<VisualElement>("root");
            if (innerRoot == null) return;

            _mainSplit = innerRoot.Q<TwoPaneSplitView>("mainSplit");
            if (_mainSplit != null)
            {
                var detailPane = _mainSplit.Q<VisualElement>("detailScroll");
                if (detailPane != null)
                {
                    _mainSplit.fixedPaneInitialDimension = _detailHeight;
                    detailPane.RegisterCallback<GeometryChangedEvent>(OnDetailPaneGeometryChanged);
                }
            }

            _logListView = innerRoot.Q<ListView>("logListView");
            _detailField = innerRoot.Q<TextField>("detailLabel");
            if (_detailField != null)             {                 _detailField.isReadOnly = true;                 _detailField.multiline = true;                 _detailField.focusable = true;                 var detailInput = _detailField.Q<VisualElement>(className: "unity-text-input");                 if (detailInput != null)                 {                     detailInput.RegisterCallback<MouseUpEvent>(evt =>                     {                         if (evt.button != 0 || evt.clickCount != 2) return;                         _detailField.schedule.Execute(() => OpenStackLinkFromSelectionOrCaret()).StartingIn(2);                     }, TrickleDown.TrickleDown);                 }                 else                 {                     _detailField.RegisterCallback<MouseUpEvent>(evt =>                     {                         if (evt.button != 0 || evt.clickCount != 2) return;                         _detailField.schedule.Execute(() => OpenStackLinkFromSelectionOrCaret()).StartingIn(2);                     }, TrickleDown.TrickleDown);                 }             }
            if (_detailField != null)
            {
                DisableTextFieldAutoSelect(_detailField);
                _detailField.RegisterCallback<FocusInEvent>(_ =>
                {
                    _detailField.schedule.Execute(() => ClearDetailSelectionKeepCaret()).StartingIn(2);
                    EditorApplication.delayCall += () => ClearDetailSelectionKeepCaret();
                }, TrickleDown.TrickleDown);
            }
            if (_detailField != null)
            {
                var input = GetDetailInputElement(_detailField);
                if (input != null)
                {
                    input.RegisterCallback<FocusInEvent>(_ =>
                    {
                        _detailField.schedule.Execute(() => SetCaretCollapsed(_detailField, 0)).StartingIn(2);
                        EditorApplication.delayCall += () => SetCaretCollapsed(_detailField, 0);
                    }, TrickleDown.TrickleDown);
                    input.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.button != 0) return;
                        _detailMouseDownTracking = true;
                        _detailMouseDownPos = evt.mousePosition;
                    }, TrickleDown.TrickleDown);
                    input.RegisterCallback<MouseUpEvent>(evt =>
                    {
                        if (evt.button != 0) return;
                        if (!_detailMouseDownTracking) return;
                        _detailMouseDownTracking = false;
                        if (evt.clickCount != 1) return;
                        float dist = (evt.mousePosition - _detailMouseDownPos).magnitude;
                        if (dist > 3f) return;
                        _detailField.schedule.Execute(() => ClearDetailSelectionKeepCaret()).StartingIn(1);
                    }, TrickleDown.TrickleDown);
                }
            }
                BindDetailDoubleClick();
            // detail double-click binding complete

            BindToolbar(innerRoot);
            SetupToolbarCountToggleIcons(innerRoot);
            BindSearchBar(innerRoot);
            BindTagBar(innerRoot);
            BindListView();
            SyncTogglesFromState(innerRoot);
            SyncSearchField();
            rootVisualElement.RegisterCallback<MouseDownEvent>(evt =>
            {
                var target = evt.target as VisualElement;
                if (!IsClickInsideListView(target) && !IsClickInsideDetailPane(target) && !IsClickInsideSplitViewDivider(target))
                {
                    _logListView?.ClearSelection();
                }
            });

            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.C)
                {
                    if (_logListView != null && _logListView.selectedIndices.Any())
                    {
                        CopySelectedMessagesToClipboard();
                        evt.StopPropagation();
                    }
                }
                if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.G)
                {
                    JumpToLineWindow.Open(this);
                    evt.StopPropagation();
                }
            });

            RefreshUI();
        }

        /// <summary>
        /// ??????????Toggle?Log/Warning/Error?????????
        /// </summary>
        private void SetupToolbarCountToggleIcons(VisualElement root)
        {
            var toggleLog = root.Q<Toggle>("toggleLog");
            var toggleWarning = root.Q<Toggle>("toggleWarning");
            var toggleError = root.Q<Toggle>("toggleError");
            AddIconToToggle(toggleLog, _iconLog);
            AddIconToToggle(toggleWarning, _iconWarning);
            AddIconToToggle(toggleError, _iconError);
        }

        private static void AddIconToToggle(Toggle toggle, Texture2D icon)
        {
            if (toggle == null || icon == null) return;
            if (toggle.Q("toggle-type-icon") != null) return;
            var label = toggle.Q<Label>();
            if (label == null) return;
            var img = new Image { image = icon, name = "toggle-type-icon" };
            img.AddToClassList("toolbar-toggle-icon");
            img.style.width = 16;
            img.style.height = 16;
            img.style.minWidth = 16;
            img.style.minHeight = 16;
            img.style.marginRight = 2;
            img.style.flexShrink = 0;
            int labelIndex = toggle.IndexOf(label);
            toggle.Insert(labelIndex, img);
        }

        private void BindToolbar(VisualElement root)
        {
            var btnClear = root.Q<Button>("btnClear");
            if (btnClear != null) btnClear.clicked += Clear;

            var toggleCollapse = root.Q<Toggle>("toggleCollapse");
            if (toggleCollapse != null)
            {
                toggleCollapse.value = _collapse;
                toggleCollapse.RegisterValueChangedCallback(ev =>
                {
                    _collapse = ev.newValue;
                    _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                    SavePrefs();
                    RefreshUI();
                });
            }

            var toggleClearOnPlay = root.Q<Toggle>("toggleClearOnPlay");
            if (toggleClearOnPlay != null) { toggleClearOnPlay.value = _clearOnPlay; toggleClearOnPlay.RegisterValueChangedCallback(ev => { _clearOnPlay = ev.newValue; SavePrefs(); }); }
            var toggleClearOnBuild = root.Q<Toggle>("toggleClearOnBuild");
            if (toggleClearOnBuild != null) { toggleClearOnBuild.value = _clearOnBuild; toggleClearOnBuild.RegisterValueChangedCallback(ev => { _clearOnBuild = ev.newValue; SavePrefs(); }); }
            var toggleErrorPause = root.Q<Toggle>("toggleErrorPause");
            if (toggleErrorPause != null) { toggleErrorPause.value = _errorPause; toggleErrorPause.RegisterValueChangedCallback(ev => { _errorPause = ev.newValue; SavePrefs(); }); }

            var toggleViewLock = root.Q<Toggle>("toggleViewLock");
            if (toggleViewLock != null)
            {
                toggleViewLock.value = _viewLocked;
                toggleViewLock.RegisterValueChangedCallback(ev =>
                {
                    _viewLocked = ev.newValue;
                    SavePrefs();
                    if (!_viewLocked)
                        RefreshUI();
                });
            }

            var toggleLog = root.Q<Toggle>("toggleLog");
            if (toggleLog != null) { toggleLog.value = _showLog; toggleLog.RegisterValueChangedCallback(ev => { _showLog = ev.newValue; _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true; SavePrefs(); RefreshUI(); }); }
            var toggleWarning = root.Q<Toggle>("toggleWarning");
            if (toggleWarning != null) { toggleWarning.value = _showWarning; toggleWarning.RegisterValueChangedCallback(ev => { _showWarning = ev.newValue; _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true; SavePrefs(); RefreshUI(); }); }
            var toggleError = root.Q<Toggle>("toggleError");
            if (toggleError != null) { toggleError.value = _showError; toggleError.RegisterValueChangedCallback(ev => { _showError = ev.newValue; _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true; SavePrefs(); RefreshUI(); }); }

            var btnTagToggle = root.Q<Button>("btnTagToggle");
            if (btnTagToggle != null)
            {
                btnTagToggle.clicked += () =>
                {
                    _tagsEnabled = !_tagsEnabled;
                    _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                    SavePrefs();
                    RefreshUI();
                };
            }

            var btnMenu = root.Q<Button>("btnMenu");
            if (btnMenu != null) btnMenu.clicked += ShowContextMenu;

            // Remote connection button
            var toolbar = root.Q("toolbar");
            if (toolbar != null)
            {
                _remoteButton = new Button(ShowRemoteMenu);
                _remoteButton.text = "Remote: Off";
                _remoteButton.style.marginLeft = 4;
                _remoteButton.style.paddingLeft = 6;
                _remoteButton.style.paddingRight = 6;
                _remoteButton.style.height = 18;
                _remoteButton.style.fontSize = 11;
                // Insert before btnMenu (last child)
                if (btnMenu != null)
                    toolbar.Insert(toolbar.IndexOf(btnMenu), _remoteButton);
                else
                    toolbar.Add(_remoteButton);
                UpdateRemoteButton();
            }
        }

        private void BindSearchBar(VisualElement root)
        {
            _searchField = root.Q<TextField>("searchField");
            if (_searchField != null)
            {
                _searchField.value = _search;
                _searchField.RegisterValueChangedCallback(ev =>
                {
                    _search = ev.newValue;
                    _searchInputLastChangeTime = EditorApplication.timeSinceStartup;
                });
            }

            var btnSearchClear = root.Q<Button>("btnSearchClear");
            if (btnSearchClear != null)
            {
                btnSearchClear.clicked += () =>
                {
                    _search = ""; _searchApplied = "";
                    _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                    InvalidateDisplayCache();
                    if (_searchField != null) _searchField.value = "";
                    SavePrefs();
                    RefreshUI();
                };
            }

            var toggleRegex = root.Q<Toggle>("toggleRegex");
            if (toggleRegex != null)
            {
                toggleRegex.value = _searchRegex;
                toggleRegex.RegisterValueChangedCallback(ev => { _searchRegex = ev.newValue; _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true; InvalidateDisplayCache(); SavePrefs(); RefreshUI(); });
            }

            // Copy Results & Copy Matches are now accessible via ☰ Menu (ShowContextMenu)
        }


        private void BindListView()
        {
            if (_logListView == null) return;
            float lineHeight = 18f;
            _logListView.fixedItemHeight = lineHeight * Mathf.Clamp(_entryLines, 1, 10) + 4;
            _logListView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _logListView.makeItem = () =>
            {
                var row = new VisualElement { name = "log-row", style = { flexDirection = FlexDirection.Row } };
                row.AddToClassList("log-row");
                row.style.position = Position.Relative;
                var icon = new Image { name = "row-icon", style = { width = 18, height = 18, marginRight = 4 } };
                icon.AddToClassList("log-row-icon");
                var content = new VisualElement { name = "row-content" };
                content.AddToClassList("log-row-content");
                content.style.flexGrow = 1;
                var msg = new Label { name = "row-message" };
                msg.AddToClassList("log-row-message");
                msg.style.flexGrow = 1;
                msg.enableRichText = true;
                content.Add(msg);
                var tags = new VisualElement { name = "row-tags" };
                tags.AddToClassList("log-row-tags");
                var count = new Label { name = "row-count" };
                count.AddToClassList("log-row-count");
                row.Add(icon);
                row.Add(content);
                row.Add(tags);
                row.Add(count);
                // 一次性缓存 child 引用，避免 bindItem 中反复 Q<>（10k+ 条滚动时差距巨大）
                row.userData = new RowChildren { Icon = icon, Message = msg, Tags = tags, Count = count, TagLabels = new List<Label>() };
                return row;
            };
            _logListView.selectionType = SelectionType.Multiple;
            _logListView.bindItem = (e, i) =>
            {
                var filtered = _currentFilteredRowsForBinding;
                if (filtered == null || i < 0 || i >= filtered.Count) return;
                var row = filtered[i];
                var entry = _entries[row.entryIndex];
                // userData 在 makeItem 时已存 RowChildren；这里若被覆盖就懒重建
                var rc = e.userData as RowChildren;
                if (rc == null)
                {
                    rc = new RowChildren
                    {
                        Icon = e.Q<Image>("row-icon"),
                        Message = e.Q<VisualElement>("row-content")?.Q<Label>("row-message"),
                        Tags = e.Q<VisualElement>("row-tags"),
                        Count = e.Q<Label>("row-count"),
                        TagLabels = new List<Label>()
                    };
                    e.userData = rc;
                }
                rc.RowIndex = i;
                bool collapseShowing = _collapse && row.displayCount > 1;
                // 早退：若 entry / displayCount / 显示版本 / collapse 状态全相同，跳过整段
                if (ReferenceEquals(rc.LastEntry, entry)
                    && rc.LastDisplayCount == row.displayCount
                    && rc.LastDisplayCacheVersion == _displayCacheVersion
                    && rc.LastCollapseShowing == collapseShowing)
                {
                    return;
                }
                rc.LastEntry = entry;
                rc.LastDisplayCount = row.displayCount;
                rc.LastDisplayCacheVersion = _displayCacheVersion;
                rc.LastCollapseShowing = collapseShowing;

                if (rc.Icon != null)
                {
                    var tex = entry.LogType == LogType.Error || entry.LogType == LogType.Exception ? _iconError : entry.LogType == LogType.Warning ? _iconWarning : _iconLog;
                    rc.Icon.image = tex;
                }
                if (rc.Message != null)
                {
                    string cached = entry.CachedDisplayText;
                    if (cached == null || entry.CachedDisplayVersion != _displayCacheVersion)
                    {
                        string display = entry.Condition ?? "";
                        if (_showMessageNumber) display = "[" + entry.MessageNumber + "] " + display;
                        if (_showTimestamp && !string.IsNullOrEmpty(entry.TimeStamp)) display = "[" + entry.TimeStamp + "] " + display;
                        if (_showFrameCount) display = "[" + entry.FrameCount + "] " + display;
                        display = GetFirstLines(display, _entryLines);
                        cached = BuildMessageWithHighlight(display);
                        entry.CachedDisplayText = cached;
                        entry.CachedDisplayVersion = _displayCacheVersion;
                    }
                    rc.Message.text = cached;
                }
                if (rc.Tags != null)
                {
                    var existingLabels = rc.TagLabels;
                    int labelIndex = 0;
                    if (_tagsEnabled)
                    {
                        var entryTags = entry.Tags ?? LogEntry.EmptyTags;
                        for (int ti = 0; ti < entryTags.Length; ti++)
                        {
                            var tag = entryTags[ti];
                            if (string.IsNullOrEmpty(tag)) continue;
                            Label lbl;
                            if (labelIndex < existingLabels.Count)
                            {
                                lbl = existingLabels[labelIndex];
                                lbl.style.display = DisplayStyle.Flex;
                            }
                            else
                            {
                                lbl = new Label();
                                lbl.AddToClassList("log-row-tag");
                                rc.Tags.Add(lbl);
                                existingLabels.Add(lbl);
                            }
                            lbl.text = tag;
                            lbl.style.backgroundColor = GetTagColor(tag);
                            labelIndex++;
                        }
                    }
                    for (int j = labelIndex; j < existingLabels.Count; j++)
                        existingLabels[j].style.display = DisplayStyle.None;
                }
                if (rc.Count != null)
                {
                    if (collapseShowing)
                    {
                        rc.Count.text = ClampCount(row.displayCount, 999);
                        rc.Count.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        rc.Count.text = "";
                        rc.Count.style.display = DisplayStyle.None;
                    }
                }
            };
            _logListView.selectionChanged += _ =>
            {
                var selected = _logListView.selectedIndices.ToList();
                if (selected.Count == 0) { _selectedIndex = -1; UpdateDetailPanel(); return; }
                var filtered = _currentFilteredRowsForBinding ?? GetFilteredRows();
                int idxRow = selected[selected.Count - 1];
                if (idxRow >= 0 && idxRow < filtered.Count)
                {
                    _selectedIndex = filtered[idxRow].entryIndex;
                    UpdateDetailPanel();
                }
            };
            if (!_logListContextBound)
            {
                _logListContextBound = true;
                _logListView.RegisterCallback<ContextClickEvent>(evt =>
                {
                    if (_logListView == null) return;
                    var target = evt.target as VisualElement;
                    int idx = GetListViewIndexFromTarget(target);
                    if (idx >= 0 && !_logListView.selectedIndices.Contains(idx))
                    {
                        _logListView.SetSelection(idx);
                    }
                    if (!_logListView.selectedIndices.Any()) return;
                    int selectedCount = _logListView.selectedIndices.Count();
                    string copyLabel = selectedCount > 1 ? $"Copy ({selectedCount})" : "Copy";
                    string copyTsLabel = selectedCount > 1 ? $"Copy with Timestamp ({selectedCount})" : "Copy with Timestamp";
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent(copyLabel), false, () => CopySelectedMessagesToClipboard(false));
                    menu.AddItem(new GUIContent(copyTsLabel), false, () => CopySelectedMessagesToClipboard(true));
                    menu.ShowAsContext();
                });
            }
        }

        private void SyncTogglesFromState(VisualElement root)
        {
            var t = root.Q<Toggle>("toggleCollapse"); if (t != null) t.SetValueWithoutNotify(_collapse);
            t = root.Q<Toggle>("toggleClearOnPlay"); if (t != null) t.SetValueWithoutNotify(_clearOnPlay);
            t = root.Q<Toggle>("toggleClearOnBuild"); if (t != null) t.SetValueWithoutNotify(_clearOnBuild);
            t = root.Q<Toggle>("toggleErrorPause"); if (t != null) t.SetValueWithoutNotify(_errorPause);
            t = root.Q<Toggle>("toggleViewLock"); if (t != null) t.SetValueWithoutNotify(_viewLocked);
            t = root.Q<Toggle>("toggleLog"); if (t != null) t.SetValueWithoutNotify(_showLog);
            t = root.Q<Toggle>("toggleWarning"); if (t != null) t.SetValueWithoutNotify(_showWarning);
            t = root.Q<Toggle>("toggleError"); if (t != null) t.SetValueWithoutNotify(_showError);
            t = root.Q<Toggle>("toggleRegex"); if (t != null) t.SetValueWithoutNotify(_searchRegex);
        }

        private void SyncSearchField()
        {
            if (_searchField != null) _searchField.SetValueWithoutNotify(_search);
        }

        private void SyncTagBarFields()
        {
            var root = rootVisualElement;
            if (root == null) return;
            var tagSearchField = root.Q<TextField>("tagSearchField");
            if (tagSearchField != null) tagSearchField.SetValueWithoutNotify(_tagSearch ?? "");
            UpdateTagSortMenuButton();
            UpdateTagSettingsControls();
        }

        private void UpdateTagSortMenuButton()
        {
            var root = rootVisualElement;
            if (root == null) return;
            var btn = root.Q<Button>("btnTagSortMenu");
            string arrow = _tagSortDesc ? "▼" : "▲";
            if (btn != null)
                btn.text = (_tagSortMode == TagSortMode.Count ? "Count " : _tagSortMode == TagSortMode.Recent ? "Recent " : "Name ") + arrow;
        }

        private bool _isRefreshing;

        private void RefreshUI()
        {
            if (_isRefreshing) return;
            if (rootVisualElement == null) return;
            // 兜底：若显示参数被外部窗口（SettingsWindow 等）改动而未走 InvalidateDisplayCache，
            // 通过参数 hash 比较检测变化。轻量，每次 RefreshUI 一次。
            int displayParamsHash = (_showMessageNumber ? 1 : 0)
                | (_showTimestamp ? 2 : 0)
                | (_showFrameCount ? 4 : 0)
                | (_searchRegex ? 8 : 0)
                | (_entryLines << 4)
                | ((_searchApplied?.GetHashCode() ?? 0) ^ 0x5a5a5a5a);
            if (displayParamsHash != _lastDisplayParamsHash)
            {
                _lastDisplayParamsHash = displayParamsHash;
                InvalidateDisplayCache();
            }
            _isRefreshing = true;
            try
            {
            if (rootVisualElement.childCount == 0) BuildUI();
            FlushPendingEntries();
            var root = rootVisualElement.Q<VisualElement>("root");
            if (root == null)
            {
                BuildUI();
                root = rootVisualElement.Q<VisualElement>("root");
                if (root == null) return;
            }
            var toggleViewLock = root.Q<Toggle>("toggleViewLock");
            if (toggleViewLock != null) toggleViewLock.SetValueWithoutNotify(_viewLocked);

            /* ????? Toggle ???????????????????? 99?*/
            var (logCount, warnCount, errCount) = CountByTypeUnfiltered();
            int cap = 99;
            var toggleLog = root.Q<Toggle>("toggleLog");
            if (toggleLog != null) { toggleLog.SetValueWithoutNotify(_showLog); toggleLog.label = ClampCount(logCount, cap); }
            var toggleWarning = root.Q<Toggle>("toggleWarning");
            if (toggleWarning != null) { toggleWarning.SetValueWithoutNotify(_showWarning); toggleWarning.label = ClampCount(warnCount, cap); }
            var toggleError = root.Q<Toggle>("toggleError");
            if (toggleError != null) { toggleError.SetValueWithoutNotify(_showError); toggleError.label = ClampCount(errCount, cap); }

            var tagBarRow = root.Q<VisualElement>("tagBarRow");
            if (tagBarRow != null) tagBarRow.style.display = _tagsEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            var btnTagToggle = root.Q<Button>("btnTagToggle");
            if (btnTagToggle != null) btnTagToggle.text = _tagsEnabled ? "Tags ▾" : "Tags";
            RebuildTagBar();

            TryScheduleBackgroundFilter();
            var filtered = GetFilteredRows();
            _currentFilteredRowsForBinding = filtered;
            if (_logListView != null)
            {
                // ???? _entryLines ??????????????Log Entry ?????????????????
                float lineHeight = 18f;
                _logListView.fixedItemHeight = lineHeight * Mathf.Clamp(_entryLines, 1, 10) + 4;

                bool firstSetup = _logListView.itemsSource == null;
                bool sourceChanged = !ReferenceEquals(_logListView.itemsSource, filtered);
                if (sourceChanged) _logListView.itemsSource = filtered;
                if (firstSetup || _listViewNeedsRebuild)
                {
                    _logListView.Rebuild();
                    _listViewNeedsRebuild = false;
                }
                else
                {
                    _logListView.RefreshItems();
                }
                int selectedFilteredIndex = -1;
                if (_selectedIndex >= 0)
                {
                    // 优先末尾扫描（选中通常在尾部，循环上限可控）
                    int scanEnd = filtered.Count;
                    int scanStart = Math.Max(0, scanEnd - 64);
                    for (int i = scanEnd - 1; i >= scanStart; i--)
                    {
                        if (filtered[i].entryIndex == _selectedIndex) { selectedFilteredIndex = i; break; }
                    }
                    if (selectedFilteredIndex < 0)
                    {
                        for (int i = 0; i < scanStart; i++)
                        {
                            if (filtered[i].entryIndex == _selectedIndex) { selectedFilteredIndex = i; break; }
                        }
                    }
                }
                _logListView.SetSelectionWithoutNotify(selectedFilteredIndex >= 0 ? new[] { selectedFilteredIndex } : new List<int>());
            }
            UpdateDetailPanel();
            }
            finally
            {
                _isRefreshing = false;
            }
        }


        private void BindDetailDoubleClick()
        {
            if (_detailField == null) return;
            // Try to bind on inner text input element first
            VisualElement input = _detailField.Q<VisualElement>(className: "unity-text-input") ??
                                   _detailField.Q<VisualElement>(className: "unity-text-input__input") ??
                                   _detailField;
            input.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount != 2) return;
                // double-click detected (ClickEvent)
                _detailField.schedule.Execute(() => OpenStackLinkFromSelectionOrCaret()).StartingIn(1);
            }, TrickleDown.TrickleDown);
            input.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || evt.clickCount != 2) return;
                // double-click detected (PointerDownEvent)
                _detailField.schedule.Execute(() => OpenStackLinkFromSelectionOrCaret()).StartingIn(1);
            }, TrickleDown.TrickleDown);
        }

        private void UpdateDetailPanel()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _entries.Count)
            {
                _detailField.SetValueWithoutNotify("Select a message to view details and stack trace.");
                _detailField.AddToClassList("detail-empty");
                return;
            }
            _detailField.RemoveFromClassList("detail-empty");
            var e = _entries[_selectedIndex];
            string full = e.FullMessage;
            var prefixParts = new List<string>();
            if (_showMessageNumber) prefixParts.Add("[" + e.MessageNumber + "]");
            if (_showTimestamp && !string.IsNullOrEmpty(e.TimeStamp)) prefixParts.Add("[" + e.TimeStamp + "]");
            if (_showFrameCount) prefixParts.Add("[" + e.FrameCount + "]");
            if (_collapse)
            {
                string firstTime = null;
                string lastTime = null;
                int totalCount = e.Count;
                if (_collapseGlobal && _cachedCollapseGroupInfo != null && _cachedCollapseGroupInfo.TryGetValue(_selectedIndex, out var info))
                {
                    totalCount = info.totalCount;
                    firstTime = info.firstTime;
                    lastTime = info.lastTime;
                }
                else if (!_collapseGlobal && e.Count > 1)
                {
                    firstTime = GetEntryFirstTime(e);
                    lastTime = GetEntryLastTime(e);
                }
                if (totalCount > 1 && !string.IsNullOrEmpty(firstTime) && !string.IsNullOrEmpty(lastTime))
                {
                    prefixParts.Add("[First " + firstTime + "]");
                    prefixParts.Add("[Last " + lastTime + "]");
                }
            }
            if (prefixParts.Count > 0)
                full = string.Join(" ", prefixParts) + " " + full;
            if (!_showStackTrace)
                full += "\n\n[Stack Trace recording is disabled. Enable it via the ☰ menu to capture traces.]";
            _detailField.SetValueWithoutNotify(full);
        }

        private string BuildMessageWithHighlight(string text)
        {
            if (string.IsNullOrEmpty(_searchApplied)) return text;
            int idx, len;
            if (_searchRegex)
            {
                var regex = GetOrCreateSearchRegex();
                try
                {
                    var m = regex != null ? regex.Match(text) : Regex.Match(text, _searchApplied);
                    if (!m.Success) return text;
                    idx = m.Index; len = m.Length;
                }
                catch { return text; }
            }
            else
            {
                idx = text.IndexOf(_searchApplied, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return text;
                len = _searchApplied.Length;
            }
            return text.Substring(0, idx) + "<color=#FFEB3B>" + text.Substring(idx, len) + "</color>" + text.Substring(idx + len);
        }

        private void ShowSearchHistoryMenu()
        {
            var menu = new GenericMenu();
            if (_searchHistory.Count == 0)
                menu.AddDisabledItem(new GUIContent("(No history)"));
            else
            {
                foreach (string item in _searchHistory)
                {
                    string s = item;
                    string display = s.Length > 40 ? s.Substring(0, 37) + "..." : s;
                    menu.AddItem(new GUIContent(display), false, () =>
                    {
                        _search = s; _searchApplied = s;
                        _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                        PushSearchHistory(s);
                        if (_searchField != null) _searchField.value = s;
                        SavePrefs();
                        RefreshUI();
                    });
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Clear Search History"), false, () => { _searchHistory.Clear(); SaveSearchHistory(); RefreshUI(); });
            }
            menu.ShowAsContext();
        }

        private void ShowSearchFilterMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Time Range"), _filterTimeRange, () => { _filterTimeRange = !_filterTimeRange; _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Number Range"), _filterNumberRange, () => { _filterNumberRange = !_filterNumberRange; _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Frame Range"), _filterFrameRange, () => { _filterFrameRange = !_filterFrameRange; _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Set Time Range..."), false, ShowTimeRangeSettings);
            menu.AddItem(new GUIContent("Set Number Range..."), false, ShowNumberRangeSettings);
            menu.AddItem(new GUIContent("Set Frame Range..."), false, ShowFrameRangeSettings);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Clear All Filters"), false, ClearAllFilters);
            menu.ShowAsContext();
        }

        private void ClearAllFilters()
        {
            _filterTimeRange = false;
            _filterNumberRange = false;
            _filterFrameRange = false;
            _filterAppendOnly = false;
            _filterDirty = true;
            _filterCriteriaVersion++;
            _tagCountsDirty = true;
            SavePrefs();
            RefreshUI();
        }

        /// <summary> ???????????????????????????</summary>
        private (string min, string max) GetDefaultTimeRangeFromLogs()
        {
            if (_entries.Count == 0)
            {
                string now = DateTime.Now.ToString("HH:mm:ss.fff");
                return (now, now);
            }
            string minTs = null, maxTs = null;
            foreach (var e in _entries)
            {
                if (string.IsNullOrEmpty(e?.TimeStamp)) continue;
                if (minTs == null || string.Compare(e.TimeStamp, minTs, StringComparison.Ordinal) < 0) minTs = e.TimeStamp;
                if (maxTs == null || string.Compare(e.TimeStamp, maxTs, StringComparison.Ordinal) > 0) maxTs = e.TimeStamp;
            }
            if (minTs == null) { string n = DateTime.Now.ToString("HH:mm:ss.fff"); return (n, n); }
            return (minTs, maxTs);
        }

        /// <summary> ????????????????????? 0,0??</summary>
        private (int min, int max) GetDefaultNumberRangeFromLogs()
        {
            if (_entries.Count == 0) return (0, 0);
            int minN = int.MaxValue, maxN = int.MinValue;
            foreach (var e in _entries)
            {
                if (e == null) continue;
                if (e.MessageNumber < minN) minN = e.MessageNumber;
                if (e.MessageNumber > maxN) maxN = e.MessageNumber;
            }
            return (minN == int.MaxValue ? 0 : minN, maxN == int.MinValue ? 0 : maxN);
        }

        /// <summary> ????????????????????? 0,0??</summary>
        private (int min, int max) GetDefaultFrameRangeFromLogs()
        {
            if (_entries.Count == 0) return (0, 0);
            int minF = int.MaxValue, maxF = int.MinValue;
            foreach (var e in _entries)
            {
                if (e == null) continue;
                if (e.FrameCount < minF) minF = e.FrameCount;
                if (e.FrameCount > maxF) maxF = e.FrameCount;
            }
            return (minF == int.MaxValue ? 0 : minF, maxF == int.MinValue ? 0 : maxF);
        }

        private static bool TryParseTime(string s, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(s)) return true;
            if (DateTime.TryParse(s, out DateTime dt))
            {
                normalized = dt.ToString("HH:mm:ss.fff");
                return true;
            }
            return false;
        }

        private void ShowTimeRangeSettings()
        {
            var (defMin, defMax) = GetDefaultTimeRangeFromLogs();
            string initMin = !string.IsNullOrEmpty(_filterTimeMin) ? _filterTimeMin : defMin;
            string initMax = !string.IsNullOrEmpty(_filterTimeMax) ? _filterTimeMax : defMax;
            SearchFilterRangeWindow.Show("Time Range", initMin, initMax, "HH:mm:ss or HH:mm:ss.fff", (min, max) =>
            {
                if (!string.IsNullOrWhiteSpace(min) && !TryParseTime(min, out _)) return "Invalid start time format";
                if (!string.IsNullOrWhiteSpace(max) && !TryParseTime(max, out _)) return "Invalid end time format";
                if (!string.IsNullOrWhiteSpace(min) && !string.IsNullOrWhiteSpace(max) && string.Compare(min, max, StringComparison.Ordinal) > 0)
                    return "Start time cannot be later than end time";
                return null;
            }, (min, max) =>
            {
                _filterTimeMin = string.IsNullOrWhiteSpace(min) ? "" : (TryParseTime(min, out string n) ? n : min);
                _filterTimeMax = string.IsNullOrWhiteSpace(max) ? "" : (TryParseTime(max, out string n2) ? n2 : max);
                _filterTimeRange = true;
                _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                SavePrefs(); RefreshUI();
            });
        }

        private void ShowNumberRangeSettings()
        {
            var (defMin, defMax) = GetDefaultNumberRangeFromLogs();
            bool customized = _filterNumberMin != 1 || _filterNumberMax != int.MaxValue;
            string initMin = customized ? _filterNumberMin.ToString() : defMin.ToString();
            string initMax = customized ? (_filterNumberMax == int.MaxValue ? "" : _filterNumberMax.ToString()) : defMax.ToString();
            SearchFilterRangeWindow.Show("Number Range", initMin, initMax, "Integer", (minStr, maxStr) =>
            {
                if (!int.TryParse(minStr ?? "0", out int minVal)) return "Start number must be an integer";
                if (!string.IsNullOrWhiteSpace(maxStr) && !int.TryParse(maxStr, out int maxVal)) return "End number must be an integer";
                int max = string.IsNullOrWhiteSpace(maxStr) ? int.MaxValue : int.Parse(maxStr);
                if (minVal > max) return "Start number cannot be greater than end number";
                return null;
            }, (minStr, maxStr) =>
            {
                int.TryParse(minStr ?? "0", out _filterNumberMin);
                _filterNumberMax = string.IsNullOrEmpty(maxStr) ? int.MaxValue : (int.TryParse(maxStr, out int m) ? m : int.MaxValue);
                _filterNumberRange = true;
                _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                SavePrefs(); RefreshUI();
            });
        }

        private void ShowFrameRangeSettings()
        {
            var (defMin, defMax) = GetDefaultFrameRangeFromLogs();
            bool customized = _filterFrameMin != 0 || _filterFrameMax != int.MaxValue;
            string initMin = customized ? _filterFrameMin.ToString() : defMin.ToString();
            string initMax = customized ? (_filterFrameMax == int.MaxValue ? "" : _filterFrameMax.ToString()) : defMax.ToString();
            SearchFilterRangeWindow.Show("Frame Range", initMin, initMax, "Integer", (minStr, maxStr) =>
            {
                if (!int.TryParse(minStr ?? "0", out int minVal)) return "Start frame must be an integer";
                if (!string.IsNullOrWhiteSpace(maxStr) && !int.TryParse(maxStr, out int maxVal)) return "End frame must be an integer";
                int max = string.IsNullOrWhiteSpace(maxStr) ? int.MaxValue : int.Parse(maxStr);
                if (minVal > max) return "Start frame cannot be greater than end frame";
                return null;
            }, (minStr, maxStr) =>
            {
                int.TryParse(minStr ?? "0", out _filterFrameMin);
                _filterFrameMax = string.IsNullOrEmpty(maxStr) ? int.MaxValue : (int.TryParse(maxStr, out int m) ? m : int.MaxValue);
                _filterFrameRange = true;
                _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                SavePrefs(); RefreshUI();
            });
        }

        private void ShowCapacitySettings()
        {
            CapacitySettingsWindow.Show(_maxEntries, _maxLoadEntries, (maxEntries, maxLoadEntries) =>
            {
                _maxEntries = Mathf.Max(100, maxEntries);
                _maxLoadEntries = Mathf.Max(_maxEntries, maxLoadEntries);
                SavePrefs();
                TrimEntriesToMax();
                EnhancedConsoleLogFile.RewriteFileWithEntries(_entries);
                _filterAppendOnly = false;
                _filterDirty = true;
                _filterCriteriaVersion++;
                _tagCountsDirty = true;
                RefreshUI();
            });
        }

        private void RestoreCapacityDefaults()
        {
            _maxEntries = DefaultMaxEntries;
            _maxLoadEntries = DefaultMaxLoadEntries;
            SavePrefs();
            TrimEntriesToMax();
            EnhancedConsoleLogFile.RewriteFileWithEntries(_entries);
            _filterAppendOnly = false;
            _filterDirty = true;
            _filterCriteriaVersion++;
            _tagCountsDirty = true;
            RefreshUI();
        }

        private void CloneWindow()
        {
            var w = CreateInstance<EnhancedConsoleWindow>();
            w.titleContent = new GUIContent("Enhanced Console");
            w.minSize = new Vector2(300, 200);
            w.Show();
            w.CopyStateFrom(this);
            w.Focus();
        }

        private void CopyStateFrom(EnhancedConsoleWindow other)
        {
            if (other == null) return;
            // 复制条目数据（完整复制源窗口状态）
            _entries.Clear();
            _entries.AddRange(other._entries);
            _nextMessageNumber = other._nextMessageNumber;
            _collapse = other._collapse;
            _collapseGlobal = other._collapseGlobal;
            _clearOnPlay = other._clearOnPlay;
            _clearOnBuild = other._clearOnBuild;
            _errorPause = other._errorPause;
            _showLog = other._showLog;
            _showWarning = other._showWarning;
            _showError = other._showError;
            _showTimestamp = other._showTimestamp;
            _showFrameCount = other._showFrameCount;
            _showMessageNumber = other._showMessageNumber;
            _entryLines = other._entryLines;
            _maxEntries = other._maxEntries;
            _maxLoadEntries = other._maxLoadEntries;
            _stackTraceLog = other._stackTraceLog;
            _stackTraceWarning = other._stackTraceWarning;
            _stackTraceError = other._stackTraceError;
            _detailHeight = other._detailHeight;
            _tagsEnabled = other._tagsEnabled;
            _tagsCollapsed = other._tagsCollapsed;
            _tagSortMode = other._tagSortMode;
            _tagSortDesc = other._tagSortDesc;
            _tagSearch = other._tagSearch ?? "";
            _filterTimeRange = other._filterTimeRange;
            _filterNumberRange = other._filterNumberRange;
            _filterFrameRange = other._filterFrameRange;
            _filterTimeMin = other._filterTimeMin;
            _filterTimeMax = other._filterTimeMax;
            _filterNumberMin = other._filterNumberMin;
            _filterNumberMax = other._filterNumberMax;
            _filterFrameMin = other._filterFrameMin;
            _filterFrameMax = other._filterFrameMax;
            _searchRegex = other._searchRegex;
            _search = other._search;
            _searchApplied = other._searchApplied;
            _viewLocked = other._viewLocked;

            _selectedTags.Clear();
            foreach (var t in other._selectedTags) _selectedTags.Add(t);
            _excludedTags.Clear();
            foreach (var t in other._excludedTags) _excludedTags.Add(t);

            _cachedFilteredRows = null;
            _cachedTagInfo = null;
            _cachedCollapseGroupInfo = null;
            _filterAppendOnly = false;
            _filterDirty = true;
            _filterCriteriaVersion++;
            _tagCountsDirty = true;
            InvalidateDisplayCache();
            ApplyStackTraceSettings();
            SyncSearchField();
            SyncTagBarFields();
            SyncTogglesFromState(rootVisualElement);
            RefreshUI();
        }

        private enum ExportFormat { Txt, Csv, Json }

        [Serializable]
        private class ExportEntry
        {
            public int messageNumber;
            public string logType;
            public int count;
            public string timeStamp;
            public string firstTimeStamp;
            public string lastTimeStamp;
            public int frameCount;
            public string condition;
            public string stackTrace;
            public string[] tags;
        }

        [Serializable]
        private class ExportEntryList
        {
            public List<ExportEntry> entries = new List<ExportEntry>();
        }

        private void ExportEntries(ExportFormat format, bool filtered)
        {
            string ext = format == ExportFormat.Txt ? "txt" : format == ExportFormat.Csv ? "csv" : "json";
            string path = EditorUtility.SaveFilePanel("Export Logs", "", "EnhancedConsoleExport." + ext, ext);
            if (string.IsNullOrEmpty(path)) return;

            var exportList = new List<ExportEntry>();
            if (filtered)
            {
                var rows = GetFilteredRows();
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (row.entryIndex < 0 || row.entryIndex >= _entries.Count) continue;
                    var e = _entries[row.entryIndex];
                    string firstTime = GetEntryFirstTime(e);
                    string lastTime = GetEntryLastTime(e);
                    int count = row.displayCount;
                    if (_collapse && _collapseGlobal && _cachedCollapseGroupInfo != null && _cachedCollapseGroupInfo.TryGetValue(row.entryIndex, out var info))
                    {
                        count = info.totalCount;
                        firstTime = info.firstTime;
                        lastTime = info.lastTime;
                    }
                    exportList.Add(BuildExportEntry(e, count, firstTime, lastTime));
                }
            }
            else
            {
                foreach (var e in _entries)
                {
                    if (e == null) continue;
                    exportList.Add(BuildExportEntry(e, e.Count, GetEntryFirstTime(e), GetEntryLastTime(e)));
                }
            }

            if (exportList.Count == 0)
            {
                EditorUtility.DisplayDialog("Export Logs", "No entries to export.", "OK");
                return;
            }

            if (format == ExportFormat.Txt)
                File.WriteAllText(path, BuildTxtExport(exportList), System.Text.Encoding.UTF8);
            else if (format == ExportFormat.Csv)
                File.WriteAllText(path, BuildCsvExport(exportList), System.Text.Encoding.UTF8);
            else
                File.WriteAllText(path, BuildJsonExport(exportList), System.Text.Encoding.UTF8);
        }

        private static ExportEntry BuildExportEntry(LogEntry e, int count, string firstTime, string lastTime)
        {
            return new ExportEntry
            {
                messageNumber = e.MessageNumber,
                logType = e.LogType.ToString(),
                count = count,
                timeStamp = e.TimeStamp ?? "",
                firstTimeStamp = firstTime ?? "",
                lastTimeStamp = lastTime ?? "",
                frameCount = e.FrameCount,
                condition = e.Condition ?? "",
                stackTrace = e.StackTrace ?? "",
                tags = e.Tags ?? LogEntry.EmptyTags
            };
        }

        private static string BuildTxtExport(List<ExportEntry> entries)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var e in entries)
            {
                sb.Append("[");
                sb.Append(e.logType);
                sb.Append("] ");
                if (e.count > 1) sb.Append("x").Append(e.count).Append(" ");
                if (!string.IsNullOrEmpty(e.firstTimeStamp) || !string.IsNullOrEmpty(e.lastTimeStamp))
                {
                    sb.Append("[");
                    sb.Append(string.IsNullOrEmpty(e.firstTimeStamp) ? e.timeStamp : e.firstTimeStamp);
                    sb.Append(" - ");
                    sb.Append(string.IsNullOrEmpty(e.lastTimeStamp) ? e.timeStamp : e.lastTimeStamp);
                    sb.Append("] ");
                }
                sb.Append(e.condition);
                if (e.tags != null && e.tags.Length > 0)
                    sb.Append(" [Tags: ").Append(string.Join(", ", e.tags)).Append("]");
                sb.AppendLine();
                if (!string.IsNullOrEmpty(e.stackTrace))
                    sb.AppendLine(e.stackTrace);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (value == null) return "";
            bool needQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            if (!needQuote) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string BuildCsvExport(List<ExportEntry> entries)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("MessageNumber,LogType,Count,TimeStamp,FirstTimeStamp,LastTimeStamp,FrameCount,Condition,StackTrace,Tags");
            foreach (var e in entries)
            {
                sb.Append(e.messageNumber).Append(",");
                sb.Append(EscapeCsv(e.logType)).Append(",");
                sb.Append(e.count).Append(",");
                sb.Append(EscapeCsv(e.timeStamp)).Append(",");
                sb.Append(EscapeCsv(e.firstTimeStamp)).Append(",");
                sb.Append(EscapeCsv(e.lastTimeStamp)).Append(",");
                sb.Append(e.frameCount).Append(",");
                sb.Append(EscapeCsv(e.condition)).Append(",");
                sb.Append(EscapeCsv(e.stackTrace)).Append(",");
                sb.Append(EscapeCsv(e.tags != null ? string.Join(";", e.tags) : ""));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string BuildJsonExport(List<ExportEntry> entries)
        {
            var list = new ExportEntryList { entries = entries };
            return JsonUtility.ToJson(list, true);
        }

        private void ShowContextMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Collapse/Off"), !_collapse, () =>
            {
                _collapse = false;
                _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                SavePrefs(); RefreshUI();
            });
            menu.AddItem(new GUIContent("Collapse/Adjacent Only"), _collapse && !_collapseGlobal, () =>
            {
                _collapse = true;
                _collapseGlobal = false;
                _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                SavePrefs(); RefreshUI();
            });
            menu.AddItem(new GUIContent("Collapse/Global (All)"), _collapse && _collapseGlobal, () =>
            {
                _collapse = true;
                _collapseGlobal = true;
                _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                SavePrefs(); RefreshUI();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("View/Lock View"), _viewLocked, () =>
            {
                _viewLocked = !_viewLocked;
                SavePrefs();
                if (!_viewLocked) RefreshUI();
            });
            menu.AddItem(new GUIContent("Window/Clone"), false, () => CloneWindow());
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Settings..."), false, () => EnhancedConsoleSettingsWindow.OpenAsUtility());
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Export/Filtered/TXT"), false, () => ExportEntries(ExportFormat.Txt, true));
            menu.AddItem(new GUIContent("Export/Filtered/CSV"), false, () => ExportEntries(ExportFormat.Csv, true));
            menu.AddItem(new GUIContent("Export/Filtered/JSON"), false, () => ExportEntries(ExportFormat.Json, true));
            menu.AddItem(new GUIContent("Export/All/TXT"), false, () => ExportEntries(ExportFormat.Txt, false));
            menu.AddItem(new GUIContent("Export/All/CSV"), false, () => ExportEntries(ExportFormat.Csv, false));
            menu.AddItem(new GUIContent("Export/All/JSON"), false, () => ExportEntries(ExportFormat.Json, false));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Filter/Clear All Filters"), false, ClearAllFilters);
            if (_searchHistory.Count > 0)
            {
                foreach (string item in _searchHistory)
                {
                    string s = item;
                    string display = (s.Length > 50 ? s.Substring(0, 47) + "..." : s).Replace("/", "?M");
                    menu.AddItem(new GUIContent("Search/History/" + display), false, () => { _search = s; _searchApplied = s; _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true; InvalidateDisplayCache(); PushSearchHistory(s); if (_searchField != null) _searchField.value = s; SavePrefs(); RefreshUI(); });
                }
                menu.AddItem(new GUIContent("Search/Clear History"), false, () => { _searchHistory.Clear(); SaveSearchHistory(); RefreshUI(); });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Jump to Line (Ctrl+G)..."), false, () => JumpToLineWindow.Open(this));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Tags/Tag Settings..."), false, () => TagRulesWindow.Open(this));
            menu.AddItem(new GUIContent("Tags/Recompute All Tags"), false, RecomputeAllTags);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Open Editor Log"), false, OpenEditorLog);
            menu.AddItem(new GUIContent("Open Player Log"), false, OpenPlayerLog);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Copy/Copy Matched Results"), false, CopyMatchedResultsToClipboard);
            menu.AddItem(new GUIContent("Copy/Copy Regex Matches"), false, CopyRegexMatchPartsToClipboard);
            menu.ShowAsContext();
        }

        #endregion

        #region Remote

        private void UpdateRemoteButton()
        {
            if (_remoteButton == null) return;
            if (RemoteConsoleServer.IsRunning)
            {
                int count = RemoteConsoleServer.ClientCount;
                _remoteButton.text = count > 0 ? $"Remote: {count}" : "Remote: 0";
                _remoteButton.style.backgroundColor = count > 0
                    ? new Color(0.2f, 0.6f, 0.2f, 0.6f)
                    : new Color(0.4f, 0.4f, 0.1f, 0.6f);
            }
            else
            {
                _remoteButton.text = "Remote: Off";
                _remoteButton.style.backgroundColor = StyleKeyword.Null;
            }
        }

        private void ShowRemoteMenu()
        {
            var menu = new GenericMenu();
            if (RemoteConsoleServer.IsRunning)
            {
                menu.AddItem(new GUIContent($"Server Running (Port {RemoteConsoleServer.Port})"), false, () => { });
                menu.AddItem(new GUIContent($"Connected Clients: {RemoteConsoleServer.ClientCount}"), false, () => { });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Stop Server"), false, () =>
                {
                    RemoteConsoleServer.Stop();
                    UpdateRemoteButton();
                });
            }
            else
            {
                menu.AddItem(new GUIContent("Start Server"), false, () =>
                {
                    RemoteConsoleServer.Start();
                    UpdateRemoteButton();
                });
            }
            menu.AddSeparator("");
            bool autoStart = RemoteConsoleServer.GetAutoStart();
            menu.AddItem(new GUIContent("Auto Start"), autoStart, () =>
            {
                RemoteConsoleServer.SetAutoStart(!autoStart);
            });
            menu.ShowAsContext();
        }

        private void OnRemoteClientCountChanged(int _)
        {
            UpdateRemoteButton();
            Repaint();
        }

        #endregion
    }

    /// <summary>
    /// ??????Enhanced Console?Clear on Build???
    /// </summary>
    public class EnhancedConsoleBuildPreprocess : UnityEditor.Build.IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            if (!EditorPrefs.GetBool("EnhancedConsole.ClearOnBuild", false))
                return;
            EnhancedConsoleLogFile.ClearFile();
            var w = UnityEngine.Resources.FindObjectsOfTypeAll<EnhancedConsoleWindow>();
            if (w != null)
            {
                foreach (var window in w)
                {
                    if (window != null) window.Clear();
                }
            }
        }
    }
}
