# UnityEnhancedConsole 功能说明书

## 1. 概述
UnityEnhancedConsole 是一个 Unity Editor 扩展，旨在提供一个比原生控制台更强大、更高效的增强版 Console 窗口。它在编辑器启动后自动监听所有日志（包括来自非主线程的日志）并实时持久化到本地文件。该工具提供了深度搜索、多维过滤、灵活的标签管理、多种格式导出、变量监视与实时图表、远程日志与监视以及便捷的文本处理能力。UI 基于 Unity 的 UI Toolkit 构建，在保持与原生控制台视觉风格一致的同时，提供了更流畅的交互体验。
- **窗口克隆 (Clone Window)**: 支持通过 `CloneWindow()` 创建当前窗口的副本。克隆窗口会通过 `CopyStateFrom()` 完整复制源窗口的所有状态，包括搜索词、过滤器设置、标签状态、折叠模式等。

## 2. 目录结构与模块职责

### 2.1 程序集定义
- **Editor/UnityEnhancedConsole.asmdef**
  - 编辑器程序集，包含所有 Editor 模块。设置 `includePlatforms: ["Editor"]`，并引用 Runtime 程序集。
- **Runtime/UnityEnhancedConsole.Runtime.asmdef**
  - 运行时程序集，包含远程通信协议和客户端。可在构建后的运行时环境中使用。

### 2.2 核心日志模块 (Editor)
- **EnhancedConsoleWindow.cs**
  - 核心窗口类，负责 UI 构建、日志采集、搜索/过滤逻辑、折叠管理、远程连接管理以及菜单功能的实现。支持 `CloneWindow()` 复制当前窗口状态。
- **EnhancedConsoleWindow.TagBar.cs**
  - 标签栏专用逻辑，处理标签的包含/排除切换、排序、搜索和 UI 绑定。
- **EnhancedConsoleData.cs**
  - 数据结构定义中心，包含 `LogEntry`、`TagRule` 以及用于导出的 `ExportEntry`。
- **EnhancedConsoleLogFile.cs**
  - 日志持久化层，负责监听 `Application.logMessageReceivedThreaded`，处理文件写入缓冲、轮转及加载。支持尾部读取优化以提升大文件加载性能。
- **EnhancedConsoleTagLogic.cs**
  - 标签计算引擎，执行自动标签识别和用户自定义正则规则。内置规则缓存和编译正则缓存，避免重复解析。
- **EnhancedConsoleBuildPreprocess.cs**
  - 实现 `IPreprocessBuildWithReport` 接口，在项目构建开始前，若开启了 Clear On Build，则自动清空相关控制台窗口。

### 2.3 变量监视模块 (Editor)
- **WatchData.cs**
  - 监视数据结构定义，包含 `WatchEntry`（单个监视变量）和 `WatchHistoryEntry`（历史快照）。支持堆栈追踪记录。
- **Watch.cs**
  - 静态 API 入口，提供 `Set()` / `SetFloat()` / `SetInt()` / `SetBool()` 等方法供业务代码调用。所有方法支持 `captureStack` 参数控制堆栈捕获。
- **WatchManager.cs**
  - 监视管理器核心，维护所有 `WatchEntry` 的注册、更新、历史记录与环形缓冲区管理。提供 `SetCaptureStackTrace()` 动态控制堆栈捕获。
- **WatchPanel.cs**
  - 监视面板 UI，包含变量列表、历史记录列表、实时图表、堆栈追踪显示区域。支持时间轴/帧号轴切换和游标拖拽。
- **WatchPanel.uss**
  - 监视面板样式表，定义图表、堆栈区域、游标标签、轴切换按钮等视觉样式。
- **WatchGraphRenderer.cs**
  - 图表渲染器，基于 `VisualElement.generateVisualContent` 绘制数值曲线。支持时间轴/帧号轴双模式、拖拽游标、悬停 Tooltip 和最近点自动吸附。
- **WatchSettingsWindow.cs**
  - 监视设置窗口，配置历史记录容量等参数。
- **WatchTestRunner.cs / WatchEditorTest.cs**
  - 监视功能测试工具和单元测试。

