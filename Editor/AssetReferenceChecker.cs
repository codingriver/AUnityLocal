using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AUnityLocal.Editor
{
    public class AssetReferenceChecker : EditorWindow
    {
        private string searchDirectory = "Assets/";
        private string targetAssetPath = "";
        private string logFilePath = "";
        private bool isChecking = false;
        private int totalAssets = 0;
        private int checkedAssets = 0;
        private int referenceCount = 0;
        private Vector2 scrollPosition;
        private List<AssetReferenceInfo> assetReferences = new List<AssetReferenceInfo>();
        private CancellationTokenSource cancellationTokenSource;
        private int batchSize = 200;
        private StringBuilder logBuilder = new StringBuilder();
        private List<string> assetPaths = new List<string>();
        private float checkInterval = 0.01f; // 检查间隔，防止编辑器卡顿
        private UnityEngine.Object targetAsset = null;

        [MenuItem("AUnityLocal/检查资源引用")]
        public static void ShowWindow()
        {
            GetWindow<AssetReferenceChecker>("Asset Reference Checker");
        }

        private void OnGUI()
        {
            GUILayout.Label("资源引用检查器（不支持Sprite，不支持递归检查，只能检查目标资源对应的上一层引用）", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(isChecking);
            
            // 目标资源选择
            EditorGUILayout.LabelField("目标资源:", GUILayout.Width(70));
            targetAsset = EditorGUILayout.ObjectField(targetAsset, typeof(UnityEngine.Object), false);
            
            if (targetAsset != null)
            {
                targetAssetPath = AssetDatabase.GetAssetPath(targetAsset);
                EditorGUILayout.LabelField(targetAssetPath);
            }
            else
            {
                targetAssetPath = "";
                EditorGUILayout.LabelField("未选择资源");
            }
            
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            searchDirectory = EditorGUILayout.TextField("搜索目录:", searchDirectory);
            
            // 美化的选择路径按钮
            GUIContent folderContent = new GUIContent("浏览", EditorGUIUtility.IconContent("Folder Icon").image);
            if (GUILayout.Button(folderContent, GUILayout.Width(70), GUILayout.Height(20)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择搜索目录", "Assets", "");
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
            
            batchSize = EditorGUILayout.IntSlider("批处理大小", batchSize, 10, 200);
            checkInterval = EditorGUILayout.Slider("检查间隔(秒)", checkInterval, 0.01f, 0.1f);

            EditorGUI.BeginDisabledGroup(isChecking || string.IsNullOrEmpty(targetAssetPath));
            if (GUILayout.Button("检查引用"))
            {
                StartChecking();
            }
            EditorGUI.EndDisabledGroup();

            if (isChecking)
            {
                EditorGUILayout.LabelField($"进度: {checkedAssets}/{totalAssets}");
                EditorGUILayout.LabelField($"已发现引用: {referenceCount}");
                if (GUILayout.Button("取消"))
                {
                    CancelChecking();
                }
            }
            else if (assetReferences.Count > 0)
            {
                EditorGUILayout.LabelField($"总引用数: {referenceCount}");
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                foreach (var reference in assetReferences)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField($"引用资源: {reference.ReferencePath}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"引用类型: {reference.ReferenceType}");
                    
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("选择资源"))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(reference.ReferencePath);
                        if (asset)
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                    }
                    
                    if (GUILayout.Button("复制路径"))
                    {
                        EditorGUIUtility.systemCopyBuffer = reference.ReferencePath;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("清除结果"))
                {
                    assetReferences.Clear();
                    referenceCount = 0;
                }
                
                if (GUILayout.Button("打开日志文件"))
                {
                    OpenLogFile();
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (!isChecking && !string.IsNullOrEmpty(targetAssetPath))
            {
                EditorGUILayout.HelpBox("未发现对该资源的引用", MessageType.Info);
            }
        }

        private void StartChecking()
        {
            if (isChecking || string.IsNullOrEmpty(targetAssetPath)) return;
            
            try
            {
                // 初始化日志
                logBuilder.Clear();
                logBuilder.AppendLine($"资源引用检查 - {System.DateTime.Now}");
                logBuilder.AppendLine($"目标资源: {targetAssetPath}");
                logBuilder.AppendLine($"搜索目录: {searchDirectory}");
                logBuilder.AppendLine($"批处理大小: {batchSize}");
                logBuilder.AppendLine($"检查间隔: {checkInterval}s");
                logBuilder.AppendLine("----------------------------------------");
                
                // 获取目标资源的GUID
                string targetGuid = AssetDatabase.AssetPathToGUID(targetAssetPath);
                if (string.IsNullOrEmpty(targetGuid))
                {
                    EditorUtility.DisplayDialog("错误", "无法获取目标资源的GUID", "确定");
                    isChecking = false;
                    return;
                }
                
                // 获取所有要检查的资源
                var allAssetPaths = Directory.GetFiles(searchDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(p => !p.EndsWith(".meta") && !p.EndsWith(".cs") && !p.EndsWith(".js") && !p.EndsWith(".dll"))
                    .ToList();
                
                // 排除目标资源自身
                allAssetPaths.Remove(targetAssetPath);
                
                assetPaths = allAssetPaths;
                totalAssets = assetPaths.Count;
                checkedAssets = 0;
                referenceCount = 0;
                assetReferences.Clear();
                isChecking = true;
                
                cancellationTokenSource = new CancellationTokenSource();
                
                // 开始单线程检查
                EditorApplication.update += UpdateCheckProgress;
                EditorApplication.update += CheckAssetsSequentially;
                
                AddLog($"开始检查资源引用，目标GUID: {targetGuid}");
            }
            catch (Exception e)
            {
                AddLog($"启动检查时出错: {e.Message}");
                isChecking = false;
            }
        }

        private void CheckAssetsSequentially()
        {
            try
            {
                if (cancellationTokenSource.Token.IsCancellationRequested || assetPaths.Count == 0)
                {
                    EditorApplication.update -= CheckAssetsSequentially;
                    FinishChecking();
                    return;
                }
                
                // 处理一批资源
                int batchCount = Math.Min(batchSize, assetPaths.Count);
                for (int i = 0; i < batchCount; i++)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                        break;
                        
                    string assetPath = assetPaths[0];
                    assetPaths.RemoveAt(0);
                    CheckSingleAsset(assetPath);
                    checkedAssets++;
                }
                
                // 控制检查频率，防止编辑器卡顿
                if (assetPaths.Count > 0 && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    EditorApplication.delayCall += () => { }; // 让出主线程
                }
            }
            catch (Exception e)
            {
                AddLog($"检查资源时出错: {e.Message}");
                if (assetPaths.Count == 0)
                {
                    FinishChecking();
                }
            }
        }

        private void CheckSingleAsset(string assetPath)
        {
            try
            {
                // 获取目标资源的GUID
                string targetGuid = AssetDatabase.AssetPathToGUID(targetAssetPath);
                if (string.IsNullOrEmpty(targetGuid))
                    return;
                
                // 读取资源文件内容
                string assetText = File.ReadAllText(assetPath);
                
                // 检查是否包含目标GUID
                if (assetText.Contains(targetGuid))
                {
                    // 确定资源类型
                    string assetType = GetAssetType(assetPath);
                    
                    lock (assetReferences)
                    {
                        referenceCount++;
                        assetReferences.Add(new AssetReferenceInfo 
                        { 
                            ReferencePath = assetPath, 
                            ReferenceType = assetType
                        });
                        
                        AddLog($"发现引用: {assetPath}, 类型: {assetType}");
                    }
                }
            }
            catch (Exception e)
            {
                AddLog($"检查资源 {assetPath} 时出错: {e.Message}");
            }
        }

        private string GetAssetType(string assetPath)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset != null)
                return asset.GetType().Name;
                
            // 根据文件扩展名猜测类型
            string extension = Path.GetExtension(assetPath).ToLower();
            switch (extension)
            {
                case ".prefab": return "GameObject";
                case ".mat": return "Material";
                case ".shader": return "Shader";
                case ".texture2d": return "Texture2D";
                case ".spriteatlas": return "SpriteAtlas";
                case ".anim": return "AnimationClip";
                case ".controller": return "AnimatorController";
                case ".fbx": return "Model";
                case ".unity": return "Scene";
                default: return "Unknown";
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

        private void FinishChecking()
        {
            isChecking = false;
            cancellationTokenSource = null;
            
            // 添加检查摘要到日志
            logBuilder.AppendLine("----------------------------------------");
            logBuilder.AppendLine($"检查完成 - {System.DateTime.Now}");
            logBuilder.AppendLine($"检查了 {checkedAssets}/{totalAssets} 个资源.");
            logBuilder.AppendLine($"发现 {referenceCount} 个引用.");
            
            // 写入日志文件
            WriteLogToFile();
            Debug.Log($"检查了 {checkedAssets}/{totalAssets} 个资源. 发现 {referenceCount} 个引用.\n\n" +
                      $"日志已保存至: {Path.GetFullPath(logFilePath)}");
            EditorUtility.DisplayDialog("检查完成", 
                $"检查了 {checkedAssets}/{totalAssets} 个资源. 发现 {referenceCount} 个引用.\n\n" +
                $"日志已保存至: {Path.GetFullPath(logFilePath)}", "确定");
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
                logFilePath = Path.Combine(Application.dataPath, $"AssetReferences_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
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

        private class AssetReferenceInfo
        {
            public string ReferencePath;
            public string ReferenceType;
        }
    }
}