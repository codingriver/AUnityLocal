using UnityEngine;
using UnityEditor;

public class WindowToolEditorTest : EditorWindow
{
    // 窗口分割权重（总和应为1.0）
    private float leftWeight = 0.2f;      // 20%
    private float centerLeftWeight = 0.3f; // 30%
    private float centerRightWeight = 0.3f; // 30%
    private float rightWeight = 0.2f;     // 20%
    
    // 分割线拖拽状态
    private bool isDraggingSplitter1 = false;
    private bool isDraggingSplitter2 = false;
    private bool isDraggingSplitter3 = false;
    
    // 状态信息
    private string statusInfo = "准备就绪";
    private float statusProgress = 0f;
    private bool showProgress = false;
    
    // 滚动视图位置
    private Vector2 leftScrollPos;
    private Vector2 centerLeftScrollPos;
    private Vector2 centerRightScrollPos;
    private Vector2 rightScrollPos;
    
    // 样式
    private GUIStyle headerStyle;
    private GUIStyle panelStyle;
    private GUIStyle splitterStyle;
    private GUIStyle statusStyle;
    
    [MenuItem("AUnityLocal/Window Tool Editor Test",false,99999)]
    public static void ShowWindow()
    {
        WindowToolEditorTest window = GetWindow<WindowToolEditorTest>("工具窗口");
        window.minSize = new Vector2(800, 600);
        window.Show();
    }
    
    private void OnEnable()
    {
        InitializeStyles();
        NormalizeWeights(); // 确保权重总和为1
    }
    
    // 标准化权重，确保总和为1.0
    private void NormalizeWeights()
    {
        float totalWeight = leftWeight + centerLeftWeight + centerRightWeight + rightWeight;
        if (totalWeight > 0)
        {
            leftWeight /= totalWeight;
            centerLeftWeight /= totalWeight;
            centerRightWeight /= totalWeight;
            rightWeight /= totalWeight;
        }
        else
        {
            // 如果权重都为0，设置默认值
            leftWeight = 0.25f;
            centerLeftWeight = 0.25f;
            centerRightWeight = 0.25f;
            rightWeight = 0.25f;
        }
    }
    
    private void InitializeStyles()
    {
        // 标题样式
        headerStyle = new GUIStyle();
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        
        // 面板样式
        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.padding = new RectOffset(8, 8, 8, 8);
        panelStyle.margin = new RectOffset(2, 2, 2, 2);
        
        // 分割线样式
        splitterStyle = new GUIStyle();
        splitterStyle.normal.background = EditorGUIUtility.isProSkin ? 
            CreateColorTexture(new Color(0.3f, 0.3f, 0.3f)) : 
            CreateColorTexture(new Color(0.6f, 0.6f, 0.6f));
        
        // 状态栏样式
        statusStyle = new GUIStyle(EditorStyles.helpBox);
        statusStyle.padding = new RectOffset(10, 10, 5, 5);
        statusStyle.alignment = TextAnchor.MiddleLeft;
    }
    
    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
    
    private void OnGUI()
    {
        if (headerStyle == null) InitializeStyles();
        
        DrawLayout();
    }
    
    private void DrawLayout()
    {
        Rect windowRect = new Rect(0, 0, position.width, position.height);
        
        // 绘制标题区域
        DrawHeader(windowRect);
        
        // 绘制状态栏区域
        Rect statusRect = DrawStatusBar(windowRect);
        
        // 绘制主体区域
        DrawMainContent(windowRect, statusRect);
    }
    
    private void DrawHeader(Rect windowRect)
    {
        Rect headerRect = new Rect(0, 0, windowRect.width, 30);
        
        // 绘制标题背景
        EditorGUI.DrawRect(headerRect, EditorGUIUtility.isProSkin ? 
            new Color(0.2f, 0.2f, 0.2f) : new Color(0.8f, 0.8f, 0.8f));
        
        // 绘制标题文本
        GUI.Label(headerRect, "Unity 工具窗口编辑器", headerStyle);
        
        // 绘制分割线
        Rect headerSeparator = new Rect(0, headerRect.yMax, windowRect.width, 1);
        EditorGUI.DrawRect(headerSeparator, EditorGUIUtility.isProSkin ? 
            new Color(0.1f, 0.1f, 0.1f) : new Color(0.5f, 0.5f, 0.5f));
    }
    
