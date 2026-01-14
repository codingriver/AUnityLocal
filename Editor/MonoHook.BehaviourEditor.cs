using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AUnityLocal.Editor
{
    
#if true
[InitializeOnLoad]
public static class BehaviourEditor
{
    static BehaviourEditor()
    {
        Register();
    }
    public static void Register()
    {
        if (m_type == null)
            m_type = typeof(UnityEngine.Behaviour);
        Debug.Assert(m_type != null);
        PropertyInfo prop = m_type.GetProperty("enabled");
        Debug.Assert(prop != null);
        MethodInfo method= prop.GetSetMethod();
        
        MethodInfo methodReplacement = new Action<UnityEngine.Behaviour, bool>(MethodReplacement).Method;
        MethodInfo methodProxy = new Action<UnityEngine.Behaviour, bool>(MethodProxy).Method;
        
        new MethodHook(method, methodReplacement, methodProxy).Install();
        Debug.LogWarning($"[MonoHook.BehaviourEditor] Register OK!");
        // SpriteRenderer renderer = null;
        // renderer.enabled = false;

    }

    static Type m_type = null;

    static void MethodReplacement(UnityEngine.Behaviour obj,bool v)
    {

        UnityEngine.Debug.LogFormat("[MonoHook.BehaviourEditor][{0}],enabled({1})", obj.name,v);
        MethodProxy(obj, v);
    }
    [MethodImpl( MethodImplOptions.NoOptimization)]
    static void MethodProxy(UnityEngine.Behaviour obj,bool v)
    {
        
    }
}
#endif
}