using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Skyunion;
using Data;
namespace AUnityLocal.Editor
{
    
    /// <summary>
    /// 模板
    /// </summary>
    [WindowToolGroup(500,WindowArea.Left)]
    public class WindowToolGroupT : WindowToolGroup
    {
        public override string title { get; } = "";
        public override string tip { get; } = "";
        
        public override void OnGUI(Rect contentRect)
        {

        }
        
    }

    
    public abstract class WindowToolGroup
    {
        
        public static List<Object> shareObjectList = new List<Object>();
        public static List<string> shareStringList = new List<string>();

        public static void SetData(List<Object> _objectList)
        {
            shareObjectList = _objectList;
        }

        public static void SetData(List<string> _stringList)
        {
            shareStringList = _stringList;
        }

        public static void Clear()
        {
            shareObjectList.Clear();
            shareStringList.Clear();
        }        
        public abstract string title { get; }
        public virtual string tip { get; }=string.Empty;
        public virtual bool Show { get; }=true;
        
        public static GUIStyle titleStyle;
        public static GUIStyle sectionHeaderStyle;
        public static GUIStyle buttonStyle;
        public static GUIStyle boxStyle;
        public static GUIStyle fieldStyle;
        
        private Vector2 scrollPosition;        
        public const int widthMin = 80;
        public const int widthMid = 120;
        public const int widthMax = 160;
        public const int heightMin = 20;
        public const int heightMid = 25;
        public const int heightMax = 32;
        public static WindowToolEditor window=null;
        
        public abstract void OnGUI(Rect contentRect);
        public  bool DrawButton(string text, string tooltip, Color? color = null, params GUILayoutOption[] options)
        {
            var originalColor = GUI.backgroundColor;
            if (color.HasValue)
                GUI.backgroundColor = color.Value;

            var content = string.IsNullOrEmpty(tooltip) ? new GUIContent(text) : new GUIContent(text, tooltip);
            bool result = GUILayout.Button(content, buttonStyle, options);

            GUI.backgroundColor = originalColor;
            return result;
        }
        public  bool DrawButton(string text, Color? color = null, params GUILayoutOption[] options)
        {
            var originalColor = GUI.backgroundColor;
            if (color.HasValue)
                GUI.backgroundColor = color.Value;
            bool result = GUILayout.Button(new GUIContent(text), buttonStyle, options);

            GUI.backgroundColor = originalColor;
            return result;
        }              
        public  bool DrawButton(string text, int width,Color? color = null)
        {
            var originalColor = GUI.backgroundColor;
            if (color.HasValue)
                GUI.backgroundColor = color.Value;
            bool result = GUILayout.Button(new GUIContent(text), buttonStyle,GUILayout.Width(width));
            GUI.backgroundColor = originalColor;
            return result;
        }        
        public bool DrawButton(string text, int width,int height, Color? color = null)
        {
            var originalColor = GUI.backgroundColor;
            if (color.HasValue)
                GUI.backgroundColor = color.Value;
            bool result = GUILayout.Button(new GUIContent(text), buttonStyle,GUILayout.Width(width),GUILayout.Height(height));
            GUI.backgroundColor = originalColor;
            return result;
        }             
        
        
        public static void InitializeStyles()
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
                fixedHeight = heightMid,
                margin = new RectOffset(5, 5, 3, 3),
                padding = new RectOffset(2, 5, 2, 5)
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
            InitializeStyles2();
        }     
        // private GUIStyle sectionHeaderStyle;
        public static GUIStyle searchButtonStyle;
        public static GUIStyle searchButtonStyle1;
        public static GUIStyle resultCountStyle;
        public static GUIStyle separatorStyle;
        // 按钮动画计时器
        public static float buttonPulseTimer = 0f;
        public static float buttonPulseDuration = 0.15f;
        public static int originalButtonFontSize = 11;

