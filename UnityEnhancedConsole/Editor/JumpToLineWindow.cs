using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    public class JumpToLineWindow : EditorWindow
    {
        private EnhancedConsoleWindow _console;
        private string _input = "";
        private string _error;

        public static void Open(EnhancedConsoleWindow console)
        {
            var w = CreateInstance<JumpToLineWindow>();
            w._console = console;
            w.titleContent = new GUIContent("跳转到日志编号");
            w.minSize = new Vector2(130, 90);
            w.maxSize = new Vector2(160, 110);
            w.ShowUtility();
        }

        private void OnEnable()
        {
            _input = "";
            _error = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("输入日志编号（MessageNumber）", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            GUI.SetNextControlName("JumpInput");
            _input = EditorGUILayout.TextField(_input, GUILayout.Height(22));
            EditorGUI.FocusTextInControl("JumpInput");

            if (!string.IsNullOrEmpty(_error))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("跳转", GUILayout.Width(60), GUILayout.Height(22)))
            {
                TryJump();
            }
            if (GUILayout.Button("取消", GUILayout.Width(60), GUILayout.Height(22)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();

            HandleKeyboard();
        }

        private void HandleKeyboard()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                e.Use();
                TryJump();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                e.Use();
                Close();
            }
        }

        private void TryJump()
        {
            if (!int.TryParse(_input, out var num) || num <= 0)
            {
                _error = "请输入有效的正整数编号";
                return;
            }

            if (_console == null)
            {
                _error = "控制台窗口已关闭";
                return;
            }

            Close();
            _console.JumpToMessageNumber(num);
        }
    }
}