### 2.4 远程通信模块
- **Runtime/RemoteConsoleProtocol.cs**
  - 共享通信协议，定义长度前缀 JSON 消息格式。支持日志消息 (`Log`) 和监视消息 (`Watch`) 两种类型。提供 `WriteMessage()` / `ReadMessage()` 流式读写方法。
- **Runtime/RemoteConsoleClient.cs**
  - 运行时 TCP 客户端 (`MonoBehaviour`)。在构建后的应用中自动连接编辑器服务端，转发 `Application.logMessageReceived` 日志和 `Watch` 监视数据。支持自动重连机制。
- **Editor/RemoteConsoleServer.cs**
  - 编辑器端 TCP 服务端 (`[InitializeOnLoad]`)。监听来自远程客户端的连接，接收日志和监视数据后注入到本地日志系统和 `WatchManager`（带 `[Remote]` 前缀标识）。提供客户端连接/断开事件。

### 2.5 工具窗口 (Editor)
- **CapacitySettingsWindow.cs**
  - 提供最大内存日志数和文件加载上限的配置界面。
- **SearchFilterRangeWindow.cs**
  - 时间、日志编号、帧号等范围过滤器的设置弹窗。
- **StringProcessorWindow.cs**
  - 独立的文本处理工具，可通过 `OpenWithText(string text)` 静态方法由外部程序注入文本进行处理。
- **TagRulesWindow.cs**
  - 标签规则编辑窗口。

### 2.6 UI 资源 (Editor)
- **EnhancedConsoleWindow.uxml / .uss**
  - 定义窗口的 UI 布局结构与视觉样式。

### 2.7 测试 (Editor)
- **EnhancedConsoleTestRunner.cs**
  - 提供用于验证窗口功能的测试日志输出工具。
- **EnhancedConsoleEditorTest.cs**
  - NUnit 单元测试文件。目前该文件中的所有测试方法均处于注释状态。

## 3. 核心数据结构

### 3.1 LogEntry
记录单条日志的完整信息：
- **Condition / StackTrace / LogType**: 核心日志内容。
- **Count**: 折叠模式下的累计重复次数（UI 显示上限为 9999）。
- **TimeStamp / FirstTimeStamp / LastTimeStamp**: 记录首次和末次出现的时间。
- **FrameCount**: 记录发生时的 `Time.frameCount`（仅限主线程）。
- **MessageNumber**: 自动递增的唯一日志编号。
- **Tags**: 该条日志所属的所有标签。

### 3.2 TagRule
定义标签的自动匹配规则：
- **matchType**: 支持 包含、正则表达式、前缀、后缀 匹配。
- **matchTarget**: 可指定仅匹配消息内容、仅匹配堆栈或两者皆匹配。

### 3.3 ExportEntry
专为 CSV/JSON 导出设计的轻量化结构：
- **MessageNumber (int)**
- **LogType (string)**
- **Count (int)**
- **TimeStamp, FirstTimeStamp, LastTimeStamp (string, 格式化)**
- **FrameCount (int)**
- **Condition (string)**
- **StackTrace (string)**
- **Tags (string, 逗号分隔)**

### 3.4 WatchEntry
记录单个监视变量的完整信息：
- **Name**: 变量名（唯一标识符）。
- **FormattedValue**: 当前值的格式化字符串。
- **NumericValue / HasNumericValue**: 数值类型的原始值（用于图表绘制）。
- **ValueType**: 值类型枚举 (`WatchValueType`: Auto / Float / Int / Bool / String)。
- **Format**: 自定义格式字符串（如 `"F2"`）。
- **CaptureStackTrace**: 是否捕获堆栈追踪。
- **History[]**: 环形缓冲区，存储历史快照（容量可配置）。
- **LastUpdateTime**: 最后更新时间戳。

### 3.5 WatchHistoryEntry
变量的单个历史快照：
- **Timestamp**: 记录时间 (`double`, 秒)。
- **FrameCount**: 记录帧号。
- **FormattedValue**: 该时刻的格式化值。
- **NumericValue / HasNumericValue**: 数值信息。
- **StackTrace**: 该时刻的调用堆栈（仅当 `CaptureStackTrace` 开启时记录）。

