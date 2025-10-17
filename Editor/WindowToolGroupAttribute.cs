using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using FxProNS;
using Skyunion;
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
        Left = 0,    // 左侧区域A
        RightTop = 1,    // 右上区域B  
        RightBottom = 2  // 右下区域C
    }
    
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class WindowToolGroupAttribute : Attribute
    {
        public WindowArea Area { get; set; } = WindowArea.Left;
        public int Order { get; set; } = 0;
    
        public WindowToolGroupAttribute(WindowArea area = WindowArea.Left, int order = 0)
        {
            Area = area;
            Order = order;
        }
    }    
}