        // 状态颜色过渡
        public static float statusColorTransitionTimer = 0f;
        public static float statusColorTransitionDuration = 0.3f;
        public static Color statusStartColor = Color.green * 0.5f;
        public static Color statusTargetColor = Color.green;
        public  static GUIStyle tabButtonStyle;
        public  static GUIStyle activeTabButtonStyle;
        private static void InitializeStyles2()
        {
            // // 主标题样式
            // sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            // {
            //     fontSize = 14,
            //     margin = new RectOffset(0, 0, 10, 5),
            //     padding = new RectOffset(8, 8, 5, 5),
            //     normal = { textColor = new Color(0.8f, 0.9f, 1.0f) } // 浅蓝色文本
            // };

            // 按钮样式
            searchButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = originalButtonFontSize,
                fontStyle = FontStyle.Bold,
                fixedHeight = 28,
            };
            searchButtonStyle1 = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = originalButtonFontSize,
                fontStyle = FontStyle.Bold,
                fixedHeight = 18,
            };

            // 标签页按钮样式
            tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 25,
                margin = new RectOffset(2, 2, 0, 0)
            };

            // 激活标签页按钮样式
            activeTabButtonStyle = new GUIStyle(tabButtonStyle)
            {
                normal = { textColor = Color.white },
                onNormal = { textColor = Color.white },
                fontStyle = FontStyle.Bold,
                // backgroundColor = new Color(0.2f, 0.4f, 0.6f)
            };

            // 结果计数样式
            resultCountStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 0.9f, 1.0f) } // 亮蓝色文本
            };

            // 分隔线样式
            separatorStyle = new GUIStyle
            {
                margin = new RectOffset(0, 0, 1, 8)
            };
        }        
        
        // 按区域分组存储
                
        public static Dictionary<WindowArea, List<WindowToolGroup>> InitializeGroups()
        {
            Dictionary<WindowArea, List<WindowToolGroup>> areaGroups = new Dictionary<WindowArea, List<WindowToolGroup>>();

            // 初始化字典
            foreach (WindowArea area in Enum.GetValues(typeof(WindowArea)))
            {
                areaGroups[area] = new List<WindowToolGroup>();
            }
        
            // 获取所有继承类
            var derivedTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<WindowToolGroup>();
        
            var groupInfos = new List<(WindowToolGroup instance, WindowArea area, int order)>();
        
            foreach (Type type in derivedTypes)
            {
                if (!type.IsAbstract && type.IsClass)
                {
                    try
                    {
                        WindowToolGroup instance = (WindowToolGroup)Activator.CreateInstance(type);
                    
                        // 获取特性信息
                        var attr = type.GetCustomAttributes(typeof(WindowToolGroupAttribute), false)
                            .FirstOrDefault() as WindowToolGroupAttribute;
                    
                        WindowArea area = attr?.Area ?? WindowArea.Left;
                        int order = attr?.Order ?? 0;
                    
                        groupInfos.Add((instance, area, order));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to create instance of {type.Name}: {e.Message}");
                    }
                }
            }
        
            // 按区域和顺序分组
            foreach (var (instance, area, order) in groupInfos.OrderBy(x => x.order))
            {
                areaGroups[area].Add(instance);
            }
        
            Debug.Log($"Initialized {groupInfos.Count} groups across {areaGroups.Count} areas");
            return areaGroups;
        }
        
        
        public virtual void SaveLog(StringBuilder logBuilder,string fileName)
        {
            if (logBuilder.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有日志内容可导出", "确定");
                return;
            }
            string logFilePath = EditorUtility.SaveFilePanel("保存日志文件", "", fileName, "txt");
        
            if (!string.IsNullOrEmpty(logFilePath))
            {
                try
                {
                    File.WriteAllText(logFilePath, logBuilder.ToString());
                    EditorUtility.DisplayDialog("成功", $"日志已保存到: {logFilePath}", "确定");
                    window.SetStatusInfo($"日志已保存到: {logFilePath}");
                    // 询问是否打开文件
                    if (EditorUtility.DisplayDialog("打开文件", "是否要打开日志文件？", "是", "否"))
                    {
                        System.Diagnostics.Process.Start(logFilePath);
                    }
                    // // 询问是否打开文件
                    // if (EditorUtility.DisplayDialog("打开文件", "是否要打开导出的日志文件？", "是", "否"))
                    // {
                    //     Application.OpenURL("file://" + filePath);
                    // }                    
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("错误", $"保存日志文件失败: {e.Message}", "确定");
                }
            }
        }
        
        public virtual void OnDestroy()
        {
            
        }

    }
    /// <summary>
    /// 其他工具
    /// </summary>
    [WindowToolGroup( 500)]
    public class WindowToolGroupTestOther : WindowToolGroup
    {
        public override string title { get; } = "WindowToolGroupTestOther";
        public override string tip { get; } = "";
        
        public override void OnGUI(Rect contentRect)
        {
            if (DrawButton("Click Me", widthMin,Color.cyan))
            {
                Debug.Log("Button Clicked!");
            }
            if (DrawButton("Click Me", widthMid,Color.cyan))
            {
                Debug.Log("Button Clicked!");
            }            
        }
    }
    
    [WindowToolGroup( 500)]
    public class WindowToolGroupAnimator : WindowToolGroup
    {
        public override string title { get; } = "动画处理";
        public override string tip { get; } = "";
        private string animArg1 = "state";
        private string animArg2 = "2";
        private Transform animRoot = null;
        private bool animIncludeInactive = true;
        private bool animIncludeDisabled = true;
        public override void OnGUI(Rect contentRect)
        {
            animRoot = (Transform)EditorGUILayout.ObjectField(new GUIContent("动画根节点:", "指定动画操作的根节点"),
                animRoot, typeof(Transform), true, GUILayout.Height(20));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("动画参数1:", GUILayout.Width(widthMin));
            animArg1 = EditorGUILayout.TextField(animArg1, fieldStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("动画参数2:", GUILayout.Width(widthMin));
            animArg2 = EditorGUILayout.TextField(animArg2, fieldStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            animIncludeInactive = EditorGUILayout.Toggle("包含未激活对象", animIncludeInactive);
            animIncludeDisabled = EditorGUILayout.Toggle("包含未启用组件", animIncludeDisabled);
            EditorGUILayout.EndHorizontal();

            if (DrawButton("设置动画参数", "设置所有Animator组件的参数", Color.cyan))
            {
                SetAnimatorParameters();
            }   
        }
        
        private void SetAnimatorParameters()
        {
            if (animRoot == null)
            {
                Debug.LogWarning("请先设置动画根节点");
                return;
            }

            var animators = animRoot.GetComponentsInChildren<Animator>(animIncludeInactive);
            foreach (var animator in animators)
            {
                if (!animIncludeDisabled && !animator.enabled) continue;

                // 设置动画参数
                if (animator.parameters.Any(p => p.name == animArg1))
                {
                    if (int.TryParse(animArg2, out int intValue))
                        animator.SetInteger(animArg1, intValue);
                    else if (float.TryParse(animArg2, out float floatValue))
                        animator.SetFloat(animArg1, floatValue);
                    else if (bool.TryParse(animArg2, out bool boolValue))
                        animator.SetBool(animArg1, boolValue);
                }
            }

            Debug.Log($"已设置 {animators.Length} 个Animator的参数");
        }        
    }
  
    [WindowToolGroup( 500)]
    public class WindowToolGroupGameObject : WindowToolGroup
    {
        public override string title { get; } = "物体操作";
        public override string tip { get; } = "物体操作 tool";
        
        private Transform objRoot = null;
        private Transform objRoot2 = null;
        private Vector3 objPositionOffset = new Vector3(1, 0, 0);
        private bool objIncludeInactive = true;
        float scaleValue = 1.0f;
        public override void OnGUI(Rect contentRect)
        {
                objRoot = (Transform)EditorGUILayout.ObjectField(new GUIContent("物体根节点:", "指定物体操作的根节点"),
                    objRoot, typeof(Transform), true, GUILayout.Height(20));

                objRoot2 = (Transform)EditorGUILayout.ObjectField(new GUIContent("参考节点:", "指定参考节点"),
                    objRoot2, typeof(Transform), true, GUILayout.Height(20));

                objPositionOffset = EditorGUILayout.Vector3Field("位置偏移:", objPositionOffset);
                objIncludeInactive = EditorGUILayout.Toggle("包含未激活对象", objIncludeInactive);
                    scaleValue = EditorGUILayout.FloatField("子物体缩放值:", scaleValue);

                    EditorGUILayout.BeginHorizontal();
                if (DrawButton("设置物体名字", "根据TroopsSkinCarEvent设置物体名字", Color.yellow, GUILayout.Width(widthMax)))
                {
                    SetObjectNames();
                }

                if (DrawButton("显示子节点数量", "显示根节点的子节点数量", Color.green, GUILayout.Width(widthMax)))
                {
                    ShowChildCount();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (DrawButton("显示选中节点数量", "显示当前选中的节点数量", Color.magenta, GUILayout.Width(widthMax)))
                {
                    ShowSelectedCount();
                }

                if (DrawButton("打印Path相对根节点", "打印选中节点相对于根节点的路径", Color.white, GUILayout.Width(widthMax)))
                {
                    Tools.PrintRelativePaths(Selection.transforms, objRoot);
                }

                EditorGUILayout.EndHorizontal();

                if (DrawButton("打印子节点下所有节点数量", "统计子节点数量", Color.white, GUILayout.Width(widthMax)))
                {
                    Tools.PrintChildCount(objRoot, true);
                }

                if (DrawButton("设置子物体坐标摆放", "按照偏移量设置子物体位置", Color.white, GUILayout.Width(widthMax)))
                {
                    SetChildObjectPositions();
                }

                if (DrawButton("打印组件参数", "打印组件的详细参数", Color.white, GUILayout.Width(widthMax)))
                {
                    PrintComponentParameters();
                }

                if (DrawButton("设置所有子物体缩放", "统一设置子物体的缩放值", Color.white, GUILayout.Width(widthMax)))
                {
                    SetAllChildScale();
                }

                if (DrawButton("设置SkinCar参数", "设置皮肤车辆相关参数", Color.white, GUILayout.Width(widthMax)))
                {
                    SetSkinCarParameters();
                }
                
                copyCount = EditorGUILayout.IntField(new GUIContent("复制数量:", "设置复制物体的数量"), copyCount);
                copyOffset = EditorGUILayout.Vector3Field("复制偏移:", copyOffset);                
                if (DrawButton("复制物体", "根据设置复制选中的物体", Color.green))
                {
                    CopyObjects();
                }                
        }
        
        private void SetObjectNames()
        {
            if (objRoot == null)
            {
                Debug.LogWarning("请先设置物体根节点");
                return;
            }

            var components = objRoot.GetComponentsInChildren<TroopsSkinCarEvent>(objIncludeInactive);
            foreach (var component in components)
            {
                component.gameObject.name = $"SkinCar_{component.GetInstanceID()}";
            }

            Debug.Log($"已设置 {components.Length} 个物体的名字");
        }

        private void ShowChildCount()
        {
            if (objRoot == null)
            {
                Debug.LogWarning("请先设置物体根节点");
                return;
            }

            int count = objRoot.childCount;
            Debug.Log($"根节点 {objRoot.name} 有 {count} 个直接子节点");
        }

        private void ShowSelectedCount()
        {
            int count = Selection.transforms.Length;
            Debug.Log($"当前选中 {count} 个节点");
        }

        private void SetChildObjectPositions()
        {
            if (objRoot == null)
            {
                Debug.LogWarning("请先设置物体根节点");
                return;
            }

            for (int i = 0; i < objRoot.childCount; i++)
            {
                var child = objRoot.GetChild(i);
                child.localPosition = objPositionOffset * i;
            }

            Debug.Log($"已设置 {objRoot.childCount} 个子物体的位置");
        }

        private void PrintComponentParameters()
        {
            if (objRoot == null)
            {
                Debug.LogWarning("请先设置物体根节点");
                return;
            }

            var components = objRoot.GetComponentsInChildren<Component>(objIncludeInactive);
            foreach (var component in components)
            {
                if (component == null) continue;

                Debug.Log($"组件: {component.GetType().Name} 在物体: {component.gameObject.name}");

                var fields = component.GetType()
                    .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    Debug.Log($"  字段: {field.Name} = {field.GetValue(component)}");
                }

                var properties = component.GetType()
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var property in properties)
                {
                    if (property.CanRead)
                    {
                        try
                        {
                            Debug.Log($"  属性: {property.Name} = {property.GetValue(component)}");
                        }
                        catch (System.Exception e)
                        {
                            Debug.Log($"  属性: {property.Name} = 无法读取 ({e.Message})");
                        }
                    }
                }
            }
        }

        private void SetAllChildScale()
        {
            if (objRoot == null)
            {
                Debug.LogWarning("请先设置物体根节点");
                return;
            }

            var scale = scaleValue*Vector3.one; // 使用基础参数3作为缩放值
            for (int i = 0; i < objRoot.childCount; i++)
            {
                var child = objRoot.GetChild(i);
                child.localScale = scale;
            }

            Debug.Log($"已设置 {objRoot.childCount} 个子物体的缩放为 {scale}");
        }
        private void SetSkinCarParameters()
        {
            if (objRoot == null)
            {
                Debug.LogWarning("请先设置物体根节点");
                return;
            }

            var skinCarComponents = objRoot.GetComponentsInChildren<TroopsSkinCarEvent>(objIncludeInactive);
            foreach (var skinCar in skinCarComponents)
            {
                // 根据具体的TroopsSkinCarEvent组件设置参数
                // 这里需要根据实际的组件属性进行设置
                Debug.Log($"设置SkinCar参数: {skinCar.gameObject.name}");
            }

            Debug.Log($"已设置 {skinCarComponents.Length} 个SkinCar组件的参数");
        }
        
        private int copyCount = 20;
        private Vector3 copyOffset = new Vector3(1, 0, 0);
        private ReorderableList<GameObject> _gameObjectFilterList = null;
        private void CopyObjects()
        {
            if (Selection.transforms.Length == 0)
            {
                Debug.LogWarning("请先选择要复制的物体");
                return;
            }

            foreach (var selected in Selection.transforms)
            {
                for (int i = 0; i < copyCount; i++)
                {
                    var copy = Object.Instantiate(selected.gameObject);
                    copy.name = $"{selected.name}_Copy_{i + 1}";
                    copy.transform.position = selected.position + copyOffset * (i + 1);
                    copy.transform.SetParent(selected.parent);
                }
            }

            Debug.Log($"已复制 {Selection.transforms.Length} 个物体，每个复制 {copyCount} 次");
        }        

    }
    
    
    [WindowToolGroup( 500)]
    public class WindowToolGroupRenderer : WindowToolGroup
    {
        public override string title { get; } = "渲染顺序";
        public override string tip { get; } = "";
        
        private Transform renderRoot = null;
        private int sortingOrder = 5300;
        private bool renderIncludeInactive = true;
        private bool renderIncludeDisabled = true;
        
        public override void OnGUI(Rect contentRect)
        {
                renderRoot = (Transform)EditorGUILayout.ObjectField(new GUIContent("渲染根节点:", "指定渲染操作的根节点"),
                    renderRoot, typeof(Transform), true, GUILayout.Height(20));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Order值:", GUILayout.Width(60));
                sortingOrder = EditorGUILayout.IntField(sortingOrder, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                renderIncludeInactive = EditorGUILayout.Toggle("包含未激活对象", renderIncludeInactive);
                renderIncludeDisabled = EditorGUILayout.Toggle("包含未启用组件", renderIncludeDisabled);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (DrawButton("设置Order", "设置粒子系统的渲染顺序", Color.blue, GUILayout.Width(widthMid)))
                {
                    SetSortingOrder();
                }

                if (DrawButton("设置Order偏移", "在当前Order基础上添加基数", Color.cyan, GUILayout.Width(widthMid)))
                {
                    AddSortingOrderBase();
                }

                if (DrawButton("打印SortingOrder", "打印所有渲染器的Order信息", Color.yellow, GUILayout.Width(widthMid)))
                {
                    PrintSortingOrder();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (DrawButton("隐藏SkinnedMeshRenderer", "隐藏所有蒙皮网格渲染器", Color.red, GUILayout.Width(widthMax)))
                {
                    ToggleSkinnedMeshRenderer(false);
                }

                if (DrawButton("显示SkinnedMeshRenderer", "显示所有蒙皮网格渲染器", Color.green, GUILayout.Width(widthMax)))
                {
                    ToggleSkinnedMeshRenderer(true);
                }

                EditorGUILayout.EndHorizontal();

                if (DrawButton("打印材质球数量", "统计并打印材质球使用情况", Color.magenta))
                {
                    PrintMaterialCount();
                }
        }
        
        private void SetSortingOrder()
        {
            if (renderRoot == null)
            {
                Debug.LogWarning("请先设置渲染根节点");
                return;
            }

            var particleSystems = renderRoot.GetComponentsInChildren<ParticleSystem>(renderIncludeInactive);
            foreach (var ps in particleSystems)
            {
                if (!renderIncludeDisabled && !ps.gameObject.activeInHierarchy) continue;

                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = sortingOrder;
                }
            }

            var spriteRenderers = renderRoot.GetComponentsInChildren<SpriteRenderer>(renderIncludeInactive);
            foreach (var sr in spriteRenderers)
            {
                if (!renderIncludeDisabled && !sr.gameObject.activeInHierarchy) continue;
                sr.sortingOrder = sortingOrder;
            }

            Debug.Log($"已设置 {particleSystems.Length} 个粒子系统和 {spriteRenderers.Length} 个精灵渲染器的Order为 {sortingOrder}");
        }

        private void AddSortingOrderBase()
        {
            if (renderRoot == null)
            {
                Debug.LogWarning("请先设置渲染根节点");
                return;
            }

            var particleSystems = renderRoot.GetComponentsInChildren<ParticleSystem>(renderIncludeInactive);
            foreach (var ps in particleSystems)
            {
                if (!renderIncludeDisabled && !ps.gameObject.activeInHierarchy) continue;

                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder += sortingOrder;
                }
            }

            var spriteRenderers = renderRoot.GetComponentsInChildren<SpriteRenderer>(renderIncludeInactive);
            foreach (var sr in spriteRenderers)
            {
                if (!renderIncludeDisabled && !sr.gameObject.activeInHierarchy) continue;
                sr.sortingOrder += sortingOrder;
            }

            Debug.Log($"已为 {particleSystems.Length} 个粒子系统和 {spriteRenderers.Length} 个精灵渲染器的Order添加偏移 {sortingOrder}");
        }

        private void PrintSortingOrder()
        {
            if (renderRoot == null)
            {
                Debug.LogWarning("请先设置渲染根节点");
                return;
            }

            var particleSystems = renderRoot.GetComponentsInChildren<ParticleSystem>(renderIncludeInactive);
            foreach (var ps in particleSystems)
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    Debug.Log($"粒子系统 {ps.gameObject.name} 的Sorting {renderer.sortingOrder}");
                }
            }

            var spriteRenderers = renderRoot.GetComponentsInChildren<SpriteRenderer>(renderIncludeInactive);
            foreach (var sr in spriteRenderers)
            {
                Debug.Log($"精灵渲染器 {sr.gameObject.name} 的Sorting {sr.sortingOrder}");
            }
        }

        private void ToggleSkinnedMeshRenderer(bool enabled)
        {
            if (renderRoot == null)
            {
                Debug.LogWarning("请先设置渲染根节点");
                return;
            }

            var skinnedRenderers = renderRoot.GetComponentsInChildren<SkinnedMeshRenderer>(renderIncludeInactive);
            foreach (var renderer in skinnedRenderers)
            {
                renderer.enabled = enabled;
            }

            Debug.Log($"已{(enabled ? "显示" : "隐藏")} {skinnedRenderers.Length} 个蒙皮网格渲染器");
        }

        private void PrintMaterialCount()
        {
            if (renderRoot == null)
            {
                Debug.LogWarning("请先设置渲染根节点");
                return;
            }

            var renderers = renderRoot.GetComponentsInChildren<Renderer>(renderIncludeInactive);
            var materialDict = new Dictionary<Material, int>();

            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        if (materialDict.ContainsKey(material))
                            materialDict[material]++;
                        else
                            materialDict[material] = 1;
                    }
                }
            }

            Debug.Log($"总共找到 {materialDict.Count} 种不同的材质球:");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== 材质球使用统计 ===");
            foreach (var kvp in materialDict.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"材质: {kvp.Key.name} - 使用次数: {kvp.Value}");
            }

            sb.AppendLine($"总计: {materialDict.Count} 种材质球，{materialDict.Values.Sum()} 次使用");

            Debug.Log(sb.ToString());            
        }        
        
    }    
    [WindowToolGroup( 500)]
    public class WindowToolGroupOpti : WindowToolGroup
    {
        public override string title { get; } = "性能分析";
        public override string tip { get; } = "";
        public override void OnGUI(Rect contentRect)
        {
            if (DrawButton("开启分析状态", "开启性能分析模式", Color.red))
            {
                SetProfilerStatus();
            }
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

            Tools.FindAndSetGameObject("Troops_root", false);
            Tools.FindAndSetGameObject("rss_root", false);
            Tools.FindAndSetGameObject("lod3_root", false);
            Tools.FindAndSetGameObject("CityRoot", false);
            Tools.FindAndSetGameObject("fogSystem", false);
            Tools.FindAndSetGameObject("BillBuffer", false);

            Tools.ToggleGameStats();
        }        
        
    }
    
    [WindowToolGroup( 500)]
    public class WindowToolGroupFBX : WindowToolGroup
    {
        public override string title { get; } = "🔧 模型处理";
        public override string tip { get; } = "";
        
        private Transform modelRoot = null;
        private bool ProcessFBXOnlyLog = false;
        
        public override void OnGUI(Rect contentRect)
        {
            modelRoot = (Transform)EditorGUILayout.ObjectField(new GUIContent("模型根节点:", "指定模型处理的根节点"),
                modelRoot, typeof(Transform), true, GUILayout.Height(20));

            ProcessFBXOnlyLog = EditorGUILayout.Toggle("只打印模型数据", ProcessFBXOnlyLog);

            EditorGUILayout.BeginHorizontal();
            if (DrawButton("查询引用模型", "查询模型引用情况", Color.blue, GUILayout.Width(widthMid)))
            {
                Tools.ProcessFBX(true, modelRoot.gameObject, ProcessFBXOnlyLog);
            }

            if (DrawButton("修复模型", "修复模型相关问题", Color.green, GUILayout.Width(widthMid)))
            {
                Tools.ProcessFBX(false, modelRoot.gameObject, ProcessFBXOnlyLog);
            }

            EditorGUILayout.EndHorizontal();
        }
        
    }
    [WindowToolGroup( 500)]
    public class WindowToolGroupPrefab : WindowToolGroup
    {
        public override string title { get; } = "Prefab操作";
        public override string tip { get; } = "";
        
        private bool ProcessPrefabOnlyLog = true;
        public override void OnGUI(Rect contentRect)
        {
            ProcessPrefabOnlyLog = EditorGUILayout.Toggle("只打印Prefab数据", ProcessPrefabOnlyLog);

            EditorGUILayout.BeginHorizontal();
            if (DrawButton("查询Prefab依赖的Prefab", "查询Prefab依赖的Prefab", Color.blue))
            {
                Tools.ProcessPrefab();
            }

            if (DrawButton("Prefab还原Animator"))
            {
                Tools.RevertSpecificPrefabAnimator();
            }    

            EditorGUILayout.EndHorizontal();
        }
        
    }
    
    

