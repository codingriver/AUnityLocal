using IGG.Game.Data.Cache.Login;
using IGG.Game.UI.TowerDefense;

namespace AUnityLocal.Editor
{
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class StringViewEditor : EditorWindow
{
    struct Item
    {
        public string MainStr;
        public string SubStr;
        public string ExtraStr;
    }
    private List<string> stringList = new List<string>();
    private Vector2 scrollPosition;
    private const float ITEM_PADDING = 2f;
    private const float TOOLBAR_HEIGHT = 20f;
    private GUIStyle textFieldStyle;
    private bool stylesInitialized = false;

    [MenuItem("AUnityLocal/String View")]
    private static void ShowWindow()
    {
          Show((List<string>)null);
    }
    public static void Show(List<string> list)
    {
        StringViewEditor window = GetWindow<StringViewEditor>("String View");
        window.stringList.Clear();
        if (list != null)
        {
            window.stringList.AddRange(list);    
        }
        window.minSize = new Vector2(500, 600);
        window.Show();
    }    
    public static void Show(string str)
    {
        Show(new List<string>(){str});
    }  
    
    
    private void OnEnable()
    {
        // // 初始化一些测试数据
        // if (stringList.Count == 0)
        // {
        //     stringList.Add("这是第一条短消息");
        //     stringList.Add("这是第二条比较长的消息，用来测试自动换行功能是否正常工作");
        //     stringList.Add("第三条消息");
        //     stringList.Add("这是一条非常非常非常非常非常非常非常非常非常非常长的消息，用来测试在窗口宽度有限的情况下，文本是否能够正确地自动换行显示");
        //     stringList.Add("最后一条消息");
        // }
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        textFieldStyle = new GUIStyle(EditorStyles.textArea)
        {
            fontSize = 13,
            wordWrap = true,
            padding = new RectOffset(8, 8, 8, 8),
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        stylesInitialized = true;
    }

    void OnGUI()
    {
        InitializeStyles();

        // 绘制工具栏
        DrawToolbar();

        // 绘制可滚动的列表
        DrawScrollableList();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(TOOLBAR_HEIGHT));
        
        GUILayout.FlexibleSpace();

        // Clean 按钮
        if (GUILayout.Button("Clean", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog("清空列表", "确定要清空所有内容吗？", "确定", "取消"))
            {
                stringList.Clear();
                GUI.FocusControl(null); // 清除焦点
            }
        }

        // Add 按钮
        if (GUILayout.Button("Add", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            stringList.Add("新消息 " + (stringList.Count + 1));
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawScrollableList()
    {
        // 计算可用区域
        Rect scrollViewRect = new Rect(0, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT);
        
        // 开始滚动视图
        scrollPosition = GUI.BeginScrollView(
            scrollViewRect,
            scrollPosition,
            new Rect(0, 0, position.width - 20, CalculateTotalContentHeight()),
            false,
            true
        );

        float currentY = ITEM_PADDING;
        float availableWidth = position.width - 40; // 减去滚动条和边距

        for (int i = 0; i < stringList.Count; i++)
        {
            // 计算当前项的高度
            float itemHeight = CalculateItemHeight(stringList[i], availableWidth);

            // 绘制背景
            Rect bgRect = new Rect(ITEM_PADDING, currentY, availableWidth, itemHeight);
            Color bgColor = i % 2 == 0 
                ? new Color(0.3f, 0.3f, 0.3f, 0.2f) 
                : new Color(0.25f, 0.25f, 0.25f, 0.2f);
            EditorGUI.DrawRect(bgRect, bgColor);

            // 绘制索引标签
            Rect labelRect = new Rect(ITEM_PADDING, currentY + 5, 30, 20);
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                //居中对齐
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUI.Label(labelRect, $"#{i + 1}", labelStyle);

            // 绘制文本框
            Rect textRect = new Rect(ITEM_PADDING + 30+ITEM_PADDING, currentY + 5, availableWidth - ITEM_PADDING-(ITEM_PADDING + 30+ITEM_PADDING), itemHeight - 10);
            stringList[i] = EditorGUI.TextArea(textRect, stringList[i], textFieldStyle);

            // // 绘制删除按钮
            // Rect deleteRect = new Rect(availableWidth - 40, currentY + 5, 35, 20);
            // if (GUI.Button(deleteRect, "✕", EditorStyles.miniButton))
            // {
            //     stringList.RemoveAt(i);
            //     GUI.FocusControl(null);
            //     break; // 删除后跳出循环，避免索引错误
            // }

            // 绘制分隔线
            Rect separatorRect = new Rect(ITEM_PADDING, currentY + itemHeight, availableWidth, 1);
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));

            currentY += itemHeight + ITEM_PADDING;
        }

        // 如果列表为空，显示提示
        if (stringList.Count == 0)
        {
            Rect emptyRect = new Rect(ITEM_PADDING, ITEM_PADDING, availableWidth, 100);
            GUIStyle emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            GUI.Label(emptyRect, "列表为空\n点击 'Add' 添加新项", emptyStyle);
        }

        GUI.EndScrollView();
    }

    private float CalculateItemHeight(string text, float width)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 40f; // 最小高度
        }

        // 计算文本高度
        GUIContent content = new GUIContent(text);
        float textHeight = textFieldStyle.CalcHeight(content, width - 90);
        
        // 返回文本高度 + 上下边距
        return Mathf.Max(textHeight + 20f, 40f);
    }

    private float CalculateTotalContentHeight()
    {
        if (stringList.Count == 0)
        {
            return 200f; // 空列表的最小高度
        }

        float totalHeight = ITEM_PADDING;
        float availableWidth = position.width - 40;

        foreach (string item in stringList)
        {
            totalHeight += CalculateItemHeight(item, availableWidth) + ITEM_PADDING;
        }

        return totalHeight + 20; // 额外的底部空间
    }
}

}