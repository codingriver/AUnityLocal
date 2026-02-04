using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 字符串处理窗口：多行文本编辑，支持行排序、去重、去除空白/换行等操作。
    /// </summary>
    public class StringProcessorWindow : EditorWindow
    {
        private string _text = "";
        private Vector2 _scroll;
        private bool _showLineNumbers;
        private const string PrefShowLineNumbers = "EnhancedConsole.StringProcessor.ShowLineNumbers";

        [MenuItem("Window/General/字符串处理", false, 2100)]
        [MenuItem("Tools/Enhanced Console/字符串处理窗口", false, 150)]
        public static void Open()
        {
            var w = GetWindow<StringProcessorWindow>("字符串处理");
            w.minSize = new Vector2(400, 300);
        }

        /// <summary>
        /// 打开窗口并填入指定文本（供 Enhanced Console 导出等调用）。
        /// </summary>
        public static void OpenWithText(string text)
        {
            var w = GetWindow<StringProcessorWindow>("字符串处理");
            w.minSize = new Vector2(400, 300);
            w._text = text ?? "";
            w.Focus();
        }

        private void OnEnable()
        {
            _showLineNumbers = EditorPrefs.GetBool(PrefShowLineNumbers, false);
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawContent();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                _text = "";
                GUI.FocusControl(null); // 移除焦点，避免有选区时控件仍显示旧内容
            }
            if (GUILayout.Button("从剪贴板粘贴", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                try
                {
                    _text = EditorGUIUtility.systemCopyBuffer ?? "";
                    GUI.FocusControl(null);
                }
                catch { }
            }
            if (GUILayout.Button("复制全部", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                try
                {
                    EditorGUIUtility.systemCopyBuffer = _text ?? "";
                }
                catch { }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("升序", EditorStyles.toolbarButton, GUILayout.Width(36)))
            {
                ApplySort(ascending: true);
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("降序", EditorStyles.toolbarButton, GUILayout.Width(36)))
            {
                ApplySort(ascending: false);
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("去重", EditorStyles.toolbarButton, GUILayout.Width(36)))
            {
                ApplyDeduplicate();
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("去首尾空白", EditorStyles.toolbarButton, GUILayout.Width(72)))
            {
                ApplyTrimLines();
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("换行→空格", EditorStyles.toolbarButton, GUILayout.Width(72)))
            {
                ApplyNewlineToSpace();
                GUI.FocusControl(null);
            }

            GUILayout.Space(8);
            bool newLineNum = GUILayout.Toggle(_showLineNumbers, "行号", EditorStyles.toolbarButton, GUILayout.Width(36));
            if (newLineNum != _showLineNumbers)
            {
                _showLineNumbers = newLineNum;
                EditorPrefs.SetBool(PrefShowLineNumbers, _showLineNumbers);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawContent()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            float lineHeight = EditorStyles.textArea.lineHeight;
            int lineCount = Mathf.Max(1, (_text ?? "").Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length);
            float contentHeight = Mathf.Min(lineCount * lineHeight, 10000f);
            contentHeight = Mathf.Max(contentHeight, position.height - 60f);

            EditorGUILayout.BeginHorizontal();
            if (_showLineNumbers)
            {
                var sb = new StringBuilder();
                for (int i = 1; i <= lineCount; i++)
                {
                    if (i > 1) sb.Append('\n');
                    sb.Append(i);
                }
                GUILayout.Label(sb.ToString(), EditorStyles.textArea, GUILayout.Width(40), GUILayout.MinHeight(contentHeight));
            }
            EditorGUI.BeginChangeCheck();
            _text = EditorGUILayout.TextArea(_text ?? "", GUILayout.MinHeight(contentHeight));
            if (EditorGUI.EndChangeCheck()) { }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private void ApplySort(bool ascending)
        {
            if (string.IsNullOrEmpty(_text)) return;
            var lines = _text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var list = new List<string>(lines);
            list.Sort(StringComparer.Ordinal);
            if (!ascending)
                list.Reverse();
            _text = string.Join("\n", list);
        }

        private void ApplyDeduplicate()
        {
            if (string.IsNullOrEmpty(_text)) return;
            var lines = _text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<string>();
            foreach (string line in lines)
            {
                if (seen.Add(line))
                    list.Add(line);
            }
            _text = string.Join("\n", list);
        }

        private void ApplyTrimLines()
        {
            if (string.IsNullOrEmpty(_text)) return;
            var lines = _text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].Trim();
            _text = string.Join("\n", lines);
        }

        private void ApplyNewlineToSpace()
        {
            if (string.IsNullOrEmpty(_text)) return;
            _text = _text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
