using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AUnityLocal.Editor
{
    
    public enum WindowArea
    {
        Left = 0,    // 左侧区域
        LeftMid = 1,    // 左侧区域
        RightMid = 2,   // 右侧区域
        Right = 3    // 右侧区域
    }
    
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class WindowToolGroupAttribute : Attribute
    {
        public WindowArea Area { get; set; } = WindowArea.Left;
        public int Order { get; set; } = 0;
    
        public WindowToolGroupAttribute(int order = 500,WindowArea area = WindowArea.Left)
        {
            Area = area;
            Order = order;
        }
    }    
}