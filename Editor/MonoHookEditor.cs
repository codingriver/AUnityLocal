using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AUnityLocal.Editor
{
    [InitializeOnLoad]
    static partial class MonoHookEditor
    {
        static MonoHookEditor()
        {
            Register();
        }

        static void Register()
        {
            MeshRendererEditor.Register();
            SkinnedMeshRendererEditor.Register();            
            GameObjectEditor.Register();
            // BehaviourEditor.Register();
            RectTransformEditor.Register();

        }

        /// <summary>
        /// 注册属性set方法钩子
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="replacement"></param>
        /// <param name="proxy"></param>
        /// <typeparam name="T"></typeparam>
        static void AddMHookForPropertySetMethod<T>(Type type, string propertyName, T replacement, T proxy)
            where T : Delegate
        {
            Debug.Assert(type != null);
            PropertyInfo prop = type.GetProperty(propertyName);
            Debug.Assert(prop != null);
            new MethodHook(prop.GetSetMethod(), replacement.Method, proxy.Method).Install();
        }

        /// <summary>
        /// 注册钩子
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="replacement"></param>
        /// <param name="proxy"></param>
        /// <typeparam name="T"></typeparam>
        static void AddMHook<T>(MethodInfo origin, T replacement, T proxy) where T : Delegate
        {
            Debug.Assert(origin != null);
            new MethodHook(origin, replacement.Method, proxy.Method).Install();
        }
    }
}