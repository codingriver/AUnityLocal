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

namespace AUnityLocal.Editor
{
    public class HierarchySearchTool : EditorWindow
    {
        // 搜索功能字段
        private string nameSearchText = "";
        private LayerMask layerMaskShow = -1;
        private LayerMask layerMask = -1;
        private List<GameObject> nameSearchResults = new List<GameObject>();
        private List<GameObject> layerSearchResults = new List<GameObject>();
        
        // 组件引用检查功能字段
        private string componentNameSearch = "";
        private System.Type targetComponentType = typeof(Component);
        private string targetComponentName = "Component";
        private List<Type> matchedComponentTypes = new List<Type>();
        private bool showComponentSearchResults = false;
        private List<ComponentReferenceInfo> componentReferences = new List<ComponentReferenceInfo>();
        private bool isCheckingComponents = false;
        private CancellationTokenSource componentCheckCancellationToken;
        private int batchSize = 200;
        private float checkInterval = 0.01f;
        private StringBuilder componentCheckLog = new StringBuilder();
        private string componentLogFilePath = "";
        private int gameObjectCount = 0;
        private int processedGameObjectCount = 0;
        private float componentCheckProgress = 0f;
        private string componentCheckProgressMessage = "";
        private List<GameObject> allGameObjects = new List<GameObject>();
        private int currentGameObjectIndex = 0;
        private Vector2 componentSearchScrollPosition;
        private Dictionary<int, int> layerDictionary = new Dictionary<int, int>();
        
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

        [MenuItem("AUnityLocal/Hierarchy Search Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<HierarchySearchTool>("Hierarchy Search");
            window.minSize = new Vector2(800, 700); // 增大默认高度
            window.maxSize = new Vector2(1200, 1000);
        }

        private void OnEnable()
        {
            InitializeStyles();
            
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
            // 更新按钮脉冲动画
            if (buttonPulseTimer > 0)
            {
                buttonPulseTimer -= Time.deltaTime;
                if (buttonPulseTimer <= 0)
                {
                    searchButtonStyle.fontSize = originalButtonFontSize;
                    Repaint();
                }
            }
            
            // 更新状态颜色过渡
            if (statusColorTransitionTimer > 0)
            {
                statusColorTransitionTimer -= Time.deltaTime;
                float t = 1 - (statusColorTransitionTimer / statusColorTransitionDuration);
                statusColor = Color.Lerp(statusStartColor, statusTargetColor, t);
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
                normal = { background = CreateButtonTexture(new Color(0.2f, 0.4f, 0.6f)) },
                hover = { background = CreateButtonTexture(new Color(0.25f, 0.45f, 0.65f)) },
                active = { background = CreateButtonTexture(new Color(0.15f, 0.35f, 0.55f)) }
            };
            searchButtonStyle1 = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = originalButtonFontSize,
                fontStyle = FontStyle.Bold,
                fixedHeight = 18,
                normal = { background = CreateButtonTexture(new Color(0.2f, 0.4f, 0.6f)) },
                hover = { background = CreateButtonTexture(new Color(0.25f, 0.45f, 0.65f)) },
                active = { background = CreateButtonTexture(new Color(0.15f, 0.35f, 0.55f)) }
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
            
            DrawLeftPanel();
            DrawResultsPanel();
            
            EditorGUILayout.EndHorizontal();
            
            DrawProgressBar();
            
            // 底部状态栏固定在窗口底部
            GUILayout.FlexibleSpace();
            DrawStatusBar();
        }

        private void DrawTitle()
        {
            // 标题区域添加渐变背景
            Rect titleRect = GUILayoutUtility.GetRect(1, 35);
            EditorGUI.DrawRect(titleRect, new Color(0.1f, 0.2f, 0.3f));
            
            GUILayout.Label("Hierarchy Search & Component Checker", new GUIStyle(EditorStyles.boldLabel)
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
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.3f + 10));
            EditorGUILayout.Space(15); // 顶部留白
            
            DrawNameSearchSection();
            EditorGUILayout.Space(20); // 分区间距
            
            DrawLayerSearchSection();
            EditorGUILayout.Space(20);
            
            DrawComponentSearchSection();
            EditorGUILayout.Space(20);
            
            DrawClearButton();
            EditorGUILayout.Space(15); // 底部留白
            
            EditorGUILayout.EndVertical();
        }

        private void DrawNameSearchSection()
        {
            DrawSectionHeader("Name Search");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);
            
