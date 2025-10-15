using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AUnityLocal.Editor
{
    /// <summary>
    /// ReorderableList的通用包装器，简化使用
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    public class ReorderableList<T>
    {
        private ReorderableList _reorderableList;
        protected List<T> _dataList;
        private string _headerText;
        public List<T> dataList => _dataList;
        

        // 回调函数
        /// <summary>
        /// 绘制每个元素
        /// </summary>
        public Action<Rect, int, T> OnDrawElement;
        
        /// <summary>
        /// 添加新元素
        /// </summary>
        public Func<T> OnAddElement;
        /// <summary>
        /// 删除元素
        /// </summary>
        public Action<T> OnRemoveElement;
        /// <summary>
        /// 粘贴回调
        /// </summary>
        public Func<object,List<T>> OnPaste;

        public int height = 130;
        /// <summary>
        /// 显示粘贴按钮
        /// </summary>
        public bool ShowPasteButton { get; set; } = true;
        // 配置属性
        public bool Draggable { get; set; } = true;
        public float ElementHeight { get; set; } = 22f;
        public bool ShowAddRemoveButtons { get; set; } = true;

        // 新增：是否显示粘贴按钮
        // private EditorWindow _parentWindow;
        private Vector2 _scrollPosition;
        
        public ReorderableList(List<T> dataList, string headerText = "List Items",int height=120,Action<Rect, int, T> _onDrawElement=null,Func<T> _onAddElement=null,Action<T> _onRemoveElement=null, Func<object,List<T>> _onPaste=null)
        {
            _dataList = dataList;
            _headerText = headerText;
            this.height = height;
            OnDrawElement = _onDrawElement;
            OnAddElement = _onAddElement;
            OnRemoveElement = _onRemoveElement;
            OnPaste = _onPaste;
            InitializeReorderableList();
        }
        public ReorderableList(string headerText = "List Items",int height=130)
        {
            _dataList =  new List<T>();
            _headerText = headerText;
            this.height = height;
            InitializeReorderableList();
        }
        private void InitializeReorderableList()
        {
            _reorderableList = new ReorderableList(_dataList, typeof(T));
            _reorderableList.drawElementCallback = DrawElementCallback;
            _reorderableList.drawHeaderCallback = DrawHeaderCallback;
            // _reorderableList.onAddCallback = AddCallback;
            // _reorderableList.onRemoveCallback = RemoveCallback;
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

            // clear按钮
            Rect cRect = new Rect(rect.xMax - buttonWidth*6- spacing * 3, rect.y, buttonWidth*2, rect.height);
            if (GUI.Button(cRect, "Clear", EditorStyles.miniButtonMid))
            {
                foreach (var item in _dataList)
                {
                    OnRemoveElement?.Invoke(item);
                }
                _dataList.Clear();                
            }                 
            // 自定义按钮（在最左边）
            Rect customButtonRect = new Rect(rect.xMax - buttonWidth * 4 - spacing * 2, rect.y, buttonWidth*2,
                rect.height);
            if (GUI.Button(customButtonRect, "Paste", EditorStyles.miniButtonLeft))
            {
                // 自定义按钮逻辑
                HandlePaste();
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
            string countText = $"Count: {_dataList.Count}";
            Vector2 countSize = EditorStyles.label.CalcSize(new GUIContent(countText));
            Rect countRect = new Rect(rect.xMax - countSize.x, rect.y, countSize.x, rect.height);
            EditorGUI.LabelField(countRect, countText);            
        }

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= 0 && index < _dataList.Count)
            {
                if (OnDrawElement != null)
                {
                    OnDrawElement?.Invoke(rect, index, _dataList[index]);
                }
                else
                {
                    // 默认绘制逻辑
                    DrawDefaultDataField(rect, index,_dataList[index]);                    
                }
                
            }
        }

        private void AddCallback(ReorderableList list)
        {
            if (OnAddElement != null)
            {
                _dataList.Add(OnAddElement());    
            }
            else
            {
                _dataList.Add(OnAddElementDefault());
            }
        }
        public T OnAddElementDefault()
        {
            Type type = typeof(T);
            // 检查是否为值类型（struct）
            if (type.IsValueType)
            {
                return (T)Activator.CreateInstance(type);
            }
            
            // 检查是否为UnityEngine命名空间下的类型
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine")||type.Namespace.StartsWith("UnityEditor"))
            {
                return default(T); // 返回null（对于引用类型）或默认值（对于值类型）
            }
        
            // 检查是否为引用类型（class）
            if (type.IsClass)
            {
                // 检查是否有无参构造函数
                if (type.GetConstructor(Type.EmptyTypes) != null)
                {
                    return (T)Activator.CreateInstance(type);
                }
                else
                {
                    // 如果没有无参构造函数，返回null
                    return default(T);
                }
            }
        
            // 其他情况返回默认值
            return default(T);
        }

        private void RemoveCallback(ReorderableList list)
        {
            if (list.index >= 0 && list.index < _dataList.Count)
            {
                T removedItem = _dataList[list.index];
                OnRemoveElement?.Invoke(removedItem);
                _dataList.RemoveAt(list.index);
            }
        }

        public void DoLayoutList()
        {
            UpdateSettings();
            EditorGUILayout.Separator();
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition,GUILayout.Height(height));
            {
                _reorderableList.DoLayoutList();
            }
            GUILayout.EndScrollView();         
            EditorGUILayout.Separator();
            
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

        #region 粘贴功能
        /// <summary>
        /// 手动触发粘贴
        /// </summary>
        public virtual void Paste()
        {
            HandlePaste();
        }
        /// <summary>
        /// 检查是否可以粘贴
        /// </summary>
        public virtual bool CanPaste()
        {
            // return  !string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer);
            return true;
        }
        private void HandlePaste()
        {
            try
            {
                Type type = typeof(T);
                var objects = Selection.objects;
                List<T> parsedData = null;
                if (objects != null && objects.Length > 0&&!type.IsValueType)
                {
                    parsedData = ParseClipboard(objects);
                }
                else
                {
                    parsedData = ParseClipboard(EditorGUIUtility.systemCopyBuffer);
                }


                if (parsedData == null || parsedData.Count == 0)
                {
                    ShowPasteMessage("无法解析剪贴板内容", false);
                    return;
                }

                var validData = parsedData;

                // 记录粘贴位置
                int pasteStartIndex = _dataList.Count;

                // 添加数据到列表
                foreach (var data in validData)
                {
                    _dataList.Add(data);
                }

                // 选中第一个粘贴的元素
                if (validData.Count > 0)
                {
                    _reorderableList.index = pasteStartIndex;
                }

                // 显示成功消息
                string message = validData.Count == parsedData.Count
                    ? $"成功粘贴 {validData.Count} 条数据"
                    : $"成功粘贴 {validData.Count} 条数据（共 {parsedData.Count} 条，{parsedData.Count - validData.Count} 条无效）";

                ShowPasteMessage(message, true);
            }
            catch (System.Exception ex)
            {
                ShowPasteMessage($"解析数据时发生错误：{ex.Message}", false);
                Debug.LogError($"Paste error: {ex}");
            }
        }
        private void ShowPasteMessage(string message, bool success)
        {
            if (success)
            {
                Debug.Log($"[GenericReorderableList] {message}");
            }
            else
            {
                EditorUtility.DisplayDialog("粘贴失败", message, "确定");
            }
        }
        public virtual List<T> ParseClipboard(object clipboard)
        {
            if (OnPaste != null)
            {
                return OnPaste(clipboard);
            }
            
            var result = new List<T>();

            // 处理Unity对象数组（从Asset或Hierarchy选择的对象）
            if (clipboard is UnityEngine.Object[])
            {
                var objects = clipboard as UnityEngine.Object[];
                if (objects == null || objects.Length == 0)
                    return result;

                foreach (var obj in objects)
                {
                    if (obj == null) continue;

                    T data = ProcessUnityObject(obj);
                    if (data != null)
                    {
                        result.Add(data);
                    }
                }
                result = result.Where(d => d != null).ToList();

                return result;
            }

            // 处理字符串剪贴板内容
            string clipboardText = clipboard as string;
            if (string.IsNullOrEmpty(clipboardText))
                return result;

            string[] lines = clipboardText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                try
                {
                    // 尝试解析数据
                    T data = ParseLineToData(trimmedLine);
                    result.Add(data);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to parse line '{trimmedLine}': {ex.Message}");
                }
            }
            Type type = typeof(T);
            if (type.IsClass)
            {
                result = result.Where(d => d != null).ToList();
            }            

            return result;
        }

  /// <summary>
        /// 处理Unity对象，将其转换为TFilter
        /// </summary>
        private T ProcessUnityObject(UnityEngine.Object obj)
        {
            try
            {
                return ConvertUnityObjectToData(obj);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to process Unity object '{obj.name}': {ex.Message}");
            }

            return default(T);
        }

        /// <summary>
        /// 将Unity对象转换为目标数据类型
        /// </summary>
        private T ConvertUnityObjectToData(UnityEngine.Object obj)
        {
            Type dataType = typeof(T);

            try
            {
                // 直接类型匹配
                if (dataType.IsAssignableFrom(obj.GetType()))
                {
                    return (T)(object)obj;
                }

                // GameObject相关转换
                if (obj is GameObject gameObject)
                {
                    return ConvertGameObjectToData(gameObject, dataType);
                }

                // Component相关转换
                if (obj is Component component)
                {
                    return ConvertComponentToData(component, dataType);
                }

                // Asset相关转换
                return ConvertAssetToData(obj, dataType);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Error converting Unity object '{obj.name}' to {dataType}: {ex.Message}");
            }

            return default(T);
        }

        /// <summary>
        /// 转换GameObject到目标数据类型
        /// </summary>
        private T ConvertGameObjectToData(GameObject gameObject, Type dataType)
        {
            // 如果目标类型是GameObject
            if (dataType == typeof(GameObject))
            {
                return (T)(object)gameObject;
            }

            // 如果目标类型是Transform
            if (dataType == typeof(Transform))
            {
                return (T)(object)gameObject.transform;
            }

            // 如果目标类型是字符串，返回名称
            if (dataType == typeof(string))
            {
                return (T)(object)gameObject.name;
            }

            // 如果目标类型是Vector3，返回位置
            if (dataType == typeof(Vector3))
            {
                return (T)(object)gameObject.transform.position;
            }

            // 尝试获取指定类型的组件
            if (typeof(Component).IsAssignableFrom(dataType))
            {
                Component component = gameObject.GetComponent(dataType);
                if (component != null)
                {
                    return (T)(object)component;
                }
            }

            return default(T);
        }

        /// <summary>
        /// 转换Component到目标数据类型
        /// </summary>
        private T ConvertComponentToData(Component component, Type dataType)
        {
            // 直接类型匹配
            if (dataType.IsAssignableFrom(component.GetType()))
            {
                return (T)(object)component;
            }

            // 如果目标类型是GameObject
            if (dataType == typeof(GameObject))
            {
                return (T)(object)component.gameObject;
            }

            // 如果目标类型是Transform
            if (dataType == typeof(Transform))
            {
                return (T)(object)component.transform;
            }

            // 如果目标类型是字符串，返回组件名称
            if (dataType == typeof(string))
            {
                return (T)(object)component.name;
            }

            // 如果目标类型是Vector3且组件是Transform，返回位置
            if (dataType == typeof(Vector3) && component is Transform transform)
            {
                return (T)(object)transform.position;
            }

            // 尝试从同一GameObject获取目标类型组件
            if (typeof(Component).IsAssignableFrom(dataType))
            {
                Component targetComponent = component.GetComponent(dataType);
                if (targetComponent != null)
                {
                    return (T)(object)targetComponent;
                }
            }

            return default(T);
        }

        /// <summary>
        /// 转换Asset到目标数据类型
        /// </summary>
        private T ConvertAssetToData(UnityEngine.Object asset, Type dataType)
        {
            // 直接类型匹配
            if (dataType.IsAssignableFrom(asset.GetType()))
            {
                return (T)(object)asset;
            }

            // 如果目标类型是字符串，返回资源名称
            if (dataType == typeof(string))
            {
                return (T)(object)asset.name;
            }

            // 特殊处理：Texture到Sprite的转换
            if (dataType == typeof(Sprite) && asset is Texture2D texture)
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    return (T)(object)sprite;
                }
            }

            // 特殊处理：Sprite到Texture的转换
            if (dataType == typeof(Texture2D) && asset is Sprite sprite1)
            {
                return (T)(object)sprite1.texture;
            }

            return default(T);
        }