[WindowToolGroup(500)]
public class WindowToolGroupAnimationHero : WindowToolGroup
{
    public override string title { get; } = "部队简化模式设置SortingOrder";
    public override string tip { get; } = "部队阵型简化模式RenderSprite的Prefab设置SortingOrder用来合批";
    
    private bool checking = true;
    private string folderPath = "Assets/BundleAssets/Troop/troop_unit_hero";  // 
    public override void OnGUI(Rect contentRect)
    {
        checking = EditorGUILayout.Toggle("检查数据", checking);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("目标路径:", GUILayout.Width(60));
        folderPath = EditorGUILayout.TextField(folderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(50)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择Prefab目录", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // 转换为相对于Assets的路径
                string assetsPath = Application.dataPath;
                if (selectedPath.StartsWith(assetsPath))
                {
                    folderPath = "Assets" + selectedPath.Substring(assetsPath.Length);
                }
                else
                {
                    Debug.LogWarning("请选择项目Assets目录下的文件夹");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        // 新增的AnimationHero sortingOrder操作按钮
        EditorGUILayout.BeginHorizontal();
        if (DrawButton("读取AnimationBase SortingOrder", Color.green))
        {
            ReadAnimationHeroSortingOrder();
        }

        if (DrawButton("设置AnimationBase SortingOrder",  Color.yellow))
        {
            SetAnimationHeroSortingOrder();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (DrawButton("读取怪物的AnimationBase SortingOrder", Color.green))
        {
            ReadAnimationBaseSortingOrderOfMonster();
        }

        if (DrawButton("设置怪物AnimationBase SortingOrder",  Color.yellow))
        {
            WriteAnimationBaseSortingOrderOfMonster();
        }
        EditorGUILayout.EndHorizontal();        
        
    }
    
    /// <summary>
    /// 读取选中Prefab中AnimationHero的sortingOrder参数并打印
    /// </summary>
    private void ReadAnimationHeroSortingOrder()
    {
        var selectedObjects = Tools.GetSelectedPrefabPaths();
        if (selectedObjects == null || selectedObjects.Count == 0)
        {
            Debug.LogWarning("请先选中要处理的Prefab文件");
            return;
        }
        shareStringList.Clear();
        foreach (var assetPath in selectedObjects)
        {
            if (!assetPath.EndsWith(".prefab"))
            {
                Debug.LogWarning($"跳过非Prefab文件: {assetPath.FileName()}");
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError($"无法加载Prefab: {assetPath}");
                continue;
            }

            // 查找组件
            var component = prefab.GetComponent<AnimationBase>();
            if (component == null)
            {
                component = prefab.GetComponentInChildren<AnimationBase>();
            }

            int sortingOrder = -1;
            if (component != null)
            {
                sortingOrder = component.sortingOrder;
                Debug.Log($"Prefab: {prefab.name}, SortingOrder: {component.sortingOrder}");
            }
            else
            {
                Debug.LogWarning($"Prefab {prefab.name} 中未找到AnimationBase组件");
            }
            shareStringList.Add($"{sortingOrder} - ({assetPath.FileName()})");
        }
        WindowToolGroupReorderableListString.SetData(shareStringList);
    }

    /// <summary>
    /// 设置选中Prefab中AnimationBase的sortingOrder参数为文件名最后一个下划线后的数字
    /// </summary>
    private void SetAnimationHeroSortingOrder()
    {
        var selectedObjects = Tools.GetSelectedPrefabPaths();
        if (selectedObjects == null || selectedObjects.Count == 0)
        {
            Debug.LogWarning("请先选中要处理的Prefab文件");
            return;
        }
        shareStringList.Clear();
        foreach (var assetPath in selectedObjects)
        {
            if (!assetPath.EndsWith(".prefab"))
            {
                Debug.LogWarning($"跳过非Prefab文件: {Path.GetFileNameWithoutExtension(assetPath)}");
                continue;
            }

            // 从文件名提取数字
            int sortingOrderValue = ExtractSortingOrderFromFileName(assetPath.FileName());
            if (sortingOrderValue == -1)
            {
                Debug.LogError($"无法从文件名 {assetPath.FileName()} 中提取有效的数字（最后一个下划线后应为数字）");
                continue;
            }

            if (checking)
            {
                // 只打印，不实际修改
                Debug.Log($"[预览] Prefab: {assetPath.FileName()}, 将设置SortingOrder为: {sortingOrderValue}");
                shareStringList.Add($"{sortingOrderValue} - ({assetPath.FileName()})");
            }
            else
            {
                // 实际修改Prefab
                ModifyPrefabSortingOrder(assetPath, sortingOrderValue);
            }
        }
        WindowToolGroupReorderableListString.SetData(shareStringList);
        if (!checking)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("AnimationHero SortingOrder 设置完成！");
        }
    }

    /// <summary>
    /// 从文件名中提取最后一个下划线后的数字
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>提取的数字，如果无法提取则返回-1</returns>
    private int ExtractSortingOrderFromFileName(string fileName)
    {
        int lastUnderscoreIndex = fileName.LastIndexOf('_');
        if (lastUnderscoreIndex == -1 || lastUnderscoreIndex == fileName.Length - 1)
        {
            return -1;
        }

        string numberPart = fileName.Substring(lastUnderscoreIndex + 1);
        if (int.TryParse(numberPart, out int result))
        {
            return result;
        }

        return -1;
    }

    /// <summary>
    /// 修改Prefab中AnimationHero的sortingOrder
    /// </summary>
    /// <param name="prefabPath">Prefab路径</param>
    /// <param name="sortingOrder">要设置的sortingOrder值</param>
    private void ModifyPrefabSortingOrder(string prefabPath, int sortingOrder)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"无法加载Prefab: {prefabPath}");
            return;
        }

