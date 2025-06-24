using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;

public class HierarchySearchTool : EditorWindow
{
    private string nameSearchText = "";
    private LayerMask layerMaskShow = -1;
    private LayerMask layerMask = -1;
    private List<GameObject> nameSearchResults = new List<GameObject>();
    private List<GameObject> layerSearchResults = new List<GameObject>();
    private Vector2 resultsScrollPosition;
    private Dictionary<int, int> layerDictionary = new Dictionary<int, int>();
    private GUIStyle sectionHeaderStyle;
    private GUIStyle searchButtonStyle;
    private GUIStyle resultCountStyle;
    private Color sectionBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.1f);

    [MenuItem("AUnityLocal/Hierarchy Search Tool")]
    public static void ShowWindow()
    {
        GetWindow<HierarchySearchTool>("Hierarchy Search");
    }

    private void OnEnable()
    {
        InitializeStyles();
        
        for (int i = 0; i < InternalEditorUtility.layers.Length; i++)
        {
            int layer = LayerMask.NameToLayer(InternalEditorUtility.layers[i]);
            layerDictionary[i] = layer;
        }
    }

    private void InitializeStyles()
    {
        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            margin = new RectOffset(0, 0, 10, 5),
            padding = new RectOffset(5, 5, 2, 2)
        };

        searchButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            fixedHeight = 25
        };

        resultCountStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            fontSize = 12,
            normal =
            {
                textColor = EditorGUIUtility.isProSkin ? 
                    new Color(0.7f, 0.9f, 1f) : 
                    new Color(0.1f, 0.3f, 0.5f)
            }
        };
    }

    private void OnGUI()
    {
        DrawTitle();
        EditorGUILayout.BeginHorizontal();
        
        DrawLeftPanel();
        DrawRightPanel();
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTitle()
    {
        GUILayout.Label("Hierarchy Search Tool", new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            margin = new RectOffset(0, 0, 5, 10)
        });
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.3f));
        
        DrawNameSearchSection();
        EditorGUILayout.Space();
        DrawLayerSearchSection();
        EditorGUILayout.Space();
        DrawClearButton();
        
        EditorGUILayout.EndVertical();
    }

    private void DrawNameSearchSection()
    {
        DrawSectionHeader("Name Search");
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        nameSearchText = EditorGUILayout.TextField("Search Text", nameSearchText);
        
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Search", searchButtonStyle))
        {
            SearchByName();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawLayerSearchSection()
    {
        DrawSectionHeader("Layer Search");
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        layerMaskShow = EditorGUILayout.MaskField("Layers", layerMaskShow, InternalEditorUtility.layers);
        
        string selectedLayers = GetSelectedLayers(layerMaskShow);
        EditorGUILayout.LabelField("Selected Layers", selectedLayers);
        
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Search Layers", searchButtonStyle))
        {
            SearchByLayer();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawClearButton()
    {
        if (GUILayout.Button("Clear Results", new GUIStyle(searchButtonStyle)
        {
            normal = { textColor = Color.red }
        }))
        {
            ClearResults();
        }
    }

    private void DrawSectionHeader(string title)
    {
        Rect rect = GUILayoutUtility.GetRect(1, 25);
        EditorGUI.DrawRect(rect, sectionBackgroundColor);
        EditorGUI.LabelField(rect, title, sectionHeaderStyle);
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical();
        
        DrawResultsSection();
        
        EditorGUILayout.EndVertical();
    }

    private void DrawResultsSection()
    {
        DrawSectionHeader("Search Results");
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 计算总结果数
        int totalResults = nameSearchResults.Count + layerSearchResults.Count;
        GUILayout.Label($"Total Results: {totalResults}", resultCountStyle);
        
        resultsScrollPosition = EditorGUILayout.BeginScrollView(resultsScrollPosition, GUILayout.ExpandHeight(true));
        
        // 显示名称搜索结果
        if (nameSearchResults.Count > 0)
        {
            DrawResultsGroup("Name Search", nameSearchResults);
        }
        
        // 显示层搜索结果
        if (layerSearchResults.Count > 0)
        {
            if (nameSearchResults.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space();
            }
            DrawResultsGroup("Layer Search", layerSearchResults);
        }
        
        // 无结果提示
        if (totalResults == 0)
        {
            EditorGUILayout.LabelField("No results to display.", EditorStyles.centeredGreyMiniLabel);
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawResultsGroup(string groupName, List<GameObject> results)
    {
        EditorGUILayout.LabelField($"<b>{groupName} Results</b>", new GUIStyle(EditorStyles.label)
        {
            richText = true,
            fontStyle = FontStyle.Bold
        });
        
        foreach (var go in results)
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
    }

    private string GetSelectedLayers(int mask)
    {
        string layers = "";
        layerMask = 0;
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                if (layerDictionary.TryGetValue(i, out int layerIndex))
                {
                    layers += LayerMask.LayerToName(layerIndex) + ", ";
                    layerMask |= (1 << layerIndex);
                }
            }
        }

        if (mask == -1)
        {
            layerMask = -1;
        }
        
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

    private void ClearResults()
    {
        nameSearchResults.Clear();
        layerSearchResults.Clear();
        Repaint();
    }
}