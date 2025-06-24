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
        private GUIStyle resultCountStyle;
        private Color sectionBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.1f);
        private Vector2 resultsScrollPosition;
        private bool isFirstLayout = true;

        [MenuItem("AUnityLocal/Hierarchy Search Tool")]
        public static void ShowWindow()
        {
            GetWindow<HierarchySearchTool>("Hierarchy Search");
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
        }

        private void InitializeStyles()
        {
            sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(0, 0, 10, 5),
                padding = new RectOffset(5, 5, 2, 2)
            };

            searchButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                fixedHeight = 25
            };

            resultCountStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 12,
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin ? 
                        new Color(0.7f, 0.9f, 1f) : 
                        new Color(0.1f, 0.3f, 0.5f)
                }
            };
        }

        private void OnGUI()
        {
            if (isFirstLayout)
            {
                isFirstLayout = false;
                minSize = new Vector2(800, 600);
            }
            
            DrawTitle();
            EditorGUILayout.BeginHorizontal();
            
            DrawLeftPanel();
            DrawResultsPanel();
            
            EditorGUILayout.EndHorizontal();
            
            DrawProgressBar();
        }

        private void DrawTitle()
        {
            GUILayout.Label("Hierarchy Search & Component Checker", new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                margin = new RectOffset(0, 0, 5, 10)
            });
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.3f));
            
            DrawNameSearchSection();
            EditorGUILayout.Space();
            DrawLayerSearchSection();
            EditorGUILayout.Space();
            DrawComponentSearchSection();
            EditorGUILayout.Space();
            DrawClearButton();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawNameSearchSection()
        {
            DrawSectionHeader("Name Search");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            nameSearchText = EditorGUILayout.TextField("Search Text", nameSearchText);
            
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Search", searchButtonStyle))
            {
                ClearOtherResults(SearchType.Name);
                SearchByName();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawLayerSearchSection()
        {
            DrawSectionHeader("Layer Search");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            layerMaskShow = EditorGUILayout.MaskField("Layers", layerMaskShow, InternalEditorUtility.layers);
            
            string selectedLayers = GetSelectedLayers(layerMaskShow);
            EditorGUILayout.LabelField("Selected Layers", selectedLayers);
            
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Search Layers", searchButtonStyle))
            {
                ClearOtherResults(SearchType.Layer);
                SearchByLayer();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawComponentSearchSection()
        {
            DrawSectionHeader("Component Check");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
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
                StartComponentChecking(); // 直接开始检查，移除确认步骤
            }
            EditorGUI.EndDisabledGroup();
            
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
            Rect rect = GUILayoutUtility.GetRect(1, 25);
            EditorGUI.DrawRect(rect, sectionBackgroundColor);
            EditorGUI.LabelField(rect, title, sectionHeaderStyle);
        }

        private void DrawResultsPanel()
        {
            EditorGUILayout.BeginVertical();
            
            DrawSectionHeader("Search Results");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
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
                
                if (GUILayout.Button("复制路径"))
                {
                    EditorGUIUtility.systemCopyBuffer = reference.GameObjectPath;
                }
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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(go, typeof(GameObject), true);
            
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
            EditorGUILayout.EndHorizontal();
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
            
            Repaint();
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
            
            Repaint();
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
            }
            catch (Exception e)
            {
                Debug.LogError($"搜索组件时出错: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"搜索组件时出错: {e.Message}", "确定");
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
            }
            catch (Exception e)
            {
                AddComponentCheckLog($"启动检查时出错: {e.Message}");
                isCheckingComponents = false;
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
            EditorUtility.DisplayDialog("检查完成", 
                $"检查完成！\n\n" +
                $"检查了 {processedGameObjectCount} 个游戏对象\n" +
                $"发现 {componentReferences.Count} 个对组件 {targetComponentName} 的引用.\n\n" +
                $"日志已保存至: {Path.GetFullPath(componentLogFilePath)}", "确定");
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
            }
        }

        private void OpenComponentLogFile()
        {
            try
            {
                if (File.Exists(componentLogFilePath))
                {
                    System.Diagnostics.Process.Start(componentLogFilePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("文件未找到", 
                        $"日志文件不存在: {componentLogFilePath}", "确定");
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", 
                    $"打开日志文件失败: {e.Message}", "确定");
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

        private class ComponentReferenceInfo
        {
            public GameObject GameObject;
            public string GameObjectPath;
            public string ComponentType;
            public string ReferencePath;
        }
    }
}    