        // 使用PrefabUtility来正确修改Prefab
        string tempPath = "Assets/temp_prefab_instance.prefab";
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        
        if (instance == null)
        {
            Debug.LogError($"无法实例化Prefab: {prefabPath}");
            return;
        }

        try
        {
            // 查找组件
            var component = instance.GetComponent<AnimationBase>();
            if (component == null)
            {
                component = instance.GetComponentInChildren<AnimationBase>();
            }

            if (component != null)
            {
                // 记录修改前的值
                int oldValue = component.sortingOrder;
                
                // 设置新值
                component.sortingOrder = sortingOrder;
                
                // 应用修改到Prefab
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                
                Debug.Log($"Prefab: {prefab.name}, SortingOrder: {oldValue} -> {sortingOrder}");
                shareStringList.Add($"{oldValue} -> {sortingOrder} - ({prefab.name})");
            }
            else
            {
                Debug.LogError($"Prefab {prefab.name} 中未找到组件");
            }
        }
        finally
        {
            // 清理临时实例
            GameObject.DestroyImmediate(instance);
        }
    }

    public void ReadAnimationBaseSortingOrderOfMonster()
    {
        shareStringList.Clear();
        
        var all_barbarianAssetList=GetMonsterAssetList();
        foreach (string assetPath in all_barbarianAssetList)
        {
            string asset_path = "Assets/" + assetPath + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset_path);
            if (prefab == null)
            {
                Debug.LogError($"无法加载Prefab: {assetPath}");
                continue;
            }

            // 查找组件
            var component = prefab.GetComponent<AnimationBase>();
            if (component == null)
            {
                component = prefab.GetComponentInChildren<AnimationBase>();
            }

            int sortingOrder = -1;
            if (component != null)
            {
                sortingOrder = component.sortingOrder;
                Debug.Log($"Prefab: {prefab.name}, SortingOrder: {component.sortingOrder}");
            }
            else
            {
                Debug.LogWarning($"Prefab {prefab.name} 中未找到AnimationBase组件");
            }
            shareStringList.Add($"{sortingOrder} - ({assetPath.FileName()})");            
        }
        WindowToolGroupReorderableListString.SetData(shareStringList);
    }      
    public void WriteAnimationBaseSortingOrderOfMonster()
    {
        shareStringList.Clear();
        int order = 101;
        var all_barbarianAssetList=GetMonsterAssetList();
        foreach (string assetPath in all_barbarianAssetList)
        {
            string asset_path = "Assets/" + assetPath + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset_path);
            if (prefab == null)
            {
                Debug.LogError($"无法加载Prefab: {assetPath}");
                continue;
            }

            // 查找组件
            var component = prefab.GetComponent<AnimationBase>();
            if (component == null)
            {
                component = prefab.GetComponentInChildren<AnimationBase>();
            }

            int sortingOrder = -1;
            if (component != null)
            {
                sortingOrder = order++;
                if (!checking)
                {
                    component.sortingOrder = sortingOrder;
                    EditorUtility.SetDirty(prefab);
                }
                
                Debug.Log($"Prefab: {prefab.name}, SortingOrder: {component.sortingOrder}");
            }
            else
            {
                Debug.LogError($"Prefab {prefab.name} 中未找到AnimationBase组件");
            }
            shareStringList.Add($"{sortingOrder} - ({assetPath.FileName()})");            
        }
        WindowToolGroupReorderableListString.SetData(shareStringList);
        AssetDatabase.SaveAssets();
    }      
    static List<string> GetMonsterAssetList()
    {
        DataService.Instance.Init();
        GameHelper.AddPreloadAssets();
        
        List<string> barbarianFormationAssetList = TroopProxy.GetMonsterFormationPrefabList();
        
        //收集怪物的asset
        List<string> all_barbarianAssetList = new List<string>();
        foreach (string asset in barbarianFormationAssetList)
        {
            string asset_path = "Assets/" + asset + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset_path);
            if (prefab == null)
                continue;

            BarbarianConfig[] components = prefab.GetComponentsInChildren<BarbarianConfig>();
            foreach (var component in components)
            {
                foreach (var config in component.m_UnitDummys)
                {
                    string unitPrefab = config.unitPrefab;
                    if (string.IsNullOrEmpty(unitPrefab))
                        continue;

                    if (!all_barbarianAssetList.Contains(unitPrefab))
                        all_barbarianAssetList.Add(unitPrefab);
                }
            }
        }
        all_barbarianAssetList.Sort();        
        return all_barbarianAssetList;        
    }
    static string FixAssetPath(string path)
    {
        string strPath = "Assets/" + path;
        strPath = strPath.Replace('\\', '/');
        if (!Path.HasExtension(strPath))
        {
            strPath = strPath + ".prefab";
        }
        return strPath;
    }       
}    
    // [WindowToolGroup( 500)]
    // public class WindowToolGroupT : WindowToolGroup
    // {
    //     public override string title { get; } = "";
    //     public override string tip { get; } = "";
    //     public override void OnGUI(Rect contentRect)
    //     {
    //
    //     }
    //     
    // }
    // [WindowToolGroup( 500)]
    // public class WindowToolGroupT : WindowToolGroup
    // {
    //     public override string title { get; } = "";
    //     public override string tip { get; } = "";
    //     public override void OnGUI(Rect contentRect)
    //     {
    //
    //     }
    //     
    // }
    
}