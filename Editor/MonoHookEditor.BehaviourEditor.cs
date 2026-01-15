using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AUnityLocal.Editor
{
    static partial class MonoHookEditor
    {
        static class BehaviourEditor
        {

            public static void Register()
            {
                Type type = typeof(UnityEngine.Behaviour);
                AddMHookForPropertySetMethod<Action<Behaviour, bool>>(type,"enabled", MethodReplacement, MethodProxy);
                Debug.LogWarning($"[MonoHookEditor.BehaviourEditor] Register OK!");
            }

            static void MethodReplacement(UnityEngine.Behaviour obj, bool v)
            {
                UnityEngine.Debug.LogFormat("[MonoHookEditor.BehaviourEditor][{0}],enabled({1})", obj.name, v);
                MethodProxy(obj, v);
            }

            [MethodImpl(MethodImplOptions.NoOptimization)]
            static void MethodProxy(UnityEngine.Behaviour obj, bool v)
            {
            }
        }
    }
}