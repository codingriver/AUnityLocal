using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AUnityLocal.Editor
{
    public  static class Tools
    {
        public static StringBuilder sb= new StringBuilder();
        public static string SelectFolder()
        {
            string dataPath = Application.dataPath;
            string selectedPath = EditorUtility.OpenFolderPanel("选择路径", dataPath, "");

            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(dataPath))
                {
                    return "Assets/" + selectedPath.Substring(dataPath.Length + 1);
                }
                else
                {
                    Debug.LogWarning("不能在Assets目录之外!");
                }
            }

            return null;
        }
        
        /// <summary>
        /// 获取Hierarchy中物体的相对路径
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static string GetRelativePath(Transform child, Transform parent=null)
        {
            if (child == null) return "";

            List<string> path = new List<string>();
            Transform current = child;

            while (current != null && current != parent)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }

            if (parent != null && current != parent)
            {
                return "不是子节点";
            }

            return string.Join("/", path);
        }

        public static List<string> GetAllChildrenPaths(string assetPath, bool includeSelf = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var ls= GetAllChildrenPaths(prefab, includeSelf);
            return ls;
        }
        
        
        /// <summary>
        /// 获取物体所有子节点的相对路径
        /// </summary>
        /// <param name="parent">父物体</param>
        /// <param name="includeSelf">是否包含自身</param>
        /// <returns>所有子节点的相对路径列表</returns>
        public static List<string> GetAllChildrenPaths(GameObject parent, bool includeSelf = false)
        {
            List<string> paths = new List<string>();
        
            if (includeSelf)
            {
                paths.Add(parent.name);
            }
        
            GetChildrenPathsRecursive(parent.transform, "", paths);
            return paths;
        }
    
        /// <summary>
        /// 递归获取子节点路径
        /// </summary>
        /// <param name="parent">父Transform</param>
        /// <param name="currentPath">当前路径</param>
        /// <param name="paths">路径列表</param>
        private static void GetChildrenPathsRecursive(Transform parent, string currentPath, List<string> paths)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                string childPath = string.IsNullOrEmpty(currentPath) ? child.name : currentPath + "/" + child.name;
            
                // 添加当前子节点路径
                paths.Add(childPath);
            
                // 递归处理子节点的子节点
                if (child.childCount > 0)
                {
                    GetChildrenPathsRecursive(child, childPath, paths);
                }
            }
        }
        public static T FindAndGetComponent<T>(string name,bool enable = true) where T : Behaviour
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
        public static void SetGameObject(string name,bool active = true)
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
                    string relativePath = Tools.GetRelativePath(com.transform, root);
                    sb.AppendLine(relativePath);
                    Debug.Log($"选中节点 {com.name} 相对于根节点 {root.name} 的路径: {relativePath}");
                }
                else
                {
                    string relativePath = Tools.GetRelativePath(com.transform, null);
                    sb.AppendLine(relativePath);
                    if (root != null)
                        Debug.LogWarning($"选中节点 {com.name} 不是根节点 {root.name} 的子节点");
                }
            }

            Debug.Log(sb.ToString());
            return sb.ToString();
        }

        public static void PrintChildCount(Transform root,bool isChild=true)
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
    }
}