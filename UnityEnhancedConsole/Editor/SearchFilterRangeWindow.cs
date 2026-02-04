using System;
using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 筛选范围设置弹窗（时间/编号/帧数）
    /// </summary>
    public class SearchFilterRangeWindow : EditorWindow
    {
        private string _title;
        private string _minValue;
        private string _maxValue;
        private string _hint;
        private System.Func<string, string, string> _validate; // 返回 null 表示通过，否则返回错误信息
        private System.Action<string, string> _onConfirm;

        public static void Show(string title, string minValue, string maxValue, string hint, System.Func<string, string, string> validate, System.Action<string, string> onConfirm)
        {
            var w = CreateInstance<SearchFilterRangeWindow>();
            w._title = title;
            w._minValue = minValue ?? "";
            w._maxValue = maxValue ?? "";
            w._hint = hint ?? "";
            w._validate = validate;
            w._onConfirm = onConfirm;
            w.titleContent = new GUIContent(title);
            w.minSize = new Vector2(280, 90);
            w.maxSize = new Vector2(400, 120);
            w.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(_title + " (" + _hint + ")", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            _minValue = EditorGUILayout.TextField("起始", _minValue);
            _maxValue = EditorGUILayout.TextField("结束", _maxValue);
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("确定", GUILayout.Width(60)))
            {
                string err = _validate?.Invoke(_minValue, _maxValue);
                if (!string.IsNullOrEmpty(err))
                {
                    EditorUtility.DisplayDialog("输入错误", err, "确定");
                    return;
                }
                _onConfirm?.Invoke(_minValue, _maxValue);
                Close();
            }
            if (GUILayout.Button("取消", GUILayout.Width(60)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
