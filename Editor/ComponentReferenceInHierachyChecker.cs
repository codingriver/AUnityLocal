using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace AUnityLocal.Editor
{
    public class ComponentReferenceInHierachyChecker : EditorWindow
    {
        private string logFilePath = "";
        private bool isChecking = false;
        private int referenceCount = 0;
        private Vector2 scrollPosition;
        private List<ComponentReferenceInfo> componentReferences = new List<ComponentReferenceInfo>();
        private CancellationTokenSource cancellationTokenSource;
        private int batchSize = 200;
        private StringBuilder logBuilder = new StringBuilder();
        private float checkInterval = 0.01f; // 检查间隔，防止编辑器卡顿
        private System.Type targetComponentType = typeof(UnityEngine.Component);
        private string targetComponentName = "Component";
        private string componentNameSearch = "";
        private List<Type> matchedComponentTypes = new List<Type>();
        private Vector2 componentSearchScrollPosition;
        private bool showComponentSearchResults = false;
        private bool showConfirmationDialog = false;
        private int gameObjectCount = 0;
        private int processedGameObjectCount = 0;
        private float progress = 0f;
        private string progressMessage = "";
        private List<GameObject> allGameObjects = new List<GameObject>();
        private int currentGameObjectIndex = 0;

        [MenuItem("AUnityLocal/Hierachy检查组件引用")]
        public static void ShowWindow()
        {
            GetWindow<ComponentReferenceInHierachyChecker>("Component Reference Checker");
        }

        private void OnGUI()
        {
            GUILayout.Label("Hierarchy组件引用检查器", EditorStyles.boldLabel);
            
            if (showConfirmationDialog)
            {
                DrawConfirmationDialog();
                return;
            }
            
            EditorGUI.BeginDisabledGroup(isChecking);
            
            // 组件名称搜索
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索组件:", GUILayout.Width(70));
            componentNameSearch = EditorGUILayout.TextField(componentNameSearch);
            
            if (GUILayout.Button("搜索", GUILayout.Width(60)))
            {
                componentReferences.Clear();
                referenceCount = 0;
                SearchForComponents();
            }
            EditorGUILayout.EndHorizontal();
            
            // 搜索结果显示
            if (showComponentSearchResults && matchedComponentTypes.Count > 0)
            {
                EditorGUILayout.LabelField($"找到 {matchedComponentTypes.Count} 个匹配的组件:");
                componentSearchScrollPosition = EditorGUILayout.BeginScrollView(componentSearchScrollPosition, 
                    GUILayout.MaxHeight(150));
                
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
            
            // 已选择的组件
            if (!string.IsNullOrEmpty(targetComponentName))
            {
                EditorGUILayout.LabelField($"当前选择: {targetComponentName}");
            }
            
            batchSize = EditorGUILayout.IntSlider("批处理大小", batchSize, 10, 500);
            checkInterval = EditorGUILayout.Slider("检查间隔(秒)", checkInterval, 0.001f, 0.1f);

            if (GUILayout.Button("检查引用"))
            {
                componentReferences.Clear();
                referenceCount = 0;
                if (targetComponentType == typeof(Component) || string.IsNullOrEmpty(targetComponentName))
                {
                    EditorUtility.DisplayDialog("选择组件", "请先选择一个具体的组件类型", "确定");
                    return;
                }
                
                PrepareForCheck();
            }
            
            EditorGUI.EndDisabledGroup();

            if (isChecking)
            {
                EditorGUILayout.LabelField($"检查中: {progressMessage}");
                EditorGUILayout.LabelField($"进度: {processedGameObjectCount}/{gameObjectCount} ({progress * 100:F1}%)");
                EditorGUILayout.LabelField($"已发现引用: {referenceCount}");
                
                Rect progressRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 20, 20);
                EditorGUI.ProgressBar(progressRect, progress, $"{progress * 100:F1}%");
                
                if (GUILayout.Button("取消"))
                {
                    CancelChecking();
                }
            }
            else if (componentReferences.Count > 0)
            {
                EditorGUILayout.LabelField($"总引用数: {referenceCount}");
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                foreach (var reference in componentReferences)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField($"游戏对象: {reference.GameObjectPath}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"组件类型: {reference.ComponentType}");
                    // EditorGUILayout.LabelField($"引用路径: {reference.ReferencePath}");
                    
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
                if (GUILayout.Button("清除结果"))
                {
                    componentReferences.Clear();
                    referenceCount = 0;
                }
                
                if (GUILayout.Button("打开日志文件"))
                {
                    OpenLogFile();
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (!isChecking && !showComponentSearchResults)
            {
                EditorGUILayout.HelpBox("请搜索并选择目标组件，然后点击\"检查引用\"开始检查", MessageType.Info);
            }
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
                
                // 按名称排序
                matchedComponentTypes = matchedComponentTypes.OrderBy(t => t.Name).ToList();
                
                showComponentSearchResults = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"搜索组件时出错: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"搜索组件时出错: {e.Message}", "确定");
            }
        }

        private void PrepareForCheck()
        {
            try
            {
                // 直接使用CollectAllGameObjects来计算游戏对象总数，确保与后续处理一致
                allGameObjects.Clear();
                var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var rootGo in rootGameObjects)
                {
                    CollectAllGameObjects(rootGo, allGameObjects);
                }
                
                gameObjectCount = allGameObjects.Count; // 使用实际收集的数量作为总数
                
                // 显示确认对话框
                showConfirmationDialog = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"准备检查时出错: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"准备检查时出错: {e.Message}", "确定");
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

        private void DrawConfirmationDialog()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("确认检查", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"你即将检查场景中所有对 {targetComponentName} 组件的引用。");
            EditorGUILayout.LabelField($"场景中共有 {gameObjectCount} 个游戏对象需要检查。");
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("开始检查"))
            {
                showConfirmationDialog = false;
                StartChecking();
            }
            
            if (GUILayout.Button("取消"))
            {
                showConfirmationDialog = false;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void StartChecking()
        {
            if (isChecking) return;
            
            try
            {
                // 初始化日志
                logBuilder.Clear();
                logBuilder.AppendLine($"组件引用检查 - {System.DateTime.Now}");
                logBuilder.AppendLine($"目标组件: {targetComponentName} ({targetComponentType.FullName})");
                logBuilder.AppendLine($"批处理大小: {batchSize}");
                logBuilder.AppendLine($"检查间隔: {checkInterval}s");
                logBuilder.AppendLine($"场景中游戏对象总数: {gameObjectCount}");
                logBuilder.AppendLine("----------------------------------------");
                
                referenceCount = 0;
                componentReferences.Clear();
                isChecking = true;
                processedGameObjectCount = 0;
                progress = 0f;
                currentGameObjectIndex = 0;
                
                cancellationTokenSource = new CancellationTokenSource();
                
                // 收集所有游戏对象（虽然在PrepareForCheck已收集，但此处可确保一致性）
                allGameObjects.Clear();
                var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var rootGo in rootGameObjects)
                {
                    CollectAllGameObjects(rootGo, allGameObjects);
                }
                
                // 开始单线程检查
                EditorApplication.update += UpdateCheckProgress;
                EditorApplication.update += CheckHierarchyInBatches;
                
                AddLog($"开始检查Hierarchy中对组件 {targetComponentName} 的引用");
            }
            catch (Exception e)
            {
                AddLog($"启动检查时出错: {e.Message}");
                isChecking = false;
            }
        }

        private void CheckHierarchyInBatches()
        {
            try
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    EditorApplication.update -= CheckHierarchyInBatches;
                    FinishChecking();
                    return;
                }
                
                int processedCount = 0;
                
                // 分批处理游戏对象，确保所有对象都被处理
                while (currentGameObjectIndex < allGameObjects.Count && processedCount < batchSize)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                        break;
                        
                    var go = allGameObjects[currentGameObjectIndex];
                    progressMessage = go.name;
                    
                    CheckComponentReferences(go);
                    
                    currentGameObjectIndex++;
                    processedGameObjectCount++;
                    // 优化进度计算，确保最终能达到1.0
                    progress = Mathf.Clamp01((float)processedGameObjectCount / gameObjectCount);
                    
                    processedCount++;
                }
                
                // 检查是否完成
                if (currentGameObjectIndex >= allGameObjects.Count)
                {
                    // 处理完所有对象后强制设置为100%
                    progress = 1.0f;
                    processedGameObjectCount = gameObjectCount;
                    EditorApplication.update -= CheckHierarchyInBatches;
                    FinishChecking();
                }
                else
                {
                    // 让出主线程
                    EditorApplication.delayCall += () => { };
                }
            }
            catch (Exception e)
            {
                AddLog($"检查Hierarchy时出错: {e.Message}");
                FinishChecking();
            }
        }

        private void CheckComponentReferences(GameObject go)
        {
            try
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    return;
                    
                // 检查游戏对象是否包含目标组件类型的引用
                var components = go.GetComponents(targetComponentType);
                if (components == null || components.Length == 0)
                    return;
                    
                // 找到对目标组件类型的引用
                string gameObjectPath = GetGameObjectPath(go);
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }
                    lock (componentReferences)
                    {
                        referenceCount++;
                        componentReferences.Add(new ComponentReferenceInfo 
                        { 
                            GameObject = go,
                            GameObjectPath = gameObjectPath,
                            ComponentType = component.GetType().Name,
                            ReferencePath = gameObjectPath
                        });
                                
                        AddLog($"{component.GetType().Name} 被引用:{gameObjectPath}");
                    }                                    
                }
            }
            catch (Exception e)
            {
                AddLog($"检查游戏对象 {go.name} 的组件时出错: {e.Message}");
            }
        }

        private void UpdateCheckProgress()
        {
            Repaint();
        }

        private void CancelChecking()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                isChecking = false;
            }
        }

        private void FinishChecking()
        {
            isChecking = false;
            cancellationTokenSource = null;
            allGameObjects.Clear();
            
            // 添加检查摘要到日志
            logBuilder.AppendLine("----------------------------------------");
            logBuilder.AppendLine($"检查完成 - {System.DateTime.Now}");
            logBuilder.AppendLine($"检查了 {processedGameObjectCount} 个游戏对象");
            logBuilder.AppendLine($"发现 {referenceCount} 个对组件 {targetComponentName} 的引用.");
            
            // 写入日志文件
            WriteLogToFile();
            Debug.Log($"检查完成 - 发现 {referenceCount} 个对组件 {targetComponentName} 的引用.\n\n" +
                      $"日志已保存至: {Path.GetFullPath(logFilePath)}");
            EditorUtility.DisplayDialog("检查完成", 
                $"检查完成！\n\n" +
                $"检查了 {processedGameObjectCount} 个游戏对象\n" +
                $"发现 {referenceCount} 个对组件 {targetComponentName} 的引用.\n\n" +
                $"日志已保存至: {Path.GetFullPath(logFilePath)}", "确定");
            Repaint();
        }

        private void AddLog(string message)
        {
            lock (logBuilder)
            {
                // logBuilder.AppendLine($"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                logBuilder.AppendLine($"{message}");
            }
        }

        private void WriteLogToFile()
        {
            try
            {
                logFilePath = Path.Combine(Application.dataPath, $"ComponentReferences_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(logFilePath, logBuilder.ToString());
                
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", 
                    $"写入日志文件失败: {e.Message}", "确定");
            }
        }

        private void OpenLogFile()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    System.Diagnostics.Process.Start(logFilePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("文件未找到", 
                        $"日志文件不存在: {logFilePath}", "确定");
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

        private class ComponentReferenceInfo
        {
            public GameObject GameObject;
            public string GameObjectPath;
            public string ComponentType;
            public string ReferencePath;
        }
    }
}