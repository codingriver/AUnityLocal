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
        public string FirstTimeStamp;
        public string LastTimeStamp;
        public int FrameCount;   // 来自 Time.frameCount（播放时有效）
        public int MessageNumber; // 消息编号，从 1 递增，Clear 后从 1 重新开始
        // 标签集合（自动识别 + 自定义规则）。为不可变数组，空时复用 EmptyTags 不分配。
        // 注意：写入端必须整体替换数组（不能 in-place 修改），以保证后台过滤线程读到的是稳定快照。
        public string[] Tags;

        // 预计算的 hash：用于 LogKey/Collapse 加速；Condition/StackTrace/LogType 任一变化时由写入端置 0 重算。
        [NonSerialized] public int CachedKeyHash;

        // 渲染层缓存（仅 Editor UI 使用）：cached msg label text（已含前缀 + GetFirstLines + 高亮 rich-text）。
        // CachedDisplayVersion 与 EnhancedConsoleWindow._displayCacheVersion 一致时即可直接复用。
        [NonSerialized] public string CachedDisplayText;
        [NonSerialized] public int CachedDisplayVersion;

        public static readonly string[] EmptyTags = new string[0];

        public string FullMessage => string.IsNullOrEmpty(StackTrace) ? Condition : Condition + "\n" + StackTrace;

        /// <summary> 仅用于读取：返回非 null 的 Tags 引用。不分配。 </summary>
        public string[] TagsOrEmpty => Tags ?? EmptyTags;

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
            var tags = Tags;
            if (tags == null || tags.Length == 0 || selectedTags == null) return false;
            foreach (var t in selectedTags)
            {
                if (string.IsNullOrEmpty(t)) continue;
                for (int i = 0; i < tags.Length; i++)
                {
                    if (string.Equals(t, tags[i], StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
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
        public int matchType;   // 0=包含 1=正则 2=前缀 3=后缀 4=RegexCapture
        public int matchTarget; // 0=仅消息 1=仅堆栈 2=两者
        public string matchContent;
        public bool disabled;   // true=禁用此规则；旧数据反序列化默认为 false（启用），兼容旧规则
    }

    public enum TagMatchType { Contains = 0, Regex = 1, Prefix = 2, Suffix = 3, RegexCapture = 4 }
    public enum TagMatchTarget { ConditionOnly = 0, StackTraceOnly = 1, Both = 2 }

    /// <summary>
    /// 自动识别标签的过滤策略设置
    /// </summary>
    [Serializable]
    public class TagFilterSettings
    {
        /// <summary> 忽略纯数字标签 </summary>
        public bool ignorePureNumber = true;
        /// <summary> 忽略时间格式标签（如 11:00:46.485） </summary>
        public bool ignoreTimeFormat = true;
        /// <summary> 最小有效标签长度，低于此值忽略 </summary>
        public int minTagLength = 1;
        /// <summary> 最大有效标签长度，高于此值忽略（0=不限制） </summary>
        public int maxTagLength = 0;
        /// <summary> 自定义忽略正则表达式列表 </summary>
        public List<string> ignorePatterns = new List<string>();
    }
}
