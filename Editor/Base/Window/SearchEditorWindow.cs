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
///   2. Project 窗口右键: Assets -> 查找引用
///   3. 代码调用: SearchEditorWindow.FindReferencesToAsset(targetAsset)
///
/// 支持的搜索范围:
///   Prefab, Material, ScriptableObject, AnimatorController, AnimationClip,
///   Scene, RenderTexture, SpriteAtlas, Shader, ShaderVariantCollection, LightingSettings
///
/// 特性:
///   - 异步协程搜索，不阻塞编辑器
///   - 可按资源类型过滤搜索范围
///   - 结果按类型分组显示，支持折叠
///   - 支持 Ping 定位、选中、复制路径等操作
/// </summary>
public class SearchEditorWindow : EditorWindow
{
    #region MenuItem

    [MenuItem("Custom/工具弹窗/查找资源被哪些prefab引用")]
    static void DoSearchRefrence()
    {
        var window = GetWindow<SearchEditorWindow>(false, "资源引用查找器", true);
        window.minSize = new Vector2(560, 450);
        window.Show();
    }

    [MenuItem("Assets/查找引用", false, 30)]
    private static void FindReferencesContextMenu()
    {
        var selected = Selection.activeObject;
        if (selected != null)
        {
            var window = GetWindow<SearchEditorWindow>(false, "资源引用查找器", true);
            window.searchObject = selected;
            window.StartSearch();
        }
    }

    [MenuItem("Assets/查找引用", true)]
    private static bool ValidateFindReferencesContextMenu()
    {
        return Selection.activeObject != null && AssetDatabase.Contains(Selection.activeObject);
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

    // 支持查找的资源类型
    private SearchTypeInfo[] searchTypes = new SearchTypeInfo[]
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

    #endregion

    #region Fields

    private Object searchObject;
    private List<Object> result = new List<Object>();
    private Vector2 scrollPosition;
    private bool isSearching = false;
    private string searchStatus = "";
    private int totalFilesToCheck = 0;
    private int checkedFiles = 0;
    private IEnumerator searchCoroutine;

    // UI 状态
    private bool showTypeFilter = false;
    private Dictionary<string, bool> resultFoldouts = new Dictionary<string, bool>();

    // 样式缓存
    private GUIStyle headerStyle;
    private GUIStyle resultHeaderStyle;
    private GUIStyle groupHeaderStyle;
    private bool stylesInitialized = false;

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
        InitStyles();

        DrawHeader();
        DrawSearchBar();
        DrawTypeFilter();
        DrawProgress();
        DrawResults();
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
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("目标资源", GUILayout.Width(56));
            searchObject = EditorGUILayout.ObjectField(searchObject, typeof(Object), false);
        }
        EditorGUILayout.EndHorizontal();

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
                EditorGUI.BeginDisabledGroup(searchObject == null);
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
            EditorGUILayout.LabelField($"查找结果  ({result.Count} 个引用)", resultHeaderStyle);

            if (result.Count > 0)
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
            if (result.Count == 0 && !isSearching)
            {
                EditorGUILayout.HelpBox(
                    "拖拽任意资源到上方\"目标资源\"栏，点击\"开始查找\"即可搜索该资源被哪些文件引用。\n" +
                    "也可以在 Project 窗口右键资源 -> 查找引用。",
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
    /// 按资源类型分组显示结果
    /// </summary>
    private void DrawGroupedResults()
    {
        // 按扩展名分组
        var groups = new Dictionary<string, List<Object>>();
        foreach (var obj in result)
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
            string key = kvp.Key;
            var items = kvp.Value;

            if (!resultFoldouts.ContainsKey(key))
                resultFoldouts[key] = true;

            // 分组折叠头
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            resultFoldouts[key] = EditorGUILayout.Foldout(resultFoldouts[key], $"{key}  ({items.Count})", true, groupHeaderStyle);
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

    #region Search Logic

    private void StartSearch()
    {
        if (searchObject == null) return;

        result.Clear();
        resultFoldouts.Clear();
        isSearching = true;
        searchStatus = "正在准备查找资源引用...";
        checkedFiles = 0;

        string assetPath = AssetDatabase.GetAssetPath(searchObject);
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

        // 收集所有需要检查的文件 GUID（根据类型过滤）
        var allGuids = new HashSet<string>();
        foreach (var st in searchTypes)
        {
            if (!st.enabled) continue;
            var guids = AssetDatabase.FindAssets(st.searchFilter);
            foreach (var g in guids)
                allGuids.Add(g);
        }

        // 排除自身
        allGuids.Remove(assetGuid);

        totalFilesToCheck = allGuids.Count;
        searchStatus = $"正在检查 {totalFilesToCheck} 个资源文件...";

        searchCoroutine = FindReferencesCoroutine(allGuids.ToList(), assetGuid);
        EditorApplication.update += UpdateSearchCoroutine;
    }

    private IEnumerator FindReferencesCoroutine(List<string> guids, string targetGuid)
    {
        result.Clear();
        checkedFiles = 0;
        int filesPerFrame = 50;

        for (int i = 0; i < guids.Count; i++)
        {
            if (!isSearching) break;

            string filePath = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (CheckFileForGuid(filePath, targetGuid))
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(filePath);
                if (obj != null)
                    result.Add(obj);
            }

            checkedFiles++;

            if (i % filesPerFrame == 0)
                yield return null;
        }

        searchStatus = $"查找完成! 共找到 {result.Count} 个引用";
        isSearching = false;
        searchCoroutine = null;
        Repaint();
    }

    /// <summary>
    /// 在文件文本内容中检查是否包含目标 GUID。
    /// 对大文件使用流式读取，并处理 GUID 跨 chunk 边界的情况。
    /// </summary>
    private bool CheckFileForGuid(string filePath, string targetGuid)
    {
        try
        {
            if (!File.Exists(filePath)) return false;

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 1024 * 1024)
            {
                // 流式读取大文件，保留上一块尾部以防 GUID 跨边界
                int overlapLen = targetGuid.Length - 1;
                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    char[] buffer = new char[8192];
                    string overlap = "";
                    int charsRead;
                    while ((charsRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string chunk = overlap + new string(buffer, 0, charsRead);
                        if (chunk.Contains(targetGuid))
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
                return content.Contains(targetGuid);
            }
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
        var sb = new StringBuilder();
        foreach (var obj in result)
        {
            sb.AppendLine(AssetDatabase.GetAssetPath(obj));
        }
        EditorGUIUtility.systemCopyBuffer = sb.ToString().TrimEnd();
        Debug.Log($"[SearchEditorWindow] 已复制 {result.Count} 条资源路径到剪贴板");
    }

    private void ClearResults()
    {
        result.Clear();
        resultFoldouts.Clear();
        searchStatus = "";
        Repaint();
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

    private void OnDestroy()
    {
        if (searchCoroutine != null)
        {
            EditorApplication.update -= UpdateSearchCoroutine;
        }
    }
}
