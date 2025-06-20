using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AUnityLocal.Editor
{
    public class PrefabToolEx : EditorWindow
    {
        // 存储依赖关系的字典：key是Prefab路径，value是它依赖的其他Prefab路径列表
        private static Dictionary<string, List<string>> dependencyGraph = new Dictionary<string, List<string>>();

        // 存储反向依赖关系：key是Prefab路径，value是依赖它的Prefab列表
        private static Dictionary<string, List<string>> reverseDependencyGraph = new Dictionary<string, List<string>>();

        // 分层存储的Prefab：key是层数，value是该层的Prefab路径列表
        private static Dictionary<int, List<string>> layeredPrefabs = new Dictionary<int, List<string>>();


        public static Dictionary<int, List<string>> StartAnalysisPrefabsdependencys(string relativePath)
        {
            // 记录开始时间
            DateTime startTime = DateTime.Now;


            try
            {
                // 清除之前的分析结果
                dependencyGraph.Clear();
                reverseDependencyGraph.Clear();
                layeredPrefabs = new Dictionary<int, List<string>>();

                // 获取目录下的所有Prefab
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { relativePath });
                string[] prefabPaths = prefabGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();

                Debug.Log($"在目录 {relativePath} 下找到 {prefabPaths.Length} 个Prefab");

                // 分析每个Prefab的依赖关系
                for (int i = 0; i < prefabPaths.Length; i++)
                {
                    string prefabPath = prefabPaths[i];
                    EditorUtility.DisplayProgressBar("Prefab依赖分析", $"正在分析: {Path.GetFileName(prefabPath)}",
                        (float)i / prefabPaths.Length);
                    AnalyzePrefabDependencies(prefabPath, prefabPaths);
                }

                // 构建反向依赖图
                BuildReverseDependencyGraph();

                // 对Prefab进行分层
                LayerPrefabsByDependency();


                // 计算耗时
                TimeSpan elapsedTime = DateTime.Now - startTime;
                Debug.Log($"分析完成，耗时: {elapsedTime.TotalSeconds:F2} 秒");
                Debug.Log($"已将Prefab分为 {layeredPrefabs.Count} 层");
            }
            catch (Exception e)
            {
                Debug.LogError($"分析过程中发生错误: {e.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return layeredPrefabs;
        }

        // 分析单个Prefab的依赖关系
        private static void AnalyzePrefabDependencies(string prefabPath, string[] allPrefabPaths)
        {
            // 获取该Prefab的所有依赖
            string[] dependencies = AssetDatabase.GetDependencies(prefabPath, true);

            // 筛选出依赖的Prefab
            List<string> dependentPrefabs = new List<string>();

            foreach (string dependencyPath in dependencies)
            {
                // 排除自身
                if (dependencyPath == prefabPath)
                    continue;

                // 检查是否是Prefab并且在目标目录中
                if (dependencyPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
                    Array.Exists(allPrefabPaths, p => p == dependencyPath))
                {
                    dependentPrefabs.Add(dependencyPath);
                }
            }

            // 添加到依赖图
            dependencyGraph[prefabPath] = dependentPrefabs;
        }

        // 构建反向依赖图
        private static void BuildReverseDependencyGraph()
        {
            // 初始化反向依赖图
            foreach (var prefabPath in dependencyGraph.Keys)
            {
                reverseDependencyGraph[prefabPath] = new List<string>();
            }

            // 填充反向依赖关系
            foreach (var pair in dependencyGraph)
            {
                string prefabPath = pair.Key;
                foreach (string dependentPrefab in pair.Value)
                {
                    if (reverseDependencyGraph.ContainsKey(dependentPrefab))
                    {
                        reverseDependencyGraph[dependentPrefab].Add(prefabPath);
                    }
                }
            }
        }

        // 根据依赖关系对Prefab进行分层
        private static void LayerPrefabsByDependency()
        {
            // 初始化所有Prefab的层数为1
            Dictionary<string, int> prefabLayers = new Dictionary<string, int>();
            foreach (var prefabPath in dependencyGraph.Keys)
            {
                prefabLayers[prefabPath] = 1;
            }

            // 计算每个Prefab的层数
            bool changed;
            do
            {
                changed = false;

                foreach (var pair in dependencyGraph)
                {
                    string prefabPath = pair.Key;
                    List<string> dependencies = pair.Value;

                    // 如果这个Prefab依赖于其他Prefab，它的层数应该比任何依赖项的层数大1
                    foreach (string dependency in dependencies)
                    {
                        if (prefabLayers.ContainsKey(dependency))
                        {
                            int requiredLayer = prefabLayers[dependency] + 1;
                            if (prefabLayers[prefabPath] < requiredLayer)
                            {
                                prefabLayers[prefabPath] = requiredLayer;
                                changed = true;
                            }
                        }
                    }
                }
            } while (changed);

            // 将Prefab按层数分组
            layeredPrefabs.Clear();
            foreach (var pair in prefabLayers)
            {
                int layer = pair.Value;
                string prefabPath = pair.Key;

                if (!layeredPrefabs.ContainsKey(layer))
                {
                    layeredPrefabs[layer] = new List<string>();
                }

                layeredPrefabs[layer].Add(prefabPath);
            }
        }


        // 获取Prefab所在的层
        private static int GetPrefabLayer(string prefabPath)
        {
            foreach (var layer in layeredPrefabs.Keys)
            {
                if (layeredPrefabs[layer].Contains(prefabPath))
                {
                    return layer;
                }
            }

            return 1; // 默认返回第一层
        }

        // 将绝对路径转换为Unity相对路径
        private static string ConvertToUnityRelativePath(string absolutePath)
        {
            string projectPath = Application.dataPath;

            if (absolutePath.StartsWith(projectPath))
            {
                string relativePath = "Assets" + absolutePath.Substring(projectPath.Length);
                return relativePath;
            }

            return null;
        }


        // 日志文件保存路径

        // 记录检查时间
        private static Stopwatch stopwatch = new Stopwatch();

        // 存储检查结果
        private static List<string> prefabPathsWithMissingSprites = new List<string>();

        private static Dictionary<string, List<string>> missingSpritePathsInPrefabs =
            new Dictionary<string, List<string>>();

        public static void CheckAllPrefabsForMissingSprites(string searchPath)
        {
            // 初始化
            prefabPathsWithMissingSprites.Clear();
            missingSpritePathsInPrefabs.Clear();
            stopwatch.Restart();

            // 获取所有Prefab
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab",new[] { searchPath });

            // 处理每个Prefab
            int processedCount = 0;
            int totalCount = allPrefabs.Length;

            foreach (string guid in allPrefabs)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                CheckPrefabForMissingSprites(prefabPath);

                // 更新进度条
                processedCount++;
                if (processedCount % 100 == 0 || processedCount == totalCount)
                {
                    float progress = (float)processedCount / totalCount;
                    EditorUtility.DisplayProgressBar("检查缺失的Sprite", $"处理中: {processedCount}/{totalCount}", progress);
                }
            }

            // 清除进度条
            EditorUtility.ClearProgressBar();

            // 停止计时器并保存日志
            stopwatch.Stop();
            SaveResultsToLog(totalCount);

            // 显示结果摘要
            string summary = $"检查完成!\n" +
                             $"总Prefab数: {totalCount}\n" +
                             $"包含缺失Sprite的Prefab数: {prefabPathsWithMissingSprites.Count}\n" +
                             $"耗时: {stopwatch.ElapsedMilliseconds} ms\n\n";

            EditorUtility.DisplayDialog("检查完成", summary, "确定");
        }

        private static void CheckPrefabForMissingSprites(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;

            // 实例化Prefab用于检查
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return;

            List<string> objectPathsWithMissingSprites = new List<string>();

            // 检查所有SpriteRenderer组件
            SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (IsSpriteReferenceMissing(renderer, "m_Sprite"))
                {
                    string objectPath = GetTransformPath(renderer.transform);
                    objectPathsWithMissingSprites.Add(objectPath);
                }
            }

            // 检查所有Image组件（UI）
            UnityEngine.UI.Image[] images = instance.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var image in images)
            {
                if (IsSpriteReferenceMissing(image, "m_Sprite"))
                {
                    string objectPath = GetTransformPath(image.transform);
                    objectPathsWithMissingSprites.Add(objectPath);
                }
            }

            // 检查所有Button组件（UI）
            UnityEngine.UI.Button[] buttons = instance.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var button in buttons)
            {
                // 检查normalSprite
                if (button.image != null && IsSpriteReferenceMissing(button.image, "m_Sprite"))
                {
                    string objectPath = GetTransformPath(button.transform) + "/Image";
                    objectPathsWithMissingSprites.Add(objectPath);
                }

                // 检查其他状态的sprite
                SerializedObject serializedButton = new SerializedObject(button);

                // 高亮状态
                SerializedProperty highlightedProp = serializedButton.FindProperty("m_SpriteState.m_HighlightedSprite");
                if (highlightedProp != null && highlightedProp.objectReferenceValue == null &&
                    highlightedProp.objectReferenceInstanceIDValue == 0)
                {
                    string objectPath = GetTransformPath(button.transform) + " (Highlighted Sprite)";
                    objectPathsWithMissingSprites.Add(objectPath);
                }

                // 按下状态
                SerializedProperty pressedProp = serializedButton.FindProperty("m_SpriteState.m_PressedSprite");
                if (pressedProp != null && pressedProp.objectReferenceValue == null &&
                    pressedProp.objectReferenceInstanceIDValue == 0)
                {
                    string objectPath = GetTransformPath(button.transform) + " (Pressed Sprite)";
                    objectPathsWithMissingSprites.Add(objectPath);
                }

                // 禁用状态
                SerializedProperty disabledProp = serializedButton.FindProperty("m_SpriteState.m_DisabledSprite");
                if (disabledProp != null && disabledProp.objectReferenceValue == null &&
                    disabledProp.objectReferenceInstanceIDValue == 0)
                {
                    string objectPath = GetTransformPath(button.transform) + " (Disabled Sprite)";
                    objectPathsWithMissingSprites.Add(objectPath);
                }
            }

            // 如果找到缺失的Sprite，记录结果
            if (objectPathsWithMissingSprites.Count > 0)
            {
                prefabPathsWithMissingSprites.Add(prefabPath);
                missingSpritePathsInPrefabs[prefabPath] = objectPathsWithMissingSprites;
            }

            // 销毁实例
            Object.DestroyImmediate(instance);
        }

        // 检查Sprite引用是否缺失
        private static bool IsSpriteReferenceMissing(Object target, string fieldName)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);

            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                // 检查引用是否为null且instanceID为0（表示引用缺失）
                return property.objectReferenceValue == null && property.objectReferenceInstanceIDValue == 0;
            }

            return false;
        }

        // 获取Transform在Prefab中的路径
        private static string GetTransformPath(Transform transform)
        {
            string path = transform.name;
            Transform parent = transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        // 保存结果到日志文件
        private static void SaveResultsToLog(long totalCount = 0)
        {
            string logDirectory = Path.Combine(Application.dataPath, "../AUnityLocal");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
                
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{timestamp}_MissingSprites.txt";
            string logFilePath = Path.Combine(logDirectory, fileName);            

            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("============================================================");
                    writer.WriteLine($"缺失Sprite引用检查结果 - {System.DateTime.Now}");
                    writer.WriteLine($"总Prefab数: {AssetDatabase.FindAssets("t:Prefab").Length}");
                    writer.WriteLine($"包含缺失Sprite的Prefab数: {prefabPathsWithMissingSprites.Count}");
                    writer.WriteLine($"检查耗时: {stopwatch.ElapsedMilliseconds} ms");
                    writer.WriteLine("============================================================");
                    writer.WriteLine();

                    if (prefabPathsWithMissingSprites.Count > 0)
                    {
                        writer.WriteLine("以下Prefab包含缺失的Sprite引用:");
                        writer.WriteLine();

                        foreach (string prefabPath in prefabPathsWithMissingSprites)
                        {
                            writer.WriteLine($"Prefab: {prefabPath}");

                            if (missingSpritePathsInPrefabs.TryGetValue(prefabPath, out List<string> objectPaths))
                            {
                                foreach (string objectPath in objectPaths)
                                {
                                    writer.WriteLine($"  - 物体路径: {objectPath}");
                                }
                            }

                            writer.WriteLine();
                        }
                    }
                    else
                    {
                        writer.WriteLine("恭喜！未发现引用缺失Sprite的Prefab。");
                    }
                    
                    string summon= $"检查完成!\n" +
                                   $"总Prefab数: {totalCount}\n" +
                                   $"包含缺失Sprite的Prefab数: {prefabPathsWithMissingSprites.Count}\n" +
                                   $"耗时: {stopwatch.ElapsedMilliseconds} ms\n\n";
                    writer.WriteLine(summon);
                }

                
                Debug.Log($"缺失Sprite检查完成，日志已保存到: {logFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"保存日志失败: {e.Message}");
            }
        }
    }
}