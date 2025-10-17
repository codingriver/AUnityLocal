using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using BehaviorDesigner.Runtime.Tasks.Unity.SharedVariables;
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
    
    /// <summary>
    /// 模板
    /// </summary>
    [WindowToolGroup(500,WindowArea.RightMid)]
    public class WindowToolGroupM : WindowToolGroup
    {
        public override string title { get; } = "";
        public override string tip { get; } = "";
        
        public override void OnGUI()
        {

        }
        
    }
    public abstract class WindowToolGroupReorderableList<TData> : WindowToolGroup
    {
        public override string title { get; } = "搜索结果";
        public override string tip { get; } = "";
        ReorderableList<TData> _ReorderableList;
        static List<TData> dataList = new List<TData>();
        public  List<TData> GetData()
        {
            return dataList;
        }
        public void SetData(List<TData> _dataList)
        {
            dataList.Clear();
            dataList.AddRange(_dataList);
        }

        public override bool Show
        {
            get
            {
                return dataList.Count > 0;
            }
        } 

        public override void OnGUI()
        {
            // GameObject过滤器
            if(_ReorderableList==null)
            {
                _ReorderableList = new ReorderableList<TData>(dataList, "搜索结果");
            }
            _ReorderableList.DoLayoutList();
        }
    }

    [WindowToolGroup(500, WindowArea.Right)]
    public class WindowToolGroupReorderableListInt : WindowToolGroupReorderableList<int>
    {
        
    }
    [WindowToolGroup(500, WindowArea.Right)]
    public class WindowToolGroupReorderableListString : WindowToolGroupReorderableList<string>
    {
        
    }
    [WindowToolGroup(500, WindowArea.Right)]
    public class WindowToolGroupReorderableListObject : WindowToolGroupReorderableList<Object>
    {
        
    }    
    [WindowToolGroup(500, WindowArea.Right)]
    public class WindowToolGroupReorderableListBool : WindowToolGroupReorderableList<bool>
    {
    }        
}