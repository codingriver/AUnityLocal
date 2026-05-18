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
        private TagFilterSettings _filterSettings;


        public static void Open(EnhancedConsoleWindow console)
        {
            var w = GetWindow<TagRulesWindow>("标签设置");
            w._console = console;
            w._rules = new List<TagRule>(EnhancedConsoleTagLogic.LoadRules());
            w._filterSettings = EnhancedConsoleTagLogic.LoadFilterSettings() ?? new TagFilterSettings();
            w.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            if (_filterSettings == null)
                _filterSettings = EnhancedConsoleTagLogic.LoadFilterSettings() ?? new TagFilterSettings();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // ── 区域 A：自动识别基础设置 ──
            EditorGUILayout.LabelField("自动识别基础设置", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            bool autoBracket = EditorGUILayout.Toggle("启用方括号自动识别", EnhancedConsoleTagLogic.AutoTagBracket);
            if (autoBracket != EnhancedConsoleTagLogic.AutoTagBracket)
            {
                EnhancedConsoleTagLogic.AutoTagBracket = autoBracket;
                NotifySettingsChanged();
            }

            EditorGUI.BeginDisabledGroup(!EnhancedConsoleTagLogic.AutoTagBracket);
            bool firstLineOnly = EnhancedConsoleTagLogic.BracketTagFirstLineOnly;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("识别范围");
            bool newFirstLine = GUILayout.Toggle(firstLineOnly, "首行", EditorStyles.radioButton, GUILayout.Width(60));
            bool newAllLines = GUILayout.Toggle(!firstLineOnly, "所有行", EditorStyles.radioButton, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
            if (newFirstLine && !firstLineOnly)
            {
                EnhancedConsoleTagLogic.BracketTagFirstLineOnly = true;
                NotifySettingsChanged();
            }
            else if (newAllLines && firstLineOnly)
            {
                EnhancedConsoleTagLogic.BracketTagFirstLineOnly = false;
                NotifySettingsChanged();
            }
            EditorGUI.EndDisabledGroup();

            bool autoStack = EditorGUILayout.Toggle("启用堆栈类名自动识别", EnhancedConsoleTagLogic.AutoTagStack);
            if (autoStack != EnhancedConsoleTagLogic.AutoTagStack)
            {
                EnhancedConsoleTagLogic.AutoTagStack = autoStack;
                NotifySettingsChanged();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);

            // ── 区域 B：自动识别过滤策略 ──
            EditorGUILayout.LabelField("自动识别过滤策略", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            bool newIgnorePureNumber = EditorGUILayout.Toggle("忽略纯数字标签", _filterSettings.ignorePureNumber);
            if (newIgnorePureNumber != _filterSettings.ignorePureNumber)
            {
                _filterSettings.ignorePureNumber = newIgnorePureNumber;
                NotifySettingsChanged();
            }

            bool newIgnoreTimeFormat = EditorGUILayout.Toggle("忽略时间格式标签", _filterSettings.ignoreTimeFormat);
            if (newIgnoreTimeFormat != _filterSettings.ignoreTimeFormat)
            {
                _filterSettings.ignoreTimeFormat = newIgnoreTimeFormat;
                NotifySettingsChanged();
            }

            EditorGUILayout.BeginHorizontal();
            int newMinLength = EditorGUILayout.IntField("最小标签长度", _filterSettings.minTagLength, GUILayout.Width(200));
            GUILayout.Label("低于此长度的标签将被忽略", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            if (newMinLength != _filterSettings.minTagLength)
            {
                _filterSettings.minTagLength = Mathf.Max(0, newMinLength);
                NotifySettingsChanged();
            }

            EditorGUILayout.BeginHorizontal();
            int newMaxLength = EditorGUILayout.IntField("最大标签长度", _filterSettings.maxTagLength, GUILayout.Width(200));
            GUILayout.Label("0 = 不限制", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            if (newMaxLength != _filterSettings.maxTagLength)
            {
                _filterSettings.maxTagLength = Mathf.Max(0, newMaxLength);
                NotifySettingsChanged();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("自定义忽略正则（匹配则忽略该标签）", EditorStyles.miniBoldLabel);

            if (_filterSettings.ignorePatterns == null)
                _filterSettings.ignorePatterns = new List<string>();
            for (int i = 0; i < _filterSettings.ignorePatterns.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _filterSettings.ignorePatterns[i] = EditorGUILayout.TextField(_filterSettings.ignorePatterns[i] ?? "");
                if (GUILayout.Button("删除", GUILayout.Width(40)))
                {
                    _filterSettings.ignorePatterns.RemoveAt(i);
                    NotifySettingsChanged();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("添加忽略正则", GUILayout.Width(120)))
            {
                _filterSettings.ignorePatterns.Add("");
                NotifySettingsChanged();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);

            // ── 区域 C：自定义标签规则 ──
            EditorGUILayout.LabelField("自定义标签规则（命中则打上对应标签）", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            for (int i = 0; i < _rules.Count; i++)
            {
                var r = _rules[i];
                EditorGUILayout.BeginHorizontal();

                bool enabled = !r.disabled;
                enabled = EditorGUILayout.Toggle(new GUIContent("", "启用/禁用此规则"), enabled, GUILayout.Width(18));
                r.disabled = !enabled;

                var prevColor = GUI.color;
                if (r.disabled) GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);

                r.tagName = EditorGUILayout.TextField(r.tagName ?? "", GUILayout.Width(85));
                r.matchType = (int)(TagMatchType)EditorGUILayout.EnumPopup((TagMatchType)r.matchType, GUILayout.Width(72));
                r.matchTarget = (int)(TagMatchTarget)EditorGUILayout.EnumPopup((TagMatchTarget)r.matchTarget, GUILayout.Width(72));
                r.matchContent = EditorGUILayout.TextField(r.matchContent ?? "");

                GUI.color = prevColor;

                if (GUILayout.Button("删除", GUILayout.Width(40)))
                {
                    _rules.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加规则"))
            {
                _rules.Add(new TagRule { tagName = "NewTag", matchType = (int)TagMatchType.Contains, matchTarget = (int)TagMatchTarget.ConditionOnly, matchContent = "" });
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            // ── 底部保存按钮 ──
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("保存", GUILayout.Width(80), GUILayout.Height(24)))
            {
                SaveAll();
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool _pendingRecompute;

        private void NotifySettingsChanged()
        {
            _pendingRecompute = true;
        }

        private void SaveAll()
        {
            EnhancedConsoleTagLogic.SaveFilterSettings(_filterSettings);
            EnhancedConsoleTagLogic.SaveRules(_rules);
            if (_console != null)
                _console.RecomputeAllTags();
            _pendingRecompute = false;
            Repaint();
        }

        private void OnDestroy()
        {
            if (_pendingRecompute)
            {
                SaveAll();
            }
        }
    }
}
