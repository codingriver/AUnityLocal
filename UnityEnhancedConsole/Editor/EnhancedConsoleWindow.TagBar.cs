using UnityEngine.UIElements;

namespace UnityEnhancedConsole
{
    public partial class EnhancedConsoleWindow
    {
        private void BindTagBar(VisualElement root)
        {
            var btnTagClearInclude = root.Q<Button>("btnTagClearInclude");
            if (btnTagClearInclude != null)
            {
                btnTagClearInclude.tooltip = "Clear included tags";
                btnTagClearInclude.clicked += () =>
                {
                    _selectedTags.Clear();
                    _filterDirty = true; _tagCountsDirty = true;
                    RefreshUI();
                };
            }

            var btnTagClearExclude = root.Q<Button>("btnTagClearExclude");
            if (btnTagClearExclude != null)
            {
                btnTagClearExclude.tooltip = "Clear excluded tags";
                btnTagClearExclude.clicked += () =>
                {
                    _excludedTags.Clear();
                    _filterDirty = true; _tagCountsDirty = true;
                    RefreshUI();
                };
            }
        }

        private void RebuildTagBar()
        {
            if (_tagBarContainer == null) return;
            _tagBarContainer.Clear();
            UpdateExcludeIndicator();
            if (!_tagsEnabled) return;
            // Use full tag set without tag-filter so the bar doesn't hide selected tags.
            var fullTags = GetAllTagsFromRowsWithoutTagFilter();
            foreach (var kv in fullTags)
            {
                var tag = kv.Key;
                int count = kv.Value;
                var item = new VisualElement();
                item.AddToClassList("tag-item");

                var btn = new Button(() => ToggleIncludeTag(tag))
                {
                    text = tag + "(" + count + ")"
                };
                btn.AddToClassList("log-row-tag");
                btn.AddToClassList("tag-btn");
                btn.tooltip = "Left click: include. Right click/x: exclude.";
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
    }
}
