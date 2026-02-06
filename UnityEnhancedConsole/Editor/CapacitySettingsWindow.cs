using System;
using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    public class CapacitySettingsWindow : EditorWindow
    {
        private int _maxEntries;
        private int _maxLoadEntries;
        private Action<int, int> _onConfirm;

        public static void Show(int maxEntries, int maxLoadEntries, Action<int, int> onConfirm)
        {
            var w = CreateInstance<CapacitySettingsWindow>();
            w._maxEntries = maxEntries;
            w._maxLoadEntries = maxLoadEntries;
            w._onConfirm = onConfirm;
            w.titleContent = new GUIContent("Capacity Settings");
            w.minSize = new Vector2(360, 140);
            w.maxSize = new Vector2(520, 180);
            w.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Capacity Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("Auto discard old logs: keep latest N entries (Max Entries). Loading uses Max Load Entries (>= Max Entries).", MessageType.Info);
            EditorGUILayout.Space(4);
            _maxEntries = EditorGUILayout.IntField("Max Entries", _maxEntries);
            _maxLoadEntries = EditorGUILayout.IntField("Max Load Entries", _maxLoadEntries);

            if (_maxEntries < 100) _maxEntries = 100;
            if (_maxLoadEntries < _maxEntries) _maxLoadEntries = _maxEntries;

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", GUILayout.Width(80)))
            {
                _onConfirm?.Invoke(_maxEntries, _maxLoadEntries);
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
