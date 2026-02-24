// ============================================================================
// WatchEditorTest.cs — 变量监视自验证测试套件
// 无 NUnit 依赖，通过 MenuItem 运行，Debug.Log 输出结果
// 菜单: Tools/Enhanced Console/Watch Tests/运行全部单元测试
// ============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEnhancedConsole
{
    public static class WatchEditorTest
    {
        private static int _passed;
        private static int _failed;
        private static int _total;
        private static readonly List<string> _failures = new List<string>();

        // ====================================================================
        // 断言工具
        // ====================================================================
        private static void AssertTrue(bool condition, string testName, string detail = null)
        {
            _total++;
            if (condition)
            {
                _passed++;
            }
            else
            {
                _failed++;
                var msg = $"FAIL: {testName}" + (detail != null ? $" — {detail}" : "");
                _failures.Add(msg);
                Debug.LogError(msg);
            }
        }

        private static void AssertFalse(bool condition, string testName, string detail = null)
            => AssertTrue(!condition, testName, detail);

        private static void AssertEqual<T>(T expected, T actual, string testName)
            => AssertTrue(EqualityComparer<T>.Default.Equals(expected, actual), testName,
                $"expected [{expected}] but got [{actual}]");

        private static void AssertNotNull(object obj, string testName)
            => AssertTrue(obj != null, testName, "was null");

        private static void AssertNull(object obj, string testName)
            => AssertTrue(obj == null, testName, $"expected null but got [{obj}]");

        private static void AssertGreaterOrEqual(int actual, int min, string testName)
            => AssertTrue(actual >= min, testName, $"expected >= {min} but got {actual}");

        private static void AssertContains(string haystack, string needle, string testName)
            => AssertTrue(haystack != null && haystack.Contains(needle), testName,
                $"expected to contain [{needle}] in [{haystack}]");

        private static void AssertNoThrow(Action action, string testName)
        {
            _total++;
            try
            {
                action();
                _passed++;
            }
            catch (Exception ex)
            {
                _failed++;
                var msg = $"FAIL: {testName} — threw {ex.GetType().Name}: {ex.Message}";
                _failures.Add(msg);
                Debug.LogError(msg);
            }
        }

        private static void Setup()
        {
            WatchManager.ClearAll();
        }

        // ====================================================================
        // 入口
        // ====================================================================
        [MenuItem("Tools/Enhanced Console/Watch Tests/运行全部单元测试", priority = 200)]
        public static void RunAll()
        {
            _passed = 0;
            _failed = 0;
            _total = 0;
            _failures.Clear();

            Debug.Log("========== 变量监视单元测试 开始 ==========");

            var sw = Stopwatch.StartNew();

            RunApiTests();
            RunDataModelTests();
            RunHistoryGraphTests();
            RunIntegrationTests();
            RunPerformanceTests();
            RunBoundaryTests();
            RunGroupTests();
            RunFormatTests();
            RunAutoTests();
            RunRemoveTests();
            RunPersistTests();

            sw.Stop();

            Debug.Log($"========== 测试完成: {_passed}/{_total} 通过, {_failed} 失败, 耗时 {sw.ElapsedMilliseconds}ms ==========");
            if (_failures.Count > 0)
            {
                Debug.LogError($"失败列表:\n  " + string.Join("\n  ", _failures));
            }
            else
            {
                Debug.Log("<color=green>全部通过!</color>");
            }

            WatchManager.ClearAll();
        }

        // ====================================================================
        // TC-API: 公共 API 测试
        // ====================================================================
        private static void RunApiTests()
        {
            // --- Set string ---
            Setup();
            Watch.Set("api/str", "hello");
            var e = WatchManager.GetEntry("api/str");
            AssertNotNull(e, "TC-API-001 Set string entry exists");
            AssertEqual("hello", e.FormattedValue, "TC-API-001 Set string value");
            AssertEqual(WatchValueType.String, e.ValueType, "TC-API-001 Set string type");

            // --- Set float ---
            Setup();
            Watch.Set("api/flt", 3.14f);
            e = WatchManager.GetEntry("api/flt");
            AssertNotNull(e, "TC-API-002 Set float entry exists");
            AssertEqual(WatchValueType.Float, e.ValueType, "TC-API-002 Set float type");
            AssertContains(e.FormattedValue, "3.14", "TC-API-002 Set float value contains 3.14");

            // --- Set float with format ---
            Setup();
            Watch.Set("api/flt1", 3.14159f, "F1");
            e = WatchManager.GetEntry("api/flt1");
            AssertEqual("3.1", e.FormattedValue, "TC-API-003 Set float F1 format");

            // --- Set int ---
            Setup();
            Watch.Set("api/num", 42);
            e = WatchManager.GetEntry("api/num");
            AssertEqual(WatchValueType.Integer, e.ValueType, "TC-API-004 Set int type");
            AssertEqual("42", e.FormattedValue, "TC-API-004 Set int value");

            // --- Set bool ---
            Setup();
            Watch.Set("api/flag", true);
            e = WatchManager.GetEntry("api/flag");
            AssertEqual(WatchValueType.Boolean, e.ValueType, "TC-API-005 Set bool type");
            AssertEqual("True", e.FormattedValue, "TC-API-005 Set bool value");

            // --- Set Vector3 ---
            Setup();
            Watch.Set("api/vec3", new Vector3(1f, 2f, 3f));
            e = WatchManager.GetEntry("api/vec3");
            AssertEqual(WatchValueType.Vector, e.ValueType, "TC-API-006 Set Vector3 type");
            AssertContains(e.FormattedValue, "1.00", "TC-API-006 Vector3 x");
            AssertContains(e.FormattedValue, "2.00", "TC-API-006 Vector3 y");

            // --- Set Vector2 ---
            Setup();
            Watch.Set("api/vec2", new Vector2(5f, 6f));
            e = WatchManager.GetEntry("api/vec2");
            AssertEqual(WatchValueType.Vector, e.ValueType, "TC-API-007 Set Vector2 type");

            // --- Set Color ---
            Setup();
            Watch.Set("api/col", Color.red);
            e = WatchManager.GetEntry("api/col");
            AssertEqual(WatchValueType.Color, e.ValueType, "TC-API-008 Set Color type");

            // --- Set object ---
            Setup();
            Watch.Set("api/obj", (object)123);
            e = WatchManager.GetEntry("api/obj");
            AssertNotNull(e, "TC-API-009 Set object entry exists");
            AssertEqual("123", e.FormattedValue, "TC-API-009 Set object value");

            // --- Remove ---
            Setup();
            Watch.Set("api/rem", "temp");
            AssertNotNull(WatchManager.GetEntry("api/rem"), "TC-API-010 Remove pre-exists");
            Watch.Remove("api/rem");
            AssertNull(WatchManager.GetEntry("api/rem"), "TC-API-010 Remove success");

            // --- Clear ---
            Setup();
            Watch.Set("a", 1);
            Watch.Set("b", 2);
            Watch.Set("c", 3);
            Watch.Clear();
            AssertEqual(0, WatchManager.EntryCount, "TC-API-011 Clear removes all");

            // --- SetPaused freeze ---
            Setup();
            Watch.Set("api/pause", 10);
            Watch.SetPaused("api/pause", true);
            Watch.Set("api/pause", 20);
            e = WatchManager.GetEntry("api/pause");
            AssertTrue(e.IsPaused, "TC-API-012 SetPaused marks paused");
            AssertEqual("10", e.FormattedValue, "TC-API-012 SetPaused freezes value");

            // --- SetPaused resume ---
            Watch.SetPaused("api/pause", false);
            Watch.Set("api/pause", 30);
            e = WatchManager.GetEntry("api/pause");
            AssertFalse(e.IsPaused, "TC-API-013 Resume unpauses");
            AssertEqual("30", e.FormattedValue, "TC-API-013 Resume accepts value");

            // --- Set string via string overload ---
            Setup();
            Watch.Set("api/sstr", "direct");
            e = WatchManager.GetEntry("api/sstr");
            AssertEqual(WatchValueType.String, e.ValueType, "TC-API-014 Set string overload type");
        }

        // ====================================================================
        // TC-DM: 数据模型测试
        // ====================================================================
        private static void RunDataModelTests()
        {
            // --- ParseName with group ---
            var (g, d) = WatchEntry.ParseName("Player/Health");
            AssertEqual("Player", g, "TC-DM-001 ParseName group");
            AssertEqual("Health", d, "TC-DM-001 ParseName display");

            // --- ParseName multi-level ---
            (g, d) = WatchEntry.ParseName("Player/Stats/Health");
            AssertEqual("Player/Stats", g, "TC-DM-002 ParseName multi-level group");
            AssertEqual("Health", d, "TC-DM-002 ParseName multi-level display");

            // --- ParseName no group ---
            (g, d) = WatchEntry.ParseName("Standalone");
            AssertEqual("", g, "TC-DM-003 ParseName no group");
            AssertEqual("Standalone", d, "TC-DM-003 ParseName no group display");

            // --- ParseName empty ---
            (g, d) = WatchEntry.ParseName("");
            AssertEqual("", g, "TC-DM-004 ParseName empty group");
            AssertEqual("", d, "TC-DM-004 ParseName empty display");

            // --- Ring buffer normal ---
            Setup();
            Watch.Set("dm/ring", 1);
            Watch.Set("dm/ring", 2);
            Watch.Set("dm/ring", 3);
            var entry = WatchManager.GetEntry("dm/ring");
            AssertEqual(3, entry.HistoryCount, "TC-DM-005 Ring buffer 3 entries");

            // --- Ring buffer only on change ---
            Setup();
            Watch.Set("dm/same", 10);
            Watch.Set("dm/same", 10);
            Watch.Set("dm/same", 10);
            entry = WatchManager.GetEntry("dm/same");
            AssertEqual(1, entry.HistoryCount, "TC-DM-006 No history for same value");

            // --- Ring buffer overflow ---
            Setup();
            WatchManager.SetHistoryDepth(5);
            for (int i = 0; i < 20; i++)
                Watch.Set("dm/overflow", i);
            entry = WatchManager.GetEntry("dm/overflow");
            AssertEqual(5, entry.HistoryCount, "TC-DM-007 Ring buffer capped at depth");
            WatchManager.SetHistoryDepth(300); // restore

            // --- Type detection float ---
            Setup();
            WatchManager.SetValue("dm/tf", 1.5f, WatchValueType.Float, null);
            AssertEqual(WatchValueType.Float, WatchManager.GetEntry("dm/tf").ValueType, "TC-DM-008 Float type");

            // --- Type detection int ---
            WatchManager.SetValue("dm/ti", 42, WatchValueType.Integer, null);
            AssertEqual(WatchValueType.Integer, WatchManager.GetEntry("dm/ti").ValueType, "TC-DM-009 Int type");

            // --- Type detection bool ---
            WatchManager.SetValue("dm/tb", true, WatchValueType.Boolean, null);
            AssertEqual(WatchValueType.Boolean, WatchManager.GetEntry("dm/tb").ValueType, "TC-DM-010 Bool type");

            // --- Type detection Vector3 ---
            WatchManager.SetValue("dm/tv", new Vector3(1, 2, 3), WatchValueType.Vector, null);
            AssertEqual(WatchValueType.Vector, WatchManager.GetEntry("dm/tv").ValueType, "TC-DM-011 Vector type");

            // --- Type detection Color ---
            WatchManager.SetValue("dm/tc", Color.blue, WatchValueType.Color, null);
            AssertEqual(WatchValueType.Color, WatchManager.GetEntry("dm/tc").ValueType, "TC-DM-012 Color type");

            // --- Type detection String ---
            WatchManager.SetValue("dm/ts", "text", WatchValueType.String, null);
            AssertEqual(WatchValueType.String, WatchManager.GetEntry("dm/ts").ValueType, "TC-DM-013 String type");

            // --- HistoryOrdered chronological ---
            Setup();
            Watch.Set("dm/chrono", "a");
            Watch.Set("dm/chrono", "b");
            Watch.Set("dm/chrono", "c");
            entry = WatchManager.GetEntry("dm/chrono");
            var history = entry.GetHistoryOrdered().ToList();
            AssertEqual(3, history.Count, "TC-DM-014 History count");
            AssertEqual("a", history[0].FormattedValue, "TC-DM-014 Oldest first");
            AssertEqual("c", history[2].FormattedValue, "TC-DM-014 Newest last");
        }

        // ====================================================================
        // TC-HG: 历史/图表数据测试
        // ====================================================================
        private static void RunHistoryGraphTests()
        {
            // --- HasNumericValue float ---
            Setup();
            Watch.Set("hg/f", 1.5f);
            var entry = WatchManager.GetEntry("hg/f");
            var h = entry.GetHistoryOrdered().First();
            AssertTrue(h.HasNumericValue, "TC-HG-001 Float HasNumericValue");
            AssertEqual(1.5, h.NumericValue, "TC-HG-001 Float NumericValue");

            // --- HasNumericValue int ---
            Setup();
            Watch.Set("hg/i", 99);
            entry = WatchManager.GetEntry("hg/i");
            h = entry.GetHistoryOrdered().First();
            AssertTrue(h.HasNumericValue, "TC-HG-002 Int HasNumericValue");
            AssertEqual(99.0, h.NumericValue, "TC-HG-002 Int NumericValue");

            // --- HasNumericValue bool ---
            Setup();
            Watch.Set("hg/b", true);
            entry = WatchManager.GetEntry("hg/b");
            h = entry.GetHistoryOrdered().First();
            AssertTrue(h.HasNumericValue, "TC-HG-003 Bool HasNumericValue");
            AssertEqual(1.0, h.NumericValue, "TC-HG-003 Bool NumericValue=1");

            // --- String no numeric ---
            Setup();
            Watch.Set("hg/s", "text");
            entry = WatchManager.GetEntry("hg/s");
            h = entry.GetHistoryOrdered().First();
            AssertFalse(h.HasNumericValue, "TC-HG-004 String no HasNumericValue");

            // --- FrameCount >= 0 ---
            Setup();
            Watch.Set("hg/fc", 42);
            entry = WatchManager.GetEntry("hg/fc");
            h = entry.GetHistoryOrdered().First();
            AssertGreaterOrEqual(h.FrameCount, 0, "TC-HG-005 FrameCount >= 0");
        }

        // ====================================================================
        // TC-INT: 集成测试 (Manager 接口)
        // ====================================================================
        private static void RunIntegrationTests()
        {
            // --- ReadEntries snapshot ---
            Setup();
            Watch.Set("int/a", 1);
            Watch.Set("int/b", 2);
            var list = WatchManager.ReadEntries();
            AssertEqual(2, list.Count, "TC-INT-001 ReadEntries snapshot count");

            // --- ReadEntries callback ---
            Setup();
            Watch.Set("int/c", 3);
            int cbCount = 0;
            WatchManager.ReadEntries((dict, keys) => { cbCount = keys.Count; });
            AssertEqual(1, cbCount, "TC-INT-002 ReadEntries callback count");

            // --- GetOrderedKeysCopy ---
            Setup();
            Watch.Set("int/x", 1);
            Watch.Set("int/y", 2);
            var keys = WatchManager.GetOrderedKeysCopy();
            AssertEqual(2, keys.Count, "TC-INT-003 GetOrderedKeysCopy count");
            AssertTrue(keys.Contains("int/x"), "TC-INT-003 Contains x");
            AssertTrue(keys.Contains("int/y"), "TC-INT-003 Contains y");

            // --- OnChanged fires ---
            Setup();
            bool changed = false;
            Action handler = () => { changed = true; };
            WatchManager.OnChanged += handler;
            Watch.Set("int/ev", 42);
            WatchManager.OnChanged -= handler;
            AssertTrue(changed, "TC-INT-004 OnChanged fires on Set");
        }

        // ====================================================================
        // TC-PERF: 性能测试
        // ====================================================================
        private static void RunPerformanceTests()
        {
            // --- Bulk 1000 entries < 1s ---
            Setup();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                Watch.Set($"perf/{i}", i);
            sw.Stop();
            AssertEqual(1000, WatchManager.EntryCount, "TC-PERF-001 1000 entries created");
            AssertTrue(sw.ElapsedMilliseconds < 1000, "TC-PERF-001 1000 entries < 1s",
                $"took {sw.ElapsedMilliseconds}ms");

            // --- High-freq update single entry ---
            Setup();
            Watch.Set("perf/hf", 0);
            sw.Restart();
            for (int i = 1; i <= 10000; i++)
                Watch.Set("perf/hf", i);
            sw.Stop();
            AssertTrue(sw.ElapsedMilliseconds < 2000, "TC-PERF-002 10000 updates < 2s",
                $"took {sw.ElapsedMilliseconds}ms");

            // --- ReadEntries 100x ---
            Setup();
            for (int i = 0; i < 100; i++)
                Watch.Set($"perf/r{i}", i);
            sw.Restart();
            for (int i = 0; i < 100; i++)
                WatchManager.ReadEntries();
            sw.Stop();
            AssertTrue(sw.ElapsedMilliseconds < 500, "TC-PERF-003 ReadEntries 100x < 500ms",
                $"took {sw.ElapsedMilliseconds}ms");
        }

        // ====================================================================
        // TC-BND: 边界测试
        // ====================================================================
        private static void RunBoundaryTests()
        {
            // --- null value ---
            Setup();
            AssertNoThrow(() => Watch.Set("bnd/null", (object)null), "TC-BND-001 null value no throw");
            var e = WatchManager.GetEntry("bnd/null");
            AssertNotNull(e, "TC-BND-001 null value entry exists");

            // --- empty name ---
            Setup();
            AssertNoThrow(() => Watch.Set("", "val"), "TC-BND-002 empty name no throw");

            // --- overwrite different type ---
            Setup();
            Watch.Set("bnd/ow", 42);
            AssertEqual(WatchValueType.Integer, WatchManager.GetEntry("bnd/ow").ValueType,
                "TC-BND-003 Overwrite initial int");
            Watch.Set("bnd/ow", "now string");
            AssertEqual(WatchValueType.String, WatchManager.GetEntry("bnd/ow").ValueType,
                "TC-BND-003 Overwrite changed to string");

            // --- long string ---
            Setup();
            var longStr = new string('X', 10000);
            AssertNoThrow(() => Watch.Set("bnd/long", longStr), "TC-BND-004 long string no throw");
            e = WatchManager.GetEntry("bnd/long");
            AssertEqual(10000, e.FormattedValue.Length, "TC-BND-004 long string preserved");

            // --- special characters ---
            Setup();
            AssertNoThrow(() => Watch.Set("bnd/特殊/字符", "值\n\t\"\\"),
                "TC-BND-005 special chars no throw");
            e = WatchManager.GetEntry("bnd/特殊/字符");
            AssertNotNull(e, "TC-BND-005 special chars entry exists");
            AssertEqual("特殊", e.Group, "TC-BND-005 special chars group");

            // --- remove nonexistent ---
            Setup();
            AssertNoThrow(() => Watch.Remove("bnd/ghost"), "TC-BND-006 remove nonexistent no throw");

            // --- pause nonexistent ---
            Setup();
            AssertNoThrow(() => Watch.SetPaused("bnd/ghost2", true),
                "TC-BND-007 pause nonexistent no throw");

            // --- get nonexistent ---
            Setup();
            AssertNull(WatchManager.GetEntry("bnd/nope"), "TC-BND-008 get nonexistent is null");

            // --- ChangeCount tracks ---
            Setup();
            Watch.Set("bnd/cc", 1);
            Watch.Set("bnd/cc", 2);
            Watch.Set("bnd/cc", 3);
            e = WatchManager.GetEntry("bnd/cc");
            AssertEqual(3, e.ChangeCount, "TC-BND-009 ChangeCount = 3");

            // --- ChangeCount no-change ---
            Setup();
            Watch.Set("bnd/nc", 10);
            Watch.Set("bnd/nc", 10);
            e = WatchManager.GetEntry("bnd/nc");
            AssertEqual(1, e.ChangeCount, "TC-BND-010 ChangeCount = 1 for same value");

            // --- ValueType overwrite ---
            Setup();
            Watch.Set("bnd/vt", 1.5f);
            AssertEqual(WatchValueType.Float, WatchManager.GetEntry("bnd/vt").ValueType,
                "TC-BND-011 Initial float type");
            Watch.Set("bnd/vt", true);
            AssertEqual(WatchValueType.Boolean, WatchManager.GetEntry("bnd/vt").ValueType,
                "TC-BND-011 Overwrite to bool type");

            // --- Unicode ---
            Setup();
            AssertNoThrow(() => Watch.Set("bnd/emoji", "测试🎮✅"),
                "TC-BND-012 Unicode no throw");
            e = WatchManager.GetEntry("bnd/emoji");
            AssertContains(e.FormattedValue, "测试", "TC-BND-012 Unicode preserved");
        }

        // ====================================================================
        // TC-GROUP: 分组测试
        // ====================================================================
        private static void RunGroupTests()
        {
            // --- group assignment ---
            Setup();
            Watch.Set("Player/HP", 100);
            var e = WatchManager.GetEntry("Player/HP");
            AssertEqual("Player", e.Group, "TC-GROUP-001 Group assignment");
            AssertEqual("HP", e.DisplayName, "TC-GROUP-001 DisplayName");

            // --- multiple same group ---
            Setup();
            Watch.Set("AI/State", "Idle");
            Watch.Set("AI/Target", "None");
            var keys = WatchManager.GetOrderedKeysCopy();
            int aiCount = keys.Count(k =>
            {
                var entry = WatchManager.GetEntry(k);
                return entry != null && entry.Group == "AI";
            });
            AssertEqual(2, aiCount, "TC-GROUP-002 Same group count");

            // --- no group ---
            Setup();
            Watch.Set("FPS", 60);
            e = WatchManager.GetEntry("FPS");
            AssertEqual("", e.Group, "TC-GROUP-003 No group");
            AssertEqual("FPS", e.DisplayName, "TC-GROUP-003 DisplayName is full name");

            // --- nested group ---
            Setup();
            Watch.Set("Game/World/Weather/Temp", 25);
            e = WatchManager.GetEntry("Game/World/Weather/Temp");
            AssertEqual("Game/World/Weather", e.Group, "TC-GROUP-004 Nested group");
            AssertEqual("Temp", e.DisplayName, "TC-GROUP-004 Nested display name");
        }

        // ====================================================================
        // TC-FORMAT: 格式化测试
        // ====================================================================
        private static void RunFormatTests()
        {
            // --- float default F2 ---
            Setup();
            Watch.Set("fmt/f", 3.14159f);
            var e = WatchManager.GetEntry("fmt/f");
            AssertEqual("3.14", e.FormattedValue, "TC-FMT-001 Float default F2");

            // --- Vector3 default ---
            Setup();
            Watch.Set("fmt/v3", new Vector3(1.5f, 2.5f, 3.5f));
            e = WatchManager.GetEntry("fmt/v3");
            AssertContains(e.FormattedValue, "1.50", "TC-FMT-002 Vector3 x format");
            AssertContains(e.FormattedValue, "2.50", "TC-FMT-002 Vector3 y format");
            AssertContains(e.FormattedValue, "3.50", "TC-FMT-002 Vector3 z format");

            // --- int no format ---
            Setup();
            Watch.Set("fmt/i", 42);
            e = WatchManager.GetEntry("fmt/i");
            AssertEqual("42", e.FormattedValue, "TC-FMT-003 Int no format");

            // --- Color format ---
            Setup();
            Watch.Set("fmt/c", Color.green);
            e = WatchManager.GetEntry("fmt/c");
            AssertContains(e.FormattedValue, "0.00", "TC-FMT-004 Color format r=0");
            AssertContains(e.FormattedValue, "1.00", "TC-FMT-004 Color format g=1");

            // --- null format fallback ---
            Setup();
            Watch.Set("fmt/nf", 3.14f, null);
            e = WatchManager.GetEntry("fmt/nf");
            AssertContains(e.FormattedValue, "3.14", "TC-FMT-005 null format uses default");
        }

        // ====================================================================
        // TC-AUTO: 自动监视测试
        // ====================================================================
        private static void RunAutoTests()
        {
            // --- Auto initial value ---
            Setup();
            int counter = 100;
            Watch.Auto("auto/counter", () => counter, null);
            // Auto values update on EditorUpdate, for now just check registration
            var e = WatchManager.GetEntry("auto/counter");
            AssertNotNull(e, "TC-AUTO-001 Auto registered");
            AssertNotNull(e.Getter, "TC-AUTO-001 Auto has getter");

            // --- Auto null getter ignored ---
            Setup();
            AssertNoThrow(() => Watch.Auto("auto/null", null, null),
                "TC-AUTO-002 null getter no throw");

            // --- Auto with owner ---
            Setup();
            var owner = ScriptableObject.CreateInstance<ScriptableObject>();
            Watch.Auto("auto/owned", () => "val", owner);
            e = WatchManager.GetEntry("auto/owned");
            AssertNotNull(e, "TC-AUTO-003 Auto with owner registered");
            AssertNotNull(e.Owner, "TC-AUTO-003 Auto has owner ref");
            UnityEngine.Object.DestroyImmediate(owner);

            // --- Auto overwrite getter ---
            Setup();
            Watch.Auto("auto/ow", () => 1, null);
            Watch.Auto("auto/ow", () => 2, null);
            e = WatchManager.GetEntry("auto/ow");
            AssertNotNull(e.Getter, "TC-AUTO-004 Overwrite getter exists");
        }

        // ====================================================================
        // TC-REMOVE: 移除测试
        // ====================================================================
        private static void RunRemoveTests()
        {
            // --- RemoveByOwner ---
            Setup();
            var owner1 = ScriptableObject.CreateInstance<ScriptableObject>();
            Watch.Auto("rm/a", () => "a", owner1);
            Watch.Auto("rm/b", () => "b", owner1);
            Watch.Set("rm/c", "c"); // no owner
            WatchManager.RemoveByOwner(owner1);
            AssertNull(WatchManager.GetEntry("rm/a"), "TC-REMOVE-001 RemoveByOwner removed a");
            AssertNull(WatchManager.GetEntry("rm/b"), "TC-REMOVE-001 RemoveByOwner removed b");
            AssertNotNull(WatchManager.GetEntry("rm/c"), "TC-REMOVE-001 Unowned kept");
            UnityEngine.Object.DestroyImmediate(owner1);

            // --- Remove null owner no crash ---
            Setup();
            AssertNoThrow(() => WatchManager.RemoveByOwner(null),
                "TC-REMOVE-002 RemoveByOwner null no throw");

            // --- Clear then reuse ---
            Setup();
            Watch.Set("rm/reuse", 1);
            Watch.Clear();
            AssertEqual(0, WatchManager.EntryCount, "TC-REMOVE-003 Clear empties");
            Watch.Set("rm/reuse", 2);
            var e = WatchManager.GetEntry("rm/reuse");
            AssertNotNull(e, "TC-REMOVE-003 Reuse after clear works");
            AssertEqual("2", e.FormattedValue, "TC-REMOVE-003 Reuse value correct");
        }

        // ====================================================================
        // TC-PERSIST: 持久化测试
        // ====================================================================
        private static void RunPersistTests()
        {
            // --- PersistToFile toggle ---
            var prevPersist = WatchManager.PersistToFile;
            WatchManager.PersistToFile = true;
            AssertTrue(WatchManager.PersistToFile, "TC-PERSIST-001 PersistToFile enabled");
            WatchManager.PersistToFile = false;
            AssertFalse(WatchManager.PersistToFile, "TC-PERSIST-001 PersistToFile disabled");
            WatchManager.PersistToFile = prevPersist;

            // --- SaveAndLoad round-trip ---
            Setup();
            Watch.Set("persist/str", "hello");
            Watch.Set("persist/num", 42);
            Watch.Set("persist/flt", 3.14f);
            WatchManager.PersistToFile = true;
            WatchManager.SaveToFile();

            string path = WatchManager.PersistFilePath;
            AssertTrue(File.Exists(path), "TC-PERSIST-002 File created");

            // Clear and reload
            WatchManager.ClearAll();
            AssertEqual(0, WatchManager.EntryCount, "TC-PERSIST-002 Cleared before load");
            WatchManager.LoadFromFile();
            AssertNotNull(WatchManager.GetEntry("persist/str"), "TC-PERSIST-002 Loaded str");
            AssertNotNull(WatchManager.GetEntry("persist/num"), "TC-PERSIST-002 Loaded num");
            AssertNotNull(WatchManager.GetEntry("persist/flt"), "TC-PERSIST-002 Loaded flt");

            // Cleanup
            if (File.Exists(path)) File.Delete(path);
            WatchManager.PersistToFile = prevPersist;

            // --- No persist when disabled ---
            Setup();
            WatchManager.PersistToFile = false;
            Watch.Set("persist/no", 1);
            WatchManager.SaveToFile();
            AssertFalse(File.Exists(path), "TC-PERSIST-003 No file when disabled");
            WatchManager.PersistToFile = prevPersist;
        }
    }
}