### 3.6 RemoteConsoleProtocol 消息格式
远程通信使用长度前缀 JSON 协议：
- **Header**: 4 字节大端整数，表示消息体 UTF-8 字节长度。
- **Body**: UTF-8 编码的 JSON 字符串。
- **消息类型**:
  - `Log`: 包含 `condition`、`stackTrace`、`logType`、`timestamp`。
  - `Watch`: 包含 `name`、`value`、`valueType`、`format`、`captureStack`、`timestamp`、`frameCount`。

## 4. 日志持久化与文件格式
- **路径**: `Project/Logs/EnhancedConsole.log`。
- **写入机制**:
  - `WriteBufferFlushCount = 50`: 缓冲区达到 50 条日志时刷新。
  - `WriteBufferFlushIntervalSec = 1.5f`: 每隔 1.5 秒强制刷新缓冲区。
  - 通过 `EditorApplication.update` 驱动定期刷新检查。
- **存储格式**: 使用管道符分隔，Condition 和 StackTrace 经过 Base64 编码。
- **文件轮转**: 每次启动（pid 变更）时备份旧日志为 `EnhancedConsole_yyyyMMdd_HHmmss.log`。
- **尾部读取优化**: 加载大文件时仅读取尾部最新数据（按 `MaxLoadEntries` 限制），避免全量解析。

## 5. 窗口功能与交互

### 5.1 工具栏 (Toolbar)
- **Clear**: 清空日志。若存在编译错误且 `_currentCycleHasErrors` 为 true，清空时保留编译错误日志。
- **Collapse (折叠控制)**:
  - **快速切换**: 点击按钮在"关闭"与"上次使用的折叠模式"间切换。
  - **下拉菜单**: 提供 Off / Adjacent / Global 三种模式。
- **Clear On Play / Clear On Build**: 自动清理选项。Clear On Build 通过 `EnhancedConsoleBuildPreprocess` 实现。
- **View Lock (视图锁定)**: 开启后，新日志仍进入 `PendingEntries`，但阻止 `RefreshUI` 调用。解锁后触发全量刷新。
- **Log/Warning/Error 数量**: 数量超过 9999 显示 "9999+"。
- **Remote (远程连接)**: 点击显示远程连接菜单，可启动/停止远程监听服务，查看当前连接的客户端数量。

### 5.2 搜索栏 (Search Bar)
- **输入防抖**: `SearchDebounceDelay = 0.35f`。停止输入 0.35s 后应用过滤。
- **文本高亮**: 匹配项使用 `<color=#FFEB3B>` (黄色) 高亮。
- **范围过滤**: 支持时间 (T)、编号 (N)、帧号 (F) 过滤。
  - **默认值**: 自动基于现有日志提取最早/最晚时间、最大编号等。
  - **状态反馈**: 激活时应用 `.search-filter-active` 样式，按钮文字更新（如 "Filter: T,N"）。

### 5.3 标签栏 (Tag Bar)
- **颜色系统**: 8 种内置色，基于标签名称 Hash 自动分配。
- **排除指示**: 排除模式下标签按钮显示红点。
- **清除按钮**: 拆分按钮下拉菜单：
  - **Clear+**: 清除包含标签。
  - **Clear-**: 清除排除标签。
  - **Clear All**: 清除全部标签过滤。

### 5.4 日志列表 (Log List)
- **显示配置**: 每个条目显示 1-10 行 Condition，高度随之调整（基础 32px + 15px/行）。
- **快捷键**: 支持 **Ctrl+C** (macOS Cmd+C) 复制选中项。
- **右键上下文菜单**:
  - Copy Selected / Copy Selected with Timestamp
  - Copy All Visible / Copy All Visible with Timestamp
  - Export (All/Filtered as TXT/CSV/JSON)

