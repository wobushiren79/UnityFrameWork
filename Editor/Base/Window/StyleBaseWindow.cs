using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity 内置 GUIStyle 样式预览工具窗口
///
/// 用途说明：
///   1. 浏览和预览 Unity Editor 中所有内置 GUIStyle 样式
///   2. 涵盖四大样式来源：GUI.skin、EditorStyles、Inspector Skin、Game Skin
///   3. 按类型自动分类（Button、Label、Toggle、Toolbar 等），默认折叠，按需展开
///   4. 通过搜索快速查找所需样式名称（搜索时自动展开所有分类）
///   5. 一键复制样式名称 / 代码示例，方便在自定义 Editor 工具代码中直接使用
///   6. 查看每个样式的详细属性（字体大小、对齐方式、边距等）
///
/// 使用场景：
///   - 开发自定义 EditorWindow / Inspector 时，查找合适的内置样式
///   - 替代手动翻阅文档，直接在编辑器内可视化预览效果
///   - 例如：GUILayout.Button("文本", "样式名称") 中的样式名称可从此工具复制
///
/// 样式来源说明：
///   - GUI.skin：当前皮肤的基础 IMGUI 样式（button, box, label, toggle 等）
///   - EditorStyles：Editor 专用静态样式（boldLabel, toolbar, helpBox, foldout 等）
///   - Inspector Skin：EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector) 中的样式
///   - Game Skin：EditorGUIUtility.GetBuiltinSkin(EditorSkin.Game) 中的样式
///
/// 打开方式：Unity 菜单栏 -> Custom -> 工具弹窗 -> 样式预览工具
/// </summary>
public class StyleBaseWindow : EditorWindow
{
    private static readonly string[] TAB_NAMES = { "GUI.skin", "EditorStyles", "Inspector Skin", "Game Skin", "全部" };

    // 分类关键词映射（按优先级匹配，越靠前优先级越高）
    private static readonly string[][] CATEGORY_KEYWORDS =
    {
        new[] { "Toolbar", "toolbar", "Toolbar" },
        new[] { "Button", "button", "btn", "Button" },
        new[] { "Toggle", "toggle", "radio", "check", "Toggle" },
        new[] { "Label", "label", "Label" },
        new[] { "TextField", "textfield", "textarea", "search", "input", "TextField" },
        new[] { "Foldout", "foldout", "Foldout" },
        new[] { "Box", "box", "helpbox", "groupbox", "framebox", "Box" },
        new[] { "Window", "window", "Window" },
        new[] { "Scroll", "scroll", "Scroll" },
        new[] { "Slider", "slider", "thumb", "Slider" },
        new[] { "Dropdown", "dropdown", "popup", "menu", "Dropdown/Popup" },
        new[] { "Tab", "tab", "Tab" },
        new[] { "Progress", "progress", "Progress" },
        new[] { "Notification", "notification", "tooltip", "Notification" },
    };
    private const string CATEGORY_OTHER = "其他";

    [MenuItem("Custom/工具弹窗/样式预览工具")]
    static void CreateWindows()
    {
        var window = GetWindow<StyleBaseWindow>();
        window.titleContent = new GUIContent("GUIStyle 样式预览");
        window.minSize = new Vector2(560, 400);
    }

    private Vector2 scrollPosition = Vector2.zero;
    private string searchStr = "";
    private int matchCount = 0;
    private bool showDetails = false;
    private string expandedStyle = "";
    private int selectedTab = 0;

    // 各来源的样式缓存
    private List<StyleEntry> skinStyles;
    private List<StyleEntry> editorStyles;
    private List<StyleEntry> inspectorSkinStyles;
    private List<StyleEntry> gameSkinStyles;
    private List<StyleEntry> allStyles;

    // 分类后的数据
    private List<StyleCategory> categorizedStyles = new List<StyleCategory>();
    private string lastSearchStr = null;
    private int lastTab = -1;
    private HashSet<string> expandedCategories = new HashSet<string>();

    // 自定义样式（延迟初始化）
    private GUIStyle headerStyle;
    private GUIStyle countLabelStyle;
    private GUIStyle detailLabelStyle;
    private GUIStyle previewBgStyle;
    private GUIStyle sourceTagStyle;
    private GUIStyle categoryHeaderStyle;
    private GUIStyle categoryCountStyle;

