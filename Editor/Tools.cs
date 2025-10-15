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
    }
}