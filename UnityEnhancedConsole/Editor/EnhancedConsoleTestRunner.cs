using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 增强型 Console 测试用例：通过菜单触发各类日志，用于验证面板功能。
    /// </summary>
    public static class EnhancedConsoleTestRunner
    {
        [MenuItem("Tools/Enhanced Console/运行测试日志 (Run Test Logs)", false, 100)]
        public static void RunTestLogs()
        {
            // 1. 普通消息
            Debug.Log("【测试】普通消息 - 单行");
            Debug.Log("【测试】多行消息\n第二行\n第三行");

            // 2. 警告
            Debug.LogWarning("【测试】这是一条警告消息");

            // 3. 错误（会带堆栈，便于验证“点击跳转”）
            Debug.LogError("【测试】这是一条错误消息");

            // 4. 重复消息（用于验证 Collapse：开启折叠后应显示 [N]）
            for (int i = 0; i < 5; i++)
                Debug.Log("【测试】重复消息 - 用于验证折叠 (Collapse)");

            // 5. 带 context 的日志（在自带 Console 中点击可高亮对象；本面板仅显示文本）
            var go = new GameObject("EnhancedConsoleTestObject");
            Debug.Log("【测试】带 Context 的 Log", go);
            Debug.LogWarning("【测试】带 Context 的 Warning", go);

            // 6. 可搜索的关键字
            Debug.Log("【测试】搜索关键字: UNIQUE_SEARCH_123");
            Debug.LogWarning("【测试】时间戳示例: " + System.DateTime.Now.ToString("HH:mm:ss.fff"));

            // 7. 特殊字符与换行测试
            // 7.1 真实换行符 \n
            Debug.Log("【换行-标准】第一行\n第二行\n第三行");

            // 7.2 Windows 风格换行 \r\n（真实换行），以及仅测试字面量 \"\\r\"（避免 Unity 对单独 \r 的特殊处理干扰日志显示）
            Debug.Log("【换行-Windows】Line1\r\nLine2\r\nLine3");
            // 字面量 \\r：应完整显示为 \"LineA\\rLineB\\rLineC\"，而不是被当作换行或截断
            Debug.Log("【换行-仅字面\\\\r】LineA\\rLineB\\rLineC");

            // 7.3 字面量 \\n 与 \\r（应按普通字符串显示）
            Debug.Log("【字面量】这里是字面 \\n 和 \\r，不是换行：foo\\nbar\\rbaz");
            Debug.Log("【混合】真实换行前\n字面 \\n 中间\n真实换行后，结尾有\\r");

            // 7.4 含反斜杠路径（验证带 \\ 的长路径不会在消息列表错乱）
            Debug.Log("【路径】C:\\\\Program Files\\\\JetBrains\\\\JetBrains Rider 2022.2.3\\\\plugins\\\\rider-unity\\\\EditorPlugin\\\\JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll");
            Debug.Log("【路径-复杂】D:\\\\Work Space\\\\Project\\\\Foo\\\\Bar\\\\baz_qux\\\\bin\\\\Debug\\\\net6.0\\\\Foo.Bar.dll");
            Debug.Log("【路径-尾反斜杠】E:\\\\Games\\\\Unity\\\\Projects\\\\MyGame\\\\");

            // 7.5 路径 + 多行
            Debug.Log(
                "【路径+多行】加载 Rider 插件:\n" +
                "  路径: C:\\\\Program Files\\\\JetBrains\\\\JetBrains Rider 2022.2.3\\\\plugins\\\\rider-unity\\\\EditorPlugin\\\\JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll\n" +
                "  状态: 已加载"
            );

            // 7.6 尖括号与颜色标签（验证富文本高亮与特殊字符组合）
            Debug.Log("【尖括号】这是包含 <tag> 的消息，不应打断搜索高亮逻辑。");
            Debug.Log("【Rider路径高亮】Rider EditorPlugin loaded from C:\\\\Program Files\\\\JetBrains\\\\JetBrains Rider 2022.2.3\\\\plugins\\\\rider-unity\\\\EditorPlugin");

            Debug.Log("【测试】全部测试日志已输出。请打开 Window > General > Enhanced Console 查看。");
            void Test()
            {
                Debug.Log("【Test。");
            }
Debug.LogWarning(@"
## 测试用例

### 1. 编辑器菜单测试（无需进入播放）

- 打开 **Window > General > Enhanced Console**。
- 菜单 **Tools > Enhanced Console > 运行测试日志 (Run Test Logs)**。
- 会输出：普通消息、多行消息、警告、错误、重复消息、带 Context 的日志、可搜索关键字等，用于验证列表、搜索、折叠、详情与堆栈跳转。

### 2. 运行时场景测试（播放模式）

- 将 `Assets/EnhancedConsoleTestHelper.cs` 挂到场景中任意物体上。
- 开启 **Clear On Play** 时，进入播放会先清空再输出 Start 日志。
- **Start**：自动输出一批 Log/Warning/Error 与重复消息。
- **E 键**：输出一条错误，用于验证 **Error Pause**（先勾选 Error Pause 再按 E）。
- **R 键**：输出 5 条相同消息，用于验证 **Collapse**（开启折叠后应显示 [5]）。
- **B 键**：输出一批混合日志。");
            Test();
            Test2();
        }

       static void Test2()
        {
            Debug.Log("【Test2。");
        }

        [MenuItem("Tools/Enhanced Console/运行错误暂停测试 (需先开启 Error Pause 并进入播放)", false, 101)]
        public static void RunErrorPauseTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("【测试】Error Pause 测试需在播放模式下运行。请先进入播放，再在场景中点击触发错误的物体，或使用运行时测试脚本。");
                return;
            }
            Debug.LogError("【测试】Error Pause - 若已开启 Error Pause，此时应暂停播放");
        }

        [MenuItem("Tools/Enhanced Console/运行标签测试日志 (Tag)", false, 102)]
        public static void RunTagTestLogs()
        {
            // 生成 100 个不同标签名 Tag001..Tag100
            string[] tagNames = new string[100];
            for (int i = 0; i < 100; i++)
                tagNames[i] = "Tag" + (i + 1).ToString("000");

            // 每个标签单独一条日志，使标签栏出现 100 个不同标签
            for (int i = 0; i < 100; i++)
                Debug.Log("[" + tagNames[i] + "] 单标签 " + (i + 1));

            // 同一行 20 个标签
            string line20 = "";
            for (int i = 0; i < 20; i++)
                line20 += "[" + tagNames[i] + "]";
            Debug.Log(line20 + " 一行20个标签");

            // 再几条一行多标签（10 个、15 个）
            string line10 = "";
            for (int i = 20; i < 30; i++)
                line10 += "[" + tagNames[i] + "]";
            Debug.Log(line10 + " 一行10个标签");
            string line15 = "";
            for (int i = 30; i < 45; i++)
                line15 += "[" + tagNames[i] + "]";
            Debug.LogWarning(line15 + " 一行15个标签(警告)");

            // 原有单标签 / 多行等示例
            Debug.Log("[Network] 连接成功");
            Debug.Log("[UI] 界面加载完成");
            Debug.Log("[TagTest] [Auto] 自动识别方括号标签");
            Debug.Log("[FirstLine] 第一行有标签\n第二行无标签");
            Debug.Log("[Start] 开始\n[Middle] 中间\n[End] 结束");

            Debug.Log("【标签测试】共 100 个不同标签，含一行 20 个标签等；请在 Enhanced Console 标签栏查看/筛选与换行。");
        }
    }
}
