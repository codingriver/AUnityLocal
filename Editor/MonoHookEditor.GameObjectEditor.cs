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
    static partial class MonoHookEditor
    {
        public static class GameObjectEditor
        {
            public static void Register()
            {
                Type type = typeof(GameObject).Assembly.GetType("UnityEngine.GameObject");
                Debug.Assert(type != null);
                MethodInfo method = type.GetMethod("SetActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding, null, new Type[] { typeof(bool) }, null);
                MonoHookEditor.AddMHook<Action<GameObject,bool>>(method,MethodReplacement, MethodProxy);
                Debug.LogWarning($"[MonoHookEditor.GameObjectEditor] Register OK!");
            }

            static void MethodReplacement(GameObject gameObject, bool v)
            {
                UnityEngine.Debug.LogFormat("[MonoHookEditor.GameObjectEditor][{0}],SetActive({1})", gameObject.name, v);
                MethodProxy(gameObject, v);
            }

            [MethodImpl(MethodImplOptions.NoOptimization)]
            static void MethodProxy(GameObject gameObject, bool v)
            {
            }
        }
    }
}