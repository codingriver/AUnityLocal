using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEnhancedConsole
{
    public partial class EnhancedConsoleWindow
    {
        private void BindTagBar(VisualElement root)
        {
            var btnTagClearMenu = root.Q<Button>("btnTagClearMenu");
            if (btnTagClearMenu != null)
            {
                btnTagClearMenu.tooltip = "清除标签过滤";
                btnTagClearMenu.clicked += () =>
                {
                    var menu = new UnityEditor.GenericMenu();
                    menu.AddItem(new GUIContent("清除包含标签 (Clear+)"), false, () =>
                    {
                        _selectedTags.Clear();
                        _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                        SavePrefs();
                        RefreshUI();
                    });
                    menu.AddItem(new GUIContent("清除排除标签 (Clear-)"), false, () =>
                    {
                        _excludedTags.Clear();
                        _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                        SavePrefs();
                        RefreshUI();
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("全部清除 (Clear All)"), false, () =>
                    {
                        _selectedTags.Clear();
                        _excludedTags.Clear();
                        _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
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
                    _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++; _tagCountsDirty = true;
                    SavePrefs();
                    RefreshUI();
                });
            }

            var btnTagRules = root.Q<Button>("btnTagRules");
            if (btnTagRules != null)
            {
                btnTagRules.tooltip = "编辑标签设置";
                btnTagRules.clicked += () => TagRulesWindow.Open(this);
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
                    menu.ShowAsContext();
                };
            }

            UpdateTagSortMenuButton();
            UpdateTagSettingsControls();
        }

        // 排序快照缓存：仅当数据/过滤/排序模式变化时才重排
        private Dictionary<string, TagInfo> _lastSortedTagSource;
        private string _lastSortedTagFilter;
        private TagSortMode _lastSortedTagSortMode = TagSortMode.Name;
        private bool _lastSortedTagSortDesc;
        private List<string> _cachedDisplayTagNames;
        private List<int> _cachedDisplayTagCounts;
        private List<string> _cachedDisplayTagLastTimes;

        private void RebuildTagBar()
        {
            if (_tagBarContainer == null)
                _tagBarContainer = rootVisualElement?.Q<VisualElement>("tagBarContainer");
            if (_tagBarContainer == null) return;
            UpdateExcludeIndicator();
            if (!_tagsEnabled) return;

            var tagBarContainer = rootVisualElement?.Q<VisualElement>("tagBarContainer");
            if (tagBarContainer != null)
            {
                if (_tagsCollapsed) tagBarContainer.AddToClassList("tag-bar-collapsed");
                else tagBarContainer.RemoveFromClassList("tag-bar-collapsed");
            }
            var btnTagCollapse = rootVisualElement?.Q<Button>("btnTagCollapse");
            if (btnTagCollapse != null) btnTagCollapse.text = _tagsCollapsed ? "More" : "Less";

            if (_tagsCollapsed)
            {
                _tagBarContainer.Clear();
                _lastTagBarOrder.Clear();
                return;
            }

            // Use full tag set without tag-filter so the bar doesn't hide selected tags.
            var fullTags = GetAllTagsFromRowsWithoutTagFilter();

            var filter = _tagSearch ?? "";
            var hasFilter = !string.IsNullOrWhiteSpace(filter);

            // 命中快照：复用上一轮排序结果，避免每次 RefreshUI 都重排 + 重建临时 List
            bool snapshotValid = _cachedDisplayTagNames != null
                && ReferenceEquals(_lastSortedTagSource, fullTags)
                && string.Equals(_lastSortedTagFilter, filter, StringComparison.Ordinal)
                && _lastSortedTagSortMode == _tagSortMode
                && _lastSortedTagSortDesc == _tagSortDesc;

            List<string> displayTagNames;
            List<int> displayTagCounts;
            List<string> displayTagLastTimes;
            if (snapshotValid)
            {
                displayTagNames = _cachedDisplayTagNames;
                displayTagCounts = _cachedDisplayTagCounts;
                displayTagLastTimes = _cachedDisplayTagLastTimes;
            }
            else
            {
                var sorted = fullTags.ToList();
                int dir = _tagSortDesc ? -1 : 1;
                // 去除 count/recent 排序：tag 元数据已不再统计，统一按名称排序，避免依赖已废弃的 count/lastTime 字段
                sorted.Sort((a, b) => dir * System.StringComparer.OrdinalIgnoreCase.Compare(a.Key, b.Key));

                displayTagNames = _cachedDisplayTagNames ?? new List<string>(sorted.Count);
                displayTagCounts = _cachedDisplayTagCounts ?? new List<int>(sorted.Count);
                displayTagLastTimes = _cachedDisplayTagLastTimes ?? new List<string>(sorted.Count);
                displayTagNames.Clear();
                displayTagCounts.Clear();
                displayTagLastTimes.Clear();
                foreach (var kv in sorted)
                {
                    var tagName = kv.Key;
                    bool match = !hasFilter || (!string.IsNullOrEmpty(tagName) && tagName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!match && !_selectedTags.Contains(tagName) && !_excludedTags.Contains(tagName))
                        continue;
                    displayTagNames.Add(tagName);
                    displayTagCounts.Add(0);
                    displayTagLastTimes.Add(null);
                }
                _cachedDisplayTagNames = displayTagNames;
                _cachedDisplayTagCounts = displayTagCounts;
                _cachedDisplayTagLastTimes = displayTagLastTimes;
                _lastSortedTagSource = fullTags;
                _lastSortedTagFilter = filter;
                _lastSortedTagSortMode = _tagSortMode;
                _lastSortedTagSortDesc = _tagSortDesc;
            }

            // 快速路径：标签集合和排序完全未变，只更新 text / style
            bool canFastPath = displayTagNames.Count == _lastTagBarOrder.Count;
            if (canFastPath)
            {
                for (int i = 0; i < displayTagNames.Count; i++)
                {
                    if (!string.Equals(displayTagNames[i], _lastTagBarOrder[i], StringComparison.OrdinalIgnoreCase))
                    {
                        canFastPath = false;
                        break;
                    }
                }
            }

            if (canFastPath)
            {
                for (int i = 0; i < displayTagNames.Count; i++)
                {
                    var tag = displayTagNames[i];
                    if (!_tagButtonPool.TryGetValue(tag, out var btn)) continue;
                    btn.text = tag;
                    btn.tooltip = "Left click: include. Right click/x: exclude.";
                    if (_excludedTags.Contains(tag))
                        btn.tooltip = "Excluded (right click to toggle)";
                    btn.EnableInClassList("selected", _selectedTags.Contains(tag));
                    btn.EnableInClassList("excluded", _excludedTags.Contains(tag));
                    btn.style.backgroundColor = GetTagColor(tag);
                }
                return;
            }

            // 完整重建路径：复用已有 Button，避免重复创建 UI 对象
            _lastTagBarOrder.Clear();
            _tagBarContainer.Clear();
            for (int i = 0; i < displayTagNames.Count; i++)
            {
                var tag = displayTagNames[i];
                _lastTagBarOrder.Add(tag);
                if (!_tagButtonPool.TryGetValue(tag, out var btn))
                {
                    string capturedTag = tag; // 避免闭包捕获循环变量
                    btn = new Button(() => ToggleIncludeTag(capturedTag))
                    {
                        text = tag
                    };
                    btn.AddToClassList("log-row-tag");
                    btn.AddToClassList("tag-btn");
                    btn.RegisterCallback<ContextClickEvent>(evt =>
                    {
                        ToggleExcludeTag(capturedTag);
                        evt.StopPropagation();
                    });
                    _tagButtonPool[tag] = btn;
                }
                else
                {
                    btn.text = tag;
                }
                btn.tooltip = "Left click: include. Right click/x: exclude.";
                btn.style.backgroundColor = GetTagColor(tag);
                btn.EnableInClassList("selected", _selectedTags.Contains(tag));
                btn.EnableInClassList("excluded", _excludedTags.Contains(tag));
                if (_excludedTags.Contains(tag))
                    btn.tooltip = "Excluded (right click to toggle)";

                var item = new VisualElement();
                item.AddToClassList("tag-item");
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
            // 切换标签 include/exclude 不影响 tag bar 计数（计数 without tag filter）
            _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++;
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
            _filterAppendOnly = false; _filterDirty = true; _filterCriteriaVersion++;
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
