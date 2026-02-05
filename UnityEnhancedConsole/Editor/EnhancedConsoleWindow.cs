using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
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

        private const string PrefCollapse = "EnhancedConsole.Collapse";
        private const string PrefClearOnPlay = "EnhancedConsole.ClearOnPlay";
        private const string PrefClearOnBuild = "EnhancedConsole.ClearOnBuild";
        private const string PrefErrorPause = "EnhancedConsole.ErrorPause";
        private const string PrefShowLog = "EnhancedConsole.ShowLog";
        private const string PrefShowWarning = "EnhancedConsole.ShowWarning";
        private const string PrefShowError = "EnhancedConsole.ShowError";
        private const string PrefEntryLines = "EnhancedConsole.EntryLines";
        private const string PrefStackTraceLog = "EnhancedConsole.StackTraceLog";
        private const string PrefStackTraceWarning = "EnhancedConsole.StackTraceWarning";
        private const string PrefStackTraceError = "EnhancedConsole.StackTraceError";
        private const string PrefDetailHeight = "EnhancedConsole.DetailHeight";
        private const string PrefShowTimestamp = "EnhancedConsole.ShowTimestamp";
        private const string PrefShowFrameCount = "EnhancedConsole.ShowFrameCount";
        private const string PrefSearchRegex = "EnhancedConsole.SearchRegex";
        private const string PrefShowMessageNumber = "EnhancedConsole.ShowMessageNumber";
        private const string PrefSearchHistoryPrefix = "EnhancedConsole.SearchHistory.";
        private const int MaxSearchHistory = 20;
        private const string PrefTagsEnabled = "EnhancedConsole.TagsEnabled";
        private const string PrefFilterTimeRange = "EnhancedConsole.FilterTimeRange";
        private const string PrefFilterNumberRange = "EnhancedConsole.FilterNumberRange";
        private const string PrefFilterFrameRange = "EnhancedConsole.FilterFrameRange";
        private const string PrefFilterTimeMin = "EnhancedConsole.FilterTimeMin";
        private const string PrefFilterTimeMax = "EnhancedConsole.FilterTimeMax";
        private const string PrefFilterNumberMin = "EnhancedConsole.FilterNumberMin";
        private const string PrefFilterNumberMax = "EnhancedConsole.FilterNumberMax";
        private const string PrefFilterFrameMin = "EnhancedConsole.FilterFrameMin";
        private const string PrefFilterFrameMax = "EnhancedConsole.FilterFrameMax";
        private const float SplitterHeight = 5f;
        private const float TimeFrameGap = 0f;
        private const float MinListHeight = 60f;
        private const float MinDetailHeight = 60f;
        /// <summary> ????????????????????N ???</summary>
        private const int MaxEntries = 20000;
        /// <summary> ??????????????????????????????N ????</summary>
        private const int MaxLoadEntries = 50000;
        /// <summary> ?????????????????</summary>
        private const int ListVirtualBufferRows = 10;
        /// <summary> ??????AddEntry ??Repaint ???????????????</summary>
        private const double MinRepaintIntervalMs = 50;
        /// <summary> ?????? / ???????????????????????????</summary>
        private const int MaxCopyLines = 100000;

        private static readonly List<LogEntry> PendingEntries = new List<LogEntry>();
        private static readonly object PendingLock = new object();

        /// <summary> ????????????????Clear ???????????????????</summary>
        private static bool _hasCompilationErrors;
        private static bool _currentCycleHasErrors;

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

        private static Color GetTagColor(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return TagColors[0];
            int i = (tag.GetHashCode() & 0x7FFFFFFF) % TagColors.Length;
            return TagColors[i];
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
        private bool _clearOnPlay;
        private bool _clearOnBuild;
        private bool _errorPause;
        private bool _showLog = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private bool _showTimestamp;
        private bool _showFrameCount;
        private bool _showMessageNumber;
        private int _entryLines = 2;
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
        private bool _filterDirty = true;
        private Dictionary<string, int> _cachedTagCounts;
        private bool _tagCountsDirty = true;
        private string _searchApplied = "";
        private double _searchInputLastChangeTime;
        private double _lastRepaintTime;
        private bool _repaintScheduled;

        /* UI Toolkit ?? */
        private ListView _logListView;
        private TextField _detailField;
        // private VisualElement _detailLinks; // removed (double-click to open)
        private TextField _searchField;
        private TwoPaneSplitView _mainSplit;
        private VisualElement _tagBarContainer;
        private Texture2D _iconLog;
        private Texture2D _iconWarning;
        private Texture2D _iconError;

        #endregion

        [MenuItem("Window/General/Enhanced Console %#d", false, 2000)]
        public static void Open()
        {
            var w = GetWindow<EnhancedConsoleWindow>("Enhanced Console", true);
            w.minSize = new Vector2(300, 200);
            w.Focus();
        }

        private void OnEnable()
        {
            LoadPrefs();
            _entries.Clear();
            _entries.AddRange(EnhancedConsoleLogFile.LoadEntries(MaxLoadEntries));
            for (int i = 0; i < _entries.Count; i++)
                _entries[i].MessageNumber = i + 1;
            _nextMessageNumber = _entries.Count;
            for (int i = 0; i < _entries.Count; i++)
                EnhancedConsoleTagLogic.ComputeTags(_entries[i]);
            _cachedFilteredRows = null;
            _cachedTagCounts = null;
            _filterDirty = true;
            _tagCountsDirty = true;
            TrimEntriesToMax();
            // ????Threaded???? logMessageReceived ???Unity ??????????????
            
            Application.logMessageReceivedThreaded += HandleLogThreaded;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            LoadIcons();
            BuildUI();
            ApplyStackTraceSettings();
            EditorApplication.update += ApplySearchDebounced;
            EditorApplication.update += FlushPendingEntries;
        }

        /// <summary>
        /// ????????? 0.35s ???????????
        /// </summary>
        private void ApplySearchDebounced()
        {
            if (_search == _searchApplied) return;
            if ((EditorApplication.timeSinceStartup - _searchInputLastChangeTime) < 0.35) return;
            _searchApplied = _search;
            _filterDirty = true;
            _tagCountsDirty = true;
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
            EditorApplication.update -= ApplySearchDebounced;
            EditorApplication.update -= FlushPendingEntries;
            Application.logMessageReceivedThreaded -= HandleLogThreaded;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (_mainSplit != null)
            {
                var detailPane = _mainSplit.Q<VisualElement>("detailScroll");
                if (detailPane != null)
                    _detailHeight = Mathf.Max(MinDetailHeight, detailPane.resolvedStyle.height);
            }
            SavePrefs();
        }

        private void HandleLogThreaded(string condition, string stackTrace, LogType type)
        {
            lock (PendingLock)
            {
                PendingEntries.Add(CreateEntry(condition, stackTrace, type));
            }
        }

        private static LogEntry CreateEntry(string condition, string stackTrace, LogType type)
        {
            var entry = new LogEntry
            {
                Condition = condition ?? "",
                StackTrace = stackTrace ?? "",
                LogType = type,
                Count = 1,
                TimeStamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                FrameCount = Application.isPlaying ? Time.frameCount : 0,
                Tags = new List<string>()
            };
            return entry;
        }

        private void AddEntry(string condition, string stackTrace, LogType type)
        {
            var entry = CreateEntry(condition, stackTrace, type);

            if (_collapse && _entries.Count > 0)
            {
                var last = _entries[_entries.Count - 1];
                if (last.IsSameContent(entry))
                {
                    last.Count++;
                    _filterDirty = true;
                    _tagCountsDirty = true;
                    if (_errorPause && (type == LogType.Error || type == LogType.Exception) && EditorApplication.isPlaying)
                        EditorApplication.isPaused = true;
                    RepaintThrottled();
                    return;
                }
            }
            EnhancedConsoleTagLogic.ComputeTags(entry);
            entry.MessageNumber = GetAndAdvanceNextMessageNumber();
            _entries.Add(entry);
            _filterDirty = true;
            _tagCountsDirty = true;
            TrimEntriesToMax();

            if (_errorPause && (type == LogType.Error || type == LogType.Exception))
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.isPaused = true;
            }

            RepaintThrottled();
        }

        private void FlushPendingEntries()
        {
            List<LogEntry> toAdd;
            lock (PendingLock)
            {
                if (PendingEntries.Count == 0) return;
                toAdd = new List<LogEntry>(PendingEntries);
                PendingEntries.Clear();
            }

            foreach (var e in toAdd)
            {
                if (_collapse && _entries.Count > 0)
                {
                    var last = _entries[_entries.Count - 1];
                    if (last.IsSameContent(e))
                    {
                        last.Count++;
                        _filterDirty = true;
                        _tagCountsDirty = true;
                        if (_errorPause && (e.LogType == LogType.Error || e.LogType == LogType.Exception) && EditorApplication.isPlaying)
                            EditorApplication.isPaused = true;
                        continue;
                    }
                }
                EnhancedConsoleTagLogic.ComputeTags(e);
                e.MessageNumber = GetAndAdvanceNextMessageNumber();
                _entries.Add(e);
                _filterDirty = true;
                _tagCountsDirty = true;
                if (_errorPause && (e.LogType == LogType.Error || e.LogType == LogType.Exception))
                {
                    if (EditorApplication.isPlaying)
                        EditorApplication.isPaused = true;
                }
            }
            TrimEntriesToMax();
            RefreshUI();
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
            _clearOnPlay = EditorPrefs.GetBool(PrefClearOnPlay, false);
            _clearOnBuild = EditorPrefs.GetBool(PrefClearOnBuild, false);
            _errorPause = EditorPrefs.GetBool(PrefErrorPause, false);
            _showLog = EditorPrefs.GetBool(PrefShowLog, true);
            _showWarning = EditorPrefs.GetBool(PrefShowWarning, true);
            _showError = EditorPrefs.GetBool(PrefShowError, true);
            _entryLines = EditorPrefs.GetInt(PrefEntryLines, 2);
            _entryLines = Mathf.Clamp(_entryLines, 1, 10);
            _stackTraceLog = (StackTraceLogType)EditorPrefs.GetInt(PrefStackTraceLog, (int)StackTraceLogType.ScriptOnly);
            _stackTraceWarning = (StackTraceLogType)EditorPrefs.GetInt(PrefStackTraceWarning, (int)StackTraceLogType.ScriptOnly);
            _stackTraceError = (StackTraceLogType)EditorPrefs.GetInt(PrefStackTraceError, (int)StackTraceLogType.ScriptOnly);
            _detailHeight = Mathf.Max(MinDetailHeight, EditorPrefs.GetFloat(PrefDetailHeight, 120f));
            _showTimestamp = EditorPrefs.GetBool(PrefShowTimestamp, false);
            _showFrameCount = EditorPrefs.GetBool(PrefShowFrameCount, false);
            _searchRegex = EditorPrefs.GetBool(PrefSearchRegex, false);
            _showMessageNumber = EditorPrefs.GetBool(PrefShowMessageNumber, false);
            _tagsEnabled = EditorPrefs.GetBool(PrefTagsEnabled, true);
            _filterTimeRange = EditorPrefs.GetBool(PrefFilterTimeRange, false);
            _filterNumberRange = EditorPrefs.GetBool(PrefFilterNumberRange, false);
            _filterFrameRange = EditorPrefs.GetBool(PrefFilterFrameRange, false);
            _filterTimeMin = EditorPrefs.GetString(PrefFilterTimeMin, "");
            _filterTimeMax = EditorPrefs.GetString(PrefFilterTimeMax, "");
            _filterNumberMin = EditorPrefs.GetInt(PrefFilterNumberMin, 1);
            _filterNumberMax = EditorPrefs.GetInt(PrefFilterNumberMax, int.MaxValue);
            _filterFrameMin = EditorPrefs.GetInt(PrefFilterFrameMin, 0);
            _filterFrameMax = EditorPrefs.GetInt(PrefFilterFrameMax, int.MaxValue);
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

        private void SavePrefs()
        {
            EditorPrefs.SetBool(PrefCollapse, _collapse);
            EditorPrefs.SetBool(PrefClearOnPlay, _clearOnPlay);
            EditorPrefs.SetBool(PrefClearOnBuild, _clearOnBuild);
            EditorPrefs.SetBool(PrefErrorPause, _errorPause);
            EditorPrefs.SetBool(PrefShowLog, _showLog);
            EditorPrefs.SetBool(PrefShowWarning, _showWarning);
            EditorPrefs.SetBool(PrefShowError, _showError);
            EditorPrefs.SetInt(PrefEntryLines, _entryLines);
            EditorPrefs.SetInt(PrefStackTraceLog, (int)_stackTraceLog);
            EditorPrefs.SetInt(PrefStackTraceWarning, (int)_stackTraceWarning);
            EditorPrefs.SetInt(PrefStackTraceError, (int)_stackTraceError);
            EditorPrefs.SetFloat(PrefDetailHeight, _detailHeight);
            EditorPrefs.SetBool(PrefShowTimestamp, _showTimestamp);
            EditorPrefs.SetBool(PrefShowFrameCount, _showFrameCount);
            EditorPrefs.SetBool(PrefSearchRegex, _searchRegex);
            EditorPrefs.SetBool(PrefShowMessageNumber, _showMessageNumber);
            EditorPrefs.SetBool(PrefTagsEnabled, _tagsEnabled);
            EditorPrefs.SetBool(PrefFilterTimeRange, _filterTimeRange);
            EditorPrefs.SetBool(PrefFilterNumberRange, _filterNumberRange);
            EditorPrefs.SetBool(PrefFilterFrameRange, _filterFrameRange);
            EditorPrefs.SetString(PrefFilterTimeMin, _filterTimeMin);
            EditorPrefs.SetString(PrefFilterTimeMax, _filterTimeMax);
            EditorPrefs.SetInt(PrefFilterNumberMin, _filterNumberMin);
            EditorPrefs.SetInt(PrefFilterNumberMax, _filterNumberMax);
            EditorPrefs.SetInt(PrefFilterFrameMin, _filterFrameMin);
            EditorPrefs.SetInt(PrefFilterFrameMax, _filterFrameMax);
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
            int log = 0, warn = 0, err = 0;
            foreach (var e in _entries)
            {
                int c = e.Count;
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

        private static string StripRichTextTags(string text)
        {
            return text;
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
        private static string GetFirstLines(string text, int maxLines)
        {
            if (string.IsNullOrEmpty(text) || maxLines <= 0) return "";
            if (maxLines == 1)
            {
                int i = text.IndexOf('\n');
                return i >= 0 ? text.Substring(0, i) : text;
            }
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    count++;
                    if (count >= maxLines) return text.Substring(0, i + 1);
                }
            }
            return text;
        }

        /// <summary>
        /// ???Collapse?????????????????????????????
        /// ?? Collapse??????????????????????
        /// </summary>
        private struct FilteredRow { public int entryIndex; public int displayCount; }

        private List<FilteredRow> GetFilteredRows()
        {
            if (!_filterDirty && _cachedFilteredRows != null)
                return _cachedFilteredRows;

            var list = new List<FilteredRow>();
            Regex searchRegex = GetOrCreateSearchRegex();
            if (_collapse)
            {
                var groups = new List<(int firstIndex, int totalCount)>();
                for (int i = 0; i < _entries.Count; i++)
                {
                    var e = _entries[i];
                    if (!EntryMatchesSearch(e, searchRegex)) continue;
                    if (!EntryMatchesTimeRange(e)) continue;
                    if (!EntryMatchesNumberRange(e)) continue;
                    if (!EntryMatchesFrameRange(e)) continue;
                    if (!ShowType(e.LogType)) continue;
                    if (_tagsEnabled && _excludedTags.Count > 0 && e.HasAnyTag(_excludedTags)) continue;
                    if (_tagsEnabled && _selectedTags.Count > 0 && !e.HasAnyTag(_selectedTags)) continue;
                    bool found = false;
                    for (int g = 0; g < groups.Count; g++)
                    {
                        if (_entries[groups[g].firstIndex].IsSameContent(e))
                        {
                            groups[g] = (groups[g].firstIndex, groups[g].totalCount + e.Count);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        groups.Add((i, e.Count));
                }
                foreach (var g in groups)
                    list.Add(new FilteredRow { entryIndex = g.firstIndex, displayCount = g.totalCount });
            }
            else
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    var e = _entries[i];
                    if (!EntryMatchesSearch(e, searchRegex)) continue;
                    if (!EntryMatchesTimeRange(e)) continue;
                    if (!EntryMatchesNumberRange(e)) continue;
                    if (!EntryMatchesFrameRange(e)) continue;
                    if (!ShowType(e.LogType)) continue;
                    if (_tagsEnabled && _excludedTags.Count > 0 && e.HasAnyTag(_excludedTags)) continue;
                    if (_tagsEnabled && _selectedTags.Count > 0 && !e.HasAnyTag(_selectedTags)) continue;
                    for (int k = 0; k < e.Count; k++)
                        list.Add(new FilteredRow { entryIndex = i, displayCount = 1 });
                }
            }
            _cachedFilteredRows = list;
            _filterDirty = false;
            return list;
        }

        /// <summary>
        /// ????????????????????????????????????????????????????0 ???????
        /// </summary>
        private Dictionary<string, int> GetAllTagsFromRowsWithoutTagFilter()
        {
            var allTags = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Regex searchRegex = GetOrCreateSearchRegex();
            if (_collapse)
            {
                var groups = new List<(int firstIndex, int totalCount)>();
                for (int i = 0; i < _entries.Count; i++)
                {
                    var e = _entries[i];
                    if (!EntryMatchesSearch(e, searchRegex)) continue;
                    if (!EntryMatchesTimeRange(e)) continue;
                    if (!EntryMatchesNumberRange(e)) continue;
                    if (!EntryMatchesFrameRange(e)) continue;
                    if (!ShowType(e.LogType)) continue;
                    bool found = false;
                    for (int g = 0; g < groups.Count; g++)
                    {
                        if (_entries[groups[g].firstIndex].IsSameContent(e))
                        {
                            groups[g] = (groups[g].firstIndex, groups[g].totalCount + e.Count);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        groups.Add((i, e.Count));
                }
                foreach (var g in groups)
                {
                    var e = _entries[g.firstIndex];
                    int c = g.totalCount;
                    foreach (var t in e.TagsOrEmpty)
                    {
                        if (string.IsNullOrEmpty(t)) continue;
                        allTags[t] = allTags.TryGetValue(t, out int n) ? n + c : c;
                    }
                }
            }
            else
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    var e = _entries[i];
                    if (!EntryMatchesSearch(e, searchRegex)) continue;
                    if (!EntryMatchesTimeRange(e)) continue;
                    if (!EntryMatchesNumberRange(e)) continue;
                    if (!EntryMatchesFrameRange(e)) continue;
                    if (!ShowType(e.LogType)) continue;
                    int c = e.Count;
                    foreach (var t in e.TagsOrEmpty)
                    {
                        if (string.IsNullOrEmpty(t)) continue;
                        allTags[t] = allTags.TryGetValue(t, out int n) ? n + c : c;
                    }
                }
            }
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
            _filterDirty = true;
            _tagCountsDirty = true;
            RefreshUI();
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
            _cachedFilteredRows = null;
            _cachedTagCounts = null;
            _filterDirty = true;
            _tagCountsDirty = true;
            _selectedIndex = -1;
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
            if (_entries.Count <= MaxEntries) return;
            int removeCount = _entries.Count - MaxEntries;
            _entries.RemoveRange(0, removeCount);
            if (_selectedIndex >= 0)
            {
                _selectedIndex -= removeCount;
                if (_selectedIndex < 0) _selectedIndex = -1;
            }
        }

        /// <summary>
        /// ??????AddEntry ??????Repaint ?? N ms ???? Repaint????????????
        /// </summary>
        private void RepaintThrottled()
        {
            double now = EditorApplication.timeSinceStartup;
            if ((now - _lastRepaintTime) * 1000 >= MinRepaintIntervalMs)
            {
                _lastRepaintTime = now;
                RefreshUI();
            }
            else if (!_repaintScheduled)
            {
                _repaintScheduled = true;
                EditorApplication.delayCall += () =>
                {
                    _repaintScheduled = false;
                    _lastRepaintTime = EditorApplication.timeSinceStartup;
                    RefreshUI();
                };
            }
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

            _logListView = innerRoot.Q<ListView>("logListView");
            _detailField = innerRoot.Q<TextField>("detailLabel");
            if (_detailField != null)             {                 _detailField.isReadOnly = true;                 _detailField.multiline = true;                 _detailField.focusable = true;                 var detailInput = _detailField.Q<VisualElement>(className: "unity-text-input");                 if (detailInput != null)                 {                     detailInput.RegisterCallback<MouseUpEvent>(evt =>                     {                         if (evt.button != 0 || evt.clickCount != 2) return;                         _detailField.schedule.Execute(() => OpenStackLinkFromSelectionOrCaret()).StartingIn(2);                     }, TrickleDown.TrickleDown);                 }                 else                 {                     _detailField.RegisterCallback<MouseUpEvent>(evt =>                     {                         if (evt.button != 0 || evt.clickCount != 2) return;                         _detailField.schedule.Execute(() => OpenStackLinkFromSelectionOrCaret()).StartingIn(2);                     }, TrickleDown.TrickleDown);                 }             }
                BindDetailDoubleClick();
            Debug.Log("EnhancedConsole: detail double-click detected");

            BindToolbar(innerRoot);
            SetupToolbarCountToggleIcons(innerRoot);
            BindSearchBar(innerRoot);
            BindTagBar(innerRoot);
            BindListView();
            SyncTogglesFromState(innerRoot);
            SyncSearchField();
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
                    _filterDirty = true; _tagCountsDirty = true;
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

            var toggleLog = root.Q<Toggle>("toggleLog");
            if (toggleLog != null) { toggleLog.value = _showLog; toggleLog.RegisterValueChangedCallback(ev => { _showLog = ev.newValue; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); }); }
            var toggleWarning = root.Q<Toggle>("toggleWarning");
            if (toggleWarning != null) { toggleWarning.value = _showWarning; toggleWarning.RegisterValueChangedCallback(ev => { _showWarning = ev.newValue; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); }); }
            var toggleError = root.Q<Toggle>("toggleError");
            if (toggleError != null) { toggleError.value = _showError; toggleError.RegisterValueChangedCallback(ev => { _showError = ev.newValue; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); }); }

            var btnMenu = root.Q<Button>("btnMenu");
            if (btnMenu != null) btnMenu.clicked += ShowContextMenu;
        }

        private void BindSearchBar(VisualElement root)
        {
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
                    _filterDirty = true; _tagCountsDirty = true;
                    if (_searchField != null) _searchField.value = "";
                    RefreshUI();
                };
            }

            var btnSearchHistory = root.Q<Button>("btnSearchHistory");
            if (btnSearchHistory != null) btnSearchHistory.clicked += ShowSearchHistoryMenu;

            var btnSearchFilter = root.Q<Button>("btnSearchFilter");
            if (btnSearchFilter != null) btnSearchFilter.clicked += ShowSearchFilterMenu;

            var toggleRegex = root.Q<Toggle>("toggleRegex");
            if (toggleRegex != null)
            {
                toggleRegex.value = _searchRegex;
                toggleRegex.RegisterValueChangedCallback(ev => { _searchRegex = ev.newValue; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
            }

            var btnCopyResult = root.Q<Button>("btnCopyResult");
            if (btnCopyResult != null) btnCopyResult.clicked += CopyMatchedResultsToClipboard;
            var btnCopyRegexMatch = root.Q<Button>("btnCopyRegexMatch");
            if (btnCopyRegexMatch != null) btnCopyRegexMatch.clicked += CopyRegexMatchPartsToClipboard;
        }


        private void BindListView()
        {
            if (_logListView == null) return;
            float lineHeight = 18f;
            _logListView.fixedItemHeight = lineHeight * Mathf.Clamp(_entryLines, 1, 10) + 4;
            _logListView.makeItem = () =>
            {
                var row = new VisualElement { name = "log-row", style = { flexDirection = FlexDirection.Row } };
                row.AddToClassList("log-row");
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
                row.Add(icon);
                row.Add(content);
                row.Add(tags);
                return row;
            };
            _logListView.bindItem = (e, i) =>
            {
                var filtered = GetFilteredRows();
                if (i < 0 || i >= filtered.Count) return;
                var row = filtered[i];
                var entry = _entries[row.entryIndex];
                var img = e.Q<Image>("row-icon");
                if (img != null)
                {
                    var tex = entry.LogType == LogType.Error || entry.LogType == LogType.Exception ? _iconError : entry.LogType == LogType.Warning ? _iconWarning : _iconLog;
                    img.image = tex;
                }
                var content = e.Q<VisualElement>("row-content");
                var msgLabel = content?.Q<Label>("row-message");
                if (msgLabel != null)
                {
                    var prefixParts = new List<string>();
                    if (_showMessageNumber) prefixParts.Add("[" + entry.MessageNumber + "]");
                    if (_showTimestamp && !string.IsNullOrEmpty(entry.TimeStamp)) prefixParts.Add("[" + entry.TimeStamp + "]");
                    if (_showFrameCount) prefixParts.Add("[" + entry.FrameCount + "]");
                    string display = entry.Condition ?? "";
                    if (prefixParts.Count > 0)
                        display = string.Join(" ", prefixParts) + " " + display;
                    if (_collapse && row.displayCount > 1) display = $"[{row.displayCount}] " + display;
                    display = GetFirstLines(display, _entryLines);
                    msgLabel.text = BuildMessageWithHighlight(display);
                }
                var tagsContainer = e.Q<VisualElement>("row-tags");
                if (tagsContainer != null)
                {
                    tagsContainer.Clear();
                    if (_tagsEnabled)
                    {
                        foreach (var tag in entry.TagsOrEmpty)
                        {
                            if (string.IsNullOrEmpty(tag)) continue;
                            var tagLabel = new Label(tag) { name = "tag-" + tag };
                            tagLabel.AddToClassList("log-row-tag");
                            tagLabel.style.backgroundColor = GetTagColor(tag);
                            tagsContainer.Add(tagLabel);
                        }
                    }
                }
            };
            _logListView.selectionChanged += _ =>
            {
                var list = _logListView.selectedIndices.ToList();
                if (list.Count == 0) { _selectedIndex = -1; UpdateDetailPanel(); return; }
                var filtered = GetFilteredRows();
                int idx = list[0];
                if (idx >= 0 && idx < filtered.Count)
                {
                    _selectedIndex = filtered[idx].entryIndex;
                    UpdateDetailPanel();
                }
            };
        }

        private void SyncTogglesFromState(VisualElement root)
        {
            var t = root.Q<Toggle>("toggleCollapse"); if (t != null) t.SetValueWithoutNotify(_collapse);
            t = root.Q<Toggle>("toggleClearOnPlay"); if (t != null) t.SetValueWithoutNotify(_clearOnPlay);
            t = root.Q<Toggle>("toggleClearOnBuild"); if (t != null) t.SetValueWithoutNotify(_clearOnBuild);
            t = root.Q<Toggle>("toggleErrorPause"); if (t != null) t.SetValueWithoutNotify(_errorPause);
            t = root.Q<Toggle>("toggleLog"); if (t != null) t.SetValueWithoutNotify(_showLog);
            t = root.Q<Toggle>("toggleWarning"); if (t != null) t.SetValueWithoutNotify(_showWarning);
            t = root.Q<Toggle>("toggleError"); if (t != null) t.SetValueWithoutNotify(_showError);
            t = root.Q<Toggle>("toggleRegex"); if (t != null) t.SetValueWithoutNotify(_searchRegex);
        }

        private void SyncSearchField()
        {
            if (_searchField != null) _searchField.SetValueWithoutNotify(_search);
        }

        private void RefreshUI()
        {
            if (rootVisualElement == null) return;
            if (rootVisualElement.childCount == 0) BuildUI();
            FlushPendingEntries();
            var root = rootVisualElement.Q<VisualElement>("root");
            if (root == null)
            {
                BuildUI();
                root = rootVisualElement.Q<VisualElement>("root");
                if (root == null) return;
            }

            /* ????? Toggle ???????????????????? 9999?*/
            var (logCount, warnCount, errCount) = CountByTypeUnfiltered();
            int cap = 9999;
            var toggleLog = root.Q<Toggle>("toggleLog");
            if (toggleLog != null) { toggleLog.SetValueWithoutNotify(_showLog); toggleLog.label = (logCount > cap ? cap : logCount).ToString(); }
            var toggleWarning = root.Q<Toggle>("toggleWarning");
            if (toggleWarning != null) { toggleWarning.SetValueWithoutNotify(_showWarning); toggleWarning.label = (warnCount > cap ? cap : warnCount).ToString(); }
            var toggleError = root.Q<Toggle>("toggleError");
            if (toggleError != null) { toggleError.SetValueWithoutNotify(_showError); toggleError.label = (errCount > cap ? cap : errCount).ToString(); }

            var tagBarRow = root.Q<VisualElement>("tagBarRow");
            if (tagBarRow != null) tagBarRow.style.display = _tagsEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            var btnSearchFilter = root.Q<Button>("btnSearchFilter");
            if (btnSearchFilter != null)
            {
                var shortParts = new List<string>();                 var fullParts = new List<string>();                 if (_filterTimeRange) { shortParts.Add("T"); fullParts.Add("Time"); }                 if (_filterNumberRange) { shortParts.Add("N"); fullParts.Add("Number"); }                 if (_filterFrameRange) { shortParts.Add("F"); fullParts.Add("Frame"); }                 btnSearchFilter.text = shortParts.Count > 0 ? "Filter: " + string.Join(",", shortParts) : "Filter";                 if (shortParts.Count > 0)                     btnSearchFilter.AddToClassList("search-filter-active");                 else                     btnSearchFilter.RemoveFromClassList("search-filter-active");                 btnSearchFilter.tooltip = fullParts.Count > 0 ? "Enabled: " + string.Join(", ", fullParts) + " (click to edit or clear)" : "Select filters to enable";
            }
            RebuildTagBar();

            var filtered = GetFilteredRows();
            if (_logListView != null)
            {
                // ???? _entryLines ??????????????Log Entry ?????????????????
                float lineHeight = 18f;
                _logListView.fixedItemHeight = lineHeight * Mathf.Clamp(_entryLines, 1, 10) + 4;

                _logListView.itemsSource = filtered;
                _logListView.Rebuild();
                int selectedFilteredIndex = -1;
                for (int i = 0; i < filtered.Count; i++)
                {
                    if (filtered[i].entryIndex == _selectedIndex) { selectedFilteredIndex = i; break; }
                }
                _logListView.SetSelectionWithoutNotify(selectedFilteredIndex >= 0 ? new[] { selectedFilteredIndex } : new List<int>());
            }
            UpdateDetailPanel();
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
                Debug.Log("EnhancedConsole: detail double-click detected (ClickEvent)");
                _detailField.schedule.Execute(() => OpenStackLinkFromSelectionOrCaret()).StartingIn(1);
            }, TrickleDown.TrickleDown);
            input.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || evt.clickCount != 2) return;
                Debug.Log("EnhancedConsole: detail double-click detected (PointerDownEvent)");
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
            if (prefixParts.Count > 0)
                full = string.Join(" ", prefixParts) + " " + full;
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
                        _filterDirty = true; _tagCountsDirty = true;
                        PushSearchHistory(s);
                        if (_searchField != null) _searchField.value = s;
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
            menu.AddItem(new GUIContent("Time Range"), _filterTimeRange, () => { _filterTimeRange = !_filterTimeRange; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Number Range"), _filterNumberRange, () => { _filterNumberRange = !_filterNumberRange; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Frame Range"), _filterFrameRange, () => { _filterFrameRange = !_filterFrameRange; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
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
            _filterDirty = true;
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
                _filterDirty = true; _tagCountsDirty = true;
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
                _filterDirty = true; _tagCountsDirty = true;
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
                _filterDirty = true; _tagCountsDirty = true;
                SavePrefs(); RefreshUI();
            });
        }

        private void ShowContextMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Show Timestamp"), _showTimestamp, () => { _showTimestamp = !_showTimestamp; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Show Frame Count"), _showFrameCount, () => { _showFrameCount = !_showFrameCount; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Show Message Number"), _showMessageNumber, () => { _showMessageNumber = !_showMessageNumber; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Search/Plain"), !_searchRegex, () => { _searchRegex = false; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Search/Regex"), _searchRegex, () => { _searchRegex = true; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Filter/Time Range"), _filterTimeRange, () => { _filterTimeRange = !_filterTimeRange; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Filter/Number Range"), _filterNumberRange, () => { _filterNumberRange = !_filterNumberRange; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Filter/Frame Range"), _filterFrameRange, () => { _filterFrameRange = !_filterFrameRange; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Filter/Set Time Range..."), false, ShowTimeRangeSettings);
            menu.AddItem(new GUIContent("Filter/Set Number Range..."), false, ShowNumberRangeSettings);
            menu.AddItem(new GUIContent("Filter/Set Frame Range..."), false, ShowFrameRangeSettings);
            menu.AddItem(new GUIContent("Filter/Clear All Filters"), false, ClearAllFilters);
            if (_searchHistory.Count > 0)
            {
                foreach (string item in _searchHistory)
                {
                    string s = item;
                    string display = (s.Length > 50 ? s.Substring(0, 47) + "..." : s).Replace("/", "?M");
                    menu.AddItem(new GUIContent("Search/History/" + display), false, () => { _search = s; _searchApplied = s; _filterDirty = true; _tagCountsDirty = true; PushSearchHistory(s); if (_searchField != null) _searchField.value = s; RefreshUI(); });
                }
                menu.AddItem(new GUIContent("Search/Clear History"), false, () => { _searchHistory.Clear(); SaveSearchHistory(); RefreshUI(); });
            }
            for (int i = 1; i <= 10; i++)
            {
                int n = i;
                menu.AddItem(new GUIContent($"Log Entry/{n} Lines"), _entryLines == n, () => { _entryLines = n; SavePrefs(); RefreshUI(); });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Stack Trace Log/None"), _stackTraceLog == StackTraceLogType.None, () => SetStackTrace(LogType.Log, StackTraceLogType.None));
            menu.AddItem(new GUIContent("Stack Trace Log/ScriptOnly"), _stackTraceLog == StackTraceLogType.ScriptOnly, () => SetStackTrace(LogType.Log, StackTraceLogType.ScriptOnly));
            menu.AddItem(new GUIContent("Stack Trace Log/Full"), _stackTraceLog == StackTraceLogType.Full, () => SetStackTrace(LogType.Log, StackTraceLogType.Full));
            menu.AddItem(new GUIContent("Stack Trace Warning/None"), _stackTraceWarning == StackTraceLogType.None, () => SetStackTrace(LogType.Warning, StackTraceLogType.None));
            menu.AddItem(new GUIContent("Stack Trace Warning/ScriptOnly"), _stackTraceWarning == StackTraceLogType.ScriptOnly, () => SetStackTrace(LogType.Warning, StackTraceLogType.ScriptOnly));
            menu.AddItem(new GUIContent("Stack Trace Warning/Full"), _stackTraceWarning == StackTraceLogType.Full, () => SetStackTrace(LogType.Warning, StackTraceLogType.Full));
            menu.AddItem(new GUIContent("Stack Trace Error/None"), _stackTraceError == StackTraceLogType.None, () => SetStackTrace(LogType.Error, StackTraceLogType.None));
            menu.AddItem(new GUIContent("Stack Trace Error/ScriptOnly"), _stackTraceError == StackTraceLogType.ScriptOnly, () => SetStackTrace(LogType.Error, StackTraceLogType.ScriptOnly));
            menu.AddItem(new GUIContent("Stack Trace Error/Full"), _stackTraceError == StackTraceLogType.Full, () => SetStackTrace(LogType.Error, StackTraceLogType.Full));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Tags/Enable Tags"), _tagsEnabled, () => { _tagsEnabled = !_tagsEnabled; _filterDirty = true; _tagCountsDirty = true; SavePrefs(); RefreshUI(); });
            menu.AddItem(new GUIContent("Tags/Tag Rules..."), false, () => TagRulesWindow.Open(this));
            menu.AddItem(new GUIContent("Tags/Recompute All Tags"), false, RecomputeAllTags);
            menu.AddItem(new GUIContent("Tags/Auto Detect Brackets"), EnhancedConsoleTagLogic.AutoTagBracket, () => { EnhancedConsoleTagLogic.AutoTagBracket = !EnhancedConsoleTagLogic.AutoTagBracket; });
            menu.AddItem(new GUIContent("Tags/Bracket Scope/First Line"), EnhancedConsoleTagLogic.BracketTagFirstLineOnly, () => { EnhancedConsoleTagLogic.BracketTagFirstLineOnly = true; });
            menu.AddItem(new GUIContent("Tags/Bracket Scope/All Lines"), !EnhancedConsoleTagLogic.BracketTagFirstLineOnly, () => { EnhancedConsoleTagLogic.BracketTagFirstLineOnly = false; });
            menu.AddItem(new GUIContent("Tags/Auto Detect Stack Class"), EnhancedConsoleTagLogic.AutoTagStack, () => { EnhancedConsoleTagLogic.AutoTagStack = !EnhancedConsoleTagLogic.AutoTagStack; });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Open Editor Log"), false, OpenEditorLog);
            menu.AddItem(new GUIContent("Open Player Log"), false, OpenPlayerLog);
            menu.ShowAsContext();
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
            if (w != null && w.Length > 0)
                w[0].Clear();
        }
    }
}
