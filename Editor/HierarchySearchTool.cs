using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;

public class HierarchySearchTool : EditorWindow
{
    private string nameSearchText = "";
    private LayerMask layerMaskShow = -1; // 默认选择所有层
    private LayerMask layerMask = -1; // 默认选择所有层
    private List<GameObject> nameSearchResults = new List<GameObject>();
    private List<GameObject> layerSearchResults = new List<GameObject>();
    private Vector2 nameScrollPosition;
    private Vector2 layerScrollPosition;
    private Dictionary<int,int> layerDictionary = new Dictionary<int, int>();
    [MenuItem("AUnityLocal/Hierarchy Search Tool")]
    public static void ShowWindow()
    {
        GetWindow<HierarchySearchTool>("Hierarchy Search");
    }

    private void OnEnable()
    {
        
        for (int i = 0; i < InternalEditorUtility.layers.Length; i++)
        {
            int layer= LayerMask.NameToLayer(InternalEditorUtility.layers[i]);
            layerDictionary[i] = layer;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Hierarchy Search Tool", EditorStyles.boldLabel);

        // 名称搜索区域
        GUILayout.Label("Name Search", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        nameSearchText = EditorGUILayout.TextField("Search Text", nameSearchText);
        if (GUILayout.Button("Search", GUILayout.Width(100)))
        {
            SearchByName();
        }
        EditorGUILayout.EndHorizontal();

        // 显示名称搜索结果
        GUILayout.Label($"Results: {nameSearchResults.Count}", EditorStyles.miniBoldLabel);
        nameScrollPosition = EditorGUILayout.BeginScrollView(nameScrollPosition, GUILayout.MaxHeight(150));
        foreach (var go in nameSearchResults)
        {
            if (go != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(go, typeof(GameObject), true);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(20);

        // 层搜索区域
        GUILayout.Label("Layer Search", EditorStyles.boldLabel);
        layerMaskShow = EditorGUILayout.MaskField("Layers", layerMaskShow, InternalEditorUtility.layers);
        // 显示当前选择的层
        // 获取选中的层名称
        string selectedLayers = GetSelectedLayers(layerMaskShow);
        EditorGUILayout.LabelField("Selected Layers", selectedLayers);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Search Layers", GUILayout.Width(100)))
        {
            SearchByLayer();
        }
        EditorGUILayout.EndHorizontal();

        // 显示层搜索结果
        GUILayout.Label($"Results: {layerSearchResults.Count}", EditorStyles.miniBoldLabel);
        layerScrollPosition = EditorGUILayout.BeginScrollView(layerScrollPosition, GUILayout.MaxHeight(150));
        foreach (var go in layerSearchResults)
        {
            if (go != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(go, typeof(GameObject), true);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }
    private string GetSelectedLayers(int mask)
    {
        string layers = "";
        layerMask = 0;
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0) // 检查位是否被设置
            {
                if (layerDictionary.TryGetValue(i, out int layerIndex))
                {
                    layers += LayerMask.LayerToName(layerIndex) + ", ";
                    layerMask|= (1 << layerIndex); // 更新layerMask
                }
                
            }
        }

        if (mask == -1)
        {
            layerMask = -1;
        }
        // 去掉最后的逗号和空格
        return string.IsNullOrEmpty(layers) ? "None" : layers.TrimEnd(',', ' ');
    }

    private void SearchByName()
    {
        nameSearchResults.Clear();
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (var go in allObjects)
        {
            if (go.name.IndexOf(nameSearchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                nameSearchResults.Add(go);
            }
        }
        
        Repaint();
    }

    private void SearchByLayer()
    {
        layerSearchResults.Clear();
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (var go in allObjects)
        {
            if ((layerMask.value & (1 << go.layer)) != 0)
            {
                layerSearchResults.Add(go);
            }
        }
        
        Repaint();
    }
}    