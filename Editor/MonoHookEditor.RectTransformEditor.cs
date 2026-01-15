using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using UnityEditorInternal;
using System.Runtime.CompilerServices;
using System.Text;
using Skyunion;
using Debug = UnityEngine.Debug;

namespace AUnityLocal.Editor
{
    static partial class MonoHookEditor
    {
        public delegate void RectTransformVector2Action(RectTransform rectTrans, Vector2 vec2);
        public static class RectTransformEditor
        {
            public static void Register()
            {
                Type type = typeof(UnityEngine.RectTransform);
                
                AddMHookForPropertySetMethod<RectTransformVector2Action>(type,"sizeDelta", MethodReplacement, MethodProxy);
                
                // MonoHookEditor.AddMHookForPropertySetMethod<Action<RectTransform, Vector2>>(type,"anchorMin", MethodReplacement2, MethodProxy2);
                // MonoHookEditor.AddMHookForPropertySetMethod<Action<RectTransform, Vector2>>(type,"anchorMax", MethodReplacement1, MethodProxy1);
                // MonoHookEditor.AddMHookForPropertySetMethod<Action<RectTransform, Vector2>>(type,"offsetMin", MethodReplacement3, MethodProxy3);
                // MonoHookEditor.AddMHookForPropertySetMethod<Action<RectTransform, Vector2>>(type,"offsetMax", MethodReplacement4, MethodProxy4);
                Debug.LogWarning($"[MonoHookEditor.RectTransformEditor] Register OK!");
            }

            
            static void MethodReplacement(UnityEngine.RectTransform obj, Vector2 v)
            {
                if (v != null)
                {
                    UnityEngine.Debug.LogFormat("[MonoHookEditor.RectTransformEditor][{0}],sizeDelta({1})", obj.name, v);
                    MethodProxy(obj, v);                    
                }
                else
                {
                    UnityEngine.Debug.LogFormat("[MonoHookEditor.RectTransformEditor][{0}],sizeDelta(NULL)", obj.name);
                    MethodProxy(obj, v);
                }

            }

            [MethodImpl(MethodImplOptions.NoOptimization)]
            static void MethodProxy(UnityEngine.RectTransform obj,Vector2 v)
            {
            }

            static void MethodReplacement1(UnityEngine.RectTransform obj, Vector2 v)
            {
                UnityEngine.Debug.LogFormat("[MonoHook.RectTransformEditor][{0}],anchorMax({1})", obj.name, v);
                MethodProxy1(obj, v);
            }

            [MethodImpl(MethodImplOptions.NoOptimization)]
            static void MethodProxy1(UnityEngine.RectTransform obj, Vector2 v)
            {
            }

            static void MethodReplacement2(UnityEngine.RectTransform obj, Vector2 v)
            {
                CoreUtils.logService.Warn("[MonoHook.RectTransformEditor][{0}],anchorMin({1})", obj.name, v);
                MethodProxy2(obj, v);
            }

            [MethodImpl(MethodImplOptions.NoOptimization)]
            static void MethodProxy2(UnityEngine.RectTransform obj, Vector2 v)
            {
            }

            static void MethodReplacement3(UnityEngine.RectTransform obj, Vector2 v)
            {
                UnityEngine.Debug.LogFormat("[MonoHook.RectTransformEditor][{0}],offsetMin({1})", obj.name, v);
                MethodProxy3(obj, v);
            }

            [MethodImpl(MethodImplOptions.NoOptimization)]
            static void MethodProxy3(UnityEngine.RectTransform obj, Vector2 v)
            {
            }

            static void MethodReplacement4(UnityEngine.RectTransform obj, Vector2 v)
            {
                UnityEngine.Debug.LogFormat("[MonoHook.RectTransformEditor][{0}],offsetMax({1})", obj.name, v);
                MethodProxy4(obj, v);
            }

            [MethodImpl(MethodImplOptions.NoOptimization)]
            static void MethodProxy4(UnityEngine.RectTransform obj, Vector2 v)
            {
            }
        }
    }
}