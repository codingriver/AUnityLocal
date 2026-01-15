using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using UnityEditorInternal;
using System.Runtime.CompilerServices;

namespace AUnityLocal.Editor
{
    static partial class MonoHookEditor
    {
        public static class MeshRendererEditor
        {
            public static void Register()
            {
                Type type = typeof(AssetDatabase).Assembly.GetType("UnityEditor.MeshRendererEditor");
                Debug.Assert(type != null);
                MethodInfo method = type.GetMethod("OnInspectorGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MonoHookEditor.AddMHook<Action<UnityEditor.Editor>>(method, MethodReplacement, MethodProxy);
            }

            static MethodInfo m_sortMethodInfo = null;

            static void MethodReplacement(UnityEditor.Editor editor)
            {
                FieldInfo fieldInfo = editor.GetType().BaseType
                    .GetField("m_SortingOrder", BindingFlags.NonPublic | BindingFlags.Instance);
                SerializedProperty _SortingOrder = (SerializedProperty)fieldInfo.GetValue(editor);

                FieldInfo fieldInfo1 = editor.GetType().BaseType
                    .GetField("m_SortingLayerID", BindingFlags.NonPublic | BindingFlags.Instance);
                SerializedProperty _SortingLayerID = (SerializedProperty)fieldInfo1.GetValue(editor);

                if (m_sortMethodInfo == null)
                {
                    m_sortMethodInfo = Assembly.GetAssembly(typeof(UnityEditor.Editor))
                        .GetType("UnityEditor.SortingLayerEditorUtility").GetMethod("RenderSortingLayerFields",
                            new Type[] { typeof(SerializedProperty), typeof(SerializedProperty) });
                }

                Debug.Assert(m_sortMethodInfo != null);

                m_sortMethodInfo.Invoke(null, new object[] { _SortingOrder, _SortingLayerID });
                editor.serializedObject.ApplyModifiedProperties();

                MethodProxy(editor);
            }

            [MethodImpl(MethodImplOptions.NoOptimization)]
            static void MethodProxy(UnityEditor.Editor editor)
            {
            }

            // public static string[] GetSortingLayerNames()
            // {
            //     //SortingLayer.layers
            //     Type internalEditorUtilityType = typeof(InternalEditorUtility);
            //     PropertyInfo sortingLayersProperty =
            //         internalEditorUtilityType.GetProperty("sortingLayerNames",
            //             BindingFlags.Static | BindingFlags.NonPublic);
            //     return (string[])sortingLayersProperty.GetValue(null, new object[0]);
            // }
        }
    }
}