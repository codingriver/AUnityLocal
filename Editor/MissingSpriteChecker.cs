using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class MissingSpriteChecker : EditorWindow
{
    private string searchDirectory = "Assets/";
    private bool isChecking = false;
    private int totalPrefabs = 0;
    private int checkedPrefabs = 0;
    private int missingCount = 0;
    private List<string> prefabPaths = new List<string>();

    [MenuItem("AUnityLocal/Check Missing Sprites in Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<MissingSpriteChecker>("Missing Sprite Checker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Missing Sprite Checker", EditorStyles.boldLabel);
        searchDirectory = EditorGUILayout.TextField("Search Directory:", searchDirectory);

        EditorGUI.BeginDisabledGroup(isChecking);
        if (GUILayout.Button("Check Missing Sprites"))
        {
            StartChecking();
        }
        EditorGUI.EndDisabledGroup();

        if (isChecking)
        {
            EditorGUILayout.LabelField($"Progress: {checkedPrefabs}/{totalPrefabs}");
            EditorGUILayout.LabelField($"Missing Sprites Found: {missingCount}");
            if (GUILayout.Button("Cancel"))
            {
                isChecking = false;
            }
        }
    }

    private void StartChecking()
    {
        prefabPaths = Directory.GetFiles(searchDirectory, "*.prefab", SearchOption.AllDirectories).ToList();
        totalPrefabs = prefabPaths.Count;
        checkedPrefabs = 0;
        missingCount = 0;
        isChecking = true;

        // 开始延迟调用链
        EditorApplication.delayCall += ProcessNextPrefab;
    }

    private void ProcessNextPrefab()
    {
        if (!isChecking || checkedPrefabs >= totalPrefabs)
        {
            FinishChecking();
            return;
        }

        string prefabPath = prefabPaths[checkedPrefabs];
        checkedPrefabs++;

        // 使用延迟调用处理当前Prefab
        EditorApplication.delayCall += () => {
            CheckSinglePrefab(prefabPath);
            // 处理完成后立即开始下一个
            EditorApplication.delayCall += ProcessNextPrefab;
        };

        Repaint();
    }

    private void CheckSinglePrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        // 检查SpriteRenderer组件
        var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var renderer in renderers)
        {
            CheckForMissingSprite(renderer.gameObject, renderer.sprite, 
                () => new SerializedObject(renderer).FindProperty("m_Sprite"),
                prefabPath);
        }

        // 检查Image组件
        var images = prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var image in images)
        {
            CheckForMissingSprite(image.gameObject, image.sprite, 
                () => new SerializedObject(image).FindProperty("m_Sprite"),
                prefabPath);
        }
    }

    private void CheckForMissingSprite(GameObject obj, Sprite sprite, 
        System.Func<SerializedProperty> getSpriteProperty, string prefabPath)
    {
        if (sprite == null)
        {
            SerializedProperty sp = getSpriteProperty();
            if (sp != null && sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
            {
                missingCount++;
                Debug.LogError($"Missing Sprite found in: {prefabPath} on GameObject: {GetGameObjectPath(obj)}", 
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            }
        }
    }

    private void FinishChecking()
    {
        isChecking = false;
        EditorUtility.DisplayDialog("Check Complete", 
            $"Checked {checkedPrefabs} prefabs. Found {missingCount} missing sprites.", "OK");
        Repaint();
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }
}