    private Rect DrawStatusBar(Rect windowRect)
    {
        Rect statusRect = new Rect(0, windowRect.height - 120, windowRect.width, 120);
        
        // 绘制状态栏背景
        EditorGUI.DrawRect(statusRect, EditorGUIUtility.isProSkin ? 
            new Color(0.25f, 0.25f, 0.25f) : new Color(0.9f, 0.9f, 0.9f));
        
        // 绘制顶部分割线
        Rect statusSeparator = new Rect(0, statusRect.y, windowRect.width, 1);
        EditorGUI.DrawRect(statusSeparator, EditorGUIUtility.isProSkin ? 
            new Color(0.1f, 0.1f, 0.1f) : new Color(0.5f, 0.5f, 0.5f));
        
        // 状态信息内容
        GUILayout.BeginArea(new Rect(statusRect.x + 10, statusRect.y + 10, 
            statusRect.width - 20, statusRect.height - 20));
        
        GUILayout.Label("状态信息", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // 状态文本
        GUILayout.BeginHorizontal();
        GUILayout.Label("当前状态:", GUILayout.Width(80));
        statusInfo = EditorGUILayout.TextField(statusInfo);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // 进度条控制
        GUILayout.BeginHorizontal();
        showProgress = EditorGUILayout.Toggle("显示进度:", showProgress, GUILayout.Width(100));
        if (showProgress)
        {
            statusProgress = EditorGUILayout.Slider(statusProgress, 0f, 1f);
        }
        GUILayout.EndHorizontal();
        
        // 显示进度条
        if (showProgress)
        {
            GUILayout.Space(5);
            Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(progressRect, statusProgress, $"进度: {(statusProgress * 100):F1}%");
        }
        
        // 显示当前权重信息
        GUILayout.Space(5);
        GUILayout.Label($"面板权重 - 左:{leftWeight:P0} 中左:{centerLeftWeight:P0} 中右:{centerRightWeight:P0} 右:{rightWeight:P0}", 
            EditorStyles.miniLabel);
        
        GUILayout.EndArea();
        
        return statusRect;
    }
    
    private void DrawMainContent(Rect windowRect, Rect statusRect)
    {
        float mainAreaY = 31; // 标题高度 + 分割线
        float mainAreaHeight = statusRect.y - mainAreaY;
        Rect mainRect = new Rect(0, mainAreaY, windowRect.width, mainAreaHeight);
        
        // 计算各区域位置（基于权重）
        float splitterWidth = 3f;
        float totalSplitterWidth = splitterWidth * 3; // 3个分割线
        float availableWidth = mainRect.width - totalSplitterWidth;
        
        float currentX = 0;
        
        // 左侧区域
        float leftWidth = availableWidth * leftWeight;
        Rect leftRect = new Rect(currentX, mainRect.y, leftWidth, mainRect.height);
        DrawPanel(leftRect, "左侧面板", ref leftScrollPos, DrawLeftContent);
        currentX += leftWidth;
        
        // 分割线1
        Rect splitter1 = new Rect(currentX, mainRect.y, splitterWidth, mainRect.height);
        DrawSplitter(splitter1, ref isDraggingSplitter1, 0); // 传入分割线索引
        currentX += splitterWidth;
        
        // 中间左侧区域
        float centerLeftWidth = availableWidth * centerLeftWeight;
        Rect centerLeftRect = new Rect(currentX, mainRect.y, centerLeftWidth, mainRect.height);
        DrawPanel(centerLeftRect, "中间左侧", ref centerLeftScrollPos, DrawCenterLeftContent);
        currentX += centerLeftWidth;
        
        // 分割线2
        Rect splitter2 = new Rect(currentX, mainRect.y, splitterWidth, mainRect.height);
        DrawSplitter(splitter2, ref isDraggingSplitter2, 1); // 传入分割线索引
        currentX += splitterWidth;
        
        // 中间右侧区域
        float centerRightWidth = availableWidth * centerRightWeight;
        Rect centerRightRect = new Rect(currentX, mainRect.y, centerRightWidth, mainRect.height);
        DrawPanel(centerRightRect, "中间右侧", ref centerRightScrollPos, DrawCenterRightContent);
        currentX += centerRightWidth;
        
        // 分割线3
        Rect splitter3 = new Rect(currentX, mainRect.y, splitterWidth, mainRect.height);
        DrawSplitter(splitter3, ref isDraggingSplitter3, 2); // 传入分割线索引
        currentX += splitterWidth;
        
        // 右侧区域
        float rightWidth = availableWidth * rightWeight;
        Rect rightRect = new Rect(currentX, mainRect.y, rightWidth, mainRect.height);
        DrawPanel(rightRect, "右侧面板", ref rightScrollPos, DrawRightContent);
    }
    
    private void DrawPanel(Rect rect, string title, ref Vector2 scrollPos, System.Action drawContent)
    {
        // 绘制面板背景
        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? 
            new Color(0.22f, 0.22f, 0.22f) : new Color(0.95f, 0.95f, 0.95f));
        
        GUILayout.BeginArea(rect);
        
        // 面板标题
        Rect titleRect = new Rect(0, 0, rect.width, 25);
        EditorGUI.DrawRect(titleRect, EditorGUIUtility.isProSkin ? 
            new Color(0.3f, 0.3f, 0.3f) : new Color(0.7f, 0.7f, 0.7f));
        GUI.Label(titleRect, title, EditorStyles.boldLabel);
        
        // 面板内容区域
        Rect contentRect = new Rect(5, 30, rect.width - 10, rect.height - 35);
        GUILayout.BeginArea(contentRect);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        drawContent?.Invoke();
        EditorGUILayout.EndScrollView();
        
        GUILayout.EndArea();
        GUILayout.EndArea();
    }
    
