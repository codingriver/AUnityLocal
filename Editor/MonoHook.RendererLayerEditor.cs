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
    
#if ENABLE_RENDERERLAYEREDITOR|| true 
[InitializeOnLoad]
public static class RendererLayerEditor
{
    static RendererLayerEditor()
    {
        Register();
    }
    public static void Register()
    {
        // MeshRendererEditor
        if (m_type_MeshRendererEditor == null)
            m_type_MeshRendererEditor = typeof(AssetDatabase).Assembly.GetType("UnityEditor.MeshRendererEditor");
        Debug.Assert(m_type_MeshRendererEditor != null);

        MethodInfo method = m_type_MeshRendererEditor.GetMethod("OnInspectorGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo methodReplacement = typeof(RendererLayerEditor).GetMethod("SubRendererOnInspectorGUI", BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo methodProxy = typeof(RendererLayerEditor).GetMethod("SubRendererOnInspectorGUIProxy", BindingFlags.Static | BindingFlags.NonPublic);
        MethodHook hooker = new MethodHook(method, methodReplacement, methodProxy);
        hooker.Install();

        // SkinnedMeshRendererEditor
        if (m_type_SkinnedMeshRendererEditor == null)
            m_type_SkinnedMeshRendererEditor = typeof(AssetDatabase).Assembly.GetType("UnityEditor.SkinnedMeshRendererEditor");
        Debug.Assert(m_type_SkinnedMeshRendererEditor != null);

        method = m_type_SkinnedMeshRendererEditor.GetMethod("OnInspectorGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        methodReplacement = typeof(RendererLayerEditor).GetMethod("SubRendererOnInspectorGUI", BindingFlags.Static | BindingFlags.NonPublic);
        methodProxy = typeof(RendererLayerEditor).GetMethod("SubRendererOnInspectorGUIProxyE", BindingFlags.Static | BindingFlags.NonPublic);
        hooker = new MethodHook(method, methodReplacement, methodProxy);
        hooker.Install();
    }

    static Type m_type_MeshRendererEditor = null;
    static Type m_type_SkinnedMeshRendererEditor = null;
    static MethodInfo m_sortMethodInfo = null;
   
    static void SubRendererOnInspectorGUI(UnityEditor.Editor editor)
    {
        FieldInfo fieldInfo= editor.GetType().BaseType.GetField("m_SortingOrder", BindingFlags.NonPublic | BindingFlags.Instance);
        SerializedProperty _SortingOrder=(SerializedProperty)fieldInfo.GetValue(editor);

        FieldInfo fieldInfo1 = editor.GetType().BaseType.GetField("m_SortingLayerID", BindingFlags.NonPublic | BindingFlags.Instance);
        SerializedProperty _SortingLayerID = (SerializedProperty)fieldInfo1.GetValue(editor);

        if (m_sortMethodInfo == null)
        {
            m_sortMethodInfo = Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetType("UnityEditor.SortingLayerEditorUtility").GetMethod("RenderSortingLayerFields", new Type[] { typeof(SerializedProperty), typeof(SerializedProperty) });
        }
        Debug.Assert(m_sortMethodInfo != null);

        m_sortMethodInfo.Invoke(null,new object[] {_SortingOrder,_SortingLayerID});
        editor.serializedObject.ApplyModifiedProperties();

        if (editor.GetType().FullName== "UnityEditor.MeshRendererEditor")
        {
            SubRendererOnInspectorGUIProxy(editor);
        }
        else
        {
            SubRendererOnInspectorGUIProxyE(editor);
        }
        

    }
    [MethodImpl( MethodImplOptions.NoOptimization)]
    static void SubRendererOnInspectorGUIProxy(UnityEditor.Editor editor)
    {
        MethodInfo method = m_type_MeshRendererEditor.GetMethod("OnInspectorGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        method.Invoke(editor, null);
    }
    [MethodImpl(MethodImplOptions.NoOptimization)]
    static void SubRendererOnInspectorGUIProxyE(UnityEditor.Editor editor)
    {
        MethodInfo method = m_type_SkinnedMeshRendererEditor.GetMethod("OnInspectorGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        method.Invoke(editor, null);
    }
    public static string[] GetSortingLayerNames()
    {
        //SortingLayer.layers
        Type internalEditorUtilityType = typeof(InternalEditorUtility);
        PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
        return (string[])sortingLayersProperty.GetValue(null, new object[0]);
    }
}
#endif
}