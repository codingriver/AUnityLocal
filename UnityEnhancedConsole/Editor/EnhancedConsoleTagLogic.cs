using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 标签计算：自动识别（方括号、LogType、堆栈命名空间）+ 自定义规则；规则持久化。
    /// </summary>
    public static class EnhancedConsoleTagLogic
    {
        private const string PrefTagRules = "EnhancedConsole.TagRules";
        private const string PrefAutoBracket = "EnhancedConsole.AutoTagBracket";
        private const string PrefBracketFirstLineOnly = "EnhancedConsole.BracketFirstLineOnly";
        private const string PrefAutoStack = "EnhancedConsole.AutoTagStack";

        private static readonly Regex BracketRegex = new Regex(@"\[([^\]]+)\]", RegexOptions.Compiled);
        private static readonly Regex StackClassRegex = new Regex(@"^\s*(at|in)\s+([^\s()]+)", RegexOptions.Compiled | RegexOptions.Multiline);

        public static bool AutoTagBracket { get => EditorPrefs.GetBool(PrefAutoBracket, true); set => EditorPrefs.SetBool(PrefAutoBracket, value); }
        /// <summary> true = 只识别首行，false = 识别全部内容 </summary>
        public static bool BracketTagFirstLineOnly { get => EditorPrefs.GetBool(PrefBracketFirstLineOnly, false); set => EditorPrefs.SetBool(PrefBracketFirstLineOnly, value); }
        public static bool AutoTagStack { get => EditorPrefs.GetBool(PrefAutoStack, false); set => EditorPrefs.SetBool(PrefAutoStack, value); }

        /// <summary>
        /// 为一条日志计算并写入 Tags（自动识别 + 自定义规则，去重）。
        /// </summary>
        public static void ComputeTags(LogEntry entry)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (entry.Tags == null) entry.Tags = new List<string>();
            else entry.Tags.Clear();

            if (AutoTagBracket && !string.IsNullOrEmpty(entry.Condition))
            {
                string textToScan = entry.Condition;
                if (BracketTagFirstLineOnly)
                {
                    int firstNewLine = entry.Condition.IndexOfAny(new[] { '\r', '\n' });
                    textToScan = firstNewLine >= 0 ? entry.Condition.Substring(0, firstNewLine) : entry.Condition;
                }
                foreach (Match m in BracketRegex.Matches(textToScan))
                {
                    if (m.Success && m.Groups.Count > 1)
                    {
                        string tag = m.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(tag)) set.Add(tag);
                    }
                }
            }

            if (AutoTagStack && !string.IsNullOrEmpty(entry.StackTrace))
            {
                var lines = entry.StackTrace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var m = StackClassRegex.Match(line);
                    if (m.Success && m.Groups.Count > 2)
                    {
                        string full = m.Groups[2].Value;
                        int lastDot = full.LastIndexOf('.');
                        string segment = lastDot >= 0 ? full.Substring(lastDot + 1) : full;
                        if (!string.IsNullOrEmpty(segment) && segment.Length < 40)
                            set.Add(segment);
                    }
                }
            }

            foreach (var rule in LoadRules())
            {
                if (string.IsNullOrEmpty(rule.tagName)) continue;
                if (MatchesRule(entry, rule))
                    set.Add(rule.tagName);
            }

            entry.Tags.Clear();
            entry.Tags.AddRange(set);
        }

        private static bool MatchesRule(LogEntry entry, TagRule rule)
        {
            string cond = entry.Condition ?? "";
            string stack = entry.StackTrace ?? "";
            if (rule.matchTarget == (int)TagMatchTarget.ConditionOnly)
                return MatchText(cond, rule);
            if (rule.matchTarget == (int)TagMatchTarget.StackTraceOnly)
                return MatchText(stack, rule);
            return MatchText(cond, rule) || MatchText(stack, rule);
        }

        private static bool MatchText(string text, TagRule rule)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(rule.matchContent)) return false;
            switch (rule.matchType)
            {
                case (int)TagMatchType.Contains:
                    return text.IndexOf(rule.matchContent, StringComparison.OrdinalIgnoreCase) >= 0;
                case (int)TagMatchType.Regex:
                    try { return Regex.IsMatch(text, rule.matchContent); } catch { return false; }
                case (int)TagMatchType.Prefix:
                    return text.StartsWith(rule.matchContent, StringComparison.OrdinalIgnoreCase);
                case (int)TagMatchType.Suffix:
                    return text.EndsWith(rule.matchContent, StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        public static List<TagRule> LoadRules()
        {
            var list = new List<TagRule>();
            string json = EditorPrefs.GetString(PrefTagRules, "");
            if (string.IsNullOrEmpty(json)) return list;
            try
            {
                var wrapper = JsonUtility.FromJson<TagRuleListWrapper>(json);
                if (wrapper?.rules != null) list.AddRange(wrapper.rules);
            }
            catch { }
            return list;
        }

        public static void SaveRules(List<TagRule> rules)
        {
            var wrapper = new TagRuleListWrapper { rules = rules ?? new List<TagRule>() };
            EditorPrefs.SetString(PrefTagRules, JsonUtility.ToJson(wrapper));
        }

        [Serializable]
        private class TagRuleListWrapper { public List<TagRule> rules; }
    }
}