### 5.5 详情区 (Detail Panel)
- **交互优化**: 禁用 `selectAllOnFocus` 和 `selectAllOnMouseUp`；区分点击与拖拽操作。
- **堆栈跳转**: 支持两种解析格式：
  - `(at relativePath:lineNumber)` (Unity)
  - `in absolutePath:lineNumber` (.NET)

### 5.6 窗口克隆 (Window Clone)
通过 `CloneWindow()` 创建实例并调用 `CopyStateFrom()` 同步源窗口状态。

### 5.7 菜单功能
- **hamburger 菜单**: 包含导出、范围过滤配置。
- **日志文件入口**: 提供 Open Editor Log 和 Open Player Log。

## 6. 变量监视系统 (Watch)

### 6.1 概述
变量监视系统允许开发者在代码中标记需要实时追踪的变量值，并在编辑器面板中以列表、历史记录和实时图表的形式可视化展示。

### 6.2 API 使用
```csharp
// 基础用法
Watch.Set("PlayerHP", hp);                           // 自动类型推断
Watch.SetFloat("Speed", speed, "F2");                // 指定格式
Watch.SetInt("Score", score);                        // 整数
Watch.SetBool("IsGrounded", isGrounded);             // 布尔

// 启用堆栈捕获
Watch.Set("CriticalValue", value, captureStack: true);
Watch.SetFloat("FPS", fps, "F1", captureStack: true);

// 动态控制堆栈捕获
WatchManager.SetCaptureStackTrace("PlayerHP", true);
```

### 6.3 监视面板
- **变量列表**: 显示所有注册的监视变量及其当前值。
- **历史记录**: 选中变量后展示其值变化的完整历史。
- **实时图表**: 数值型变量自动绘制趋势曲线。
  - 悬停显示 Tooltip（值、时间/帧号）。
  - 最近点自动吸附。
- **堆栈追踪区域**: 选中历史记录条目时显示该时刻的调用堆栈（需启用 `captureStack`）。
  - 支持右键菜单 "Toggle Stack Capture" 动态开关。

### 6.4 时间轴与帧号轴
- **双轴模式**: 通过面板中的轴切换按钮 (`⏱/🎞`) 在时间轴和帧号轴之间切换。
- **拖拽游标 (Scrubber)**: 在图表上点击或拖拽可定位到任意历史时刻。
  - 游标上方显示当前值标签。
  - 拖拽时自动选中对应的历史记录条目。
  - 游标吸附至最近的数据点。
- **帧号模式**: X 轴显示帧号而非时间，适合分析逐帧数据变化。

### 6.5 环形缓冲区
历史记录使用固定容量的环形缓冲区，容量可通过 `WatchSettingsWindow` 配置。超出容量时自动覆盖最旧数据。

## 7. 远程日志与监视

### 7.1 概述
远程功能允许构建后的应用通过 TCP 网络将日志和监视数据实时发送到编辑器，类似 Unity Console 的远程调试功能。

### 7.2 架构
```
[构建应用 (Runtime)]                    [Unity 编辑器 (Editor)]
RemoteConsoleClient  ─── TCP ───>  RemoteConsoleServer
  ├─ 转发 Application.log              ├─ 注入日志 (带 [Remote] 前缀)
  └─ 转发 Watch.Set() 数据             └─ 注入 WatchManager (带 [Remote] 前缀)
```

### 7.3 编辑器端 (Server)
- **自动加载**: 使用 `[InitializeOnLoad]` 在编辑器启动时初始化。
- **启动/停止**: 通过工具栏 Remote 按钮控制，或在窗口首次打开时自动启动。
- **默认端口**: 可配置，默认使用 TCP 监听。
- **多客户端**: 支持同时连接多个远程客户端。
- **事件通知**:
  - `OnClientConnected(string clientId)`: 客户端连接。
  - `OnClientDisconnected(string clientId)`: 客户端断开。
  - `OnClientCountChanged(int count)`: 连接数变化。
- **消息处理**: 在 `EditorApplication.update` 中处理待处理消息队列 (`ProcessPendingMessages`)，确保在主线程执行。

