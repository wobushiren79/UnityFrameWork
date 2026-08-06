using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 像素图预览工具窗口
/// 功能：多文件夹选择（支持拖拽）→ 扫描并以 Grid 列表预览所有图片 → 点击图片 Ping 定位到 Project 中的位置
/// 性能优化：虚拟滚动只绘制可见区域、AssetPreview 异步缩略图（不加载原图）、预览纹理缓存
/// </summary>
public class PixelArtPreviewWindow : EditorWindow
{
    #region 常量
    /// <summary>文件夹列表的 EditorPrefs 缓存键前缀</summary>
    private const string CACHE_KEY_PREFIX = "PixelArtPreviewWindow_Folders";
    /// <summary>支持的图片扩展名</summary>
    private static readonly string[] IMAGE_EXTENSIONS = { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".bmp", ".gif", ".tif", ".tiff" };
    /// <summary>单元格缩略图尺寸范围</summary>
    private const float CELL_SIZE_MIN = 48f;
    private const float CELL_SIZE_MAX = 256f;
    /// <summary>单元格之间的间距</summary>
    private const float CELL_GAP = 4f;
    /// <summary>单元格下方文件名标签高度</summary>
    private const float LABEL_HEIGHT = 18f;
    /// <summary>文件夹列表单行高度</summary>
    private const float FOLDER_ROW_HEIGHT = 20f;
    /// <summary>文件夹拖拽区域高度范围</summary>
    private const float FOLDER_AREA_MIN_HEIGHT = 52f;
    private const float FOLDER_AREA_MAX_HEIGHT = 140f;
    /// <summary>点击判定允许的鼠标位移（避免拖拽滚动被误判为点击）</summary>
    private const float CLICK_TOLERANCE = 5f;
    #endregion

    #region 数据字段
    /// <summary>已选择的文件夹路径列表（Assets 相对路径）</summary>
    private List<string> folderPaths = new List<string>();
    /// <summary>扫描出的全部图片资源路径（按路径排序）</summary>
    private List<string> imagePaths = new List<string>();
    /// <summary>经搜索过滤后的展示列表</summary>
    private List<string> filteredPaths = new List<string>();
    /// <summary>缩略图缓存：资源路径 -> 预览纹理</summary>
    private readonly Dictionary<string, Texture2D> previewCache = new Dictionary<string, Texture2D>();
    /// <summary>共享缩略图集合（GetMiniThumbnail 返回，禁止 Destroy）</summary>
    private readonly HashSet<Texture2D> sharedThumbnails = new HashSet<Texture2D>();
    /// <summary>正在等待 AssetPreview 异步生成的资源 InstanceID 集合（非空时驱动 Repaint）</summary>
    private readonly HashSet<int> loadingPreviewIds = new HashSet<int>();
    /// <summary>文件名过滤关键字</summary>
    private string searchFilter = string.Empty;
    /// <summary>单元格缩略图尺寸（像素）</summary>
    private float cellSize = 96f;
    /// <summary>文件夹列表滚动位置</summary>
    private Vector2 folderScroll;
    /// <summary>Grid 滚动位置</summary>
    private Vector2 gridScroll;
    /// <summary>是否有文件夹被拖拽悬停在拖放区上</summary>
    private bool isDragOverFolderArea;
    /// <summary>鼠标按下时的位置与目标路径（用于区分点击与拖拽滚动）</summary>
    private Vector2 downClickPos;
    private string downClickPath;
    /// <summary>当前悬停的图片路径（状态栏显示用）</summary>
    private string hoverPath;
    #endregion

    #region 样式
    private GUIStyle cellLabelStyle;
    private GUIStyle searchFieldStyle;
    private GUIStyle statusBarStyle;
    private bool stylesInited;
    #endregion

    #region 窗口入口与生命周期
    /// <summary>
    /// 打开像素图预览工具窗口
    /// </summary>
    [MenuItem("Custom/工具弹窗/像素图预览工具")]
    public static void ShowWindow()
    {
        var window = GetWindow<PixelArtPreviewWindow>("像素图预览");
        window.minSize = new Vector2(480, 360);
        window.Show();
    }

    /// <summary>
    /// 窗口启用：恢复上次选择的文件夹，开启鼠标移动事件以支持 hover 高亮
    /// </summary>
    private void OnEnable()
    {
        wantsMouseMove = true;
        LoadFoldersFromPrefs();
        RefreshFilter();
    }

    /// <summary>
    /// 窗口关闭：释放所有自有的预览纹理
    /// </summary>
    private void OnDisable()
    {
        ClearPreviewCache();
    }
    #endregion

