using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

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
    }
}