    private struct StyleEntry
    {
        public GUIStyle style;
        public string source;
        public string codeHint;
    }

    private class StyleCategory
    {
        public string name;
        public List<StyleEntry> entries = new List<StyleEntry>();
    }

    private void InitCustomStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft
        };

        countLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = Color.gray }
        };

        detailLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true,
            richText = true
        };

        previewBgStyle = new GUIStyle("box")
        {
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip
        };

        sourceTagStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic,
            normal = { textColor = new Color(0.4f, 0.7f, 1f) }
        };

        categoryHeaderStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };

        categoryCountStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        };
    }

    #region 样式缓存与收集

    private void CacheAllStyles()
    {
        if (skinStyles != null) return;

        // 1. GUI.skin
        skinStyles = new List<StyleEntry>();
        foreach (GUIStyle style in GUI.skin)
        {
            skinStyles.Add(new StyleEntry
            {
                style = style,
                source = "GUI.skin",
                codeHint = "\"" + style.name + "\""
            });
        }

        // 2. EditorStyles（反射静态属性）
        editorStyles = new List<StyleEntry>();
        PropertyInfo[] props = typeof(EditorStyles).GetProperties(BindingFlags.Public | BindingFlags.Static);
        foreach (var prop in props)
        {
            if (prop.PropertyType != typeof(GUIStyle)) continue;
            GUIStyle style = prop.GetValue(null) as GUIStyle;
            if (style == null) continue;
            editorStyles.Add(new StyleEntry
            {
                style = style,
                source = "EditorStyles",
                codeHint = "EditorStyles." + prop.Name
            });
        }

        // 3. Inspector Skin
        inspectorSkinStyles = new List<StyleEntry>();
        GUISkin inspSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
        if (inspSkin != null)
        {
            foreach (GUIStyle style in inspSkin)
            {
                inspectorSkinStyles.Add(new StyleEntry
                {
                    style = style,
                    source = "Inspector",
                    codeHint = "EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector) -> \"" + style.name + "\""
                });
            }
        }

        // 4. Game Skin
        gameSkinStyles = new List<StyleEntry>();
        GUISkin gameSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Game);
        if (gameSkin != null)
        {
            foreach (GUIStyle style in gameSkin)
            {
                gameSkinStyles.Add(new StyleEntry
                {
                    style = style,
                    source = "Game",
                    codeHint = "EditorGUIUtility.GetBuiltinSkin(EditorSkin.Game) -> \"" + style.name + "\""
                });
            }
        }

        // 5. 合并去重
        allStyles = new List<StyleEntry>();
        HashSet<string> allAdded = new HashSet<string>();
        AddUniqueEntries(allStyles, allAdded, skinStyles);
        AddUniqueEntries(allStyles, allAdded, editorStyles);
        AddUniqueEntries(allStyles, allAdded, inspectorSkinStyles);
        AddUniqueEntries(allStyles, allAdded, gameSkinStyles);
    }

    private void AddUniqueEntries(List<StyleEntry> target, HashSet<string> nameSet, List<StyleEntry> source)
    {
        foreach (var entry in source)
        {
            string key = entry.source + "/" + (string.IsNullOrEmpty(entry.style.name) ? entry.codeHint : entry.style.name);
            if (nameSet.Add(key))
            {
                target.Add(entry);
            }
        }
    }

    #endregion

    #region 分类与过滤

    private List<StyleEntry> GetCurrentSourceList()
    {
        switch (selectedTab)
        {
            case 0: return skinStyles;
            case 1: return editorStyles;
            case 2: return inspectorSkinStyles;
            case 3: return gameSkinStyles;
            default: return allStyles;
        }
    }

    private string ClassifyStyle(string styleName)
    {
        string lower = styleName.ToLower();
        foreach (var category in CATEGORY_KEYWORDS)
        {
            // category[0] = 分类显示名, category[1..n-1] = 匹配关键词
            for (int i = 1; i < category.Length - 1; i++)
            {
                if (lower.Contains(category[i]))
                    return category[category.Length - 1]; // 最后一个元素是分类显示名
            }
        }
        return CATEGORY_OTHER;
    }

    private void FilterAndCategorize()
    {
        if (lastSearchStr == searchStr && lastTab == selectedTab) return;
        lastSearchStr = searchStr;
        lastTab = selectedTab;

        var sourceList = GetCurrentSourceList();
        string lowerSearch = string.IsNullOrEmpty(searchStr) ? "" : searchStr.ToLower();
        bool isSearching = !string.IsNullOrEmpty(searchStr);

        // 按分类收集
        Dictionary<string, StyleCategory> categoryMap = new Dictionary<string, StyleCategory>();
        // 保持分类顺序
        List<string> categoryOrder = new List<string>();

        matchCount = 0;

        foreach (var entry in sourceList)
        {
            string name = string.IsNullOrEmpty(entry.style.name) ? entry.codeHint : entry.style.name;

            // 搜索过滤
            if (isSearching && !name.ToLower().Contains(lowerSearch))
                continue;

            matchCount++;

            string cat = ClassifyStyle(name);
            if (!categoryMap.ContainsKey(cat))
            {
                categoryMap[cat] = new StyleCategory { name = cat };
                categoryOrder.Add(cat);
            }
            categoryMap[cat].entries.Add(entry);
        }

        // 按预定义顺序排列分类，"其他"放最后
        categorizedStyles.Clear();
        // 先按 CATEGORY_KEYWORDS 中的顺序添加
        foreach (var catDef in CATEGORY_KEYWORDS)
        {
            string catName = catDef[catDef.Length - 1];
            if (categoryMap.ContainsKey(catName))
            {
                categorizedStyles.Add(categoryMap[catName]);
                categoryMap.Remove(catName);
            }
        }
        // 添加剩余分类（"其他"等）
        foreach (var kvp in categoryMap)
        {
            categorizedStyles.Add(kvp.Value);
        }

        // 搜索模式下自动展开所有分类
        if (isSearching)
        {
            foreach (var cat in categorizedStyles)
            {
                expandedCategories.Add(cat.name);
            }
        }
    }

    #endregion

    #region 绘制

    private void OnGUI()
    {
        InitCustomStyles();
        CacheAllStyles();

        DrawToolbar();
        GUILayout.Space(4);
        DrawStyleList();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 标题行
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("GUIStyle 样式预览工具", headerStyle);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("全部展开", EditorStyles.miniButtonLeft, GUILayout.Width(56)))
        {
            foreach (var cat in categorizedStyles)
                expandedCategories.Add(cat.name);
        }
        if (GUILayout.Button("全部折叠", EditorStyles.miniButtonMid, GUILayout.Width(56)))
        {
            expandedCategories.Clear();
        }
        showDetails = GUILayout.Toggle(showDetails, "详情", EditorStyles.miniButtonRight, GUILayout.Width(42));

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(2);

        // Tab 切换行
        int newTab = GUILayout.Toolbar(selectedTab, TAB_NAMES, EditorStyles.toolbarButton);
        if (newTab != selectedTab)
        {
            selectedTab = newTab;
            scrollPosition = Vector2.zero;
            lastSearchStr = null; // 强制重新分类
        }

        GUILayout.Space(2);

        // 搜索行
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("搜索：", GUILayout.Width(36));
        GUI.SetNextControlName("SearchField");
        string newSearch = EditorGUILayout.TextField(searchStr, EditorStyles.toolbarSearchField);
        if (newSearch != searchStr)
        {
            searchStr = newSearch;
            scrollPosition = Vector2.zero;
        }
        if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
        {
            searchStr = "";
            GUI.FocusControl(null);
            lastSearchStr = null;
        }
        EditorGUILayout.EndHorizontal();

        // 统计行
        FilterAndCategorize();
        var sourceList = GetCurrentSourceList();
        GUILayout.Label("共 " + sourceList.Count + " 个样式，匹配 " + matchCount + " 个，" + categorizedStyles.Count + " 个分类", countLabelStyle);

        EditorGUILayout.EndVertical();
    }

    private void DrawStyleList()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (categorizedStyles.Count == 0)
        {
            GUILayout.Space(20);
            EditorGUILayout.HelpBox("未找到匹配的样式，请尝试其他关键词。", MessageType.Info);
        }
        else
        {
            foreach (var category in categorizedStyles)
            {
                DrawCategory(category);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCategory(StyleCategory category)
    {
        bool isOpen = expandedCategories.Contains(category.name);

        // 分类头部
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        bool newOpen = EditorGUILayout.Foldout(isOpen, category.name, true, categoryHeaderStyle);

        GUILayout.FlexibleSpace();
        GUILayout.Label("(" + category.entries.Count + ")", categoryCountStyle, GUILayout.Width(40));

        EditorGUILayout.EndHorizontal();

        if (newOpen != isOpen)
        {
            if (newOpen)
                expandedCategories.Add(category.name);
            else
                expandedCategories.Remove(category.name);
        }

        // 折叠时不绘制内部样式项，节省性能
        if (!newOpen) return;

        EditorGUI.indentLevel++;
        foreach (var entry in category.entries)
        {
            DrawStyleItem(entry);
        }
        EditorGUI.indentLevel--;

        GUILayout.Space(2);
    }

    private void DrawStyleItem(StyleEntry entry)
    {
        GUIStyle style = entry.style;
        string displayName = string.IsNullOrEmpty(style.name) ? entry.codeHint : style.name;
        string expandKey = displayName + entry.source;
        bool isExpanded = expandedStyle == expandKey;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        // 来源标签（仅在"全部"Tab下显示）
        if (selectedTab == 4)
        {
            GUILayout.Label(entry.source, sourceTagStyle, GUILayout.Width(70), GUILayout.Height(20));
        }

        // 样式预览：固定区域绘制
        Rect previewRect = GUILayoutUtility.GetRect(200, 22, GUILayout.Width(200), GUILayout.Height(22));
        GUI.Box(previewRect, GUIContent.none, previewBgStyle);

        float savedFixedW = style.fixedWidth;
        float savedFixedH = style.fixedHeight;
        style.fixedWidth = 0;
        style.fixedHeight = 0;
        GUI.Button(previewRect, displayName, style);
        style.fixedWidth = savedFixedW;
        style.fixedHeight = savedFixedH;

        // 样式名称（可选中复制）
        EditorGUILayout.SelectableLabel(displayName, EditorStyles.label, GUILayout.Height(20));

        // 复制按钮
        if (GUILayout.Button("复制", EditorStyles.miniButton, GUILayout.Width(40), GUILayout.Height(20)))
        {
            EditorGUIUtility.systemCopyBuffer = displayName;
            ShowNotification(new GUIContent("已复制: " + displayName));
        }

        // 详情展开按钮
        if (GUILayout.Button(isExpanded ? "▼" : "▶", EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(20)))
        {
            expandedStyle = isExpanded ? "" : expandKey;
        }

        EditorGUILayout.EndHorizontal();

        if (showDetails || isExpanded)
        {
            DrawStyleDetails(entry);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStyleDetails(StyleEntry entry)
    {
        GUIStyle style = entry.style;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("box");

        Color prevColor = GUI.color;
        GUI.color = new Color(0.85f, 0.85f, 0.85f);

        string info = string.Format(
            "<b>来源:</b> {0}   <b>Font Size:</b> {1}   <b>Alignment:</b> {2}   <b>WordWrap:</b> {3}   <b>RichText:</b> {4}\n" +
            "<b>Margin:</b> L{5} R{6} T{7} B{8}   <b>Padding:</b> L{9} R{10} T{11} B{12}\n" +
            "<b>FixedWidth:</b> {13}   <b>FixedHeight:</b> {14}   <b>StretchW:</b> {15}   <b>StretchH:</b> {16}",
            entry.source,
            style.fontSize, style.alignment, style.wordWrap, style.richText,
            style.margin.left, style.margin.right, style.margin.top, style.margin.bottom,
            style.padding.left, style.padding.right, style.padding.top, style.padding.bottom,
            style.fixedWidth, style.fixedHeight, style.stretchWidth, style.stretchHeight
        );
        EditorGUILayout.LabelField(info, detailLabelStyle, GUILayout.Height(48));

        // 代码示例
        string codeExample;
        if (entry.source == "EditorStyles")
            codeExample = "GUILayout.Button(\"文本\", " + entry.codeHint + ");";
        else
        {
            string styleName = string.IsNullOrEmpty(style.name) ? entry.codeHint : style.name;
            codeExample = "GUILayout.Button(\"文本\", \"" + styleName + "\");";
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField("代码示例", codeExample);
        if (GUILayout.Button("复制", EditorStyles.miniButton, GUILayout.Width(40)))
        {
            EditorGUIUtility.systemCopyBuffer = codeExample;
            ShowNotification(new GUIContent("已复制代码示例"));
        }
        EditorGUILayout.EndHorizontal();

        GUI.color = prevColor;
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    #endregion
}
