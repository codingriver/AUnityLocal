using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using FxProNS;
using Skyunion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AUnityLocal.Editor
{
    public class WindowToolEditor : EditorWindow
    {
        // 样式缓存
        private GUIStyle titleStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle buttonStyle;
        private GUIStyle boxStyle;
        private GUIStyle fieldStyle;
        private Vector2 scrollPosition;

        [MenuItem("AUnityLocal/EditorWindowTool", false, 1000)]
        public static void ShowWindow()
        {
            var window = GetWindow<WindowToolEditor>("EditorWindowTool 工具");
            window.minSize = new Vector2(900, 800);
            window.maxSize = new Vector2(1400, 1200);
        }

        void OnEnable()
        {
            
        }
        public void InitializeStyles()
        {
            // 标题样式
            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 15, 20),
                normal = { textColor = new Color(0.2f, 0.8f, 1.0f) }
            };

            // 区域标题样式
            sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(5, 5, 10, 5),
                normal = { textColor = new Color(0.8f, 0.9f, 1.0f) }
            };

            // 按钮样式
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 35,
                margin = new RectOffset(5, 5, 3, 3),
                padding = new RectOffset(10, 10, 8, 8)
            };

            // 盒子样式
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            // 字段样式
            fieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 11,
                fixedHeight = 20
            };
        }        
        private void OnGUI()
        {
            if (titleStyle == null) InitializeStyles();
            if (WindowToolGroup.titleStyle == null) WindowToolGroup.InitializeStyles();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawTitle();

            EditorGUILayout.BeginVertical(WindowToolGroup.boxStyle);

            foreach (var area in groupList)
            {
                DrawSection(area.title, area.tip,area.OnGUI);
            }
            
            EditorGUILayout.EndVertical();            
            
            EditorGUILayout.EndScrollView();
        }

        #region UI辅助方法

        private void DrawTitle()
        {
            EditorGUILayout.LabelField("EditorWindowTool 工具集", titleStyle);
            GUILayout.Space(10);
        }
        private void DrawSection(string title, string tooltip, System.Action content)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField(title, sectionHeaderStyle);
            GUILayout.Space(5);
            GUILayout.Label(tooltip);
            content?.Invoke();
            EditorGUILayout.EndVertical();
        }

        #endregion
        

        public static List<WindowToolGroup> groupList = new List<WindowToolGroup>()
        { };
        static WindowToolEditor()
        {
            // 静态构造函数中初始化
            InitializeAutoRegisterGroups();
        }
        private static void InitializeAutoRegisterGroups()
        {
            var groupTypes = new List<(Type type, int order)>();
        
            // 获取所有程序集
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        // 检查是否继承自基类且有自动注册特性
                        if (type.IsSubclassOf(typeof(WindowToolGroup)) && 
                            !type.IsAbstract && 
                            type.GetCustomAttribute<WindowToolGroupAttribute>() != null)
                        {
                            var attr = type.GetCustomAttribute<WindowToolGroupAttribute>();
                            groupTypes.Add((type, attr.Order));
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error loading types from assembly {assembly.FullName}: {e.Message}");
                }
            }
        
            // 按顺序排序并创建实例
            foreach (var (type, order) in groupTypes.OrderBy(x => x.order))
            {
                try
                {
                    var instance = (WindowToolGroup)Activator.CreateInstance(type);
                    groupList.Add(instance);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create instance of {type.Name}: {e.Message}");
                }
            }
        }


        
        
        
    }
}