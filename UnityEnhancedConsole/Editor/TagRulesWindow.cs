using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    public class TagRulesWindow : EditorWindow
    {
        private EnhancedConsoleWindow _console;
        private Vector2 _scroll;
        private List<TagRule> _rules;

        public static void Open(EnhancedConsoleWindow console)
        {
            var w = GetWindow<TagRulesWindow>("标签规则");
            w._console = console;
            w._rules = new List<TagRule>(EnhancedConsoleTagLogic.LoadRules());
            w.minSize = new Vector2(400, 200);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("自定义标签规则（命中则打上对应标签）", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _rules.Count; i++)
            {
                var r = _rules[i];
                EditorGUILayout.BeginHorizontal();
                r.tagName = EditorGUILayout.TextField(r.tagName ?? "", GUILayout.Width(100));
                r.matchType = (int)(TagMatchType)EditorGUILayout.EnumPopup((TagMatchType)r.matchType, GUILayout.Width(80));
                r.matchTarget = (int)(TagMatchTarget)EditorGUILayout.EnumPopup((TagMatchTarget)r.matchTarget, GUILayout.Width(80));
                r.matchContent = EditorGUILayout.TextField(r.matchContent ?? "");
                if (GUILayout.Button("删除", GUILayout.Width(40)))
                {
                    _rules.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加规则"))
            {
                _rules.Add(new TagRule { tagName = "NewTag", matchType = (int)TagMatchType.Contains, matchTarget = (int)TagMatchTarget.ConditionOnly, matchContent = "" });
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("保存", GUILayout.Width(60)))
            {
                EnhancedConsoleTagLogic.SaveRules(_rules);
                if (_console != null)
                    _console.RecomputeAllTags();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
