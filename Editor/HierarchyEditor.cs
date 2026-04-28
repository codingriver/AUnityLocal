namespace AUnityLocal.Editor
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 快捷键定位目标物体：Ctrl+Shift+L
    /// 选中 BattleDebug.hitGos[0] → Hierarchy 高亮 → Scene 视图聚焦 → Inspector 显示信息。
    /// </summary>
    public static class HierarchyEditor
    {
        [MenuItem("AUnityLocal/定位目标物体 %#l")] // Ctrl+Shift+L
        private static void SelectAndLocateTarget()
        {
            Object target = null;

            var hitList = ToolBarToolEditor.GetStaticFieldValue(
                "IGG.Game.Module.KSBattle.BattleLog+BattleDebug", "hitGos");

            if (hitList is List<GameObject> hitGos && hitGos.Count >= 1)
            {
                target = hitGos[0];
            }

            if (target == null)
            {
                ToastTip.Show("targetObject is null!");
                return;
            }

            if (target.Equals(null))
            {
                ToastTip.Show("targetObject is destroyed!");
                return;
            }

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }
    }
}