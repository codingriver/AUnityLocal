using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 单条日志条目
    /// </summary>
    [Serializable]
    public class LogEntry
    {
        public string Condition;
        public string StackTrace;
        public LogType LogType;
        public int Count;       // 折叠时相同消息的重复次数
        public string TimeStamp;
        public int FrameCount;   // 来自 Time.frameCount（播放时有效）
        public int MessageNumber; // 消息编号，从 1 递增，Clear 后从 1 重新开始
        public List<string> Tags; // 标签集合（自定义 + 自动识别）

        public string FullMessage => string.IsNullOrEmpty(StackTrace) ? Condition : Condition + "\n" + StackTrace;

        public List<string> TagsOrEmpty => Tags ?? (Tags = new List<string>());

        /// <summary> 仅按日志内容（Condition）匹配，不搜索堆栈。useRegex 为 true 时按正则匹配，否则按普通关键字（忽略大小写）。 </summary>
        public bool MatchesSearch(string search, bool useRegex = false)
        {
            if (string.IsNullOrEmpty(search)) return true;
            if (Condition == null) return false;
            if (useRegex)
            {
                try { return Regex.IsMatch(Condition, search); }
                catch { return false; }
            }
            return Condition.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool IsSameContent(LogEntry other)
        {
            return other != null &&
                   string.Equals(Condition, other.Condition, StringComparison.Ordinal) &&
                   string.Equals(StackTrace, other.StackTrace, StringComparison.Ordinal) &&
                   LogType == other.LogType;
        }

        public bool HasAnyTag(IEnumerable<string> selectedTags)
        {
            if (Tags == null || Tags.Count == 0) return false;
            foreach (var t in selectedTags)
                if (Tags.Contains(t)) return true;
            return false;
        }
    }

    /// <summary>
    /// 堆栈跟踪显示类型
    /// </summary>
    public enum StackTraceLogType
    {
        None = 0,
        ScriptOnly = 1,
        Full = 2
    }

    /// <summary>
    /// 自定义标签规则
    /// </summary>
    [Serializable]
    public class TagRule
    {
        public string tagName;
        public int matchType;   // 0=包含 1=正则 2=前缀 3=后缀
        public int matchTarget; // 0=仅消息 1=仅堆栈 2=两者
        public string matchContent;
    }

    public enum TagMatchType { Contains = 0, Regex = 1, Prefix = 2, Suffix = 3 }
    public enum TagMatchTarget { ConditionOnly = 0, StackTraceOnly = 1, Both = 2 }
}
