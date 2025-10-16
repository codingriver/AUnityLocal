using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using FxProNS;
using Skyunion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AUnityLocal.Editor
{
    public class EditorWindowTool : EditorWindow
    {
        // 样式缓存
        private GUIStyle titleStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle buttonStyle;
        private GUIStyle boxStyle;
        private GUIStyle fieldStyle;
        private Vector2 scrollPosition;

        [MenuItem("AUnityLocal/EditorWindowTool", false, 1000)]
        public static void ShowWindow()
        {
            var window = GetWindow<EditorWindowTool>("EditorWindowTool 工具");
            window.minSize = new Vector2(900, 800);
            window.maxSize = new Vector2(1400, 1200);
        }


        private string arg1 = "state";
        private string arg2 = "2";
        private Vector3 arg3 = new Vector3(1, 0, 0);
        private int count = 20;
        private Transform root = null;
        private Transform root2 = null;
        private List<GameObject> objs = new List<GameObject>();
        bool includeInactive = true;
        bool includeDisabled = true;
        private int sortingOrder = 5300;
        private ReorderableList<GameObject> _gameObjectFilterList = null;

        void OnEnable()
        {
            _gameObjectFilterList = new ReorderableList<GameObject>("GameObjects");
        }

        private void InitializeStyles()
        {
            // 标题样式
            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 15, 20),
                normal = { textColor = new Color(0.2f, 0.8f, 1.0f) }
            };

            // 区域标题样式
            sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(5, 5, 10, 5),
                normal = { textColor = new Color(0.8f, 0.9f, 1.0f) }
            };

            // 按钮样式
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 35,
                margin = new RectOffset(5, 5, 3, 3),
                padding = new RectOffset(10, 10, 8, 8)
            };

            // 盒子样式
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            // 字段样式
            fieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 11,
                fixedHeight = 20
            };
        }

        private void OnGUI()
        {
            if (titleStyle == null) InitializeStyles();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawTitle();

            EditorGUILayout.BeginVertical(boxStyle);

            // 基础参数区域
            DrawSection("🔧 基础参数", () =>
            {
                root = (Transform)EditorGUILayout.ObjectField(new GUIContent("根节点:", "指定操作的根节点"),
                    root, typeof(Transform), true, GUILayout.Height(20));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("参数1:", GUILayout.Width(60));
                arg1 = EditorGUILayout.TextField(arg1, fieldStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("参数2:", GUILayout.Width(60));
                arg2 = EditorGUILayout.TextField(arg2, fieldStyle);
                EditorGUILayout.EndHorizontal();

                arg3 = EditorGUILayout.Vector3Field("参数3:", arg3);
                count = EditorGUILayout.IntField(new GUIContent("复制数量:", "设置复制物体的数量"), count);

                EditorGUILayout.BeginHorizontal();
                includeInactive = EditorGUILayout.Toggle("包含未激活对象", includeInactive);
                includeDisabled = EditorGUILayout.Toggle("包含未启用组件", includeDisabled);
                EditorGUILayout.EndHorizontal();
            });

            GUILayout.Space(10);

            // 动画控制区域
            DrawSection("🎬 动画控制", () =>
            {
                if (DrawButton("设置动画参数", "设置所有Animator组件的参数", Color.cyan))
                {
                    SetAnimatorParameters();
                }
            });

            GUILayout.Space(5);

            // 物体操作区域
            DrawSection("📦 物体操作", () =>
            {
                root2= (Transform)EditorGUILayout.ObjectField(new GUIContent("root2节点:", "指定参考节点"),
                    root2, typeof(Transform), true, GUILayout.Height(20));
                EditorGUILayout.BeginHorizontal();
                if (DrawButton("设置物体名字", "根据TroopsSkinCarEvent设置物体名字", Color.yellow, GUILayout.Width(180)))
                {
                    SetObjectNames();
                }

                if (DrawButton("显示子节点数量", "显示根节点的子节点数量", Color.green, GUILayout.Width(180)))
                {
                    ShowChildCount();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (DrawButton("显示选中节点数量", "显示当前选中的节点数量", Color.magenta, GUILayout.Width(180)))
                {
                    ShowSelectedCount();
                }

                if (DrawButton("打印Path相对根节点", "打印选中节点相对于根节点的路径", Color.white, GUILayout.Width(180)))
                {
                    Tools.PrintRelativePaths(Selection.transforms,root);
                }

                EditorGUILayout.EndHorizontal();
                if (DrawButton("打印子节点下所有节点数量", "", Color.white, GUILayout.Width(180)))
                {
                    Tools.PrintChildCount(root,true);
                }
                if (DrawButton("设置子物体坐标摆放", "", Color.white, GUILayout.Width(180)))
                {
                    if (root != null)
                    {
                        for (int i = 0; i < root.childCount; i++)
                        {
                            root.GetChild(i).localPosition = arg3 * i;
                        }
                    }
                }                     
                if (DrawButton("打印组件参数", "", Color.white, GUILayout.Width(180)))
                {
                    if (root != null)
                    {
                        var arr=root.GetComponentsInChildren<TroopsSkinCarEvent>(true);
                        foreach (var com in arr)
                        {
                            Debug.Log($"物体 {com.gameObject.name} 的 m_DressId 为 {com.m_DressId}");
                        }
                    }
                }                    
                if (DrawButton("设置组件参数", "", Color.white, GUILayout.Width(180)))
                {
                    if (root != null&&root2!=null)
                    {
                        for (int i = 0; i < root.childCount; i++)
                        {
                            var t= root.GetChild(i);
                            var t2= root2.Find(t.name);
                            if (t2 != null)
                            {
                                var evt1 = t.GetComponentInChildren<TroopsSkinCarEvent>();
                                var evt2 = t2.GetComponentInChildren<TroopsSkinCarEvent>();
                                if (evt1 != null && evt2 != null)
                                {
                                    evt2.m_DressId = evt1.m_DressId;
                                    Debug.Log($"设置 {t.name} 的 m_DressId 为 {evt2.m_DressId}");
                                }
                            }
                        }
                    }
                }            
                if (DrawButton("设置所有子物体缩放", "", Color.white, GUILayout.Width(180)))
                {
                    if (root != null)
                    {
                        var arr=root.GetComponentsInChildren<Transform>(true);
                        foreach (var transform in arr)
                        {
                            if (transform.localScale.x + transform.localScale.y + transform.localScale.z < 0.01f)
                            {
                                transform.localScale = Vector3.one;
                            }
                        }
                    }
                }
                
                if (DrawButton("设置SkinCar参数", "", Color.white, GUILayout.Width(180)))
                {
                    if (root != null)
                    {
                        var arr=root.GetComponentsInChildren<TroopsSkinCar>(true);
                        foreach (var com in arr)
                        {
                            com.m_formation_last_state = Troops.ENMU_SQUARE_STAT.FIGHT;
                            com.m_formation_state = Troops.ENMU_SQUARE_STAT.FIGHT;
                            EditorUtility.SetDirty(com);
                        }
                    }
                }                    
            });

            GUILayout.Space(5);

            // 复制功能区域
            DrawSection("📋 复制功能", () =>
            {
                _gameObjectFilterList?.DoLayoutList();
                GUILayout.Space(5);
                if (DrawButton("复制物体", "根据设置复制选中的物体", Color.green))
                {
                    CopyObjects();
                }
            });

            GUILayout.Space(5);

            // 渲染控制区域
            DrawSection("🎨 渲染控制", () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Order值:", GUILayout.Width(60));
                sortingOrder = EditorGUILayout.IntField(sortingOrder, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (DrawButton("设置Order", "设置粒子系统的渲染顺序", Color.blue, GUILayout.Width(120)))
                {
                    SetSortingOrder();
                }

                if (DrawButton("设置Order偏移", "在当前Order基础上添加基数", Color.cyan, GUILayout.Width(120)))
                {
                    AddSortingOrderBase();
                }

                if (DrawButton("打印SortingOrder", "打印所有渲染器的Order信息", Color.yellow, GUILayout.Width(120)))
                {
                    PrintSortingOrder();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (DrawButton("隐藏SkinnedMeshRenderer", "隐藏所有蒙皮网格渲染器", Color.red, GUILayout.Width(180)))
                {
                    ToggleSkinnedMeshRenderer(false);
                }

                if (DrawButton("显示SkinnedMeshRenderer", "显示所有蒙皮网格渲染器", Color.green, GUILayout.Width(180)))
                {
                    ToggleSkinnedMeshRenderer(true);
                }

                EditorGUILayout.EndHorizontal();

                if (DrawButton("打印材质球数量", "统计并打印材质球使用情况", Color.magenta))
                {
                    PrintMaterialCount();
                }
            });

            GUILayout.Space(5);

            // 性能分析区域
            DrawSection("📊 性能分析", () =>
            {
                if (DrawButton("开启分析状态", "开启性能分析模式", Color.red))
                {
                    SetProfilerStatus();
                }
            });
            GUILayout.Space(5);

            
            DrawSection("📊 模型处理", () =>
            {
                ProcessFBXOnlyLog= EditorGUILayout.Toggle("只打印模型数据",ProcessFBXOnlyLog);
                if (DrawButton("查询引用模型"))
                {
                    ProcessFBX(true);
                }
                if (DrawButton("修复模型"))
                {
                    ProcessFBX(false);
                }                
            });
            DrawSection("📊 Prefab相关", () =>
            {
                if (DrawButton("查询Prefab依赖的Prefab"))
                {
                    ProcessPrefab();
                }
                if (DrawButton("Prefab还原Animator"))
                {
                    RevertSpecificPrefabAnimator();
                }                
            });            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawSection(string title, System.Action content)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            GUILayout.Label(title, sectionHeaderStyle);

            // 绘制分隔线
            Rect rect = GUILayoutUtility.GetRect(1, 2);
            EditorGUI.DrawRect(rect, new Color(0.4f, 0.6f, 0.8f, 0.5f));

            GUILayout.Space(5);
            content?.Invoke();
            EditorGUILayout.EndVertical();
        }

        private bool DrawButton(string text, string tooltip = "", Color? color = null, params GUILayoutOption[] options)
        {
            Color originalColor = GUI.backgroundColor;
            if (color.HasValue)
            {
                GUI.backgroundColor = color.Value;
            }

            GUIContent content = new GUIContent(text, tooltip);
            bool result = GUILayout.Button(content, buttonStyle, options);

            GUI.backgroundColor = originalColor;
            return result;
        }

        // 将原来的按钮功能拆分为独立方法
        private void SetAnimatorParameters()
        {
            Animator[] allObjects = null;
            if (root != null)
            {
                allObjects = root.GetComponentsInChildren<Animator>(includeInactive);
            }
            else
            {
                allObjects = FindObjectsOfType<Animator>(includeInactive);
            }

            if (allObjects == null)
            {
                Debug.LogWarning("未找到任何 Animator 组件");
                return;
            }

            int intArg2 = 0;
            if (!int.TryParse(arg2, out intArg2))
            {
                Debug.LogWarning("参数2必须是整数");
                return;
            }

            foreach (var com in allObjects)
            {
                if (!includeDisabled && !com.enabled)
                {
                    continue;
                }

                com.SetInteger(arg1, intArg2);
                Debug.Log($"设置 {com.gameObject.name} 的 {arg1} 为 {intArg2}");
            }
        }

        private void SetObjectNames()
        {
            TroopsSkinCarEvent[] allObjects = null;
            if (root != null)
            {
                allObjects = root.GetComponentsInChildren<TroopsSkinCarEvent>(includeInactive);
            }
            else
            {
                allObjects = FindObjectsOfType<TroopsSkinCarEvent>(includeInactive);
            }

            if (allObjects == null)
            {
                Debug.LogWarning("未找到任何 TroopsSkinCarEvent 组件");
                return;
            }

            foreach (var o in allObjects)
            {
                o.transform.parent.name = o.m_DressId.ToString();
            }
        }

        private void CopyObjects()
        {
            foreach (var asset in _gameObjectFilterList.dataList)
            {
                for (int i = 0; i < count; i++)
                {
                    if (asset != null)
                    {
                        var newObj = Instantiate(asset);
                        newObj.transform.SetParent(asset.transform.parent);
                        newObj.name = asset.name + "_copy_" + (i + 1);
                        newObj.transform.position += arg3 * (i + 1);
                    }
                }
            }
        }

        private void ShowChildCount()
        {
            if (root == null)
            {
                Debug.LogError("请先指定节点");
                return;
            }

            Debug.LogWarning($"节点 {root.name} 的子节点数量: {root.childCount}");
        }

        private void ShowSelectedCount()
        {
            var selectedObjects = Selection.transforms;
            Debug.LogWarning($"选中节点数量: {selectedObjects.Length}");
        }

        private void SetSortingOrder()
        {
            if (root == null)
            {
                Debug.LogError("请先指定节点");
                return;
            }

            ParticleSystemRenderer[] particleRenderers =
                root.GetComponentsInChildren<ParticleSystemRenderer>(includeInactive);

            foreach (ParticleSystemRenderer renderer in particleRenderers)
            {
                renderer.sortingOrder = sortingOrder;
            }
        }

        private void AddSortingOrderBase()
        {
            if (root == null)
            {
                Debug.LogError("请先指定节点");
                return;
            }

            ParticleSystemRenderer[] particleRenderers =
                root.GetComponentsInChildren<ParticleSystemRenderer>(includeInactive);

            foreach (ParticleSystemRenderer renderer in particleRenderers)
            {
                int order = renderer.sortingOrder;
                renderer.sortingOrder += sortingOrder;
                Debug.Log($"设置 {renderer.gameObject.name} 的 Order 从 {order} 到 {renderer.sortingOrder}");
            }
        }

        private void ToggleSkinnedMeshRenderer(bool enable)
        {
            SkinnedMeshRenderer[] allObjects = null;
            if (root != null)
            {
                allObjects = root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
            }
            else
            {
                allObjects = FindObjectsOfType<SkinnedMeshRenderer>(includeInactive);
            }

            if (allObjects == null || allObjects.Length == 0)
            {
                Debug.LogWarning("未找到任何 SkinnedMeshRenderer 组件");
                return;
            }

            foreach (var renderer in allObjects)
            {
                if (!includeDisabled && !renderer.enabled)
                {
                    continue;
                }

                renderer.enabled = enable;
                Debug.Log($"{(enable ? "显示" : "隐藏")} {renderer.gameObject.name} 的 SkinnedMeshRenderer");
            }

            Debug.Log($"操作完成，共{(enable ? "显示" : "隐藏")}了 {allObjects.Length} 个 SkinnedMeshRenderer");
        }

        private void PrintMaterialCount()
        {
            Dictionary<Material, int> materialCount = new Dictionary<Material, int>();
            Renderer[] allRenderers = null;

            if (root != null)
            {
                allRenderers = root.GetComponentsInChildren<Renderer>(includeInactive);
            }
            else
            {
                allRenderers = FindObjectsOfType<Renderer>(includeInactive);
            }

            if (allRenderers == null || allRenderers.Length == 0)
            {
                Debug.LogWarning("未找到任何 Renderer 组件");
                return;
            }

            foreach (var renderer in allRenderers)
            {
                if (!includeDisabled && !renderer.enabled)
                {
                    continue;
                }

                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        if (materialCount.ContainsKey(material))
                        {
                            materialCount[material]++;
                        }
                        else
                        {
                            materialCount[material] = 1;
                        }
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== 材质球使用统计 ===");
            foreach (var kvp in materialCount.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"材质: {kvp.Key.name} - 使用次数: {kvp.Value}");
            }

            sb.AppendLine($"总计: {materialCount.Count} 种材质球，{materialCount.Values.Sum()} 次使用");

            Debug.Log(sb.ToString());
        }

        private void PrintSortingOrder()
        {
            if (root == null)
            {
                Debug.LogError("请先指定节点");
                return;
            }

            ParticleSystemRenderer[] particleRenderers =
                root.GetComponentsInChildren<ParticleSystemRenderer>(includeInactive);

            if (particleRenderers.Length == 0)
            {
                Debug.LogWarning("未找到任何 ParticleSystemRenderer 组件");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Particle System Sorting Order 信息 ===");

            foreach (ParticleSystemRenderer renderer in particleRenderers)
            {
                sb.AppendLine($"物体: {renderer.gameObject.name} - Sorting Order: {renderer.sortingOrder}");
            }

            Debug.Log(sb.ToString());
        }

        private void SetProfilerStatus()
        {
            // EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
            // ProfilerDriver.enabled = true;
            Tools.FindAndGetComponent<Camera>("UICam", false);
            Tools.FindAndGetComponent<Camera>("UICam", false);
            var go = GameObject.Find("world_root");
            if (go != null)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var t= go.transform.GetChild(i);
                    if (t.name != "LargeLand")
                    {
                        t.gameObject.SetActive(false);
                    }
                }
            }

            Tools.SetGameObject("Troops_root", false);
            Tools.SetGameObject("rss_root", false);
            Tools.SetGameObject("lod3_root", false);
            Tools.SetGameObject("CityRoot", false);
            Tools.SetGameObject("fogSystem", false);
            Tools.SetGameObject("BillBuffer", false);

            Tools.ToggleGameStats();
        }

        private void DrawTitle()
        {
            GUILayout.Label("EditorWindowTool 工具面板", titleStyle);

            // 绘制装饰线
            Rect titleRect = GUILayoutUtility.GetRect(1, 3);
            EditorGUI.DrawRect(titleRect, new Color(0.2f, 0.8f, 1.0f, 0.6f));

            GUILayout.Space(10);
        }
        bool ProcessFBXOnlyLog = false;
        void ProcessFBX(bool isRun)
        {
            List<string> assetList=FindFBX();
            Debug.Log(assetList.ToStr());
            StringBuilder sb= new StringBuilder();

            if (!ProcessFBXOnlyLog)
            {
                foreach (string assetPath in assetList)
                {
                    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                
                    if (importer != null)
                    {
                        // 修改设置
                        ModifyImporterSettings(importer,assetPath,isRun);
                    
                        // 保存并重新导入
                        importer.SaveAndReimport();
                        AssetDatabase.Refresh();
                    }
                }                
            }


            if (ProcessFBXOnlyLog)
            {
                foreach (string assetPath in assetList)
                {
                    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                
                    if (importer != null)
                    {
                        sb.AppendLine(assetPath);
                        var ls= Tools.GetAllChildrenPaths(assetPath);
                        sb.AppendLine(ls.ToStr(null,spacing:"\n"));
                    }
                }

                sb.AppendLine("------------------------------------");
                foreach (string assetPath in assetList)
                {
                    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                
                    if (importer != null)
                    {
                        sb.AppendLine(assetPath);
                        var ls= ExtractNodesFromModel(assetPath);
                        sb.AppendLine(ls.ToStr(null,spacing:"\n"));
                    }
                }
                Debug.Log(sb.ToString());           
                File.WriteAllText("./AUnityLocal/fbx.txt", sb.ToString());
            }

        }
        static void ModifyImporterSettings(ModelImporter importer,string assetPath,bool isRun)
        {
            if (!isRun)
            {
                if (importer.optimizeGameObjects)
                {
                    if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                    {
                        importer.optimizeGameObjects = false;
                        importer.extraExposedTransformPaths = new string[0];
                        Debug.Log("修复FBX：" + assetPath);
                    }
                }
                return;
            }
            
            if (importer.optimizeGameObjects)
            {
                return;
            }
            // 在这里设置你想要的导入参数
            importer.optimizeGameObjects = true;
            importer.extraExposedTransformPaths = ExtractNodesFromModel(assetPath).ToArray();

            // 添加更多你需要的设置...
        }
        static List<string>  ExtractNodesFromModel(string assetPath)
        {
            List<string> foundNodes = new List<string>();
            // 加载模型资源
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        
            if (modelPrefab == null)
            {
                Debug.LogWarning($"Could not load model at path: {assetPath}");
                return foundNodes;
            }

            Debug.Log($"=== Extracting nodes from: {assetPath} ===");
        
            // 遍历所有子物体
            Transform[] allTransforms = modelPrefab.GetComponentsInChildren<Transform>(true);


            var renderers = modelPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var rendererTransforms=renderers.Select((r) => r.transform).ToList();
            foreach (var skinnedMeshRenderer in renderers)
            {
                if (skinnedMeshRenderer.rootBone != null)
                {
                    rendererTransforms.Add(skinnedMeshRenderer.rootBone);
                }
            }
            
            foreach (Transform transform in allTransforms)
            {
                string nodeName = transform.name;
            
                // 检查是否包含ROOT或以TAG_开头
                if (nodeName.Contains("ROOT") || nodeName.StartsWith("TAG_")||rendererTransforms.Contains(transform))
                {
                    foundNodes.Add(Editor.Tools.GetRelativePath(transform,modelPrefab.transform));
                    Debug.Log($"Found node: {nodeName} (Path: {Tools.GetRelativePath(transform)})");
                }
            }
        
            if (foundNodes.Count == 0)
            {
                Debug.Log($"No matching nodes found in {assetPath}");
            }
            else
            {
                Debug.Log($"Total matching nodes found: {foundNodes.Count}");
            }
        
            
            Debug.Log(foundNodes.ToStr(null,spacing:"\n"));
            return foundNodes;
        }

        private List<string> FindFBX()
        {
            List<string> list = new List<string>();
            if (root == null)
            {
                Debug.LogWarning("请先选择一个GameObject");
                return new List<string>();
            }
            GameObject selectedObject = root.gameObject;
            Debug.Log($"=== 检查物体及其子物体: {selectedObject.name} ===");
        
            // 获取所有MeshFilter组件（包括子物体）
            MeshFilter[] meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                    list.Add(assetPath);
                    Debug.Log($"GameObject: {meshFilter.gameObject.name} - MeshFilter - Mesh: {meshFilter.sharedMesh.name}, Path: {assetPath}");
                }
            }

            // 获取所有SkinnedMeshRenderer组件（包括子物体）
            SkinnedMeshRenderer[] skinnedMeshRenderers = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
            {
                if (skinnedMeshRenderer.sharedMesh != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(skinnedMeshRenderer.sharedMesh);
                    list.Add(assetPath);
                    Debug.Log($"GameObject: {skinnedMeshRenderer.gameObject.name} - SkinnedMeshRenderer - Mesh: {skinnedMeshRenderer.sharedMesh.name}, Path: {assetPath}");
                }
            }

            return list;
        }

        bool ProcessPrefabOnlyLog = true;
        static void ProcessPrefab()
        {
            var list = Selection.objects;
        
            if (list == null)
            {
                Debug.LogWarning("Please select a prefab in the Project window");
                return;
            }

            foreach (var obj in list)
            {
                if(obj==null) continue;
                // Debug.Log(obj.name);
                AnalyzePrefabDependencies(obj as GameObject, true);
                // Debug.Log(PrefabUtility.IsPartOfPrefabAsset(obj));                      
            }
      
        }
   [System.Serializable]
    public class PrefabDependency
    {
        public GameObject prefab;
        public string prefabName;
        public string assetPath;
        public PrefabAssetType prefabType;
        public DependencyType dependencyType;
        public string componentName;
        public string propertyName;
        public Transform parentTransform;
        public int hierarchyDepth;
        
        public PrefabDependency(GameObject prefab, DependencyType depType, string component = "", string property = "", Transform parent = null, int depth = 0)
        {
            this.prefab = prefab;
            this.prefabName = prefab ? prefab.name : "Unknown";
            this.assetPath = prefab ? AssetDatabase.GetAssetPath(prefab) : "";
            this.prefabType = prefab ? PrefabUtility.GetPrefabAssetType(prefab) : PrefabAssetType.NotAPrefab;
            this.dependencyType = depType;
            this.componentName = component;
            this.propertyName = property;
            this.parentTransform = parent;
            this.hierarchyDepth = depth;
        }
    }
    
    public enum DependencyType
    {
        NestedPrefab,           // 嵌套的prefab
        ComponentReference,     // 组件引用的prefab
        ChildPrefabInstance,    // 子对象中的prefab实例
        PrefabVariant,          // prefab变体
        DirectReference        // 直接引用
    }
    
    /// <summary>
    /// 分析指定prefab的所有依赖关系
    /// </summary>
    /// <param name="targetPrefab">要分析的prefab</param>
    /// <param name="printToConsole">是否打印到控制台</param>
    /// <returns>依赖关系列表</returns>
    public static List<PrefabDependency> AnalyzePrefabDependencies(GameObject targetPrefab, bool printToConsole = true)
    {
        if (targetPrefab == null)
        {
            Debug.LogWarning("Target prefab is null");
            return new List<PrefabDependency>();
        }
        
        if (!PrefabUtility.IsPartOfPrefabAsset(targetPrefab))
        {
            Debug.LogWarning($"'{targetPrefab.name}' is not a prefab asset");
            return new List<PrefabDependency>();
        }
        
        var dependencies = new List<PrefabDependency>();
        
        Debug.Log($"Analyzing dependencies for prefab: {targetPrefab.name}");
        
        // 分析prefab内部的所有依赖
        AnalyzePrefabInternal(targetPrefab, targetPrefab.transform, 0, dependencies);
        
        // 移除重复项
        dependencies = RemoveDuplicateDependencies(dependencies);
        
        Debug.Log($"Found {dependencies.Count} prefab dependencies in {targetPrefab.name}");
        
        if (printToConsole)
        {
            PrintDependenciesToConsole(targetPrefab, dependencies);
        }
        
        return dependencies;
    }
    

    
    
    /// <summary>
    /// 批量分析多个prefab
    /// </summary>
    /// <param name="prefabs">要分析的prefab列表</param>
    /// <param name="printToConsole">是否打印到控制台</param>
    /// <returns>所有依赖关系的字典，key为prefab名称</returns>
    public static Dictionary<string, List<PrefabDependency>> AnalyzeMultiplePrefabs(GameObject[] prefabs, bool printToConsole = true)
    {
        var results = new Dictionary<string, List<PrefabDependency>>();
        
        foreach (var prefab in prefabs)
        {
            if (prefab != null)
            {
                var dependencies = AnalyzePrefabDependencies(prefab, false);
                results[prefab.name] = dependencies;
            }
        }
        
        if (printToConsole)
        {
            PrintMultiplePrefabsAnalysis(results);
        }
        
        return results;
    }
    
    /// <summary>
    /// 分析指定文件夹下的所有prefab
    /// </summary>
    /// <param name="folderPath">文件夹路径</param>
    /// <param name="recursive">是否递归搜索子文件夹</param>
    /// <param name="printToConsole">是否打印到控制台</param>
    /// <returns>所有依赖关系的字典</returns>
    public static Dictionary<string, List<PrefabDependency>> AnalyzePrefabsInFolder(string folderPath, bool recursive = true, bool printToConsole = true)
    {
        string searchPattern = recursive ? "t:GameObject" : "t:GameObject";
        string[] guids = AssetDatabase.FindAssets(searchPattern, new[] { folderPath });
        
        var prefabs = new List<GameObject>();
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            
            // 如果不递归，检查是否在直接路径下
            if (!recursive)
            {
                string directory = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
                if (directory != folderPath.TrimEnd('/'))
                    continue;
            }
            
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset != null && PrefabUtility.IsPartOfPrefabAsset(asset))
            {
                prefabs.Add(asset);
            }
        }
        
        Debug.Log($"Found {prefabs.Count} prefabs in folder: {folderPath}");
        
        return AnalyzeMultiplePrefabs(prefabs.ToArray(), printToConsole);
    }
    
    private static void AnalyzePrefabInternal(GameObject rootPrefab, Transform current, int depth, List<PrefabDependency> dependencies)
    {
        // 检查当前对象是否是嵌套的prefab
        if (current.gameObject != rootPrefab)
        {
            // 检查是否为prefab实例
            if (PrefabUtility.IsPartOfPrefabInstance(current.gameObject))
            {
                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject);
                if (prefabSource != null && prefabSource != rootPrefab)
                {
                    DependencyType depType = depth == 1 ? DependencyType.NestedPrefab : DependencyType.ChildPrefabInstance;
                    dependencies.Add(new PrefabDependency(
                        prefabSource, 
                        depType, 
                        "GameObject", 
                        "Prefab Instance", 
                        current,
                        depth
                    ));
                }
            }
            // 检查是否为prefab变体
            else if (PrefabUtility.GetPrefabAssetType(current.gameObject) == PrefabAssetType.Variant)
            {
                GameObject variantSource = PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject);
                if (variantSource != null)
                {
                    dependencies.Add(new PrefabDependency(
                        variantSource,
                        DependencyType.PrefabVariant,
                        "GameObject",
                        "Variant Source",
                        current,
                        depth
                    ));
                }
            }
        }
        
        // 分析组件中的prefab引用
        Component[] components = current.GetComponents<Component>();
        foreach (Component comp in components)
        {
            if (comp == null) continue;
            AnalyzeComponentForPrefabReferences(comp, current, depth, dependencies);
        }
        
        // 递归检查子对象
        for (int i = 0; i < current.childCount; i++)
        {
            AnalyzePrefabInternal(rootPrefab, current.GetChild(i), depth + 1, dependencies);
        }
    }
    
    private static void AnalyzeComponentForPrefabReferences(Component component, Transform parent, int depth, List<PrefabDependency> dependencies)
    {
        SerializedObject serializedObject = new SerializedObject(component);
        SerializedProperty property = serializedObject.GetIterator();
        
        while (property.NextVisible(true))
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (property.objectReferenceValue is GameObject go)
                {
                    // 检查是否为prefab资源
                    if (PrefabUtility.IsPartOfPrefabAsset(go))
                    {
                        dependencies.Add(new PrefabDependency(
                            go,
                            DependencyType.ComponentReference,
                            component.GetType().Name,
                            property.displayName,
                            parent,
                            depth
                        ));
                    }
                }
            }
        }
    }
    
    private static List<PrefabDependency> RemoveDuplicateDependencies(List<PrefabDependency> dependencies)
    {
        var uniqueDependencies = new List<PrefabDependency>();
        var seenPrefabs = new HashSet<string>();
        
        foreach (var dependency in dependencies)
        {
            string key = $"{dependency.assetPath}_{dependency.dependencyType}_{dependency.componentName}_{dependency.propertyName}";
            if (!seenPrefabs.Contains(key))
            {
                seenPrefabs.Add(key);
                uniqueDependencies.Add(dependency);
            }
        }
        
        return uniqueDependencies;
    }
    
    private static void PrintDependenciesToConsole(GameObject targetPrefab, List<PrefabDependency> dependencies)
    {
        if (targetPrefab == null || dependencies.Count == 0) return;
        
        Debug.Log($"=== Prefab Dependencies Analysis for '{targetPrefab.name}' ===");
        Debug.Log($"Total Dependencies Found: {dependencies.Count}");
        
        // 按类型分组统计
        var typeGroups = dependencies.GroupBy(d => d.dependencyType);
        Debug.Log("\n=== Summary by Dependency Type ===");
        foreach (var group in typeGroups)
        {
            Debug.Log($"{group.Key}: {group.Count()} dependencies");
        }
        
        // 按Prefab类型分组统计
        var prefabTypeGroups = dependencies.GroupBy(d => d.prefabType);
        Debug.Log("\n=== Summary by Prefab Type ===");
        foreach (var group in prefabTypeGroups)
        {
            Debug.Log($"{group.Key}: {group.Count()} prefabs");
        }
        
        // 详细列表
        Debug.Log("\n=== Detailed Dependencies List ===");
        var groupedDependencies = dependencies
            .GroupBy(d => d.dependencyType)
            .OrderBy(g => g.Key.ToString());
        
        foreach (var group in groupedDependencies)
        {
            Debug.Log($"\n--- {group.Key} Dependencies ---");
            foreach (var dependency in group.OrderBy(d => d.prefabName))
            {
                string logMessage = $"• {dependency.prefabName} ({dependency.prefabType})";
                
                if (!string.IsNullOrEmpty(dependency.componentName))
                    logMessage += $" | Component: {dependency.componentName}";
                
                if (!string.IsNullOrEmpty(dependency.propertyName))
                    logMessage += $" | Property: {dependency.propertyName}";
                
                if (dependency.hierarchyDepth > 0)
                    logMessage += $" | Depth: {dependency.hierarchyDepth}";
                
                logMessage += $" | Path: {dependency.assetPath}";
                
                Debug.Log(logMessage);
            }
        }
        
        Debug.Log("=== End of Dependencies Analysis ===");
    }
    
    private static void PrintMultiplePrefabsAnalysis(Dictionary<string, List<PrefabDependency>> results)
    {
        Debug.Log($"=== Multiple Prefabs Dependencies Analysis ===");
        Debug.Log($"Analyzed {results.Count} prefabs");
        
        int totalDependencies = results.Values.Sum(deps => deps.Count);
        Debug.Log($"Total Dependencies Found: {totalDependencies}");
        
        foreach (var kvp in results.OrderBy(r => r.Key))
        {
            Debug.Log($"\n--- {kvp.Key} ({kvp.Value.Count} dependencies) ---");
            
            if (kvp.Value.Count > 0)
            {
                var typeGroups = kvp.Value.GroupBy(d => d.dependencyType);
                foreach (var group in typeGroups)
                {
                    Debug.Log($"  {group.Key}: {group.Count()}");
                    foreach (var dep in group.Take(3)) // 只显示前3个
                    {
                        Debug.Log($"    • {dep.prefabName} ({dep.prefabType})");
                    }
                    if (group.Count() > 3)
                    {
                        Debug.Log($"    ... and {group.Count() - 3} more");
                    }
                }
            }
        }
        
        Debug.Log("=== End of Multiple Prefabs Analysis ===");
    }

    public static void RevertSpecificPrefabAnimator()
    {
        var objs=Selection.objects;

        foreach (var obj in objs)
        {
            if (obj == null)
            {
                continue;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                continue;
            }
            string prefabPath = AssetDatabase.GetAssetPath(obj);
            // RevertSpecificPrefabAnimator(prefabPath);
            RevertAddedPrefabAnimator(prefabPath);
        }

        
    }
    public static void RevertSpecificPrefabAnimator(string prefabPath)
    {

        // 打开Prefab进行编辑
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            
            // 查找所有嵌套的Prefab实例
            var allTransforms = prefabRoot.GetComponentsInChildren<Transform>(true);
            
            foreach (var transform in allTransforms)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject))
                {
                    // 获取原始Prefab的路径
                    var originalPrefab = PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject);
                    
                    if (originalPrefab != null)
                    {
                        var originalAnimator = originalPrefab.GetComponent<Animator>();
                        var currentAnimator = transform.GetComponent<Animator>();
                        
                        // 如果原始Prefab有Animator但当前实例的Animator被修改了
                        if (originalAnimator != null && currentAnimator != null)
                        {
                            // 检查是否有覆盖
                            var overrides = new System.Collections.Generic.List<ObjectOverride>();
                            overrides=PrefabUtility.GetObjectOverrides(transform.gameObject);
                            var removedComponents = PrefabUtility.GetRemovedComponents(transform.gameObject);
                            var animatorOverride = overrides.FirstOrDefault(o => o.instanceObject == currentAnimator);
                            
                            if (animatorOverride.instanceObject != null)
                            {
                                // 恢复到原始状态
                                PrefabUtility.RevertObjectOverride(currentAnimator, InteractionMode.AutomatedAction);
                                Debug.Log($"Successfully reverted Animator on {transform.name}");
                            }
                        }
                    }
                }
            }
        }
        
        // 刷新资源
        AssetDatabase.Refresh();
        Debug.Log("Prefab Animator revert completed!");
    }    
    public static void RevertAddedPrefabAnimator(string prefabPath)
    {
        RuntimeAnimatorController controller = null;
        
        // 打开Prefab进行编辑
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            // 查找所有嵌套的Prefab实例
            var allTransforms = prefabRoot.GetComponentsInChildren<Transform>(true).Where((a)=>a.GetComponent<Animator>()!=null);
            
            foreach (var transform in allTransforms)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject))
                {
                    controller= transform.GetComponent<Animator>().runtimeAnimatorController;
                    ProcessNestedPrefabAnimator(transform.gameObject);
                }
            }
        }
        // 打开Prefab进行编辑
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            
            // 查找所有嵌套的Prefab实例
            var ani = prefabRoot.GetComponentInChildren<Animator>(true);
            if (ani != null)
            {
                if (ani.runtimeAnimatorController == null)
                {
                    ani.runtimeAnimatorController=controller;
                    EditorUtility.SetDirty(ani);
                    Debug.LogWarning($"Animator runtimeAnimatorController 同步设置成功>>{controller!=null}");
                }
                else
                {
                    Debug.LogWarning($"Animator runtimeAnimatorController  设置失败，原始值存在 同步>>{controller!=null}");
                }
            }            
            
        }        
        // AssetDatabase.SaveAssets();
        // 刷新资源
        AssetDatabase.Refresh();
        
        Debug.Log("Added Prefab Animator revert completed!");
    }

    private static void ProcessNestedPrefabAnimator(GameObject nestedPrefabInstance)
    {
        // 获取原始Prefab
        var originalPrefab = PrefabUtility.GetCorrespondingObjectFromSource(nestedPrefabInstance);
        
        if (originalPrefab == null) return;

        // 检查原始Prefab是否有Animator组件
        var originalAnimator = originalPrefab.GetComponent<Animator>();
        var currentAnimator = nestedPrefabInstance.GetComponent<Animator>();

        // 情况1: 原始Prefab没有Animator，但实例中添加了Animator
        if (originalAnimator == null && currentAnimator != null)
        {
            Debug.Log($"Found added Animator component on {nestedPrefabInstance.name}");
            
            // 检查这个Animator是否是添加的组件覆盖
            if (IsAddedComponent(nestedPrefabInstance, currentAnimator))
            {
                // 删除添加的Animator组件
                Object.DestroyImmediate(currentAnimator);
                EditorUtility.SetDirty(nestedPrefabInstance);
                Debug.Log($"Removed added Animator from {nestedPrefabInstance.name}");
            }
        }
        // 情况2: 原始Prefab有Animator，但实例中的Animator是新添加的（原来的被删除了）
        else if (originalAnimator != null && currentAnimator != null)
        {
            // 检查当前Animator是否是添加的组件
            if (IsAddedComponent(nestedPrefabInstance, currentAnimator))
            {
                // 删除添加的Animator
                Object.DestroyImmediate(currentAnimator);
                Debug.Log($"Removed added Animator from {nestedPrefabInstance.name}");
                
                // 恢复原始的Animator组件
                RestoreOriginalAnimator(nestedPrefabInstance, originalAnimator);
                EditorUtility.SetDirty(nestedPrefabInstance);
            }
        }
        // 情况3: 原始Prefab有Animator，但实例中没有（被删除了）
        else if (originalAnimator != null && currentAnimator == null)
        {
            // 检查是否有删除的组件覆盖
            if (IsRemovedComponent(nestedPrefabInstance, typeof(Animator)))
            {
                // 恢复被删除的Animator组件
                RestoreOriginalAnimator(nestedPrefabInstance, originalAnimator);
            }
        }
    }

    /// <summary>
    /// 检查组件是否是添加的覆盖
    /// </summary>
    private static bool IsAddedComponent(GameObject prefabInstance, Component component)
    {
        // 获取添加的组件覆盖
        var addedComponents = PrefabUtility.GetAddedComponents(prefabInstance);
        
        return addedComponents.Any(addedComp => addedComp.instanceComponent == component);
    }

    /// <summary>
    /// 检查是否有被删除的组件
    /// </summary>
    private static bool IsRemovedComponent(GameObject prefabInstance, System.Type componentType)
    {
        // 获取删除的组件覆盖
        var removedComponents = PrefabUtility.GetRemovedComponents(prefabInstance);
        
        return removedComponents.Any(removedComp => 
            removedComp.assetComponent != null && 
            removedComp.assetComponent.GetType() == componentType);
    }

    /// <summary>
    /// 恢复原始的Animator组件
    /// </summary>
    private static void RestoreOriginalAnimator(GameObject prefabInstance, Animator originalAnimator)
    {
        // 方法1: 尝试恢复删除的组件
        var removedComponents = PrefabUtility.GetRemovedComponents(prefabInstance);
        var removedAnimator = removedComponents.FirstOrDefault(rc => 
            rc.assetComponent is Animator);

        if (removedAnimator.assetComponent != null)
        {
            // 恢复删除的组件
            PrefabUtility.RevertRemovedComponent(prefabInstance, removedAnimator.assetComponent, InteractionMode.AutomatedAction);
            Debug.Log($"Restored removed Animator component on {prefabInstance.name}");
        }
        else
        {
            // 方法2: 手动添加并复制属性
            var newAnimator = prefabInstance.AddComponent<Animator>();
            CopyAnimatorProperties(originalAnimator, newAnimator);
            Debug.Log($"Manually restored Animator component on {prefabInstance.name}");
        }
    }

    /// <summary>
    /// 复制Animator属性
    /// </summary>
    private static void CopyAnimatorProperties(Animator source, Animator target)
    {
        if (source == null || target == null) return;

        target.runtimeAnimatorController = source.runtimeAnimatorController;
        target.avatar = source.avatar;
        target.applyRootMotion = source.applyRootMotion;
        target.updateMode = source.updateMode;
        target.cullingMode = source.cullingMode;
    }

    /// <summary>
    /// 更全面的处理方法 - 处理所有Animator相关的覆盖
    /// </summary>
    [MenuItem("Tools/Advanced/Comprehensive Animator Revert")]
    public static void ComprehensiveAnimatorRevert()
    {
        GameObject selectedPrefab = Selection.activeGameObject;
        
        if (selectedPrefab == null || !PrefabUtility.IsPartOfPrefabAsset(selectedPrefab))
        {
            Debug.LogError("Please select a prefab asset!");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
        
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var prefabRoot = editingScope.prefabContentsRoot;
            var allGameObjects = prefabRoot.GetComponentsInChildren<Transform>(true)
                .Select(t => t.gameObject).ToArray();
            
            foreach (var go in allGameObjects)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    ProcessAllAnimatorOverrides(go);
                }
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Comprehensive Animator revert completed!");
    }

    private static void ProcessAllAnimatorOverrides(GameObject prefabInstance)
    {
        // 1. 处理添加的Animator组件
        var addedComponents = PrefabUtility.GetAddedComponents(prefabInstance);
        foreach (var addedComp in addedComponents)
        {
            if (addedComp.instanceComponent is Animator)
            {
                Debug.Log($"Reverting added Animator on {prefabInstance.name}");
                PrefabUtility.RevertAddedComponent(addedComp.instanceComponent, InteractionMode.AutomatedAction);
            }
        }

        // 2. 处理删除的Animator组件
        var removedComponents = PrefabUtility.GetRemovedComponents(prefabInstance);
        foreach (var removedComp in removedComponents)
        {
            if (removedComp.assetComponent is Animator)
            {
                Debug.Log($"Reverting removed Animator on {prefabInstance.name}");
                PrefabUtility.RevertRemovedComponent(prefabInstance, removedComp.assetComponent, InteractionMode.AutomatedAction);
            }
        }

        // 3. 处理修改的Animator组件
        var objectOverrides = PrefabUtility.GetObjectOverrides(prefabInstance);
        foreach (var overrideInfo in objectOverrides)
        {
            if (overrideInfo.instanceObject is Animator)
            {
                Debug.Log($"Reverting modified Animator on {prefabInstance.name}");
                PrefabUtility.RevertObjectOverride(overrideInfo.instanceObject, InteractionMode.AutomatedAction);
            }
        }
    }

    /// <summary>
    /// 批量处理多个Prefab的Animator覆盖
    /// </summary>
    [MenuItem("Tools/Advanced/Batch Revert All Animator Overrides")]
    public static void BatchRevertAllAnimatorOverrides()
    {
        if (!EditorUtility.DisplayDialog("Batch Revert", 
            "This will revert ALL Animator overrides in ALL prefabs. Continue?", 
            "Yes", "Cancel"))
        {
            return;
        }

        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        int processedCount = 0;
        
        try
        {
            for (int i = 0; i < prefabGUIDs.Length; i++)
            {
                string guid = prefabGUIDs[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                EditorUtility.DisplayProgressBar("Processing Prefabs", 
                    $"Processing {System.IO.Path.GetFileName(path)}", 
                    (float)i / prefabGUIDs.Length);
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null && HasAnimatorOverrides(prefab))
                {
                    using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
                    {
                        var allGameObjects = editingScope.prefabContentsRoot
                            .GetComponentsInChildren<Transform>(true)
                            .Select(t => t.gameObject).ToArray();
                        
                        foreach (var go in allGameObjects)
                        {
                            if (PrefabUtility.IsPartOfPrefabInstance(go))
                            {
                                ProcessAllAnimatorOverrides(go);
                            }
                        }
                    }
                    processedCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"Batch processing completed! Processed {processedCount} prefabs.");
    }

    private static bool HasAnimatorOverrides(GameObject prefab)
    {
        // 简化的检查逻辑，实际使用时可以更精确
        var allGameObjects = prefab.GetComponentsInChildren<Transform>(true)
            .Select(t => t.gameObject).ToArray();
        
        foreach (var go in allGameObjects)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                var addedComponents = PrefabUtility.GetAddedComponents(go);
                var removedComponents = PrefabUtility.GetRemovedComponents(go);
                var objectOverrides = PrefabUtility.GetObjectOverrides(go);
                
                if (addedComponents.Any(ac => ac.instanceComponent is Animator) ||
                    removedComponents.Any(rc => rc.assetComponent is Animator) ||
                    objectOverrides.Any(oo => oo.instanceObject is Animator))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    }
}