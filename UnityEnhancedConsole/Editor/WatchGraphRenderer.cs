using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// Renders a numeric value history graph using UI Toolkit's generateVisualContent.
    /// Supports single value, Vector2/3 component lines, and interactive tooltip.
    /// </summary>
    public class WatchGraphRenderer
    {
        // --- Configuration ---
        public float TimeRange { get; set; } = 5f; // seconds of history to display
        public float MinY { get; set; } = float.NaN;
        public float MaxY { get; set; } = float.NaN;
        public bool AutoScale { get; set; } = true;

        // --- Colors ---
        static readonly Color GridColor = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color AxisColor = new Color(1f, 1f, 1f, 0.2f);
        static readonly Color LabelColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        static readonly Color ValueLineColor = new Color(0.35f, 0.75f, 0.95f, 1f);
        static readonly Color VectorXColor = new Color(0.95f, 0.3f, 0.3f, 1f);
        static readonly Color VectorYColor = new Color(0.3f, 0.95f, 0.3f, 1f);
        static readonly Color VectorZColor = new Color(0.3f, 0.5f, 0.95f, 1f);
        static readonly Color ZeroLineColor = new Color(1f, 1f, 1f, 0.12f);
        static readonly Color TooltipBgColor = new Color(0.15f, 0.15f, 0.15f, 0.92f);
        static readonly Color TooltipBorderColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

        const float Padding = 32f;
        const float PaddingRight = 8f;
        const float PaddingTop = 8f;
        const float PaddingBottom = 20f;
        const float LineWidth = 1.5f;
        const int MaxGridLines = 5;

        // --- Scrubber ---
        static readonly Color ScrubberColor = new Color(1f, 0.7f, 0.2f, 0.9f);
        static readonly Color ScrubberDotColor = new Color(1f, 0.85f, 0.4f, 1f);

        // --- State ---
        private WatchEntry _entry;
        private VisualElement _graphElement;
        private Label _tooltipLabel;
        private Label _scrubberLabel;
        private float _hoverX = -1f;

        // Scrubber state
        private int _scrubberIndex = -1;
        private bool _isDraggingScrubber;
        private List<WatchHistoryEntry> _renderedHistory;

        // Frame axis mode
        public bool UseFrameAxis { get; set; }

        /// <summary>Fires with the history index when scrubber position changes.</summary>
        public Action<int> OnScrubberPositionChanged;

        public VisualElement Build()
        {
            var container = new VisualElement();
            container.AddToClassList("watch-graph-container");
            container.style.flexGrow = 1;
            container.style.minHeight = 60;

            // Scrubber value label (displayed above graph)
            _scrubberLabel = new Label();
            _scrubberLabel.AddToClassList("watch-scrubber-label");
            _scrubberLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _scrubberLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _scrubberLabel.style.fontSize = 11;
            _scrubberLabel.style.height = 18;
            _scrubberLabel.style.display = DisplayStyle.None;
            _scrubberLabel.style.color = ScrubberDotColor;
            container.Add(_scrubberLabel);

            _graphElement = new VisualElement();
            _graphElement.AddToClassList("watch-graph");
            _graphElement.style.flexGrow = 1;
            _graphElement.generateVisualContent += OnGenerateVisualContent;
            _graphElement.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            _graphElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            _graphElement.RegisterCallback<MouseDownEvent>(OnMouseDown);
            _graphElement.RegisterCallback<MouseUpEvent>(OnMouseUp);
            container.Add(_graphElement);

            _tooltipLabel = new Label();
            _tooltipLabel.AddToClassList("watch-graph-tooltip");
            _tooltipLabel.style.position = Position.Absolute;
            _tooltipLabel.style.display = DisplayStyle.None;
            _tooltipLabel.style.backgroundColor = TooltipBgColor;
            _tooltipLabel.style.color = LabelColor;
            _tooltipLabel.style.borderTopColor = TooltipBorderColor;
            _tooltipLabel.style.borderBottomColor = TooltipBorderColor;
            _tooltipLabel.style.borderLeftColor = TooltipBorderColor;
            _tooltipLabel.style.borderRightColor = TooltipBorderColor;
            _tooltipLabel.style.borderTopWidth = 1;
            _tooltipLabel.style.borderBottomWidth = 1;
            _tooltipLabel.style.borderLeftWidth = 1;
            _tooltipLabel.style.borderRightWidth = 1;
            _tooltipLabel.style.borderTopLeftRadius = 3;
            _tooltipLabel.style.borderTopRightRadius = 3;
            _tooltipLabel.style.borderBottomLeftRadius = 3;
            _tooltipLabel.style.borderBottomRightRadius = 3;
            _tooltipLabel.style.paddingLeft = 6;
            _tooltipLabel.style.paddingRight = 6;
            _tooltipLabel.style.paddingTop = 2;
            _tooltipLabel.style.paddingBottom = 2;
            _tooltipLabel.style.fontSize = 10;
            container.Add(_tooltipLabel);

            return container;
        }

        public void SetEntry(WatchEntry entry)
        {
            _entry = entry;
            _graphElement?.MarkDirtyRepaint();
        }

        public void Refresh()
        {
            _graphElement?.MarkDirtyRepaint();
        }

        // --- Rendering ---

        private double GetXValue(WatchHistoryEntry h) => UseFrameAxis ? h.FrameCount : h.Timestamp;

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_entry == null || _entry.HistoryCount < 2) return;

            var rect = _graphElement.contentRect;
            if (rect.width < 10 || rect.height < 10) return;

            var painter = mgc.painter2D;
            float left = Padding;
            float right = rect.width - PaddingRight;
            float top = PaddingTop;
            float bottom = rect.height - PaddingBottom;
            float plotW = right - left;
            float plotH = bottom - top;

            if (plotW <= 0 || plotH <= 0) return;

            // Collect history data
            bool isVectorType = _entry.ValueType == WatchValueType.Vector;
            var history = new List<WatchHistoryEntry>();
            foreach (var h in _entry.GetHistoryOrdered())
            {
                if (h.HasNumericValue || isVectorType)
                    history.Add(h);
            }
            if (history.Count < 2) return;
            _renderedHistory = history;

            // Compute X axis range (time or frame based)
            double xStart, xEnd;
            if (UseFrameAxis)
            {
                int frameMin = int.MaxValue, frameMax = int.MinValue;
                foreach (var h in history)
                {
                    if (h.FrameCount < frameMin) frameMin = h.FrameCount;
                    if (h.FrameCount > frameMax) frameMax = h.FrameCount;
                }
                double span = frameMax - frameMin;
                if (span < 1) span = 10;
                xStart = frameMin - span * 0.05;
                xEnd = frameMax + span * 0.05;
            }
            else
            {
                double now = EditorApplication.timeSinceStartup;
                double preferredStart = now - TimeRange;
                double dataEarliest = double.MaxValue, dataLatest = double.MinValue;
                foreach (var h in history)
                {
                    if (h.Timestamp < dataEarliest) dataEarliest = h.Timestamp;
                    if (h.Timestamp > dataLatest) dataLatest = h.Timestamp;
                }
                if (dataLatest >= preferredStart)
                {
                    xStart = preferredStart;
                    xEnd = now;
                }
                else
                {
                    double span = dataLatest - dataEarliest;
                    if (span < 0.01) span = 1.0;
                    xStart = dataEarliest - span * 0.05;
                    xEnd = dataLatest + span * 0.1;
                }
            }
            double xDuration = xEnd - xStart;
            if (xDuration < 0.001) xDuration = 1.0;

            // Determine Y range
            float yMin = float.MaxValue, yMax = float.MinValue;
            if (AutoScale)
            {
                bool isVector = _entry.ValueType == WatchValueType.Vector;
                if (isVector)
                {
                    foreach (var h in history)
                    {
                        if (TryParseVectorComponents(h.FormattedValue, out var components))
                        {
                            foreach (float c in components)
                            {
                                if (c < yMin) yMin = c;
                                if (c > yMax) yMax = c;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var h in history)
                    {
                        float v = (float)h.NumericValue;
                        if (v < yMin) yMin = v;
                        if (v > yMax) yMax = v;
                    }
                }

                if (yMin >= yMax)
                {
                    float center = yMin == float.MaxValue ? 0f : yMin;
                    yMin = center - 1f;
                    yMax = center + 1f;
                }

                // Add 10% padding
                float yPad = (yMax - yMin) * 0.1f;
                yMin -= yPad;
                yMax += yPad;
            }
            else
            {
                yMin = float.IsNaN(MinY) ? 0f : MinY;
                yMax = float.IsNaN(MaxY) ? 1f : MaxY;
            }

            // --- Draw grid ---
            DrawGrid(painter, left, top, right, bottom, plotW, plotH, yMin, yMax, xStart, xEnd);

            // --- Draw zero line if in range ---
            if (yMin < 0 && yMax > 0)
            {
                float zy = bottom - ((0 - yMin) / (yMax - yMin)) * plotH;
                painter.strokeColor = ZeroLineColor;
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(left, zy));
                painter.LineTo(new Vector2(right, zy));
                painter.Stroke();
            }

            // --- Draw value lines ---
            if (_entry.ValueType == WatchValueType.Vector)
            {
                DrawVectorLines(painter, history, left, top, right, bottom, plotW, plotH, yMin, yMax, xStart, xDuration);
            }
            else
            {
                DrawSingleLine(painter, history, left, top, right, bottom, plotW, plotH, yMin, yMax, xStart, xDuration, ValueLineColor);
            }

            // --- Draw hover line ---
            if (_hoverX >= left && _hoverX <= right)
            {
                painter.strokeColor = new Color(1f, 1f, 1f, 0.3f);
                painter.lineWidth = 1f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(_hoverX, top));
                painter.LineTo(new Vector2(_hoverX, bottom));
                painter.Stroke();
            }

            // --- Draw scrubber ---
            if (_scrubberIndex >= 0 && _scrubberIndex < _renderedHistory.Count && xDuration > 0)
            {
                var scrubEntry = _renderedHistory[_scrubberIndex];
                float sx = (float)((GetXValue(scrubEntry) - xStart) / xDuration);
                float scrubX = left + sx * plotW;

                // Scrubber vertical line
                painter.strokeColor = ScrubberColor;
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(scrubX, top));
                painter.LineTo(new Vector2(scrubX, bottom));
                painter.Stroke();

                // Scrubber dot at value intersection
                if (scrubEntry.HasNumericValue)
                {
                    float vy = ((float)scrubEntry.NumericValue - yMin) / (yMax - yMin);
                    float dotY = bottom - Mathf.Clamp01(vy) * plotH;
                    painter.fillColor = ScrubberColor;
                    painter.BeginPath();
                    painter.Arc(new Vector2(scrubX, dotY), 4f, 0f, 360f);
                    painter.Fill();
                }

                // Update scrubber label
                UpdateScrubberLabel(scrubEntry);
            }
        }

        private void DrawGrid(Painter2D painter, float left, float top, float right, float bottom,
            float plotW, float plotH, float yMin, float yMax, double timeStart, double timeEnd)
        {
            painter.strokeColor = GridColor;
            painter.lineWidth = 1f;

            // Horizontal grid lines
            int hLines = Mathf.Min(MaxGridLines, Mathf.Max(2, Mathf.FloorToInt(plotH / 30f)));
            for (int i = 0; i <= hLines; i++)
            {
                float t = (float)i / hLines;
                float y = bottom - t * plotH;
                painter.BeginPath();
                painter.MoveTo(new Vector2(left, y));
                painter.LineTo(new Vector2(right, y));
                painter.Stroke();
            }

            // Vertical grid lines (time)
            int vLines = Mathf.Min(MaxGridLines, Mathf.Max(2, Mathf.FloorToInt(plotW / 60f)));
            for (int i = 0; i <= vLines; i++)
            {
                float t = (float)i / vLines;
                float x = left + t * plotW;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, top));
                painter.LineTo(new Vector2(x, bottom));
                painter.Stroke();
            }

            // Axis border
            painter.strokeColor = AxisColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(left, top));
            painter.LineTo(new Vector2(left, bottom));
            painter.LineTo(new Vector2(right, bottom));
            painter.Stroke();
        }

        private void DrawSingleLine(Painter2D painter, List<WatchHistoryEntry> history,
            float left, float top, float right, float bottom, float plotW, float plotH,
            float yMin, float yMax, double timeStart, double timeDuration, Color lineColor)
        {
            painter.strokeColor = lineColor;
            painter.lineWidth = LineWidth;
            painter.BeginPath();

            bool started = false;
            for (int i = 0; i < history.Count; i++)
            {
                var h = history[i];

                float tx = (float)((GetXValue(h) - timeStart) / timeDuration);
                float ty = ((float)h.NumericValue - yMin) / (yMax - yMin);
                float x = left + tx * plotW;
                float y = bottom - ty * plotH;

                if (!started) { painter.MoveTo(new Vector2(x, y)); started = true; }
                else painter.LineTo(new Vector2(x, y));
            }

            if (started) painter.Stroke();
        }

        private void DrawVectorLines(Painter2D painter, List<WatchHistoryEntry> history,
            float left, float top, float right, float bottom, float plotW, float plotH,
            float yMin, float yMax, double timeStart, double timeDuration)
        {
            // Parse all vector entries into component arrays
            var xPts = new List<(float time, float value)>();
            var yPts = new List<(float time, float value)>();
            var zPts = new List<(float time, float value)>();

            for (int i = 0; i < history.Count; i++)
            {
                var h = history[i];
                float tx = (float)((GetXValue(h) - timeStart) / timeDuration);

                if (TryParseVectorComponents(h.FormattedValue, out var comp))
                {
                    if (comp.Length >= 1) xPts.Add((tx, comp[0]));
                    if (comp.Length >= 2) yPts.Add((tx, comp[1]));
                    if (comp.Length >= 3) zPts.Add((tx, comp[2]));
                }
            }

            DrawComponentLine(painter, xPts, left, bottom, plotW, plotH, yMin, yMax, VectorXColor);
            DrawComponentLine(painter, yPts, left, bottom, plotW, plotH, yMin, yMax, VectorYColor);
            DrawComponentLine(painter, zPts, left, bottom, plotW, plotH, yMin, yMax, VectorZColor);
        }

        private void DrawComponentLine(Painter2D painter, List<(float time, float value)> points,
            float left, float bottom, float plotW, float plotH, float yMin, float yMax, Color color)
        {
            if (points.Count < 2) return;

            painter.strokeColor = color;
            painter.lineWidth = LineWidth;
            painter.BeginPath();

            for (int i = 0; i < points.Count; i++)
            {
                float x = left + points[i].time * plotW;
                float ty = (points[i].value - yMin) / (yMax - yMin);
                float y = bottom - ty * plotH;

                if (i == 0) painter.MoveTo(new Vector2(x, y));
                else painter.LineTo(new Vector2(x, y));
            }

            painter.Stroke();
        }

        // --- Scrubber ---

        private void UpdateScrubberLabel(WatchHistoryEntry entry)
        {
            if (_scrubberLabel == null) return;
            string frameInfo = $"F{entry.FrameCount}";
            double ago = EditorApplication.timeSinceStartup - entry.Timestamp;
            _scrubberLabel.text = $"{entry.FormattedValue}  ({frameInfo}, {ago:F1}s ago)";
            _scrubberLabel.style.display = DisplayStyle.Flex;
        }

        private int FindClosestHistoryIndex(float mouseX)
        {
            if (_renderedHistory == null || _renderedHistory.Count == 0) return -1;

            var rect = _graphElement.contentRect;
            float left = Padding;
            float right = rect.width - PaddingRight;
            float plotW = right - left;
            if (plotW <= 0) return -1;

            float tx = (mouseX - left) / plotW;
            if (tx < 0f) tx = 0f;
            if (tx > 1f) tx = 1f;

            // Compute X range from rendered history
            if (_renderedHistory.Count < 2) return 0;
            double xMin = GetXValue(_renderedHistory[0]);
            double xMax = GetXValue(_renderedHistory[_renderedHistory.Count - 1]);
            if (UseFrameAxis)
            {
                foreach (var h in _renderedHistory) { if (GetXValue(h) < xMin) xMin = GetXValue(h); if (GetXValue(h) > xMax) xMax = GetXValue(h); }
            }
            else
            {
                double now = EditorApplication.timeSinceStartup;
                xMax = now;
                xMin = now - TimeRange;
                if (GetXValue(_renderedHistory[0]) < xMin) xMin = GetXValue(_renderedHistory[0]);
            }
            double xRange = xMax - xMin;
            if (xRange < 0.001) xRange = 1.0;
            double targetX = xMin + tx * xRange;

            int bestIdx = 0;
            double bestDist = double.MaxValue;
            for (int i = 0; i < _renderedHistory.Count; i++)
            {
                double dist = Math.Abs(GetXValue(_renderedHistory[i]) - targetX);
                if (dist < bestDist) { bestDist = dist; bestIdx = i; }
            }
            return bestIdx;
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0) // Left click
            {
                _isDraggingScrubber = true;
                int idx = FindClosestHistoryIndex(evt.localMousePosition.x);
                if (idx >= 0 && idx != _scrubberIndex)
                {
                    _scrubberIndex = idx;
                    OnScrubberPositionChanged?.Invoke(_scrubberIndex);
                    _graphElement?.MarkDirtyRepaint();
                }
                evt.StopPropagation();
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 0)
            {
                _isDraggingScrubber = false;
            }
        }

        // --- Interaction ---

        private void OnMouseMove(MouseMoveEvent evt)
        {
            _hoverX = evt.localMousePosition.x;

            if (_isDraggingScrubber)
            {
                int idx = FindClosestHistoryIndex(evt.localMousePosition.x);
                if (idx >= 0 && idx != _scrubberIndex)
                {
                    _scrubberIndex = idx;
                    OnScrubberPositionChanged?.Invoke(_scrubberIndex);
                }
            }

            _graphElement?.MarkDirtyRepaint();
            UpdateTooltip(evt.localMousePosition);
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _hoverX = -1f;
            _isDraggingScrubber = false;
            _tooltipLabel.style.display = DisplayStyle.None;
            _graphElement?.MarkDirtyRepaint();
        }

        private void UpdateTooltip(Vector2 mousePos)
        {
            if (_entry == null || _entry.HistoryCount == 0)
            {
                _tooltipLabel.style.display = DisplayStyle.None;
                return;
            }

            var rect = _graphElement.contentRect;
            float left = Padding;
            float right = rect.width - PaddingRight;
            float plotW = right - left;
            if (plotW <= 0 || mousePos.x < left || mousePos.x > right)
            {
                _tooltipLabel.style.display = DisplayStyle.None;
                return;
            }

            // Use cached rendered history for tooltip (same data as rendering)
            var history = _renderedHistory;
            if (history == null || history.Count == 0)
            {
                // Fallback: build from entry
                history = new List<WatchHistoryEntry>();
                foreach (var h in _entry.GetHistoryOrdered())
                    history.Add(h);
            }

            // Compute X range matching OnGenerateVisualContent
            double xStart, xEnd;
            if (UseFrameAxis)
            {
                double minF = double.MaxValue, maxF = double.MinValue;
                foreach (var h in history) { double v = h.FrameCount; if (v < minF) minF = v; if (v > maxF) maxF = v; }
                double span = maxF - minF;
                if (span < 1) span = 1;
                xStart = minF - span * 0.05;
                xEnd = maxF + span * 0.05;
            }
            else
            {
                double now = EditorApplication.timeSinceStartup;
                double preferredStart = now - TimeRange;
                double dataEarliest = double.MaxValue, dataLatest = double.MinValue;
                foreach (var h in history) { if (h.Timestamp < dataEarliest) dataEarliest = h.Timestamp; if (h.Timestamp > dataLatest) dataLatest = h.Timestamp; }
                if (dataLatest >= preferredStart) { xStart = preferredStart; xEnd = now; }
                else { double sp = dataLatest - dataEarliest; if (sp < 0.01) sp = 1.0; xStart = dataEarliest - sp * 0.05; xEnd = dataLatest + sp * 0.1; }
            }
            double xDur = xEnd - xStart;
            if (xDur < 0.001) xDur = 1.0;

            float tx = (mousePos.x - left) / plotW;
            double targetX = xStart + tx * xDur;

            // Find closest history entry
            WatchHistoryEntry closest = default;
            double closestDist = double.MaxValue;
            foreach (var h in history)
            {
                double dist = Math.Abs(GetXValue(h) - targetX);
                if (dist < closestDist) { closestDist = dist; closest = h; }
            }

            if (closestDist < double.MaxValue)
            {
                double ago = EditorApplication.timeSinceStartup - closest.Timestamp;
                string info = UseFrameAxis
                    ? $"{closest.FormattedValue}  (F{closest.FrameCount}, {ago:F1}s ago)"
                    : $"{closest.FormattedValue}  ({ago:F1}s ago, F{closest.FrameCount})";
                _tooltipLabel.text = info;
                _tooltipLabel.style.display = DisplayStyle.Flex;
                _tooltipLabel.style.left = Mathf.Min(mousePos.x + 10, rect.width - 160);
                _tooltipLabel.style.top = Mathf.Max(mousePos.y - 20, 0);
            }
            else
            {
                _tooltipLabel.style.display = DisplayStyle.None;
            }
        }

        // --- Helpers ---

        private static bool TryParseVectorComponents(string formatted, out float[] components)
        {
            components = null;
            if (string.IsNullOrEmpty(formatted)) return false;

            // Expected format: "(x, y)" or "(x, y, z)"
            string trimmed = formatted.Trim();
            if (trimmed.Length < 3) return false;
            if (trimmed[0] == '(') trimmed = trimmed.Substring(1);
            if (trimmed[trimmed.Length - 1] == ')') trimmed = trimmed.Substring(0, trimmed.Length - 1);

            string[] parts = trimmed.Split(',');
            var result = new List<float>();
            for (int i = 0; i < parts.Length; i++)
            {
                if (float.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float val))
                {
                    result.Add(val);
                }
                else return false;
            }

            components = result.ToArray();
            return components.Length >= 2;
        }
    }
}
