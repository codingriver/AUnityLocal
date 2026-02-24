using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 变量监视值类型枚举
    /// </summary>
    public enum WatchValueType
    {
        String = 0,
        Integer = 1,
        Float = 2,
        Boolean = 3,
        Vector = 4,
        Color = 5,
        Object = 6
    }

    /// <summary>
    /// 变量监视历史记录条目（环形缓冲区元素）
    /// </summary>
    [Serializable]
    public struct WatchHistoryEntry
    {
        public double Timestamp;
        public int FrameCount;
        public string FormattedValue;
        public double NumericValue;
        public bool HasNumericValue;
    }

    /// <summary>
    /// 变量监视条目
    /// </summary>
    [Serializable]
    public class WatchEntry
    {
        public string Name;
        public string Group;
        public string DisplayName;
        public object CurrentValue;
        public string FormattedValue;
        public string Format;
        public WatchValueType ValueType;
        public bool IsPaused;

        // Auto-watch
        public Func<object> Getter;
        public WeakReference Owner;

        // History ring buffer
        public WatchHistoryEntry[] History;
        public int HistoryHead;
        public int HistoryCount;

        // Change tracking
        public string PreviousFormattedValue;
        public double LastChangeTime;
        public int ChangeCount;

        /// <summary>
        /// 解析名称路径，以 '/' 分隔为 Group 和 DisplayName
        /// 例如 "Player/Health" => Group="Player", DisplayName="Health"
        /// </summary>
        public static (string group, string displayName) ParseName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return ("", name ?? "");
            int idx = name.LastIndexOf('/');
            if (idx < 0)
                return ("", name);
            return (name.Substring(0, idx), name.Substring(idx + 1));
        }

        /// <summary>
        /// 记录历史（每次调用都记录，用于图表连续采样；值变化时额外更新 ChangeCount）
        /// </summary>
        public void RecordHistory(string newFormattedValue, double numericValue, bool hasNumeric)
        {
            if (History == null || History.Length == 0)
                return;

            double now = UnityEditor.EditorApplication.timeSinceStartup;
            bool changed = !string.Equals(newFormattedValue, PreviousFormattedValue, StringComparison.Ordinal);

            History[HistoryHead] = new WatchHistoryEntry
            {
                Timestamp = now,
                FrameCount = WatchManager.IsMainThread ? Time.frameCount : 0,
                FormattedValue = newFormattedValue,
                NumericValue = numericValue,
                HasNumericValue = hasNumeric
            };
            HistoryHead = (HistoryHead + 1) % History.Length;
            if (HistoryCount < History.Length)
                HistoryCount++;

            if (changed)
            {
                PreviousFormattedValue = newFormattedValue;
                LastChangeTime = now;
                ChangeCount++;
            }
        }

        /// <summary>
        /// 按时间顺序获取历史记录
        /// </summary>
        public IEnumerable<WatchHistoryEntry> GetHistoryOrdered()
        {
            if (History == null || HistoryCount == 0)
                yield break;
            int start = HistoryCount < History.Length ? 0 : HistoryHead;
            for (int i = 0; i < HistoryCount; i++)
            {
                yield return History[(start + i) % History.Length];
            }
        }
    }
}
