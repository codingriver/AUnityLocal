using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// UI panel for displaying watched variables. Integrates into EnhancedConsoleWindow as a tab.
    /// </summary>
    public class WatchPanel
    {
        private const string UssPath = "Assets/AUnityLocal/UnityEnhancedConsole/Editor/WatchPanel.uss";
        private const float FlashDuration = 0.3f;

        // Root element
        private VisualElement _root;
        private ListView _listView;
        private VisualElement _emptyState;
        private TextField _searchField;
        private VisualElement _historyPanel;
        private ListView _historyListView;
        private Label _historyLabel;
        private WatchGraphRenderer _graphRenderer;
        private VisualElement _graphContainer;
        private bool _showGraph;
        private TextField _stackTraceField;

        // State
        private string _searchText = "";
        private readonly List<WatchRowItem> _filteredItems = new List<WatchRowItem>();
        private readonly HashSet<string> _collapsedGroups = new HashSet<string>();
        private string _selectedEntryName;
        private readonly Dictionary<string, double> _flashTimes = new Dictionary<string, double>();

        // Callback
        public Action OnRequestRepaint;

        /// <summary>Row item for the list — either a group header or an entry.</summary>
        private struct WatchRowItem
        {
            public bool IsGroup;
            public string GroupName;
            public WatchEntry Entry; // null for group headers
        }

        public VisualElement Build()
        {
            _root = new VisualElement();
            _root.AddToClassList("watch-panel");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (styleSheet != null)
                _root.styleSheets.Add(styleSheet);

            // Toolbar
            var toolbar = new VisualElement();
            toolbar.AddToClassList("watch-toolbar");
            _root.Add(toolbar);

            var clearBtn = new Button(() => OnClear()) { text = "Clear" };
            clearBtn.AddToClassList("watch-toolbar-button");
            toolbar.Add(clearBtn);

            var pauseAllBtn = new Button(() => OnPauseAll()) { text = "Pause All" };
            pauseAllBtn.AddToClassList("watch-toolbar-button");
            toolbar.Add(pauseAllBtn);

            var resumeAllBtn = new Button(() => OnResumeAll()) { text = "Resume All" };
            resumeAllBtn.AddToClassList("watch-toolbar-button");
            toolbar.Add(resumeAllBtn);

            _searchField = new TextField();
            _searchField.AddToClassList("watch-search-field");
            _searchField.value = "";
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue ?? "";
                RebuildFilteredList();
                RefreshUI();
            });
            toolbar.Add(_searchField);

            var exportBtn = new Button(OnExportClicked) { text = "Export" };
            exportBtn.AddToClassList("watch-toolbar-button");
            toolbar.Add(exportBtn);

            var settingsBtn = new Button(() => WatchSettingsWindow.Show()) { text = "⚙" };
            settingsBtn.AddToClassList("watch-toolbar-button");
            settingsBtn.style.fontSize = 14;
            toolbar.Add(settingsBtn);

            // Column header
            var header = new VisualElement();
            header.AddToClassList("watch-header");
            _root.Add(header);

            var nameHeader = new Label("Name");
            nameHeader.AddToClassList("watch-header-cell");
            nameHeader.AddToClassList("watch-header-name");
            header.Add(nameHeader);

            var valueHeader = new Label("Value");
            valueHeader.AddToClassList("watch-header-cell");
            valueHeader.AddToClassList("watch-header-value");
            header.Add(valueHeader);

            var changesHeader = new Label("Changes");
            changesHeader.AddToClassList("watch-header-cell");
            changesHeader.AddToClassList("watch-header-changes");
            header.Add(changesHeader);

            var timeHeader = new Label("Last");
            timeHeader.AddToClassList("watch-header-cell");
            timeHeader.AddToClassList("watch-header-time");
            header.Add(timeHeader);

            // Split view: list + history
            var splitView = new TwoPaneSplitView(1, 120, TwoPaneSplitViewOrientation.Vertical);
            splitView.AddToClassList("watch-detail-split");
            _root.Add(splitView);

            // List view
            _listView = new ListView();
            _listView.AddToClassList("watch-list-view");
            _listView.fixedItemHeight = 22;
            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;
            _listView.selectionType = SelectionType.Single;
            _listView.selectionChanged += OnSelectionChanged;
            _listView.RegisterCallback<ContextClickEvent>(OnContextClick);
            _listView.RegisterCallback<KeyDownEvent>(OnKeyDown);
            splitView.Add(_listView);

            // History panel (bottom)
            _historyPanel = new VisualElement();
            _historyPanel.AddToClassList("watch-history-panel");
            splitView.Add(_historyPanel);

            var historyToolbar = new VisualElement();
            historyToolbar.AddToClassList("watch-history-toolbar");
            _historyPanel.Add(historyToolbar);

            _historyLabel = new Label("History");
            _historyLabel.AddToClassList("watch-history-label");
            historyToolbar.Add(_historyLabel);

            // Spacer between label and graph toggle
            var historySpacer = new VisualElement();
            historySpacer.style.flexGrow = 1;
            historyToolbar.Add(historySpacer);

            var graphToggleBtn = new Button(() => ToggleGraphView()) { text = "Graph" };
            graphToggleBtn.AddToClassList("watch-toolbar-button");
            graphToggleBtn.name = "graphToggleBtn";
            historyToolbar.Add(graphToggleBtn);

            // Time/Frame axis toggle
            var axisToggleBtn = new Button(() => ToggleAxisMode()) { text = "Time" };
            axisToggleBtn.AddToClassList("watch-toolbar-button");
            axisToggleBtn.name = "axisToggleBtn";
            historyToolbar.Add(axisToggleBtn);

            _historyListView = new ListView();
            _historyListView.AddToClassList("watch-history-list");
            _historyListView.fixedItemHeight = 18;
            _historyListView.makeItem = MakeHistoryItem;
            _historyListView.bindItem = BindHistoryItem;
            _historyListView.selectionType = SelectionType.Single;
            _historyListView.RegisterCallback<ContextClickEvent>(OnHistoryContextClick);
            _historyListView.selectionChanged += OnHistorySelectionChanged;
            _historyPanel.Add(_historyListView);

            // Graph container (hidden by default)
            _graphRenderer = new WatchGraphRenderer();
            _graphContainer = _graphRenderer.Build();
            _graphContainer.style.display = DisplayStyle.None;
            _historyPanel.Add(_graphContainer);

            // Wire scrubber position changes to history list
            _graphRenderer.OnScrubberPositionChanged = OnScrubberPositionChanged;

            // Stack trace display (hidden by default)
            _stackTraceField = new TextField();
            _stackTraceField.AddToClassList("watch-stacktrace-area");
            _stackTraceField.multiline = true;
            _stackTraceField.isReadOnly = true;
            _stackTraceField.style.display = DisplayStyle.None;
            _stackTraceField.label = "Stack Trace";
            _historyPanel.Add(_stackTraceField);

            // Empty state
            _emptyState = new VisualElement();
            _emptyState.AddToClassList("watch-empty");
            var emptyLabel = new Label("No watched variables");
            emptyLabel.AddToClassList("watch-empty-label");
            _emptyState.Add(emptyLabel);
            var emptyHint = new Label("Use Watch.Set(\"name\", value) to start watching");
            emptyHint.AddToClassList("watch-empty-hint");
            _emptyState.Add(emptyHint);
            _root.Add(_emptyState);
            _emptyState.style.display = DisplayStyle.None;

            // Subscribe to manager changes
            WatchManager.OnChanged -= OnWatchChanged;
            WatchManager.OnChanged += OnWatchChanged;

            RebuildFilteredList();
            RefreshUI();

            return _root;
        }

        public void Dispose()
        {
            WatchManager.OnChanged -= OnWatchChanged;
        }

        // === List Item Creation ===

        private VisualElement MakeItem()
        {
            var row = new VisualElement();
            row.AddToClassList("watch-row");

            // Foldout for group headers (hidden by default, shown only for group rows)
            var foldout = new Foldout();
            foldout.name = "watchFoldout";
            foldout.AddToClassList("watch-group-foldout");
            foldout.style.display = DisplayStyle.None;
            row.Add(foldout);

            var nameLabel = new Label();
            nameLabel.name = "watchName";
            nameLabel.AddToClassList("watch-cell");
            nameLabel.AddToClassList("watch-cell-name");
            row.Add(nameLabel);

            var valueContainer = new VisualElement();
            valueContainer.name = "watchValueContainer";
            valueContainer.style.flexDirection = FlexDirection.Row;
            valueContainer.style.alignItems = Align.Center;
            valueContainer.AddToClassList("watch-cell-value");
            row.Add(valueContainer);

            // Color swatch (hidden by default)
            var colorSwatch = new VisualElement();
            colorSwatch.name = "watchColorSwatch";
            colorSwatch.AddToClassList("watch-color-swatch");
            colorSwatch.style.display = DisplayStyle.None;
            valueContainer.Add(colorSwatch);

            // Bool dot (hidden by default)
            var boolDot = new VisualElement();
            boolDot.name = "watchBoolDot";
            boolDot.AddToClassList("watch-bool-dot");
            boolDot.style.display = DisplayStyle.None;
            valueContainer.Add(boolDot);

            var valueLabel = new Label();
            valueLabel.name = "watchValue";
            valueLabel.style.flexGrow = 1;
            valueContainer.Add(valueLabel);

            // Trend arrow
            var trendLabel = new Label();
            trendLabel.name = "watchTrend";
            trendLabel.AddToClassList("watch-trend");
            valueContainer.Add(trendLabel);

            var changesLabel = new Label();
            changesLabel.name = "watchChanges";
            changesLabel.AddToClassList("watch-cell");
            changesLabel.AddToClassList("watch-cell-changes");
            row.Add(changesLabel);

            var timeLabel = new Label();
            timeLabel.name = "watchTime";
            timeLabel.AddToClassList("watch-cell");
            timeLabel.AddToClassList("watch-cell-time");
            row.Add(timeLabel);

            return row;
        }

        private void BindItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _filteredItems.Count) return;
            var item = _filteredItems[index];

            var foldout = element.Q<Foldout>("watchFoldout");
            var nameLabel = element.Q<Label>("watchName");
            var valueLabel = element.Q<Label>("watchValue");
            var changesLabel = element.Q<Label>("watchChanges");
            var timeLabel = element.Q<Label>("watchTime");
            var trendLabel = element.Q<Label>("watchTrend");
            var colorSwatch = element.Q("watchColorSwatch");
            var boolDot = element.Q("watchBoolDot");

            // Reset classes
            element.RemoveFromClassList("watch-group-row");
            element.RemoveFromClassList("watch-row--paused");
            element.RemoveFromClassList("watch-row--odd");
            element.RemoveFromClassList("watch-flash");
            colorSwatch.style.display = DisplayStyle.None;
            boolDot.style.display = DisplayStyle.None;
            trendLabel.text = "";

            // Unregister previous foldout callback to avoid stale captures
            foldout.UnregisterValueChangedCallback(OnFoldoutToggled);

            if (item.IsGroup)
            {
                // Group header row — use Foldout for native triangle toggle
                element.AddToClassList("watch-group-row");
                foldout.style.display = DisplayStyle.Flex;
                nameLabel.style.display = DisplayStyle.None;

                bool isExpanded = !_collapsedGroups.Contains(item.GroupName);
                foldout.SetValueWithoutNotify(isExpanded);
                foldout.text = item.GroupName;
                foldout.userData = item.GroupName; // store group name for callback

                foldout.RegisterValueChangedCallback(OnFoldoutToggled);

                valueLabel.text = "";
                changesLabel.text = "";
                timeLabel.text = "";
                ClearValueTypeClasses(valueLabel);
                return;
            }

            // Entry row — hide foldout, show name label
            foldout.style.display = DisplayStyle.None;
            nameLabel.style.display = DisplayStyle.Flex;

            var entry = item.Entry;
            if (entry == null) return;

            // Name (indented if grouped)
            nameLabel.text = string.IsNullOrEmpty(entry.Group) ? entry.Name : $"    {entry.DisplayName}";

            // Paused state
            if (entry.IsPaused)
                element.AddToClassList("watch-row--paused");

            // Odd row styling
            if (index % 2 == 1)
                element.AddToClassList("watch-row--odd");

            // Value with type-specific coloring
            ClearValueTypeClasses(valueLabel);
            string formatted = entry.FormattedValue ?? "null";
            valueLabel.text = formatted;

            switch (entry.ValueType)
            {
                case WatchValueType.Float:
                    valueLabel.AddToClassList("watch-value-float");
                    break;
                case WatchValueType.Integer:
                    valueLabel.AddToClassList("watch-value-int");
                    break;
                case WatchValueType.Boolean:
                    bool boolVal = entry.CurrentValue is bool b && b;
                    valueLabel.AddToClassList(boolVal ? "watch-value-bool-true" : "watch-value-bool-false");
                    boolDot.style.display = DisplayStyle.Flex;
                    boolDot.RemoveFromClassList("watch-bool-dot--true");
                    boolDot.RemoveFromClassList("watch-bool-dot--false");
                    boolDot.AddToClassList(boolVal ? "watch-bool-dot--true" : "watch-bool-dot--false");
                    break;
                case WatchValueType.String:
                    valueLabel.AddToClassList("watch-value-string");
                    break;
                case WatchValueType.Vector:
                    valueLabel.AddToClassList("watch-value-vector");
                    break;
                case WatchValueType.Color:
                    valueLabel.AddToClassList("watch-value-color");
                    if (entry.CurrentValue is Color c)
                    {
                        colorSwatch.style.display = DisplayStyle.Flex;
                        colorSwatch.style.backgroundColor = c;
                    }
                    break;
                default:
                    if (formatted == "null")
                        valueLabel.AddToClassList("watch-value-null");
                    break;
            }

            // Trend arrow
            if (entry.HistoryCount >= 2 && entry.ValueType is WatchValueType.Float or WatchValueType.Integer)
            {
                var history = new List<WatchHistoryEntry>();
                foreach (var h in entry.GetHistoryOrdered()) history.Add(h);
                if (history.Count >= 2)
                {
                    var prev = history[history.Count - 2];
                    var curr = history[history.Count - 1];
                    if (prev.HasNumericValue && curr.HasNumericValue)
                    {
                        if (curr.NumericValue > prev.NumericValue)
                        {
                            trendLabel.text = "\u25B2";
                            trendLabel.RemoveFromClassList("watch-trend-down");
                            trendLabel.AddToClassList("watch-trend-up");
                        }
                        else if (curr.NumericValue < prev.NumericValue)
                        {
                            trendLabel.text = "\u25BC";
                            trendLabel.RemoveFromClassList("watch-trend-up");
                            trendLabel.AddToClassList("watch-trend-down");
                        }
                    }
                }
            }

            // Change count
            changesLabel.text = entry.ChangeCount > 9999 ? "9999" : entry.ChangeCount.ToString();

            // Last change time
            if (entry.LastChangeTime > 0)
            {
                double elapsed = EditorApplication.timeSinceStartup - entry.LastChangeTime;
                timeLabel.text = elapsed < 1 ? "now" :
                                 elapsed < 60 ? $"{elapsed:F0}s" :
                                 elapsed < 3600 ? $"{elapsed / 60:F0}m" : $"{elapsed / 3600:F0}h";
            }
            else
            {
                timeLabel.text = "-";
            }

            // Flash animation
            if (_flashTimes.TryGetValue(entry.Name, out double flashTime))
            {
                double age = EditorApplication.timeSinceStartup - flashTime;
                if (age < FlashDuration)
                    element.AddToClassList("watch-flash");
                else
                    _flashTimes.Remove(entry.Name);
            }
        }

        private static void ClearValueTypeClasses(Label label)
        {
            label.RemoveFromClassList("watch-value-float");
            label.RemoveFromClassList("watch-value-int");
            label.RemoveFromClassList("watch-value-bool-true");
            label.RemoveFromClassList("watch-value-bool-false");
            label.RemoveFromClassList("watch-value-string");
            label.RemoveFromClassList("watch-value-vector");
            label.RemoveFromClassList("watch-value-color");
            label.RemoveFromClassList("watch-value-null");
        }

        // === History Item ===

        private VisualElement MakeHistoryItem()
        {
            var row = new VisualElement();
            row.AddToClassList("watch-history-row");

            var tsLabel = new Label();
            tsLabel.name = "histTime";
            tsLabel.AddToClassList("watch-history-timestamp");
            row.Add(tsLabel);

            var frameLabel = new Label();
            frameLabel.name = "histFrame";
            frameLabel.AddToClassList("watch-history-frame");
            row.Add(frameLabel);

            var valLabel = new Label();
            valLabel.name = "histValue";
            valLabel.AddToClassList("watch-history-value");
            row.Add(valLabel);

            return row;
        }

        private readonly List<WatchHistoryEntry> _historyCache = new List<WatchHistoryEntry>();

        private void BindHistoryItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _historyCache.Count) return;
            var entry = _historyCache[index];

            var tsLabel = element.Q<Label>("histTime");
            var frameLabel = element.Q<Label>("histFrame");
            var valLabel = element.Q<Label>("histValue");

            tsLabel.text = $"{entry.Timestamp:F2}s";
            frameLabel.text = entry.FrameCount > 0 ? $"F{entry.FrameCount}" : "-";
            valLabel.text = entry.FormattedValue ?? "null";
        }

        // === Rebuild ===

        public void RebuildFilteredList()
        {
            _filteredItems.Clear();
            var orderedKeys = WatchManager.GetOrderedKeysCopy();

            // Group entries
            var groups = new Dictionary<string, List<WatchEntry>>();
            var ungrouped = new List<WatchEntry>();

            foreach (var key in orderedKeys)
            {
                var entry = WatchManager.GetEntry(key);
                if (entry == null) continue;

                // Search filter — matches name, current value, and history values
                if (!string.IsNullOrEmpty(_searchText))
                {
                    bool matched = entry.Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (entry.FormattedValue != null && entry.FormattedValue.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!matched)
                    {
                        foreach (var h in entry.GetHistoryOrdered())
                        {
                            if (h.FormattedValue != null && h.FormattedValue.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                matched = true;
                                break;
                            }
                        }
                    }
                    if (!matched) continue;
                }

                if (string.IsNullOrEmpty(entry.Group))
                {
                    ungrouped.Add(entry);
                }
                else
                {
                    if (!groups.ContainsKey(entry.Group))
                        groups[entry.Group] = new List<WatchEntry>();
                    groups[entry.Group].Add(entry);
                }
            }

            // Add grouped items
            foreach (var kvp in groups)
            {
                _filteredItems.Add(new WatchRowItem { IsGroup = true, GroupName = kvp.Key });
                if (!_collapsedGroups.Contains(kvp.Key))
                {
                    foreach (var entry in kvp.Value)
                        _filteredItems.Add(new WatchRowItem { IsGroup = false, Entry = entry });
                }
            }

            // Add ungrouped items
            foreach (var entry in ungrouped)
                _filteredItems.Add(new WatchRowItem { IsGroup = false, Entry = entry });
        }

        public void RefreshUI()
        {
            if (_root == null) return;

            bool hasEntries = _filteredItems.Count > 0;
            _emptyState.style.display = hasEntries ? DisplayStyle.None : DisplayStyle.Flex;
            _listView.style.display = hasEntries ? DisplayStyle.Flex : DisplayStyle.None;

            _listView.itemsSource = _filteredItems;
            _listView.Rebuild();

            RefreshHistory();
        }

        private void RefreshHistory()
        {
            _historyCache.Clear();
            if (!string.IsNullOrEmpty(_selectedEntryName))
            {
                var entry = WatchManager.GetEntry(_selectedEntryName);
                if (entry != null)
                {
                    _historyLabel.text = $"History: {entry.Name} ({entry.HistoryCount} records)";
                    foreach (var h in entry.GetHistoryOrdered())
                        _historyCache.Add(h);
                    // Reverse for newest-first display
                    _historyCache.Reverse();

                    // Update graph renderer
                    _graphRenderer?.SetEntry(entry);
                    UpdateGraphToggleVisibility(entry);
                }
                else
                {
                    _historyLabel.text = "History";
                    _graphRenderer?.SetEntry(null);
                    UpdateGraphToggleVisibility(null);
                }
            }
            else
            {
                _historyLabel.text = "History (select an entry)";
                _graphRenderer?.SetEntry(null);
                UpdateGraphToggleVisibility(null);
            }
            _historyListView.itemsSource = _historyCache;
            _historyListView.Rebuild();

            // Clear stack trace display
            if (_stackTraceField != null)
            {
                _stackTraceField.value = "";
                _stackTraceField.style.display = DisplayStyle.None;
            }

            // Refresh graph if visible
            if (_showGraph && _graphContainer != null)
                _graphRenderer?.Refresh();
        }

        private void OnHistorySelectionChanged(IEnumerable<object> selection)
        {
            if (_stackTraceField == null) return;
            foreach (var sel in selection)
            {
                if (sel is WatchHistoryEntry h && !string.IsNullOrEmpty(h.StackTrace))
                {
                    _stackTraceField.value = h.StackTrace;
                    _stackTraceField.style.display = DisplayStyle.Flex;
                    return;
                }
            }
            _stackTraceField.value = "";
            _stackTraceField.style.display = DisplayStyle.None;
        }

        private void ToggleGraphView()
        {
            _showGraph = !_showGraph;
            ApplyGraphViewState();
        }

        private void ApplyGraphViewState()
        {
            if (_historyListView == null || _graphContainer == null) return;

            var toggleBtn = _historyPanel?.Q<Button>("graphToggleBtn");

            if (_showGraph)
            {
                _historyListView.style.display = DisplayStyle.None;
                _graphContainer.style.display = DisplayStyle.Flex;
                if (toggleBtn != null) toggleBtn.text = "List";
                _graphRenderer?.Refresh();
            }
            else
            {
                _historyListView.style.display = DisplayStyle.Flex;
                _graphContainer.style.display = DisplayStyle.None;
                if (toggleBtn != null) toggleBtn.text = "Graph";
            }
        }

        private void ToggleAxisMode()
        {
            if (_graphRenderer == null) return;
            _graphRenderer.UseFrameAxis = !_graphRenderer.UseFrameAxis;
            var btn = _historyPanel?.Q<Button>("axisToggleBtn");
            if (btn != null) btn.text = _graphRenderer.UseFrameAxis ? "Frame" : "Time";
            _graphRenderer.Refresh();
        }

        private void OnScrubberPositionChanged(int historyIndex)
        {
            // Highlight the corresponding entry in the history list
            if (_historyListView == null || _historyCache == null) return;
            // _historyCache is reversed (newest first), _renderedHistory is chronological
            // historyIndex is into _renderedHistory (chronological), convert to _historyCache index
            int reversedIndex = _historyCache.Count - 1 - historyIndex;
            if (reversedIndex >= 0 && reversedIndex < _historyCache.Count)
            {
                _historyListView.SetSelection(reversedIndex);
                _historyListView.ScrollToItem(reversedIndex);
            }
        }

        private void UpdateGraphToggleVisibility(WatchEntry entry)
        {
            var toggleBtn = _historyPanel?.Q<Button>("graphToggleBtn");
            if (toggleBtn == null) return;

            // Only show graph toggle for numeric/vector types
            bool canGraph = entry != null &&
                (entry.ValueType is WatchValueType.Float or WatchValueType.Integer or WatchValueType.Vector) &&
                entry.HistoryCount >= 2;

            toggleBtn.style.display = canGraph ? DisplayStyle.Flex : DisplayStyle.None;

            // If graph is showing but entry can't be graphed, switch back to list
            if (_showGraph && !canGraph)
            {
                _showGraph = false;
                ApplyGraphViewState();
            }
        }

        // === Event Handlers ===

        /// <summary>Handles Foldout triangle click for group fold/expand.</summary>
        private void OnFoldoutToggled(ChangeEvent<bool> evt)
        {
            var foldout = evt.currentTarget as Foldout;
            if (foldout == null) return;

            string groupName = foldout.userData as string;
            if (string.IsNullOrEmpty(groupName)) return;

            if (evt.newValue)
                _collapsedGroups.Remove(groupName);
            else
                _collapsedGroups.Add(groupName);

            // Defer rebuild to avoid modifying ListView during callback
            EditorApplication.delayCall += () =>
            {
                RebuildFilteredList();
                RefreshUI();
            };
        }

        private void OnWatchChanged()
        {
            // Track flash for changed entries
            foreach (var key in WatchManager.GetOrderedKeysCopy())
            {
                var entry = WatchManager.GetEntry(key);
                if (entry != null && entry.LastChangeTime > 0)
                {
                    double age = EditorApplication.timeSinceStartup - entry.LastChangeTime;
                    if (age < FlashDuration)
                        _flashTimes[key] = entry.LastChangeTime;
                }
            }

            RebuildFilteredList();
            RefreshUI();
            OnRequestRepaint?.Invoke();
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            foreach (var sel in selection)
            {
                if (sel is WatchRowItem item)
                {
                    // Group rows are handled by Foldout, skip selection
                    if (item.IsGroup)
                        return;

                    _selectedEntryName = item.Entry?.Name;
                    RefreshHistory();
                    return;
                }
            }
            _selectedEntryName = null;
            RefreshHistory();
        }

        private void OnContextClick(ContextClickEvent evt)
        {
            var menu = new GenericMenu();

            if (!string.IsNullOrEmpty(_selectedEntryName))
            {
                var entry = WatchManager.GetEntry(_selectedEntryName);
                if (entry != null)
                {
                    string name = _selectedEntryName;
                    menu.AddItem(new GUIContent("Copy Value"), false, () =>
                    {
                        GUIUtility.systemCopyBuffer = entry.FormattedValue ?? "";
                    });
                    menu.AddItem(new GUIContent("Copy Name"), false, () =>
                    {
                        GUIUtility.systemCopyBuffer = name;
                    });
                    menu.AddItem(new GUIContent(entry.IsPaused ? "Resume" : "Pause"), false, () =>
                    {
                        Watch.SetPaused(name, !entry.IsPaused);
                    });
                    menu.AddItem(new GUIContent("Remove"), false, () =>
                    {
                        Watch.Remove(name);
                    });
                    bool isCapturing = entry.CaptureStackTrace;
                    menu.AddItem(new GUIContent(isCapturing ? "Disable Stack Trace" : "Enable Stack Trace"), isCapturing, () =>
                    {
                        WatchManager.SetCaptureStackTrace(name, !isCapturing);
                    });
                    menu.AddSeparator("");
                }
            }

            menu.AddItem(new GUIContent("Clear All"), false, () => Watch.Clear());
            menu.ShowAsContext();
            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.ctrlKey && evt.keyCode == KeyCode.C)
            {
                if (!string.IsNullOrEmpty(_selectedEntryName))
                {
                    var entry = WatchManager.GetEntry(_selectedEntryName);
                    if (entry != null)
                        GUIUtility.systemCopyBuffer = $"{entry.Name}: {entry.FormattedValue}";
                }
                evt.StopPropagation();
            }
        }

        private void OnClear()
        {
            Watch.Clear();
        }

        private void OnPauseAll()
        {
            foreach (var key in WatchManager.GetOrderedKeysCopy())
                Watch.SetPaused(key, true);
        }

        private void OnResumeAll()
        {
            foreach (var key in WatchManager.GetOrderedKeysCopy())
                Watch.SetPaused(key, false);
        }

        // ── Export ────────────────────────────────────────

        private void OnExportClicked()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Export as CSV"), false, () => ExportAs("csv"));
            menu.AddItem(new GUIContent("Export as JSON"), false, () => ExportAs("json"));
            menu.ShowAsContext();
        }

        private void ExportAs(string format)
        {
            var entries = WatchManager.ReadEntries();
            if (entries.Count == 0)
            {
                EditorUtility.DisplayDialog("Export", "No watch entries to export.", "OK");
                return;
            }

            string ext = format == "json" ? "json" : "csv";
            string defaultName = $"WatchExport_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
            string path = EditorUtility.SaveFilePanel($"Export Watch Data as {ext.ToUpper()}", "", defaultName, ext);
            if (string.IsNullOrEmpty(path)) return;

            string content = format == "json" ? BuildJson(entries) : BuildCsv(entries);
            File.WriteAllText(path, content, Encoding.UTF8);
            EditorUtility.DisplayDialog("Export", $"Exported {entries.Count} entries to:\n{path}", "OK");
        }

        private static string BuildCsv(List<WatchEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name,Group,ValueType,CurrentValue,IsPaused,ChangeCount,HistoryTimestamp,HistoryFrame,HistoryValue");
            foreach (var e in entries)
            {
                var history = new List<WatchHistoryEntry>(e.GetHistoryOrdered());
                if (history.Count == 0)
                {
                    sb.AppendLine($"{CsvEscape(e.Name)},{CsvEscape(e.Group)},{e.ValueType},{CsvEscape(e.FormattedValue)},{e.IsPaused},{e.ChangeCount},,,");
                }
                else
                {
                    foreach (var h in history)
                    {
                        sb.AppendLine($"{CsvEscape(e.Name)},{CsvEscape(e.Group)},{e.ValueType},{CsvEscape(e.FormattedValue)},{e.IsPaused},{e.ChangeCount},{h.Timestamp:F3},{h.FrameCount},{CsvEscape(h.FormattedValue)}");
                    }
                }
            }
            return sb.ToString();
        }

        private static string CsvEscape(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            if (v.Contains(",") || v.Contains("\"") || v.Contains("\n"))
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            return v;
        }

        private static string BuildJson(List<WatchEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                sb.AppendLine("  {");
                sb.AppendLine($"    \"name\": {JsonEscape(e.Name)},");
                sb.AppendLine($"    \"group\": {JsonEscape(e.Group)},");
                sb.AppendLine($"    \"valueType\": \"{e.ValueType}\",");
                sb.AppendLine($"    \"currentValue\": {JsonEscape(e.FormattedValue)},");
                sb.AppendLine($"    \"isPaused\": {(e.IsPaused ? "true" : "false")},");
                sb.AppendLine($"    \"changeCount\": {e.ChangeCount},");
                sb.Append("    \"history\": [");
                var history = new List<WatchHistoryEntry>(e.GetHistoryOrdered());
                if (history.Count > 0)
                {
                    sb.AppendLine();
                    for (int j = 0; j < history.Count; j++)
                    {
                        var h = history[j];
                        sb.Append($"      {{ \"timestamp\": {h.Timestamp:F3}, \"frame\": {h.FrameCount}, \"value\": {JsonEscape(h.FormattedValue)}");
                        if (h.HasNumericValue && !double.IsNaN(h.NumericValue) && !double.IsInfinity(h.NumericValue))
                            sb.Append($", \"numeric\": {h.NumericValue}");
                        sb.Append(" }");
                        if (j < history.Count - 1) sb.Append(",");
                        sb.AppendLine();
                    }
                    sb.Append("    ");
                }
                sb.AppendLine("]");
                sb.Append("  }");
                if (i < entries.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("]");
            return sb.ToString();
        }

        // ── History context menu ──────────────────────────────────────
        private void OnHistoryContextClick(ContextClickEvent evt)
        {
            if (_historyCache == null || _historyCache.Count == 0) return;

            var menu = new GenericMenu();
            int selectedIdx = _historyListView.selectedIndex;

            if (selectedIdx >= 0 && selectedIdx < _historyCache.Count)
            {
                var h = _historyCache[selectedIdx];
                menu.AddItem(new GUIContent("Copy Value"), false, () =>
                {
                    EditorGUIUtility.systemCopyBuffer = h.FormattedValue ?? "";
                });
                menu.AddItem(new GUIContent("Copy Row (Time | Frame | Value)"), false, () =>
                {
                    var age = EditorApplication.timeSinceStartup - h.Timestamp;
                    EditorGUIUtility.systemCopyBuffer = $"{age:F2}s ago | Frame {h.FrameCount} | {h.FormattedValue}";
                });
                menu.AddSeparator("");
            }

            menu.AddItem(new GUIContent("Copy All History"), false, () =>
            {
                var sb = new StringBuilder();
                var now = EditorApplication.timeSinceStartup;
                for (int i = 0; i < _historyCache.Count; i++)
                {
                    var h = _historyCache[i];
                    var age = now - h.Timestamp;
                    sb.AppendLine($"{age:F2}s ago | Frame {h.FrameCount} | {h.FormattedValue}");
                }
                EditorGUIUtility.systemCopyBuffer = sb.ToString();
            });

            menu.AddItem(new GUIContent("Copy All History (CSV)"), false, () =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("Timestamp,Frame,Value");
                for (int i = 0; i < _historyCache.Count; i++)
                {
                    var h = _historyCache[i];
                    sb.AppendLine($"{h.Timestamp:F4},{h.FrameCount},{CsvEscape(h.FormattedValue)}");
                }
                EditorGUIUtility.systemCopyBuffer = sb.ToString();
            });

            menu.ShowAsContext();
            evt.StopPropagation();
        }

        private static string JsonEscape(string v)
        {
            if (v == null) return "null";
            var sb = new StringBuilder(v.Length + 2);
            sb.Append('"');
            foreach (char c in v)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
