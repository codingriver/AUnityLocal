using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EditorExtensions;

public class ObjectFilterEditorWindow : EditorWindow
{
    // 不同类型的过滤器列表
    public List<StringFilter> stringFilters = new List<StringFilter>();
    public List<GameObjectFilter> gameObjectFilters = new List<GameObjectFilter>();
    public List<IntFilter> intFilters = new List<IntFilter>();
    public List<CustomDataFilter> customDataFilters = new List<CustomDataFilter>();
    public List<EnumFilter> enumFilters = new List<EnumFilter>();
    
    // ReorderableList实例
    private ObjectFilterReorderableList<StringFilter, string> _stringFilterList;
    private ObjectFilterReorderableList<GameObjectFilter, GameObject> _gameObjectFilterList;
    private ObjectFilterReorderableList<IntFilter, int> _intFilterList;
    private ObjectFilterReorderableList<CustomDataFilter, CustomData> _customDataFilterList;
    private ObjectFilterReorderableList<EnumFilter, FilterType> _enumFilterList;
    
    [MenuItem("AUnityLocal/ObjectFilterEditorWindow",false,500)]
    public static void ShowWindow()
    {
        GetWindow<ObjectFilterEditorWindow>("Object Filter Editor");
    }
    
    void OnEnable()
    {
        InitializeReorderableLists();
    }
    
    void InitializeReorderableLists()
    {
        // 字符串过滤器
        _stringFilterList = new ObjectFilterReorderableList<StringFilter, string>(
            stringFilters, 
            "String Filters", 
            "Text",
            this
        );
        
        // GameObject过滤器
        _gameObjectFilterList = new ObjectFilterReorderableList<GameObjectFilter, GameObject>(
            gameObjectFilters, 
            "GameObject Filters", 
            "GameObject",
            this
        );
        
        // 整数过滤器
        _intFilterList = new ObjectFilterReorderableList<IntFilter, int>(
            intFilters, 
            "Integer Filters", 
            "Value",
            this
        );
        
        // 自定义数据过滤器 - 使用自定义绘制
        _customDataFilterList = new ObjectFilterReorderableList<CustomDataFilter, CustomData>(
            customDataFilters, 
            "Custom Data Filters", 
            "Custom Data",
            this
        );
        _customDataFilterList.ElementHeight = 44f; // 更高以容纳多行
        _customDataFilterList.OnDrawDataField = DrawCustomDataField;
        
        // 枚举过滤器
        _enumFilterList = new ObjectFilterReorderableList<EnumFilter, FilterType>(
            enumFilters, 
            "Enum Filters", 
            "Filter Type",
            this
        );
    }
    
    // 自定义绘制CustomData
    private void DrawCustomDataField(Rect rect, CustomData data, System.Action<CustomData> onDataChanged)
    {
        if (data == null)
        {
            data = new CustomData();
            onDataChanged(data);
        }
        
        const float LINE_HEIGHT = 18f;
        const float GAP = 2f;
        
        Rect nameRect = new Rect(rect.x, rect.y, rect.width, LINE_HEIGHT);
        Rect valueRect = new Rect(rect.x, rect.y + LINE_HEIGHT + GAP, rect.width * 0.5f - GAP, LINE_HEIGHT);
        Rect enabledRect = new Rect(rect.x + rect.width * 0.5f, rect.y + LINE_HEIGHT + GAP, rect.width * 0.5f, LINE_HEIGHT);
        
        data.name = EditorGUI.TextField(nameRect, "Name", data.name ?? "");
        data.value = EditorGUI.IntField(valueRect, "Value", data.value);
        data.enabled = EditorGUI.Toggle(enabledRect, "Enabled", data.enabled);
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Object Filter Lists", EditorStyles.boldLabel);
        
        // 绘制各种过滤器列表
        _stringFilterList?.DoLayoutList();
        
        GUILayout.Space(10);
        _gameObjectFilterList?.DoLayoutList();
        
        GUILayout.Space(10);
        _intFilterList?.DoLayoutList();
        
        GUILayout.Space(10);
        _customDataFilterList?.DoLayoutList();
        
        GUILayout.Space(10);
        _enumFilterList?.DoLayoutList();
        
        // 显示统计信息
        GUILayout.Space(20);
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"String Filters: {stringFilters.Count}");
        EditorGUILayout.LabelField($"GameObject Filters: {gameObjectFilters.Count}");
        EditorGUILayout.LabelField($"Integer Filters: {intFilters.Count}");
        EditorGUILayout.LabelField($"Custom Data Filters: {customDataFilters.Count}");
        EditorGUILayout.LabelField($"Enum Filters: {enumFilters.Count}");
    }
}
