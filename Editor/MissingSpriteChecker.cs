using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AUnityLocal.Editor
{
    public class MissingSpriteChecker : EditorWindow
    {
        private string searchDirectory = "Assets/";
        private string logFilePath = "";
        private bool isChecking = false;
        private int totalPrefabs = 0;
        private int checkedPrefabs = 0;
        private int missingCount = 0;
        private Vector2 scrollPosition;
        private List<MissingSpriteInfo> missingSprites = new List<MissingSpriteInfo>();
        private CancellationTokenSource cancellationTokenSource;
        private int batchSize = 200;
        private StringBuilder logBuilder = new StringBuilder();
        private List<string> prefabPaths = new List<string>();
        private float checkInterval = 0.01f; // 检查间隔，防止编辑器卡顿

        [MenuItem("AUnityLocal/检查Missing Sprite")]
        public static void ShowWindow()
        {
            GetWindow<MissingSpriteChecker>("Missing Sprite Checker");
        }

        private void OnGUI()
        {
            GUILayout.Label("Prefab Missing Sprite Checker", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            searchDirectory = EditorGUILayout.TextField("Search Directory:", searchDirectory);
            // 美化的选择路径按钮
            GUIContent folderContent = new GUIContent("浏览", EditorGUIUtility.IconContent("Folder Icon").image);
            if (GUILayout.Button(folderContent, GUILayout.Width(70), GUILayout.Height(20)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择Prefab目录", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        searchDirectory = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "请选择项目内的Assets目录下的文件夹", "确定");
                    }
                }
            }           
            EditorGUILayout.EndHorizontal();
            batchSize = EditorGUILayout.IntSlider("Batch Size (Single Thread)", batchSize, 10, 200);
            checkInterval = EditorGUILayout.Slider("Check Interval (Seconds)", checkInterval, 0.01f, 0.1f);

            EditorGUI.BeginDisabledGroup(isChecking);
            if (GUILayout.Button("Check Missing Sprites"))
            {
                StartChecking();
            }
            EditorGUI.EndDisabledGroup();

            if (isChecking)
            {
                EditorGUILayout.LabelField($"Progress: {checkedPrefabs}/{totalPrefabs}");
                EditorGUILayout.LabelField($"Missing Sprites Found: {missingCount}");
                if (GUILayout.Button("Cancel"))
                {
                    CancelChecking();
                }
            }
            else if (missingSprites.Count > 0)
            {
                EditorGUILayout.LabelField($"Total Missing Sprites: {missingCount}");
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                foreach (var missingSprite in missingSprites)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField($"Prefab: {missingSprite.PrefabPath}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"GameObject: {missingSprite.GameObjectPath}");
                    
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select Prefab"))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(missingSprite.PrefabPath);
                        if (prefab)
                        {
                            Selection.activeObject = prefab;
                            EditorGUIUtility.PingObject(prefab);
                        }
                    }
                    
                    if (GUILayout.Button("Copy Path"))
                    {
                        EditorGUIUtility.systemCopyBuffer = missingSprite.PrefabPath;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Results"))
                {
                    missingSprites.Clear();
                    missingCount = 0;
                }
                
                if (GUILayout.Button("Open Log File"))
                {
                    OpenLogFile();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void StartChecking()
        {
            if (isChecking) return;
            
            try
            {
                // 初始化日志
                logBuilder.Clear();
                logBuilder.AppendLine($"Missing Sprites Check - {System.DateTime.Now}");
                logBuilder.AppendLine($"Search Directory: {searchDirectory}");
                logBuilder.AppendLine($"Batch Size: {batchSize}");
                logBuilder.AppendLine($"Check Interval: {checkInterval}s");
                logBuilder.AppendLine("----------------------------------------");
                
                var prefabFiles = Directory.GetFiles(searchDirectory, "*.prefab", SearchOption.AllDirectories);
                prefabPaths = new List<string>(prefabFiles);
                totalPrefabs = prefabPaths.Count;
                checkedPrefabs = 0;
                missingCount = 0;
                missingSprites.Clear();
                isChecking = true;
                
                cancellationTokenSource = new CancellationTokenSource();
                
                // 开始单线程检查
                EditorApplication.update += UpdateCheckProgress;
                EditorApplication.update += CheckPrefabsSequentially;
            }
            catch (Exception e)
            {
                AddLog($"Error starting check: {e.Message}");
                isChecking = false;
            }
        }

        private void CheckPrefabsSequentially()
        {
            try
            {
                if (cancellationTokenSource.Token.IsCancellationRequested || prefabPaths.Count == 0)
                {
                    EditorApplication.update -= CheckPrefabsSequentially;
                    FinishChecking();
                    return;
                }
                
                // 处理一批预制体
                int batchCount = Math.Min(batchSize, prefabPaths.Count);
                for (int i = 0; i < batchCount; i++)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                        break;
                        
                    string prefabPath = prefabPaths[0];
                    prefabPaths.RemoveAt(0);
                    CheckSinglePrefab(prefabPath);
                    checkedPrefabs++;
                }
                
                // 控制检查频率，防止编辑器卡顿
                if (prefabPaths.Count > 0 && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    EditorApplication.delayCall += () => { }; // 让出主线程
                }
            }
            catch (Exception e)
            {
                AddLog($"Error checking prefabs: {e.Message}");
                if (prefabPaths.Count == 0)
                {
                    FinishChecking();
                }
            }
        }

        private void CheckSinglePrefab(string prefabPath)
        {
            try
            {
                var prefab = AssetDatabase.LoadMainAssetAtPath(prefabPath) as GameObject;
                if (prefab == null) 
                {
                    AddLog($"Warning: Could not load prefab at path: {prefabPath}");
                    return;
                }

                var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    CheckForMissingSprite(renderers[i].gameObject, renderers[i].sprite, 
                        () => new SerializedObject(renderers[i]).FindProperty("m_Sprite"),
                        prefabPath);
                }

                var images = prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                for (int i = 0; i < images.Length; i++)
                {
                    CheckForMissingSprite(images[i].gameObject, images[i].sprite, 
                        () => new SerializedObject(images[i]).FindProperty("m_Sprite"),
                        prefabPath);
                }
            }
            catch (Exception e)
            {
                AddLog($"Error checking prefab {prefabPath}: {e.Message}");
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
            }
        }

        private void CheckForMissingSprite(GameObject obj, Sprite sprite, 
            System.Func<SerializedProperty> getSpriteProperty, string prefabPath)
        {
            if (sprite == null)
            {
                SerializedProperty sp = getSpriteProperty();
                if (sp != null && sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                {
                    lock (missingSprites)
                    {
                        missingCount++;
                        string gameObjectPath = GetGameObjectPath(obj);
                        missingSprites.Add(new MissingSpriteInfo 
                        { 
                            PrefabPath = prefabPath, 
                            GameObjectPath = gameObjectPath 
                        });
                        
                        AddLog($"Missing Sprite found in: {prefabPath} on GameObject: {gameObjectPath}");
                    }
                }
            }
        }

        private void FinishChecking()
        {
            isChecking = false;
            cancellationTokenSource = null;
            
            // 添加检查摘要到日志
            logBuilder.AppendLine("----------------------------------------");
            logBuilder.AppendLine($"Check Complete - {System.DateTime.Now}");
            logBuilder.AppendLine($"Checked {checkedPrefabs}/{totalPrefabs} prefabs.");
            logBuilder.AppendLine($"Found {missingCount} missing sprites.");
            
            // 写入日志文件
            WriteLogToFile();
            Debug.Log($"Checked {checkedPrefabs}/{totalPrefabs} prefabs. Found {missingCount} missing sprites.\n\n" +
                      $"Log saved to: {Path.GetFullPath(logFilePath)}");
            EditorUtility.DisplayDialog("Check Complete", 
                $"Checked {checkedPrefabs}/{totalPrefabs} prefabs. Found {missingCount} missing sprites.\n\n" +
                $"Log saved to: {Path.GetFullPath(logFilePath)}", "OK");
            Repaint();
        }

        private void AddLog(string message)
        {
            lock (logBuilder)
            {
                logBuilder.AppendLine($"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            }
        }

        private void WriteLogToFile()
        {
            try
            {
                logFilePath = Path.Combine(Application.dataPath, $"MissingSpritesLog_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(logFilePath, logBuilder.ToString());
                
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to write log file: {e.Message}", "OK");
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
                    EditorUtility.DisplayDialog("File Not Found", 
                        $"Log file not found at path: {logFilePath}", "OK");
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to open log file: {e.Message}", "OK");
            }
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }

        private class MissingSpriteInfo
        {
            public string PrefabPath;
            public string GameObjectPath;
        }
    }
}