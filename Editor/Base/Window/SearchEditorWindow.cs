using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 资源引用查找器 (Asset Reference Finder)
///
/// 功能说明:
///   在整个项目中搜索"哪些资源引用了指定的目标资源"。
///   通过读取资源文件的文本内容，匹配目标资源的 GUID 来判断引用关系。
///
/// 使用方式:
///   1. 菜单栏: Custom -> 工具弹窗 -> 查找资源被哪些prefab引用
///   2. Project 窗口右键: Assets -> 查找引用 (支持多选，自动把所有选中资源设为目标并开始查找)
///   3. 代码调用: SearchEditorWindow.FindReferencesToAsset(targetAsset)
///
/// 支持的搜索范围:
///   Prefab, Material, ScriptableObject, AnimatorController, AnimationClip,
///   Scene, RenderTexture, SpriteAtlas, Shader, ShaderVariantCollection, LightingSettings
///
/// 特性:
///   - 支持多个目标资源同时查找，结果按目标资源分组列出
///   - 异步协程搜索，不阻塞编辑器；一次文件读取同时匹配所有目标 GUID
///   - 可按资源类型过滤搜索范围
///   - 结果按目标/类型两级分组显示，支持折叠
///   - 支持 Ping 定位、选中、复制路径等操作
/// </summary>
public class SearchEditorWindow : EditorWindow
{
    #region MenuItem

    [MenuItem("Custom/工具弹窗/查找资源被哪些prefab引用")]
    static void DoSearchRefrence()
    {
        OpenWindow();
    }

