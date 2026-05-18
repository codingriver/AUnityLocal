using System;
using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    public class EnhancedConsoleSettingsWindow : EditorWindow
    {
        private Vector2 _scroll;
        private bool _foldDisplay = true;
        private bool _foldLogFilter = true;
        private bool _foldBehavior = true;
        private bool _foldCapacity = true;
        private bool _foldRangeFilter = true;
        private bool _foldSearch = true;
        private bool _foldStackTrace = true;
        private bool _foldTags = true;

        private int _entryLines;
        private bool _showTimestamp;
        private bool _showFrameCount;
        private bool _showMessageNumber;
        private bool _showStackTrace;

        private bool _showLog;
        private bool _showWarning;
        private bool _showError;

        private bool _collapse;
        private bool _collapseGlobal;
        private bool _clearOnPlay;
        private bool _clearOnBuild;
        private bool _errorPause;
        private bool _viewLocked;

        private int _maxEntries;
        private int _maxLoadEntries;

        private bool _filterTimeRange;
        private string _filterTimeMin;
        private string _filterTimeMax;
        private bool _filterNumberRange;
        private int _filterNumberMin;
        private int _filterNumberMax;
        private bool _filterFrameRange;
        private int _filterFrameMin;
        private int _filterFrameMax;

        private bool _searchRegex;

        private StackTraceLogType _stackTraceLog;
        private StackTraceLogType _stackTraceWarning;
        private StackTraceLogType _stackTraceError;

        private bool _tagsEnabled;
        private EnhancedConsoleWindow.TagSortMode _tagSortMode;
        private bool _tagSortDesc;

        [MenuItem("Window/Enhanced Console Settings")]
        public static void Open()
        {
            var w = GetWindow<EnhancedConsoleSettingsWindow>("Enhanced Console Settings");
            w.minSize = new Vector2(420, 500);
            w.LoadFromPrefs();
        }

        public static void OpenAsUtility()
        {
            var w = CreateInstance<EnhancedConsoleSettingsWindow>();
            w.titleContent = new GUIContent("Enhanced Console Settings");
            w.minSize = new Vector2(420, 500);
            w.LoadFromPrefs();
            w.ShowUtility();
        }

        private void OnEnable()
        {
            LoadFromPrefs();
        }

        private void LoadFromPrefs()
        {
            _entryLines = Mathf.Clamp(EditorPrefs.GetInt(EnhancedConsoleWindow.PrefEntryLines, 2), 1, 10);
            _showTimestamp = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefShowTimestamp, false);
            _showFrameCount = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefShowFrameCount, false);
            _showMessageNumber = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefShowMessageNumber, false);
            _showStackTrace = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefShowStackTrace, true);

            _showLog = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefShowLog, true);
            _showWarning = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefShowWarning, true);
            _showError = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefShowError, true);

            _collapse = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefCollapse, false);
            _collapseGlobal = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefCollapseGlobal, true);
            _clearOnPlay = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefClearOnPlay, false);
            _clearOnBuild = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefClearOnBuild, false);
            _errorPause = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefErrorPause, false);
            _viewLocked = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefViewLocked, false);

            _maxEntries = Mathf.Max(100, EditorPrefs.GetInt(EnhancedConsoleWindow.PrefMaxEntries, 20000));
            _maxLoadEntries = Mathf.Max(_maxEntries, EditorPrefs.GetInt(EnhancedConsoleWindow.PrefMaxLoadEntries, 50000));

            _filterTimeRange = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefFilterTimeRange, false);
            _filterTimeMin = EditorPrefs.GetString(EnhancedConsoleWindow.PrefFilterTimeMin, "");
            _filterTimeMax = EditorPrefs.GetString(EnhancedConsoleWindow.PrefFilterTimeMax, "");
            _filterNumberRange = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefFilterNumberRange, false);
            _filterNumberMin = EditorPrefs.GetInt(EnhancedConsoleWindow.PrefFilterNumberMin, 1);
            _filterNumberMax = EditorPrefs.GetInt(EnhancedConsoleWindow.PrefFilterNumberMax, int.MaxValue);
            _filterFrameRange = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefFilterFrameRange, false);
            _filterFrameMin = EditorPrefs.GetInt(EnhancedConsoleWindow.PrefFilterFrameMin, 0);
            _filterFrameMax = EditorPrefs.GetInt(EnhancedConsoleWindow.PrefFilterFrameMax, int.MaxValue);

            _searchRegex = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefSearchRegex, false);

            _stackTraceLog = (StackTraceLogType)EditorPrefs.GetInt(EnhancedConsoleWindow.PrefStackTraceLog, (int)StackTraceLogType.ScriptOnly);
            _stackTraceWarning = (StackTraceLogType)EditorPrefs.GetInt(EnhancedConsoleWindow.PrefStackTraceWarning, (int)StackTraceLogType.ScriptOnly);
            _stackTraceError = (StackTraceLogType)EditorPrefs.GetInt(EnhancedConsoleWindow.PrefStackTraceError, (int)StackTraceLogType.ScriptOnly);

            _tagsEnabled = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefTagsEnabled, true);
            _tagSortMode = (EnhancedConsoleWindow.TagSortMode)EditorPrefs.GetInt(EnhancedConsoleWindow.PrefTagSortMode, (int)EnhancedConsoleWindow.TagSortMode.Name);
            _tagSortDesc = EditorPrefs.GetBool(EnhancedConsoleWindow.PrefTagSortDesc, false);
        }

        private void SaveAll()
        {
            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefEntryLines, _entryLines);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefShowTimestamp, _showTimestamp);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefShowFrameCount, _showFrameCount);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefShowMessageNumber, _showMessageNumber);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefShowStackTrace, _showStackTrace);

            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefShowLog, _showLog);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefShowWarning, _showWarning);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefShowError, _showError);

            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefCollapse, _collapse);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefCollapseGlobal, _collapseGlobal);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefClearOnPlay, _clearOnPlay);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefClearOnBuild, _clearOnBuild);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefErrorPause, _errorPause);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefViewLocked, _viewLocked);

            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefMaxEntries, _maxEntries);
            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefMaxLoadEntries, _maxLoadEntries);

            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefFilterTimeRange, _filterTimeRange);
            EditorPrefs.SetString(EnhancedConsoleWindow.PrefFilterTimeMin, _filterTimeMin ?? "");
            EditorPrefs.SetString(EnhancedConsoleWindow.PrefFilterTimeMax, _filterTimeMax ?? "");
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefFilterNumberRange, _filterNumberRange);
            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefFilterNumberMin, _filterNumberMin);
            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefFilterNumberMax, _filterNumberMax);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefFilterFrameRange, _filterFrameRange);
            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefFilterFrameMin, _filterFrameMin);
            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefFilterFrameMax, _filterFrameMax);

            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefSearchRegex, _searchRegex);

            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefStackTraceLog, (int)_stackTraceLog);
            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefStackTraceWarning, (int)_stackTraceWarning);
            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefStackTraceError, (int)_stackTraceError);

            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefTagsEnabled, _tagsEnabled);
            EditorPrefs.SetInt(EnhancedConsoleWindow.PrefTagSortMode, (int)_tagSortMode);
            EditorPrefs.SetBool(EnhancedConsoleWindow.PrefTagSortDesc, _tagSortDesc);

            EnhancedConsoleWindow.NotifySettingsChanged();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Enhanced Console Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // ── Display ──
            _foldDisplay = EditorGUILayout.BeginFoldoutHeaderGroup(_foldDisplay, "Display Options");
            if (_foldDisplay)
            {
                EditorGUI.indentLevel++;
                bool newShowTimestamp = EditorGUILayout.Toggle("Show Timestamp", _showTimestamp);
                bool newShowFrame = EditorGUILayout.Toggle("Show Frame Count", _showFrameCount);
                bool newShowNumber = EditorGUILayout.Toggle("Show Message Number", _showMessageNumber);
                bool newShowStack = EditorGUILayout.Toggle("Show Stack Trace", _showStackTrace);
                int newEntryLines = EditorGUILayout.IntSlider("Entry Lines", _entryLines, 1, 10);
                EditorGUI.indentLevel--;

                if (newShowTimestamp != _showTimestamp || newShowFrame != _showFrameCount ||
                    newShowNumber != _showMessageNumber || newShowStack != _showStackTrace ||
                    newEntryLines != _entryLines)
                {
                    _showTimestamp = newShowTimestamp;
                    _showFrameCount = newShowFrame;
                    _showMessageNumber = newShowNumber;
                    _showStackTrace = newShowStack;
                    _entryLines = newEntryLines;
                    SaveAll();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);

            // ── Log Type Filter ──
            _foldLogFilter = EditorGUILayout.BeginFoldoutHeaderGroup(_foldLogFilter, "Log Type Filter");
            if (_foldLogFilter)
            {
                EditorGUI.indentLevel++;
                bool newShowLog = EditorGUILayout.Toggle("Show Log", _showLog);
                bool newShowWarning = EditorGUILayout.Toggle("Show Warning", _showWarning);
                bool newShowError = EditorGUILayout.Toggle("Show Error", _showError);
                EditorGUI.indentLevel--;

                if (newShowLog != _showLog || newShowWarning != _showWarning || newShowError != _showError)
                {
                    _showLog = newShowLog;
                    _showWarning = newShowWarning;
                    _showError = newShowError;
                    SaveAll();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);

            // ── Behavior ──
            _foldBehavior = EditorGUILayout.BeginFoldoutHeaderGroup(_foldBehavior, "Behavior");
            if (_foldBehavior)
            {
                EditorGUI.indentLevel++;
                bool newCollapse = EditorGUILayout.Toggle("Collapse", _collapse);
                bool newCollapseGlobal = EditorGUILayout.Toggle("Collapse Global", _collapseGlobal);
                bool newClearOnPlay = EditorGUILayout.Toggle("Clear On Play", _clearOnPlay);
                bool newClearOnBuild = EditorGUILayout.Toggle("Clear On Build", _clearOnBuild);
                bool newErrorPause = EditorGUILayout.Toggle("Error Pause", _errorPause);
                bool newViewLocked = EditorGUILayout.Toggle("View Locked", _viewLocked);
                EditorGUI.indentLevel--;

                if (newCollapse != _collapse || newCollapseGlobal != _collapseGlobal ||
                    newClearOnPlay != _clearOnPlay || newClearOnBuild != _clearOnBuild ||
                    newErrorPause != _errorPause || newViewLocked != _viewLocked)
                {
                    _collapse = newCollapse;
                    _collapseGlobal = newCollapseGlobal;
                    _clearOnPlay = newClearOnPlay;
                    _clearOnBuild = newClearOnBuild;
                    _errorPause = newErrorPause;
                    _viewLocked = newViewLocked;
                    SaveAll();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);

            // ── Capacity ──
            _foldCapacity = EditorGUILayout.BeginFoldoutHeaderGroup(_foldCapacity, "Capacity");
            if (_foldCapacity)
            {
                EditorGUI.indentLevel++;
                int newMaxEntries = EditorGUILayout.IntField("Max Entries", _maxEntries);
                int newMaxLoadEntries = EditorGUILayout.IntField("Max Load Entries", _maxLoadEntries);
                if (newMaxEntries < 100) newMaxEntries = 100;
                if (newMaxLoadEntries < newMaxEntries) newMaxLoadEntries = newMaxEntries;

                EditorGUILayout.Space(4);
                if (GUILayout.Button("Restore Defaults", GUILayout.Width(140)))
                {
                    newMaxEntries = 20000;
                    newMaxLoadEntries = 50000;
                }
                EditorGUI.indentLevel--;

                if (newMaxEntries != _maxEntries || newMaxLoadEntries != _maxLoadEntries)
                {
                    _maxEntries = newMaxEntries;
                    _maxLoadEntries = newMaxLoadEntries;
                    SaveAll();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);

            // ── Range Filter ──
            _foldRangeFilter = EditorGUILayout.BeginFoldoutHeaderGroup(_foldRangeFilter, "Range Filters");
            if (_foldRangeFilter)
            {
                EditorGUI.indentLevel++;

                // Time
                bool newTimeRange = EditorGUILayout.Toggle("Time Range", _filterTimeRange);
                EditorGUI.BeginDisabledGroup(!newTimeRange);
                string newTimeMin = EditorGUILayout.TextField("  Time Min", _filterTimeMin);
                string newTimeMax = EditorGUILayout.TextField("  Time Max", _filterTimeMax);
                EditorGUI.EndDisabledGroup();
                if (newTimeRange != _filterTimeRange || newTimeMin != _filterTimeMin || newTimeMax != _filterTimeMax)
                {
                    _filterTimeRange = newTimeRange;
                    _filterTimeMin = newTimeMin ?? "";
                    _filterTimeMax = newTimeMax ?? "";
                    SaveAll();
                }
                EditorGUILayout.Space(4);

                // Number
                bool newNumberRange = EditorGUILayout.Toggle("Number Range", _filterNumberRange);
                EditorGUI.BeginDisabledGroup(!newNumberRange);
                int newNumberMin = EditorGUILayout.IntField("  Number Min", _filterNumberMin);
                int newNumberMax = EditorGUILayout.IntField("  Number Max", _filterNumberMax);
                EditorGUI.EndDisabledGroup();
                if (newNumberRange != _filterNumberRange || newNumberMin != _filterNumberMin || newNumberMax != _filterNumberMax)
                {
                    _filterNumberRange = newNumberRange;
                    _filterNumberMin = newNumberMin;
                    _filterNumberMax = newNumberMax;
                    SaveAll();
                }
                EditorGUILayout.Space(4);

                // Frame
                bool newFrameRange = EditorGUILayout.Toggle("Frame Range", _filterFrameRange);
                EditorGUI.BeginDisabledGroup(!newFrameRange);
                int newFrameMin = EditorGUILayout.IntField("  Frame Min", _filterFrameMin);
                int newFrameMax = EditorGUILayout.IntField("  Frame Max", _filterFrameMax);
                EditorGUI.EndDisabledGroup();
                if (newFrameRange != _filterFrameRange || newFrameMin != _filterFrameMin || newFrameMax != _filterFrameMax)
                {
                    _filterFrameRange = newFrameRange;
                    _filterFrameMin = newFrameMin;
                    _filterFrameMax = newFrameMax;
                    SaveAll();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);

            // ── Search ──
            _foldSearch = EditorGUILayout.BeginFoldoutHeaderGroup(_foldSearch, "Search");
            if (_foldSearch)
            {
                EditorGUI.indentLevel++;
                bool newRegex = EditorGUILayout.Toggle("Use Regex", _searchRegex);
                EditorGUI.indentLevel--;
                if (newRegex != _searchRegex)
                {
                    _searchRegex = newRegex;
                    SaveAll();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);

            // ── Stack Trace ──
            _foldStackTrace = EditorGUILayout.BeginFoldoutHeaderGroup(_foldStackTrace, "Stack Trace Settings");
            if (_foldStackTrace)
            {
                EditorGUI.indentLevel++;
                var newStackLog = (StackTraceLogType)EditorGUILayout.EnumPopup("Log", _stackTraceLog);
                var newStackWarning = (StackTraceLogType)EditorGUILayout.EnumPopup("Warning", _stackTraceWarning);
                var newStackError = (StackTraceLogType)EditorGUILayout.EnumPopup("Error", _stackTraceError);
                EditorGUI.indentLevel--;
                if (newStackLog != _stackTraceLog || newStackWarning != _stackTraceWarning || newStackError != _stackTraceError)
                {
                    _stackTraceLog = newStackLog;
                    _stackTraceWarning = newStackWarning;
                    _stackTraceError = newStackError;
                    SaveAll();
                    ApplyStackTraceToUnity();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);

            // ── Tags ──
            _foldTags = EditorGUILayout.BeginFoldoutHeaderGroup(_foldTags, "Tags");
            if (_foldTags)
            {
                EditorGUI.indentLevel++;
                bool newTagsEnabled = EditorGUILayout.Toggle("Enable Tags", _tagsEnabled);
                var newSortMode = (EnhancedConsoleWindow.TagSortMode)EditorGUILayout.EnumPopup("Sort Mode", _tagSortMode);
                bool newSortDesc = EditorGUILayout.Toggle("Sort Descending", _tagSortDesc);
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Open Tag Rules...", GUILayout.Width(160)))
                {
                    var consoles = Resources.FindObjectsOfTypeAll<EnhancedConsoleWindow>();
                    TagRulesWindow.Open(consoles.Length > 0 ? consoles[0] : null);
                }
                EditorGUI.indentLevel--;
                if (newTagsEnabled != _tagsEnabled || newSortMode != _tagSortMode || newSortDesc != _tagSortDesc)
                {
                    _tagsEnabled = newTagsEnabled;
                    _tagSortMode = newSortMode;
                    _tagSortDesc = newSortDesc;
                    SaveAll();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.EndScrollView();
        }

        private static void ApplyStackTraceToUnity()
        {
            int log = EditorPrefs.GetInt(EnhancedConsoleWindow.PrefStackTraceLog, (int)StackTraceLogType.ScriptOnly);
            int warning = EditorPrefs.GetInt(EnhancedConsoleWindow.PrefStackTraceWarning, (int)StackTraceLogType.ScriptOnly);
            int error = EditorPrefs.GetInt(EnhancedConsoleWindow.PrefStackTraceError, (int)StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Log, (UnityEngine.StackTraceLogType)log);
            Application.SetStackTraceLogType(LogType.Warning, (UnityEngine.StackTraceLogType)warning);
            Application.SetStackTraceLogType(LogType.Error, (UnityEngine.StackTraceLogType)error);
            Application.SetStackTraceLogType(LogType.Assert, (UnityEngine.StackTraceLogType)error);
            Application.SetStackTraceLogType(LogType.Exception, (UnityEngine.StackTraceLogType)error);
        }
    }
}
