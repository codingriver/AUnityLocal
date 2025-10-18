using System.Collections.Generic;
using DG.DemiEditor;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace AUnityLocal.Editor
{
    public class WindowToolEditor : EditorWindow
    {
        // 窗口分割权重（总和应为1.0）
        private float leftWeight = 0.4999f; // 20%
        private float centerLeftWeight = 0.0001f; // 30%
        private float centerRightWeight = 0.3f; // 30%
        private float rightWeight = 0.2f; // 20%

        string title = "WindowTool 工具窗口";
        string title1 = "左侧面板";
        string title2 = "中间左侧";
        string title3 = "搜索";
        string title4 = "结果";        
        
        // 分割线拖拽状态
        private bool isDraggingSplitter1 = false;
        private bool isDraggingSplitter2 = false;
        private bool isDraggingSplitter3 = false;

        // 状态信息
        
        private string statusInfo = "准备就绪";
        private float progress = 0.7f;
        private string progressMsg = string.Empty;
        private bool showProgress = false;

        // 滚动视图位置
        private Vector2 leftScrollPos;
        private Vector2 centerLeftScrollPos;
        private Vector2 centerRightScrollPos;
        private Vector2 rightScrollPos;

        // 样式
        private GUIStyle headerStyle;
        private GUIStyle panelStyle;
        private GUIStyle splitterStyle;
        private GUIStyle statusStyle;
        private GUIStyle titleStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle buttonStyle;
        private GUIStyle boxStyle;
        private GUIStyle fieldStyle;
        private Vector2 scrollPosition;
        
        [MenuItem("AUnityLocal/WindowTool")]
        public static void ShowWindow()
        {
            WindowToolEditor window = GetWindow<WindowToolEditor>("WindowTool 工具");
            window.minSize = new Vector2(1000, 600);
            window.Show();
        }
        Dictionary<WindowArea, List<WindowToolGroup>>  areaGroups = new Dictionary<WindowArea, List<WindowToolGroup>>();
        private void OnEnable()
        {
            WindowToolGroup.window = this;
            NormalizeWeights(); // 确保权重总和为1
            areaGroups = WindowToolGroup.InitializeGroups();
        }

        // 标准化权重，确保总和为1.0
        private void NormalizeWeights()
        {
            float totalWeight = leftWeight + centerLeftWeight + centerRightWeight + rightWeight;
            if (totalWeight > 0)
            {
                leftWeight /= totalWeight;
                centerLeftWeight /= totalWeight;
                centerRightWeight /= totalWeight;
                rightWeight /= totalWeight;
            }
            else
            {
                // 如果权重都为0，设置默认值
                leftWeight = 0.25f;
                centerLeftWeight = 0.25f;
                centerRightWeight = 0.25f;
                rightWeight = 0.25f;
            }
        }

        private void InitializeStyles()
        {
            // 标题样式
            headerStyle = new GUIStyle();
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            // 面板样式
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.padding = new RectOffset(8, 8, 8, 8);
            panelStyle.margin = new RectOffset(2, 2, 2, 2);

            // 分割线样式
            splitterStyle = new GUIStyle();
            splitterStyle.normal.background = EditorGUIUtility.isProSkin
                ? CreateColorTexture(new Color(0.3f, 0.3f, 0.3f))
                : CreateColorTexture(new Color(0.6f, 0.6f, 0.6f));

            // 状态栏样式
            statusStyle = new GUIStyle(EditorStyles.helpBox);
            statusStyle.padding = new RectOffset(10, 10, 5, 5);
            statusStyle.alignment = TextAnchor.MiddleLeft;
            InitializeStyles2();
        }
        public void InitializeStyles2()
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
        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (headerStyle == null) InitializeStyles();
            if (WindowToolGroup.titleStyle == null) WindowToolGroup.InitializeStyles();
            WindowToolGroup.window = this;
            
            DrawLayout();
        }

        private void DrawLayout()
        {
            Rect windowRect = new Rect(0, 0, position.width, position.height);

            // 绘制标题区域
            DrawHeader(windowRect);

            // 绘制状态栏区域
            Rect statusRect = DrawStatusBar(windowRect);

            // 绘制主体区域
            DrawMainContent(windowRect, statusRect);
        }

        private void DrawHeader(Rect windowRect)
        {
            Rect headerRect = new Rect(0, 0, windowRect.width, 30);

            // 绘制标题背景
            EditorGUI.DrawRect(headerRect,
                EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.8f, 0.8f, 0.8f));

            // 绘制标题文本
            GUI.Label(headerRect, title, headerStyle);

            // 绘制分割线
            Rect headerSeparator = new Rect(0, headerRect.yMax, windowRect.width, 1);
            EditorGUI.DrawRect(headerSeparator,
                EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.5f, 0.5f, 0.5f));
        }

        private Rect DrawStatusBar(Rect windowRect)
        {
            // int StatusBarHeight = 52;
            // if (!showProgress)
            // {
            //     StatusBarHeight = 29;
            // }
            int StatusBarHeight = 29;
            Rect statusRect = new Rect(0, windowRect.height - StatusBarHeight, windowRect.width, StatusBarHeight);

            // 绘制状态栏背景
            EditorGUI.DrawRect(statusRect,
                EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.9f, 0.9f, 0.9f));

            // 绘制顶部分割线
            Rect statusSeparator = new Rect(0, statusRect.y, windowRect.width, 3);
            EditorGUI.DrawRect(statusSeparator,
                EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.5f, 0.5f, 0.5f));

            
            GUI.BeginGroup(statusRect);
            EditorGUI.DrawRect(new Rect(statusRect.width/2f-2, 0, 4, statusRect.height),
                EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.5f, 0.5f, 0.5f));            
            if (showProgress)
            {
                var progressBarRect = new Rect(statusRect.width/2f+2+5, 6, statusRect.width/2/2-10, 20);
                EditorGUI.DrawRect(progressBarRect,Color.cyan);
                EditorGUI.ProgressBar(progressBarRect, progress, $"进度: {(progress * 100):F1}% -- {progressMsg}");    
            }
            GUI.Label(new Rect(5, statusRect.height-20-3, statusRect.width-10, 20), statusInfo,EditorStyles.boldLabel);
            
            // float buttonWidth = 100f;
            // float spacing = 5f;
            // float buttonHeight = windowRect.height;
            // if(GUI.Button(new Rect(windowRect.width-buttonWidth-1, 3, buttonWidth,buttonHeight ),"清理",EditorStyles.toolbarButton)) //EditorStyles.miniButtonRight
            // {
            //     WindowToolGroupReorderableListObject.ClearAll();
            // }
                
            GUI.EndGroup();
            // // 状态信息内容
            // GUILayout.BeginArea(new Rect(statusRect.x + 2, statusRect.y + 2,
            //     statusRect.width - 4, statusRect.height-4));
            //
            // //信息
            // GUILayout.FlexibleSpace();
            // GUILayout.Label(statusInfo, EditorStyles.boldLabel);
            //
            // // 显示进度条
            // if (showProgress)
            // {
            //     GUILayout.FlexibleSpace();
            //     GUILayout.Space(2);
            //     Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            //     EditorGUI.ProgressBar(progressRect, statusProgress, $"进度: {(statusProgress * 100):F1}%");
            // }
            //
            // GUILayout.EndArea();

            return statusRect;
        }
        private void DrawMainContent(Rect windowRect, Rect statusRect)
        {
            float mainAreaY = 31; // 标题高度 + 分割线
            float mainAreaHeight = statusRect.y - mainAreaY;
            Rect mainRect = new Rect(0, mainAreaY, windowRect.width, mainAreaHeight);

            // 计算各区域位置（基于权重）
            float splitterWidth = 3f;
            float totalSplitterWidth = splitterWidth * 3; // 3个分割线
            float availableWidth = mainRect.width - totalSplitterWidth;

            float currentX = 0;

            // 左侧区域
            float leftWidth = availableWidth * leftWeight;
            Rect leftRect = new Rect(currentX, mainRect.y, leftWidth, mainRect.height);
            DrawPanel(leftRect, title1, ref leftScrollPos,WindowArea.Left);
            currentX += leftWidth;

            // 分割线1
            Rect splitter1 = new Rect(currentX, mainRect.y, splitterWidth, mainRect.height);
            DrawSplitter(splitter1, ref isDraggingSplitter1, 0); // 传入分割线索引
            currentX += splitterWidth;

            // 中间左侧区域
            float centerLeftWidth = availableWidth * centerLeftWeight;
            Rect centerLeftRect = new Rect(currentX, mainRect.y, centerLeftWidth, mainRect.height);
            DrawPanel(centerLeftRect, title2, ref centerLeftScrollPos,WindowArea.LeftMid);
            currentX += centerLeftWidth;

            // 分割线2
            Rect splitter2 = new Rect(currentX, mainRect.y, splitterWidth, mainRect.height);
            DrawSplitter(splitter2, ref isDraggingSplitter2, 1); // 传入分割线索引
            currentX += splitterWidth;

            // 中间右侧区域
            float centerRightWidth = availableWidth * centerRightWeight;
            Rect centerRightRect = new Rect(currentX, mainRect.y, centerRightWidth, mainRect.height);
            DrawPanel(centerRightRect, title3, ref centerRightScrollPos, WindowArea.RightMid);
            currentX += centerRightWidth;

            // 分割线3
            Rect splitter3 = new Rect(currentX, mainRect.y, splitterWidth, mainRect.height);
            DrawSplitter(splitter3, ref isDraggingSplitter3, 2); // 传入分割线索引
            currentX += splitterWidth;

            // 右侧区域
            float rightWidth = availableWidth * rightWeight;
            Rect rightRect = new Rect(currentX, mainRect.y, rightWidth, mainRect.height);
            DrawPanel(rightRect, title4, ref rightScrollPos, WindowArea.Right);
        }

        private void DrawPanel(Rect rect, string title, ref Vector2 scrollPos, WindowArea area)
        {
            // 绘制面板背景
            EditorGUI.DrawRect(rect,
                EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.95f, 0.95f, 0.95f));

            GUILayout.BeginArea(rect);

            // 面板标题
            GUILayout.BeginHorizontal(EditorStyles.toolbar,GUILayout.Height(25));
            GUILayout.Label(title, EditorStyles.boldLabel);
            
            if (area == WindowArea.Right)
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Clear", "Clear 数据"), EditorStyles.toolbarButton))
                {  
                    // Clear
                    WindowToolGroupReorderableListObject.ClearAll();
                }                    
            }
        
            GUILayout.EndHorizontal();
            // 面板内容区域
            Rect contentRect = new Rect(5, 30, rect.width - 10, rect.height - 35);
            GUILayout.BeginArea(contentRect);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            DrawContent(contentRect, area);
            EditorGUILayout.EndScrollView();   

            GUILayout.EndArea();
            GUILayout.EndArea();
        }

        private void DrawSplitter(Rect rect, ref bool isDragging, int splitterIndex)
        {
            // 绘制分割线
            EditorGUI.DrawRect(rect,
                EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.6f, 0.6f, 0.6f));

            // 处理鼠标事件
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);

            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (rect.Contains(e.mousePosition) && e.button == 0)
                    {
                        isDragging = true;
                        e.Use();
                    }

                    break;

                case EventType.MouseDrag:
                    if (isDragging)
                    {
                        float totalSplitterWidth = 3f * 3; // 3个分割线
                        float availableWidth = position.width - totalSplitterWidth;
                        float deltaWeight = e.delta.x / availableWidth;

                        // 根据分割线索引调整相应的权重
                        switch (splitterIndex)
                        {
                            case 0: // 左侧和中间左侧之间的分割线
                                AdjustWeights(ref leftWeight, ref centerLeftWeight, deltaWeight);
                                break;
                            case 1: // 中间左侧和中间右侧之间的分割线
                                AdjustWeights(ref centerLeftWeight, ref centerRightWeight, deltaWeight);
                                break;
                            case 2: // 中间右侧和右侧之间的分割线
                                AdjustWeights(ref centerRightWeight, ref rightWeight, deltaWeight);
                                break;
                        }

                        NormalizeWeights();
                        Repaint();
                        e.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (isDragging)
                    {
                        isDragging = false;
                        e.Use();
                    }

                    break;
            }
        }

        // 调整两个相邻面板的权重
        private void AdjustWeights(ref float leftWeight, ref float rightWeight, float delta)
        {
            float minWeight = 0.05f; // 最小权重5%

            // 计算新的权重
            float newLeftWeight = leftWeight + delta;
            float newRightWeight = rightWeight - delta;

            // 确保权重在合理范围内
            if (newLeftWeight < minWeight)
            {
                newRightWeight = rightWeight - (minWeight - leftWeight);
                newLeftWeight = minWeight;
            }
            else if (newRightWeight < minWeight)
            {
                newLeftWeight = leftWeight + (rightWeight - minWeight);
                newRightWeight = minWeight;
            }

            // 应用新权重
            leftWeight = newLeftWeight;
            rightWeight = newRightWeight;
        }

        private void DrawContent(Rect rect, WindowArea area)
        {
            if (areaGroups.TryGetValue(area, out var areaGroup))
            {
                foreach (var group in areaGroup)
                {
                    if (group.Show)
                    {
                        EditorGUILayout.BeginVertical(boxStyle);
                        if (!string.IsNullOrEmpty(group.title))
                        {
                            EditorGUILayout.LabelField(group.title, sectionHeaderStyle);    
                        }
                        if (!string.IsNullOrEmpty(group.tip))
                        {
                            GUILayout.Space(5);
                            GUILayout.Label(group.tip);
                        }
                        group.OnGUI(rect);
                        EditorGUILayout.EndVertical();                        
                    }
                }
                    
            }
        }

        // 窗口关闭时的清理
        private void OnDestroy()
        {
            // 清理创建的纹理资源
            if (splitterStyle?.normal?.background != null)
            {
                DestroyImmediate(splitterStyle.normal.background);
            }
        }

        public void SetProgressBar(float progress,string progressInfo = "")
        {
            showProgress = true;
            this.progress = progress;
            progressMsg = progressInfo;
            Repaint();
        }

        public void SetStatusInfo(string info)
        {
            statusInfo = info;
            Repaint();
        }

        public void SetProgressBarShow(bool showProgressBar)
        {
            return;
            showProgress = showProgressBar;
            Repaint();
        }

    }
}