using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EditorExtensions
{
    /// <summary>
    /// ReorderableList的通用包装器，简化使用
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    public class GenericReorderableList<T> where T : class, new()
    {
        private ReorderableList _reorderableList;
        private List<T> _dataList;
        private string _headerText;

        // 回调函数
        public Action<Rect, int, T> OnDrawElement;
        public Action<T> OnAddElement;
        public Action<int, T> OnRemoveElement;
        public Func<string> OnSelectPath;

        // 配置属性
        public bool Draggable { get; set; } = true;
        public float ElementHeight { get; set; } = 22f;
        public bool ShowAddRemoveButtons { get; set; } = true;
        
        // 新增：粘贴相关回调
        public Func<UnityEngine.Object[], List<T>> OnPasteObjects;
        public Func<bool> CanPaste;
        public Action<List<T>> OnPasteComplete;
        
        // 新增：是否显示粘贴按钮
        public bool ShowPasteButton { get; set; } = true;        
        private EditorWindow _parentWindow;
        private Vector2 _scrollPosition;        

        public GenericReorderableList(List<T> dataList, string headerText = "List Items", EditorWindow parentWindow = null)
        {
            _dataList = dataList;
            _headerText = headerText;
            _parentWindow = parentWindow;
            InitializeReorderableList();
        }

        private void InitializeReorderableList()
        {
            _reorderableList = new ReorderableList(_dataList, typeof(T));
            _reorderableList.drawElementCallback = DrawElementCallback;
            _reorderableList.drawHeaderCallback = DrawHeaderCallback;
            _reorderableList.onAddCallback = AddCallback;
            _reorderableList.onRemoveCallback = RemoveCallback;
            _reorderableList.drawFooterCallback = DrawFooterCallback; // 添加自定义底部绘制

            UpdateSettings();
        }

        private void UpdateSettings()
        {
            _reorderableList.draggable = Draggable;
            _reorderableList.elementHeight = ElementHeight;
            _reorderableList.displayAdd = false;
            _reorderableList.displayRemove = false;            
        }
        private void DrawFooterCallback(Rect rect)
        {
            float buttonWidth = 25f;
            float spacing = 5f;
    
            // 自定义按钮（在最左边）
            Rect customButtonRect = new Rect(rect.xMax - buttonWidth * 3 - spacing * 2, rect.y, buttonWidth, rect.height);
            if (GUI.Button(customButtonRect, "P", EditorStyles.miniButtonLeft))
            {
                // 自定义按钮逻辑
                Debug.Log("Custom Button Clicked!");
            }
    
            
            if (ShowAddRemoveButtons)
            {
                // 加号按钮
                Rect addButtonRect = new Rect(rect.xMax - buttonWidth * 2 - spacing, rect.y, buttonWidth, rect.height);
                if (GUI.Button(addButtonRect, "+", EditorStyles.miniButtonMid))
                {
                    AddCallback(_reorderableList);
                }
        
                // 减号按钮
                Rect removeButtonRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);
                GUI.enabled = _reorderableList.index >= 0 && _reorderableList.index < _dataList.Count;
                if (GUI.Button(removeButtonRect, "-", EditorStyles.miniButtonRight))
                {
                    RemoveCallback(_reorderableList);
                }
                GUI.enabled = true;
            }
        }
        private void DrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, _headerText);
        }

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= 0 && index < _dataList.Count)
            {
                OnDrawElement?.Invoke(rect, index, _dataList[index]);
            }
        }

        private void AddCallback(ReorderableList list)
        {
            T newItem = new T();
            OnAddElement?.Invoke(newItem);
            _dataList.Add(newItem);
        }

        private void RemoveCallback(ReorderableList list)
        {
            if (list.index >= 0 && list.index < _dataList.Count)
            {
                T removedItem = _dataList[list.index];
                OnRemoveElement?.Invoke(list.index, removedItem);
                _dataList.RemoveAt(list.index);
            }
        }

        public void DoLayoutList()
        {
            UpdateSettings();
            _reorderableList.DoLayoutList();
        }

        public void DoList(Rect rect)
        {
            UpdateSettings();
            _reorderableList.DoList(rect);
        }

        public void SetHeaderText(string headerText)
        {
            _headerText = headerText;
        }
    }

    /// <summary>
    /// 带路径选择功能的ReorderableList包装器
    /// </summary>
    /// <typeparam name="T">必须实现IPathFilter接口的类型</typeparam>
    public class PathFilterReorderableList<T> : GenericReorderableList<T> where T : class, IPathFilter, new()
    {
        private EditorWindow _parentWindow;

        public PathFilterReorderableList(System.Collections.Generic.List<T> dataList, string headerText,
            EditorWindow parentWindow = null)
            : base(dataList, headerText)
        {
            _parentWindow = parentWindow;

            // 设置默认的绘制和添加回调
            OnDrawElement = DrawPathFilterElement;
            OnAddElement = AddPathFilterElement;
        }

        private void DrawPathFilterElement(Rect rect, int index, T filter)
        {
            const float GAP = 5;
            rect.y++;

            Rect r = rect;

            // 启用复选框
            r.width = 60;
            r.height = 18;
            filter.Valid = GUI.Toggle(r, filter.Valid, new GUIContent("启用"));

            // 路径文本框
            r.xMin = r.xMax + GAP;
            r.xMax = rect.xMax - 300;
            GUI.enabled = false;
            filter.Path = GUI.TextField(r, filter.Path ?? "");
            GUI.enabled = true;

            // 选择按钮
            r.xMin = r.xMax + GAP;
            r.width = 50;
            if (GUI.Button(r, new GUIContent("Select", "选择目录")))
            {
                string selectedPath = SelectFolder();
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    filter.Path = selectedPath;
                }
            }

            // 过滤器文本框
            r.xMin = r.xMax + GAP;
            r.xMax = rect.xMax;
            filter.Filter = EditorGUI.TextField(r, filter.Filter ?? "");
        }

        private void AddPathFilterElement(T filter)
        {
            string path = SelectFolder();
            if (!string.IsNullOrEmpty(path))
            {
                filter.Path = path;
                filter.Filter = "*.prefab"; // 默认过滤器
                filter.Valid = true;
            }
        }

        private string SelectFolder()
        {
            string dataPath = Application.dataPath;
            string selectedPath = EditorUtility.OpenFolderPanel("选择路径", dataPath, "");

            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(dataPath))
                {
                    return "Assets/" + selectedPath.Substring(dataPath.Length + 1);
                }
                else
                {
                    if (_parentWindow != null)
                    {
                        _parentWindow.ShowNotification(new GUIContent("不能在Assets目录之外!"));
                    }
                    else
                    {
                        Debug.LogWarning("不能在Assets目录之外!");
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 专门用于IObjectFilter的ReorderableList包装器
    /// </summary>
    /// <typeparam name="TFilter">过滤器类型，必须实现IObjectFilter接口</typeparam>
    /// <typeparam name="TData">数据类型</typeparam>
    public class ObjectFilterReorderableList<TFilter, TData> : GenericReorderableList<TFilter>
        where TFilter : class, IObjectFilter<TData>, new()
    {
        private EditorWindow _parentWindow;
        private string _dataFieldLabel;

        // 自定义绘制回调
        public Action<Rect, TData, Action<TData>> OnDrawDataField;

        public ObjectFilterReorderableList(
            List<TFilter> dataList,
            string headerText,
            string dataFieldLabel = "Data",
            EditorWindow parentWindow = null)
            : base(dataList, headerText)
        {
            _parentWindow = parentWindow;
            _dataFieldLabel = dataFieldLabel;

            // 设置默认的绘制和添加回调
            OnDrawElement = DrawObjectFilterElement;
            OnAddElement = AddObjectFilterElement;

            // 设置更高的元素高度以容纳更多内容
            ElementHeight = 22f;
        }

        private void DrawObjectFilterElement(Rect rect, int index, TFilter filter)
        {
            const float GAP = 5;
            const float TOGGLE_WIDTH = 60;

            rect.y += 1;
            rect.height = 18;

            Rect currentRect = rect;

            // 启用复选框
            currentRect.width = TOGGLE_WIDTH;
            filter.Valid = GUI.Toggle(currentRect, filter.Valid, new GUIContent("启用"));

            // 数据字段
            currentRect.x += TOGGLE_WIDTH + GAP;
            currentRect.width = rect.width - TOGGLE_WIDTH - GAP;

            DrawDataField(currentRect, filter);
        }

        private void DrawDataField(Rect rect, TFilter filter)
        {
            if (OnDrawDataField != null)
            {
                // 使用自定义绘制
                OnDrawDataField.Invoke(rect, filter.data, (newData) => filter.data = newData);
            }
            else
            {
                // 默认绘制逻辑
                DrawDefaultDataField(rect, filter);
            }
        }

        private void DrawDefaultDataField(Rect rect, TFilter filter)
        {
            Type dataType = typeof(TData);

            // 根据数据类型选择合适的绘制方式
            if (dataType == typeof(string))
            {
                filter.data = (TData)(object)EditorGUI.TextField(rect, _dataFieldLabel, filter.data?.ToString() ?? "");
            }
            else if (dataType == typeof(int))
            {
                int value = filter.data != null ? (int)(object)filter.data : 0;
                filter.data = (TData)(object)EditorGUI.IntField(rect, _dataFieldLabel, value);
            }
            else if (dataType == typeof(float))
            {
                float value = filter.data != null ? (float)(object)filter.data : 0f;
                filter.data = (TData)(object)EditorGUI.FloatField(rect, _dataFieldLabel, value);
            }
            else if (dataType == typeof(bool))
            {
                bool value = filter.data != null ? (bool)(object)filter.data : false;
                filter.data = (TData)(object)EditorGUI.Toggle(rect, _dataFieldLabel, value);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(dataType))
            {
                // Unity对象引用
                UnityEngine.Object obj = filter.data as UnityEngine.Object;
                filter.data = (TData)(object)EditorGUI.ObjectField(rect, _dataFieldLabel, obj, dataType, true);
            }
            else if (dataType.IsEnum)
            {
                // 枚举类型
                Enum enumValue = filter.data as Enum ?? (Enum)Enum.GetValues(dataType).GetValue(0);
                filter.data = (TData)(object)EditorGUI.EnumPopup(rect, _dataFieldLabel, enumValue);
            }
            else
            {
                // 其他类型显示为标签
                EditorGUI.LabelField(rect, _dataFieldLabel, filter.data?.ToString() ?? "null");
            }
        }

        private void AddObjectFilterElement(TFilter filter)
        {
            filter.Valid = true;
            // data会使用默认值 (new T() 或 null)
        }
    }

// 字符串过滤器
    [System.Serializable]
    public class StringFilter : IObjectFilter<string>
    {
        [SerializeField] private bool _valid = true;
        [SerializeField] private string _data = "";

        public bool Valid
        {
            get => _valid;
            set => _valid = value;
        }

        public string data
        {
            get => _data;
            set => _data = value;
        }
    }

// GameObject过滤器
    [System.Serializable]
    public class GameObjectFilter : IObjectFilter<GameObject>
    {
        [SerializeField] private bool _valid = true;
        [SerializeField] private GameObject _data;

        public bool Valid
        {
            get => _valid;
            set => _valid = value;
        }

        public GameObject data
        {
            get => _data;
            set => _data = value;
        }
    }

// 整数过滤器
    [System.Serializable]
    public class IntFilter : IObjectFilter<int>
    {
        [SerializeField] private bool _valid = true;
        [SerializeField] private int _data = 0;

        public bool Valid
        {
            get => _valid;
            set => _valid = value;
        }

        public int data
        {
            get => _data;
            set => _data = value;
        }
    }

// 自定义数据类过滤器
    [System.Serializable]
    public class CustomData
    {
        public string name;
        public int value;
        public bool enabled;

        public override string ToString()
        {
            return $"{name} ({value})";
        }
    }

    [System.Serializable]
    public class CustomDataFilter : IObjectFilter<CustomData>
    {
        [SerializeField] private bool _valid = true;
        [SerializeField] private CustomData _data = new CustomData();

        public bool Valid
        {
            get => _valid;
            set => _valid = value;
        }

        public CustomData data
        {
            get => _data;
            set => _data = value;
        }
    }

// 枚举过滤器
    public enum FilterType
    {
        Include,
        Exclude,
        Transform
    }

    [System.Serializable]
    public class EnumFilter : IObjectFilter<FilterType>
    {
        [SerializeField] private bool _valid = true;
        [SerializeField] private FilterType _data = FilterType.Include;

        public bool Valid
        {
            get => _valid;
            set => _valid = value;
        }

        public FilterType data
        {
            get => _data;
            set => _data = value;
        }
    }
    
    
    /// <summary>
    /// 粘贴数据接口
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public interface IPasteable<T>
    {
        /// <summary>
        /// 解析剪贴板文本并转换为数据列表
        /// </summary>
        /// <param name="clipboardText">剪贴板文本</param>
        /// <returns>解析出的数据列表</returns>
        List<T> ParseClipboardText(string clipboardText);
    
        /// <summary>
        /// 验证粘贴的数据是否有效
        /// </summary>
        /// <param name="data">要验证的数据</param>
        /// <returns>是否有效</returns>
        bool ValidatePastedData(T data);
    
        /// <summary>
        /// 获取粘贴按钮的提示文本
        /// </summary>
        string GetPasteTooltip();
    }

    /// <summary>
    /// 默认的粘贴实现基类
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public abstract class DefaultPasteable<T> : IPasteable<T>
    {
        public abstract List<T> ParseClipboardText(string clipboardText);
    
        public virtual bool ValidatePastedData(T data)
        {
            return data != null;
        }
    
        public virtual string GetPasteTooltip()
        {
            return "从剪贴板粘贴数据";
        }
    }

    
    /// <summary>
    /// 路径过滤器接口
    /// </summary>
    public interface IPathFilter
    {
        bool Valid { get; set; }
        string Path { get; set; }
        string Filter { get; set; }
    }

    public interface IObjectFilter<T>
    {
        bool Valid { get; set; }
        T data { get; set; }
    }
    
}