### 7.4 运行时端 (Client)
- **MonoBehaviour**: `RemoteConsoleClient` 作为 MonoBehaviour 挂载到场景中。
- **自动重连**: 连接断开后自动尝试重连。
- **日志转发**: 自动挂钩 `Application.logMessageReceived`，将所有日志发送至编辑器。
- **监视转发**: 在运行时调用的 `Watch.Set()` 数据自动打包为 `Watch` 类型消息发送。
- **DontDestroyOnLoad**: 确保跨场景持续工作。

### 7.5 协议细节
- **传输层**: TCP 长连接。
- **消息格式**: 4 字节大端长度头 + UTF-8 JSON 体。
- **线程安全**: 发送操作通过锁保护，接收在独立线程中运行。

## 8. 过滤与折叠逻辑
- **初始化**: 打开范围过滤器时，通过 `GetDefaultTimeRangeFromLogs()` 等方法预填范围。
- **折叠模式**:
  - Off: 按原始顺序显示。
  - Adjacent: 仅折叠相邻重复项。
  - Global: 全局哈希聚合重复项。

## 9. 导出
- 导出包含 `ExportEntry` 中定义的完整字段，支持所有日志或仅过滤结果。

## 10. 性能优化

### 10.1 标签规则缓存
- 标签规则从 `EditorPrefs` 加载后缓存在内存中，不再每次 `ComputeTags()` 都重新解析 JSON。
- 正则表达式规则编译后缓存 (`Regex` 对象复用)，避免重复编译开销。
- 缓存在规则变更时自动失效并重建。

### 10.2 日志文件尾部读取
- 加载大日志文件时，使用尾部读取策略：从文件末尾向前扫描，仅解析最新的 `MaxLoadEntries` 条记录。
- 避免对大文件进行全量读取和解析，显著减少加载时间和内存占用。

### 10.3 FlushBuffer 复用
- `FlushPendingEntries()` 使用复用的 `_flushBuffer` 列表，将 `PendingEntries` 快速交换到本地缓冲区后释放锁。
- 减少每次刷新时的内存分配和 GC 压力。

### 10.4 HasAnyTag 零分配优化
- `LogEntry.HasAnyTag()` 使用零分配实现，避免在高频过滤操作中产生临时集合。

## 11. 偏好设置 (EditorPrefs)
- 存储折叠模式、自动清理设置、堆栈设置、范围过滤器状态、最大容量、标签偏好、远程服务端配置等。

## 12. 工具窗口
- **CapacitySettingsWindow**: 配置 `MaxEntries` 与 `MaxLoadEntries`。
- **WatchSettingsWindow**: 配置监视变量历史记录容量等参数。
- **StringProcessorWindow**: 文本处理。支持 `OpenWithText(string text)` 注入文本。
- **TagRulesWindow**: 定义标签匹配规则。

## 13. 测试辅助
- 提供多种测试日志输出（Run Test Logs / Tag Test Logs）。
- `EnhancedConsoleEditorTest.cs` 包含测试桩，但目前处于非活跃状态。
- `WatchTestRunner.cs` / `WatchEditorTest.cs` 提供监视功能的测试工具和单元测试。

## 14. 行为细节与边界
- **刷新节流**: `MinRepaintIntervalMs = 50`。
- **线程安全**: `PendingEntries` 受 `lock` 保护；主线程 ID 校验确保安全访问 Unity API。远程消息队列同样受锁保护。
- **编译错误处理**: 挂钩 `CompilationPipeline`。编译开始时执行清理（若启用），标记并保护编译错误日志。
- **视图锁定细节**: 锁定期间日志持续累积至内存，但 UI 层面的 ListView 刷新被挂起。
- **远程标识**: 所有来自远程客户端的日志和监视变量名称自动添加 `[Remote]` 前缀，便于区分本地与远程数据。

## 15. 关键常量参考
- `DefaultMaxEntries = 20000`
- `DefaultMaxLoadEntries = 50000`
- `MaxCopyLines = 100000`
- `MaxSearchHistory = 20`
- `MinRepaintIntervalMs = 50`
- `SearchDebounceDelay = 0.35f`
- `WriteBufferFlushCount = 50`
- `WriteBufferFlushIntervalSec = 1.5f`