            nameSearchText = EditorGUILayout.TextField("Search Text", nameSearchText);
            
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Search", searchButtonStyle))
            {
                ClearOtherResults(SearchType.Name);
                SearchByName();
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
            
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Search Layers", searchButtonStyle))
            {
                ClearOtherResults(SearchType.Layer);
                SearchByLayer();
            }
            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawComponentSearchSection()
        {
            DrawSectionHeader("Component Check");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索组件:", GUILayout.Width(70));
            componentNameSearch = EditorGUILayout.TextField(componentNameSearch);
            
            if (GUILayout.Button("搜索", GUILayout.Width(60)))
            {
                SearchForComponents();
            }
            EditorGUILayout.EndHorizontal();
            
            if (showComponentSearchResults && matchedComponentTypes.Count > 0)
            {
                EditorGUILayout.LabelField($"找到 {matchedComponentTypes.Count} 个匹配的组件:");
                componentSearchScrollPosition = EditorGUILayout.BeginScrollView(componentSearchScrollPosition, 
                    GUILayout.MaxHeight(100));
                
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
                
                batchSize = EditorGUILayout.IntSlider("批处理大小", batchSize, 10, 500);
                checkInterval = EditorGUILayout.Slider("检查间隔(秒)", checkInterval, 0.001f, 0.1f);
                
                EditorGUI.BeginDisabledGroup(isCheckingComponents || targetComponentType == typeof(Component));
                if (GUILayout.Button("检查引用", searchButtonStyle))
                {
                    if (targetComponentType == typeof(Component) || string.IsNullOrEmpty(targetComponentName))
                    {
                        EditorUtility.DisplayDialog("选择组件", "请先选择一个具体的组件类型", "确定");
                        return;
                    }
                    
                    ClearOtherResults(SearchType.Component);
                    StartComponentChecking();
                }
                EditorGUI.EndDisabledGroup();
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawClearButton()
        {
            if (GUILayout.Button("Clear All Results", new GUIStyle(searchButtonStyle)
            {
                normal = { textColor = Color.red }
            }))
            {
                ClearAllResults();
            }
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
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(15);
            
            DrawSectionHeader("Search Results");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);
            
            if (isCheckingComponents)
            {
                EditorGUILayout.LabelField($"检查中: {componentCheckProgressMessage}");
                EditorGUILayout.LabelField($"进度: {processedGameObjectCount}/{gameObjectCount} ({componentCheckProgress * 100:F1}%)");
            }
            else if (nameSearchResults.Count > 0)
            {
                DrawNameSearchResults();
            }
            else if (layerSearchResults.Count > 0)
            {
                DrawLayerSearchResults();
            }
            else if (componentReferences.Count > 0)
            {
                DrawComponentSearchResults();
            }
            else
            {
                EditorGUILayout.LabelField("No results to display.", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawNameSearchResults()
        {
            GUILayout.Label($"Name Search Results: {nameSearchResults.Count}", resultCountStyle);
            resultsScrollPosition = EditorGUILayout.BeginScrollView(resultsScrollPosition);
            
            foreach (var go in nameSearchResults)
            {
                if (go != null)
                {
                    DrawGameObjectResult(go);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawLayerSearchResults()
        {
            GUILayout.Label($"Layer Search Results: {layerSearchResults.Count}", resultCountStyle);
            resultsScrollPosition = EditorGUILayout.BeginScrollView(resultsScrollPosition);
            
            foreach (var go in layerSearchResults)
            {
                if (go != null)
                {
                    DrawGameObjectResult(go);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawComponentSearchResults()
        {
            GUILayout.Label($"Component Reference Results: {componentReferences.Count}", resultCountStyle);
            resultsScrollPosition = EditorGUILayout.BeginScrollView(resultsScrollPosition);
            
            foreach (var reference in componentReferences)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField($"游戏对象: {reference.GameObjectPath}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"组件类型: {reference.ComponentType}");
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("选择游戏对象"))
                {
                    if (reference.GameObject != null)
                    {
                        Selection.activeObject = reference.GameObject;
                        EditorGUIUtility.PingObject(reference.GameObject);
                    }
                }
                // 移除"复制路径"按钮
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清除组件结果"))
            {
                componentReferences.Clear();
            }
            
            if (GUILayout.Button("打开日志文件"))
            {
                OpenComponentLogFile();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGameObjectResult(GameObject go)
        {
            // 结果项背景
            Rect itemRect = EditorGUILayout.BeginHorizontal();
            Color bgColor = EditorGUIUtility.isProSkin ? 
                new Color(0.1f, 0.1f, 0.12f) : 
                new Color(0.9f, 0.9f, 0.95f);
            EditorGUI.DrawRect(itemRect, bgColor);
            
            // 悬停高亮效果
            if (itemRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(itemRect, new Color(bgColor.r * 0.95f, bgColor.g * 0.95f, bgColor.b * 0.95f, 0.7f));
            }
            
            EditorGUILayout.ObjectField(go, typeof(GameObject), true);
            
            if (GUILayout.Button("Select", new GUIStyle(searchButtonStyle1)
            {
                fixedWidth = 70,
                margin = new RectOffset(5, 0, 0, 0)
            }))
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
            EditorGUILayout.EndHorizontal();
            
            // 结果项分隔线
            GUILayout.Space(3);
        }

        private void DrawProgressBar()
        {
            if (isCheckingComponents)
            {
                EditorGUILayout.Space();
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
            if (isCheckingComponents) icon = "⟳";
            else if (statusColor == Color.red) icon = "✗";
            
            // 状态文本（带图标）
            EditorGUI.LabelField(statusRect, $"{icon}  {statusMessage}", new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                normal = { textColor = statusColor },
                margin = new RectOffset(8, 0, 0, 0)
            });
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

        private void SearchByName()
        {
            nameSearchResults.Clear();
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            
            foreach (var go in allObjects)
            {
                if (go.name.IndexOf(nameSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    nameSearchResults.Add(go);
                }
            }
            
            // 添加搜索完成的状态反馈
            UpdateStatus($"找到 {nameSearchResults.Count} 个结果", Color.cyan);
            Repaint();
            
            // 微动画：短暂放大按钮
            AnimateButton();
        }

        private void SearchByLayer()
        {
            layerSearchResults.Clear();
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            
            foreach (var go in allObjects)
            {
                if ((layerMask.value & (1 << go.layer)) != 0)
                {
                    layerSearchResults.Add(go);
                }
            }
            
            UpdateStatus($"找到 {layerSearchResults.Count} 个结果", Color.cyan);
            Repaint();
            AnimateButton();
        }

        private void SearchForComponents()
        {
            if (string.IsNullOrEmpty(componentNameSearch))
            {
                showComponentSearchResults = false;
                return;
            }
            
            matchedComponentTypes.Clear();
            
            try
            {
                var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
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
                AnimateButton();
            }
            catch (Exception e)
            {
                Debug.LogError($"搜索组件时出错: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"搜索组件时出错: {e.Message}", "确定");
                UpdateStatus("搜索组件时出错", Color.red);
            }
        }

        private void StartComponentChecking()
        {
            if (isCheckingComponents) return;
            
            try
            {
                allGameObjects.Clear();
                var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var rootGo in rootGameObjects)
                {
                    CollectAllGameObjects(rootGo, allGameObjects);
                }
                
                gameObjectCount = allGameObjects.Count;
                
                componentCheckLog.Clear();
                componentCheckLog.AppendLine($"组件引用检查 - {System.DateTime.Now}");
                componentCheckLog.AppendLine($"目标组件: {targetComponentName} ({targetComponentType.FullName})");
                componentCheckLog.AppendLine($"批处理大小: {batchSize}");
                componentCheckLog.AppendLine($"检查间隔: {checkInterval}s");
                componentCheckLog.AppendLine($"场景中游戏对象总数: {gameObjectCount}");
                componentCheckLog.AppendLine("----------------------------------------");
                
                componentReferences.Clear();
                isCheckingComponents = true;
                processedGameObjectCount = 0;
                componentCheckProgress = 0f;
                currentGameObjectIndex = 0;
                
                componentCheckCancellationToken = new CancellationTokenSource();
                
                EditorApplication.update += UpdateComponentCheckProgress;
                EditorApplication.update += CheckHierarchyForComponentReferencesInBatches;
                
                AddComponentCheckLog($"开始检查Hierarchy中对组件 {targetComponentName} 的引用");
                UpdateStatus("检查中...", Color.yellow);
            }
            catch (Exception e)
            {
                AddComponentCheckLog($"启动检查时出错: {e.Message}");
                isCheckingComponents = false;
                UpdateStatus("启动检查时出错", Color.red);
            }
        }

        private void CollectAllGameObjects(GameObject go, List<GameObject> collection)
        {
            collection.Add(go);
            
            foreach (Transform child in go.transform)
            {
                CollectAllGameObjects(child.gameObject, collection);
            }
        }

        private void CheckHierarchyForComponentReferencesInBatches()
        {
            try
            {
                if (componentCheckCancellationToken.Token.IsCancellationRequested)
                {
                    EditorApplication.update -= CheckHierarchyForComponentReferencesInBatches;
                    FinishComponentChecking();
                    return;
                }
                
                int processedCount = 0;
                
                while (currentGameObjectIndex < allGameObjects.Count && processedCount < batchSize)
                {
                    if (componentCheckCancellationToken.Token.IsCancellationRequested)
                        break;
                        
                    var go = allGameObjects[currentGameObjectIndex];
                    componentCheckProgressMessage = go.name;
                    
                    CheckComponentReferences(go);
                    
                    currentGameObjectIndex++;
                    processedGameObjectCount++;
                    componentCheckProgress = Mathf.Clamp01((float)processedGameObjectCount / gameObjectCount);
                    
                    processedCount++;
                }
                
                if (currentGameObjectIndex >= allGameObjects.Count)
                {
                    componentCheckProgress = 1.0f;
                    processedGameObjectCount = gameObjectCount;
                    EditorApplication.update -= CheckHierarchyForComponentReferencesInBatches;
                    FinishComponentChecking();
                }
                else
                {
                    EditorApplication.delayCall += () => { };
                }
            }
            catch (Exception e)
            {
                AddComponentCheckLog($"检查Hierarchy时出错: {e.Message}");
                FinishComponentChecking();
                UpdateStatus("检查时出错", Color.red);
            }
        }

        private void CheckComponentReferences(GameObject go)
        {
            try
            {
                if (componentCheckCancellationToken.Token.IsCancellationRequested)
                    return;
                        
                var components = go.GetComponents(targetComponentType);
                if (components == null || components.Length == 0)
                    return;
                        
                string gameObjectPath = GetGameObjectPath(go);
                foreach (var component in components)
                {
                    if (component == null)
                        continue;
                        
                    lock (componentReferences)
                    {
                        componentReferences.Add(new ComponentReferenceInfo 
                        { 
                            GameObject = go,
                            GameObjectPath = gameObjectPath,
                            ComponentType = component.GetType().Name,
                            ReferencePath = gameObjectPath
                        });
                                    
                        AddComponentCheckLog($"{component.GetType().Name} 被引用:{gameObjectPath}");
                    }                                    
                }
            }
            catch (Exception e)
            {
                AddComponentCheckLog($"检查游戏对象 {go.name} 的组件时出错: {e.Message}");
            }
        }

        private void UpdateComponentCheckProgress()
        {
            Repaint();
        }

        private void CancelComponentChecking()
        {
            if (componentCheckCancellationToken != null)
            {
                componentCheckCancellationToken.Cancel();
                isCheckingComponents = false;
                UpdateStatus("检查已取消", Color.red);
            }
        }

        private void FinishComponentChecking()
        {
            isCheckingComponents = false;
            componentCheckCancellationToken = null;
            allGameObjects.Clear();
            
            componentCheckLog.AppendLine("----------------------------------------");
            componentCheckLog.AppendLine($"检查完成 - {System.DateTime.Now}");
            componentCheckLog.AppendLine($"检查了 {processedGameObjectCount} 个游戏对象");
            componentCheckLog.AppendLine($"发现 {componentReferences.Count} 个对组件 {targetComponentName} 的引用.");
            
            WriteComponentCheckLogToFile();
            Debug.Log($"检查完成 - 发现 {componentReferences.Count} 个对组件 {targetComponentName} 的引用.\n\n" +
                      $"日志已保存至: {Path.GetFullPath(componentLogFilePath)}");
            // 移除弹窗
            UpdateStatus($"检查完成 - 发现 {componentReferences.Count} 个引用", Color.green);
            Repaint();
        }

        private void AddComponentCheckLog(string message)
        {
            lock (componentCheckLog)
            {
                componentCheckLog.AppendLine($"{message}");
            }
        }

        private void WriteComponentCheckLogToFile()
        {
            try
            {
                componentLogFilePath = Path.Combine(Application.dataPath, 
                    $"ComponentReferences_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(componentLogFilePath, componentCheckLog.ToString());
                
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
            if (obj == null)
                return "Null GameObject";
                
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }

        private void ClearAllResults()
        {
            nameSearchResults.Clear();
            layerSearchResults.Clear();
            componentReferences.Clear();
            UpdateStatus("已清除所有结果", Color.green);
            Repaint();
        }

        private enum SearchType
        {
            Name,
            Layer,
            Component
        }

        private void ClearOtherResults(SearchType currentSearchType)
        {
            switch (currentSearchType)
            {
                case SearchType.Name:
                    layerSearchResults.Clear();
                    componentReferences.Clear();
                    break;
                case SearchType.Layer:
                    nameSearchResults.Clear();
                    componentReferences.Clear();
                    break;
                case SearchType.Component:
                    nameSearchResults.Clear();
                    layerSearchResults.Clear();
                    break;
            }
        }

        // 按钮动画（替换协程）
        private void AnimateButton()
        {
            searchButtonStyle.fontSize = 12;
            buttonPulseTimer = buttonPulseDuration;
            Repaint();
        }

        // 平滑状态颜色过渡（替换协程）
        private void UpdateStatus(string message, Color? targetColor = null)
        {
            statusMessage = message;
            
            if (targetColor.HasValue)
            {
                statusStartColor = statusColor;
                statusTargetColor = targetColor.Value;
                statusColorTransitionTimer = statusColorTransitionDuration;
            }
            
            Repaint();
        }

        private class ComponentReferenceInfo
        {
            public GameObject GameObject;
            public string GameObjectPath;
            public string ComponentType;
            public string ReferencePath;
        }
    }
}