namespace AUnityLocal.Editor
{
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class HierarchyEditor
    {
        static HierarchyEditor()
        {
            // 注册层级窗口 GUI 回调
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            // 根据 instanceID 获取对象
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            GameObject go = obj as GameObject;
            if (go == null)
            {
                Debug.Log(obj?.GetType().FullName);
                
                return;    
            }
            

            // 计算按钮位置（在右侧）
            Rect buttonRect = new Rect(
                selectionRect.xMax - 20,
                selectionRect.y,
                18,
                selectionRect.height
            );

            // 画一个小按钮
            if (GUI.Button(buttonRect, "+"))
            {
                Debug.Log($"点击了 {go.name}");
            }
        }
        // static HierarchyEditor()
        // {
        //     // 注册层级窗口 GUI 回调
        //     EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        // }

        // static EditorWindow GetHierarchyWindow()
        // {
        //     var type = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        //     return EditorWindow.GetWindow(type, false, null, false);
        // }
        // static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        // {
        //     // 根据 instanceID 获取对象
        //     GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        //     if (go == null) return;
        //
        //     // 计算按钮位置（在右侧）
        //     Rect buttonRect = new Rect(
        //         selectionRect.xMax - 20,
        //         selectionRect.y,
        //         18,
        //         selectionRect.height
        //     );
        //
        //     // 画一个小按钮
        //     if (GUI.Button(buttonRect, "+"))
        //     {
        //         Debug.Log($"点击了 {go.name}");
        //     }
        // }        
    }
}