using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AUnityLocal.Editor
{
    public static class Tools
    {
        public static StringBuilder sb = new StringBuilder();

        #region OnGUI

        public static void OnGUISelectFolder(ref string folderPath, string btnName = "浏览", string title = "目标路径:",
            string defaultName = "")
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.Width(60));
            // GUI.enabled = false;
            folderPath = EditorGUILayout.TextField(folderPath);
            // GUI.enabled = true;
            if (GUILayout.Button(btnName, GUILayout.Width(50)))
            {
                folderPath = Tools.SelectFolder(folderPath, title, defaultName);
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion



        /// <summary>
        /// 选择文件夹，支持传入默认路径参数
        /// </summary>
        /// <param name="path">可选的默认路径，如果提供则直接返回（经过验证）</param>
        /// <param name="title">文件夹选择对话框标题</param>
        /// <param name="defaultName">默认文件夹名称</param>
        /// <returns>相对于Assets的路径，失败返回null</returns>
        public static string SelectFolder(string path = null, string title = "选择路径", string defaultName = "")
        {
            // 如果没有传入路径或传入路径无效，则打开文件夹选择对话框
            string dataPath = Application.dataPath;
            // 确定默认打开路径
            string defaultPath = dataPath;
            // 如果传入了路径参数，优先验证并使用传入的路径
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log($"使用传入路径参数: {path}");

                // 验证传入的路径
                string validatedPath = ValidateAndNormalizePath(path);
                if (!string.IsNullOrEmpty(validatedPath))
                {
                    Debug.Log($"传入路径验证成功: {validatedPath}");
                    defaultPath = validatedPath;
                }
                else
                {
                    Debug.LogWarning($"传入路径无效: {path}，将打开文件夹选择对话框");
                }
            }

            if (!string.IsNullOrEmpty(path))
            {
                // 尝试从传入路径推导默认打开位置
                string parentPath = GetParentPathForDialog(path);
                if (!string.IsNullOrEmpty(parentPath))
                {
                    defaultPath = parentPath;
                }
            }

            Debug.Log($"打开文件夹选择对话框，默认路径: {defaultPath}");
            string selectedPath = EditorUtility.OpenFolderPanel(title, defaultPath, defaultName);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                return ConvertToAssetsRelativePath(selectedPath);
            }

            Debug.Log("用户取消了文件夹选择");
            return ConvertToAssetsRelativePath(path);
        }

        /// <summary>
        /// 验证并规范化路径
        /// </summary>
        /// <param name="path">要验证的路径</param>
        /// <returns>规范化后的相对路径，无效则返回null</returns>
        private static string ValidateAndNormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // 规范化路径分隔符
            path = path.Replace('\\', '/');

            // 如果是相对路径（以Assets开头）
            if (path.StartsWith("Assets/") || path == "Assets")
            {
                // 验证Assets相对路径是否存在
                if (AssetDatabase.IsValidFolder(path))
                {
                    return path;
                }
                else
                {
                    Debug.LogWarning($"Assets相对路径不存在: {path}");
                    return "Assets";
                }
            }

            // 如果是绝对路径
            if (System.IO.Path.IsPathRooted(path))
            {
                return ConvertToAssetsRelativePath(path);
            }

            // 尝试作为相对于Assets的路径处理
            string assetsRelativePath = "Assets/" + path.TrimStart('/');
            if (AssetDatabase.IsValidFolder(assetsRelativePath))
            {
                return assetsRelativePath;
            }

            Debug.LogWarning($"无法验证路径: {path}");
            return "Assets";
        }

        /// <summary>
        /// 将绝对路径转换为相对于Assets的路径
        /// </summary>
        /// <param name="absolutePath">绝对路径</param>
        /// <returns>相对路径，失败返回null</returns>
        private static string ConvertToAssetsRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return null;
            }

            string dataPath = Application.dataPath;

            // 规范化路径分隔符
            absolutePath = absolutePath.Replace('\\', '/');
            dataPath = dataPath.Replace('\\', '/');

            if (absolutePath.StartsWith(dataPath))
            {
                if (absolutePath.Length == dataPath.Length)
                {
                    // 选择的就是Assets目录
                    return "Assets";
                }
                else
                {
                    // 转换为Assets相对路径
                    string relativePath = "Assets" + absolutePath.Substring(dataPath.Length);
                    Debug.Log($"转换路径: {absolutePath} -> {relativePath}");
                    return relativePath;
                }
            }
            else if (absolutePath.StartsWith("Assets/"))
            {
                return absolutePath;
            }
            else
            {
                Debug.LogWarning($"选择的路径不在Assets目录内: {absolutePath}");
                return "Assets";
            }
        }

        /// <summary>
        /// 从给定路径获取用于对话框的父级路径
        /// </summary>
        /// <param name="path">输入路径</param>
        /// <returns>父级绝对路径</returns>
        private static string GetParentPathForDialog(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                // 如果是Assets相对路径，转换为绝对路径
                if (path.StartsWith("Assets"))
                {
                    string dataPath = Application.dataPath;
                    if (path == "Assets")
                    {
                        return dataPath;
                    }
                    else
                    {
                        string absolutePath = dataPath + path.Substring("Assets".Length);
                        if (System.IO.Directory.Exists(absolutePath))
                        {
                            return absolutePath;
                        }
                        else
                        {
                            // 如果目录不存在，返回父目录
                            return System.IO.Path.GetDirectoryName(absolutePath);
                        }
                    }
                }

                // 如果是绝对路径
                if (System.IO.Path.IsPathRooted(path))
                {
                    if (System.IO.Directory.Exists(path))
                    {
                        return path;
                    }
                    else
                    {
                        return System.IO.Path.GetDirectoryName(path);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"获取父级路径失败: {ex.Message}");
            }

            return Application.dataPath;
        }


        public static List<string> GetAllChildrenFullName(string assetPath, bool includeSelf = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var ls = prefab.transform.GetAllChildrenFullName(includeSelf);
            return ls;
        }

        public static T FindAndGetComponent<T>(string name, bool enable = true) where T : Behaviour
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                var com = go.GetComponent<T>();
                if (com != null)
                {
                    com.enabled = enable;
                    return com;
                }
            }

            return null;
        }

        public static void FindAndSetGameObject(string name, bool active = true)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        public static void ToggleGameStats()
        {
            var gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var gameWindow = EditorWindow.GetWindow(gameViewType);

            // 获取showStats字段
            var showStatsField = gameViewType.GetField("m_Stats",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (showStatsField != null)
            {
                bool currentValue = (bool)showStatsField.GetValue(gameWindow);
                // showStatsField.SetValue(gameWindow, !currentValue);
                showStatsField.SetValue(gameWindow, true);
            }

            gameWindow.Repaint();
        }

        public static string PrintRelativePaths<T>(T[] data, Transform root = null) where T : Component
        {
            StringBuilder sb = new StringBuilder();
            foreach (T com in data)
            {
                if (root != null && com.transform.IsChildOf(root))
                {
                    string relativePath = com.transform.FullName(root);
                    sb.AppendLine(relativePath);
                    Debug.Log($"选中节点 {com.name} 相对于根节点 {root.name} 的路径: {relativePath}");
                }
                else
                {
                    string relativePath = com.transform.FullName();
                    sb.AppendLine(relativePath);
                    if (root != null)
                        Debug.LogWarning($"选中节点 {com.name} 不是根节点 {root.name} 的子节点");
                }
            }

            Debug.Log(sb.ToString());
            return sb.ToString();
        }

        public static void PrintChildCount(Transform root, bool isChild = true)
        {
            if (isChild)
            {
                PrintChildCount(root.gameObject);
            }
            else
            {
                int totalCount = GetTotalChildCount(root);
                Debug.Log($"根物体 [{root.name}] 总共包含子物体数量: {totalCount}");
            }
        }

        public static void PrintChildCount(GameObject obj)
        {
            Debug.Log($"=== {obj.name} 的子物体统计 ===");

            // 打印根物体的总子物体数量
            int totalCount = GetTotalChildCount(obj.transform);
            Debug.Log($"根物体 [{obj.name}] 总共包含子物体数量: {totalCount}");

            // 遍历每个直接子物体
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                int childCount = GetTotalChildCount(child);
                Debug.Log($"子物体 [{child.name}] 包含子物体数量: {childCount}");
            }
        }

        static int GetTotalChildCount(Transform transform)
        {
            int count = 0;

            // 递归计算所有子物体数量
            count += transform.childCount;

            for (int i = 0; i < transform.childCount; i++)
            {
                count += GetTotalChildCount(transform.GetChild(i));
            }

            return count;
        }

        // static void PrintAllRootObjectsChildCount()
        // {
        //     GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        //
        //     Debug.Log("=== 场景中所有根物体的子物体统计 ===");
        //
        //     foreach (GameObject rootObj in rootObjects)
        //     {
        //         int totalCount = GetTotalChildCount(rootObj);
        //         Debug.Log($"根物体 [{rootObj.name}] 总共包含子物体数量: {totalCount}");
        //     }
        // }

        public static bool IsModelAsset(string assetPath)
        {
            return assetPath.EndsWith(".fbx") ||
                   assetPath.EndsWith(".obj") ||
                   assetPath.EndsWith(".dae") ||
                   assetPath.EndsWith(".3ds") ||
                   assetPath.EndsWith(".blend");
        }


        #region prefab fbx

        public static void ProcessFBX(bool isRun, GameObject root, bool ProcessFBXOnlyLog)
        {
            List<string> assetList = FindFBX(root);
            Debug.Log(assetList.ToStr());
            StringBuilder sb = new StringBuilder();

            if (!ProcessFBXOnlyLog)
            {
                foreach (string assetPath in assetList)
                {
                    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;

                    if (importer != null)
                    {
                        // 修改设置
                        ModifyImporterSettings(importer, assetPath, isRun);

                        // 保存并重新导入
                        importer.SaveAndReimport();
                        AssetDatabase.Refresh();
                    }
                }
            }


            if (ProcessFBXOnlyLog)
            {
                foreach (string assetPath in assetList)
                {
                    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;

                    if (importer != null)
                    {
                        sb.AppendLine(assetPath);
                        var ls = Tools.GetAllChildrenFullName(assetPath);
                        sb.AppendLine(ls.ToStr(null, spacing: "\n"));
                    }
                }

                sb.AppendLine("------------------------------------");
                foreach (string assetPath in assetList)
                {
                    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;

                    if (importer != null)
                    {
                        sb.AppendLine(assetPath);
                        var ls = ExtractNodesFromModel(assetPath);
                        sb.AppendLine(ls.ToStr(null, spacing: "\n"));
                    }
                }

                Debug.Log(sb.ToString());
                File.WriteAllText("./AUnityLocal/fbx.txt", sb.ToString());
            }
        }

        static void ModifyImporterSettings(ModelImporter importer, string assetPath, bool isRun)
        {
            if (!isRun)
            {
                if (importer.optimizeGameObjects)
                {
                    if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                    {
                        importer.optimizeGameObjects = false;
                        importer.extraExposedTransformPaths = new string[0];
                        Debug.Log("修复FBX：" + assetPath);
                    }
                }

                return;
            }

            if (importer.optimizeGameObjects)
            {
                return;
            }

            // 在这里设置你想要的导入参数
            importer.optimizeGameObjects = true;
            importer.extraExposedTransformPaths = ExtractNodesFromModel(assetPath).ToArray();

            // 添加更多你需要的设置...
        }

        static List<string> ExtractNodesFromModel(string assetPath)
        {
            List<string> foundNodes = new List<string>();
            // 加载模型资源
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (modelPrefab == null)
            {
                Debug.LogWarning($"Could not load model at path: {assetPath}");
                return foundNodes;
            }

            Debug.Log($"=== Extracting nodes from: {assetPath} ===");

            // 遍历所有子物体
            Transform[] allTransforms = modelPrefab.GetComponentsInChildren<Transform>(true);


            var renderers = modelPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var rendererTransforms = renderers.Select((r) => r.transform).ToList();
            foreach (var skinnedMeshRenderer in renderers)
            {
                if (skinnedMeshRenderer.rootBone != null)
                {
                    rendererTransforms.Add(skinnedMeshRenderer.rootBone);
                }
            }

            foreach (Transform transform in allTransforms)
            {
                string nodeName = transform.name;

                // 检查是否包含ROOT或以TAG_开头
                if (nodeName.Contains("ROOT") || nodeName.StartsWith("TAG_") || rendererTransforms.Contains(transform))
                {
                    foundNodes.Add(transform.FullName(modelPrefab.transform));
                    Debug.Log($"Found node: {nodeName} (Path: {transform.FullName()})");
                }
            }

            if (foundNodes.Count == 0)
            {
                Debug.Log($"No matching nodes found in {assetPath}");
            }
            else
            {
                Debug.Log($"Total matching nodes found: {foundNodes.Count}");
            }


            Debug.Log(foundNodes.ToStr(null, spacing: "\n"));
            return foundNodes;
        }

        public static List<string> FindFBX(GameObject root)
        {
            List<string> list = new List<string>();
            if (root == null)
            {
                Debug.LogWarning("请先选择一个GameObject");
                return new List<string>();
            }

            GameObject selectedObject = root;
            Debug.Log($"=== 检查物体及其子物体: {selectedObject.name} ===");

            // 获取所有MeshFilter组件（包括子物体）
            MeshFilter[] meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                    list.Add(assetPath);
                    Debug.Log(
                        $"GameObject: {meshFilter.gameObject.name} - MeshFilter - Mesh: {meshFilter.sharedMesh.name}, Path: {assetPath}");
                }
            }

            // 获取所有SkinnedMeshRenderer组件（包括子物体）
            SkinnedMeshRenderer[] skinnedMeshRenderers = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
            {
                if (skinnedMeshRenderer.sharedMesh != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(skinnedMeshRenderer.sharedMesh);
                    list.Add(assetPath);
                    Debug.Log(
                        $"GameObject: {skinnedMeshRenderer.gameObject.name} - SkinnedMeshRenderer - Mesh: {skinnedMeshRenderer.sharedMesh.name}, Path: {assetPath}");
                }
            }

            return list;
        }


        public static void ProcessPrefab()
        {
            var list = Selection.objects;

            if (list == null)
            {
                Debug.LogWarning("Please select a prefab in the Project window");
                return;
            }

            foreach (var obj in list)
            {
                if (obj == null) continue;
                // Debug.Log(obj.name);
                AnalyzePrefabDependencies(obj as GameObject, true);
                // Debug.Log(PrefabUtility.IsPartOfPrefabAsset(obj));                      
            }
        }

        [System.Serializable]
        public class PrefabDependency
        {
            public GameObject prefab;
            public string prefabName;
            public string assetPath;
            public PrefabAssetType prefabType;
            public DependencyType dependencyType;
            public string componentName;
            public string propertyName;
            public Transform parentTransform;
            public int hierarchyDepth;

            public PrefabDependency(GameObject prefab, DependencyType depType, string component = "",
                string property = "", Transform parent = null, int depth = 0)
            {
                this.prefab = prefab;
                this.prefabName = prefab ? prefab.name : "Unknown";
                this.assetPath = prefab ? AssetDatabase.GetAssetPath(prefab) : "";
                this.prefabType = prefab ? PrefabUtility.GetPrefabAssetType(prefab) : PrefabAssetType.NotAPrefab;
                this.dependencyType = depType;
                this.componentName = component;
                this.propertyName = property;
                this.parentTransform = parent;
                this.hierarchyDepth = depth;
            }
        }

        public enum DependencyType
        {
            NestedPrefab, // 嵌套的prefab
            ComponentReference, // 组件引用的prefab
            ChildPrefabInstance, // 子对象中的prefab实例
            PrefabVariant, // prefab变体
            DirectReference // 直接引用
        }

        /// <summary>
        /// 分析指定prefab的所有依赖关系
        /// </summary>
        /// <param name="targetPrefab">要分析的prefab</param>
        /// <param name="printToConsole">是否打印到控制台</param>
        /// <returns>依赖关系列表</returns>
        public static List<PrefabDependency> AnalyzePrefabDependencies(GameObject targetPrefab,
            bool printToConsole = true)
        {
            if (targetPrefab == null)
            {
                Debug.LogWarning("Target prefab is null");
                return new List<PrefabDependency>();
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(targetPrefab))
            {
                Debug.LogWarning($"'{targetPrefab.name}' is not a prefab asset");
                return new List<PrefabDependency>();
            }

            var dependencies = new List<PrefabDependency>();

            Debug.Log($"Analyzing dependencies for prefab: {targetPrefab.name}");

            // 分析prefab内部的所有依赖
            AnalyzePrefabInternal(targetPrefab, targetPrefab.transform, 0, dependencies);

            // 移除重复项
            dependencies = RemoveDuplicateDependencies(dependencies);

            Debug.Log($"Found {dependencies.Count} prefab dependencies in {targetPrefab.name}");

            if (printToConsole)
            {
                PrintDependenciesToConsole(targetPrefab, dependencies);
            }

            return dependencies;
        }


        /// <summary>
        /// 批量分析多个prefab
        /// </summary>
        /// <param name="prefabs">要分析的prefab列表</param>
        /// <param name="printToConsole">是否打印到控制台</param>
        /// <returns>所有依赖关系的字典，key为prefab名称</returns>
        public static Dictionary<string, List<PrefabDependency>> AnalyzeMultiplePrefabs(GameObject[] prefabs,
            bool printToConsole = true)
        {
            var results = new Dictionary<string, List<PrefabDependency>>();

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var dependencies = AnalyzePrefabDependencies(prefab, false);
                    results[prefab.name] = dependencies;
                }
            }

            if (printToConsole)
            {
                PrintMultiplePrefabsAnalysis(results);
            }

            return results;
        }

        /// <summary>
        /// 分析指定文件夹下的所有prefab
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="recursive">是否递归搜索子文件夹</param>
        /// <param name="printToConsole">是否打印到控制台</param>
        /// <returns>所有依赖关系的字典</returns>
        public static Dictionary<string, List<PrefabDependency>> AnalyzePrefabsInFolder(string folderPath,
            bool recursive = true, bool printToConsole = true)
        {
            string searchPattern = recursive ? "t:GameObject" : "t:GameObject";
            string[] guids = AssetDatabase.FindAssets(searchPattern, new[] { folderPath });

            var prefabs = new List<GameObject>();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 如果不递归，检查是否在直接路径下
                if (!recursive)
                {
                    string directory = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
                    if (directory != folderPath.TrimEnd('/'))
                        continue;
                }

                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset != null && PrefabUtility.IsPartOfPrefabAsset(asset))
                {
                    prefabs.Add(asset);
                }
            }

            Debug.Log($"Found {prefabs.Count} prefabs in folder: {folderPath}");

            return AnalyzeMultiplePrefabs(prefabs.ToArray(), printToConsole);
        }

        private static void AnalyzePrefabInternal(GameObject rootPrefab, Transform current, int depth,
            List<PrefabDependency> dependencies)
        {
            // 检查当前对象是否是嵌套的prefab
            if (current.gameObject != rootPrefab)
            {
                // 检查是否为prefab实例
                if (PrefabUtility.IsPartOfPrefabInstance(current.gameObject))
                {
                    GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject);
                    if (prefabSource != null && prefabSource != rootPrefab)
                    {
                        DependencyType depType =
                            depth == 1 ? DependencyType.NestedPrefab : DependencyType.ChildPrefabInstance;
                        dependencies.Add(new PrefabDependency(
                            prefabSource,
                            depType,
                            "GameObject",
                            "Prefab Instance",
                            current,
                            depth
                        ));
                    }
                }
                // 检查是否为prefab变体
                else if (PrefabUtility.GetPrefabAssetType(current.gameObject) == PrefabAssetType.Variant)
                {
                    GameObject variantSource = PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject);
                    if (variantSource != null)
                    {
                        dependencies.Add(new PrefabDependency(
                            variantSource,
                            DependencyType.PrefabVariant,
                            "GameObject",
                            "Variant Source",
                            current,
                            depth
                        ));
                    }
                }
            }

            // 分析组件中的prefab引用
            Component[] components = current.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null) continue;
                AnalyzeComponentForPrefabReferences(comp, current, depth, dependencies);
            }

            // 递归检查子对象
            for (int i = 0; i < current.childCount; i++)
            {
                AnalyzePrefabInternal(rootPrefab, current.GetChild(i), depth + 1, dependencies);
            }
        }

        private static void AnalyzeComponentForPrefabReferences(Component component, Transform parent, int depth,
            List<PrefabDependency> dependencies)
        {
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.GetIterator();

            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (property.objectReferenceValue is GameObject go)
                    {
                        // 检查是否为prefab资源
                        if (PrefabUtility.IsPartOfPrefabAsset(go))
                        {
                            dependencies.Add(new PrefabDependency(
                                go,
                                DependencyType.ComponentReference,
                                component.GetType().Name,
                                property.displayName,
                                parent,
                                depth
                            ));
                        }
                    }
                }
            }
        }

        private static List<PrefabDependency> RemoveDuplicateDependencies(List<PrefabDependency> dependencies)
        {
            var uniqueDependencies = new List<PrefabDependency>();
            var seenPrefabs = new HashSet<string>();

            foreach (var dependency in dependencies)
            {
                string key =
                    $"{dependency.assetPath}_{dependency.dependencyType}_{dependency.componentName}_{dependency.propertyName}";
                if (!seenPrefabs.Contains(key))
                {
                    seenPrefabs.Add(key);
                    uniqueDependencies.Add(dependency);
                }
            }

            return uniqueDependencies;
        }

        private static void PrintDependenciesToConsole(GameObject targetPrefab, List<PrefabDependency> dependencies)
        {
            if (targetPrefab == null || dependencies.Count == 0) return;

            Debug.Log($"=== Prefab Dependencies Analysis for '{targetPrefab.name}' ===");
            Debug.Log($"Total Dependencies Found: {dependencies.Count}");

            // 按类型分组统计
            var typeGroups = dependencies.GroupBy(d => d.dependencyType);
            Debug.Log("\n=== Summary by Dependency Type ===");
            foreach (var group in typeGroups)
            {
                Debug.Log($"{group.Key}: {group.Count()} dependencies");
            }

            // 按Prefab类型分组统计
            var prefabTypeGroups = dependencies.GroupBy(d => d.prefabType);
            Debug.Log("\n=== Summary by Prefab Type ===");
            foreach (var group in prefabTypeGroups)
            {
                Debug.Log($"{group.Key}: {group.Count()} prefabs");
            }

            // 详细列表
            Debug.Log("\n=== Detailed Dependencies List ===");
            var groupedDependencies = dependencies
                .GroupBy(d => d.dependencyType)
                .OrderBy(g => g.Key.ToString());

            foreach (var group in groupedDependencies)
            {
                Debug.Log($"\n--- {group.Key} Dependencies ---");
                foreach (var dependency in group.OrderBy(d => d.prefabName))
                {
                    string logMessage = $"• {dependency.prefabName} ({dependency.prefabType})";

                    if (!string.IsNullOrEmpty(dependency.componentName))
                        logMessage += $" | Component: {dependency.componentName}";

                    if (!string.IsNullOrEmpty(dependency.propertyName))
                        logMessage += $" | Property: {dependency.propertyName}";

                    if (dependency.hierarchyDepth > 0)
                        logMessage += $" | Depth: {dependency.hierarchyDepth}";

                    logMessage += $" | Path: {dependency.assetPath}";

                    Debug.Log(logMessage);
                }
            }

            Debug.Log("=== End of Dependencies Analysis ===");
        }

        private static void PrintMultiplePrefabsAnalysis(Dictionary<string, List<PrefabDependency>> results)
        {
            Debug.Log($"=== Multiple Prefabs Dependencies Analysis ===");
            Debug.Log($"Analyzed {results.Count} prefabs");

            int totalDependencies = results.Values.Sum(deps => deps.Count);
            Debug.Log($"Total Dependencies Found: {totalDependencies}");

            foreach (var kvp in results.OrderBy(r => r.Key))
            {
                Debug.Log($"\n--- {kvp.Key} ({kvp.Value.Count} dependencies) ---");

                if (kvp.Value.Count > 0)
                {
                    var typeGroups = kvp.Value.GroupBy(d => d.dependencyType);
                    foreach (var group in typeGroups)
                    {
                        Debug.Log($"  {group.Key}: {group.Count()}");
                        foreach (var dep in group.Take(3)) // 只显示前3个
                        {
                            Debug.Log($"    • {dep.prefabName} ({dep.prefabType})");
                        }

                        if (group.Count() > 3)
                        {
                            Debug.Log($"    ... and {group.Count() - 3} more");
                        }
                    }
                }
            }

            Debug.Log("=== End of Multiple Prefabs Analysis ===");
        }

        public static void RevertSpecificPrefabAnimator()
        {
            var objs = Selection.objects;

            foreach (var obj in objs)
            {
                if (obj == null)
                {
                    continue;
                }

                if (!PrefabUtility.IsPartOfPrefabAsset(obj))
                {
                    continue;
                }

                string prefabPath = AssetDatabase.GetAssetPath(obj);
                // RevertSpecificPrefabAnimator(prefabPath);
                RevertAddedPrefabAnimator(prefabPath);
            }
        }

        public static void RevertAddedPrefabAnimator(string prefabPath)
        {
            RuntimeAnimatorController controller = null;

            // 打开Prefab进行编辑
            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                // 查找所有嵌套的Prefab实例
                var allTransforms = prefabRoot.GetComponentsInChildren<Transform>(true)
                    .Where((a) => a.GetComponent<Animator>() != null);

                foreach (var transform in allTransforms)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject))
                    {
                        controller = transform.GetComponent<Animator>().runtimeAnimatorController;
                        ProcessNestedPrefabAnimator(transform.gameObject);
                    }
                }
            }

            // 打开Prefab进行编辑
            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var prefabRoot = editingScope.prefabContentsRoot;

                // 查找所有嵌套的Prefab实例
                var ani = prefabRoot.GetComponentInChildren<Animator>(true);
                if (ani != null)
                {
                    if (ani.runtimeAnimatorController == null)
                    {
                        ani.runtimeAnimatorController = controller;
                        EditorUtility.SetDirty(ani);
                        Debug.LogWarning($"Animator runtimeAnimatorController 同步设置成功>>{controller != null}");
                    }
                    else
                    {
                        Debug.LogWarning($"Animator runtimeAnimatorController  设置失败，原始值存在 同步>>{controller != null}");
                    }
                }
            }

            // AssetDatabase.SaveAssets();
            // 刷新资源
            AssetDatabase.Refresh();

            Debug.Log("Added Prefab Animator revert completed!");
        }

        private static void ProcessNestedPrefabAnimator(GameObject nestedPrefabInstance)
        {
            // 获取原始Prefab
            var originalPrefab = PrefabUtility.GetCorrespondingObjectFromSource(nestedPrefabInstance);

            if (originalPrefab == null) return;

            // 检查原始Prefab是否有Animator组件
            var originalAnimator = originalPrefab.GetComponent<Animator>();
            var currentAnimator = nestedPrefabInstance.GetComponent<Animator>();

            // 情况1: 原始Prefab没有Animator，但实例中添加了Animator
            if (originalAnimator == null && currentAnimator != null)
            {
                Debug.Log($"Found added Animator component on {nestedPrefabInstance.name}");

                // 检查这个Animator是否是添加的组件覆盖
                if (IsAddedComponent(nestedPrefabInstance, currentAnimator))
                {
                    // 删除添加的Animator组件
                    Object.DestroyImmediate(currentAnimator);
                    EditorUtility.SetDirty(nestedPrefabInstance);
                    Debug.Log($"Removed added Animator from {nestedPrefabInstance.name}");
                }
            }
            // 情况2: 原始Prefab有Animator，但实例中的Animator是新添加的（原来的被删除了）
            else if (originalAnimator != null && currentAnimator != null)
            {
                // 检查当前Animator是否是添加的组件
                if (IsAddedComponent(nestedPrefabInstance, currentAnimator))
                {
                    // 删除添加的Animator
                    Object.DestroyImmediate(currentAnimator);
                    Debug.Log($"Removed added Animator from {nestedPrefabInstance.name}");

                    // 恢复原始的Animator组件
                    RestoreOriginalAnimator(nestedPrefabInstance, originalAnimator);
                    EditorUtility.SetDirty(nestedPrefabInstance);
                }
            }
            // 情况3: 原始Prefab有Animator，但实例中没有（被删除了）
            else if (originalAnimator != null && currentAnimator == null)
            {
                // 检查是否有删除的组件覆盖
                if (IsRemovedComponent(nestedPrefabInstance, typeof(Animator)))
                {
                    // 恢复被删除的Animator组件
                    RestoreOriginalAnimator(nestedPrefabInstance, originalAnimator);
                }
            }
        }

        /// <summary>
        /// 检查组件是否是添加的覆盖
        /// </summary>
        private static bool IsAddedComponent(GameObject prefabInstance, Component component)
        {
            // 获取添加的组件覆盖
            var addedComponents = PrefabUtility.GetAddedComponents(prefabInstance);

            return addedComponents.Any(addedComp => addedComp.instanceComponent == component);
        }

        /// <summary>
        /// 检查是否有被删除的组件
        /// </summary>
        private static bool IsRemovedComponent(GameObject prefabInstance, System.Type componentType)
        {
            // 获取删除的组件覆盖
            var removedComponents = PrefabUtility.GetRemovedComponents(prefabInstance);

            return removedComponents.Any(removedComp =>
                removedComp.assetComponent != null &&
                removedComp.assetComponent.GetType() == componentType);
        }

        /// <summary>
        /// 恢复原始的Animator组件
        /// </summary>
        private static void RestoreOriginalAnimator(GameObject prefabInstance, Animator originalAnimator)
        {
            // 方法1: 尝试恢复删除的组件
            var removedComponents = PrefabUtility.GetRemovedComponents(prefabInstance);
            var removedAnimator = removedComponents.FirstOrDefault(rc =>
                rc.assetComponent is Animator);

            if (removedAnimator.assetComponent != null)
            {
                // 恢复删除的组件
                PrefabUtility.RevertRemovedComponent(prefabInstance, removedAnimator.assetComponent,
                    InteractionMode.AutomatedAction);
                Debug.Log($"Restored removed Animator component on {prefabInstance.name}");
            }
            else
            {
                // 方法2: 手动添加并复制属性
                var newAnimator = prefabInstance.AddComponent<Animator>();
                CopyAnimatorProperties(originalAnimator, newAnimator);
                Debug.Log($"Manually restored Animator component on {prefabInstance.name}");
            }
        }

        /// <summary>
        /// 复制Animator属性
        /// </summary>
        private static void CopyAnimatorProperties(Animator source, Animator target)
        {
            if (source == null || target == null) return;

            target.runtimeAnimatorController = source.runtimeAnimatorController;
            target.avatar = source.avatar;
            target.applyRootMotion = source.applyRootMotion;
            target.updateMode = source.updateMode;
            target.cullingMode = source.cullingMode;
        }


        private static void ProcessAllAnimatorOverrides(GameObject prefabInstance)
        {
            // 1. 处理添加的Animator组件
            var addedComponents = PrefabUtility.GetAddedComponents(prefabInstance);
            foreach (var addedComp in addedComponents)
            {
                if (addedComp.instanceComponent is Animator)
                {
                    Debug.Log($"Reverting added Animator on {prefabInstance.name}");
                    PrefabUtility.RevertAddedComponent(addedComp.instanceComponent, InteractionMode.AutomatedAction);
                }
            }

            // 2. 处理删除的Animator组件
            var removedComponents = PrefabUtility.GetRemovedComponents(prefabInstance);
            foreach (var removedComp in removedComponents)
            {
                if (removedComp.assetComponent is Animator)
                {
                    Debug.Log($"Reverting removed Animator on {prefabInstance.name}");
                    PrefabUtility.RevertRemovedComponent(prefabInstance, removedComp.assetComponent,
                        InteractionMode.AutomatedAction);
                }
            }

            // 3. 处理修改的Animator组件
            var objectOverrides = PrefabUtility.GetObjectOverrides(prefabInstance);
            foreach (var overrideInfo in objectOverrides)
            {
                if (overrideInfo.instanceObject is Animator)
                {
                    Debug.Log($"Reverting modified Animator on {prefabInstance.name}");
                    PrefabUtility.RevertObjectOverride(overrideInfo.instanceObject, InteractionMode.AutomatedAction);
                }
            }
        }

        public static void BatchRevertAllAnimatorOverrides()
        {
            if (!EditorUtility.DisplayDialog("Batch Revert",
                    "This will revert ALL Animator overrides in ALL prefabs. Continue?",
                    "Yes", "Cancel"))
            {
                return;
            }

            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
            int processedCount = 0;

            try
            {
                for (int i = 0; i < prefabGUIDs.Length; i++)
                {
                    string guid = prefabGUIDs[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    EditorUtility.DisplayProgressBar("Processing Prefabs",
                        $"Processing {System.IO.Path.GetFileName(path)}",
                        (float)i / prefabGUIDs.Length);

                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab != null && HasAnimatorOverrides(prefab))
                    {
                        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
                        {
                            var allGameObjects = editingScope.prefabContentsRoot
                                .GetComponentsInChildren<Transform>(true)
                                .Select(t => t.gameObject).ToArray();

                            foreach (var go in allGameObjects)
                            {
                                if (PrefabUtility.IsPartOfPrefabInstance(go))
                                {
                                    ProcessAllAnimatorOverrides(go);
                                }
                            }
                        }

                        processedCount++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            Debug.Log($"Batch processing completed! Processed {processedCount} prefabs.");
        }

        private static bool HasAnimatorOverrides(GameObject prefab)
        {
            // 简化的检查逻辑，实际使用时可以更精确
            var allGameObjects = prefab.GetComponentsInChildren<Transform>(true)
                .Select(t => t.gameObject).ToArray();

            foreach (var go in allGameObjects)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    var addedComponents = PrefabUtility.GetAddedComponents(go);
                    var removedComponents = PrefabUtility.GetRemovedComponents(go);
                    var objectOverrides = PrefabUtility.GetObjectOverrides(go);

                    if (addedComponents.Any(ac => ac.instanceComponent is Animator) ||
                        removedComponents.Any(rc => rc.assetComponent is Animator) ||
                        objectOverrides.Any(oo => oo.instanceObject is Animator))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        public static Object[] GetSelection(Object root = null, Action<Object> action = null)
        {
            if (root != null)
            {
                action?.Invoke(root);
                return new Object[] { root };
            }

            var arr = Selection.objects;
            var arr1 = arr.Select(b => b as Object).ToArray();
            foreach (var a in arr1)
            {
                action?.Invoke(a);
            }

            return arr1;
        }

        public static Object[] GetSelection(Object[] array = null, Action<Object> action = null)
        {
            if (array != null && array.Length > 0)
            {
                var data = array.Where(a => a != null).ToArray();
                foreach (var a in data)
                {
                    action?.Invoke(a);
                }

                return data;
            }

            var arr = Selection.objects;
            var arr1 = arr.Where((a) => a != null).ToArray();
            foreach (var a in arr1)
            {
                action?.Invoke(a);
            }

            return arr1;
        }

        public static T[] GetSelection<T>(T root = null, Action<T> action = null) where T : Object
        {
            if (root != null)
            {
                action?.Invoke(root);
                return new T[] { root };
            }

            var arr = Selection.objects;
            var arr1 = arr.Where((a) => a is T && a != null).Select(b => b as T).ToArray();
            foreach (var a in arr1)
            {
                action?.Invoke(a);
            }

            return arr1;
        }

        public static T[] GetSelection<T>(T[] array = null, Action<T> action = null) where T : Object
        {
            if (array != null && array.Length > 0)
            {
                var data = array.Where(a => a != null).ToArray();
                foreach (var a in data)
                {
                    action?.Invoke(a);
                }

                return data;
            }

            var arr = Selection.objects;
            var arr1 = arr.Where((a) => a is T && a != null).Select(b => b as T).ToArray();
            foreach (var a in arr1)
            {
                action?.Invoke(a);
            }

            return arr1;
        }

        /// <summary>
        /// 获取Prefab文件路径（支持传入参数或使用选中对象）
        /// </summary>
        /// <param name="paths">可选的路径数组，可以是prefab文件路径或目录路径</param>
        /// <returns>Prefab文件路径列表</returns>
        public static List<string> GetSelectedPrefabPaths(string[] paths = null)
        {
            HashSet<string> prefabPaths = new HashSet<string>();

            // 如果传入了参数且长度大于0，优先使用传入参数
            if (paths != null && paths.Length > 0)
            {
                Debug.Log($"使用传入参数，共 {paths.Length} 个路径");

                foreach (string path in paths)
                {
                    if (string.IsNullOrEmpty(path))
                    {
                        Debug.LogWarning("跳过空路径");
                        continue;
                    }

                    ProcessPath(path, prefabPaths);
                }
            }
            else
            {
                // 使用选中的对象
                var selectedObjects = Selection.objects;

                if (selectedObjects == null || selectedObjects.Length == 0)
                {
                    Debug.LogWarning("请先选中要处理的Prefab文件或包含Prefab的目录，或者传入有效的路径参数");
                    return prefabPaths.ToList();
                }

                Debug.Log($"使用选中对象，共 {selectedObjects.Length} 个对象");

                foreach (var obj in selectedObjects)
                {
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    ProcessPath(assetPath, prefabPaths);
                }
            }

            Debug.Log($"总共找到 {prefabPaths.Count} 个Prefab文件");
            return prefabPaths.ToList();
        }

        /// <summary>
        /// 处理单个路径，可能是prefab文件或目录
        /// </summary>
        /// <param name="path">要处理的路径</param>
        /// <param name="prefabPaths">用于收集prefab路径的集合</param>
        private static void ProcessPath(string path, HashSet<string> prefabPaths)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("路径为空，跳过处理");
                return;
            }

            // 检查路径是否存在
            if (!System.IO.File.Exists(path) &&
                !System.IO.Directory.Exists(Application.dataPath + "/" + path.Substring("Assets/".Length)))
            {
                // 对于Assets路径，需要特殊处理
                if (!path.StartsWith("Assets/") || !AssetDatabase.IsValidFolder(path))
                {
                    Debug.LogWarning($"路径不存在或无效: {path}");
                    return;
                }
            }

            // 检查是否为目录
            if (AssetDatabase.IsValidFolder(path))
            {
                // 获取目录下所有prefab文件（包括子目录）
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                int foundCount = 0;

                foreach (string guid in guids)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (prefabPaths.Add(prefabPath)) // Add返回true表示是新添加的
                    {
                        foundCount++;
                    }
                }

                Debug.Log($"从目录 {path} 中找到 {foundCount} 个新的Prefab文件（总共扫描到 {guids.Length} 个）");
            }
            else if (path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
            {
                // 直接指定的prefab文件
                if (prefabPaths.Add(path))
                {
                    Debug.Log($"添加Prefab文件: {path}");
                }
                else
                {
                    Debug.Log($"Prefab文件已存在，跳过: {path}");
                }
            }
            else
            {
                // 尝试作为Asset路径加载，检查是否为Prefab
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset != null && PrefabUtility.GetPrefabAssetType(asset) != PrefabAssetType.NotAPrefab)
                {
                    if (prefabPaths.Add(path))
                    {
                        Debug.Log($"添加Prefab资源: {path}");
                    }
                    else
                    {
                        Debug.Log($"Prefab资源已存在，跳过: {path}");
                    }
                }
                else
                {
                    Debug.LogWarning($"跳过非Prefab文件或无效路径: {path}");
                }
            }
        }
    }
}