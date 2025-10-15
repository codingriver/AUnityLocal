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

        [MenuItem("AUnityLocal/EditorWindowTool",false,1000)]
        public static void ShowWindow()
        {
            var window = GetWindow<EditorWindowTool>("EditorWindowTool 工具");
            window.minSize = new Vector2(800, 700); // 增大默认高度
            window.maxSize = new Vector2(1200, 1000);
        }
        

        private string arg1 = "state";
        private string arg2 = "2";
        private Vector3 arg3= new Vector3(1,0,0);
        private int count=20;
        private Transform root= null;
        private List<GameObject> objs = new List<GameObject>();
        bool includeInactive = true;
        bool includeDisabled = true;
        private int sortingOrder=5300;
        private ReorderableList<GameObject> _gameObjectFilterList = null;
        void OnEnable()
        {
            _gameObjectFilterList = new ReorderableList<GameObject>("GameObjects");
        }        
        private void OnGUI()
        {
            DrawTitle();

            // EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f + 10));
            EditorGUILayout.BeginVertical();
            root = (Transform)EditorGUILayout.ObjectField("根节点:", root, typeof(Transform), true);
            arg1= EditorGUILayout.TextField("参数1:", arg1);
            arg2= EditorGUILayout.TextField("参数2:", arg2);
            arg3= EditorGUILayout.Vector3Field("参数3:", arg3);
            count= EditorGUILayout.IntField("复制数量:", count);
            includeInactive = EditorGUILayout.Toggle("包含未激活对象", includeInactive);
            includeDisabled = EditorGUILayout.Toggle("包含未启用组件", includeDisabled);
            if (GUILayout.Button("设置动画参数"))
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
                int intArg2=0;
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
                    com.SetInteger(arg1,intArg2 );
                    Debug.Log($"设置 {com.gameObject.name} 的 {arg1} 为 true");
                }
            }

            if (GUILayout.Button("设置物体名字"))
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
                    Debug.LogWarning("未找到任何 Animator 组件");
                    return;
                }

                foreach (var o in allObjects)
                {
                    o.transform.parent.name=o.m_DressId.ToString();
                }
                
            }

            GUILayout.Space(10);
            _gameObjectFilterList?.DoLayoutList();
            // 简单使用
            if (GUILayout.Button("复制物体"))
            {
                var _objs = _gameObjectFilterList.dataList;
                foreach (var asset in _objs)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (asset != null)
                        {
                            
                            var newObj = Instantiate(asset);
                            newObj.transform.SetParent(asset.transform.parent);
                            newObj.name = asset.name + "_copy" + (i + 1);
                            newObj.transform.position += arg3 * (i + 1);                            
                        }

                    }                    
                }

                
            }
            if (GUILayout.Button("显示子节点数量"))
            {
                if (root == null)
                {
                    Debug.LogError("请先指定节点");
                    return;
                }
                Debug.LogWarning($"节点 {root.name} 的子节点数量: {root.childCount}");
            }
            if (GUILayout.Button("显示选中节点数量"))
            {
                var selectedObjects = Selection.transforms;
                Debug.LogWarning($"选中节点数量: {selectedObjects.Length}");
            }            
            if (GUILayout.Button("设置Order"))
            {
                if (root == null)
                {
                    Debug.LogError("请先指定节点");
                    return;
                }
                ParticleSystemRenderer[] particleRenderers = root.GetComponentsInChildren<ParticleSystemRenderer>(includeInactive);
        
                foreach (ParticleSystemRenderer renderer in particleRenderers)
                {

                    renderer.sortingOrder = sortingOrder;
                }                
                
            }   
            sortingOrder= EditorGUILayout.IntField("设置叠加Order基数:", sortingOrder);
            if (GUILayout.Button("设置Order基数"))
            {
                if (root == null)
                {
                    Debug.LogError("请先指定节点");
                    return;
                }
                ParticleSystemRenderer[] particleRenderers = root.GetComponentsInChildren<ParticleSystemRenderer>(includeInactive);
        
                foreach (ParticleSystemRenderer renderer in particleRenderers)
                {

                    int order = renderer.sortingOrder;
                    renderer.sortingOrder += sortingOrder;
                    Debug.Log($"设置 {renderer.gameObject.name} 的 Order 从 {order} 到 {renderer.sortingOrder}");
                }                
                
                
            }
            if (GUILayout.Button("打印Path相对根节点"))
            {
                
                var selectedObjects = Selection.transforms;
                StringBuilder sb = new StringBuilder();
                foreach (Transform transform in selectedObjects)
                {
                    if (transform.IsChildOf(root))
                    {
                        string relativePath = GetRelativePath(transform, root);
                        sb.AppendLine(relativePath);
                        Debug.Log($"选中节点 {transform.name} 相对于根节点 {root.name} 的路径: {relativePath}");
                    }
                    else 
                    {
                        string relativePath = GetRelativePath(transform, null);
                        sb.AppendLine(relativePath);
                        Debug.LogWarning($"选中节点 {transform.name} 不是根节点 {root.name} 的子节点");
                    }
                }
                Debug.Log(sb.ToString());

            }

            if (GUILayout.Button("隐藏SkinnedMeshRenderer"))
            {
                if (root == null)
                {
                    Debug.LogError("请先指定节点");
                    return;
                }
                SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
                foreach (SkinnedMeshRenderer renderer in renderers)
                {
                    renderer.enabled = false;
                }
            }

            if (GUILayout.Button("显示SkinnedMeshRenderer"))
            {
                if (root == null)
                {
                    Debug.LogError("请先指定节点");
                    return;
                }

                SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
                foreach (SkinnedMeshRenderer renderer in renderers)
                {
                    renderer.enabled = true;
                }
            }
            if (GUILayout.Button("打印SortingOrder"))
            {
                if (root == null)
                {
                    Debug.LogError("请先指定节点");
                    return;
                }
                StringBuilder sb= new StringBuilder();
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
                foreach (var renderer in renderers)
                {
                    sb.AppendLine($" SortingOrder:{renderer.sortingOrder}, sortingLayerName:{renderer.sortingLayerName},物体 {GetRelativePath(renderer.transform,root)}");
                }
                Debug.Log(sb.ToString());
            }
            if (GUILayout.Button("打印材质球数量"))
            {
                if (root == null)
                {
                    Debug.LogError("请先指定节点");
                    return;
                }
                StringBuilder sb= new StringBuilder();
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
                int count = 0;
                HashSet<Material> uniqueMaterials = new HashSet<Material>();
                foreach (var renderer in renderers)
                {
                    sb.AppendLine($" sharedMaterials Count:{renderer.sharedMaterials.Length},物体 {GetRelativePath(renderer.transform,root)}");
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            uniqueMaterials.Add(material);
                        }
                    }
                    count+= renderer.sharedMaterials.Length;
                }

                sb.Insert(0, $"总材质球数量: {uniqueMaterials.Count},引用次数: {count}\n");
                Debug.Log(sb.ToString());
            }

            if (GUILayout.Button("开启分析状态"))
            {
                SetProfilerStatus();
            }
            
            EditorGUILayout.EndVertical();

        }
        StringBuilder _relativePath = new StringBuilder();
        string GetRelativePath(Transform target, Transform root=null)
        {
            if (target == root)
                return target.name;

            Stack<string> pathStack = new Stack<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                pathStack.Push(current.name);
                current = current.parent;
            }

            StringBuilder relativePath =_relativePath;
            relativePath.Clear();
            while (pathStack.Count > 0)
            {
                relativePath.Append(pathStack.Pop());
                if (pathStack.Count > 0)
                    relativePath.Append("/");
            }

            return relativePath.ToString();
        }
        
        
        

        private void DrawTitle()
        {
            GUILayout.Label("EditorWindowTool 工具", new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                margin = new RectOffset(0, 0, 10, 15),
                normal = { textColor = new Color(0.9f, 1.0f, 1.0f) } // 亮青色标题
            });

            // 添加标题分隔线
            Rect separatorRect = GUILayoutUtility.GetRect(1, 1);
            EditorGUI.DrawRect(separatorRect, new Color(0.3f, 0.4f, 0.5f));
        }
        
        ///打开性能分析状态
        void SetProfilerStatus()
        {
            // EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
            // ProfilerDriver.enabled = true;
            FindAndGetComponent<Camera>("UICam", false);
            FindAndGetComponent<Camera>("UICam", false);
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

            FindGameObject("Troops_root", false);
            FindGameObject("rss_root", false);
            FindGameObject("lod3_root", false);
            FindGameObject("CityRoot", false);
            FindGameObject("fogSystem", false);
            FindGameObject("BillBuffer", false);

            ToggleGameStats();
        }

        T FindAndGetComponent<T>(string name,bool enable = true) where T : Behaviour
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                var com = go.GetComponent<T>();
                if (com != null)
                {
                    com.enabled = enable;
                    return com;
                }
            }
            return null;
        }
        void FindGameObject(string name,bool active = true)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                go.SetActive(active);
            }
         
        }
        static void ToggleGameStats()
        {
            var gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var gameWindow = EditorWindow.GetWindow(gameViewType);
        
            // 获取showStats字段
            var showStatsField = gameViewType.GetField("m_Stats", 
                BindingFlags.NonPublic | BindingFlags.Instance);
        
            if (showStatsField != null)
            {
                bool currentValue = (bool)showStatsField.GetValue(gameWindow);
                // showStatsField.SetValue(gameWindow, !currentValue);
                showStatsField.SetValue(gameWindow, true);
            }
        
            gameWindow.Repaint();
        }
    }
}