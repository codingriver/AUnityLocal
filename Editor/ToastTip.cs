namespace AUnityLocal.Editor
{
    /// <summary>
    /// ToastTip.Show("✅ 成功！");
    /// ToastTip.Show("❌ 失败");
    /// </summary>
    using UnityEngine;
    using UnityEditor;

    public class ToastTip : EditorWindow
    {
        private string message;
        private ToastType toastType;
        private double startTime;
        private const float DURATION = 2f;
        private const float FADE_DURATION = 0.8f;
        private const float MAX_WIDTH = 300f;
        private const float MIN_WIDTH = 180f;
        private const float MIN_HEIGHT = 60f;
        private const float ICON_SIZE = 32f;
        private const float PADDING = 15f;
        private const float SPACING = 10f;

        public enum ToastType
        {
            Success,
            Info,
            Warning,
            Error
        }

        public static void Show(string msg, ToastType type = ToastType.Warning)
        {
            if(measureStyle==null)
                measureStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft,
                    padding = new RectOffset(0, 0, 0, 0)
                };            
            ToastTip window = CreateInstance<ToastTip>();
            window.message = msg;
            window.toastType = type;
            window.startTime = EditorApplication.timeSinceStartup;

            window.titleContent = new GUIContent("");
            window.ShowPopup();

            // 延迟一帧计算和设置大小
            EditorApplication.delayCall += () =>
            {
                if (window != null)
                {
                    Vector2 size = CalculateWindowSize(msg);
                    Rect pos = window.position;
                    pos.width = size.x;
                    pos.height = size.y;

                    // 重新定位
                    Rect main = EditorGUIUtility.GetMainWindowPosition();
                    pos.x = main.x + (main.width - pos.width) * 0.5f;
                    pos.y = main.y + main.height - pos.height - 80;

                    window.position = pos;
                    window.minSize = size;
                    window.maxSize = size;
                }
            };

            EditorApplication.update += window.OnUpdate;
        }

        private static GUIStyle measureStyle = null;
        private static Vector2 CalculateWindowSize(string msg)
        {
            // 创建测量样式


            // 计算可用文本宽度 = 总宽度 - 边距 - 图标 - 间距
            float availableTextWidth = MAX_WIDTH - (PADDING * 2) - ICON_SIZE - SPACING - 10;

            // 计算文本高度
            GUIContent content = new GUIContent(msg);
            float textHeight = measureStyle.CalcHeight(content, availableTextWidth);

            // 计算文本宽度
            Vector2 textSize = measureStyle.CalcSize(content);
            float textWidth = Mathf.Min(textSize.x, availableTextWidth);

            // 计算窗口尺寸
            float windowWidth = Mathf.Max(textWidth + ICON_SIZE + SPACING + (PADDING * 2) + 10, MIN_WIDTH);
            float contentHeight = Mathf.Max(textHeight, ICON_SIZE);
            float windowHeight = Mathf.Max(contentHeight + (PADDING * 2) + 10, MIN_HEIGHT);

            Debug.Log($"ToastTipAdvanced - 消息: '{msg}'");
            Debug.Log($"文本尺寸: {textSize.x}x{textHeight}");
            Debug.Log($"内容高度: {contentHeight}, 窗口高度: {windowHeight}");

            return new Vector2(windowWidth, windowHeight);
        }

        void OnGUI()
        {
            float elapsedTime = (float)(EditorApplication.timeSinceStartup - startTime);
            float alpha = CalculateAlpha(elapsedTime);

            // 绘制背景
            Color bgColor = toastType switch
            {
                ToastType.Success => new Color(0.2f, 0.6f, 0.3f, 0.9f * alpha),
                ToastType.Info => new Color(0.2f, 0.4f, 0.7f, 0.9f * alpha),
                ToastType.Warning => new Color(0.8f, 0.6f, 0.2f, 0.9f * alpha),
                ToastType.Error => new Color(0.8f, 0.2f, 0.2f, 0.9f * alpha),
                _ => new Color(0.15f, 0.15f, 0.15f, 0.9f * alpha)
            };

            Rect bgRect = new Rect(5, 5, position.width - 10, position.height - 10);
            EditorGUI.DrawRect(bgRect, bgColor);

            // 绘制边框
            Color borderColor = new Color(1, 1, 1, 0.3f * alpha);
            DrawBorder(bgRect, borderColor, 2);

            // 绘制内容
            float currentX = PADDING + 5;
            float currentY = PADDING + 5;
            float contentHeight = position.height - (PADDING * 2) - 10;

            // 绘制图标
            string icon = toastType switch
            {
                ToastType.Success => "✓",
                ToastType.Info => "i",
                ToastType.Warning => "!",
                ToastType.Error => "✕",
                _ => "•"
            };

            GUIStyle iconStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1, 1, 1, alpha) }
            };

            Rect iconRect = new Rect(currentX, currentY, ICON_SIZE, contentHeight);
            GUI.Label(iconRect, icon, iconStyle);

            currentX += ICON_SIZE + SPACING;

            // 绘制消息
            GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1, 1, 1, alpha) },
                wordWrap = true,
                padding = new RectOffset(0, 0, 0, 0)
            };

            float messageWidth = position.width - currentX - PADDING - 5;
            Rect messageRect = new Rect(currentX, currentY, messageWidth, contentHeight);
            GUI.Label(messageRect, message, messageStyle);
        }

        private void DrawBorder(Rect rect, Color color, float width)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - width, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.x + rect.width - width, rect.y, width, rect.height), color);
        }

        private float CalculateAlpha(float elapsedTime)
        {
            if (elapsedTime < DURATION)
            {
                return 1f;
            }
            else if (elapsedTime < DURATION + FADE_DURATION)
            {
                return 1f - ((elapsedTime - DURATION) / FADE_DURATION);
            }
            else
            {
                return 0f;
            }
        }

        private void OnUpdate()
        {
            float elapsedTime = (float)(EditorApplication.timeSinceStartup - startTime);

            if (elapsedTime >= DURATION + FADE_DURATION)
            {
                EditorApplication.update -= OnUpdate;
                Close();
                return;
            }

            Repaint();
        }

        void OnDestroy()
        {
            EditorApplication.update -= OnUpdate;
        }
    }
}