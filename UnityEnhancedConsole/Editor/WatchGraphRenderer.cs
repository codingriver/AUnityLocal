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

        // --- State ---
        private WatchEntry _entry;
        private VisualElement _graphElement;
        private Label _tooltipLabel;
        private float _hoverX = -1f;

        public VisualElement Build()
        {
            var container = new VisualElement();
            container.AddToClassList("watch-graph-container");
            container.style.flexGrow = 1;
            container.style.minHeight = 60;

            _graphElement = new VisualElement();
            _graphElement.AddToClassList("watch-graph");
            _graphElement.style.flexGrow = 1;
            _graphElement.generateVisualContent += OnGenerateVisualContent;
            _graphElement.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            _graphElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
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

            double now = EditorApplication.timeSinceStartup;
            double preferredStart = now - TimeRange;

            // Adaptive time window: find actual data time bounds
            double dataEarliest = double.MaxValue;
            double dataLatest = double.MinValue;
            foreach (var h in history)
            {
                if (h.Timestamp < dataEarliest) dataEarliest = h.Timestamp;
                if (h.Timestamp > dataLatest) dataLatest = h.Timestamp;
            }

            // If preferred window contains data, use it; otherwise expand to cover all history
            double timeStart, timeEnd;
            bool hasDataInWindow = dataLatest >= preferredStart;
            if (hasDataInWindow)
            {
                timeStart = preferredStart;
                timeEnd = now;
            }
            else
            {
                // All data is older than preferred window — show all history with 10% right margin
                double span = dataLatest - dataEarliest;
                if (span < 0.01) span = 1.0; // avoid zero-width window
                timeStart = dataEarliest - span * 0.05;
                timeEnd = dataLatest + span * 0.1;
            }
            double timeDuration = timeEnd - timeStart;
            if (timeDuration < 0.001) timeDuration = 1.0;

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
            DrawGrid(painter, left, top, right, bottom, plotW, plotH, yMin, yMax, timeStart, timeEnd);

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
                DrawVectorLines(painter, history, left, top, right, bottom, plotW, plotH, yMin, yMax, timeStart, timeDuration);
            }
            else
            {
                DrawSingleLine(painter, history, left, top, right, bottom, plotW, plotH, yMin, yMax, timeStart, timeDuration, ValueLineColor);
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

                float tx = (float)((h.Timestamp - timeStart) / timeDuration);
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
                float tx = (float)((h.Timestamp - timeStart) / timeDuration);

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

        // --- Interaction ---

        private void OnMouseMove(MouseMoveEvent evt)
        {
            _hoverX = evt.localMousePosition.x;
            _graphElement?.MarkDirtyRepaint();
            UpdateTooltip(evt.localMousePosition);
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _hoverX = -1f;
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

            // Compute the same adaptive time window as OnGenerateVisualContent
            double now = EditorApplication.timeSinceStartup;
            double preferredStart = now - TimeRange;
            double dataEarliest = double.MaxValue;
            double dataLatest = double.MinValue;
            foreach (var h in _entry.GetHistoryOrdered())
            {
                if (h.Timestamp < dataEarliest) dataEarliest = h.Timestamp;
                if (h.Timestamp > dataLatest) dataLatest = h.Timestamp;
            }

            double timeStart, timeEnd;
            if (dataLatest >= preferredStart)
            {
                timeStart = preferredStart;
                timeEnd = now;
            }
            else
            {
                double span = dataLatest - dataEarliest;
                if (span < 0.01) span = 1.0;
                timeStart = dataEarliest - span * 0.05;
                timeEnd = dataLatest + span * 0.1;
            }
            double timeDuration = timeEnd - timeStart;
            if (timeDuration < 0.001) timeDuration = 1.0;

            float tx = (mousePos.x - left) / plotW;
            double targetTime = timeStart + tx * timeDuration;

            // Find closest history entry
            WatchHistoryEntry closest = default;
            double closestDist = double.MaxValue;
            foreach (var h in _entry.GetHistoryOrdered())
            {
                double dist = Math.Abs(h.Timestamp - targetTime);
                if (dist < closestDist) { closestDist = dist; closest = h; }
            }

            if (closestDist < double.MaxValue)
            {
                double ago = now - closest.Timestamp;
                _tooltipLabel.text = $"{closest.FormattedValue}  ({ago:F1}s ago)";
                _tooltipLabel.style.display = DisplayStyle.Flex;
                _tooltipLabel.style.left = Mathf.Min(mousePos.x + 10, rect.width - 120);
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
