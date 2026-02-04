// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.Assertions;
//
// namespace UnityEnhancedConsole.Tests
// {
//     /// <summary>
//     /// 增强型 Console 编辑器单元测试（Edit Mode）。
//     /// 在 Test Runner 中运行：Window > General > Test Runner。
//     /// </summary>
//     public class EnhancedConsoleEditorTest
//     {
//         [Test]
//         public void OpenEnhancedConsole_DoesNotThrow()
//         {
//             Assert.DoesNotThrow(() =>
//             {
//                 var w = EditorWindow.GetWindow<EnhancedConsoleWindow>("Enhanced Console", false, null);
//                 Assert.That(w, Is.Not.Null);
//             });
//         }
//
//         [Test]
//         public void LogEntry_MatchesSearch_ReturnsTrue_WhenKeywordInCondition()
//         {
//             var entry = new LogEntry { Condition = "hello world", StackTrace = "" };
//             Assert.That(entry.MatchesSearch("world"), Is.True);
//             Assert.That(entry.MatchesSearch("HELLO"), Is.True);
//             Assert.That(entry.MatchesSearch(""), Is.True);
//         }
//
//         [Test]
//         public void LogEntry_MatchesSearch_ReturnsFalse_WhenKeywordNotPresent()
//         {
//             var entry = new LogEntry { Condition = "hello world", StackTrace = "" };
//             Assert.That(entry.MatchesSearch("xyz"), Is.False);
//         }
//
//         [Test]
//         public void LogEntry_IsSameContent_ReturnsTrue_WhenConditionAndTypeMatch()
//         {
//             var a = new LogEntry { Condition = "msg", StackTrace = "at X.Y()", LogType = LogType.Log };
//             var b = new LogEntry { Condition = "msg", StackTrace = "at X.Y()", LogType = LogType.Log };
//             Assert.That(a.IsSameContent(b), Is.True);
//         }
//
//         [Test]
//         public void LogEntry_IsSameContent_ReturnsFalse_WhenConditionDiffers()
//         {
//             var a = new LogEntry { Condition = "msg1", StackTrace = "", LogType = LogType.Log };
//             var b = new LogEntry { Condition = "msg2", StackTrace = "", LogType = LogType.Log };
//             Assert.That(a.IsSameContent(b), Is.False);
//         }
//
//         [Test]
//         public void LogEntry_FullMessage_ConcatenatesConditionAndStackTrace()
//         {
//             var entry = new LogEntry { Condition = "err", StackTrace = "at A.B()" };
//             Assert.That(entry.FullMessage, Is.EqualTo("err\nat A.B()"));
//         }
//
//         [Test]
//         public void LogEntry_HasAnyTag_ReturnsTrue_WhenEntryContainsSelectedTag()
//         {
//             var entry = new LogEntry { Tags = new List<string> { "Network", "UI" } };
//             Assert.That(entry.HasAnyTag(new[] { "UI" }), Is.True);
//             Assert.That(entry.HasAnyTag(new[] { "Network" }), Is.True);
//             Assert.That(entry.HasAnyTag(new[] { "ui" }), Is.True);
//         }
//
//         [Test]
//         public void LogEntry_HasAnyTag_ReturnsFalse_WhenEntryDoesNotContainSelectedTag()
//         {
//             var entry = new LogEntry { Tags = new List<string> { "Network" } };
//             Assert.That(entry.HasAnyTag(new[] { "UI" }), Is.False);
//         }
//
//         [Test]
//         public void LogEntry_HasAnyTag_ReturnsFalse_WhenTagsNullOrEmpty()
//         {
//             var entry = new LogEntry { Tags = null };
//             Assert.That(entry.HasAnyTag(new[] { "A" }), Is.False);
//             entry.Tags = new List<string>();
//             Assert.That(entry.HasAnyTag(new[] { "A" }), Is.False);
//         }
//
//         [Test]
//         public void TagLogic_ComputeTags_ExtractsBracketTags()
//         {
//             var entry = new LogEntry { Condition = "[Network] ok [UI]", StackTrace = "", LogType = LogType.Log };
//             var was = EnhancedConsoleTagLogic.AutoTagBracket;
//             EnhancedConsoleTagLogic.AutoTagBracket = true;
//             EnhancedConsoleTagLogic.ComputeTags(entry);
//             EnhancedConsoleTagLogic.AutoTagBracket = was;
//             Assert.That(entry.Tags, Does.Contain("Network"));
//             Assert.That(entry.Tags, Does.Contain("UI"));
//         }
//     }
// }
