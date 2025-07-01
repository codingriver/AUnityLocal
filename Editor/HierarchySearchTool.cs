using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace AUnityLocal.Editor
{
    public class HierarchySearchTool : EditorWindow
    {
        
        // 扩展的标签页列表
         static string[] tabNames = new string[] {
            "名称搜索", "Layer搜索", "UI.Text搜索", "组件搜索", "批量改名","资源搜索", "标签搜索",
            "材质搜索", "预设搜索", "脚本搜索", "音频搜索", "视频搜索",
            "图片搜索", "字体搜索", "动画搜索", "控制器搜索", "粒子搜索",
            "灯光搜索", "相机搜索", "碰撞体搜索", "触发器搜索"
        };        
         private  const int TAB_LINES=4;
        // 搜索功能字段
        private string nameSearchText = "";
        private LayerMask layerMaskShow = -1;
        private LayerMask layerMask = -1;
        private List<ItemData> searchResults = new List<ItemData>();
        

        // 组件引用检查功能字段
        private string componentNameSearch = "";
        private System.Type targetComponentType = typeof(Component);
        private string targetComponentName = "Component";
        private List<Type> matchedComponentTypes = new List<Type>();
        private bool showComponentSearchResults = false;
        private List<ItemData> componentReferences = new List<ItemData>();
        private CancellationTokenSource componentCheckCancellationToken;
        private int batchSize = 200;
        private float checkInterval = 0.01f;
        private StringBuilder log = new StringBuilder();
        private string componentLogFilePath = "";
        private int gameObjectCount = 0;
        private int processedGameObjectCount = 0;
        private float componentCheckProgress = 0f;
        private string componentCheckProgressMessage = "";
        private int currentGameObjectIndex = 0;
        private Vector2 componentSearchScrollPosition;
        private Dictionary<int, int> layerDictionary = new Dictionary<int, int>();

        // 标签页相关字段
        private enum SearchTab { Name, Layer, UI, Component }
        // private SearchTab currentTab = SearchTab.Name;
        private int currentTab = 0;
        private GUIStyle tabButtonStyle;
        private GUIStyle activeTabButtonStyle;

        // 界面样式字段
        private GUIStyle sectionHeaderStyle;
        private GUIStyle searchButtonStyle;
        private GUIStyle searchButtonStyle1;
        private GUIStyle resultCountStyle;
        private GUIStyle separatorStyle;
        private Color sectionBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.1f);
        private Vector2 resultsScrollPosition;
        private bool isFirstLayout = true;

        // 新增状态显示字段
        private string statusMessage = "就绪";
        private Color statusColor = Color.green;

        // 按钮动画计时器
        private float buttonPulseTimer = 0f;
        private float buttonPulseDuration = 0.15f;
        private int originalButtonFontSize = 11;

        // 状态颜色过渡
        private float statusColorTransitionTimer = 0f;
        private float statusColorTransitionDuration = 0.3f;
        private Color statusStartColor = Color.green;
        private Color statusTargetColor = Color.green;

        private bool includeInactive = true;
        private string searchText= "";
        private bool ShowParam = false;
        private string searchTitle = "";

        
        private void DrawCurrentTabContent()
        {
            switch (currentTab)
            {
                case 0:
                    DrawCommonSection(tabNames[currentTab],"GameObject Name",SearchByName);
                    break;
                case 1:
                    DrawLayerSearchSection();
                    break;
                case 2:
                    DrawCommonSection(tabNames[currentTab],"UI.Text text",SearchUITextByText);
                    break;
                case 3:
                    DrawComponentSearchSection();
                    break;
                case 4:
                    DrawBatchRenameSection();
                    break;
                default:
                    DrawCommonSection(tabNames[0],"GameObject Name",SearchByName);
                    break;
            }
        }        
        
        [MenuItem("AUnityLocal/Hierarchy 工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<HierarchySearchTool>("Hierarchy 工具");
            window.minSize = new Vector2(800, 700); // 增大默认高度
            window.maxSize = new Vector2(1200, 1000);
        }

        private void OnEnable()
        {
            InitializeStyles();
            componentNameSearch=PlayerPrefs.GetString("ComponentNameSearch", ""); // 保存搜索组件名称
            // 初始化层字典
            for (int i = 0; i < InternalEditorUtility.layers.Length; i++)
            {
                int layer = LayerMask.NameToLayer(InternalEditorUtility.layers[i]);
                layerDictionary[i] = layer;
            }

            // 注册编辑器更新回调
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            // 移除编辑器更新回调
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // 更新状态颜色过渡
            if (statusColor != statusTargetColor)
            {
                statusColorTransitionTimer += Time.deltaTime;
                if (statusColorTransitionTimer >= statusColorTransitionDuration)
                {
                    statusColor = statusTargetColor;
                    statusColorTransitionTimer = 0f;
                }
                else
                {
                    float t = statusColorTransitionTimer / statusColorTransitionDuration;
                    statusColor = Color.Lerp(statusStartColor, statusTargetColor, t);
                }
                Repaint();
            }

            // 更新按钮动画
            if (buttonPulseTimer > 0)
            {
                buttonPulseTimer -= Time.deltaTime;
                Repaint();
            }
        }

        private void InitializeStyles()
        {
            // 主标题样式
            sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5),
                padding = new RectOffset(8, 8, 5, 5),
                normal = { textColor = new Color(0.8f, 0.9f, 1.0f) } // 浅蓝色文本
            };

            // 按钮样式
            searchButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = originalButtonFontSize,
                fontStyle = FontStyle.Bold,
                fixedHeight = 28,
            };
            searchButtonStyle1 = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = originalButtonFontSize,
                fontStyle = FontStyle.Bold,
                fixedHeight = 18,
            };
            
            // 标签页按钮样式
            tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 25,
                margin = new RectOffset(2, 2, 0, 0)
            };
            
            // 激活标签页按钮样式
            activeTabButtonStyle = new GUIStyle(tabButtonStyle)
            {
                normal = { textColor = Color.white },
                onNormal = { textColor = Color.white },
                fontStyle = FontStyle.Bold,
                // backgroundColor = new Color(0.2f, 0.4f, 0.6f)
            };

            // 结果计数样式
            resultCountStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 0.9f, 1.0f) } // 亮蓝色文本
            };

            // 分隔线样式
            separatorStyle = new GUIStyle
            {
                margin = new RectOffset(0, 0, 1, 8)
            };
        }

        // 辅助方法：创建带渐变的按钮背景
        private Texture2D CreateButtonTexture(Color baseColor)
        {
            Texture2D tex = new Texture2D(1, 1);
            Color topColor = Color.Lerp(baseColor, Color.white, 0.2f);
            Color bottomColor = Color.Lerp(baseColor, new Color(0.1f, 0.3f, 0.5f), 0.3f);
            tex.SetPixel(0, 0, new Color((topColor.r + bottomColor.r) / 2,
                (topColor.g + bottomColor.g) / 2,
                (topColor.b + bottomColor.b) / 2));
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (isFirstLayout)
            {
                isFirstLayout = false;
                minSize = new Vector2(800, 700); // 增大默认高度
            }

            DrawTitle();
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f + 10));
            DrawLeftPanel();
            EditorGUILayout.EndVertical();
            
            // 绘制分隔线
            DrawSeparator();            
            
            EditorGUILayout.BeginVertical();
            DrawResultsPanel();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();

            // // 底部状态栏固定在窗口底部
            // GUILayout.FlexibleSpace();
            DrawProgressBar();
            DrawStatusBar();
        }

        private void DrawTitle()
        {
            GUILayout.Label("Hierarchy 工具", new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                margin = new RectOffset(0, 0, 10, 15),
                normal = { textColor = new Color(0.9f, 1.0f, 1.0f) } // 亮青色标题
            });

            // 添加标题分隔线
            Rect separatorRect = GUILayoutUtility.GetRect(1, 1);
            EditorGUI.DrawRect(separatorRect, new Color(0.3f, 0.4f, 0.5f));
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(15); // 顶部留白

            DrawTabButtons();
            EditorGUILayout.Space(10);
            
            DrawCurrentTabContent();
            EditorGUILayout.Space(20);
            // 底部状态栏固定在窗口底部
            GUILayout.FlexibleSpace();            
            DrawClearButton();
            EditorGUILayout.EndVertical();
        }
        

        
        private void DrawTabButtons()
        {


            // 计算每行显示的标签数
            int tabsPerRow = Mathf.CeilToInt(tabNames.Length*1f / TAB_LINES);
            
            for (int row = 0; row < TAB_LINES; row++)
            {
                EditorGUILayout.BeginHorizontal();
        
                int startIndex = row * tabsPerRow;
                int endIndex = Mathf.Min(startIndex + tabsPerRow, tabNames.Length);
        
                for (int i = startIndex; i < endIndex; i++)
                {
                    string tabName = tabNames[i];
                    bool isActive = currentTab == i;
            
                    if (GUILayout.Button(tabName, isActive ? activeTabButtonStyle : tabButtonStyle))
                    {
                        if (currentTab != i)
                        {
                            ShowParam = false; // 切换标签时重置参数显示状态
                        }
                        currentTab = i;
                        Repaint();
                    }
                }
        
                EditorGUILayout.EndHorizontal();
            }
        }


        private void DrawCommonSection(string name,string searchDesc, Action searchAction, string buttonText = "搜索",string buttonText2 = "联合搜索")
        {
            
            DrawSectionHeader(name);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            // 搜索文本框
            string searchInput = EditorGUILayout.TextField(searchDesc, searchText);
            if (searchInput != searchText)
            {
                searchText = searchInput;
                PlayerPrefs.SetString(name + "SearchText", searchText); // 保存搜索文本
            }

            EditorGUILayout.Space(5);
            // 是否包含非激活物体的选项
            includeInactive = EditorGUILayout.Toggle("Include Inactive Objects", includeInactive);

            EditorGUILayout.Space(10);
            if (GUILayout.Button(buttonText, searchButtonStyle))
            {
                ClearResults();
                searchAction.Invoke();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawUITextSearchSection()
        {
            DrawSectionHeader("UI.Text Search");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            searchText = EditorGUILayout.TextField("text", searchText);

            EditorGUILayout.Space(5);
            // 新增：是否搜索非激活物体的选项
            includeInactive = EditorGUILayout.Toggle("包含非激活物体", includeInactive);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Search", searchButtonStyle))
            {
                ClearResults();
                SearchUITextByText();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawLayerSearchSection()
        {
            DrawSectionHeader("Layer Search");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            layerMaskShow = EditorGUILayout.MaskField("Layers", layerMaskShow, InternalEditorUtility.layers);

            string selectedLayers = GetSelectedLayers(layerMaskShow);
            EditorGUILayout.LabelField("Selected Layers", selectedLayers);

            EditorGUILayout.Space(5);
            // 新增：是否搜索非激活物体的选项
            includeInactive = EditorGUILayout.Toggle("包含非激活物体", includeInactive);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Search Layers", searchButtonStyle))
            {
                SearchByLayer();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawComponentSearchSection()
        {
            DrawSectionHeader("Component 组件引用");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索组件:", GUILayout.Width(70));
            string componentNameSearch1 = EditorGUILayout.TextField(componentNameSearch);
            if(componentNameSearch1!= componentNameSearch)
            {
                componentNameSearch = componentNameSearch1;
                PlayerPrefs.SetString("ComponentNameSearch", componentNameSearch); // 保存搜索组件名称
            }

            if (GUILayout.Button("搜索", GUILayout.Width(60)))
            {
                SearchForComponents();
            }

            EditorGUILayout.EndHorizontal();

            if (showComponentSearchResults && matchedComponentTypes.Count > 0)
            {
                EditorGUILayout.LabelField($"找到 {matchedComponentTypes.Count} 个匹配的组件:");
                componentSearchScrollPosition = EditorGUILayout.BeginScrollView(componentSearchScrollPosition,
                    GUILayout.MaxHeight(300));

                foreach (var type in matchedComponentTypes)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(type.Name);

                    if (GUILayout.Button("选择", GUILayout.Width(60)))
                    {
                        targetComponentName = type.Name;
                        targetComponentType = type;
                        showComponentSearchResults = false;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
            else if (showComponentSearchResults && matchedComponentTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到匹配的组件", MessageType.Info);
            }

            // 只有在不显示组件搜索结果时才显示这些设置
            if (!showComponentSearchResults)
            {
                if (!string.IsNullOrEmpty(targetComponentName))
                {
                    EditorGUILayout.LabelField($"当前选择: {targetComponentName}");
                }

                EditorGUI.BeginDisabledGroup(targetComponentType == typeof(Component));
                if (GUILayout.Button("检查引用", searchButtonStyle))
                {
                    if (targetComponentType == typeof(Component) || string.IsNullOrEmpty(targetComponentName))
                    {
                        EditorUtility.DisplayDialog("选择组件", "请先选择一个具体的组件类型", "确定");
                        return;
                    }

                    ClearResults();
                    SearchComponents();
                }

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawClearButton()
        {
            if (!string.IsNullOrEmpty(componentLogFilePath))
            {
                if (GUILayout.Button("打开日志文件", searchButtonStyle))
                {
                    OpenComponentLogFile();
                }
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Clear All Results", new GUIStyle(searchButtonStyle)
                {
                    normal = { textColor = Color.red }
                }))
            {
                ClearResults();
            }
        }

        private void DrawSeparator()
        {
            // 分隔线样式
            Rect separatorRect = GUILayoutUtility.GetRect(1, position.height - 80, GUILayout.Width(2));
            EditorGUI.DrawRect(separatorRect, new Color(0.3f, 0.4f, 0.5f));
        }
        private void DrawSectionHeader(string title)
        {
            // 带渐变背景的标题栏
            Rect rect = GUILayoutUtility.GetRect(1, 30);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.25f, 0.35f));

            // 标题文本添加轻微阴影效果
            EditorGUI.LabelField(rect, title, sectionHeaderStyle);
        }

        private void DrawResultsPanel()
        {
            
            EditorGUILayout.Space(15);

            DrawSectionHeader("Search Results");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            if (searchResults.Count > 0)
            {
                DrawResults(searchTitle, searchResults, DrawGameObjectResult, EditorGUIUtility.singleLineHeight);
            }
            else
            {
                EditorGUILayout.LabelField("No results to display.", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();

        }

        private void DrawProgressBar()
        {
            {
                EditorGUILayout.Space(5);
                Rect progressRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 20, 20);
                EditorGUI.ProgressBar(progressRect, componentCheckProgress,
                    $"{componentCheckProgress * 100:F1}% - {componentCheckProgressMessage}");
            }
        }

        private void DrawStatusBar()
        {
            // 状态条背景（带渐变）
            Rect statusRect = GUILayoutUtility.GetRect(1, 22);
            Color topColor = new Color(0.15f, 0.15f, 0.15f);
            Color bottomColor = new Color(0.1f, 0.1f, 0.1f);
            for (int y = 0; y < statusRect.height; y++)
            {
                float t = y / statusRect.height;
                Color color = Color.Lerp(topColor, bottomColor, t);
                EditorGUI.DrawRect(new Rect(0, statusRect.y + y, statusRect.width, 1), color);
            }

            // 状态图标
            string icon = "✓";
            if (statusColor == Color.red) icon = "✗";

            // 状态文本（带图标）
            EditorGUI.LabelField(statusRect, $"{icon}  {statusMessage}", new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                normal = { textColor = statusColor },
                margin = new RectOffset(8, 0, 0, 0)
            });
        }

        Vector2 scrollPosition = Vector2.zero;
        float maxScroll = 0;
        float viewportHeight = 0;
        float contentHeight = 0;

        private void DrawResults<T>(string title, List<T> results, Action<int, T> drawItemCallback,
            float itemHeight = 20f)
        {
            GUILayout.Label($"{title}: {results.Count}", resultCountStyle);
            itemHeight = itemHeight + 4;
            // 记录视口高度
            int startIndex = 0;
            int endIndex = results.Count - 1;
            // 开始滚动视图
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            {
                if (maxScroll > 0)
                {
                    float curHeight = scrollPosition.y;
                    // 计算当前可见范围的起始和结束索引
                    startIndex = Mathf.Max(0, Mathf.FloorToInt(curHeight / itemHeight) - 1);
                    endIndex = Mathf.Min(results.Count - 1,
                        Mathf.CeilToInt((curHeight + viewportHeight) / itemHeight) + 1);
                }

                // 开始一个垂直组以捕获内容高度
                EditorGUILayout.BeginVertical();
                {
                    if (maxScroll <= 0)
                    {
                        DrawPlaceholderBox(itemHeight * results.Count);
                        UpdateStatus("列表渲染中...", Color.yellow);
                    }
                    else
                    {
                        UpdateStatus($"找到 {searchResults.Count} 个结果", Color.cyan, false);
                        int before = startIndex;
                        DrawPlaceholderBox(itemHeight * before);
                        // 绘制显示项
                        for (int i = startIndex; i <= endIndex; i++)
                        {
                            T item = results[i];
                            if (item != null)
                            {
                                try
                                {
                                    GUILayout.Space(2);
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        drawItemCallback(i+1, item);    
                                    }
                                    GUILayout.Space(2);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError(e.ToString());
                                }
                            }
                            else
                            {
                                DrawPlaceholderBox(itemHeight);
                            }
                        }

                        int after = results.Count - endIndex - 1;
                        DrawPlaceholderBox(itemHeight * after);
                    }
                }
                EditorGUILayout.EndVertical();

                // 在 Repaint 事件中计算内容高度
                if (Event.current.type == EventType.Repaint)
                {
                    // 获取内容区域矩形
                    Rect contentRect = GUILayoutUtility.GetLastRect();
                    contentHeight = contentRect.height;
                }
            }
            EditorGUILayout.EndScrollView();

            // 获取视口高度 (滚动视图结束后的矩形高度)
            if (Event.current.type == EventType.Repaint)
            {
                Rect scrollViewRect = GUILayoutUtility.GetLastRect();
                viewportHeight = scrollViewRect.height;
            }

            // 计算滚动范围
            float minScrollY = 0;

            if (Event.current.type == EventType.Repaint)
            {
                maxScroll = Mathf.Max(0, contentHeight - viewportHeight);
                if (viewportHeight >= contentHeight)
                {
                    maxScroll = viewportHeight;
                }
            }

            // Debug.Log($"Scroll Range Y: [{minScrollY}, {maxScrollY}],cur scroll {scrollPosition.y},itemHeight:{itemHeight},startIndex:{startIndex}, endIndex:{endIndex}, viewportHeight: {viewportHeight}, contentHeight: {contentHeight},{Time.realtimeSinceStartup}");
        }

        // 绘制单个占位框
        private void DrawPlaceholderBox(float height)
        {
            if (height <= 0)
            {
                return;
            }

            GUILayout.Space(height);
            return;
        }

        private void DrawGameObjectResult(int index, ItemData data)
        {
            GUILayout.Label(index.ToString(),GUILayout.MaxWidth(40));
            EditorGUILayout.ObjectField(data.GameObject, typeof(GameObject), true);//GUILayout.ExpandWidth(false)
            if (ShowParam)
            {
                EditorGUILayout.LabelField($"{data.Param}",GUILayout.ExpandWidth(false));    
            }
            if (GUILayout.Button("Select", new GUIStyle(searchButtonStyle1)
                {
                    fixedWidth = 70,
                    margin = new RectOffset(5, 0, 0, 0)
                }))
            {
                Selection.activeGameObject = data.GameObject;
                EditorGUIUtility.PingObject(data.GameObject);
            }
            // GUILayout.FlexibleSpace();
        }

        private string GetSelectedLayers(int mask)
        {
            string layers = "";
            layerMask = 0;
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    if (layerDictionary.TryGetValue(i, out int layerIndex))
                    {
                        layers += LayerMask.LayerToName(layerIndex) + ", ";
                        layerMask |= (1 << layerIndex);
                    }
                }
            }

            if (mask == -1)
            {
                layerMask = -1;
            }

            return string.IsNullOrEmpty(layers) ? "None" : layers.TrimEnd(',', ' ');
        }

        private void SearchUITextByText()
        {
            ClearResults();
            searchTitle= "UI.Text Search Results";
            UnityEngine.UI.Text[] allObjects = FindObjectsOfType<UnityEngine.UI.Text>(includeInactive);

            foreach (var obj in allObjects)
            {
                if (obj?.text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    searchResults.Add(new ItemData(obj?.gameObject,obj.GetType().Name));
                }
            }

            ShowParam = true;
            // 添加搜索完成的状态反馈
            UpdateStatus($"找到 {searchResults.Count} 个结果", Color.cyan);
        }

        private void SearchByName()
        {
            ClearResults();
            searchTitle= "Name Search Results";
            GameObject[] allObjects = FindObjectsOfType<GameObject>(includeInactive);

            foreach (var go in allObjects)
            {
                // 新增：根据searchInactiveName决定是否包含非激活物体
                if (go.name.IndexOf(nameSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    searchResults.Add(new ItemData(go,string.Empty,string.Empty));
                }
            }
            // 添加搜索完成的状态反馈
            UpdateStatus($"找到 {searchResults.Count} 个结果", Color.cyan);
        }

        private void SearchByLayer()
        {
            ClearResults();
            searchTitle= "Layer Search Results";
            GameObject[] allObjects = FindObjectsOfType<GameObject>(includeInactive);
            foreach (var go in allObjects)
            {
                // 新增：根据searchInactiveLayer决定是否包含非激活物体
                if ((layerMask.value & (1 << go.layer)) != 0)
                {
                    searchResults.Add(new ItemData(go,string.Empty,string.Empty));
                }
            }
            
            UpdateStatus($"找到 {searchResults.Count} 个结果", Color.cyan);
        }

        private void SearchForComponents()
        {
            ClearResults();
            if (string.IsNullOrEmpty(componentNameSearch))
            {
                showComponentSearchResults = false;
                return;
            }

            matchedComponentTypes.Clear();

            try
            {
                var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch
                        {
                            return Type.EmptyTypes;
                        }
                    })
                    .Where(t => typeof(Component).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .ToList();
                foreach (var type in allTypes)
                {
                    if (type.Name.IndexOf(componentNameSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchedComponentTypes.Add(type);
                    }
                }

                matchedComponentTypes = matchedComponentTypes.OrderBy(t => t.Name).ToList();
                showComponentSearchResults = true;
                UpdateStatus($"找到 {matchedComponentTypes.Count} 个匹配组件", Color.cyan);
            }
            catch (Exception e)
            {
                Debug.LogError($"搜索组件时出错: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"搜索组件时出错: {e.Message}", "确定");
                UpdateStatus("搜索组件时出错", Color.red);
            }
        }

        private void SearchComponents()
        {
            ClearResults();
            searchTitle= "Component Search Results";
            var components = FindObjectsOfType(targetComponentType,includeInactive);
            log.Clear();
            AddLog($"组件引用检查 - {System.DateTime.Now}");
            AddLog($"目标组件: {targetComponentName} ({targetComponentType.FullName})");
            AddLog($"场景中使用组件总数: {components.Length}");
            AddLog("----------------------------------------");
            foreach (var component in components)
            {
                if (component == null || component.GetInstanceID() == 0)
                    continue;                
                var go = (component as UnityEngine.Component)?.gameObject;
                if (go == null || go.GetInstanceID() == 0)
                    continue;
                searchResults.Add(new ItemData(go,component.GetType().Name));
                AddLog($"{component.GetType().Name} 被引用 {GetGameObjectPath(go)}");
            }
            AddLog("----------------------------------------");
            AddLog($"检查完成 - {System.DateTime.Now}");
            AddLog($"发现 {searchResults.Count} 个对组件 {targetComponentName} 的引用.");

            WriteLogToFile();
            Debug.Log($"检查完成 - 发现 {searchResults.Count} 个对组件 {targetComponentName} 的引用.\n\n" +
                      $"日志已保存至: {Path.GetFullPath(componentLogFilePath)}");
            ShowParam = true;
            // 添加搜索完成的状态反馈
            UpdateStatus($"找到 {searchResults.Count} 个结果", Color.cyan);
        }

        private void AddLog(string message)
        {
            lock (log)
            {
                log.AppendLine($"{message}");
            }
        }

        private void WriteLogToFile()
        {
            try
            {
                componentLogFilePath = Path.Combine(Application.dataPath,
                    $"../AUnityLocal/ComponentReferences_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(componentLogFilePath, log.ToString());

                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误",
                    $"写入日志文件失败: {e.Message}", "确定");
                UpdateStatus("写入日志文件失败", Color.red);
            }
        }

        private void OpenComponentLogFile()
        {
            try
            {
                if (File.Exists(componentLogFilePath))
                {
                    System.Diagnostics.Process.Start(componentLogFilePath);
                    UpdateStatus("已打开日志文件", Color.green);
                }
                else
                {
                    EditorUtility.DisplayDialog("文件未找到",
                        $"日志文件不存在: {componentLogFilePath}", "确定");
                    UpdateStatus("日志文件不存在", Color.red);
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误",
                    $"打开日志文件失败: {e.Message}", "确定");
                UpdateStatus("打开日志文件失败", Color.red);
            }
        }

        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null || obj.GetInstanceID() == 0)
            {
                return "Null GameObject";
            }

            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private void ClearResults()
        {
            maxScroll = 0;
            scrollPosition = Vector2.zero;
            viewportHeight = 0;
            contentHeight = 0;
            componentReferences.Clear();
            searchResults.Clear();
            componentLogFilePath = "";
            UpdateStatus("已清除所有结果", Color.green);
        }

        // 平滑状态颜色过渡（替换协程）
        private void UpdateStatus(string message, Color? targetColor = null,bool needRepaint = true)
        {
            statusMessage = message;

            if (targetColor.HasValue)
            {
                statusStartColor = statusColor;
                statusTargetColor = targetColor.Value;
                statusColorTransitionTimer = 0f;
            }

            if (needRepaint)
            {
                Repaint();
            }
        }
        
        
// 批量改名功能所需的变量
        private Transform batchRenameRoot = null;
        private string batchRenameNewName = "";
        private int batchRenameStartIndex = 1;
        private bool batchRenameIncludeChildren = false;
        private bool batchRenameSearchExecuted = false; // 标记是否已执行搜索        
// 修改DrawBatchRenameSection方法，添加搜索按钮
        private void DrawBatchRenameSection()
        {
            DrawSectionHeader("批量改名");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            // 选择根对象
            batchRenameRoot = (Transform)EditorGUILayout.ObjectField("根对象", batchRenameRoot, typeof(Transform), true);

            EditorGUILayout.Space(5);
    
            // 搜索按钮
            EditorGUI.BeginDisabledGroup(batchRenameRoot == null);
            if (GUILayout.Button("搜索子物体", searchButtonStyle))
            {
                SearchBatchRenameChildren();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // 改名设置（仅在搜索后显示）
            if (batchRenameSearchExecuted)
            {
                // 新名称
                batchRenameNewName = EditorGUILayout.TextField("新名称", batchRenameNewName);

                // 起始序号
                batchRenameStartIndex = EditorGUILayout.IntField("起始序号", batchRenameStartIndex);

                // // 是否包含子物体
                // batchRenameIncludeChildren = EditorGUILayout.Toggle("包含所有子物体", batchRenameIncludeChildren);

                EditorGUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("预览", searchButtonStyle))
                {
                    PreviewBatchRename();
                }

                if (GUILayout.Button("执行", new GUIStyle(searchButtonStyle)
                    {
                        normal = { textColor = Color.red }
                    }))
                {
                    ExecuteBatchRename();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }
// 搜索批量改名的子物体
        private void SearchBatchRenameChildren()
        {
            if (batchRenameRoot == null)
            {
                UpdateStatus("请先选择根对象", Color.red);
                return;
            }
    
            ClearResults();
            searchTitle = "批量改名 - 子物体列表";
    
            try
            {
                Transform[] children;
        
                if (batchRenameIncludeChildren)
                {
                    // 获取所有子物体
                    children = batchRenameRoot.GetComponentsInChildren<Transform>(true);
                }
                else
                {
                    // 只获取直接子物体
                    children = new Transform[batchRenameRoot.childCount];
                    for (int i = 0; i < batchRenameRoot.childCount; i++)
                    {
                        children[i] = batchRenameRoot.GetChild(i);
                    }
                }
        
                // 添加到搜索结果
                foreach (Transform child in children)
                {
                    if (child != batchRenameRoot) // 排除根对象自身
                    {
                        searchResults.Add(new ItemData(child.gameObject, child.gameObject.activeSelf ? "活动" : "非活动"));
                    }
                }
        
                batchRenameSearchExecuted = true;
                UpdateStatus($"找到 {searchResults.Count} 个子物体", Color.cyan);
            }
            catch (Exception e)
            {
                Debug.LogError($"搜索子物体时出错: {e.Message}");
                UpdateStatus("搜索子物体时出错", Color.red);
            }
        }

// 预览批量改名
        private void PreviewBatchRename()
        {
            if (searchResults.Count == 0)
            {
                UpdateStatus("没有可重命名的对象，请先搜索子物体", Color.red);
                return;
            }
    
            if (string.IsNullOrEmpty(batchRenameNewName))
            {
                UpdateStatus("请输入新名称", Color.red);
                return;
            }
    
            try
            {
                ShowParam = true;
                // 为每个对象生成预览名称
                for (int i = 0; i < searchResults.Count; i++)
                {
                    ItemData item = searchResults[i];
                    if (item != null && item.GameObject != null)
                    {
                        item.Param = $"{batchRenameNewName}{batchRenameStartIndex + i}";
                    }
                }
        
                UpdateStatus($"预览完成: {searchResults.Count} 个对象将被重命名", Color.cyan);
            }
            catch (Exception e)
            {
                Debug.LogError($"预览批量改名时出错: {e.Message}");
                UpdateStatus("预览批量改名时出错", Color.red);
            }
        }

// 执行批量改名
        private void ExecuteBatchRename()
        {
            if (searchResults.Count == 0)
            {
                UpdateStatus("没有可重命名的对象，请先搜索子物体", Color.red);
                return;
            }
    
            if (!EditorUtility.DisplayDialog("确认批量改名", 
                    $"确定要将 {searchResults.Count} 个对象重命名吗？", 
                    "确定", "取消"))
            {
                UpdateStatus("批量改名已取消", Color.yellow);
                return;
            }
    
            try
            {
                GameObject[] gameObjects = searchResults
                    .Where(item => item != null && item.GameObject != null)
                    .Select(item => item.GameObject)
                    .ToArray();
            
                Undo.RecordObjects(gameObjects, "批量改名");
        
                for (int i = 0; i < searchResults.Count; i++)
                {
                    ItemData item = searchResults[i];
                    if (item != null && item.GameObject != null)
                    {
                        item.GameObject.name = item.Param;
                    }
                }
        
                UpdateStatus($"批量改名完成: {searchResults.Count} 个对象已重命名", Color.green);
        
                // 刷新层级视图
                EditorApplication.RepaintHierarchyWindow();
            }
            catch (Exception e)
            {
                Debug.LogError($"批量改名时出错: {e.Message}");
                UpdateStatus("批量改名时出错", Color.red);
            }
        }
        // 定义组件引用类，用于存储搜索结果
        public class ItemData
        {
            public GameObject GameObject { get; set; }
            public string GameObjectPath { get; set; }
            public string Param { get; set; }
            // 构造函数
            public ItemData(GameObject gameObject, string param="",string path="")
            {
                GameObject = gameObject;
                GameObjectPath = path;
                this.Param = param;
            }
        }
    }
}    