// 保持原有的ParseLineToData等方法不变...
        private T ParseLineToData(string line)
        {
            Type dataType = typeof(T);

            try
            {
                // 根据数据类型解析
                if (dataType == typeof(string))
                {
                    return (T)(object)line;
                }
                else if (dataType == typeof(int))
                {
                    if (int.TryParse(line, out int intValue))
                        return (T)(object)intValue;
                }
                else if (dataType == typeof(float))
                {
                    if (float.TryParse(line, out float floatValue))
                        return (T)(object)floatValue;
                }
                else if (dataType == typeof(double))
                {
                    if (double.TryParse(line, out double doubleValue))
                        return (T)(object)doubleValue;
                }
                else if (dataType == typeof(bool))
                {
                    if (bool.TryParse(line, out bool boolValue))
                        return (T)(object)boolValue;
                }
                else if (dataType == typeof(Vector2))
                {
                    return ParseVector2(line);
                }
                else if (dataType == typeof(Vector3))
                {
                    return ParseVector3(line);
                }
                else if (dataType == typeof(Color))
                {
                    return ParseColor(line);
                }
                else if (dataType.IsEnum)
                {
                    if (System.Enum.TryParse(dataType, line, true, out object enumValue))
                        return (T)enumValue;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Error parsing '{line}' as {dataType}: {ex.Message}");
            }

            return default(T);
        }

        private T ParseVector2(string line)
        {
            // 支持格式: "1,2" 或 "(1,2)" 或 "1 2"
            string cleaned = line.Trim('(', ')', ' ');
            string[] parts = cleaned.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2 &&
                float.TryParse(parts[0], out float x) &&
                float.TryParse(parts[1], out float y))
            {
                return (T)(object)new Vector2(x, y);
            }

            return default(T);
        }

        private T ParseVector3(string line)
        {
            // 支持格式: "1,2,3" 或 "(1,2,3)" 或 "1 2 3"
            string cleaned = line.Trim('(', ')', ' ');
            string[] parts = cleaned.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3 &&
                float.TryParse(parts[0], out float x) &&
                float.TryParse(parts[1], out float y) &&
                float.TryParse(parts[2], out float z))
            {
                return (T)(object)new Vector3(x, y, z);
            }

            return default(T);
        }

        private T ParseColor(string line)
        {
            // 支持十六进制颜色 "#RRGGBB" 或 "RRGGBB"
            if (line.StartsWith("#"))
                line = line.Substring(1);

            if (line.Length == 6 && System.Int32.TryParse(line, System.Globalization.NumberStyles.HexNumber, null,
                    out int colorValue))
            {
                float r = ((colorValue >> 16) & 0xFF) / 255f;
                float g = ((colorValue >> 8) & 0xFF) / 255f;
                float b = (colorValue & 0xFF) / 255f;
                return (T)(object)new Color(r, g, b, 1f);
            }

            return default(T);
        }
        #endregion

        public void DrawDefaultDataField(Rect rect,int index, T data)
        {
            string _dataFieldLabel = string.Empty;
            Type dataType = typeof(T);
            T newData = data;

            try
            {
                // 根据数据类型选择合适的绘制方式
                if (dataType == typeof(string))
                {
                    newData = (T)(object)EditorGUI.TextField(rect, _dataFieldLabel, data?.ToString() ?? "");
                }
                else if (dataType == typeof(int))
                {
                    int value = data != null ? (int)(object)data : 0;
                    newData = (T)(object)EditorGUI.IntField(rect, _dataFieldLabel, value);
                }
                else if (dataType == typeof(float))
                {
                    float value = data != null ? (float)(object)data : 0f;
                    newData = (T)(object)EditorGUI.FloatField(rect, _dataFieldLabel, value);
                }
                else if (dataType == typeof(double))
                {
                    double value = data != null ? (double)(object)data : 0.0;
                    newData = (T)(object)EditorGUI.DoubleField(rect, _dataFieldLabel, value);
                }
                else if (dataType == typeof(bool))
                {
                    bool value = data != null && (bool)(object)data;
                    newData = (T)(object)EditorGUI.Toggle(rect, _dataFieldLabel, value);
                }
                else if (dataType == typeof(Vector2))
                {
                    Vector2 value = data != null ? (Vector2)(object)data : Vector2.zero;
                    newData = (T)(object)EditorGUI.Vector2Field(rect, _dataFieldLabel, value);
                }
                else if (dataType == typeof(Vector3))
                {
                    Vector3 value = data != null ? (Vector3)(object)data : Vector3.zero;
                    newData = (T)(object)EditorGUI.Vector3Field(rect, _dataFieldLabel, value);
                }
                else if (dataType == typeof(Color))
                {
                    Color value = data != null ? (Color)(object)data : Color.white;
                    newData = (T)(object)EditorGUI.ColorField(rect, _dataFieldLabel, value);
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(dataType))
                {
                    // Unity对象引用
                    UnityEngine.Object obj = data as UnityEngine.Object;
                    newData = (T)(object)EditorGUI.ObjectField(rect, _dataFieldLabel, obj, dataType, true);
                }
                else if (dataType.IsEnum)
                {
                    // 枚举类型
                    Enum enumValue = data as Enum ?? (Enum)Enum.GetValues(dataType).GetValue(0);
                    newData = (T)(object)EditorGUI.EnumPopup(rect, _dataFieldLabel, enumValue);
                }
                else
                {
                    // 其他类型显示为标签
                    EditorGUI.LabelField(rect, _dataFieldLabel, data?.ToString() ?? "null");
                    return; // 不更新数据
                }

                // if (newData != data)
                {
                    _dataList[index] = newData;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Error drawing field for type {dataType}: {ex.Message}");
                EditorGUI.LabelField(rect, _dataFieldLabel, "Error");
            }
        }        
        
        public string SelectFolder()
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
                    Debug.LogWarning("不能在Assets目录之外!");
                }
            }

            return null;
        }        
    }
