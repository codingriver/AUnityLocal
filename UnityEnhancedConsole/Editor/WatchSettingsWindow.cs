using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    public class WatchSettingsWindow : EditorWindow
    {
        private int _historyDepth;
        private int _maxEntries;
        private float _autoUpdateInterval;
        private float _flashDuration;
        private float _graphTimeRange;
        private bool _showTrendArrow;
        private bool _persistToFile;

        private const string PrefFlashDuration = "EnhancedConsole_WatchFlashDuration";
        private const string PrefGraphTimeRange = "EnhancedConsole_WatchGraphTimeRange";
        private const string PrefShowTrendArrow = "EnhancedConsole_WatchShowTrendArrow";
        private const string PrefPersistToFile = "EnhancedConsole_WatchPersistToFile";

        public static void Show()
        {
            var window = GetWindow<WatchSettingsWindow>(true, "Watch Settings", true);
            window.minSize = new Vector2(320, 260);
            window.maxSize = new Vector2(400, 300);
            window.LoadSettings();
            window.ShowUtility();
        }

        private void LoadSettings()
        {
            _historyDepth = WatchManager.HistoryDepth;
            _maxEntries = EditorPrefs.GetInt("EnhancedConsole_WatchMaxEntries", WatchManager.MaxEntries);
            _autoUpdateInterval = EditorPrefs.GetFloat("EnhancedConsole_WatchAutoUpdateInterval", 0f);
            _flashDuration = EditorPrefs.GetFloat(PrefFlashDuration, 0.3f);
            _graphTimeRange = EditorPrefs.GetFloat(PrefGraphTimeRange, 5f);
            _showTrendArrow = EditorPrefs.GetBool(PrefShowTrendArrow, true);
            _persistToFile = EditorPrefs.GetBool(PrefPersistToFile, false);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Watch Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _historyDepth = EditorGUILayout.IntField("History Depth", _historyDepth);
            _historyDepth = Mathf.Clamp(_historyDepth, 1, WatchManager.MaxHistoryDepth);

            _maxEntries = EditorGUILayout.IntField("Max Entries", _maxEntries);
            _maxEntries = Mathf.Clamp(_maxEntries, 1, 100000);

            _autoUpdateInterval = EditorGUILayout.FloatField("Auto Update Interval (s)", _autoUpdateInterval);
            _autoUpdateInterval = Mathf.Max(0f, _autoUpdateInterval);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);

            _flashDuration = EditorGUILayout.FloatField("Flash Duration (s)", _flashDuration);
            _flashDuration = Mathf.Clamp(_flashDuration, 0f, 5f);

            _graphTimeRange = EditorGUILayout.FloatField("Graph Time Range (s)", _graphTimeRange);
            _graphTimeRange = Mathf.Clamp(_graphTimeRange, 1f, 300f);

            _showTrendArrow = EditorGUILayout.Toggle("Show Trend Arrow", _showTrendArrow);
            _persistToFile = EditorGUILayout.Toggle("Persist to File", _persistToFile);

            EditorGUILayout.Space(12);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Apply", GUILayout.Width(80)))
            {
                WatchManager.SetHistoryDepth(_historyDepth);
                WatchManager.SetMaxEntries(_maxEntries);
                WatchManager.SetAutoUpdateInterval(_autoUpdateInterval);
                EditorPrefs.SetFloat(PrefFlashDuration, _flashDuration);
                EditorPrefs.SetFloat(PrefGraphTimeRange, _graphTimeRange);
                EditorPrefs.SetBool(PrefShowTrendArrow, _showTrendArrow);
                EditorPrefs.SetBool(PrefPersistToFile, _persistToFile);
                Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