    private void DrawSplitter(Rect rect, ref bool isDragging, int splitterIndex)
    {
        // 绘制分割线
        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? 
            new Color(0.1f, 0.1f, 0.1f) : new Color(0.6f, 0.6f, 0.6f));
        
        // 处理鼠标事件
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        
        Event e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown:
                if (rect.Contains(e.mousePosition) && e.button == 0)
                {
                    isDragging = true;
                    e.Use();
                }
                break;
                
            case EventType.MouseDrag:
                if (isDragging)
                {
                    float totalSplitterWidth = 3f * 3; // 3个分割线
                    float availableWidth = position.width - totalSplitterWidth;
                    float deltaWeight = e.delta.x / availableWidth;
                    
                    // 根据分割线索引调整相应的权重
                    switch (splitterIndex)
                    {
                        case 0: // 左侧和中间左侧之间的分割线
                            AdjustWeights(ref leftWeight, ref centerLeftWeight, deltaWeight);
                            break;
                        case 1: // 中间左侧和中间右侧之间的分割线
                            AdjustWeights(ref centerLeftWeight, ref centerRightWeight, deltaWeight);
                            break;
                        case 2: // 中间右侧和右侧之间的分割线
                            AdjustWeights(ref centerRightWeight, ref rightWeight, deltaWeight);
                            break;
                    }
                    
                    NormalizeWeights();
                    Repaint();
                    e.Use();
                }
                break;
                
