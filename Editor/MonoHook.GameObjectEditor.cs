using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using UnityEditorInternal;
using System.Runtime.CompilerServices;
using System.Text;

namespace AUnityLocal.Editor
{
    
#if true
[InitializeOnLoad]
public static class GameObjectEditor
{
    static GameObjectEditor()
    {
        Register();
    }
    public static void Register()
    {
        if (m_type_GameObject == null)
            m_type_GameObject = typeof(GameObject).Assembly.GetType("UnityEngine.GameObject");
        Debug.Assert(m_type_GameObject != null);
        MethodInfo method = m_type_GameObject.GetMethod("SetActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic|BindingFlags.ExactBinding,null,
            new Type[] { typeof(bool)}, null);
        MethodInfo methodReplacement = typeof(GameObjectEditor).GetMethod("MethodReplacement", BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo methodProxy = typeof(GameObjectEditor).GetMethod("MethodProxy", BindingFlags.Static | BindingFlags.NonPublic);
        
        new MethodHook(method, methodReplacement, methodProxy).Install();
        Debug.LogWarning($"[MonoHook.GameObjectEditor] Register OK!");
        // UnityEditorInternal.AssemblyStripper.StripForMonoBackend();
        
    }

    static Type m_type_GameObject = null;

    static void MethodReplacement(GameObject gameObject,bool v)
    {
        UnityEngine.Debug.LogFormat("[MonoHook.GameObjectEditor][{0}],SetActive({1})", gameObject.name,v);
        MethodProxy(gameObject, v);
    }
    [MethodImpl( MethodImplOptions.NoOptimization)]
    static void MethodProxy(GameObject gameObject,bool v)
    {
        
    }
}
#endif
}