using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace AUnityLocal.Editor
{
    public class SpriteToolExEditor : EditorWindow
{
    // 原有字段保持不变...
    private Sprite originalSprite;
    private Sprite replacementSprite;
    private string searchPath = "Assets";
    private bool dryRun = true;
    private bool useNewSpriteSize = false;
    private bool includeInactiveObjects = true;
    private Vector2 scrollPosition;
    private List<string> processedPrefabs = new List<string>();
    private List<string> modifiedPrefabs = new List<string>();
    private string resultText = "";
    private string logFilePath = "";
    private Vector2Int progressBarSize = new Vector2Int(400, 20);
    private float progress = 0f;
    private string progressMessage = "";
    private bool isProcessing = false;
    private bool isFindingReferences = false;

    [MenuItem("AUnityLocal/Sprite替换工具")]
    public static void ShowWindow()
    {
        var window = GetWindow<SpriteToolExEditor>("Sprite替换工具");
        window.Init();
    }

    private void Init()
    {
        searchPath = PlayerPrefs.GetString("SpriteToolExSearchPath", searchPath);
    }

    private void OnGUI()
    {
        // GUI界面保持不变...
        GUILayout.Label("Sprite批量替换工具\n 1.替换 prefab上的Sprite \n2.查询Sprite引用的prefab", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        originalSprite = (Sprite)EditorGUILayout.ObjectField("原始Sprite (A)", originalSprite, typeof(Sprite), false);
        replacementSprite = (Sprite)EditorGUILayout.ObjectField("替换Sprite (B)", replacementSprite, typeof(Sprite), false);
        
        var searchPath1 = EditorGUILayout.TextField("搜索路径", searchPath);
        if (searchPath1 != searchPath)
        {
            searchPath = searchPath1;
            PlayerPrefs.SetString("SpriteToolExSearchPath", searchPath);
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("选择路径"))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择Prefab目录", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    searchPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    PlayerPrefs.SetString("SpriteToolExSearchPath", searchPath);
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
        includeInactiveObjects = EditorGUILayout.Toggle("包含非激活对象", includeInactiveObjects);

        EditorGUILayout.Space();
        
        EditorGUI.BeginDisabledGroup(isProcessing || isFindingReferences);
        
        if (GUILayout.Button("查找引用", GUILayout.Height(30)))
        {
            if (originalSprite == null)
            {
                EditorUtility.DisplayDialog("错误", "请指定原始Sprite", "确定");
                return;
            }

            if (!Directory.Exists(Path.Combine(Application.dataPath, searchPath.Substring(7))))
            {
                EditorUtility.DisplayDialog("错误", "指定的搜索路径不存在: " + searchPath, "确定");
                return;
            }

            EditorApplication.delayCall += FindSpriteReferences;
        }
        
        if (GUILayout.Button("开始替换", GUILayout.Height(30)))
        {
            if (originalSprite == null || replacementSprite == null)
            {
                EditorUtility.DisplayDialog("错误", "请指定原始Sprite和替换Sprite", "确定");
                return;
            }

            if (!Directory.Exists(Path.Combine(Application.dataPath, searchPath.Substring(7))))
            {
                EditorUtility.DisplayDialog("错误", "指定的搜索路径不存在: " + searchPath, "确定");
                return;
            }

            EditorApplication.delayCall += ReplaceSpritesInPrefabs;
        }
        EditorGUI.EndDisabledGroup();

        if (isProcessing || isFindingReferences)
        {
            EditorGUILayout.Space();
            Rect progressRect = GUILayoutUtility.GetRect(progressBarSize.x, progressBarSize.y);
            EditorGUI.ProgressBar(progressRect, progress, progressMessage);
            Repaint();
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
        EditorGUILayout.LabelField($"引用的Prefab数量: {modifiedPrefabs.Count}");
        
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
                bool hasReference = modifiedPrefabs.Contains(prefabPath);
                EditorGUILayout.LabelField($"{"● " + (hasReference ? "<color=green>有引用</color>" : "<color=grey>无引用</color>") + " " + prefabPath}", 
                    hasReference ? EditorStyles.boldLabel : EditorStyles.miniLabel);
            }
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void ReplaceSpritesInPrefabs()
    {
        isProcessing = true;
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
        logBuilder.AppendLine($"包含非激活对象: {includeInactiveObjects}");
        logBuilder.AppendLine("----------------------------------");
        logBuilder.AppendLine("替换成功的记录:");
        
        string[] prefabPaths = Directory.GetFiles(Path.Combine(Application.dataPath, searchPath.Substring(7)), "*.prefab", SearchOption.AllDirectories)
            .Select(p => NormalizePath("Assets" + p.Substring(Application.dataPath.Length))).ToArray(); // 规范化路径格式
        
        int totalPrefabs = prefabPaths.Length;
        int processedCount = 0;
        
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string prefabPath in prefabPaths)
            {
                processedCount++;
                progress = (float)processedCount / totalPrefabs;
                progressMessage = $"处理中 ({processedCount}/{totalPrefabs}): {Path.GetFileName(prefabPath)}";
                
                if (ProcessPrefabAndLog(prefabPath, logBuilder))
                {
                    processedPrefabs.Add(prefabPath);
                    if (!dryRun || modifiedPrefabs.Contains(prefabPath))
                    {
                        modifiedPrefabs.Add(prefabPath);
                    }
                }
                
                if (processedCount % 10 == 0)
                {
                    Repaint();
                    System.Threading.Thread.Sleep(10);
                }
            }
        }
        catch (System.Exception e)
        {
            logBuilder.AppendLine($"错误: {e.Message}");
            logBuilder.AppendLine($"堆栈跟踪: {e.StackTrace}");
            Debug.LogError($"处理过程中发生错误: {e.Message}");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            isProcessing = false;
        }
        
        resultText = BuildResultText();
        
        logBuilder.AppendLine("----------------------------------");
        logBuilder.AppendLine($"处理完成!");
        logBuilder.AppendLine($"处理的Prefab数量: {processedPrefabs.Count}");
        logBuilder.AppendLine($"修改的Prefab数量: {modifiedPrefabs.Count}");
        logBuilder.AppendLine($"结束时间: {System.DateTime.Now}");
        logBuilder.AppendLine("===== 处理完成 =====");
        
        SaveLogToFile(logBuilder.ToString());
        Debug.Log(logBuilder.ToString());
        
        EditorUtility.DisplayDialog("完成", 
            $"处理完成!\n处理的Prefab数量: {processedPrefabs.Count}\n修改的Prefab数量: {modifiedPrefabs.Count}\n日志已保存到: {logFilePath}", 
            "确定");
    }
    
    private void FindSpriteReferences()
    {
        isFindingReferences = true;
        processedPrefabs.Clear();
        modifiedPrefabs.Clear();
        StringBuilder logBuilder = new StringBuilder();
        
        logBuilder.AppendLine("===== Sprite引用查找工具执行日志 =====");
        logBuilder.AppendLine($"开始时间: {System.DateTime.Now}");
        logBuilder.AppendLine($"查找的Sprite: {GetSpriteInfo(originalSprite)}");
        logBuilder.AppendLine($"搜索路径: {searchPath}");
        logBuilder.AppendLine($"包含非激活对象: {includeInactiveObjects}");
        logBuilder.AppendLine("----------------------------------");
        logBuilder.AppendLine("找到引用的记录:");
        
        string[] prefabPaths = Directory.GetFiles(Path.Combine(Application.dataPath, searchPath.Substring(7)), "*.prefab", SearchOption.AllDirectories)
            .Select(p => NormalizePath("Assets" + p.Substring(Application.dataPath.Length))).ToArray(); // 规范化路径格式
        
        int totalPrefabs = prefabPaths.Length;
        int processedCount = 0;
        
        try
        {
            foreach (string prefabPath in prefabPaths)
            {
                processedCount++;
                progress = (float)processedCount / totalPrefabs;
                progressMessage = $"查找中 ({processedCount}/{totalPrefabs}): {Path.GetFileName(prefabPath)}";
                
                if (FindReferencesInPrefabAndLog(prefabPath, logBuilder))
                {
                    processedPrefabs.Add(prefabPath);
                    modifiedPrefabs.Add(prefabPath);
                }
                
                if (processedCount % 10 == 0)
                {
                    Repaint();
                    System.Threading.Thread.Sleep(10);
                }
            }
        }
        catch (System.Exception e)
        {
            logBuilder.AppendLine($"错误: {e.Message}");
            logBuilder.AppendLine($"堆栈跟踪: {e.StackTrace}");
            Debug.LogError($"查找过程中发生错误: {e.Message}");
        }
        finally
        {
            isFindingReferences = false;
        }
        
        resultText = BuildReferenceResultText();
        
        logBuilder.AppendLine("----------------------------------");
        logBuilder.AppendLine($"查找完成!");
        logBuilder.AppendLine($"处理的Prefab数量: {processedPrefabs.Count}");
        logBuilder.AppendLine($"找到引用的Prefab数量: {modifiedPrefabs.Count}");
        logBuilder.AppendLine($"结束时间: {System.DateTime.Now}");
        logBuilder.AppendLine("===== 查找完成 =====");
        
        SaveLogToFile(logBuilder.ToString());
        Debug.Log(logBuilder.ToString());
        
        EditorUtility.DisplayDialog("完成", 
            $"查找完成!\n处理的Prefab数量: {processedPrefabs.Count}\n找到引用的Prefab数量: {modifiedPrefabs.Count}\n日志已保存到: {logFilePath}", 
            "确定");
    }

    // 修改：在替换日志中添加层次结构
    private bool ProcessPrefabAndLog(string prefabPath, StringBuilder logBuilder)
    {
        prefabPath = NormalizePath(prefabPath); // 规范化路径格式
        
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return false;
        }

        bool prefabModified = false;
        int spriteRendererCount = 0;
        int imageCount = 0;
        List<string> spriteRendererPaths = new List<string>(); // 存储层次结构
        List<string> imagePaths = new List<string>(); // 存储层次结构

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            return false;
        }

        try
        {
            SpriteRenderer[] spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>(includeInactiveObjects);
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                if (renderer.sprite == originalSprite)
                {
                    prefabModified = true;
                    spriteRendererCount++;
                    string path = GetTransformPath(renderer.transform); // 获取层次结构
                    spriteRendererPaths.Add(path);
                }
            }

            UnityEngine.UI.Image[] images = instance.GetComponentsInChildren<UnityEngine.UI.Image>(includeInactiveObjects);
            foreach (UnityEngine.UI.Image image in images)
            {
                if (image.sprite == originalSprite)
                {
                    prefabModified = true;
                    imageCount++;
                    string path = GetTransformPath(image.transform); // 获取层次结构
                    imagePaths.Add(path);
                }
            }

            if (prefabModified)
            {
                logBuilder.AppendLine($"");
                logBuilder.AppendLine($"【替换成功】Prefab: {prefabPath}"); // 输出规范化后的路径
                
                DestroyImmediate(instance);
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                
                spriteRendererCount = 0;
                imageCount = 0;
                
                spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>(includeInactiveObjects);
                foreach (SpriteRenderer renderer in spriteRenderers)
                {
                    if (renderer.sprite == originalSprite)
                    {
                        Vector3 originalScale = renderer.transform.localScale;
                        renderer.sprite = replacementSprite;
                        spriteRendererCount++;
                        
                        if (useNewSpriteSize && replacementSprite != null)
                        {
                            Vector2 originalSize = originalSprite.rect.size;
                            Vector2 newSize = replacementSprite.rect.size;
                            Vector2 sizeRatio = new Vector2(newSize.x / originalSize.x, newSize.y / originalSize.y);
                            renderer.transform.localScale = new Vector3(
                                originalScale.x * sizeRatio.x,
                                originalScale.y * sizeRatio.y,
                                originalScale.z
                            );
                            
                            string path = GetTransformPath(renderer.transform); // 获取层次结构
                            logBuilder.AppendLine($"  - 修改SpriteRenderer: {renderer.name}, 路径: {path}, 调整尺寸: {originalScale} -> {renderer.transform.localScale}");
                        }
                        else
                        {
                            string path = GetTransformPath(renderer.transform); // 获取层次结构
                            logBuilder.AppendLine($"  - 修改SpriteRenderer: {renderer.name}, 路径: {path}, 保持原尺寸");
                        }
                    }
                }

                images = instance.GetComponentsInChildren<UnityEngine.UI.Image>(includeInactiveObjects);
                foreach (UnityEngine.UI.Image image in images)
                {
                    if (image.sprite == originalSprite)
                    {
                        Vector2 originalSizeDelta = image.rectTransform.sizeDelta;
                        image.sprite = replacementSprite;
                        imageCount++;
                        
                        if (useNewSpriteSize && replacementSprite != null)
                        {
                            image.SetNativeSize();
                            string path = GetTransformPath(image.transform); // 获取层次结构
                            logBuilder.AppendLine($"  - 修改UI Image: {image.name}, 路径: {path}, 调整尺寸: {originalSizeDelta} -> {image.rectTransform.sizeDelta}");
                        }
                        else
                        {
                            string path = GetTransformPath(image.transform); // 获取层次结构
                            logBuilder.AppendLine($"  - 修改UI Image: {image.name}, 路径: {path}, 保持原尺寸");
                        }
                    }
                }

                if (!dryRun)
                {
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.UserAction);
                    logBuilder.AppendLine($"  ✔ 已保存修改 (实际替换)");
                }
                else
                {
                    logBuilder.AppendLine($"  ⚠ 预览模式: 未保存修改 (测试替换)");
                }
                
                logBuilder.AppendLine($"  共修改: {spriteRendererCount}个SpriteRenderer, {imageCount}个UI Image");
                return true;
            }
        }
        finally
        {
            DestroyImmediate(instance);
        }
        
        return false;
    }
    
    // 修改：在引用查找日志中添加层次结构
    private bool FindReferencesInPrefabAndLog(string prefabPath, StringBuilder logBuilder)
    {
        prefabPath = NormalizePath(prefabPath); // 规范化路径格式
        
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return false;
        }

        bool hasReference = false;
        int spriteRendererCount = 0;
        int imageCount = 0;
        List<string> spriteRendererPaths = new List<string>(); // 存储层次结构
        List<string> imagePaths = new List<string>(); // 存储层次结构

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            return false;
        }

        try
        {
            SpriteRenderer[] spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>(includeInactiveObjects);
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                if (renderer.sprite == originalSprite)
                {
                    hasReference = true;
                    spriteRendererCount++;
                    string path = GetTransformPath(renderer.transform); // 获取层次结构
                    spriteRendererPaths.Add(path);
                }
            }

            UnityEngine.UI.Image[] images = instance.GetComponentsInChildren<UnityEngine.UI.Image>(includeInactiveObjects);
            foreach (UnityEngine.UI.Image image in images)
            {
                if (image.sprite == originalSprite)
                {
                    hasReference = true;
                    imageCount++;
                    string path = GetTransformPath(image.transform); // 获取层次结构
                    imagePaths.Add(path);
                }
            }

            if (hasReference)
            {
                logBuilder.AppendLine($"");
                logBuilder.AppendLine($"【找到引用】Prefab: {prefabPath}"); // 输出规范化后的路径
                logBuilder.AppendLine($"  引用位置:");
                
                if (spriteRendererCount > 0)
                {
                    logBuilder.AppendLine($"  - SpriteRenderer: {spriteRendererCount}个");
                    foreach (string path in spriteRendererPaths)
                    {
                        logBuilder.AppendLine($"    - {path}"); // 输出层次结构
                    }
                }
                
                if (imageCount > 0)
                {
                    logBuilder.AppendLine($"  - UI Image: {imageCount}个");
                    foreach (string path in imagePaths)
                    {
                        logBuilder.AppendLine($"    - {path}"); // 输出层次结构
                    }
                }
                
                logBuilder.AppendLine($"  总计: {spriteRendererCount + imageCount}个引用");
                return true;
            }
        }
        finally
        {
            DestroyImmediate(instance);
        }
        
        return false;
    }

    // 新增：获取Transform的层次结构路径
    private string GetTransformPath(Transform transform)
    {
        if (transform == null)
            return "null";
            
        StringBuilder path = new StringBuilder(transform.name);
        Transform parent = transform.parent;
        
        while (parent != null && parent != transform.root)
        {
            path.Insert(0, parent.name + "/");
            parent = parent.parent;
        }
        
        return path.ToString();
    }

    // 新增：路径规范化方法，确保使用正斜杠
    private string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private string GetSpriteInfo(Sprite sprite)
    {
        if (sprite == null) return "null";
        return $"{sprite.name} ({sprite.rect.width}x{sprite.rect.height}px)";
    }
    
    private string BuildReferenceResultText()
    {
        StringBuilder resultBuilder = new StringBuilder();
        resultBuilder.AppendLine("===== Sprite引用查找工具处理结果 =====");
        resultBuilder.AppendLine($"处理的Prefab数量: {processedPrefabs.Count}");
        resultBuilder.AppendLine($"找到引用的Prefab数量: {modifiedPrefabs.Count}");
        resultBuilder.AppendLine("----------------------------------");
        resultBuilder.AppendLine("找到引用的Prefabs:");
        
        foreach (string prefabPath in modifiedPrefabs)
        {
            resultBuilder.AppendLine(prefabPath);
        }
        
        return resultBuilder.ToString();
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
            string logFolder = Path.Combine(Application.dataPath, "..", "AUnityLocal");
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }
            
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            logFilePath = Path.Combine(logFolder, $"SpriteToolEx_{timestamp}.log");
            
            File.WriteAllText(logFilePath, logContent);
            Debug.Log($"日志已保存到: {logFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存日志文件失败: {e.Message}");
            logFilePath = "";
        }
    }
}
}
