namespace AUnityLocal.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 快捷键定位目标物体：Ctrl+Shift+L
    /// 运行时记录最后一次点击屏幕坐标，编辑器侧通过屏幕空间距离计算最近 EntityBehavior 物体。
    /// Hierarchy 高亮 → Scene 视图聚焦 → Inspector 显示信息。
    /// 配置项可通过菜单 "AUnityLocal/定位目标物体设置..." 进行界面化修改。
    /// </summary>
    public static class HierarchyEditor
    {
        private const string PREF_TYPE_NAME = "AUnityLocal.HierarchyEditor.TypeName";
        private const string PREF_FIELD_NAME = "AUnityLocal.HierarchyEditor.FieldName";
        private const string PREF_ELEMENT_INDEX = "AUnityLocal.HierarchyEditor.ElementIndex";
        private const string PREF_USE_DIRECT_TARGET = "AUnityLocal.HierarchyEditor.UseDirectTarget";
        private const string PREF_DIRECT_TARGET_GLOBAL_ID = "AUnityLocal.HierarchyEditor.DirectTargetGlobalId";

        /// <summary>反射目标类型的全名（含命名空间，嵌套类用 + 连接）</summary>
        public static string TypeName
        {
            get => EditorPrefs.GetString(PREF_TYPE_NAME, "IGG.Game.Module.KSBattle.BattleLog+BattleDebug");
            set => EditorPrefs.SetString(PREF_TYPE_NAME, value);
        }

        /// <summary>反射目标字段名</summary>
        public static string FieldName
        {
            get => EditorPrefs.GetString(PREF_FIELD_NAME, "hitGos");
            set => EditorPrefs.SetString(PREF_FIELD_NAME, value);
        }

        /// <summary>从 List 中取的元素索引</summary>
        public static int ElementIndex
        {
            get => EditorPrefs.GetInt(PREF_ELEMENT_INDEX, 0);
            set => EditorPrefs.SetInt(PREF_ELEMENT_INDEX, value);
        }

        /// <summary>是否优先使用直接目标物体（而非反射）</summary>
        public static bool UseDirectTarget
        {
            get => EditorPrefs.GetBool(PREF_USE_DIRECT_TARGET, false);
            set => EditorPrefs.SetBool(PREF_USE_DIRECT_TARGET, value);
        }

        /// <summary>直接目标物体的 GlobalObjectId 字符串</summary>
        private static string DirectTargetGlobalId
        {
            get => EditorPrefs.GetString(PREF_DIRECT_TARGET_GLOBAL_ID, string.Empty);
            set => EditorPrefs.SetString(PREF_DIRECT_TARGET_GLOBAL_ID, value);
        }

        /// <summary>获取配置中保存的直接目标物体</summary>
        public static GameObject GetDirectTarget()
        {
            string idStr = DirectTargetGlobalId;
            if (string.IsNullOrEmpty(idStr))
                return null;

            if (!GlobalObjectId.TryParse(idStr, out GlobalObjectId globalId))
                return null;

            UnityEngine.Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
            return obj as GameObject;
        }

        /// <summary>设置直接目标物体（使用 GlobalObjectId 持久化）</summary>
        public static void SetDirectTarget(GameObject go)
        {
            if (go == null || go.Equals(null))
            {
                DirectTargetGlobalId = string.Empty;
            }
            else
            {
                GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(go);
                DirectTargetGlobalId = globalId.ToString();
            }
        }

        /// <summary>
        /// 根据当前配置解析出最终要定位的目标物体。
        /// 优先使用直接目标；未启用或无效时，通过反射获取静态字段值。
        /// </summary>
        public static UnityEngine.Object ResolveTarget()
        {
            if (UseDirectTarget)
            {
                GameObject direct = GetDirectTarget();
                if (direct != null && !direct.Equals(null))
                    return direct;
            }

            // 运行时优先使用屏幕空间距离计算（剔除 Physics.RaycastAll 依赖）
            if (Application.isPlaying)
            {
                UnityEngine.Object runtimeTarget = ResolveNearestEntityByScreenDistance();
                if (runtimeTarget != null)
                    return runtimeTarget;
            }

            // 兜底旧逻辑
            object hitList = ToolBarToolEditor.GetStaticFieldValue(TypeName, FieldName);
            if (hitList is List<GameObject> hitGos)
            {
                int idx = ElementIndex;
                if (idx >= 0 && hitGos.Count > idx)
                    return hitGos[idx];
            }

            return null;
        }

        /// <summary>
        /// 通过屏幕空间距离查找最后一次点击位置最近的 EntityBehavior 物体。
        /// 逻辑在编辑器侧执行，运行时仅提供 LastClickScreenPos。
        /// </summary>
        private static UnityEngine.Object ResolveNearestEntityByScreenDistance()
        {
            Type battleDebugType = ToolBarToolEditor.FindType("IGG.Game.Module.KSBattle.BattleLog+BattleDebug");
            if (battleDebugType == null)
                return null;

            FieldInfo hasLastClickField = battleDebugType.GetField("HasLastClick",
                BindingFlags.Public | BindingFlags.Static);
            if (hasLastClickField == null || !(bool)hasLastClickField.GetValue(null))
                return null;

            FieldInfo lastClickField = battleDebugType.GetField("LastClickScreenPos",
                BindingFlags.Public | BindingFlags.Static);
            if (lastClickField == null)
                return null;
            Vector2 lastClick = (Vector2)lastClickField.GetValue(null);

            Camera cam = Camera.main;
            if (cam == null)
                return null;

            Type entityBehaviorType = ToolBarToolEditor.FindType("IGG.Game.Module.KSBattle.Entity.EntityBehavior");
            if (entityBehaviorType == null)
                return null;

            UnityEngine.Object[] behaviors = UnityEngine.Object.FindObjectsOfType(entityBehaviorType);
            if (behaviors == null || behaviors.Length == 0)
                return null;

            GameObject bestGo = null;
            float bestDist = float.MaxValue;

            foreach (UnityEngine.Object behavior in behaviors)
            {
                if (behavior == null)
                    continue;

                Component comp = behavior as Component;
                if (comp == null || comp.transform == null)
                    continue;

                Vector3 screenPos = cam.WorldToScreenPoint(comp.transform.position);
                if (screenPos.z <= 0)
                    continue;

                float dist = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), lastClick);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestGo = comp.gameObject;
                }
            }

            return bestGo;
        }

        [MenuItem("AUnityLocal/定位目标物体 %#l")] // Ctrl+Shift+L
        private static void SelectAndLocateTarget()
        {
            UnityEngine.Object target = ResolveTarget();

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

        [MenuItem("AUnityLocal/定位目标物体设置...", false, 1001)]
        private static void OpenSettings()
        {
            HierarchyEditorSettingsWindow.ShowWindow();
        }
    }

    /// <summary>
    /// HierarchyEditor 的配置窗口，支持界面化修改定位目标参数。
    /// </summary>
    public class HierarchyEditorSettingsWindow : EditorWindow
    {
        private string m_typeName;
        private string m_fieldName;
        private int m_elementIndex;
        private bool m_useDirectTarget;
        private GameObject m_directTarget;
        private Vector2 m_scrollPos;

        [MenuItem("AUnityLocal/定位目标物体设置...", false, 1001)]
        public static void ShowWindow()
        {
            HierarchyEditorSettingsWindow window = GetWindow<HierarchyEditorSettingsWindow>("定位目标设置");
            window.minSize = new Vector2(420, 280);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            m_typeName = HierarchyEditor.TypeName;
            m_fieldName = HierarchyEditor.FieldName;
            m_elementIndex = HierarchyEditor.ElementIndex;
            m_useDirectTarget = HierarchyEditor.UseDirectTarget;
            m_directTarget = HierarchyEditor.GetDirectTarget();
        }

        private void OnGUI()
        {
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("定位目标物体设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // ---------- 直接目标模式 ----------
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("直接目标（优先级最高）", EditorStyles.boldLabel);
            m_useDirectTarget = EditorGUILayout.Toggle("启用直接目标", m_useDirectTarget);
            if (m_useDirectTarget)
            {
                m_directTarget = EditorGUILayout.ObjectField(
                    "目标物体", m_directTarget, typeof(GameObject), true) as GameObject;
                EditorGUILayout.HelpBox(
                    "设置后将直接定位该物体，不再通过反射获取。支持场景中的物体和项目资源。",
                    MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // ---------- 反射目标模式 ----------
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("反射获取（备用）", EditorStyles.boldLabel);

            bool cacheEnabled = GUI.enabled;
            GUI.enabled = !m_useDirectTarget;
            m_typeName = EditorGUILayout.TextField("类型全名", m_typeName);
            m_fieldName = EditorGUILayout.TextField("字段名", m_fieldName);
            m_elementIndex = EditorGUILayout.IntField("列表索引", m_elementIndex);
            GUI.enabled = cacheEnabled;

            EditorGUILayout.HelpBox(
                "通过反射读取指定类型的静态字段（要求是 List<GameObject>），并取指定索引的元素。",
                MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            // ---------- 操作按钮 ----------
            if (GUILayout.Button("保存设置", GUILayout.Height(32)))
            {
                SaveSettings();
                ToastTip.Show("设置已保存！");
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("重置为默认值", GUILayout.Height(24)))
            {
                m_typeName = "IGG.Game.Module.KSBattle.BattleLog+BattleDebug";
                m_fieldName = "hitGos";
                m_elementIndex = 0;
                m_useDirectTarget = false;
                m_directTarget = null;
                SaveSettings();
                ToastTip.Show("已重置为默认值！");
            }

            EditorGUILayout.EndScrollView();
        }

        private void SaveSettings()
        {
            HierarchyEditor.TypeName = m_typeName;
            HierarchyEditor.FieldName = m_fieldName;
            HierarchyEditor.ElementIndex = m_elementIndex;
            HierarchyEditor.UseDirectTarget = m_useDirectTarget;
            HierarchyEditor.SetDirectTarget(m_directTarget);
        }
    }
}
