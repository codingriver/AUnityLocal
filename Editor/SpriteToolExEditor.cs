using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class SpriteToolExEditor : EditorWindow
{
    private Sprite originalSprite;
    private Sprite replacementSprite;
    private string searchPath = "Assets/C";
    private bool dryRun = true;
    private bool useNewSpriteSize = false;
    private Vector2 scrollPosition;
    private List<string> processedPrefabs = new List<string>();
    private List<string> modifiedPrefabs = new List<string>();
    private string resultText = "";
    private string logFilePath = "";

    [MenuItem("AUnityLocalEditor/Sprite替换工具")]
    public static void ShowWindow()
    {
        GetWindow<SpriteToolExEditor>("Sprite替换工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite批量替换工具\n 替换Sprite,或者查询Sprite引用的prefab", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        originalSprite = (Sprite)EditorGUILayout.ObjectField("原始Sprite (A)", originalSprite, typeof(Sprite), false);
        replacementSprite = (Sprite)EditorGUILayout.ObjectField("替换Sprite (B)", replacementSprite, typeof(Sprite), false);
        
        searchPath = EditorGUILayout.TextField("搜索路径", searchPath);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("选择路径"))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择Prefab目录", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    searchPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "请选择项目内的Assets目录下的文件夹", "确定");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        dryRun = EditorGUILayout.Toggle("仅预览（不保存修改）", dryRun);
        useNewSpriteSize = EditorGUILayout.Toggle("使用新Sprite的尺寸", useNewSpriteSize);

        EditorGUILayout.Space();
        
        if (GUILayout.Button("开始替换", GUILayout.Height(30)))
        {
            if (originalSprite == null || replacementSprite == null)
            {
                EditorUtility.DisplayDialog("错误", "请指定原始Sprite和替换Sprite", "确定");
                return;
            }

            if (!Directory.Exists(searchPath))
            {
                EditorUtility.DisplayDialog("错误", "指定的搜索路径不存在: " + searchPath, "确定");
                return;
            }

            ReplaceSpritesInPrefabs();
        }

        EditorGUILayout.Space();
        GUILayout.Label("处理结果", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("复制处理结果"))
        {
            CopyResultToClipboard();
        }
        
        if (!string.IsNullOrEmpty(logFilePath) && GUILayout.Button("打开日志文件"))
        {
            System.Diagnostics.Process.Start(logFilePath);
        }
        EditorGUILayout.EndHorizontal();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField($"处理的Prefab数量: {processedPrefabs.Count}");
        EditorGUILayout.LabelField($"修改的Prefab数量: {modifiedPrefabs.Count}");
        
        if (!string.IsNullOrEmpty(logFilePath))
        {
            EditorGUILayout.LabelField($"日志文件: {Path.GetFileName(logFilePath)}");
        }
        
        EditorGUILayout.Space();
        
        if (processedPrefabs.Count > 0)
        {
            GUILayout.Label("处理的Prefabs:", EditorStyles.miniBoldLabel);
            foreach (string prefabPath in processedPrefabs)
            {
                bool modified = modifiedPrefabs.Contains(prefabPath);
                EditorGUILayout.LabelField($"{"● " + (modified ? "<color=green>已修改</color>" : "<color=grey>未修改</color>") + " " + prefabPath}", 
                    modified ? EditorStyles.boldLabel : EditorStyles.miniLabel);
            }
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void ReplaceSpritesInPrefabs()
    {
        processedPrefabs.Clear();
        modifiedPrefabs.Clear();
        StringBuilder logBuilder = new StringBuilder();
        
        logBuilder.AppendLine("===== Sprite替换工具执行日志 =====");
        logBuilder.AppendLine($"开始时间: {System.DateTime.Now}");
        logBuilder.AppendLine($"原始Sprite: {GetSpriteInfo(originalSprite)}");
        logBuilder.AppendLine($"替换Sprite: {GetSpriteInfo(replacementSprite)}");
        logBuilder.AppendLine($"搜索路径: {searchPath}");
        logBuilder.AppendLine($"仅预览模式: {dryRun}");
        logBuilder.AppendLine($"使用新Sprite尺寸: {useNewSpriteSize}");
        logBuilder.AppendLine("----------------------------------");
        
        string[] prefabPaths = Directory.GetFiles(searchPath, "*.prefab", SearchOption.AllDirectories);
        
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string prefabPath in prefabPaths)
            {
                ProcessPrefab(prefabPath, logBuilder);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        
        // 构建结果文本
        resultText = BuildResultText();
        
        logBuilder.AppendLine("----------------------------------");
        logBuilder.AppendLine($"处理完成!");
        logBuilder.AppendLine($"处理的Prefab数量: {processedPrefabs.Count}");
        logBuilder.AppendLine($"修改的Prefab数量: {modifiedPrefabs.Count}");
        logBuilder.AppendLine($"结束时间: {System.DateTime.Now}");
        logBuilder.AppendLine("===== 处理完成 =====");
        
        // 保存日志到文件
        SaveLogToFile(logBuilder.ToString());
        
        // 合并输出一条日志
        Debug.Log(logBuilder.ToString());
        
        EditorUtility.DisplayDialog("完成", 
            $"处理完成!\n处理的Prefab数量: {processedPrefabs.Count}\n修改的Prefab数量: {modifiedPrefabs.Count}\n日志已保存到: {logFilePath}", 
            "确定");
    }

    private void ProcessPrefab(string prefabPath, StringBuilder logBuilder)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            logBuilder.AppendLine($"警告: 无法加载Prefab: {prefabPath}");
            return;
        }

        processedPrefabs.Add(prefabPath);
        bool prefabModified = false;
        int spriteRendererCount = 0;
        int imageCount = 0;

        logBuilder.AppendLine($"处理Prefab: {prefabPath}");

        // 创建Prefab实例用于检查和修改
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            logBuilder.AppendLine($"警告: 无法实例化Prefab: {prefabPath}");
            return;
        }

        try
        {
            // 获取所有SpriteRenderer组件
            SpriteRenderer[] spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                if (renderer.sprite == originalSprite)
                {
                    Vector3 originalScale = renderer.transform.localScale;
                    renderer.sprite = replacementSprite;
                    spriteRendererCount++;
                    
                    // 如果启用了使用新尺寸选项，调整SpriteRenderer的大小
                    if (useNewSpriteSize && replacementSprite != null)
                    {
                        // 计算原始和替换Sprite的大小比例
                        Vector2 originalSize = originalSprite.rect.size;
                        Vector2 newSize = replacementSprite.rect.size;
                        Vector2 sizeRatio = new Vector2(newSize.x / originalSize.x, newSize.y / originalSize.y);
                        
                        // 应用比例到localScale
                        Vector3 currentScale = renderer.transform.localScale;
                        renderer.transform.localScale = new Vector3(
                            currentScale.x * sizeRatio.x,
                            currentScale.y * sizeRatio.y,
                            currentScale.z
                        );
                        
                        logBuilder.AppendLine($"  - 修改SpriteRenderer: {renderer.name}, 调整尺寸: {originalScale} -> {renderer.transform.localScale}");
                    }
                    else
                    {
                        logBuilder.AppendLine($"  - 修改SpriteRenderer: {renderer.name}, 保持原尺寸");
                    }
                    
                    prefabModified = true;
                }
            }

            // 获取所有Image组件（UI）
            UnityEngine.UI.Image[] images = instance.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (UnityEngine.UI.Image image in images)
            {
                if (image.sprite == originalSprite)
                {
                    Vector2 originalSizeDelta = image.rectTransform.sizeDelta;
                    image.sprite = replacementSprite;
                    imageCount++;
                    
                    // 如果启用了使用新尺寸选项，调整UI Image的大小
                    if (useNewSpriteSize && replacementSprite != null)
                    {
                        Vector2 newSizeDelta = replacementSprite.rect.size;
                        image.SetNativeSize();
                        
                        logBuilder.AppendLine($"  - 修改UI Image: {image.name}, 调整尺寸: {originalSizeDelta} -> {image.rectTransform.sizeDelta}");
                    }
                    else
                    {
                        logBuilder.AppendLine($"  - 修改UI Image: {image.name}, 保持原尺寸");
                    }
                    
                    prefabModified = true;
                }
            }

            // 如果有修改，则应用到Prefab
            if (prefabModified)
            {
                if (!dryRun)
                {
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.UserAction);
                    modifiedPrefabs.Add(prefabPath);
                    logBuilder.AppendLine($"  ✔ 已保存修改");
                }
                else
                {
                    // 仅预览模式下不保存修改
                    modifiedPrefabs.Add(prefabPath + " (预览模式下不保存)");
                    logBuilder.AppendLine($"  ⚠ 预览模式: 未保存修改");
                }
                
                logBuilder.AppendLine($"  共修改: {spriteRendererCount}个SpriteRenderer, {imageCount}个UI Image");
            }
            else
            {
                logBuilder.AppendLine($"  ❌ 未找到需要替换的Sprite");
            }
        }
        finally
        {
            // 清理实例
            DestroyImmediate(instance);
        }
    }

    private string GetSpriteInfo(Sprite sprite)
    {
        if (sprite == null) return "null";
        return $"{sprite.name} ({sprite.rect.width}x{sprite.rect.height}px)";
    }

    private string BuildResultText()
    {
        StringBuilder resultBuilder = new StringBuilder();
        resultBuilder.AppendLine("===== Sprite替换工具处理结果 =====");
        resultBuilder.AppendLine($"处理的Prefab数量: {processedPrefabs.Count}");
        resultBuilder.AppendLine($"修改的Prefab数量: {modifiedPrefabs.Count}");
        resultBuilder.AppendLine("----------------------------------");
        resultBuilder.AppendLine("修改的Prefabs:");
        
        foreach (string prefabPath in modifiedPrefabs)
        {
            resultBuilder.AppendLine(prefabPath);
        }
        
        return resultBuilder.ToString();
    }

    private void CopyResultToClipboard()
    {
        if (string.IsNullOrEmpty(resultText))
        {
            EditorUtility.DisplayDialog("提示", "没有处理结果可复制", "确定");
            return;
        }
        
        TextEditor te = new TextEditor();
        te.text = resultText;
        te.SelectAll();
        te.Copy();
        
        EditorUtility.DisplayDialog("成功", "处理结果已复制到剪贴板", "确定");
    }

    private void SaveLogToFile(string logContent)
    {
        try
        {
            // 创建日志文件夹（如果不存在）
            string logFolder = Path.Combine(Application.dataPath, "..", "SpriteReplacementLogs");
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }
            
            // 生成带时间戳的日志文件名
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            logFilePath = Path.Combine(logFolder, $"SpriteReplacement_{timestamp}.log");
            
            // 写入日志内容
            File.WriteAllText(logFilePath, logContent);
            
            // 在控制台输出日志位置
            Debug.Log($"日志已保存到: {logFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存日志文件失败: {e.Message}");
            logFilePath = "";
        }
    }
}    