//
//     /// <summary>
//     /// 带路径选择功能的ReorderableList包装器
//     /// </summary>
//     /// <typeparam name="T">必须实现IPathFilter接口的类型</typeparam>
//     public class PathFilterReorderableList<T> : ReorderableList<T> where T : class, IPathFilter, new()
//     {
//         private EditorWindow _parentWindow;
//
//         public PathFilterReorderableList(System.Collections.Generic.List<T> dataList, string headerText,
//             EditorWindow parentWindow = null)
//             : base(dataList, headerText)
//         {
//             _parentWindow = parentWindow;
//
//             // 设置默认的绘制和添加回调
//             OnDrawElement = DrawPathFilterElement;
//             OnAddElement = AddPathFilterElement;
//         }
//
//         private void DrawPathFilterElement(Rect rect, int index, T filter)
//         {
//             const float GAP = 5;
//             rect.y++;
//
//             Rect r = rect;
//
//             // 启用复选框
//             r.width = 60;
//             r.height = 18;
//             filter.Valid = GUI.Toggle(r, filter.Valid, new GUIContent("启用"));
//
//             // 路径文本框
//             r.xMin = r.xMax + GAP;
//             r.xMax = rect.xMax - 300;
//             GUI.enabled = false;
//             filter.Path = GUI.TextField(r, filter.Path ?? "");
//             GUI.enabled = true;
//
//             // 选择按钮
//             r.xMin = r.xMax + GAP;
//             r.width = 50;
//             if (GUI.Button(r, new GUIContent("Select", "选择目录")))
//             {
//                 string selectedPath = SelectFolder();
//                 if (!string.IsNullOrEmpty(selectedPath))
//                 {
//                     filter.Path = selectedPath;
//                 }
//             }
//
//             // 过滤器文本框
//             r.xMin = r.xMax + GAP;
//             r.xMax = rect.xMax;
//             filter.Filter = EditorGUI.TextField(r, filter.Filter ?? "");
//         }
//
//         private T AddPathFilterElement()
//         {
//             T filter = new T();
//             string path = SelectFolder();
//             if (!string.IsNullOrEmpty(path))
//             {
//                 
//                 filter.Path = path;
//                 filter.Filter = "*.prefab"; // 默认过滤器
//                 filter.Valid = true;
//                 
//             }
//             return filter;
//         }
//
//
//     }
    
}