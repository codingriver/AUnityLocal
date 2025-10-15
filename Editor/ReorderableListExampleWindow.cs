using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AUnityLocal.Editor;

namespace AUnityLocal.Editor
{
    public class ReorderableListExampleWindow : EditorWindow
    {
        
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
    public class PathData
    {
        public string path;
        public string filter;
        public bool valid;

        public override string ToString()
        {
            return $"{path} ({filter}) - {(valid ? "Valid" : "Invalid")}";
        }
    }    
// 枚举过滤器
    public enum FilterType
    {
        Include,
        Exclude,
        Transform
    }    
        // 不同类型的过滤器列表
        public List<string> stringFilters = new List<string>();
        public List<GameObject> gameObjectFilters = new List<GameObject>();
        public List<Transform> tFilters = new List<Transform>();
        public List<int> intFilters = new List<int>();
        public List<CustomData> customDataFilters = new List<CustomData>();
        public List<FilterType> enumFilters = new List<FilterType>();
        private List<Transform> _transformList=new List<Transform>();
        private ReorderableList<Transform> _transformFilterList;
        private List<bool> _boolFilters = new List<bool>();
        // ReorderableList实例
        private ReorderableList< string> _stringFilterList;
        private ReorderableList< GameObject> _gameObjectFilterList;
        private ReorderableList<int> _intFilterList;
        private ReorderableList<CustomData> _customDataFilterList;
        private ReorderableList<FilterType> _enumFilterList;
        private ReorderableList<bool> _boolFilterList;
        
        private ReorderableList<PathData> _pathDataFilterList;
        private List<PathData>_pathDataList=new List<PathData>();

        [SerializeField]
        // public List<PathFilter> pathFilters = new List<PathFilter>();
        // private PathFilterReorderableList _pathFilterList;
        private bool _enablePaste = true;

        private bool _allowFolders = true;
        private bool _allowFiles = true;


        [MenuItem("AUnityLocal/ObjectFilterEditorWindow", false, 500)]
        public static void ShowWindow()
        {
            GetWindow<ReorderableListExampleWindow>("Object Filter Editor");
        }

        void OnEnable()
        {
            InitializeReorderableLists();
        }

        void InitializeReorderableLists()
        {
            // 字符串过滤器
            _stringFilterList = new ReorderableList<string>(
                stringFilters,
                "String Filters"
            );

            // GameObject过滤器
            _gameObjectFilterList = new ReorderableList<GameObject>(
                gameObjectFilters,
                "GameObject Filters"
            );

            // 整数过滤器
            _intFilterList = new ReorderableList<int>(intFilters,
                "Integer Filters"
            );

            // 自定义数据过滤器 - 使用自定义绘制
            _customDataFilterList = new ReorderableList<CustomData>(
                customDataFilters,
                "Custom Data Filters"
            );
            _customDataFilterList.ElementHeight = 44f; // 更高以容纳多行
            _customDataFilterList.OnDrawElement = DrawCustomDataField;

            // 枚举过滤器
            _enumFilterList = new ReorderableList<FilterType>(
                enumFilters,
                "Enum Filters"
            );
            _transformFilterList = new ReorderableList<Transform>(_transformList,"Transform List");
            // 布尔过滤器
            _boolFilterList = new ReorderableList<bool>(
                _boolFilters,
                "Boolean Filters"
            );
            _pathDataFilterList = new ReorderableList<PathData>(
                _pathDataList,
                "Path Data Filters",_onAddElement:()=>
                {
                    return new PathData{path="",filter="*.prefab",valid=true};
                },_onDrawElement:OnDrawPathDataElement
            );
            
            // 如果列表为空，添加一些示例数据
            // if (pathFilters.Count == 0)
            // {
            //     pathFilters.Add(new PathFilter("Assets", true));
            //     pathFilters.Add(new PathFilter("Assets/Scripts", true));
            //     pathFilters.Add(new PathFilter("Assets/Textures", false));
            // }
            //
            // _pathFilterList = new PathFilterReorderableList(
            //     pathFilters, 
            //     "Path Filters", 
            //     "Path",
            //     _allowFolders,
            //     _allowFiles,
            //     this);        
        }

        // 自定义绘制CustomData
        private void DrawCustomDataField(Rect rect, int index, CustomData data)
        {
            const float LINE_HEIGHT = 18f;
            const float GAP = 2f;

            Rect nameRect = new Rect(rect.x, rect.y, rect.width, LINE_HEIGHT);
            Rect valueRect = new Rect(rect.x, rect.y + LINE_HEIGHT + GAP, rect.width * 0.5f - GAP, LINE_HEIGHT);
            Rect enabledRect = new Rect(rect.x + rect.width * 0.5f, rect.y + LINE_HEIGHT + GAP, rect.width * 0.5f,
                LINE_HEIGHT);

            data.name = EditorGUI.TextField(nameRect, "Name", data.name ?? "");
            data.value = EditorGUI.IntField(valueRect, "Value", data.value);
            data.enabled = EditorGUI.Toggle(enabledRect, "Enabled", data.enabled);
        }

        private void OnDrawPathDataElement(Rect rect, int index, PathData filter)
         {
             const float LINE_HEIGHT = 18f;
             const float GAP = 5;
             rect.y++;

             Rect r = rect;

             // 启用复选框
             r.width = 60;
             r.height = LINE_HEIGHT;
             filter.valid = GUI.Toggle(r, filter.valid, new GUIContent("启用"));

             // 路径文本框
             r.xMin = r.xMax + GAP;
             r.xMax = rect.xMax - 300;
             GUI.enabled = false;
             filter.path = GUI.TextField(r, filter.path ?? "");
             GUI.enabled = true;

             // 选择按钮
             r.xMin = r.xMax + GAP;
             r.width = 50;
             if (GUI.Button(r, new GUIContent("Select", "选择目录")))
             {
                 string selectedPath = Tools.SelectFolder();
                 if (!string.IsNullOrEmpty(selectedPath))
                 {
                     filter.path = selectedPath;
                 }
             }

             // 过滤器文本框
             r.xMin = r.xMax + GAP;
             r.xMax = rect.xMax;
             filter.filter = EditorGUI.TextField(r, filter.filter ?? "");
         }
        
        
        Vector2 scrollPos;
        void OnGUI()
        {
            EditorGUILayout.LabelField("Object Filter Lists", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("选中Project中的资源，然后点击P按钮粘贴", MessageType.Info);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            // 绘制各种过滤器列表
            _stringFilterList?.DoLayoutList();

            
            _gameObjectFilterList?.DoLayoutList();
            GUILayout.Space(10);
            _intFilterList?.DoLayoutList();

            GUILayout.Space(10);
            _customDataFilterList?.DoLayoutList();

            GUILayout.Space(10);
            _enumFilterList?.DoLayoutList();
            GUILayout.Space(10);
            _transformFilterList?.DoLayoutList();
            GUILayout.Space(10);
            _boolFilterList?.DoLayoutList();
            
            _pathDataFilterList.DoLayoutList();
            // 显示统计信息
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"String Filters: {stringFilters.Count}");
            EditorGUILayout.LabelField($"GameObject Filters: {gameObjectFilters.Count}");
            EditorGUILayout.LabelField($"Integer Filters: {intFilters.Count}");
            EditorGUILayout.LabelField($"Custom Data Filters: {customDataFilters.Count}");
            EditorGUILayout.LabelField($"Enum Filters: {enumFilters.Count}");
            GUILayout.EndScrollView();
        }
    }
}