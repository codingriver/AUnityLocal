using System;
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
        private List<string> failedPrefabs = new List<string>(); // 新增：记录失败项
        private string resultText = "";
        private string logFilePath = "";
        private Vector2Int progressBarSize = new Vector2Int(400, 20);
        private float progress = 0f;
        private string progressMessage = "";
        private bool isProcessing = false;
        private bool isFindingReferences = false;

        // 美化相关的样式
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;
        private GUIStyle pathLabelStyle;
        private bool stylesInitialized = false;

        // 缓存已处理的Prefab实例ID
        private HashSet<int> processedPrefabInstanceIds = new HashSet<int>();

        // 缓存Prefab的修改状态
        private Dictionary<string, bool> prefabModificationState = new Dictionary<string, bool>();

        private void Init()
        {
            searchPath = PlayerPrefs.GetString("SpriteToolExSearchPath", searchPath);
            processedPrefabInstanceIds.Clear();
            prefabModificationState.Clear();
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            // 标题样式
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 16;
            headerStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
            headerStyle.alignment = TextAnchor.MiddleCenter;

            // 盒子样式
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.padding = new RectOffset(15, 15, 10, 10);
            boxStyle.margin = new RectOffset(5, 5, 5, 5);

            // 按钮样式
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.fixedHeight = 35;

            // 标签样式
            labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.fontSize = 11;

            // 路径标签样式
            pathLabelStyle = new GUIStyle(EditorStyles.miniLabel);
            pathLabelStyle.fontSize = 10;
            pathLabelStyle.normal.textColor = Color.gray;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();
            
            EditorGUILayout.BeginVertical();
            
            // 绘制标题区域
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            // 绘制Sprite选择区域
            DrawSpriteSelectionArea();
            
            EditorGUILayout.Space(10);
            
            // 绘制路径设置区域
            DrawPathSettingsArea();
            
            EditorGUILayout.Space(10);
            
            // 绘制选项设置区域
            DrawOptionsArea();
            
            EditorGUILayout.Space(15);
            
            // 绘制操作按钮区域
            DrawActionButtonsArea();
            
            // 绘制进度条
            // DrawProgressBar();
            
            EditorGUILayout.Space(10);
            
            // 绘制结果区域
            DrawResultsArea();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            // 绘制图标和标题
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            // 使用Unity内置图标
            GUIContent titleContent = new GUIContent(" Sprite 批量替换工具", EditorGUIUtility.IconContent("Sprite Icon").image);
            GUILayout.Label(titleContent, headerStyle, GUILayout.Height(30));
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // 副标题
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("批量替换 Prefab 中的 Sprite 或查找 Sprite 引用", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSpriteSelectionArea()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            GUILayout.Label("🎨 Sprite 设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 原始Sprite
            EditorGUILayout.BeginHorizontal();
            originalSprite = (Sprite)EditorGUILayout.ObjectField("原始 Sprite (A):",originalSprite, typeof(Sprite), false);
            EditorGUILayout.EndHorizontal();
            
            if (originalSprite != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(105);
                GUILayout.Label($"尺寸: {originalSprite.rect.width}×{originalSprite.rect.height}px", pathLabelStyle);
                EditorGUILayout.EndHorizontal();
                
                // 显示完整Sprite名称
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(105);
                GUILayout.Label($"完整名称: {GetSpriteFullName(originalSprite)}", pathLabelStyle);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            
            // 替换Sprite
            EditorGUILayout.BeginHorizontal();
            replacementSprite = (Sprite)EditorGUILayout.ObjectField("替换 Sprite (B):",replacementSprite, typeof(Sprite), false);
            EditorGUILayout.EndHorizontal();
            
            if (replacementSprite != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(105);
                GUILayout.Label($"尺寸: {replacementSprite.rect.width}×{replacementSprite.rect.height}px", pathLabelStyle);
                EditorGUILayout.EndHorizontal();
                
                // 显示完整Sprite名称
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(105);
                GUILayout.Label($"完整名称: {GetSpriteFullName(replacementSprite)}", pathLabelStyle);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawPathSettingsArea()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            GUILayout.Label("📁 搜索路径", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("路径:", GUILayout.Width(40));
            var searchPath1 = EditorGUILayout.TextField(searchPath);
            if (searchPath1 != searchPath)
            {
                searchPath = searchPath1;
                PlayerPrefs.SetString("SpriteToolExSearchPath", searchPath);
            }
            
            // 美化的选择路径按钮
            GUIContent folderContent = new GUIContent("浏览", EditorGUIUtility.IconContent("Folder Icon").image);
            if (GUILayout.Button(folderContent, GUILayout.Width(70), GUILayout.Height(20)))
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
            
            // 显示路径状态
            if (searchPath.Length>=7&&Directory.Exists(Path.Combine(Application.dataPath, searchPath.Substring(7))))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(45);
                GUIContent validContent = new GUIContent("✓ 路径有效", EditorGUIUtility.IconContent("TestPassed").image);
                GUILayout.Label(validContent, pathLabelStyle);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(45);
                GUIContent invalidContent = new GUIContent("✗ 路径无效", EditorGUIUtility.IconContent("TestFailed").image);
                GUILayout.Label(invalidContent, pathLabelStyle);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawOptionsArea()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            GUILayout.Label("选项设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 使用网格布局来组织选项
            EditorGUILayout.BeginVertical();
            
            dryRun = EditorGUILayout.ToggleLeft(new GUIContent("仅预览（不保存修改）", "启用后只会显示将要修改的内容，不会实际保存"), dryRun);
            useNewSpriteSize = EditorGUILayout.ToggleLeft(new GUIContent("使用新Sprite的尺寸", "替换时自动调整为新Sprite的尺寸"), useNewSpriteSize);
            includeInactiveObjects = EditorGUILayout.ToggleLeft(new GUIContent("包含非激活对象", "搜索时包括被禁用的GameObject"), includeInactiveObjects);
            
            // 新增：显示详细的处理信息
            bool showDetails = processedPrefabs.Count > 0 || modifiedPrefabs.Count > 0;
            if (showDetails)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("处理统计:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"已处理Prefabs: {processedPrefabs.Count}", pathLabelStyle);
                EditorGUILayout.LabelField($"已修改Prefabs: {modifiedPrefabs.Count}", pathLabelStyle);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtonsArea()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            EditorGUI.BeginDisabledGroup(isProcessing || isFindingReferences);
            
            EditorGUILayout.BeginHorizontal();
            
            // 查找missing
            GUIStyle findButtonStyle1 = new GUIStyle(buttonStyle);
            findButtonStyle1.normal.textColor = new Color(1f, 0.2f, 0.2f);
            GUIContent findContent1 = new GUIContent("统计Sprite Missing", EditorGUIUtility.IconContent("Search Icon").image);
            if (GUILayout.Button(findContent1, findButtonStyle1))
            {

                EditorApplication.delayCall += FindSpriteMissingReferences;
            }
            
            GUILayout.Space(10);            
            
            // 查找引用按钮
            GUIStyle findButtonStyle = new GUIStyle(buttonStyle);
            findButtonStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
            
            GUIContent findContent = new GUIContent("查找引用", EditorGUIUtility.IconContent("Search Icon").image);
            if (GUILayout.Button(findContent, findButtonStyle))
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
            
            GUILayout.Space(10);
            
            // 开始替换按钮
            GUIStyle replaceButtonStyle = new GUIStyle(buttonStyle);
            replaceButtonStyle.normal.textColor = dryRun ? new Color(1f, 0.8f, 0.3f) : new Color(0.3f, 1f, 0.3f);
            
            string buttonText = dryRun ? "预览替换" : "开始替换";
            GUIContent replaceContent = new GUIContent(buttonText, EditorGUIUtility.IconContent("Refresh").image);
            if (GUILayout.Button(replaceContent, replaceButtonStyle))
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
            
            EditorGUILayout.EndHorizontal();
            
            // 新增：重置按钮
            EditorGUILayout.Space(10);
            if (GUILayout.Button("重置工具", buttonStyle))
            {
                ResetTool();
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }

        // 新增：重置工具状态
        private void ResetTool()
        {
            originalSprite = null;
            replacementSprite = null;
            processedPrefabs.Clear();
            modifiedPrefabs.Clear();
            resultText = "";
            logFilePath = "";
            processedPrefabInstanceIds.Clear();
            prefabModificationState.Clear();
            Repaint();
        }

        private void DrawProgressBar()
        {
            if (isProcessing || isFindingReferences)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                GUILayout.Label("处理进度", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                
                Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(progressRect, progress, progressMessage);
                
                EditorGUILayout.EndVertical();
                Repaint();
            }
        }

        private void DrawResultsArea()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            // 结果标题
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("处理结果", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            // 操作按钮
            if (GUILayout.Button(new GUIContent("复制结果", "复制结果到剪贴板"), GUILayout.Width(100), GUILayout.Height(20)))
            {
                CopyResultToClipboard();
            }
            
            if (!string.IsNullOrEmpty(logFilePath) && GUILayout.Button(new GUIContent("打开日志", "打开日志文件"), GUILayout.Width(100), GUILayout.Height(20)))
            {
                System.Diagnostics.Process.Start(logFilePath);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 结果统计信息
            if (processedPrefabs.Count > 0 || modifiedPrefabs.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 处理数量统计
                GUIStyle statStyle = new GUIStyle(EditorStyles.miniLabel);
                statStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                
                GUILayout.Label($"已处理: {processedPrefabs.Count} 个Prefab", statStyle);
                GUILayout.Space(20);
                GUILayout.Label($"已修改: {modifiedPrefabs.Count} 个Prefab", statStyle);
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }
            
            // 结果文本区域
            if (!string.IsNullOrEmpty(resultText))
            {
                GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
                textAreaStyle.wordWrap = true;
                textAreaStyle.fontSize = 10;
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                EditorGUILayout.TextArea(resultText, textAreaStyle, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
            else
            {
                // 空状态提示
                EditorGUILayout.BeginVertical(GUILayout.Height(100));
                GUILayout.FlexibleSpace();
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("暂无处理结果", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void CopyResultToClipboard()
        {
            if (!string.IsNullOrEmpty(resultText))
            {
                EditorGUIUtility.systemCopyBuffer = resultText;
                EditorUtility.DisplayDialog("提示", "结果已复制到剪贴板", "确定");
            }
        }

        void FindSpriteMissingReferences()
        {
            PrefabToolEx.CheckAllPrefabsForMissingSprites(searchPath);
        }
        private string GetGameObjectPath(GameObject obj, GameObject root)
        {
            if (obj == root)
                return root.name;
            
            List<string> path = new List<string>();
            Transform current = obj.transform;
            
            while (current != null && current.gameObject != root)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }
            
            path.Insert(0, root.name);
            return string.Join("/", path);
        }

        private void SaveLogFile(string logType, string content)
        {
            try
            {
                string logDirectory = Path.Combine(Application.dataPath, "../AUnityLocal");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{timestamp}_{logType}.txt";
                logFilePath = Path.Combine(logDirectory, fileName);
                
                File.WriteAllText(logFilePath, content, Encoding.UTF8);
            }
            catch (System.Exception e)
            {
                Debug.LogError("保存日志文件失败: " + e.Message);
                logFilePath = "";
            }
        }

        private void OnDestroy()
        {
            // 清理进度条
            EditorUtility.ClearProgressBar();
        }

        private void OnInspectorUpdate()
        {
            // 在处理过程中更新进度条
            // if (isProcessing || isFindingReferences)
            // {
            //     EditorUtility.DisplayProgressBar(progressMessage, 
            //         $"{progressMessage} {(progress * 100):F1}%", progress);
            //     Repaint();
            // }
        }

        // 工具栏菜单项
        [MenuItem("AUnityLocal/Sprite替换工具")]
        public static void ShowWindow()
        {
            SpriteToolExEditor window = GetWindow<SpriteToolExEditor>();
            window.titleContent = new GUIContent("Sprite替换工具");
            window.minSize = new Vector2(450, 600);
            window.Init();
            window.Show();
        }

        // 右键菜单 - 从选中的Sprite创建替换任务
        [MenuItem("Assets/Sprite替换工具/设为原始Sprite", false, 1000)]
        public static void SetAsOriginalSprite()
        {
            if (Selection.activeObject is Sprite sprite)
            {
                SpriteToolExEditor window = GetWindow<SpriteToolExEditor>();
                window.originalSprite = sprite;
                window.titleContent = new GUIContent("Sprite替换工具");
                window.minSize = new Vector2(450, 600);
                window.Init();
                window.Show();
                window.Focus();
            }
        }

        [MenuItem("Assets/Sprite替换工具/设为替换Sprite", false, 1001)]
        public static void SetAsReplacementSprite()
        {
            if (Selection.activeObject is Sprite sprite)
            {
                SpriteToolExEditor window = GetWindow<SpriteToolExEditor>();
                window.replacementSprite = sprite;
                window.titleContent = new GUIContent("Sprite替换工具");
                window.minSize = new Vector2(450, 600);
                window.Init();
                window.Show();
                window.Focus();
            }
        }

        // 验证菜单项是否可用
        [MenuItem("Assets/Sprite替换工具/设为原始Sprite", true)]
        [MenuItem("Assets/Sprite替换工具/设为替换Sprite", true)]
        public static bool ValidateSetSprite()
        {
            return Selection.activeObject is Sprite;
        }

        // 快捷键支持
        [MenuItem("Tools/Sprite替换工具 %&s")] // Ctrl+Alt+S
        public static void ShowWindowWithShortcut()
        {
            ShowWindow();
        }

        // 帮助方法 - 获取相对路径
        private string GetRelativePath(string fullPath)
        {
            if (fullPath.StartsWith(Application.dataPath))
            {
                return "Assets" + fullPath.Substring(Application.dataPath.Length);
            }
            return fullPath;
        }

        // 帮助方法 - 格式化文件大小
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        // 帮助方法 - 验证输入
        private bool ValidateInputs()
        {
            if (originalSprite == null)
            {
                EditorUtility.DisplayDialog("错误", "请选择原始Sprite", "确定");
                return false;
            }

            if (string.IsNullOrEmpty(searchPath))
            {
                EditorUtility.DisplayDialog("错误", "请选择搜索路径", "确定");
                return false;
            }

            if (!AssetDatabase.IsValidFolder(searchPath))
            {
                EditorUtility.DisplayDialog("错误", "搜索路径无效", "确定");
                return false;
            }

            return true;
        }

        private bool ValidateReplacementInputs()
        {
            if (!ValidateInputs())
                return false;

            if (replacementSprite == null)
            {
                EditorUtility.DisplayDialog("错误", "请选择替换Sprite", "确定");
                return false;
            }

            if (originalSprite == replacementSprite)
            {
                EditorUtility.DisplayDialog("错误", "原始Sprite和替换Sprite不能相同", "确定");
                return false;
            }

            return true;
        }

        // 新增方法：获取Sprite完整名称（包括文件名和Sprite名称）
        private string GetSpriteFullName(Sprite sprite)
        {
            if (sprite == null)
                return "None";
                
            string assetPath = AssetDatabase.GetAssetPath(sprite);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            
            // 尝试获取Sprite在SpriteSheet中的名称
            string spriteName = sprite.name;
            
            // 如果Sprite名称包含文件名部分，移除它以避免重复
            if (spriteName.StartsWith(fileName))
            {
                spriteName = spriteName.Substring(fileName.Length).TrimStart('_');
            }
            
            if (string.IsNullOrEmpty(spriteName))
                return fileName;
                
            return $"{fileName}#{spriteName}";
        }
        

        // 新增：重试失败项功能
        private void RetryFailedPrefabs()
        {
            if (failedPrefabs.Count == 0) return;

            StringBuilder sb = new StringBuilder(resultText);
            sb.AppendLine("\n=== 重试失败项 ===");
            
            List<string> retrySuccess = new List<string>();
            List<string> stillFailed = new List<string>();

            for (int i = 0; i < failedPrefabs.Count; i++)
            {
                progress = (float)i / failedPrefabs.Count;
                progressMessage = $"重试失败项 ({i + 1}/{failedPrefabs.Count})";
                
                try
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(failedPrefabs[i]);
                    if (prefab != null)
                    {
                        bool modified = ProcessPrefab(prefab, failedPrefabs[i], sb);
                        if (modified)
                        {
                            modifiedPrefabs.Add(failedPrefabs[i]);
                            retrySuccess.Add(failedPrefabs[i]);
                        }
                    }
                }
                catch
                {
                    stillFailed.Add(failedPrefabs[i]);
                }
            }

            failedPrefabs = stillFailed;
            sb.AppendLine($"成功重试: {retrySuccess.Count} 个, 仍然失败: {stillFailed.Count} 个");
            resultText = sb.ToString();
        }

        private void FindSpriteReferences()
        {
            isFindingReferences = true;
            progress = 0f;
            progressMessage = "正在查找Sprite引用...";
            failedPrefabs.Clear(); // 重置失败列表
            DateTime startTime = DateTime.Now;
            try
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });
                List<string> referencingPrefabs = new List<string>();
                string originalSpriteFullName = GetSpriteFullName(originalSprite);
                
                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    progress = (float)i / prefabGuids.Length;
                    progressMessage = $"正在查找引用... ({i + 1}/{prefabGuids.Length})";
                    EditorUtility.DisplayProgressBar(progressMessage, $"{progressMessage} {(progress * 100):F1}%", progress);
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    
                    try
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        
                        if (prefab != null)
                        {
                            int instanceId = prefab.GetInstanceID();
                            if (processedPrefabInstanceIds.Contains(instanceId))
                                continue;
                                
                            processedPrefabInstanceIds.Add(instanceId);
                            
                            // 扩展检测组件类型
                            bool hasReference = CheckSpriteReferences(prefab);
                            
                            if (hasReference)
                            {
                                referencingPrefabs.Add(prefabPath);
                            }
                        }
                    }
                    catch
                    {
                        failedPrefabs.Add(prefabPath);
                    }
                }
                
                // 生成结果（添加失败项统计）
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"=== Sprite引用查找结果 ===");
                sb.AppendLine($"查找时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"原始Sprite: {originalSpriteFullName}");
                sb.AppendLine($"搜索路径: {searchPath}");
                sb.AppendLine($"包含非激活对象: {(includeInactiveObjects ? "是" : "否")}");
                sb.AppendLine($"总计扫描: {prefabGuids.Length} 个Prefab");
                sb.AppendLine($"找到引用: {referencingPrefabs.Count} 个Prefab");
                sb.AppendLine($"加载失败: {failedPrefabs.Count} 个Prefab");
                
                if (referencingPrefabs.Count > 0)
                {
                    sb.AppendLine("\n引用此Sprite的Prefab列表:");
                    for (int i = 0; i < referencingPrefabs.Count; i++)
                    {
                        sb.AppendLine($"{i + 1}. {referencingPrefabs[i]}");
                    }
                }
                
                if (failedPrefabs.Count > 0)
                {
                    sb.AppendLine("\n加载失败的Prefab:");
                    for (int i = 0; i < failedPrefabs.Count; i++)
                    {
                        sb.AppendLine($"{i + 1}. {failedPrefabs[i]}");
                    }
                }
                
                TimeSpan elapsedTime = DateTime.Now - startTime;
                sb.AppendLine($"替换完成完成，总耗时: {elapsedTime.TotalSeconds:F2} 秒");
                resultText = sb.ToString();
                processedPrefabs = new List<string>(referencingPrefabs);
                modifiedPrefabs.Clear();
                SaveLogFile("SpriteReferenceSearch", sb.ToString());
                progress = 1f;
                progressMessage = "查找完成";
                
                EditorUtility.DisplayDialog("查找完成", 
                    $"扫描了 {prefabGuids.Length} 个Prefab\n找到 {referencingPrefabs.Count} 个引用\n失败 {failedPrefabs.Count} 个", 
                    "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("错误", "查找过程中出现错误: " + e.Message, "确定");
                Debug.LogError("FindSpriteReferences Error: " + e);
            }
            finally
            {
                isFindingReferences = false;
                EditorUtility.ClearProgressBar();
                processedPrefabInstanceIds.Clear();
            }
        }

        // 扩展组件检测范围
        private bool CheckSpriteReferences(GameObject prefab)
        {
            bool hasReference = false;
            
            // 1. 检测标准组件
            var spriteRenderers = prefab.GetComponentsInChildren<SpriteRenderer>(includeInactiveObjects);
            var images = prefab.GetComponentsInChildren<UnityEngine.UI.Image>(includeInactiveObjects);
            
            hasReference |= spriteRenderers.Any(sr => sr.sprite == originalSprite);
            hasReference |= images.Any(img => img.sprite == originalSprite);
            
            // 2. 检测Tilemap组件
            var tilemaps = prefab.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(includeInactiveObjects);
            foreach (var tilemap in tilemaps)
            {
                // 只检查已使用的Tile位置
                for (int x = tilemap.cellBounds.xMin; x < tilemap.cellBounds.xMax; x++)
                {
                    for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax; y++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, 0);
                        var tile = tilemap.GetTile<UnityEngine.Tilemaps.Tile>(pos);
                        if (tile != null && tile.sprite == originalSprite)
                        {
                            hasReference = true;
                            break;
                        }
                    }
                    if (hasReference) break;
                }
            }
            
            // 3. 检测自定义组件（需要用户自行实现接口）
            var customSpriteHolders = prefab.GetComponentsInChildren<ICustomSpriteHolder>(includeInactiveObjects);
            hasReference |= customSpriteHolders.Any(holder => holder.GetSprite() == originalSprite);
            
            return hasReference;
        }

        private void ReplaceSpritesInPrefabs()
        {
            isProcessing = true;
            progress = 0f;
            progressMessage = "正在搜索Prefab...";
            failedPrefabs.Clear(); // 重置失败列表
            DateTime startTime = DateTime.Now;
            try
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });
                int max = prefabGuids.Length;
                Dictionary<int, List<string>> layeredPrefabs=PrefabToolEx.StartAnalysisPrefabsdependencys(searchPath);
                processedPrefabs.Clear();
                modifiedPrefabs.Clear();
                
                string originalSpriteFullName = GetSpriteFullName(originalSprite);
                string replacementSpriteFullName = GetSpriteFullName(replacementSprite);
                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"=== Sprite替换{(dryRun ? "预览" : "执行")}结果 ===");
                sb.AppendLine($"处理时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"原始Sprite: {originalSpriteFullName}");
                sb.AppendLine($"替换Sprite: {replacementSpriteFullName}");
                sb.AppendLine($"搜索路径: {searchPath}");
                sb.AppendLine($"使用新尺寸: {(useNewSpriteSize ? "是" : "否")}");
                sb.AppendLine($"包含非激活对象: {(includeInactiveObjects ? "是" : "否")}");
                sb.AppendLine($"模式: {(dryRun ? "仅预览" : "实际替换")}");
                sb.AppendLine();
                var sortedKeys= layeredPrefabs.Keys.ToList();
                sortedKeys.Sort();
                sortedKeys.Reverse();
                int count = 0;
                for (int i = 0; i < sortedKeys.Count; i++)
                {
                    var key= sortedKeys[i];
                    var ls= layeredPrefabs[key];
                    sb.AppendLine($"Prefab依赖层级 {key}: {ls.Count} 个Prefab");
                    foreach (var prefabPath in ls)
                    {
                        count++;
                        progress = (float)count / max;
                        progressMessage = $"正在处理Prefab(layer:{key})... ({count}/{max})";
                        EditorUtility.DisplayProgressBar(progressMessage, $"{progressMessage} {(progress * 100):F1}%", progress);
                        try
                        {
                            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        
                            if (prefab != null)
                            {
                                int instanceId = prefab.GetInstanceID();
                                if (processedPrefabInstanceIds.Contains(instanceId))
                                    continue;
                                
                                processedPrefabInstanceIds.Add(instanceId);
                                processedPrefabs.Add(prefabPath);
                                bool modified = ProcessPrefab(prefab, prefabPath, sb);
                            
                                if (modified)
                                {
                                    modifiedPrefabs.Add(prefabPath);
                                    prefabModificationState[prefabPath] = true;
                                }
                                else
                                {
                                    prefabModificationState[prefabPath] = false;
                                }
                            }
                        }
                        catch
                        {
                            failedPrefabs.Add(prefabPath);
                        }
                    }
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                
                sb.AppendLine();
                sb.AppendLine($"总计处理: {processedPrefabs.Count} 个Prefab");
                sb.AppendLine($"发现修改: {modifiedPrefabs.Count} 个Prefab");
                sb.AppendLine($"处理失败: {failedPrefabs.Count} 个Prefab");
                
                if (!dryRun && modifiedPrefabs.Count > 0)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    sb.AppendLine("所有修改已保存。");
                }
                
                resultText = sb.ToString();
                string logType = dryRun ? "SpriteReplacePreview" : "SpriteReplace";
                TimeSpan elapsedTime = DateTime.Now - startTime;
                sb.AppendLine($"替换完成完成，总耗时: {elapsedTime.TotalSeconds:F2} 秒");
                Debug.Log($"{logType}，总耗时: {elapsedTime.TotalSeconds:F2} 秒");
                SaveLogFile(logType, sb.ToString());
                progress = 1f;
                progressMessage = "处理完成";
                
                string dialogTitle = dryRun ? "预览完成" : "替换完成";
                string dialogMessage = $"处理了 {processedPrefabs.Count} 个Prefab\n" +
                                      $"{(dryRun ? "发现" : "修改了")} {modifiedPrefabs.Count} 个Prefab\n" +
                                      $"失败 {failedPrefabs.Count} 个";
                
                EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("错误", "处理过程中出现错误: " + e.Message, "确定");
                Debug.LogError("ReplaceSpritesInPrefabs Error: " + e);
            }
            finally
            {
                isProcessing = false;
                EditorUtility.ClearProgressBar();
                processedPrefabInstanceIds.Clear();
            }
        }

        private bool ProcessPrefab(GameObject prefab, string prefabPath, StringBuilder logBuilder)
        {
            bool modified = false;
            List<string> modifications = new List<string>();
            
            // 处理SpriteRenderer组件
            var spriteRenderers = prefab.GetComponentsInChildren<SpriteRenderer>(includeInactiveObjects);
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite == originalSprite)
                {
                    if (!dryRun)
                    {
                        sr.sprite = replacementSprite;
                        
                        if (useNewSpriteSize && replacementSprite != null)
                        {
                            sr.size = new Vector2(
                                replacementSprite.rect.width / replacementSprite.pixelsPerUnit,
                                replacementSprite.rect.height / replacementSprite.pixelsPerUnit
                            );
                        }
                        
                        // 修复材质引用残留问题
                        if (sr.material != null && 
                            sr.material.mainTexture == originalSprite.texture)
                        {
                            sr.material.mainTexture = replacementSprite.texture;
                        }
                        
                        EditorUtility.SetDirty(prefab);
                    }
                    
                    string objPath = GetGameObjectPath(sr.gameObject, prefab);
                    modifications.Add($"  - SpriteRenderer: {objPath}");
                    modified = true;
                }
            }
            
            // 处理UI Image组件
            var images = prefab.GetComponentsInChildren<UnityEngine.UI.Image>(includeInactiveObjects);
            foreach (var img in images)
            {
                if (img.sprite == originalSprite)
                {
                    if (!dryRun)
                    {
                        img.sprite = replacementSprite;
                        
                        if (useNewSpriteSize && replacementSprite != null)
                        {
                            img.SetNativeSize();
                        }
                        
                        // 修复材质引用残留问题
                        // if (img.material != null && 
                        //     img.material.mainTexture == originalSprite.texture)
                        // {
                        //     img.material.mainTexture = replacementSprite.texture;
                        // }
                        
                        EditorUtility.SetDirty(prefab);
                    }
                    
                    string objPath = GetGameObjectPath(img.gameObject, prefab);
                    modifications.Add($"  - UI Image: {objPath}");
                    modified = true;
                }
            }
            
            // 处理Tilemap组件
            var tilemaps = prefab.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(includeInactiveObjects);
            foreach (var tilemap in tilemaps)
            {
                bool tilemapModified = false;
                List<Vector3Int> modifiedPositions = new List<Vector3Int>();
                
                for (int x = tilemap.cellBounds.xMin; x < tilemap.cellBounds.xMax; x++)
                {
                    for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax; y++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, 0);
                        var tile = tilemap.GetTile<UnityEngine.Tilemaps.Tile>(pos);
                        if (tile != null && tile.sprite == originalSprite)
                        {
                            if (!dryRun)
                            {
                                // 创建新Tile实例避免修改原始资源
                                var newTile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                                newTile.sprite = replacementSprite;
                                newTile.color = tile.color;
                                newTile.transform = tile.transform;
                                newTile.gameObject = tile.gameObject;
                                newTile.flags = tile.flags;
                                newTile.colliderType = tile.colliderType;
                                
                                tilemap.SetTile(pos, newTile);
                                EditorUtility.SetDirty(tilemap);
                            }
                            
                            modifiedPositions.Add(pos);
                            tilemapModified = true;
                        }
                    }
                }
                
                if (tilemapModified)
                {
                    modifications.Add($"  - Tilemap: {tilemap.name} (修改位置: {modifiedPositions.Count})");
                    modified = true;
                }
            }
            
            // 记录修改日志
            if (modified)
            {
                logBuilder.AppendLine($"Prefab: {prefabPath}");
                foreach (string mod in modifications)
                {
                    logBuilder.AppendLine(mod);
                }
                logBuilder.AppendLine();
            }
            
            return modified;
        }
        // 新增接口：支持自定义Sprite组件
        public interface ICustomSpriteHolder
        {
            Sprite GetSprite();
            void SetSprite(Sprite newSprite);
        }
        
    }
}