    #region 文件夹持久化
    /// <summary>
    /// 当前项目专属的缓存键（前缀 + 项目路径哈希，保证不同项目互不干扰）
    /// </summary>
    private static string PrefsKey => CACHE_KEY_PREFIX + "_" + Application.dataPath.GetHashCode();

    /// <summary>
    /// 保存文件夹列表到 EditorPrefs
    /// </summary>
    private void SaveFoldersToPrefs()
    {
        EditorPrefs.SetString(PrefsKey, string.Join(";", folderPaths));
    }

    /// <summary>
    /// 从 EditorPrefs 恢复文件夹列表（过滤已失效的目录）
    /// </summary>
    private void LoadFoldersFromPrefs()
    {
        folderPaths = EditorPrefs.GetString(PrefsKey, string.Empty)
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(AssetDatabase.IsValidFolder)
            .ToList();
    }

    /// <summary>
    /// 添加一个文件夹（去重 + 校验 + 持久化）
    /// </summary>
    /// <param name="path">Assets 相对路径</param>
    private void AddFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) return;
        if (folderPaths.Contains(path)) return;
        folderPaths.Add(path);
        SaveFoldersToPrefs();
    }
    #endregion

    #region 图片扫描与过滤
    /// <summary>
    /// 点击「预览」：扫描所有已选文件夹（含子目录）下的图片资源
    /// </summary>
    private void ScanImages()
    {
        var pathSet = new HashSet<string>();
        foreach (string folder in folderPaths)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IMAGE_EXTENSIONS.Contains(Path.GetExtension(path).ToLowerInvariant()))
                    pathSet.Add(path);
            }
        }
        imagePaths = pathSet.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        // 扫描结果变化后旧缓存意义不大，统一释放重新按需生成（AssetPreview 内部有磁盘缓存，二次生成很快）
        ClearPreviewCache();
        RefreshFilter();
        gridScroll = Vector2.zero;
    }

    /// <summary>
    /// 根据搜索关键字刷新展示列表
    /// </summary>
    private void RefreshFilter()
    {
        if (string.IsNullOrEmpty(searchFilter))
        {
            filteredPaths = new List<string>(imagePaths);
        }
        else
        {
            filteredPaths = imagePaths
                .Where(p => Path.GetFileName(p).IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }
    }
    #endregion

    #region 缩略图缓存（性能核心）
    /// <summary>
    /// 获取指定图片的缩略图；未就绪时返回共享小缩略图占位并等待 AssetPreview 异步生成
    /// 只应对可见单元格调用，配合虚拟滚动实现懒加载
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>可绘制的纹理，可能为共享占位图</returns>
    private Texture2D GetPreview(string path)
    {
        if (previewCache.TryGetValue(path, out Texture2D cached))
            return cached;

        Texture2D asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (asset == null)
        {
            previewCache[path] = null;
            return null;
        }

        int instanceId = asset.GetInstanceID();
        Texture2D preview = AssetPreview.GetAssetPreview(asset);
        if (preview != null)
        {
            previewCache[path] = preview;
            loadingPreviewIds.Remove(instanceId);
            return preview;
        }

        // 预览未就绪：仍在后台加载则等待，加载失败则用共享缩略图兜底（避免无限等待重绘）
        if (AssetPreview.IsLoadingAssetPreview(instanceId))
        {
            loadingPreviewIds.Add(instanceId);
        }
        else
        {
            loadingPreviewIds.Remove(instanceId);
            Texture2D miniFinal = AssetPreview.GetMiniThumbnail(asset);
            previewCache[path] = miniFinal;
            sharedThumbnails.Add(miniFinal);
            return miniFinal;
        }
        return AssetPreview.GetMiniThumbnail(asset);
    }

    /// <summary>
    /// 释放全部预览纹理（共享缩略图除外，其生命周期由 Unity 管理）
    /// </summary>
    private void ClearPreviewCache()
    {
        foreach (var kv in previewCache)
        {
            if (kv.Value != null && !sharedThumbnails.Contains(kv.Value))
                DestroyImmediate(kv.Value);
        }
        previewCache.Clear();
        sharedThumbnails.Clear();
        loadingPreviewIds.Clear();
    }
    #endregion

    #region GUI 绘制
    /// <summary>
    /// 初始化 GUI 样式（懒加载，必须在 OnGUI 内调用）
    /// </summary>
    private void InitStyles()
    {
        if (stylesInited) return;
        stylesInited = true;
        cellLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperLeft,
            clipping = TextClipping.Clip,
            wordWrap = false,
        };
        searchFieldStyle = EditorStyles.toolbarSearchField ?? GUI.skin.FindStyle("ToolbarSeachTextField");
        statusBarStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
        };
    }

    /// <summary>
    /// 窗口主绘制入口
    /// </summary>
    private void OnGUI()
    {
        InitStyles();
        DrawToolbar();
        DrawFolderArea();
        DrawGrid();
        DrawStatusBar();
        // 有待生成的预览时持续重绘，直到可见项全部就绪
        if (loadingPreviewIds.Count > 0) Repaint();
    }

    /// <summary>
    /// 顶部工具栏：预览按钮、搜索框、缩略图大小、数量统计
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("预览", EditorStyles.toolbarButton, GUILayout.Width(50)))
            ScanImages();

        GUILayout.Space(8);
        EditorGUI.BeginChangeCheck();
        searchFilter = GUILayout.TextField(searchFilter, searchFieldStyle, GUILayout.Width(180));
        if (EditorGUI.EndChangeCheck())
        {
            RefreshFilter();
            gridScroll = Vector2.zero;
        }

        GUILayout.Space(8);
        GUILayout.Label("大小", EditorStyles.miniLabel, GUILayout.Width(28));
        cellSize = GUILayout.HorizontalSlider(cellSize, CELL_SIZE_MIN, CELL_SIZE_MAX, GUILayout.Width(120));

        GUILayout.FlexibleSpace();
        if (imagePaths.Count > 0)
            GUILayout.Label($"共 {filteredPaths.Count} / {imagePaths.Count} 张", EditorStyles.miniLabel, GUILayout.Width(110));
        else
            GUILayout.Label("未扫描", EditorStyles.miniLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 文件夹选择区：列表 + ObjectField 添加 + 整区拖拽接收
    /// </summary>
    private void DrawFolderArea()
    {
        float areaHeight = Mathf.Clamp((folderPaths.Count + 1.5f) * FOLDER_ROW_HEIGHT, FOLDER_AREA_MIN_HEIGHT, FOLDER_AREA_MAX_HEIGHT);
        Rect areaRect = GUILayoutUtility.GetRect(0, areaHeight, GUILayout.ExpandWidth(true));

        // 拖拽悬停时高亮背景
        Color oldBg = GUI.backgroundColor;
        if (isDragOverFolderArea) GUI.backgroundColor = new Color(0.4f, 0.7f, 1f, 0.6f);
        GUI.Box(areaRect, GUIContent.none, GUI.skin.GetStyle("HelpBox"));
        GUI.backgroundColor = oldBg;

        Rect inner = new Rect(areaRect.x + 4, areaRect.y + 4, areaRect.width - 8, areaRect.height - 8);
        float contentH = (folderPaths.Count + 1) * FOLDER_ROW_HEIGHT;
        folderScroll = GUI.BeginScrollView(inner, folderScroll, new Rect(0, 0, inner.width - 16, contentH));
        float y = 0;
        for (int i = 0; i < folderPaths.Count; i++)
        {
            Rect rowRect = new Rect(0, y, inner.width - 16, FOLDER_ROW_HEIGHT - 2);
            GUIContent folderContent = new GUIContent(folderPaths[i], EditorGUIUtility.IconContent("Folder Icon").image);
            GUI.Label(new Rect(rowRect.x + 2, rowRect.y, rowRect.width - 26, rowRect.height), folderContent, EditorStyles.label);
            if (GUI.Button(new Rect(rowRect.xMax - 22, rowRect.y, 20, rowRect.height), "×", EditorStyles.miniButton))
            {
                folderPaths.RemoveAt(i);
                SaveFoldersToPrefs();
                GUIUtility.ExitGUI();
            }
            y += FOLDER_ROW_HEIGHT;
        }
        // 添加文件夹行（空列表时显示引导文案）
        Rect addRect = new Rect(0, y, inner.width - 16, FOLDER_ROW_HEIGHT);
        string placeholder = folderPaths.Count == 0 ? "将 Project 中的文件夹拖拽到此处，或在此选择" : string.Empty;
        var added = EditorGUI.ObjectField(addRect, placeholder, null, typeof(DefaultAsset), false) as DefaultAsset;
        if (added != null) AddFolder(AssetDatabase.GetAssetPath(added));
        GUI.EndScrollView();

        HandleFolderDrag(areaRect);
    }

    /// <summary>
    /// 处理文件夹拖放（只接受 Project 中的文件夹）
    /// </summary>
    /// <param name="dropRect">拖放区域</param>
    private void HandleFolderDrag(Rect dropRect)
    {
        Event evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition))
        {
            if (evt.type != EventType.DragExited) return;
        }
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                bool valid = DragAndDrop.objectReferences.Any(o => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(o)));
                DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                isDragOverFolderArea = valid;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                        AddFolder(AssetDatabase.GetAssetPath(obj));
                }
                evt.Use();
                break;
            case EventType.DragExited:
                isDragOverFolderArea = false;
                Repaint();
                break;
        }
    }

    /// <summary>
    /// Grid 图片区：虚拟滚动，只创建并绘制可见区域的单元格
    /// </summary>
    private void DrawGrid()
    {
        Rect viewRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        int count = filteredPaths.Count;
        if (count == 0)
        {
            string tip = folderPaths.Count == 0 ? "请先添加文件夹" : "点击「预览」扫描图片";
            GUI.Label(viewRect, tip, new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 });
            return;
        }

        float contentW = viewRect.width - 16f; // 预留垂直滚动条宽度
        float cellFullW = cellSize + CELL_GAP;
        float cellFullH = cellSize + LABEL_HEIGHT + CELL_GAP;
        int cols = Mathf.Max(1, Mathf.FloorToInt(contentW / cellFullW));
        int rows = Mathf.CeilToInt(count / (float)cols);

        Rect contentRect = new Rect(0, 0, cols * cellFullW + 2, rows * cellFullH);
        gridScroll = GUI.BeginScrollView(viewRect, gridScroll, contentRect);

        // 只绘制可见行（虚拟化核心：大量图片时 GUI 元素数量恒定）
        int firstRow = Mathf.Max(0, Mathf.FloorToInt(gridScroll.y / cellFullH));
        int lastRow = Mathf.Min(rows - 1, Mathf.FloorToInt((gridScroll.y + viewRect.height) / cellFullH));
        hoverPath = null;
        for (int row = firstRow; row <= lastRow; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                if (index >= count) break;
                Rect cellRect = new Rect(col * cellFullW + 2, row * cellFullH, cellSize, cellSize + LABEL_HEIGHT);
                DrawCell(cellRect, filteredPaths[index]);
            }
        }
        GUI.EndScrollView();
    }

    /// <summary>
    /// 绘制单个图片单元格：深底 + 缩略图 + 文件名，点击 Ping 定位、双击打开
    /// </summary>
    /// <param name="cellRect">单元格矩形（ScrollView 内容坐标）</param>
    /// <param name="path">图片资源路径</param>
    private void DrawCell(Rect cellRect, string path)
    {
        Rect imageRect = new Rect(cellRect.x, cellRect.y, cellRect.width, cellRect.width);
        Rect labelRect = new Rect(cellRect.x, cellRect.y + cellRect.width + 1, cellRect.width, LABEL_HEIGHT - 2);

        Event evt = Event.current;
        bool hover = cellRect.Contains(evt.mousePosition);
        if (hover)
        {
            hoverPath = path;
            EditorGUI.DrawRect(cellRect, new Color(1f, 1f, 1f, 0.08f));
        }

        // 深灰底衬出透明像素图，再按原比例绘制缩略图
        EditorGUI.DrawRect(imageRect, new Color(0.15f, 0.15f, 0.15f, 1f));
        Texture2D tex = GetPreview(path);
        if (tex != null)
            GUI.DrawTexture(imageRect, tex, ScaleMode.ScaleToFit, true);

        GUI.Label(labelRect, Path.GetFileName(path), cellLabelStyle);

        // 手动点击判定：用按下/抬起距离区分点击与拖拽滚动
        if (evt.type == EventType.MouseDown && evt.button == 0 && hover)
        {
            downClickPos = evt.mousePosition;
            downClickPath = path;
        }
        else if (evt.type == EventType.MouseUp && evt.button == 0 && downClickPath == path && hover)
        {
            downClickPath = null;
            if (Vector2.Distance(downClickPos, evt.mousePosition) < CLICK_TOLERANCE)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                    if (evt.clickCount == 2) AssetDatabase.OpenAsset(asset);
                }
                evt.Use();
            }
        }
    }

    /// <summary>
    /// 底部状态栏：显示悬停图片路径
    /// </summary>
    private void DrawStatusBar()
    {
        Rect statusRect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(statusRect, new Color(0f, 0f, 0f, 0.2f));
        if (!string.IsNullOrEmpty(hoverPath))
            GUI.Label(new Rect(statusRect.x + 6, statusRect.y, statusRect.width - 12, statusRect.height), hoverPath, statusBarStyle);
    }
    #endregion
}
