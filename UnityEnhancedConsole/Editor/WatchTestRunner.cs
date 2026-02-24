using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 变量监视功能手动测试运行器。
    /// 通过菜单触发各类 Watch 操作，用于验证面板 UI 显示。
    /// </summary>
    public static class WatchTestRunner
    {
        // ─── 基础 API 测试 ────────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/1. 基础类型监视", false, 200)]
        public static void RunBasicTypes()
        {
            Watch.Clear();

            // 基础类型
            Watch.Set("字符串", "Hello World");
            Watch.Set("浮点数", 3.14159f);
            Watch.Set("整数", 42);
            Watch.Set("布尔值", true);
            Watch.Set("向量3", new Vector3(1.5f, 2.5f, 3.5f));
            Watch.Set("向量2", new Vector2(100f, 200f));
            Watch.Set("颜色", Color.cyan);
            Watch.Set("空值", (object)null);

            Debug.Log("【Watch测试】基础类型监视已设置。请切换到 Watch 标签页查看 8 个条目。");
        }

        [MenuItem("Tools/Enhanced Console/Watch 测试/2. 分组与路径", false, 201)]
        public static void RunGroupPaths()
        {
            Watch.Clear();

            // 多层分组
            Watch.Set("Player/Health", 100);
            Watch.Set("Player/Mana", 50);
            Watch.Set("Player/Speed", 5.2f);
            Watch.Set("Player/IsAlive", true);
            Watch.Set("Player/Position", new Vector3(10, 0, 5));

            Watch.Set("Enemy/Count", 3);
            Watch.Set("Enemy/NearestDist", 15.7f);
            Watch.Set("Enemy/Boss/Health", 5000);
            Watch.Set("Enemy/Boss/Phase", 2);

            Watch.Set("System/FPS", 60);
            Watch.Set("System/Memory", "256 MB");
            Watch.Set("System/DrawCalls", 128);

            // 无分组
            Watch.Set("GlobalTimer", 0f);

            Debug.Log("【Watch测试】分组与路径已设置。应看到 Player(5) / Enemy(4) / System(3) 三个分组 + 1 个无分组条目。点击分组名可折叠/展开。");
        }

        [MenuItem("Tools/Enhanced Console/Watch 测试/3. 值变化与闪烁", false, 202)]
        public static void RunValueChanges()
        {
            // 连续改变值，验证闪烁动画和变化计数
            Watch.Set("Counter", 0);

            EditorApplication.CallbackFunction updater = null;
            int frame = 0;
            updater = () =>
            {
                frame++;
                if (frame <= 20)
                {
                    Watch.Set("Counter", frame);
                    Watch.Set("Ping", Random.Range(10f, 200f));
                    Watch.Set("Status", frame % 2 == 0 ? "Active" : "Idle");
                }
                else
                {
                    EditorApplication.update -= updater;
                    Debug.Log("【Watch测试】值变化序列结束。Counter 应显示 20 次变化，每次值变化应有黄色闪烁。");
                }
            };
            EditorApplication.update += updater;

            Debug.Log("【Watch测试】开始值变化测试（20帧）。观察闪烁动画和趋势箭头。");
        }

        [MenuItem("Tools/Enhanced Console/Watch 测试/4. 自动监视 (Auto)", false, 203)]
        public static void RunAutoWatch()
        {
            Watch.Clear();

            // Auto-watch: Lambda 自动求值
            Watch.Auto("Auto/Time", () => EditorApplication.timeSinceStartup, null, "F1");
            Watch.Auto("Auto/Random", () => Random.Range(0, 100), null);
            Watch.Auto("Auto/Platform", () => Application.platform, null);

            Debug.Log("【Watch测试】自动监视已注册。Auto/Time 和 Auto/Random 应持续更新（随 Editor Update 刷新）。");
        }

        // ─── 历史与图表测试 ───────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/5. 历史记录", false, 210)]
        public static void RunHistoryTest()
        {
            Watch.Clear();
            Watch.Set("History/Value", 0f);

            EditorApplication.CallbackFunction updater = null;
            int frame = 0;
            updater = () =>
            {
                frame++;
                float val = Mathf.Sin(frame * 0.1f) * 100f;
                Watch.Set("History/Value", val);
                Watch.Set("History/Step", frame);

                if (frame >= 100)
                {
                    EditorApplication.update -= updater;
                    Debug.Log("【Watch测试】历史记录测试结束。选中 History/Value 应在历史面板看到 100 条记录。点击 Graph 按钮可切换为图表视图。");
                }
            };
            EditorApplication.update += updater;

            Debug.Log("【Watch测试】开始历史记录测试（100帧正弦波形）。选中条目后在右侧查看历史列表。");
        }

        [MenuItem("Tools/Enhanced Console/Watch 测试/6. 向量图表", false, 211)]
        public static void RunVectorGraphTest()
        {
            Watch.Clear();
            Watch.Set("VectorGraph/Position", Vector3.zero);

            EditorApplication.CallbackFunction updater = null;
            int frame = 0;
            updater = () =>
            {
                frame++;
                float t = frame * 0.05f;
                Watch.Set("VectorGraph/Position", new Vector3(
                    Mathf.Cos(t) * 5f,
                    Mathf.Sin(t * 2f) * 3f,
                    Mathf.Sin(t * 0.5f) * 10f
                ));

                if (frame >= 150)
                {
                    EditorApplication.update -= updater;
                    Debug.Log("【Watch测试】向量图表测试结束。选中 VectorGraph/Position 后点击 Graph 应看到 R/G/B 三条分量曲线。");
                }
            };
            EditorApplication.update += updater;

            Debug.Log("【Watch测试】开始向量图表测试（150帧三维圆周运动）。");
        }

        // ─── 特殊显示测试 ────────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/7. 布尔与颜色显示", false, 220)]
        public static void RunSpecialDisplay()
        {
            Watch.Clear();

            // 布尔指示器
            Watch.Set("Flags/IsReady", true);
            Watch.Set("Flags/IsLoading", false);
            Watch.Set("Flags/HasError", true);
            Watch.Set("Flags/IsConnected", false);

            // 颜色色块
            Watch.Set("Colors/Red", Color.red);
            Watch.Set("Colors/Green", Color.green);
            Watch.Set("Colors/Blue", Color.blue);
            Watch.Set("Colors/Custom", new Color(0.5f, 0.2f, 0.8f, 1f));
            Watch.Set("Colors/Transparent", new Color(1f, 1f, 0f, 0.3f));

            Debug.Log("【Watch测试】布尔与颜色显示测试。布尔值左侧应有绿/灰圆点指示器，颜色值左侧应有色块预览。");
        }

        [MenuItem("Tools/Enhanced Console/Watch 测试/8. 趋势箭头", false, 221)]
        public static void RunTrendArrows()
        {
            Watch.Clear();

            // 先设初始值
            Watch.Set("Trend/Rising", 10f);
            Watch.Set("Trend/Falling", 100f);
            Watch.Set("Trend/Stable", 50f);

            // 延迟一帧后改变
            EditorApplication.delayCall += () =>
            {
                Watch.Set("Trend/Rising", 20f);   // ↑
                Watch.Set("Trend/Falling", 80f);   // ↓
                Watch.Set("Trend/Stable", 50f);    // 无变化 → 无箭头

                Debug.Log("【Watch测试】趋势箭头测试。Rising 应显示 ▲，Falling 应显示 ▼，Stable 无箭头。");
            };
        }

        // ─── 搜索与过滤 ──────────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/9. 搜索过滤", false, 230)]
        public static void RunSearchFilter()
        {
            Watch.Clear();

            Watch.Set("Network/Ping", 35);
            Watch.Set("Network/PacketLoss", 0.02f);
            Watch.Set("Network/BytesIn", 1024);
            Watch.Set("Render/FPS", 60);
            Watch.Set("Render/DrawCalls", 256);
            Watch.Set("Audio/Volume", 0.8f);

            Debug.Log("【Watch测试】搜索过滤测试。在搜索框输入 'Network' 应只显示 3 个条目，输入 'FPS' 应显示 1 个。");
        }

        // ─── 暂停与恢复 ──────────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/10. 暂停与恢复", false, 240)]
        public static void RunPauseResume()
        {
            Watch.Clear();
            Watch.Set("PauseTest/AutoValue", 0);

            EditorApplication.CallbackFunction updater = null;
            int frame = 0;
            updater = () =>
            {
                frame++;
                Watch.Set("PauseTest/AutoValue", frame);

                // 第 10 帧暂停
                if (frame == 10)
                {
                    Watch.SetPaused("PauseTest/AutoValue", true);
                    Debug.Log("【Watch测试】第10帧暂停。值应停在 10。");
                }

                // 第 30 帧恢复
                if (frame == 30)
                {
                    Watch.SetPaused("PauseTest/AutoValue", false);
                    Debug.Log("【Watch测试】第30帧恢复。值应跳到 30 并继续更新。");
                }

                if (frame >= 50)
                {
                    EditorApplication.update -= updater;
                    Debug.Log("【Watch测试】暂停与恢复测试结束。最终值应为 50。");
                }
            };
            EditorApplication.update += updater;

            Debug.Log("【Watch测试】开始暂停/恢复测试（第10帧暂停，第30帧恢复）。");
        }

        // ─── 大批量压力测试 ──────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/11. 压力测试 (1000条)", false, 250)]
        public static void RunStressTest()
        {
            Watch.Clear();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                Watch.Set($"Stress/Item_{i:D4}", Random.Range(0f, 1000f));
            }
            sw.Stop();

            Debug.Log($"【Watch测试】压力测试完成。1000 个条目创建耗时 {sw.ElapsedMilliseconds}ms。请检查面板滚动性能和搜索响应速度。");
        }

        // ─── 右键菜单与操作 ──────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/12. 右键菜单准备", false, 260)]
        public static void RunContextMenuPrepare()
        {
            Watch.Clear();

            Watch.Set("ContextMenu/CopyThis", "Hello World");
            Watch.Set("ContextMenu/RemoveThis", 42);
            Watch.Set("ContextMenu/PauseThis", 3.14f);

            Debug.Log("【Watch测试】右键菜单准备完成。在 Watch 面板中右键条目，测试：\n" +
                      "  - Copy Value (复制值到剪贴板)\n" +
                      "  - Copy Name (复制名称)\n" +
                      "  - Pause / Resume (暂停/恢复)\n" +
                      "  - Remove (删除条目)\n" +
                      "  - Clear All (清空全部)");
        }

        // ─── 导出测试 ────────────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/13. 导出数据准备", false, 270)]
        public static void RunExportPrepare()
        {
            Watch.Clear();

            Watch.Set("Export/PlayerHP", 100);
            Watch.Set("Export/PlayerMP", 50);
            Watch.Set("Export/EnemyCount", 3);
            Watch.Set("Export/FPS", 60.5f);
            Watch.Set("Export/Position", new Vector3(10, 0, 5));

            // 写入一些历史
            for (int i = 0; i < 5; i++)
            {
                Watch.Set("Export/FPS", 60.5f + Random.Range(-5f, 5f));
            }

            Debug.Log("【Watch测试】导出数据准备完成。请点击工具栏 Export 按钮测试 CSV 和 JSON 导出。\n" +
                      "  CSV 文件应包含表头和 5 行数据。\n" +
                      "  JSON 文件应包含完整的条目信息和历史记录。");
        }

        // ─── 设置测试 ────────────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/14. 打开设置窗口", false, 280)]
        public static void RunSettingsTest()
        {
            WatchSettingsWindow.Show();
            Debug.Log("【Watch测试】设置窗口已打开。可调整：\n" +
                      "  - 历史记录深度 (History Depth)\n" +
                      "  - 最大条目数 (Max Entries)\n" +
                      "  - 自动更新间隔 (Auto Update Interval)\n" +
                      "  - 闪烁持续时间 (Flash Duration)\n" +
                      "  - 图表时间范围 (Graph Time Range)\n" +
                      "  - 显示趋势箭头 (Show Trend Arrow)\n" +
                      "  - 持久化到文件 (Persist To File)");
        }

        // ─── 边界条件测试 ────────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/15. 边界条件", false, 290)]
        public static void RunBoundaryTests()
        {
            Watch.Clear();

            // 极长字符串
            Watch.Set("Boundary/LongString", new string('A', 500));

            // Unicode 与特殊字符
            Watch.Set("Boundary/Unicode", "你好世界🌍🎮");
            Watch.Set("Boundary/SpecialChars", "Tab\there\nNewline\"Quote\"\\Backslash");

            // 极端数值
            Watch.Set("Boundary/MaxFloat", float.MaxValue);
            Watch.Set("Boundary/MinFloat", float.MinValue);
            Watch.Set("Boundary/Infinity", float.PositiveInfinity);
            Watch.Set("Boundary/NaN", float.NaN);
            Watch.Set("Boundary/Zero", 0f);

            // 空值
            Watch.Set("Boundary/Null", (object)null);

            // 深层嵌套分组
            Watch.Set("A/B/C/D/E/F/Value", 1);

            Debug.Log("【Watch测试】边界条件测试。共 10 个条目，包含极端值、特殊字符、深层分组等。验证无异常显示。");
        }

        // ─── 清空 ─────────────────────────────────────────────

        [MenuItem("Tools/Enhanced Console/Watch 测试/-- 清空全部 Watch --", false, 300)]
        public static void ClearAllWatch()
        {
            Watch.Clear();
            Debug.Log("【Watch测试】已清空全部监视条目。");
        }
    }
}
