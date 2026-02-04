using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityToolbarExtender;

namespace AUnityLocal.Editor
{
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;
    using UnityEngine.UIElements;
    

    [InitializeOnLoad]
    public static class ToolBarToolEditor
    {
        public static  UnityEngine.Object targetObject;
        
        static ToolBarToolEditor()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
        }

        static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("D", "快速创建预制体"),ToolbarStyles.commandButtonStyle))
            {
                SelectTarget();
            }
        }

        static void SelectTarget()
        {
            targetObject = null;
            //IGG.Game.Module.KSBattle.BattleLog.BattleDebug.hitGos
            var hitList = GetStaticFieldValue("IGG.Game.Module.KSBattle.BattleLog+BattleDebug", "hitGos");
            if (hitList != null)
            {
                var hitGos = (System.Collections.Generic.List<UnityEngine.GameObject>)hitList;
                if (hitGos != null&&hitGos.Count>=1)
                {
                    var go = hitGos[0];
                    targetObject = go;
                            
                }
            }

            if (targetObject == null)
            {
                ToastTip.Show("targetObject is null!");
                return;
            }

            if (targetObject.Equals(null))
            {
                ToastTip.Show("targetObject is destroyed!");
                targetObject = null;
                return;
            }

            UnityEditor.Selection.activeObject = targetObject;
            EditorGUIUtility.PingObject(targetObject);
        }
        
        /// <summary>
        /// 查找类并获取静态字段值
        /// </summary>
        public static object GetStaticFieldValue(string typeName, string fieldName)
        {
            // 1. 查找类型
            Type targetType = FindType(typeName);
            if (targetType == null)
            {
                Debug.LogError($"❌ 未找到类型: {typeName}");
                return null;
            }

            // 2. 获取静态字段
            FieldInfo field = targetType.GetField(fieldName, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        
            if (field == null)
            {
                Debug.LogError($"❌ 未找到静态字段: {fieldName} 在类型 {typeName}");
                return null;
            }

            // 3. 获取字段值（静态字段传入 null）
            object value = field.GetValue(null);
        
            Debug.Log($"✅ 成功获取 {typeName}.{fieldName} = {value}");
            return value;
        }        
        /// <summary>
        /// 查找类型
        /// </summary>
        private static Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }
        // static void CreateMenu()
        // {
        //     GenericMenu menu = new GenericMenu();
        //
        //     menu.AddItem(new GUIContent("创建空物体"), false, () => 
        //     {
        //         var go = new GameObject("New GameObject");
        //         Selection.activeGameObject = go;
        //     });
        //
        //     menu.AddItem(new GUIContent("创建 UI/Canvas"), false, () => 
        //     {
        //         var canvas = new GameObject("Canvas", typeof(Canvas));
        //         Selection.activeGameObject = canvas;
        //     });
        //
        //     menu.AddSeparator("");
        //
        //     menu.AddItem(new GUIContent("从选中创建预制体"), false, () => 
        //     {
        //         if (Selection.activeGameObject != null)
        //         {
        //             string path = EditorUtility.SaveFilePanelInProject(
        //                 "保存预制体", 
        //                 Selection.activeGameObject.name, 
        //                 "prefab", 
        //                 "选择保存位置");
        //         
        //             if (!string.IsNullOrEmpty(path))
        //             {
        //                 PrefabUtility.SaveAsPrefabAsset(Selection.activeGameObject, path);
        //             }
        //         }
        //     });
        //
        //     menu.ShowAsContext();
        // }  
        //
        
        static class ToolbarStyles
        {
            public static readonly GUIStyle commandButtonStyle;

            static ToolbarStyles()
            {
                commandButtonStyle = new GUIStyle("Command")
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    imagePosition = ImagePosition.ImageAbove,
                    fontStyle = FontStyle.Bold
                };
            }
        }        
    }
    
    

}