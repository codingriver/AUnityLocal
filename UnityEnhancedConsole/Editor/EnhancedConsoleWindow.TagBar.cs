using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEnhancedConsole
{
    public partial class EnhancedConsoleWindow
    {
        private void BindTagBar(VisualElement root)
        {
            var btnTagClear = root.Q<Button>("btnTagClear");
            if (btnTagClear != null)
            {
                btnTagClear.text = "Clear+";
                btnTagClear.tooltip = "清空包含标签（默认：Clear+）";
                btnTagClear.clicked += () =>
                {
                    _selectedTags.Clear();
                    _filterDirty = true; _tagCountsDirty = true;
                    SavePrefs();
                    RefreshUI();
                };
            }

            var btnTagClearMenu = root.Q<Button>("btnTagClearMenu");
            if (btnTagClearMenu != null)
            {
                btnTagClearMenu.tooltip = "选择清空方式";
                btnTagClearMenu.clicked += () =>
                {
                    var menu = new UnityEditor.GenericMenu();
                    menu.AddItem(new GUIContent("Clear+", "清空包含标签"), false, () =>
                    {
                        _selectedTags.Clear();
                        _filterDirty = true; _tagCountsDirty = true;
                        SavePrefs();
                        RefreshUI();
                    });
                    menu.AddItem(new GUIContent("Clear-", "清空排除标签"), false, () =>
                    {
                        _excludedTags.Clear();
                        _filterDirty = true; _tagCountsDirty = true;
                        SavePrefs();
                        RefreshUI();
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Clear All", "清空包含与排除标签"), false, () =>
                    {
                        _selectedTags.Clear();
                        _excludedTags.Clear();
                        _filterDirty = true; _tagCountsDirty = true;
                        SavePrefs();
                        RefreshUI();
                    });
                    menu.ShowAsContext();
                };
            }

            var toggleTagsEnabled = root.Q<Toggle>("toggleTagsEnabled");
            if (toggleTagsEnabled != null)
            {
                toggleTagsEnabled.tooltip = "启用/禁用标签过滤";
                toggleTagsEnabled.RegisterValueChangedCallback(ev =>
                {
                    _tagsEnabled = ev.newValue;
                    _filterDirty = true; _tagCountsDirty = true;
                    SavePrefs();
                    RefreshUI();
                });
            }

            var btnTagRules = root.Q<Button>("btnTagRules");
            if (btnTagRules != null)
            {
                btnTagRules.tooltip = "编辑标签规则";
                btnTagRules.clicked += () => TagRulesWindow.Open(this);
            }

            var btnTagRecompute = root.Q<Button>("btnTagRecompute");
            if (btnTagRecompute != null)
            {
                btnTagRecompute.tooltip = "重新计算所有标签";
                btnTagRecompute.clicked += RecomputeAllTags;
            }

            var toggleAutoBracket = root.Q<Toggle>("toggleAutoBracket");
            if (toggleAutoBracket != null)
            {
                toggleAutoBracket.tooltip = "自动识别方括号标签";
                toggleAutoBracket.RegisterValueChangedCallback(ev =>
                {
                    EnhancedConsoleTagLogic.AutoTagBracket = ev.newValue;
                    UpdateTagSettingsControls();
                    RecomputeAllTags();
                });
            }

            var toggleBracketFirstLine = root.Q<Toggle>("toggleBracketFirstLine");
            if (toggleBracketFirstLine != null)
            {
                toggleBracketFirstLine.tooltip = "方括号标签只识别首行";
                toggleBracketFirstLine.RegisterValueChangedCallback(ev =>
                {
                    if (!ev.newValue) return;
                    EnhancedConsoleTagLogic.BracketTagFirstLineOnly = true;
                    UpdateTagSettingsControls();
                    RecomputeAllTags();
                });
            }

            var toggleBracketAllLines = root.Q<Toggle>("toggleBracketAllLines");
            if (toggleBracketAllLines != null)
            {
                toggleBracketAllLines.tooltip = "方括号标签识别所有行";
                toggleBracketAllLines.RegisterValueChangedCallback(ev =>
                {
                    if (!ev.newValue) return;
                    EnhancedConsoleTagLogic.BracketTagFirstLineOnly = false;
                    UpdateTagSettingsControls();
                    RecomputeAllTags();
                });
            }

            var toggleAutoStack = root.Q<Toggle>("toggleAutoStack");
            if (toggleAutoStack != null)
            {
                toggleAutoStack.tooltip = "自动识别堆栈类名";
                toggleAutoStack.RegisterValueChangedCallback(ev =>
                {
                    EnhancedConsoleTagLogic.AutoTagStack = ev.newValue;
                    UpdateTagSettingsControls();
                    RecomputeAllTags();
                });
            }

            var btnTagCollapse = root.Q<Button>("btnTagCollapse");
            if (btnTagCollapse != null)
            {
                btnTagCollapse.tooltip = "展开/收起标签栏";
                btnTagCollapse.clicked += () =>
                {
                    _tagsCollapsed = !_tagsCollapsed;
                    SavePrefs();
                    RefreshUI();
                };
            }

            var tagSearchField = root.Q<TextField>("tagSearchField");
            if (tagSearchField != null)
            {
                tagSearchField.tooltip = "按名称过滤标签";
                tagSearchField.value = _tagSearch ?? "";
                tagSearchField.RegisterValueChangedCallback(ev =>
                {
                    _tagSearch = ev.newValue ?? "";
                    SavePrefs();
                    RefreshUI();
                });
            }

            var btnSortMenu = root.Q<Button>("btnTagSortMenu");
            if (btnSortMenu != null)
            {
                btnSortMenu.tooltip = "标签排序方式（再次点击当前项切换升/降序）";
                btnSortMenu.clicked += () =>
                {
                    var menu = new UnityEditor.GenericMenu();
                    AddSortMenuItem(menu, "Name", TagSortMode.Name);
                    AddSortMenuItem(menu, "Count", TagSortMode.Count);
                    AddSortMenuItem(menu, "Recent", TagSortMode.Recent);
                    menu.ShowAsContext();
                };
            }

            UpdateTagSortMenuButton();
            UpdateTagSettingsControls();
        }

        private void RebuildTagBar()
        {
            if (_tagBarContainer == null)
                _tagBarContainer = rootVisualElement?.Q<VisualElement>("tagBarContainer");
            if (_tagBarContainer == null) return;
            _tagBarContainer.Clear();
            UpdateExcludeIndicator();
            if (!_tagsEnabled) return;
            // Use full tag set without tag-filter so the bar doesn't hide selected tags.
            var fullTags = GetAllTagsFromRowsWithoutTagFilter();
            var tagBarContainer = rootVisualElement?.Q<VisualElement>("tagBarContainer");
            if (tagBarContainer != null)
            {
                if (_tagsCollapsed) tagBarContainer.AddToClassList("tag-bar-collapsed");
                else tagBarContainer.RemoveFromClassList("tag-bar-collapsed");
            }
            var btnTagCollapse = rootVisualElement?.Q<Button>("btnTagCollapse");
            if (btnTagCollapse != null) btnTagCollapse.text = _tagsCollapsed ? "More" : "Less";

            var filter = _tagSearch ?? "";
            var hasFilter = !string.IsNullOrWhiteSpace(filter);
            var sorted = fullTags.ToList();
            int dir = _tagSortDesc ? -1 : 1;
            if (_tagSortMode == TagSortMode.Count)
                sorted.Sort((a, b) => dir * a.Value.count.CompareTo(b.Value.count));
            else if (_tagSortMode == TagSortMode.Recent)
                sorted.Sort((a, b) => dir * string.Compare(a.Value.lastTime ?? "", b.Value.lastTime ?? "", System.StringComparison.Ordinal));
            else
                sorted.Sort((a, b) => dir * System.StringComparer.OrdinalIgnoreCase.Compare(a.Key, b.Key));

            foreach (var kv in sorted)
            {
                var tag = kv.Key;
                int count = kv.Value.count;
                string lastTime = kv.Value.lastTime;
                bool match = !hasFilter || (!string.IsNullOrEmpty(tag) && tag.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (!match && !_selectedTags.Contains(tag) && !_excludedTags.Contains(tag))
                    continue;
                var item = new VisualElement();
                item.AddToClassList("tag-item");

                var btn = new Button(() => ToggleIncludeTag(tag))
                {
                    text = tag + "(" + count + ")"
                };
                btn.AddToClassList("log-row-tag");
                btn.AddToClassList("tag-btn");
                btn.tooltip = string.IsNullOrEmpty(lastTime) ? "Left click: include. Right click/x: exclude." : ("Left click: include. Right click/x: exclude. Last: " + lastTime);
                btn.style.backgroundColor = GetTagColor(tag);
                btn.RegisterCallback<ContextClickEvent>(evt =>
                {
                    ToggleExcludeTag(tag);
                    evt.StopPropagation();
                });

                if (_selectedTags.Contains(tag))
                    btn.AddToClassList("selected");
                if (_excludedTags.Contains(tag))
                {
                    btn.AddToClassList("excluded");
                    btn.tooltip = "Excluded (right click to toggle)";
                }

                item.Add(btn);
                _tagBarContainer.Add(item);
            }
        }

        private void ToggleIncludeTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            if (_selectedTags.Contains(tag)) _selectedTags.Remove(tag);
            else
            {
                _selectedTags.Add(tag);
                _excludedTags.Remove(tag);
            }
            _filterDirty = true; _tagCountsDirty = true;
            SavePrefs();
            RefreshUI();
        }

        private void ToggleExcludeTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            if (_excludedTags.Contains(tag)) _excludedTags.Remove(tag);
            else
            {
                _excludedTags.Add(tag);
                _selectedTags.Remove(tag);
            }
            _filterDirty = true; _tagCountsDirty = true;
            SavePrefs();
            RefreshUI();
        }

        private void UpdateExcludeIndicator()
        {
            var indicator = rootVisualElement?.Q<VisualElement>("tagExcludeIndicator");
            if (indicator == null) return;
            bool show = _tagsEnabled && _excludedTags.Count > 0;
            indicator.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            indicator.tooltip = show ? ("Exclude filter enabled: " + _excludedTags.Count + " tag(s)") : "";
        }

        private void UpdateTagSettingsControls()
        {
            var root = rootVisualElement;
            if (root == null) return;
            var toggleTagsEnabled = root.Q<Toggle>("toggleTagsEnabled");
            if (toggleTagsEnabled != null) toggleTagsEnabled.SetValueWithoutNotify(_tagsEnabled);

            bool autoBracket = EnhancedConsoleTagLogic.AutoTagBracket;
            bool firstLineOnly = EnhancedConsoleTagLogic.BracketTagFirstLineOnly;
            var toggleAutoBracket = root.Q<Toggle>("toggleAutoBracket");
            if (toggleAutoBracket != null) toggleAutoBracket.SetValueWithoutNotify(autoBracket);
            var toggleBracketFirstLine = root.Q<Toggle>("toggleBracketFirstLine");
            if (toggleBracketFirstLine != null) toggleBracketFirstLine.SetValueWithoutNotify(firstLineOnly);
            var toggleBracketAllLines = root.Q<Toggle>("toggleBracketAllLines");
            if (toggleBracketAllLines != null) toggleBracketAllLines.SetValueWithoutNotify(!firstLineOnly);

            if (toggleBracketFirstLine != null) toggleBracketFirstLine.SetEnabled(autoBracket);
            if (toggleBracketAllLines != null) toggleBracketAllLines.SetEnabled(autoBracket);

            var toggleAutoStack = root.Q<Toggle>("toggleAutoStack");
            if (toggleAutoStack != null) toggleAutoStack.SetValueWithoutNotify(EnhancedConsoleTagLogic.AutoTagStack);
        }

        private void ToggleTagSort(TagSortMode mode)
        {
            if (_tagSortMode == mode)
                _tagSortDesc = !_tagSortDesc;
            else
            {
                _tagSortMode = mode;
                _tagSortDesc = false;
            }
            SavePrefs();
            UpdateTagSortMenuButton();
            RefreshUI();
        }

        private void AddSortMenuItem(UnityEditor.GenericMenu menu, string label, TagSortMode mode)
        {
            bool isCurrent = _tagSortMode == mode;
            string arrow = _tagSortDesc ? "▼" : "▲";
            string display = isCurrent ? (label + " " + arrow) : label;
            menu.AddItem(new GUIContent(display), false, () => ToggleTagSort(mode));
        }
    }
}
