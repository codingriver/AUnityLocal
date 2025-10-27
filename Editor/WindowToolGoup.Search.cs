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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AUnityLocal.Editor
{
    public abstract class WindowToolGroupSearch : WindowToolGroup
    {
        public override string title { get; } = "搜索";
        public override string tip { get; } = "";

        protected virtual bool IsNameMatch<T>(T o, string searchName, bool exactMatch) where T : UnityEngine.Object
        {
            string objectName = o.IsValid() ? o.name : string.Empty;
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            return objectName.IsMatch(searchName, exactMatch);
        }

        protected virtual bool IsTextMatch<T>(T uitext, string searchName, bool exactMatch)
            where T : UnityEngine.UI.Text
        {
            string objectName = uitext.IsValid() ? uitext.text : string.Empty;
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            return objectName.IsMatch(searchName, exactMatch);
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

        public override void OnGUI(Rect contentRect)
        {
            // 搜索参数
            searchName = EditorGUILayout.TextField("Search Name:", searchName);
            parentTransform =
                (Transform)EditorGUILayout.ObjectField("Parent:", parentTransform, typeof(Transform), true);
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
                EditorUtility.SetDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
            }
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

            //显示
            WindowToolGroupReorderableListObject.SetData(shareObjectList);
        }

        private void PerformSearch2()
        {
            if (shareObjectList.Count == 0)
            {
                PerformSearch(true);
                return;
            }

            List<Object> list = new List<Object>();
            list.AddRange(shareObjectList);

            shareObjectList.Clear();
            foreach (var o in list)
            {
                // 检查当前物体
                if (IsNameMatch(o, searchName, exactMatch))
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
                if (includeInactive && !obj.activeInHierarchy)
                {
                    continue;
                }

                if (IsNameMatch(obj, searchName, exactMatch))
                {
                    shareObjectList.Add(obj);
                }
            }
        }

        private void SearchInChildren(Transform parent)
        {
            // 检查当前物体
            if (IsNameMatch(parent.gameObject, searchName, exactMatch))
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

        public override void OnGUI(Rect contentRect)
        {
            // 搜索参数
            searchText = EditorGUILayout.TextField("Search Text:", searchText);
            parentTransform =
                (Transform)EditorGUILayout.ObjectField("Parent:", parentTransform, typeof(Transform), true);
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
            if (shareObjectList.Count == 0)
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
                if (obj != null && HasMatchingTextComponent(obj, out var textCom))
                {
                    shareObjectList.Add(textCom);
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
                if (!includeInactive && !obj.activeInHierarchy)
                {
                    continue;
                }

                if (HasMatchingTextComponent(obj, out var textCom))
                {
                    shareObjectList.Add(textCom);
                }
            }
        }

        private void SearchInChildren(Transform parent)
        {
            // 检查当前物体
            if (HasMatchingTextComponent(parent.gameObject, out var textCom))
            {
                shareObjectList.Add(textCom);
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

        private bool HasMatchingTextComponent(GameObject obj, out Text matchedTextComponent)
        {
            matchedTextComponent = null;

            if (obj.IsNull())
            {
                return false;
            }

            // 获取所有Text组件（包括子类）
            Text[] textComponents = obj.GetComponents<Text>();

            foreach (Text textComponent in textComponents)
            {
                if (textComponent.IsNull())
                    continue;

                // 如果需要检查Text组件启用状态
                if (checkTextEnabled && !textComponent.enabled)
                    continue;

                // 检查文本内容是否匹配
                if (IsTextMatch(textComponent, searchText, exactMatch))
                {
                    matchedTextComponent = textComponent;
                    return true;
                }
            }

            return false;
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

        public override void OnGUI(Rect contentRect)
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
                        true // 启用垂直滚动条
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
                    Rect lineRect = new Rect(0, i * lineHeight,
                        scrollRect.width - (totalHeight > scrollRect.height ? 20 : 0), lineHeight);

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
            parentTransform =
                (Transform)EditorGUILayout.ObjectField("Parent:", parentTransform, typeof(Transform), true);
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
                        if (obj.IsValid())
                        {
                            Debug.LogWarning($"无法设置 {go.name} 上的 {component.GetType().Name} 组件状态: {e.Message}");
                        }
                        else
                        {
                            Debug.LogWarning(e);
                        }
                    }
                }
            }

            string action = enabled ? "启用" : "禁用";
            Debug.Log($"{action}操作完成 - 共{action}了 {modifiedCount} 个 {currentSearchType.Name} 组件");

            // 标记场景为已修改
            if (modifiedCount > 0)
            {
                EditorUtility.SetDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
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
                    if (type.Name.IsMatch(componentNameSearch))
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
                var com = HasTargetComponent(obj);
                if (obj != null && com!=null)
                {
                    shareObjectList.Add(com);
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

                    if (!shareObjectList.Contains(unityComponent))
                    {
                        shareObjectList.Add(unityComponent);
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
            var com = HasTargetComponent(parent.gameObject);
            if (com!=null)
            {
                if (!shareObjectList.Contains(com))
                {
                    shareObjectList.Add(com);
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

        private Component HasTargetComponent(GameObject obj)
        {
            if (selectedComponentType == null || obj == null)
                return null;

            return obj.GetComponent(selectedComponentType) ;
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

        public override void OnGUI(Rect contentRect)
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
                            true // 启用垂直滚动条
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
                        Rect lineRect = new Rect(0, i * lineHeight,
                            scrollRect.width - (totalHeight > scrollRect.height ? 20 : 0), lineHeight);

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
                    EditorGUILayout.LabelField(
                        $"Layer列表已隐藏 (总数: {availableLayers.Count}, 已选择: {GetSelectedLayerCount()})");
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
            parentTransform =
                (Transform)EditorGUILayout.ObjectField("Parent:", parentTransform, typeof(Transform), true);
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
            window.SetProgressBar(searchProgress, "开始搜索...");
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
                window.SetProgressBarShow(false);
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
            window.SetProgressBar(searchProgress, "开始搜索...");
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
                    window.SetProgressBar(searchProgress, searchProgressMessage);
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
                window.SetProgressBarShow(false);
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
                searchProgressMessage = $"正在搜索 {go?.name} ({i + 1}/{total})";
                window.SetProgressBar(searchProgress, searchProgressMessage);
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
                window.SetProgressBar(searchProgress, searchProgressMessage);
                if (i % 20 == 0) // 每处理20个物体刷新一次UI
                {
                    await System.Threading.Tasks.Task.Yield();
                }
            }
        }

        private void GetAllChildren(Transform parent, List<Transform> result)
        {
            if (!parent.IsValid())
            {
                return;
            }

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

    [WindowToolGroup(504, WindowArea.RightMid)]
    public class WindowToolGroupSearchMissing : WindowToolGroupSearch
    {
        public override string title { get; } = "Missing组件搜索";
        public override string tip { get; } = "搜索场景或Prefab中的Missing组件";

        // 搜索选项
        private bool searchInScene = false;
        private bool searchInPrefabs = true;
        private bool includeInactive = true;
        private string searchDirectory = "Assets/";
        private Transform parentTransform = null;

        // 检查参数
        private int batchSize = 40;

        // 进度相关
        private bool isSearching = false;
        private float searchProgress = 0f;
        private string searchProgressMessage = "";
        private CancellationTokenSource cancellationTokenSource;

        // 结果相关
        private List<MissingComponentInfo> missingComponents = new List<MissingComponentInfo>();
        private Vector2 resultScrollPosition = Vector2.zero;
        private bool showResults = false;
        private int totalChecked = 0;
        private int missingCount = 0;

        // 日志相关
        private StringBuilder logBuilder = new StringBuilder();
        private string logFilePath = "";

        private struct MissingComponentInfo
        {
            public string assetPath;
            public string gameObjectPath;
            public string componentType;
            public GameObject gameObject; // 用于场景对象

            public MissingComponentInfo(string assetPath, string gameObjectPath, string componentType,
                GameObject gameObject = null)
            {
                this.assetPath = assetPath;
                this.gameObjectPath = gameObjectPath;
                this.componentType = componentType;
                this.gameObject = gameObject;
            }
        }

        private SearchTarget searchTarget = SearchTarget.Assets;

        // 将原来的两个bool改为一个枚举
        public enum SearchTarget
        {
            Scene, // 场景中的物体
            Assets // Prefab文件
        }

        public override void OnGUI(Rect contentRect)
        {
            // 第一步：选择搜索范围
            EditorGUILayout.LabelField("搜索选项", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // 单选按钮组
            EditorGUILayout.LabelField("检查目标:", EditorStyles.label);
            EditorGUI.indentLevel++;

            searchTarget = (SearchTarget)EditorGUILayout.EnumPopup("选择检查目标", searchTarget);
            searchInScene = (searchTarget == SearchTarget.Scene);
            searchInPrefabs = (searchTarget == SearchTarget.Assets);

            if (searchInScene)
            {
                parentTransform =
                    (Transform)EditorGUILayout.ObjectField("限制在父对象下:", parentTransform, typeof(Transform), true);
                includeInactive = EditorGUILayout.Toggle("包含未激活对象:", includeInactive);
            }

            if (searchInPrefabs)
            {
                EditorGUILayout.BeginHorizontal();
                searchDirectory = EditorGUILayout.TextField("Prefab搜索目录:", searchDirectory);

                GUIContent folderContent = new GUIContent("浏览", EditorGUIUtility.IconContent("Folder Icon").image);
                if (GUILayout.Button(folderContent, GUILayout.Width(70), GUILayout.Height(20)))
                {
                    searchDirectory = Tools.SelectFolder(searchDirectory, "选择Prefab目录");
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 搜索按钮
            GUILayout.BeginHorizontal();

            GUI.enabled = !isSearching && (searchInScene || searchInPrefabs);
            if (DrawButton("开始检查"))
            {
                StartMissingComponentCheck();
            }

            // if (DrawButton("组合检查"))
            // {
            //     StartCombinedCheck();
            // }

            GUI.enabled = true;

            if (isSearching && DrawButton("取消"))
            {
                CancelCheck();
            }

            GUILayout.EndHorizontal();

            // 显示进度
            if (isSearching)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"进度: {searchProgressMessage}");
                EditorGUILayout.LabelField($"已检查: {totalChecked}, 发现Missing: {missingCount}");
            }

            // 显示结果
            if (missingComponents.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("第三步：检查结果", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();
                if (DrawButton($"显示结果 ({missingComponents.Count})"))
                {
                    showResults = !showResults;
                }

                if (DrawButton("清除结果"))
                {
                    ClearResults();
                }

                if (DrawButton("导出日志"))
                {
                    SaveLog();
                }

                GUILayout.EndHorizontal();

                if (showResults)
                {
                    DrawResultsList();
                }
            }
        }

        private void DrawResultsList()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Missing组件列表 (总计: {missingComponents.Count})", EditorStyles.boldLabel);

            Rect scrollRect = GUILayoutUtility.GetRect(0, 300, GUILayout.ExpandWidth(true));
            float lineHeight = 90f; // 每个结果项的高度
            float totalHeight = missingComponents.Count * lineHeight;

            resultScrollPosition = GUI.BeginScrollView(
                scrollRect,
                resultScrollPosition,
                new Rect(0, 0, scrollRect.width - 20, totalHeight),
                false,
                true
            );

            for (int i = 0; i < missingComponents.Count; i++)
            {
                var missing = missingComponents[i];
                Rect itemRect = new Rect(0, i * lineHeight, scrollRect.width - 20, lineHeight - 5);

                GUI.Box(itemRect, "", EditorStyles.helpBox);

                // 内容区域
                Rect contentRect = new Rect(itemRect.x + 5, itemRect.y + 5, itemRect.width - 10, itemRect.height - 10);

                // 资源路径
                Rect pathRect = new Rect(contentRect.x, contentRect.y, contentRect.width, 16);
                GUI.Label(pathRect, $"资源: {missing.assetPath}", EditorStyles.boldLabel);

                // 对象路径
                Rect objPathRect = new Rect(contentRect.x, pathRect.yMax + 2, contentRect.width, 16);
                GUI.Label(objPathRect, $"对象: {missing.gameObjectPath}");

                // 组件类型
                Rect componentRect = new Rect(contentRect.x, objPathRect.yMax + 2, contentRect.width, 16);
                GUI.Label(componentRect, $"组件: {missing.componentType}");

                // 按钮
                Rect buttonRect = new Rect(contentRect.x, componentRect.yMax + 5, 80, 18);

                if (GUI.Button(buttonRect, "选择"))
                {
                    SelectMissingComponent(missing);
                }

                buttonRect.x += 85;
                if (GUI.Button(buttonRect, "复制路径"))
                {
                    EditorGUIUtility.systemCopyBuffer = missing.assetPath;
                }

                if (missing.gameObject != null)
                {
                    buttonRect.x += 85;
                    if (GUI.Button(buttonRect, "定位场景"))
                    {
                        Selection.activeGameObject = missing.gameObject;
                        EditorGUIUtility.PingObject(missing.gameObject);
                    }
                }
            }

            GUI.EndScrollView();
        }

        private void SelectMissingComponent(MissingComponentInfo missing)
        {
            if (missing.gameObject != null)
            {
                // 场景对象
                Selection.activeGameObject = missing.gameObject;
                EditorGUIUtility.PingObject(missing.gameObject);
            }
            else
            {
                // Prefab对象
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(missing.assetPath);
                if (prefab != null)
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            }
        }

        private async void StartMissingComponentCheck(bool clear = true)
        {
            if (clear)
            {
                shareObjectList.Clear();
                ClearResults();
            }

            if (!searchInScene && !searchInPrefabs)
            {
                Debug.LogWarning("请至少选择一种搜索范围");
                return;
            }

            isSearching = true;
            searchProgress = 0f;
            totalChecked = 0;
            missingCount = 0;
            cancellationTokenSource = new CancellationTokenSource();

            // 初始化日志
            InitializeLog();

            window.SetProgressBar(searchProgress, "开始检查Missing组件...");

            try
            {
                if (searchInScene)
                {
                    await CheckMissingInScene();
                }

                if (searchInPrefabs && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await CheckMissingInPrefabs();
                }

                // 更新共享列表
                foreach (var missing in missingComponents)
                {
                    if (missing.gameObject != null && !shareObjectList.Contains(missing.gameObject))
                    {
                        shareObjectList.Add(missing.gameObject);
                    }
                }

                WindowToolGroupReorderableListObject.SetData(shareObjectList);
                Debug.Log($"Missing组件检查完成 - 检查了 {totalChecked} 个对象，发现 {missingCount} 个Missing组件");
            }
            catch (Exception e)
            {
                Debug.LogError($"检查Missing组件时出错: {e.Message}");
                AddLog($"Error: {e.Message}");
            }
            finally
            {
                isSearching = false;
                window.SetProgressBarShow(false);
                cancellationTokenSource = null;
            }
        }

        private async void StartCombinedCheck()
        {
            if (shareObjectList.Count == 0)
            {
                StartMissingComponentCheck(true);
                return;
            }

            isSearching = true;
            searchProgress = 0f;
            totalChecked = 0;
            missingCount = 0;
            cancellationTokenSource = new CancellationTokenSource();

            window.SetProgressBar(searchProgress, "开始组合检查...");

            try
            {
                List<Object> objectsToCheck = new List<Object>(shareObjectList);
                shareObjectList.Clear();
                missingComponents.Clear();

                int total = objectsToCheck.Count;
                for (int i = 0; i < total; i++)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    GameObject obj = objectsToCheck[i] as GameObject;
                    if (obj != null)
                    {
                        bool hasMissing = CheckGameObjectForMissingComponents(obj, GetAssetPath(obj), obj);
                        if (hasMissing)
                        {
                            shareObjectList.Add(obj);
                        }

                        totalChecked++;
                    }

                    searchProgress = (float)(i + 1) / total;
                    searchProgressMessage = $"正在组合检查 {obj?.name} ({i + 1}/{total})";
                    window.SetProgressBar(searchProgress, searchProgressMessage);

                    if (i % 10 == 0)
                    {
                        await System.Threading.Tasks.Task.Yield();
                    }
                }

                WindowToolGroupReorderableListObject.SetData(shareObjectList);
                Debug.Log($"组合检查完成 - 检查了 {totalChecked} 个对象，发现 {missingCount} 个Missing组件");
            }
            catch (Exception e)
            {
                Debug.LogError($"组合检查时出错: {e.Message}");
            }
            finally
            {
                isSearching = false;
                window.SetProgressBarShow(false);
                cancellationTokenSource = null;
            }
        }

        private async System.Threading.Tasks.Task CheckMissingInScene()
        {
            GameObject[] allObjects;

            if (parentTransform == null)
            {
                allObjects = Object.FindObjectsOfType<GameObject>(includeInactive);
            }
            else
            {
                List<GameObject> childObjects = new List<GameObject>();
                GetAllChildren(parentTransform, childObjects);
                allObjects = childObjects.ToArray();
            }

            int total = allObjects.Length;

            for (int i = 0; i < total; i++)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                var go = allObjects[i];

                // 过滤掉预制体和资源文件中的对象
                if (EditorUtility.IsPersistent(go))
                    continue;

                if (!includeInactive && !go.activeInHierarchy)
                    continue;

                CheckGameObjectForMissingComponents(go, "Scene", go);
                totalChecked++;

                searchProgress = (float)(i + 1) / total;
                searchProgressMessage = $"正在检查场景对象 {go.name} ({i + 1}/{total})";
                window.SetProgressBar(searchProgress, searchProgressMessage);

                if (i % 50 == 0)
                {
                    await System.Threading.Tasks.Task.Yield();
                }
            }
        }

        private async System.Threading.Tasks.Task CheckMissingInPrefabs()
        {
            var prefabFiles = Directory.GetFiles(searchDirectory, "*.prefab", SearchOption.AllDirectories);
            int total = prefabFiles.Length;

            for (int i = 0; i < total; i++)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                string prefabPath = prefabFiles[i].Replace('\\', '/');
                if (prefabPath.StartsWith(Application.dataPath))
                {
                    prefabPath = "Assets" + prefabPath.Substring(Application.dataPath.Length);
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    CheckPrefabForMissingComponents(prefab, prefabPath);
                    totalChecked++;
                }

                searchProgress = (float)(i + 1) / total;
                searchProgressMessage = $"正在检查Prefab {Path.GetFileNameWithoutExtension(prefabPath)} ({i + 1}/{total})";
                window.SetProgressBar(searchProgress, searchProgressMessage);

                if (i % batchSize == 0)
                {
                    await System.Threading.Tasks.Task.Yield();
                }
            }
        }

        private void CheckPrefabForMissingComponents(GameObject prefab, string assetPath)
        {
            // 检查根对象
            CheckGameObjectForMissingComponents(prefab, assetPath, prefab);

            // 检查所有子对象
            Transform[] children = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child.gameObject != prefab) // 跳过根对象，已经检查过了
                {
                    CheckGameObjectForMissingComponents(child.gameObject, assetPath, prefab);
                }
            }
        }

        private bool CheckGameObjectForMissingComponents(GameObject go, string assetPath, GameObject gameObject = null)
        {
            bool hasMissing = false;
            var components = go.GetComponents<Component>();
            string objectPath = go.transform.FullName();

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    string componentType = $"Missing Component at index {i}";

                    var missingInfo = new MissingComponentInfo(
                        assetPath,
                        objectPath,
                        componentType,
                        gameObject
                    );

                    missingComponents.Add(missingInfo);
                    missingCount++;
                    hasMissing = true;

                    AddLog($"Found missing component: {assetPath} -> {objectPath} (Index: {i})");
                }
            }

            return hasMissing;
        }

        private void GetAllChildren(Transform parent, List<GameObject> children)
        {
            children.Add(parent.gameObject);

            for (int i = 0; i < parent.childCount; i++)
            {
                GetAllChildren(parent.GetChild(i), children);
            }
        }

        private string GetAssetPath(GameObject go)
        {
            if (go == null) return "Scene";

            if (EditorUtility.IsPersistent(go))
            {
                return AssetDatabase.GetAssetPath(go);
            }

            return "Scene";
        }

        private void CancelCheck()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                AddLog("检查已被用户取消");
            }
        }

        private void ClearResults()
        {
            missingComponents.Clear();
            showResults = false;
            totalChecked = 0;
            missingCount = 0;
            logBuilder.Clear();
        }

        private void InitializeLog()
        {
            logBuilder.Clear();
            AddLog($"Missing组件检查开始 - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            AddLog($"搜索场景: {searchInScene}");
            AddLog($"搜索Prefab: {searchInPrefabs}");
            if (searchInPrefabs)
            {
                AddLog($"Prefab目录: {searchDirectory}");
            }

            if (parentTransform != null)
            {
                AddLog($"限制在父对象: {parentTransform.FullName()}");
            }

            AddLog($"包含未激活对象: {includeInactive}");
            AddLog("----------------------------------------");
        }

        private void AddLog(string message)
        {
            logBuilder.AppendLine(message);
        }

        private void SaveLog()
        {
            StringBuilder finalLog = new StringBuilder(logBuilder.ToString());
            finalLog.AppendLine("----------------------------------------");
            finalLog.AppendLine($"检查完成时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            finalLog.AppendLine($"总计检查对象: {totalChecked}");
            finalLog.AppendLine($"发现Missing组件: {missingCount}");
            finalLog.AppendLine($"涉及资源数量: {missingComponents.GroupBy(m => m.assetPath).Count()}");
            string fileName = $"MissingComponentCheck_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            SaveLog(finalLog, fileName);
        }

        private bool DrawButton(string text)
        {
            return GUILayout.Button(text, GUILayout.Height(25));
        }

        public override void OnDestroy()
        {
            CancelCheck();
            base.OnDestroy();
        }
    }


    [WindowToolGroup(505, WindowArea.RightMid)]
    public class WindowToolGroupSearchMissingSprite : WindowToolGroup
    {
        public override string title { get; } = "Missing Sprite搜索";
        public override string tip { get; } = "搜索场景或Prefab中的Missing Sprite引用";

        // 搜索选项
        private bool searchInScene = false;
        private bool searchInPrefabs = true;
        private bool includeInactive = true;
        private string searchDirectory = "Assets/";
        private Transform parentTransform = null;

        // 检查参数
        private int batchSize = 40;

        // 进度相关
        private bool isSearching = false;
        private float searchProgress = 0f;
        private string searchProgressMessage = "";
        private CancellationTokenSource cancellationTokenSource;

        // 结果相关
        private List<MissingSpriteInfo> missingSprites = new List<MissingSpriteInfo>();
        private Vector2 resultScrollPosition = Vector2.zero;
        private bool showResults = false;
        private int totalChecked = 0;
        private int missingCount = 0;

        // 日志相关
        private StringBuilder logBuilder = new StringBuilder();
        private string logFilePath = "";

        // 检查类型选项
        private CheckType selectedCheckType = CheckType.Both;

        private enum CheckType
        {
            SpriteRenderer,
            UIImage,
            Both
        }

        private struct MissingSpriteInfo
        {
            public string assetPath;
            public string gameObjectPath;
            public string componentType;
            public GameObject gameObject; // 用于场景对象

            public MissingSpriteInfo(string assetPath, string gameObjectPath, string componentType,
                GameObject gameObject = null)
            {
                this.assetPath = assetPath;
                this.gameObjectPath = gameObjectPath;
                this.componentType = componentType;
                this.gameObject = gameObject;
            }
        }

        private SearchTarget searchTarget = SearchTarget.Assets;

        // 将原来的两个bool改为一个枚举
        public enum SearchTarget
        {
            Scene, // 场景中的物体
            Assets // Prefab文件
        }

        public override void OnGUI(Rect contentRect)
        {
            // 第一步：选择搜索范围
            EditorGUILayout.LabelField("搜索选项", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // 单选按钮组
            EditorGUILayout.LabelField("检查目标:", EditorStyles.label);
            EditorGUI.indentLevel++;

            searchTarget = (SearchTarget)EditorGUILayout.EnumPopup("选择检查目标", searchTarget);
            searchInScene = (searchTarget == SearchTarget.Scene);
            searchInPrefabs = (searchTarget == SearchTarget.Assets);

            if (searchInScene)
            {
                parentTransform =
                    (Transform)EditorGUILayout.ObjectField("限制在父对象下:", parentTransform, typeof(Transform), true);
                includeInactive = EditorGUILayout.Toggle("包含未激活对象:", includeInactive);
            }

            if (searchInPrefabs)
            {
                EditorGUILayout.BeginHorizontal();
                searchDirectory = EditorGUILayout.TextField("Prefab搜索目录:", searchDirectory);

                GUIContent folderContent = new GUIContent("浏览", EditorGUIUtility.IconContent("Folder Icon").image);
                if (GUILayout.Button(folderContent, GUILayout.Width(70), GUILayout.Height(20)))
                {
                    searchDirectory = Tools.SelectFolder(searchDirectory, "选择Prefab目录");
                }

                EditorGUILayout.EndHorizontal();
            }

            // 检查类型选项
            selectedCheckType = (CheckType)EditorGUILayout.EnumPopup("检查类型:", selectedCheckType);

            EditorGUILayout.Space();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 搜索按钮
            GUILayout.BeginHorizontal();

            GUI.enabled = !isSearching && (searchInScene || searchInPrefabs);
            if (DrawButton("开始检查"))
            {
                StartMissingSpriteCheck();
            }

            GUI.enabled = true;

            if (isSearching && DrawButton("取消"))
            {
                CancelCheck();
            }

            GUILayout.EndHorizontal();

            // 显示进度
            if (isSearching)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"进度: {searchProgressMessage}");
                EditorGUILayout.LabelField($"已检查: {totalChecked}, 发现Missing Sprite: {missingCount}");
            }

            // 显示结果
            if (missingSprites.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("第三步：检查结果", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();
                if (DrawButton($"显示结果 ({missingSprites.Count})"))
                {
                    showResults = !showResults;
                }

                if (DrawButton("清除结果"))
                {
                    ClearResults();
                }

                if (DrawButton("导出日志"))
                {
                    SaveLog();
                }

                GUILayout.EndHorizontal();

                if (showResults)
                {
                    DrawResultsList();
                }
            }
        }

        private void DrawResultsList()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Missing Sprite列表 (总计: {missingSprites.Count})", EditorStyles.boldLabel);

            Rect scrollRect = GUILayoutUtility.GetRect(0, 300, GUILayout.ExpandWidth(true));
            float lineHeight = 90f; // 每个结果项的高度
            float totalHeight = missingSprites.Count * lineHeight;

            resultScrollPosition = GUI.BeginScrollView(
                scrollRect,
                resultScrollPosition,
                new Rect(0, 0, scrollRect.width - 20, totalHeight),
                false,
                true
            );

            for (int i = 0; i < missingSprites.Count; i++)
            {
                var missing = missingSprites[i];
                Rect itemRect = new Rect(0, i * lineHeight, scrollRect.width - 20, lineHeight - 5);

                GUI.Box(itemRect, "", EditorStyles.helpBox);

                // 内容区域
                Rect contentRect = new Rect(itemRect.x + 5, itemRect.y + 5, itemRect.width - 10, itemRect.height - 10);

                // 资源路径
                Rect pathRect = new Rect(contentRect.x, contentRect.y, contentRect.width, 16);
                GUI.Label(pathRect, $"资源: {missing.assetPath}", EditorStyles.boldLabel);

                // 对象路径
                Rect objPathRect = new Rect(contentRect.x, pathRect.yMax + 2, contentRect.width, 16);
                GUI.Label(objPathRect, $"对象: {missing.gameObjectPath}");

                // 组件类型
                Rect componentRect = new Rect(contentRect.x, objPathRect.yMax + 2, contentRect.width, 16);
                GUI.Label(componentRect, $"组件: {missing.componentType}");

                // 按钮
                Rect buttonRect = new Rect(contentRect.x, componentRect.yMax + 5, 80, 18);

                if (GUI.Button(buttonRect, "选择"))
                {
                    SelectMissingSprite(missing);
                }

                buttonRect.x += 85;
                if (GUI.Button(buttonRect, "复制路径"))
                {
                    EditorGUIUtility.systemCopyBuffer = missing.assetPath;
                }

                if (missing.gameObject != null)
                {
                    buttonRect.x += 85;
                    if (GUI.Button(buttonRect, "定位场景"))
                    {
                        Selection.activeGameObject = missing.gameObject;
                        EditorGUIUtility.PingObject(missing.gameObject);
                    }
                }
            }

            GUI.EndScrollView();
        }

        private void SelectMissingSprite(MissingSpriteInfo missing)
        {
            if (missing.gameObject != null)
            {
                // 场景对象
                Selection.activeGameObject = missing.gameObject;
                EditorGUIUtility.PingObject(missing.gameObject);
            }
            else
            {
                // Prefab对象
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(missing.assetPath);
                if (prefab != null)
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            }
        }

        private async void StartMissingSpriteCheck(bool clear = true)
        {
            if (clear)
            {
                shareObjectList.Clear();
                ClearResults();
            }

            if (!searchInScene && !searchInPrefabs)
            {
                Debug.LogWarning("请至少选择一种搜索范围");
                return;
            }

            isSearching = true;
            searchProgress = 0f;
            totalChecked = 0;
            missingCount = 0;
            cancellationTokenSource = new CancellationTokenSource();

            // 初始化日志
            InitializeLog();

            searchProgressMessage = $"开始检查Missing Sprite...";
            window.SetProgressBar(searchProgress);
            window.SetStatusInfo(searchProgressMessage);

            try
            {
                if (searchInScene)
                {
                    await CheckMissingSpriteInScene();
                }

                if (searchInPrefabs && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await CheckMissingSpriteInPrefabs();
                }

                // 更新共享列表
                foreach (var missing in missingSprites)
                {
                    if (missing.gameObject != null && !shareObjectList.Contains(missing.gameObject))
                    {
                        shareObjectList.Add(missing.gameObject);
                    }
                }

                WindowToolGroupReorderableListObject.SetData(shareObjectList);
                Debug.Log($"Missing Sprite检查完成 - 检查了 {totalChecked} 个对象，发现 {missingCount} 个Missing Sprite");
                window.SetStatusInfo($"Missing Sprite检查完成 - 检查了 {totalChecked} 个对象，发现 {missingCount} 个Missing Sprite");
            }
            catch (Exception e)
            {
                Debug.LogError($"检查Missing Sprite时出错: {e.Message}");
                AddLog($"Error: {e.Message}");
                window.SetStatusInfo($"Missing Sprite检查异常");
            }
            finally
            {
                isSearching = false;
                window.SetProgressBarShow(false);
                cancellationTokenSource = null;
            }
        }


        private async System.Threading.Tasks.Task CheckMissingSpriteInScene()
        {
            GameObject[] allObjects;

            if (parentTransform == null)
            {
                // allObjects = Object.FindObjectsOfType<GameObject>(includeInactive);
                allObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            }
            else
            {
                allObjects = new GameObject[] { parentTransform.gameObject };
            }

            int total = allObjects.Length;

            for (int i = 0; i < total; i++)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                var go = allObjects[i];

                // 过滤掉预制体和资源文件中的对象
                if (EditorUtility.IsPersistent(go))
                    continue;

                if (!includeInactive && !go.activeInHierarchy)
                    continue;

                CheckGameObjectForMissingSprites(go, "Scene", null, includeInactive);
                totalChecked++;

                searchProgress = (float)(i + 1) / total;
                searchProgressMessage = $"正在检查场景对象 {go.name} ({i + 1}/{total})";
                window.SetProgressBar(searchProgress, $"({i + 1}/{total})");
                window.SetStatusInfo(searchProgressMessage);

                if (i % 50 == 0)
                {
                    await System.Threading.Tasks.Task.Yield();
                }
            }
        }

        private async System.Threading.Tasks.Task CheckMissingSpriteInPrefabs()
        {
            var prefabFiles = Directory.GetFiles(searchDirectory, "*.prefab", SearchOption.AllDirectories);
            int total = prefabFiles.Length;

            for (int i = 0; i < total; i++)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                string prefabPath = prefabFiles[i].Replace('\\', '/');

                // 确保路径是相对于Assets的
                if (prefabPath.StartsWith(Application.dataPath))
                {
                    prefabPath = "Assets" + prefabPath.Substring(Application.dataPath.Length);
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    CheckGameObjectForMissingSprites(prefab, prefabPath, prefab);
                    totalChecked++;
                }

                searchProgress = (float)(i + 1) / total;
                window.SetProgressBar(searchProgress, $"({i + 1}/{total})");
                searchProgressMessage = $"正在检查Prefab {Path.GetFileName(prefabPath)} ({i + 1}/{total})";
                window.SetStatusInfo(searchProgressMessage);

                if (i % batchSize == 0)
                {
                    await System.Threading.Tasks.Task.Yield();
                }
            }
        }

        private bool CheckGameObjectForMissingSprites(GameObject go, string assetPath, GameObject sceneObject = null,
            bool includeInactive = true)
        {
            bool hasMissing = false;

            // 检查SpriteRenderer
            if (selectedCheckType == CheckType.SpriteRenderer || selectedCheckType == CheckType.Both)
            {
                var spriteRenderers = go.GetComponentsInChildren<SpriteRenderer>(includeInactive);
                foreach (var sr in spriteRenderers)
                {
                    if (sr.sprite == null)
                    {
                        SerializedProperty sp = new SerializedObject(sr).FindProperty("m_Sprite");
                        if (sp != null && sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                        {
                            string componentPath = sr.gameObject.transform.FullName();
                            var missingInfo = new MissingSpriteInfo(
                                assetPath,
                                componentPath,
                                "SpriteRenderer",
                                sceneObject ?? sr.gameObject
                            );

                            missingSprites.Add(missingInfo);
                            missingCount++;
                            hasMissing = true;

                            AddLog($"Missing Sprite in SpriteRenderer: {assetPath} -> {componentPath}");
                        }
                    }
                }
            }

            // 检查UI Image
            if (selectedCheckType == CheckType.UIImage || selectedCheckType == CheckType.Both)
            {
                var images = go.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var img in images)
                {
                    if (img.sprite == null)
                    {
                        SerializedProperty sp = new SerializedObject(img).FindProperty("m_Sprite");
                        if (sp != null && sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                        {
                            string componentPath = img.gameObject.transform.FullName();
                            var missingInfo = new MissingSpriteInfo(
                                assetPath,
                                componentPath,
                                "UI.Image",
                                sceneObject ?? img.gameObject
                            );

                            missingSprites.Add(missingInfo);
                            missingCount++;
                            hasMissing = true;

                            AddLog($"Missing Sprite in UI.Image: {assetPath} -> {componentPath}");
                        }
                    }
                }
            }

            return hasMissing;
        }

        private void CancelCheck()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                Debug.Log("Missing Sprite检查已取消");
                AddLog("检查已取消");
            }
        }

        private void ClearResults()
        {
            missingSprites.Clear();
            showResults = false;
            totalChecked = 0;
            missingCount = 0;
            logBuilder.Clear();
        }

        private void InitializeLog()
        {
            logBuilder.Clear();
            logBuilder.AppendLine($"Missing Sprite检查开始 - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logBuilder.AppendLine($"检查范围: {(searchInScene ? "场景" : "")} {(searchInPrefabs ? "Prefab" : "")}");
            logBuilder.AppendLine($"检查类型: {selectedCheckType}");

            if (searchInPrefabs)
            {
                logBuilder.AppendLine($"Prefab目录: {searchDirectory}");
            }

            if (searchInScene && parentTransform != null)
            {
                logBuilder.AppendLine($"限制父对象: {parentTransform.FullName()}");
            }

            logBuilder.AppendLine("----------------------------------------");
        }

        private void AddLog(string message)
        {
            logBuilder.AppendLine($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        }

        private void SaveLog()
        {
            // 添加汇总信息
            logBuilder.AppendLine("----------------------------------------");
            logBuilder.AppendLine($"检查完成 - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logBuilder.AppendLine($"总计检查对象: {totalChecked}");
            logBuilder.AppendLine($"发现Missing Sprite: {missingCount}");
            string fileName = $"MissingSprite_Log_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            SaveLog(logBuilder, fileName);
        }


        private bool DrawButton(string text)
        {
            return GUILayout.Button(text, GUILayout.Height(25));
        }

        public override void OnDestroy()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource = null;
            }

            base.OnDestroy();
        }
    }

    /// <summary>
    /// 资源依赖查看工具
    /// </summary>
    [WindowToolGroup(506, WindowArea.RightMid)]
    public class AssetDependencyTool : WindowToolGroup
    {
        public override string title { get; } = "资源依赖查看器";
        public override string tip { get; } = "查看指定资源的依赖关系";

        private Object targetAsset;
        private int depth = 0;
        private Vector2 scrollPosition;
        private List<string> dependencies = new List<string>();
        private bool showFullPath = false;
        private string searchFilter = "";
        private bool isChecking = false;

        // 过滤选项
        private bool filterScripts = true; // 默认过滤脚本
        private bool filterCode = true; // 默认过滤代码文件
        private bool showFilterOptions = false;

        // 层级显示选项
        private bool showLayerInfo = false; // 是否显示层级信息

        // 脚本和代码文件扩展名
        private readonly HashSet<string> scriptExtensions = new HashSet<string>
        {
            ".cs", ".js", ".boo", ".dll", ".asmdef", ".asmref"
        };

        private readonly HashSet<string> codeExtensions = new HashSet<string>
        {
            ".cs", ".js", ".boo", ".cpp", ".c", ".h", ".hpp", ".cc", ".cxx",
            ".py", ".java", ".kt", ".swift", ".ts", ".jsx", ".vue", ".php",
            ".rb", ".go", ".rs", ".scala", ".sh", ".bat", ".ps1"
        };

        public override void OnGUI(Rect contentRect)
        {
            GUILayout.BeginVertical();

            // 资源选择区域
            GUILayout.Label("目标资源:", EditorStyles.boldLabel);
            targetAsset = EditorGUILayout.ObjectField(targetAsset, typeof(Object), false);

            GUILayout.Space(5);

            // 深度设置
            GUILayout.BeginHorizontal();
            GUILayout.Label("深度层次:", GUILayout.Width(60));
            depth = EditorGUILayout.IntField(depth, GUILayout.Width(50));
            depth = Mathf.Max(0, depth);

            string depthDescription = depth == 0 ? "(全部依赖)" : $"(第1层到第{depth}层)";
            GUILayout.Label(depthDescription, EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 基本选项设置
            GUILayout.BeginHorizontal();
            showFullPath = EditorGUILayout.Toggle("显示完整路径", showFullPath);
            showLayerInfo = EditorGUILayout.Toggle("显示层级信息", showLayerInfo);
            GUILayout.EndHorizontal();

            // 过滤选项区域
            GUILayout.BeginHorizontal();
            showFilterOptions = EditorGUILayout.Foldout(showFilterOptions, "过滤选项", true);
            GUILayout.EndHorizontal();

            if (showFilterOptions)
            {
                EditorGUI.indentLevel++;

                GUILayout.BeginVertical("box");

                // 文件类型过滤
                GUILayout.Label("文件类型过滤:", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();
                bool newFilterScripts = EditorGUILayout.Toggle("过滤脚本文件", filterScripts);
                if (newFilterScripts != filterScripts)
                {
                    filterScripts = newFilterScripts;
                }

                if (GUILayout.Button("?", GUILayout.Width(20)))
                {
                    EditorUtility.DisplayDialog("脚本文件",
                        "包括: .cs, .js, .boo, .dll, .asmdef, .asmref 等Unity脚本相关文件", "确定");
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                bool newFilterCode = EditorGUILayout.Toggle("过滤代码文件", filterCode);
                if (newFilterCode != filterCode)
                {
                    filterCode = newFilterCode;
                }

                if (GUILayout.Button("?", GUILayout.Width(20)))
                {
                    EditorUtility.DisplayDialog("代码文件",
                        "包括: .cs, .cpp, .py, .java, .ts, .php 等各种编程语言文件", "确定");
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                EditorGUI.indentLevel--;
            }

            // 搜索过滤
            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(40));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("清空", GUILayout.Width(40)))
            {
                searchFilter = "";
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 操作按钮
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("检查依赖"))
            {
                RefreshDependencies();
                
            }

            GUI.enabled = dependencies.Count > 0;
            if (GUILayout.Button("复制结果"))
            {
                CopyDependenciesToClipboard();
            }

            GUI.enabled = true;
            GUI.enabled = shareStringBuilder.Length > 0;
            if (GUILayout.Button("复制带层级结果"))
            {
                EditorGUIUtility.systemCopyBuffer = shareStringBuilder.ToString();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            shareStringList.Clear();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 按层级显示依赖
        /// </summary>
        private void DisplayDependenciesByLayer(List<string> filteredDeps)
        {
            var layerDeps = GetDependenciesWithLayers();
            // 按层级分组
            for (int layer = 1; layer <= depth; layer++)
            {
                var layerItems = layerDeps.Where(item => item.layer == layer && filteredDeps.Contains(item.path))
                    .ToList();

                if (layerItems.Count > 0)
                {
                    shareStringBuilder.AppendLine($"第 {layer} 层 ({layerItems.Count}个)");
                    // 按类型再次分组
                    var typeGroups = layerItems.GroupBy(item => GetFileCategory(item.path));

                    foreach (var typeGroup in typeGroups.OrderBy(g => g.Key))
                    {
                        if (typeGroups.Count() > 1)
                        {
                            shareStringBuilder.AppendLine($"  [{typeGroup.Key}] ({typeGroup.Count()}个)");
                        }
                        
                        foreach (var item in typeGroup.OrderBy(i => i.path))
                        {
                            shareStringBuilder.AppendLine($"    {item.path}");
                        }
                        
                    }
                }
            }
        }

        /// <summary>
        /// 按类型显示依赖
        /// </summary>
        private void DisplayDependenciesByType(List<string> filteredDeps)
        {
            var groupedDeps = filteredDeps.GroupBy(dep => GetFileCategory(dep));

            foreach (var group in groupedDeps.OrderBy(g => g.Key))
            {
                // 显示分类标题
                if (groupedDeps.Count() > 1)
                {
                    shareStringList.Add($"[{group.Key}] ({group.Count()}个)");
                }

                foreach (string dep in group.OrderBy(d => d))
                {
                    shareStringList.Add($"{dep}");
                }
            }
        }

        /// <summary>
        /// 显示单个依赖项
        /// </summary>
        private void DisplayDependencyItem(string dep)
        {

            // string displayName = showFullPath ? dep : System.IO.Path.GetFileName(dep);
            //
            // // 显示文件类型标签
            // string extension = System.IO.Path.GetExtension(dep);
        }

        /// <summary>
        /// 获取带层级信息的依赖
        /// </summary>
        private List<(string path, int layer)> GetDependenciesWithLayers()
        {
            if (targetAsset == null)
                return new List<(string, int)>();

            string assetPath = AssetDatabase.GetAssetPath(targetAsset);
            if (string.IsNullOrEmpty(assetPath))
                return new List<(string, int)>();

            var result = new List<(string path, int layer)>();
            var visited = new HashSet<string>();
            var queue = new Queue<(string path, int currentLayer)>();

            queue.Enqueue((assetPath, 0));
            visited.Add(assetPath);

            while (queue.Count > 0)
            {
                var (currentPath, currentLayer) = queue.Dequeue();

                if (depth > 0 && currentLayer >= depth)
                    continue;

                string[] directDeps = AssetDatabase.GetDependencies(currentPath, false);

                foreach (string dep in directDeps)
                {
                    if (dep == currentPath || visited.Contains(dep))
                        continue;

                    visited.Add(dep);
                    int nextLayer = currentLayer + 1;

                    // 添加到结果中（如果在指定深度范围内）
                    if (depth == 0 || nextLayer <= depth)
                    {
                        result.Add((dep, nextLayer));

                        // 继续搜索下一层
                        if (depth == 0 || nextLayer < depth)
                        {
                            queue.Enqueue((dep, nextLayer));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取文件分类
        /// </summary>
        private string GetFileCategory(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLower();

            if (scriptExtensions.Contains(extension))
                return "脚本文件";

            if (codeExtensions.Contains(extension))
                return "代码文件";

            // 其他资源类型分类
            switch (extension)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".tiff":
                case ".gif":
                case ".bmp":
                case ".exr":
                case ".hdr":
                    return "贴图资源";

                case ".fbx":
                case ".obj":
                case ".dae":
                case ".3ds":
                case ".blend":
                case ".ma":
                case ".mb":
                    return "模型资源";

                case ".wav":
                case ".mp3":
                case ".ogg":
                case ".aiff":
                case ".flac":
                    return "音频资源";

                case ".mp4":
                case ".mov":
                case ".avi":
                case ".webm":
                    return "视频资源";

                case ".mat":
                    return "材质资源";

                case ".shader":
                case ".cginc":
                case ".hlsl":
                    return "着色器";

                case ".prefab":
                    return "预制体";

                case ".unity":
                    return "场景文件";

                case ".asset":
                    return "资源文件";

                case ".controller":
                    return "动画控制器";

                case ".anim":
                    return "动画文件";

                case ".physicMaterial":
                case ".physicsMaterial2D":
                    return "物理材质";

                case ".ttf":
                case ".otf":
                    return "字体资源";

                case ".txt":
                case ".json":
                case ".xml":
                case ".csv":
                    return "文本文件";

                default:
                    return "其他文件";
            }
        }

        /// <summary>
        /// 检查依赖关系
        /// </summary>
        private void CheckDependencies()
        {
            if (targetAsset == null) return;

            isChecking = true;

            try
            {
                string assetPath = AssetDatabase.GetAssetPath(targetAsset);
                if (string.IsNullOrEmpty(assetPath))
                {
                    dependencies.Clear();
                    return;
                }

                // 获取依赖关系
                string[] deps;
                if (depth == 0)
                {
                    // 获取所有依赖
                    deps = AssetDatabase.GetDependencies(assetPath, true);
                }
                else
                {
                    // 获取指定深度的依赖
                    deps = GetDependenciesWithDepth(assetPath, depth);
                }

                // 排除自身
                dependencies = deps.Where(dep => dep != assetPath).ToList();
            }
            finally
            {
                isChecking = false;
            }
        }

        /// <summary>
        /// 获取指定深度的依赖
        /// </summary>
        private string[] GetDependenciesWithDepth(string assetPath, int maxDepth)
        {
            var result = new HashSet<string>();
            var visited = new HashSet<string>();
            var queue = new Queue<(string path, int currentDepth)>();

            queue.Enqueue((assetPath, 0));
            visited.Add(assetPath);

            while (queue.Count > 0)
            {
                var (currentPath, currentDepth) = queue.Dequeue();

                if (currentDepth >= maxDepth)
                    continue;

                string[] directDeps = AssetDatabase.GetDependencies(currentPath, false);

                foreach (string dep in directDeps)
                {
                    if (dep == currentPath || visited.Contains(dep))
                        continue;

                    visited.Add(dep);
                    result.Add(dep);

                    // 继续搜索下一层
                    if (currentDepth + 1 < maxDepth)
                    {
                        queue.Enqueue((dep, currentDepth + 1));
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 刷新依赖列表
        /// </summary>
        private void RefreshDependencies()
        {

            WindowToolGroupReorderableListString.showFullPath = showFullPath;
            shareStringList.Clear();
            shareStringBuilder.Clear();            
            if (targetAsset != null)
            {

                CheckDependencies();
                if (dependencies.Count > 0)
                {
                    var filteredDeps = FilterDependencies();
                    var allDeps = GetAllDependencies(); // 获取未过滤的总数
                    
                    shareStringList.Add($"显示: {filteredDeps.Count} / 总计: {allDeps.Count}");
                    // 显示过滤状态
                    if (filterScripts || filterCode)
                    {
                        var filterInfo = new List<string>();
                        if (filterScripts) filterInfo.Add("脚本");
                        if (filterCode) filterInfo.Add("代码");
                        shareStringBuilder.AppendLine($"(已过滤: {string.Join(", ", filterInfo)})");
                    }
                    

                    // 如果显示层级信息且depth > 0，按层级分组显示
                    if (showLayerInfo && depth > 0)
                    {
                        shareStringBuilder.AppendLine($"资源: {AssetDatabase.GetAssetPath(targetAsset)}");
                        shareStringBuilder.AppendLine($"依赖深度: {(depth == 0 ? "全部" : depth.ToString())}");
                        shareStringBuilder.AppendLine($"依赖数量: {filteredDeps.Count}");
                        shareStringBuilder.AppendLine($"显示: {filteredDeps.Count} / 总计: {allDeps.Count}");
                        DisplayDependenciesByLayer(filteredDeps);
                    }
                    else
                    {
                        // 按类型分组显示
                        DisplayDependenciesByType(filteredDeps);
                    }
                    
                    if(depth > 0&& showLayerInfo)
                    {
                        // 处理不同操作系统的换行符
                        List<string> lines = shareStringBuilder.ToString()
                            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .ToList();

                        foreach (string line in lines)
                        {
                            Console.WriteLine($"'{line}'");
                        }                
                        WindowToolGroupReorderableListString.SetData(lines);                             
                    }
                    else
                    {
                        WindowToolGroupReorderableListString.SetData(shareStringList);
                    }

                }                
            }
        }

        /// <summary>
        /// 过滤依赖列表
        /// </summary>
        private List<string> FilterDependencies()
        {
            var filtered = dependencies.AsEnumerable();

            // 应用文件类型过滤
            if (filterScripts)
            {
                filtered = filtered.Where(dep => !IsScriptFile(dep));
            }

            if (filterCode)
            {
                filtered = filtered.Where(dep => !IsCodeFile(dep));
            }

            // 应用搜索过滤
            if (!string.IsNullOrEmpty(searchFilter))
            {
                filtered = filtered.Where(dep =>
                    dep.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return filtered.ToList();
        }

        /// <summary>
        /// 获取所有依赖（未过滤）
        /// </summary>
        private List<string> GetAllDependencies()
        {
            return dependencies;
        }

        /// <summary>
        /// 判断是否为脚本文件
        /// </summary>
        private bool IsScriptFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            return scriptExtensions.Contains(extension);
        }

        /// <summary>
        /// 判断是否为代码文件
        /// </summary>
        private bool IsCodeFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            return codeExtensions.Contains(extension);
        }

        /// <summary>
        /// 复制依赖列表到剪贴板
        /// </summary>
        private void CopyDependenciesToClipboard()
        {
            if (dependencies.Count == 0) return;

            var filteredDeps = FilterDependencies();
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"资源: {AssetDatabase.GetAssetPath(targetAsset)}");
            sb.AppendLine($"依赖深度: {(depth == 0 ? "全部" : depth.ToString())}");
            sb.AppendLine($"依赖数量: {filteredDeps.Count}");
            sb.AppendLine("依赖列表:");
            sb.AppendLine("----------------------------------------");

            foreach (string dep in filteredDeps)
            {
                sb.AppendLine(dep);
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log($"已复制 {filteredDeps.Count} 个依赖项到剪贴板");
        }
    }
}