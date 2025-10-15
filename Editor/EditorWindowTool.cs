using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using FxProNS;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
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


    }
}