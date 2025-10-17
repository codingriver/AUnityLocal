using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AUnityLocal.Editor
{

    public abstract class WindowToolGroupSearch : WindowToolGroup
    {
        public override string title { get; } = "搜索";
        public override string tip { get; } = "";
        
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
    }

    [WindowToolGroup(500, WindowArea.RightMid)]
    public class WindowToolGroupSearchObjectByName : WindowToolGroupSearch
    {
        public override string title { get; } = "Hierarchy 物体名字搜索";
        public override string tip { get; } = "Hierarchy 物体名字搜索";
        
        private string searchName = "";
        private Transform parentTransform = null;
        private bool exactMatch = false;
        private bool includeInactive = true;        
        public override void OnGUI()
        {
            // 搜索参数
            searchName = EditorGUILayout.TextField("Search Name:", searchName);
            parentTransform = (Transform)EditorGUILayout.ObjectField("Parent:", parentTransform, typeof(Transform), true);
            exactMatch = EditorGUILayout.Toggle("完全匹配:", exactMatch);
            includeInactive = EditorGUILayout.Toggle("Include Inactive:", includeInactive);
        
            EditorGUILayout.Space();
        
            GUILayout.BeginHorizontal();
            // 搜索按钮
            if (DrawButton("搜索"))
            {
                PerformSearch();
            }
            // 搜索按钮
            if (DrawButton("组合搜索"))
            {
                PerformSearch2();
            }            
            GUILayout.EndHorizontal();
            
            // 组件操作按钮
            if (shareObjectList.Count > 0)
            {
                EditorGUILayout.Space();
    
                GUILayout.BeginHorizontal();
                if (DrawButton("设置active"))
                {
                    SetAllGameObject(true);
                }
                if (DrawButton("设置inactive"))
                {
                    SetAllGameObject(false);
                }
                GUILayout.EndHorizontal();
            }                
        }
        
        void SetAllGameObject(bool active)
        {
            if (shareObjectList.Count == 0)
            {
                Debug.LogWarning("没有搜索到任何物体");
                return;
            }

            int modifiedCount = 0;
    
            foreach (var obj in shareObjectList)
            {
                GameObject go = obj as GameObject;
                if (go == null) continue;
        
                if (go.activeSelf != active)
                {
                    go.SetActive(active);
                    modifiedCount++;
                }
            }
    
            string action = active ? "启用" : "禁用";
            Debug.Log($"{action}操作完成 - 共{action}了 {modifiedCount} 个物体");
    
            // 标记场景为已修改
            if (modifiedCount > 0)
            {
                EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
            }
        }
        private void PerformSearch(bool clear=true)
        {
            if (clear)
            {
                shareObjectList.Clear();    
            }

            if (parentTransform == null)
            {
                // 搜索整个Hierarchy
                SearchInScene();
            }
            else
            {
                // 在指定父物体下搜索
                SearchInChildren(parentTransform);
            }
            //显示
            WindowToolGroupReorderableListObject.SetData(shareObjectList);
        }
        private void PerformSearch2()
        {
            
            if(shareObjectList.Count==0)
            {
                PerformSearch(true);
                return;
            }            
            List<Object> list= new List<Object>();
            list.AddRange(shareObjectList);

            shareObjectList.Clear();
            foreach (var o in list)
            {
                // 检查当前物体
                if (IsNameMatch(o.name))
                {
                    shareObjectList.Add(o);
                }
            }
            //显示
            WindowToolGroupReorderableListObject.SetData(shareObjectList);
        }
        private void SearchInScene()
        {
            // GameObject[] allObjects = includeInactive ? 
            //     Resources.FindObjectsOfTypeAll<GameObject>() : 
            //     GameObject.FindObjectsOfType<GameObject>();
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();


            foreach (GameObject obj in allObjects)
            {
                // // 过滤掉预制体和资源文件中的对象
                // if (EditorUtility.IsPersistent(obj))
                //     continue;
                if (includeInactive&&!obj.activeInHierarchy)
                {
                    continue;
                }

                if (IsNameMatch(obj.name))
                {
                    shareObjectList.Add(obj);
                }
            }
        }

        private void SearchInChildren(Transform parent)
        {
            // 检查当前物体
            if (IsNameMatch(parent.name))
            {
                shareObjectList.Add(parent.gameObject);
            }

            // 递归检查所有子物体
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (includeInactive || child.gameObject.activeInHierarchy)
                {
                    SearchInChildren(child);
                }
            }
        }

        private bool IsNameMatch(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return true;
            }
            
            if (exactMatch)
            {
                return objectName.Equals(searchName, System.StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return objectName.IndexOf(searchName, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }        
    }
    
    [WindowToolGroup(450, WindowArea.RightMid)]
    public class WindowToolGroupSearchTextComponent : WindowToolGroupSearch
    {
        public override string title { get; } = "UGUI Text组件文本搜索";
        public override string tip { get; } = "搜索UGUI Text组件的文本内容";
        
        private string searchText = "";
        private Transform parentTransform = null;
        private bool exactMatch = false;
        private bool includeInactive = true;
        private bool checkTextEnabled = false; // 新增选项：是否检查Text组件启用状态
        
        public override void OnGUI()
        {
            // 搜索参数
            searchText = EditorGUILayout.TextField("Search Text:", searchText);
            parentTransform = (Transform)EditorGUILayout.ObjectField("Parent:", parentTransform, typeof(Transform), true);
            exactMatch = EditorGUILayout.Toggle("完全匹配:", exactMatch);
            includeInactive = EditorGUILayout.Toggle("Include Inactive:", includeInactive);
            checkTextEnabled = EditorGUILayout.Toggle("Text组件必须启用:", checkTextEnabled);
        
            EditorGUILayout.Space();
        
            GUILayout.BeginHorizontal();
            // 搜索按钮
            if (DrawButton("搜索"))
            {
                PerformSearch();
            }
            // 组合搜索按钮
            if (DrawButton("组合搜索"))
            {
                PerformSearch2();
            }            
            GUILayout.EndHorizontal();
        }
        
        private void PerformSearch(bool clear = true)
        {
            if (clear)
            {
                shareObjectList.Clear();    
            }

            if (parentTransform == null)
            {
                // 搜索整个Hierarchy
                SearchInScene();
            }
            else
            {
                // 在指定父物体下搜索
                SearchInChildren(parentTransform);
            }
            
            // 显示结果
            WindowToolGroupReorderableListObject.SetData(shareObjectList);
        }
        
        private void PerformSearch2()
        {
            if(shareObjectList.Count == 0)
            {
                PerformSearch(true);
                return;
            }            
            
            List<Object> list = new List<Object>();
            list.AddRange(shareObjectList);

            shareObjectList.Clear();
            foreach (var o in list)
            {
                GameObject obj = o as GameObject;
                if (obj != null && HasMatchingTextComponent(obj))
                {
                    shareObjectList.Add(obj);
                }
            }
            
            // 显示结果
            WindowToolGroupReorderableListObject.SetData(shareObjectList);
        }
        
        private void SearchInScene()
        {
            // GameObject[] allObjects = includeInactive ? 
            //     Resources.FindObjectsOfTypeAll<GameObject>() : 
            //     GameObject.FindObjectsOfType<GameObject>();
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                // // 过滤掉预制体和资源文件中的对象
                // if (EditorUtility.IsPersistent(obj))
                //     continue;
                if(!includeInactive&&!obj.activeInHierarchy)
                {
                    continue;
                }
                
                if (HasMatchingTextComponent(obj))
                {
                    shareObjectList.Add(obj);
                }
            }
        }

        private void SearchInChildren(Transform parent)
        {
            // 检查当前物体
            if (HasMatchingTextComponent(parent.gameObject))
            {
                shareObjectList.Add(parent.gameObject);
            }

            // 递归检查所有子物体
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (includeInactive || child.gameObject.activeInHierarchy)
                {
                    SearchInChildren(child);
                }
            }
        }

        private bool HasMatchingTextComponent(GameObject obj)
        {
            // 获取所有Text组件（包括子类）
            Text[] textComponents = obj.GetComponents<Text>();
            
            foreach (Text textComponent in textComponents)
            {
                if (textComponent == null)
                    continue;
                
                // 如果需要检查Text组件启用状态
                if (checkTextEnabled && !textComponent.enabled)
                    continue;
                
                // 检查文本内容是否匹配
                if (IsTextMatch(textComponent.text))
                {
                    return true;
                }
            }
            
            return false;
        }

        private bool IsTextMatch(string textContent)
        {
            if (string.IsNullOrEmpty(textContent))
                return false;
                
            if (exactMatch)
            {
                return textContent.Equals(searchText, System.StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return textContent.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }        
    }    
    
    [WindowToolGroup(460, WindowArea.RightMid)]
    public class WindowToolGroupSearchComponent : WindowToolGroupSearch
    {
        public override string title { get; } = "组件搜索";
        public override string tip { get; } = "根据组件名称搜索组件类型，然后搜索挂载该组件的物体";
        
        private string componentNameSearch = "";
        private Transform parentTransform = null;
        private bool includeInactive = true;
        private bool showComponentSearchResults = false;
        
        // 组件搜索相关
        private List<Type> matchedComponentTypes = new List<Type>();
        private Type selectedComponentType = null;
        private string selectedComponentName = "";
        private Vector2 componentScrollPosition = Vector2.zero;
        
        public override void OnGUI()
        {
            // 第一步：搜索组件类型
            EditorGUILayout.LabelField("第一步：搜索组件类型", EditorStyles.boldLabel);
            componentNameSearch = EditorGUILayout.TextField("组件名称:", componentNameSearch);
            
            GUILayout.BeginHorizontal();
            if (DrawButton("搜索组件类型"))
            {
                SearchForComponents();
            }
            if (DrawButton("清空组件搜索"))
            {
                ClearComponentSearch();
            }
            GUILayout.EndHorizontal();
            
            // 显示搜索到的组件类型
            if (showComponentSearchResults && matchedComponentTypes.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"找到 {matchedComponentTypes.Count} 个匹配的组件类型:", EditorStyles.boldLabel);
                
                // 使用固定高度的区域，不使用ScrollView以避免水平滚动
                Rect scrollRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
                
                // 计算每行高度
                float lineHeight = EditorGUIUtility.singleLineHeight + 2;
                float totalHeight = matchedComponentTypes.Count * lineHeight;
                
                // 只有当内容超出显示区域时才显示垂直滚动条
                if (totalHeight > scrollRect.height)
                {
                    componentScrollPosition = GUI.BeginScrollView(
                        scrollRect, 
                        componentScrollPosition, 
                        new Rect(0, 0, scrollRect.width - 20, totalHeight), // 减去滚动条宽度
                        false, // 禁用水平滚动条
                        true   // 启用垂直滚动条
                    );
                }
                else
                {
                    GUI.BeginGroup(scrollRect);
                    componentScrollPosition = Vector2.zero;
                }
                
                // 绘制组件列表
                for (int i = 0; i < matchedComponentTypes.Count; i++)
                {
                    var componentType = matchedComponentTypes[i];
                    Rect lineRect = new Rect(0, i * lineHeight, scrollRect.width - (totalHeight > scrollRect.height ? 20 : 0), lineHeight);
                    
                    // 计算各部分的宽度
                    float buttonWidth = 60;
                    float spacing = 5;
                    float availableWidth = lineRect.width - buttonWidth - spacing;
                    float nameWidth = Mathf.Min(200, availableWidth * 0.6f);
                    float namespaceWidth = availableWidth - nameWidth;
                    
                    // 组件名称
                    Rect nameRect = new Rect(lineRect.x, lineRect.y, nameWidth, lineRect.height);
                    GUI.Label(nameRect, componentType.Name);
                    
                    // 命名空间
                    Rect namespaceRect = new Rect(nameRect.xMax, lineRect.y, namespaceWidth, lineRect.height);
                    GUI.Label(namespaceRect, $"({componentType.Namespace})", EditorStyles.miniLabel);
                    
                    // 选择按钮 - 固定在右侧
                    Rect buttonRect = new Rect(lineRect.xMax - buttonWidth, lineRect.y, buttonWidth, lineRect.height);
                    if (GUI.Button(buttonRect, "选择"))
                    {
                        selectedComponentType = componentType;
                        selectedComponentName = componentType.Name;
                        ClearComponentSearch();
                    }
                }
                
                if (totalHeight > scrollRect.height)
                {
                    GUI.EndScrollView();
                }
                else
                {
                    GUI.EndGroup();
                }
            }
            
            EditorGUILayout.Space();
            
            // 第二步：搜索挂载组件的物体
            EditorGUILayout.LabelField("第二步：搜索挂载组件的物体", EditorStyles.boldLabel);
            
            // 显示选中的组件类型
            GUI.enabled = false;
            EditorGUILayout.TextField("选中的组件:", selectedComponentName);
            GUI.enabled = true;
            
            // 搜索参数
            parentTransform = (Transform)EditorGUILayout.ObjectField("Parent:", parentTransform, typeof(Transform), true);
            includeInactive = EditorGUILayout.Toggle("Include Inactive:", includeInactive);
            
            EditorGUILayout.Space();
            
            GUILayout.BeginHorizontal();
            
            GUI.enabled = selectedComponentType != null;
            if (DrawButton("搜索"))
            {
                PerformComponentSearch();
            }
            if (DrawButton("组合搜索"))
            {
                PerformComponentSearch2();
            }
            GUI.enabled = true;
            
            GUILayout.EndHorizontal();
            // 组件操作按钮
            if (shareObjectList.Count > 0)
            {
                EditorGUILayout.Space();
    
                GUILayout.BeginHorizontal();
                if (DrawButton("启用所有组件"))
                {
                    SetAllComponentsEnabled(true);
                }
                if (DrawButton("禁用所有组件"))
                {
                    SetAllComponentsEnabled(false);
                }
                GUILayout.EndHorizontal();
            }            
        }
        
        private void SetAllComponentsEnabled(bool enabled)
        {
            if (shareObjectList.Count == 0)
            {
                Debug.LogWarning("没有搜索到任何物体");
                return;
            }

            // 获取当前搜索的组件类型
            Type currentSearchType = selectedComponentType;
            if (currentSearchType == null)
            {
                Debug.LogWarning("当前不是组件搜索模式或未指定组件类型");
                return;
            }

            int modifiedCount = 0;
    
            foreach (var obj in shareObjectList)
            {
                GameObject go = obj as GameObject;
                if (go == null) continue;
        
                // 只获取当前搜索的组件类型
                Component component = go.GetComponent(currentSearchType);
                if (component == null) continue;
        
                // 检查组件是否有enabled属性
                var enabledProperty = component.GetType().GetProperty("enabled");
                if (enabledProperty != null && enabledProperty.CanWrite)
                {
                    try
                    {
                        enabledProperty.SetValue(component, enabled);
                        modifiedCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"无法设置 {go.name} 上的 {component.GetType().Name} 组件状态: {e.Message}");
                    }
                }
            }
    
            string action = enabled ? "启用" : "禁用";
            Debug.Log($"{action}操作完成 - 共{action}了 {modifiedCount} 个 {currentSearchType.Name} 组件");
    
            // 标记场景为已修改
            if (modifiedCount > 0)
            {
                EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
            }
        }
        private void SearchForComponents()
        {
            if (string.IsNullOrEmpty(componentNameSearch))
            {
                showComponentSearchResults = false;
                return;
            }

            matchedComponentTypes.Clear();

            try
            {
                var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch
                        {
                            return Type.EmptyTypes;
                        }
                    })
                    .Where(t => typeof(Component).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .ToList();
                    
                foreach (var type in allTypes)
                {
                    if (type.Name.IndexOf(componentNameSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchedComponentTypes.Add(type);
                    }
                }

                matchedComponentTypes = matchedComponentTypes.OrderBy(t => t.Name).ToList();
                showComponentSearchResults = true;
                
                Debug.Log($"找到 {matchedComponentTypes.Count} 个匹配的组件类型");
            }
            catch (Exception e)
            {
                Debug.LogError($"搜索组件时出错: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"搜索组件时出错: {e.Message}", "确定");
            }
        }
        
        private void ClearComponentSearch()
        {
            // componentNameSearch = "";
            matchedComponentTypes.Clear();
            // selectedComponentType = null;
            // selectedComponentName = "";
            // showComponentSearchResults = false;
        }
        
        private void PerformComponentSearch(bool clear = true)
        {
            if (clear)
            {
                shareObjectList.Clear();
            }
            
            if (selectedComponentType == null)
            {
                Debug.LogWarning("请先选择一个组件类型");
                return;
            }

            if (parentTransform == null)
            {
                // 搜索整个场景
                SearchComponentsInScene();
            }
            else
            {
                // 在指定父物体下搜索
                SearchComponentsInChildren(parentTransform);
            }
            
            // 显示结果
            WindowToolGroupReorderableListObject.SetData(shareObjectList);
            Debug.Log($"搜索完成 - 找到 {shareObjectList.Count} 个挂载 {selectedComponentName} 组件的物体");
        }
        
        private void PerformComponentSearch2()
        {
            if (shareObjectList.Count == 0)
            {
                PerformComponentSearch(true);
                return;
            }
            
            if (selectedComponentType == null)
            {
                Debug.LogWarning("请先选择一个组件类型");
                return;
            }
            
            List<Object> list = new List<Object>();
            list.AddRange(shareObjectList);

            shareObjectList.Clear();
            foreach (var o in list)
            {
                GameObject obj = o as GameObject;
                if (obj != null && HasTargetComponent(obj))
                {
                    shareObjectList.Add(obj);
                }
            }
            
            // 显示结果
            WindowToolGroupReorderableListObject.SetData(shareObjectList);
            Debug.Log($"组合搜索完成 - 找到 {shareObjectList.Count} 个挂载 {selectedComponentName} 组件的物体");
        }
        
        private void SearchComponentsInScene()
        {
            try
            {
                var components = Object.FindObjectsOfType(selectedComponentType, includeInactive);
                
                foreach (var component in components)
                {
                    if (component == null)
                        continue;
                        
                    var unityComponent = component as Component;
                    if (unityComponent == null)
                        continue;
                        
                    var gameObject = unityComponent.gameObject;
                    if (gameObject == null)
                        continue;
                    
                    // 过滤掉预制体和资源文件中的对象
                    if (EditorUtility.IsPersistent(gameObject))
                        continue;
                    
                    if (!shareObjectList.Contains(gameObject))
                    {
                        shareObjectList.Add(gameObject);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"搜索组件时出错: {e.Message}");
            }
        }
        
        private void SearchComponentsInChildren(Transform parent)
        {
            // 检查当前物体
            if (HasTargetComponent(parent.gameObject))
            {
                if (!shareObjectList.Contains(parent.gameObject))
                {
                    shareObjectList.Add(parent.gameObject);
                }
            }

            // 递归检查所有子物体
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (includeInactive || child.gameObject.activeInHierarchy)
                {
                    SearchComponentsInChildren(child);
                }
            }
        }
        
        private bool HasTargetComponent(GameObject obj)
        {
            if (selectedComponentType == null || obj == null)
                return false;
                
            return obj.GetComponent(selectedComponentType) != null;
        }
    }


  [WindowToolGroup(503, WindowArea.RightMid)]
    public class WindowToolGroupSearchLayer : WindowToolGroupSearch
    {
        public override string title { get; } = "Layer搜索";
        public override string tip { get; } = "根据Layer层级搜索场景中的物体";
        
        private Transform parentTransform = null;
        private bool includeInactive = true;
        private bool showLayerSearchResults = false;
        
        // Layer搜索相关
        private List<LayerInfo> availableLayers = new List<LayerInfo>();
        private LayerMask selectedLayerMask = 0;
        private Vector2 layerScrollPosition = Vector2.zero;
        private bool showLayerList = true; // 控制Layer列表显示/隐藏
        
        // 进度条相关
        private bool isSearching = false;
        private float searchProgress = 0f;
        private string searchProgressMessage = "";
        
        private struct LayerInfo
        {
            public int layerIndex;
            public string layerName;
            public bool isSelected;
            
            public LayerInfo(int index, string name)
            {
                layerIndex = index;
                layerName = name;
                isSelected = false;
            }
        }
        
        public override void OnGUI()
        {
            // 第一步：选择Layer层级
            EditorGUILayout.LabelField("第一步：选择Layer层级", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (DrawButton("刷新Layer列表"))
            {
                RefreshLayerList();
            }
            
            // 隐藏/显示按钮
            string hideButtonText = showLayerList ? "隐藏" : "显示";
            if (DrawButton(hideButtonText))
            {
                showLayerList = !showLayerList;
            }
            
            GUILayout.EndHorizontal();
            
            // 只有在显示状态下才显示全选/全不选按钮和Layer列表
            if (showLayerList)
            {
                GUILayout.BeginHorizontal();
                if (DrawButton("全选"))
                {
                    SelectAllLayers();
                }
                if (DrawButton("全不选"))
                {
                    DeselectAllLayers();
                }
                GUILayout.EndHorizontal();
                
                // 显示Layer选择列表
                if (availableLayers.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"可用的Layer层级 (已选择: {GetSelectedLayerCount()}):", EditorStyles.boldLabel);
                    
                    // 使用固定高度的区域，禁用水平滚动
                    Rect scrollRect = GUILayoutUtility.GetRect(0, 200, GUILayout.ExpandWidth(true));
                    
                    // 计算每行高度
                    float lineHeight = EditorGUIUtility.singleLineHeight + 2;
                    float totalHeight = availableLayers.Count * lineHeight;
                    
                    // 只有当内容超出显示区域时才显示垂直滚动条
                    if (totalHeight > scrollRect.height)
                    {
                        layerScrollPosition = GUI.BeginScrollView(
                            scrollRect, 
                            layerScrollPosition, 
                            new Rect(0, 0, scrollRect.width - 20, totalHeight),
                            false, // 禁用水平滚动条
                            true   // 启用垂直滚动条
                        );
                    }
                    else
                    {
                        GUI.BeginGroup(scrollRect);
                        layerScrollPosition = Vector2.zero;
                    }
                    
                    // 绘制Layer列表
                    for (int i = 0; i < availableLayers.Count; i++)
                    {
                        var layerInfo = availableLayers[i];
                        Rect lineRect = new Rect(0, i * lineHeight, scrollRect.width - (totalHeight > scrollRect.height ? 20 : 0), lineHeight);
                        
                        // 计算各部分的宽度
                        float toggleWidth = 20;
                        float indexWidth = 30;
                        float spacing = 5;
                        float nameWidth = lineRect.width - toggleWidth - indexWidth - spacing * 2;
                        
                        // 复选框
                        Rect toggleRect = new Rect(lineRect.x, lineRect.y, toggleWidth, lineRect.height);
                        bool newSelected = GUI.Toggle(toggleRect, layerInfo.isSelected, "");
                        
                        // Layer索引
                        Rect indexRect = new Rect(toggleRect.xMax + spacing, lineRect.y, indexWidth, lineRect.height);
                        GUI.Label(indexRect, layerInfo.layerIndex.ToString(), EditorStyles.miniLabel);
                        
                        // Layer名称
                        Rect nameRect = new Rect(indexRect.xMax + spacing, lineRect.y, nameWidth, lineRect.height);
                        GUI.Label(nameRect, layerInfo.layerName);
                        
                        // 更新选择状态
                        if (newSelected != layerInfo.isSelected)
                        {
                            var updatedInfo = layerInfo;
                            updatedInfo.isSelected = newSelected;
                            availableLayers[i] = updatedInfo;
                            UpdateLayerMask();
                        }
                    }
                    
                    if (totalHeight > scrollRect.height)
                    {
                        GUI.EndScrollView();
                    }
                    else
                    {
                        GUI.EndGroup();
                    }
                }
            }
            else
            {
                // 隐藏状态下显示简要信息
                if (availableLayers.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Layer列表已隐藏 (总数: {availableLayers.Count}, 已选择: {GetSelectedLayerCount()})");
                }
            }
            
            EditorGUILayout.Space();
            
            // 第二步：搜索参数设置
            EditorGUILayout.LabelField("第二步：搜索参数设置", EditorStyles.boldLabel);
            
            // 显示选中的Layer信息
            GUI.enabled = false;
            EditorGUILayout.TextField("选中的Layer:", GetSelectedLayersText());
            GUI.enabled = true;
            
            // 搜索参数
            parentTransform = (Transform)EditorGUILayout.ObjectField("Parent:", parentTransform, typeof(Transform), true);
            includeInactive = EditorGUILayout.Toggle("Include Inactive:", includeInactive);
            
            EditorGUILayout.Space();
            
            // 搜索按钮
            GUILayout.BeginHorizontal();
            
            GUI.enabled = !isSearching && GetSelectedLayerCount() > 0;
            if (DrawButton("搜索物体"))
            {
                PerformLayerSearch();
            }
            if (DrawButton("组合搜索"))
            {
                PerformLayerSearch2();
            }
            GUI.enabled = true;
            
            GUILayout.EndHorizontal();
            
            // 显示搜索进度
            if (isSearching)
            {
                EditorGUILayout.Space();
                EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20), searchProgress, searchProgressMessage);
            }
        }
        
        private void RefreshLayerList()
        {
            availableLayers.Clear();
            
            // 获取所有定义的Layer
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    availableLayers.Add(new LayerInfo(i, layerName));
                }
            }
            
            UpdateLayerMask();
            Debug.Log($"刷新完成 - 找到 {availableLayers.Count} 个可用的Layer");
        }
        
        private void SelectAllLayers()
        {
            for (int i = 0; i < availableLayers.Count; i++)
            {
                var layerInfo = availableLayers[i];
                layerInfo.isSelected = true;
                availableLayers[i] = layerInfo;
            }
            UpdateLayerMask();
        }
        
        private void DeselectAllLayers()
        {
            for (int i = 0; i < availableLayers.Count; i++)
            {
                var layerInfo = availableLayers[i];
                layerInfo.isSelected = false;
                availableLayers[i] = layerInfo;
            }
            UpdateLayerMask();
        }
        
        private void UpdateLayerMask()
        {
            selectedLayerMask = 0;
            foreach (var layerInfo in availableLayers)
            {
                if (layerInfo.isSelected)
                {
                    selectedLayerMask |= (1 << layerInfo.layerIndex);
                }
            }
        }
        
        private int GetSelectedLayerCount()
        {
            return availableLayers.Count(l => l.isSelected);
        }
        
        private string GetSelectedLayersText()
        {
            var selectedLayers = availableLayers.Where(l => l.isSelected).Select(l => l.layerName);
            return selectedLayers.Any() ? string.Join(", ", selectedLayers) : "无";
        }
        
        private async void PerformLayerSearch(bool clear = true)
        {
            if (clear)
            {
                shareObjectList.Clear();
            }
            
            if (GetSelectedLayerCount() == 0)
            {
                Debug.LogWarning("请先选择至少一个Layer");
                return;
            }

            isSearching = true;
            searchProgress = 0f;
            
            try
            {
                if (parentTransform == null)
                {
                    // 搜索整个场景
                    await SearchLayersInScene();
                }
                else
                {
                    // 在指定父物体下搜索
                    await SearchLayersInChildren(parentTransform);
                }
                
                // 显示结果
                WindowToolGroupReorderableListObject.SetData(shareObjectList);
                Debug.Log($"搜索完成 - 找到 {shareObjectList.Count} 个在选定Layer上的物体");
            }
            catch (Exception e)
            {
                Debug.LogError($"搜索Layer时出错: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"搜索Layer时出错: {e.Message}", "确定");
            }
            finally
            {
                isSearching = false;
            }
        }
        
        private async void PerformLayerSearch2()
        {
            if (shareObjectList.Count == 0)
            {
                PerformLayerSearch(true);
                return;
            }
            
            if (GetSelectedLayerCount() == 0)
            {
                Debug.LogWarning("请先选择至少一个Layer");
                return;
            }
            
            isSearching = true;
            searchProgress = 0f;
            
            try
            {
                List<Object> list = new List<Object>();
                list.AddRange(shareObjectList);

                shareObjectList.Clear();
                
                int total = list.Count;
                for (int i = 0; i < total; i++)
                {
                    GameObject obj = list[i] as GameObject;
                    if (obj != null && IsInSelectedLayers(obj))
                    {
                        shareObjectList.Add(obj);
                    }
                    
                    searchProgress = (float)(i + 1) / total;
                    searchProgressMessage = $"正在组合搜索 {obj?.name} ({i + 1}/{total})";
                    
                    if (i % 10 == 0) // 每处理10个物体刷新一次UI
                    {
                        await System.Threading.Tasks.Task.Yield();
                    }
                }
                
                // 显示结果
                WindowToolGroupReorderableListObject.SetData(shareObjectList);
                Debug.Log($"组合搜索完成 - 找到 {shareObjectList.Count} 个在选定Layer上的物体");
            }
            catch (Exception e)
            {
                Debug.LogError($"组合搜索Layer时出错: {e.Message}");
            }
            finally
            {
                isSearching = false;
            }
        }
        
        private async System.Threading.Tasks.Task SearchLayersInScene()
        {
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(includeInactive);
            int total = allObjects.Length;
            
            for (int i = 0; i < total; i++)
            {
                var go = allObjects[i];
                
                // 过滤掉预制体和资源文件中的对象
                if (EditorUtility.IsPersistent(go))
                    continue;
                
                if (IsInSelectedLayers(go))
                {
                    if (!shareObjectList.Contains(go))
                    {
                        shareObjectList.Add(go);
                    }
                }
                
                searchProgress = (float)(i + 1) / total;
                searchProgressMessage = $"正在搜索 {go.name} ({i + 1}/{total})";
                
                if (i % 50 == 0) // 每处理50个物体刷新一次UI
                {
                    await System.Threading.Tasks.Task.Yield();
                }
            }
        }
        
        private async System.Threading.Tasks.Task SearchLayersInChildren(Transform parent)
        {
            List<Transform> allChildren = new List<Transform>();
            GetAllChildren(parent, allChildren);
            
            int total = allChildren.Count;
            
            for (int i = 0; i < total; i++)
            {
                var child = allChildren[i];
                
                if (includeInactive || child.gameObject.activeInHierarchy)
                {
                    if (IsInSelectedLayers(child.gameObject))
                    {
                        if (!shareObjectList.Contains(child.gameObject))
                        {
                            shareObjectList.Add(child.gameObject);
                        }
                    }
                }
                
                searchProgress = (float)(i + 1) / total;
                searchProgressMessage = $"正在搜索 {child.name} ({i + 1}/{total})";
                
                if (i % 20 == 0) // 每处理20个物体刷新一次UI
                {
                    await System.Threading.Tasks.Task.Yield();
                }
            }
        }
        
        private void GetAllChildren(Transform parent, List<Transform> result)
        {
            result.Add(parent);
            
            for (int i = 0; i < parent.childCount; i++)
            {
                GetAllChildren(parent.GetChild(i), result);
            }
        }
        
        private bool IsInSelectedLayers(GameObject obj)
        {
            if (obj == null)
                return false;
                
            return (selectedLayerMask.value & (1 << obj.layer)) != 0;
        }
        
        void OnEnable()
        {
            RefreshLayerList();
        }
    }
            
}