            case EventType.MouseUp:
                if (isDragging)
                {
                    isDragging = false;
                    e.Use();
                }
                break;
        }
    }
    
    // 调整两个相邻面板的权重
    private void AdjustWeights(ref float leftWeight, ref float rightWeight, float delta)
    {
        float minWeight = 0.05f; // 最小权重5%
        
        // 计算新的权重
        float newLeftWeight = leftWeight + delta;
        float newRightWeight = rightWeight - delta;
        
        // 确保权重在合理范围内
        if (newLeftWeight < minWeight)
        {
            newRightWeight = rightWeight - (minWeight - leftWeight);
            newLeftWeight = minWeight;
        }
        else if (newRightWeight < minWeight)
        {
            newLeftWeight = leftWeight + (rightWeight - minWeight);
            newRightWeight = minWeight;
        }
        
        // 应用新权重
        leftWeight = newLeftWeight;
        rightWeight = newRightWeight;
    }
    
    // 绘制左侧面板内容
    private void DrawLeftContent()
    {
        GUILayout.Label("左侧面板内容", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 示例内容
        GUILayout.Label("这是左侧面板的内容区域");
        
        if (GUILayout.Button("左侧按钮 1"))
        {
            Debug.Log("点击了左侧按钮 1");
        }
        
        if (GUILayout.Button("左侧按钮 2"))
        {
            Debug.Log("点击了左侧按钮 2");
        }
        
        GUILayout.Space(10);
        
        // 添加一些示例选项
        GUILayout.Label("设置选项:", EditorStyles.boldLabel);
        bool toggleValue = EditorGUILayout.Toggle("启用功能 A", true);
        bool toggleValue2 = EditorGUILayout.Toggle("启用功能 B", false);
        
        GUILayout.Space(10);
        
        // 添加滑块
        GUILayout.Label("参数调节:");
        float sliderValue = EditorGUILayout.Slider("数值 1", 0.5f, 0f, 1f);
        int intValue = EditorGUILayout.IntSlider("数值 2", 50, 0, 100);
        
        GUILayout.Space(10);
        
        // 添加文本输入
        GUILayout.Label("文本输入:");
        string textValue = EditorGUILayout.TextField("输入框", "默认文本");
        
        // 添加更多内容以测试滚动
        for (int i = 0; i < 10; i++)
        {
            GUILayout.Label($"左侧列表项 {i + 1}");
        }
    }
    
    // 绘制中间左侧面板内容
    private void DrawCenterLeftContent()
    {
        GUILayout.Label("中间左侧面板", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("这是中间左侧面板的内容");
        
        // 添加颜色选择器
        GUILayout.Label("颜色设置:", EditorStyles.boldLabel);
        Color selectedColor = EditorGUILayout.ColorField("主色调", Color.blue);
        Color secondaryColor = EditorGUILayout.ColorField("辅助色", Color.red);
        
        GUILayout.Space(10);
        
        // 添加枚举选择
        GUILayout.Label("选项设置:");
        System.Enum enumValue = EditorGUILayout.EnumPopup("模式选择", PrimitiveType.Cube);
        
        GUILayout.Space(10);
        
        // 添加对象字段
        GUILayout.Label("对象引用:");
        GameObject objRef = EditorGUILayout.ObjectField("游戏对象", null, typeof(GameObject), true) as GameObject;
        
        if (GUILayout.Button("中间左侧操作"))
        {
            Debug.Log("执行中间左侧操作");
        }
        
        GUILayout.Space(10);
        
        // 添加进度条示例
        GUILayout.Label("进度显示:");
        Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(progressRect, 0.7f, "处理进度 70%");
        
        // 添加更多测试内容
        for (int i = 0; i < 8; i++)
        {
            if (GUILayout.Button($"中左按钮 {i + 1}"))
            {
                Debug.Log($"点击了中左按钮 {i + 1}");
            }
        }
    }
    
    // 绘制中间右侧面板内容
    private void DrawCenterRightContent()
    {
        GUILayout.Label("中间右侧面板", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("这是中间右侧面板的内容");
        
        // 添加向量输入
        GUILayout.Label("向量设置:", EditorStyles.boldLabel);
        Vector3 vector3Value = EditorGUILayout.Vector3Field("位置", Vector3.zero);
        Vector2 vector2Value = EditorGUILayout.Vector2Field("大小", Vector2.one);
        
        GUILayout.Space(10);
        
        // 添加曲线编辑器
        GUILayout.Label("曲线编辑:");
        AnimationCurve curve = EditorGUILayout.CurveField("动画曲线", AnimationCurve.EaseInOut(0, 0, 1, 1));
        
        GUILayout.Space(10);
        
        // 添加范围滑块
        GUILayout.Label("范围设置:");
        float minValue = 10f, maxValue = 90f;
        EditorGUILayout.MinMaxSlider("范围", ref minValue, ref maxValue, 0f, 100f);
        GUILayout.Label($"范围: {minValue:F1} - {maxValue:F1}");
        
        GUILayout.Space(10);
        
        // 添加帮助框
        EditorGUILayout.HelpBox("这是一个帮助信息框，用于显示重要提示。", MessageType.Info);
        EditorGUILayout.HelpBox("这是一个警告信息。", MessageType.Warning);
        
        if (GUILayout.Button("中间右侧操作"))
        {
            Debug.Log("执行中间右侧操作");
        }
        
        // 添加分组
        GUILayout.Space(10);
        GUILayout.Label("分组内容:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("组内容 1");
        GUILayout.Label("组内容 2");
        if (GUILayout.Button("组内按钮"))
        {
            Debug.Log("点击了组内按钮");
        }
        EditorGUILayout.EndVertical();
    }
    
    // 绘制右侧面板内容
    private void DrawRightContent()
    {
        GUILayout.Label("右侧面板内容", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("这是右侧面板的内容区域");
        
        // 添加标签页式内容
        GUILayout.Label("工具集合:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("工具 1: 场景清理"))
        {
            Debug.Log("执行场景清理工具");
        }
        
        if (GUILayout.Button("工具 2: 资源检查"))
        {
            Debug.Log("执行资源检查工具");
        }
        
        if (GUILayout.Button("工具 3: 性能分析"))
        {
            Debug.Log("执行性能分析工具");
        }
        
        GUILayout.Space(10);
        
        // 添加信息显示区域
        GUILayout.Label("系统信息:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label($"Unity版本: {Application.unityVersion}");
        GUILayout.Label($"平台: {Application.platform}");
        GUILayout.Label($"内存使用: {(System.GC.GetTotalMemory(false) / 1024f / 1024f):F2} MB");
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // 添加日志区域
        GUILayout.Label("操作日志:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        for (int i = 0; i < 5; i++)
        {
            GUILayout.Label($"[{System.DateTime.Now.AddMinutes(-i):HH:mm}] 操作记录 {5 - i}", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // 添加快捷操作
        GUILayout.Label("快捷操作:", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("保存", GUILayout.Height(30)))
        {
            Debug.Log("执行保存操作");
        }
        if (GUILayout.Button("加载", GUILayout.Height(30)))
        {
            Debug.Log("执行加载操作");
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("重置", GUILayout.Height(30)))
        {
            Debug.Log("执行重置操作");
        }
        if (GUILayout.Button("导出", GUILayout.Height(30)))
        {
            Debug.Log("执行导出操作");
        }
        GUILayout.EndHorizontal();
        
        // 添加更多内容以测试滚动
        GUILayout.Space(10);
        GUILayout.Label("扩展功能:", EditorStyles.boldLabel);
        
        for (int i = 0; i < 6; i++)
        {
            if (GUILayout.Button($"扩展功能 {i + 1}"))
            {
                Debug.Log($"执行扩展功能 {i + 1}");
            }
        }
    }
    
    // 窗口关闭时的清理
    private void OnDestroy()
    {
        // 清理创建的纹理资源
        if (splitterStyle?.normal?.background != null)
        {
            DestroyImmediate(splitterStyle.normal.background);
        }
    }
}
