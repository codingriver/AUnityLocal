using System;
using System.Diagnostics;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 变量监视公共 API。所有方法标记 [Conditional("UNITY_EDITOR")]，
    /// 在非编辑器构建中自动移除，零开销。
    /// </summary>
    public static class Watch
    {
        // ─── Manual Set ───────────────────────────────────────

        [Conditional("UNITY_EDITOR")]
        public static void Set(string name, object value, string format = null)
        {
            WatchManager.SetValue(name, value, WatchValueType.Object, format);
        }

        [Conditional("UNITY_EDITOR")]
        public static void Set(string name, float value, string format = null)
        {
            WatchManager.SetFloat(name, value, format ?? "F2");
        }

        [Conditional("UNITY_EDITOR")]
        public static void Set(string name, int value, string format = null)
        {
            WatchManager.SetInt(name, value, format);
        }

        [Conditional("UNITY_EDITOR")]
        public static void Set(string name, bool value)
        {
            WatchManager.SetBool(name, value);
        }

        [Conditional("UNITY_EDITOR")]
        public static void Set(string name, Vector3 value, string format = null)
        {
            WatchManager.SetValue(name, value, WatchValueType.Vector, format ?? "F2");
        }

        [Conditional("UNITY_EDITOR")]
        public static void Set(string name, Vector2 value, string format = null)
        {
            WatchManager.SetValue(name, value, WatchValueType.Vector, format ?? "F2");
        }

        [Conditional("UNITY_EDITOR")]
        public static void Set(string name, Color value, string format = null)
        {
            WatchManager.SetValue(name, value, WatchValueType.Color, format);
        }

        [Conditional("UNITY_EDITOR")]
        public static void Set(string name, string value)
        {
            WatchManager.SetValue(name, value, WatchValueType.String, null);
        }

        // ─── Auto-watch ──────────────────────────────────────

        [Conditional("UNITY_EDITOR")]
        public static void Auto(string name, Func<object> getter, UnityEngine.Object owner, string format = null)
        {
            WatchManager.RegisterAuto(name, getter, owner, format);
        }

        // ─── Remove ──────────────────────────────────────────

        [Conditional("UNITY_EDITOR")]
        public static void Remove(string name)
        {
            WatchManager.RemoveEntry(name);
        }

        [Conditional("UNITY_EDITOR")]
        public static void RemoveAll(UnityEngine.Object owner)
        {
            WatchManager.RemoveByOwner(owner);
        }

        [Conditional("UNITY_EDITOR")]
        public static void Clear()
        {
            WatchManager.ClearAll();
        }

        // ─── Pause ───────────────────────────────────────────

        [Conditional("UNITY_EDITOR")]
        public static void SetPaused(string name, bool paused)
        {
            WatchManager.SetEntryPaused(name, paused);
        }
    }
}