    [MenuItem("Assets/查找引用", false, 30)]
    private static void FindReferencesContextMenu()
    {
        // 取全部选中资源(支持多选)，过滤掉非资产对象并按 GUID 去重
        var targets = Selection.objects
            .Where(o => o != null && AssetDatabase.Contains(o))
            .GroupBy(o => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o)))
            .Select(g => g.First())
            .ToList();
        if (targets.Count == 0) return;

        var window = OpenWindow();
        window.SetTargets(targets);
        window.StartSearch();
    }

    [MenuItem("Assets/查找引用", true)]
    private static bool ValidateFindReferencesContextMenu()
    {
        return Selection.objects.Any(o => o != null && AssetDatabase.Contains(o));
    }

    /// <summary>
    /// 打开(或聚焦)查找器窗口，统一入口保证 minSize 等初始化一致
    /// </summary>
    private static SearchEditorWindow OpenWindow()
    {
        var window = GetWindow<SearchEditorWindow>(false, "资源引用查找器", true);
        window.minSize = new Vector2(560, 450);
        window.Show();
        window.Focus();
        return window;
    }

    #endregion

    #region Search Type Definition

    private class SearchTypeInfo
    {
        public string label;
        public string extension;
        public string searchFilter;
        public bool enabled;

        public SearchTypeInfo(string label, string extension, string searchFilter, bool defaultEnabled = true)
        {
            this.label = label;
            this.extension = extension;
            this.searchFilter = searchFilter;
            this.enabled = defaultEnabled;
        }
    }

    // 支持查找的资源类型(domain reload 后为 null 时由 EnsureState 经 CreateDefaultSearchTypes 重建)
    private SearchTypeInfo[] searchTypes = CreateDefaultSearchTypes();

    /// <summary>
    /// 构建默认的搜索类型列表
    /// </summary>
    private static SearchTypeInfo[] CreateDefaultSearchTypes()
    {
        return new SearchTypeInfo[]
        {
            new SearchTypeInfo("Prefab",                ".prefab",          "t:Prefab"),
            new SearchTypeInfo("Material",              ".mat",             "t:Material"),
            new SearchTypeInfo("ScriptableObject",      ".asset",           "t:ScriptableObject"),
            new SearchTypeInfo("AnimatorController",    ".controller",      "t:AnimatorController"),
            new SearchTypeInfo("AnimationClip",         ".anim",            "t:AnimationClip"),
            new SearchTypeInfo("Scene",                 ".unity",           "t:Scene"),
            new SearchTypeInfo("RenderTexture",         ".rendertexture",   "t:RenderTexture"),
            new SearchTypeInfo("SpriteAtlas",           ".spriteatlas",     "t:SpriteAtlas"),
            new SearchTypeInfo("Shader",                ".shadergraph",     "t:Shader"),
            new SearchTypeInfo("ShaderVariants",        ".shadervariants",  "t:ShaderVariantCollection"),
            new SearchTypeInfo("LightingSettings",      ".lighting",        "t:LightingSettings"),
        };
    }

    #endregion

    #region Target Result Definition

    /// <summary>
    /// 单个目标资源的查找结果分组
    /// </summary>
    private class TargetSearchResult
    {
        public Object target;
        public string targetGuid;
        public List<Object> references = new List<Object>();
    }

    #endregion

    #region Fields

    private List<Object> searchObjects = new List<Object>();
    private List<TargetSearchResult> searchResults = new List<TargetSearchResult>();
    private Vector2 scrollPosition;
    private Vector2 targetScrollPosition;
    private bool isSearching = false;
    private string searchStatus = "";
    private int totalFilesToCheck = 0;
    private int checkedFiles = 0;
    private IEnumerator searchCoroutine;

    // UI 状态
    private bool showTypeFilter = false;
    private Dictionary<string, bool> resultFoldouts = new Dictionary<string, bool>();

    // 搜索历史
    private List<string> searchHistory = new List<string>();
    private Vector2 historyScrollPosition;
    private const string HistoryPrefsKey = "SearchEditorWindow_History";
    private const int MaxHistoryCount = 10;

    // 样式缓存
    private GUIStyle headerStyle;
    private GUIStyle resultHeaderStyle;
    private GUIStyle groupHeaderStyle;
    private bool stylesInitialized = false;

    /// <summary>
    /// 所有目标的引用总数(各目标分组引用数之和)
    /// </summary>
    private int TotalReferenceCount
    {
        get { return searchResults.Sum(r => r.references.Count); }
    }

    #endregion

    #region Style Init

    private void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(4, 4, 8, 4)
        };

        resultHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            margin = new RectOffset(4, 4, 4, 2)
        };

        groupHeaderStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold
        };

        stylesInitialized = true;
    }

    #endregion

    #region OnGUI

    private void OnGUI()
    {
        EnsureState();
        InitStyles();

        DrawHeader();
        DrawSearchBar();
        DrawTypeFilter();
        DrawProgress();
        DrawResults();
        DrawSearchHistory();
    }

    /// <summary>
    /// Domain reload 后私有字段会丢失，绘制前兜底重建运行时集合
    /// </summary>
    private void EnsureState()
    {
        if (searchObjects == null) searchObjects = new List<Object>();
        if (searchResults == null) searchResults = new List<TargetSearchResult>();
        if (resultFoldouts == null) resultFoldouts = new Dictionary<string, bool>();
        if (searchHistory == null) searchHistory = new List<string>();
        if (searchTypes == null) searchTypes = CreateDefaultSearchTypes();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("资源引用查找器", headerStyle);
        DrawSeparator();
    }

    private void DrawSearchBar()
    {
        EditorGUILayout.Space(4);

        // 目标列表标题行 + 批量操作
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField($"目标资源 ({searchObjects.Count(o => o != null)} 个)", GUILayout.Width(120));
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(!Selection.objects.Any(o => o != null && AssetDatabase.Contains(o)));
            if (GUILayout.Button("添加当前选中", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                AddTargets(Selection.objects);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(searchObjects.Count == 0);
            if (GUILayout.Button("清空目标", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                searchObjects.Clear();
            }
            EditorGUI.EndDisabledGroup();
        }
        EditorGUILayout.EndHorizontal();

        // 目标列表(支持多行、删除、拖拽批量添加)
        EditorGUILayout.BeginVertical("box");
        {
            if (searchObjects.Count == 0)
            {
                EditorGUILayout.LabelField("拖拽资源到此处，或点击下方\"添加目标\"", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                targetScrollPosition = EditorGUILayout.BeginScrollView(targetScrollPosition, GUILayout.MaxHeight(150));
                for (int i = 0; i < searchObjects.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        searchObjects[i] = EditorGUILayout.ObjectField(searchObjects[i], typeof(Object), false);
                        if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
                        {
                            searchObjects.RemoveAt(i);
                            i--;
                            continue;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            if (GUILayout.Button("+ 添加目标", EditorStyles.miniButton))
            {
                searchObjects.Add(null);
            }
        }
        EditorGUILayout.EndVertical();

        // 让整个目标区域成为拖放区，可一次拖入多个资源
        HandleTargetDragDrop(GUILayoutUtility.GetLastRect());

        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();
        {
            // 类型过滤折叠按钮
            showTypeFilter = EditorGUILayout.Foldout(showTypeFilter, "搜索范围过滤", true);

            GUILayout.FlexibleSpace();

            // 搜索 / 取消按钮
            if (isSearching)
            {
                if (GUILayout.Button("取消搜索", GUILayout.Width(80), GUILayout.Height(22)))
                {
                    CancelSearch();
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(!searchObjects.Any(o => o != null));
                if (GUILayout.Button("开始查找", GUILayout.Width(80), GUILayout.Height(22)))
                {
                    StartSearch();
                }
                EditorGUI.EndDisabledGroup();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTypeFilter()
    {
        if (!showTypeFilter) return;

        EditorGUILayout.Space(2);
        EditorGUI.indentLevel++;

        // 全选 / 全不选
        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Space(12);
            if (GUILayout.Button("全选", EditorStyles.miniButtonLeft, GUILayout.Width(50)))
            {
                foreach (var t in searchTypes) t.enabled = true;
            }
            if (GUILayout.Button("全不选", EditorStyles.miniButtonRight, GUILayout.Width(50)))
            {
                foreach (var t in searchTypes) t.enabled = false;
            }
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();

        // 类型复选框 - 每行3个
        int columns = 3;
        for (int i = 0; i < searchTypes.Length; i += columns)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            for (int j = 0; j < columns && i + j < searchTypes.Length; j++)
            {
                var st = searchTypes[i + j];
                st.enabled = EditorGUILayout.ToggleLeft(st.label, st.enabled, GUILayout.Width(160));
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(2);
    }

    private void DrawProgress()
    {
        if (string.IsNullOrEmpty(searchStatus)) return;

        EditorGUILayout.Space(4);
        DrawSeparator();

        // 状态文字
        EditorGUILayout.LabelField(searchStatus, EditorStyles.miniLabel);

        // 进度条
        if (isSearching && totalFilesToCheck > 0)
        {
            float progress = Mathf.Clamp01(checkedFiles / (float)totalFilesToCheck);
            Rect rect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, progress, $"{checkedFiles} / {totalFilesToCheck}  ({(int)(progress * 100)}%)");
        }

        EditorGUILayout.Space(2);
    }

    private void DrawResults()
    {
        DrawSeparator();
        EditorGUILayout.Space(2);

        // 结果标题行
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField($"查找结果  ({TotalReferenceCount} 个引用)", resultHeaderStyle);

            if (TotalReferenceCount > 0)
            {
                if (GUILayout.Button("复制路径", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    CopyResultPaths();
                }
                if (GUILayout.Button("清空", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    ClearResults();
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        // 滚动列表
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        {
            if (TotalReferenceCount == 0 && !isSearching)
            {
                EditorGUILayout.HelpBox(
                    "拖拽任意资源到上方\"目标资源\"区域(可一次拖入多个)，点击\"开始查找\"即可搜索这些资源被哪些文件引用。\n" +
                    "也可以在 Project 窗口选中一个或多个资源，右键 -> 查找引用。",
                    MessageType.Info);
            }
            else
            {
                DrawGroupedResults();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制搜索历史
    /// </summary>
    private void DrawSearchHistory()
    {
        if (searchHistory.Count == 0) return;

        DrawSeparator();
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("最近搜索", headerStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空历史", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                ClearHistory();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition, GUILayout.MaxHeight(200));
        {
            foreach (var guid in searchHistory)
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                if (obj == null) continue;

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(12);
                    var icon = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(obj));
                    if (icon != null)
                    {
                        GUILayout.Label(new GUIContent(icon), GUILayout.Width(18), GUILayout.Height(18));
                    }

                    if (GUILayout.Button(obj.name, EditorStyles.label))
                    {
                        SetTargets(new List<Object> { obj });
                        StartSearch();
                    }

                    if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        RemoveFromHistory(guid);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制分组结果：多目标时按"目标资源 -> 资源类型"两级分组，单目标时只按类型分组
    /// </summary>
    private void DrawGroupedResults()
    {
        if (searchResults.Count == 1)
        {
            DrawTypeGroups(searchResults[0].references, searchResults[0].targetGuid);
            return;
        }

        foreach (var targetResult in searchResults)
        {
            string key = targetResult.targetGuid;
            if (!resultFoldouts.ContainsKey(key))
                resultFoldouts[key] = true;

            // 目标分组折叠头(图标 + 目标名 + 引用数 + Ping)
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                string targetPath = targetResult.target != null ? AssetDatabase.GetAssetPath(targetResult.target) : "";
                var icon = AssetDatabase.GetCachedIcon(targetPath);
                if (icon != null)
                {
                    GUILayout.Label(new GUIContent(icon), GUILayout.Width(18), GUILayout.Height(18));
                }

                string targetName = targetResult.target != null ? targetResult.target.name : "(已丢失资源)";
                resultFoldouts[key] = EditorGUILayout.Foldout(resultFoldouts[key], $"{targetName}  ({targetResult.references.Count})", true, groupHeaderStyle);

                if (targetResult.target != null)
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Search Icon", "|在 Project 中高亮目标资源"), GUILayout.Width(26), GUILayout.Height(18)))
                    {
                        EditorGUIUtility.PingObject(targetResult.target);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!resultFoldouts[key]) continue;

            EditorGUI.indentLevel++;
            DrawTypeGroups(targetResult.references, key);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }
    }

    /// <summary>
    /// 按资源类型分组绘制一组引用列表，foldout 键带目标前缀避免多目标间冲突
    /// </summary>
    private void DrawTypeGroups(List<Object> references, string keyPrefix)
    {
        // 按扩展名分组
        var groups = new Dictionary<string, List<Object>>();
        foreach (var obj in references)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            string ext = Path.GetExtension(path).ToLower();
            string groupName = GetGroupName(ext);

            if (!groups.ContainsKey(groupName))
                groups[groupName] = new List<Object>();
            groups[groupName].Add(obj);
        }

        // 按组绘制
        foreach (var kvp in groups.OrderBy(g => g.Key))
        {
            string key = keyPrefix + "|" + kvp.Key;
            var items = kvp.Value;

            if (!resultFoldouts.ContainsKey(key))
                resultFoldouts[key] = true;

            // 分组折叠头
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            resultFoldouts[key] = EditorGUILayout.Foldout(resultFoldouts[key], $"{kvp.Key}  ({items.Count})", true, groupHeaderStyle);
            EditorGUILayout.EndHorizontal();

            if (!resultFoldouts[key]) continue;

            // 绘制该组的每一项
            for (int i = 0; i < items.Count; i++)
            {
                DrawResultItem(items[i]);
            }

            EditorGUILayout.Space(2);
        }
    }

    private void DrawResultItem(Object obj)
    {
        string path = AssetDatabase.GetAssetPath(obj);

        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.Space(16);

            // 资源图标 + ObjectField
            var icon = AssetDatabase.GetCachedIcon(path);
            if (icon != null)
            {
                GUILayout.Label(new GUIContent(icon), GUILayout.Width(18), GUILayout.Height(18));
            }

            EditorGUILayout.ObjectField(obj, typeof(Object), false);

            // Ping
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Search Icon", "|在 Project 中高亮"), GUILayout.Width(26), GUILayout.Height(18)))
            {
                EditorGUIUtility.PingObject(obj);
            }

            // 选中
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Selectable Icon", "|选中该资源"), GUILayout.Width(26), GUILayout.Height(18)))
            {
                Selection.activeObject = obj;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Target Management

    /// <summary>
    /// 替换当前目标列表并清空旧结果(右键查找、历史记录的统一入口)
    /// </summary>
    private void SetTargets(List<Object> targets)
    {
        EnsureState();
        searchObjects.Clear();
        AddTargets(targets);
        ClearResults();
    }

    /// <summary>
    /// 批量添加目标资源，自动过滤非资产对象并按 GUID 去重
    /// </summary>
    private void AddTargets(IEnumerable<Object> targets)
    {
        EnsureState();
        var existGuids = new HashSet<string>(
            searchObjects.Where(o => o != null)
                .Select(o => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o))));

        foreach (var obj in targets)
        {
            if (obj == null || !AssetDatabase.Contains(obj)) continue;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            if (string.IsNullOrEmpty(guid) || existGuids.Contains(guid)) continue;

            existGuids.Add(guid);
            searchObjects.Add(obj);
        }

        // 新目标占位行被填入后，移除多余的空行
        searchObjects.RemoveAll(o => o == null);
        Repaint();
    }

    /// <summary>
    /// 处理目标区域的拖放事件，支持一次拖入多个资源
    /// </summary>
    private void HandleTargetDragDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (evt == null || !dropArea.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            AddTargets(DragAndDrop.objectReferences);
            evt.Use();
        }
    }

    #endregion

    #region Search Logic

    private void StartSearch()
    {
        // 正在搜索时先停掉旧协程，避免重复注册 update 回调
        if (isSearching || searchCoroutine != null)
        {
            CancelSearch();
        }

        // 过滤空目标并按 GUID 去重，同步重建结果分组
        var targets = searchObjects
            .Where(o => o != null)
            .GroupBy(o => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o)))
            .Select(g => g.First())
            .ToList();
        if (targets.Count == 0) return;

        searchResults.Clear();
        resultFoldouts.Clear();
        isSearching = true;
        searchStatus = "正在准备查找资源引用...";
        checkedFiles = 0;

        var targetGuids = new List<string>();
        foreach (var target in targets)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));
            targetGuids.Add(guid);
            searchResults.Add(new TargetSearchResult { target = target, targetGuid = guid });
            AddToHistory(target);
        }

        // 收集所有需要检查的文件 GUID（根据类型过滤）
        var allGuids = new HashSet<string>();
        foreach (var st in searchTypes)
        {
            if (!st.enabled) continue;
            var guids = AssetDatabase.FindAssets(st.searchFilter);
            foreach (var g in guids)
                allGuids.Add(g);
        }

        // 排除所有目标自身
        foreach (var guid in targetGuids)
            allGuids.Remove(guid);

        totalFilesToCheck = allGuids.Count;
        searchStatus = $"正在检查 {totalFilesToCheck} 个资源文件...";

        searchCoroutine = FindReferencesCoroutine(allGuids.ToList(), targetGuids);
        EditorApplication.update += UpdateSearchCoroutine;
    }

    private IEnumerator FindReferencesCoroutine(List<string> guids, List<string> targetGuids)
    {
        checkedFiles = 0;
        int filesPerFrame = 50;
        var matched = new bool[targetGuids.Count];

        for (int i = 0; i < guids.Count; i++)
        {
            if (!isSearching) yield break;

            string filePath = AssetDatabase.GUIDToAssetPath(guids[i]);

            // 一次文件读取同时匹配所有目标，命中哪个目标就记入哪个分组
            if (CheckFileForGuids(filePath, targetGuids, matched))
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(filePath);
                if (obj != null)
                {
                    for (int t = 0; t < targetGuids.Count; t++)
                    {
                        if (matched[t])
                            searchResults[t].references.Add(obj);
                    }
                }
            }

            checkedFiles++;

            if (i % filesPerFrame == 0)
                yield return null;
        }

        searchStatus = $"查找完成! 共找到 {TotalReferenceCount} 个引用";
        isSearching = false;
        searchCoroutine = null;
        Repaint();
    }

    /// <summary>
    /// 在文件文本内容中检查包含哪些目标 GUID，结果写入 matched 数组并返回是否有任一命中。
    /// 对大文件使用流式读取，并处理 GUID 跨 chunk 边界的情况。
    /// </summary>
    private bool CheckFileForGuids(string filePath, List<string> targetGuids, bool[] matched)
    {
        for (int i = 0; i < matched.Length; i++) matched[i] = false;

        try
        {
            if (!File.Exists(filePath)) return false;

            bool anyMatched = false;
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 1024 * 1024)
            {
                // 流式读取大文件，保留上一块尾部以防 GUID 跨边界
                int overlapLen = targetGuids.Max(g => g.Length) - 1;
                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    char[] buffer = new char[8192];
                    string overlap = "";
                    int charsRead;
                    while ((charsRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string chunk = overlap + new string(buffer, 0, charsRead);
                        for (int t = 0; t < targetGuids.Count; t++)
                        {
                            if (!matched[t] && chunk.Contains(targetGuids[t]))
                            {
                                matched[t] = true;
                                anyMatched = true;
                            }
                        }

                        // 所有目标都已命中时提前结束读取
                        if (anyMatched && matched.All(m => m))
                            return true;

                        // 保留末尾 overlap，防止 GUID 跨两个 chunk
                        if (chunk.Length > overlapLen)
                            overlap = chunk.Substring(chunk.Length - overlapLen);
                        else
                            overlap = chunk;
                    }
                }
            }
            else
            {
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                for (int t = 0; t < targetGuids.Count; t++)
                {
                    if (content.Contains(targetGuids[t]))
                    {
                        matched[t] = true;
                        anyMatched = true;
                    }
                }
            }
            return anyMatched;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SearchEditorWindow] 检查文件 {filePath} 时出错: {ex.Message}");
        }

        return false;
    }

    private void UpdateSearchCoroutine()
    {
        if (searchCoroutine != null)
        {
            if (!searchCoroutine.MoveNext())
            {
                EditorApplication.update -= UpdateSearchCoroutine;
                searchCoroutine = null;
            }
        }
        Repaint();
    }

    private void CancelSearch()
    {
        isSearching = false;
        searchStatus = "查找已取消";

        if (searchCoroutine != null)
        {
            EditorApplication.update -= UpdateSearchCoroutine;
            searchCoroutine = null;
        }
        Repaint();
    }

    #endregion

    #region Helpers

    private string GetGroupName(string extension)
    {
        return extension switch
        {
            ".prefab"          => "Prefab",
            ".mat"             => "Material",
            ".asset"           => "ScriptableObject",
            ".controller"      => "AnimatorController",
            ".anim"            => "AnimationClip",
            ".unity"           => "Scene",
            ".rendertexture"   => "RenderTexture",
            ".spriteatlas"     => "SpriteAtlas",
            ".shadergraph"     => "Shader",
            ".shadervariants"  => "ShaderVariants",
            ".lighting"        => "LightingSettings",
            _ => "Other"
        };
    }

    private void CopyResultPaths()
    {
        var paths = searchResults
            .SelectMany(r => r.references)
            .Select(obj => AssetDatabase.GetAssetPath(obj))
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .OrderBy(p => p)
            .ToList();
        EditorGUIUtility.systemCopyBuffer = string.Join("\n", paths);
        Debug.Log($"[SearchEditorWindow] 已复制 {paths.Count} 条资源路径到剪贴板");
    }

    private void ClearResults()
    {
        searchResults.Clear();
        resultFoldouts.Clear();
        searchStatus = "";
        Repaint();
    }

    private void AddToHistory(Object obj)
    {
        if (obj == null) return;

        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
        if (string.IsNullOrEmpty(guid)) return;

        // 如果已存在，先移除（避免重复）
        searchHistory.Remove(guid);

        // 添加到列表开头
        searchHistory.Insert(0, guid);

        // 限制最多 10 条
        while (searchHistory.Count > MaxHistoryCount)
        {
            searchHistory.RemoveAt(searchHistory.Count - 1);
        }

        SaveHistory();
    }

    private void RemoveFromHistory(string guid)
    {
        searchHistory.Remove(guid);
        SaveHistory();
    }

    private void ClearHistory()
    {
        searchHistory.Clear();
        SaveHistory();
    }

    private void SaveHistory()
    {
        string data = string.Join(";", searchHistory);
        EditorPrefs.SetString(HistoryPrefsKey, data);
    }

    private void LoadHistory()
    {
        searchHistory.Clear();
        string data = EditorPrefs.GetString(HistoryPrefsKey, "");
        if (!string.IsNullOrEmpty(data))
        {
            var guids = data.Split(';');
            foreach (var guid in guids)
            {
                if (!string.IsNullOrEmpty(guid))
                {
                    searchHistory.Add(guid);
                }
            }
        }
    }

    private void DrawSeparator()
    {
        var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
    }

    #endregion

    #region Public API

    /// <summary>
    /// 静态方法: 查找引用了 targetAsset 的所有 Prefab。
    /// 适用于从代码中调用，同步执行（会阻塞）。
    /// </summary>
    public static List<Object> FindReferencesToAsset(Object targetAsset)
    {
        if (targetAsset == null) return new List<Object>();

        string assetPath = AssetDatabase.GetAssetPath(targetAsset);
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

        var results = new List<Object>();
        var allPrefabs = AssetDatabase.FindAssets("t:Prefab");

        foreach (var guid in allPrefabs)
        {
            string filePath = AssetDatabase.GUIDToAssetPath(guid);
            if (File.ReadAllText(filePath).Contains(assetGuid))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(filePath);
                if (obj != null) results.Add(obj);
            }
        }

        return results;
    }

    #endregion

    #region Lifecycle

    private void OnDestroy()
    {
        if (searchCoroutine != null)
        {
            EditorApplication.update -= UpdateSearchCoroutine;
        }
    }

    private void OnEnable()
    {
        EnsureState();
        LoadHistory();
    }

    #endregion
}
