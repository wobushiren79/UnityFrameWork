using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Project资源收藏夹工具
/// 用于快速收藏和定位Project中的资源
/// </summary>
public class ProjectAssetCollectorWindow : EditorWindow
{
    // --- 常量 ---
    /// <summary>
    /// 缓存键前缀（最终键会追加当前项目唯一标识，确保各项目数据相互独立）
    /// </summary>
    private const string CACHE_KEY_PREFIX = "ProjectAssetCollector_CachedPaths";
    private const float DROP_AREA_HEIGHT = 60f;
    private const float ITEM_HEIGHT = 22f;

    /// <summary>
    /// 当前项目专属的缓存键（懒加载，由前缀 + 项目路径哈希组成）
    /// </summary>
    private static string cacheKey;
    
    // --- 资源类型筛选 ---
    /// <summary>
    /// 资源细分类型筛选枚举（用于下拉筛选，顺序需与 filterLabels 一致）
    /// </summary>
    private enum AssetFilterType
    {
        All,                // 全部
        Folder,             // 文件夹
        Prefab,             // 预制体
        Material,           // 材质球
        Scene,              // 场景
        Texture,            // 贴图/精灵
        Model,              // 模型
        Animation,          // 动画
        Audio,              // 音频
        Script,             // 脚本/Shader
        ScriptableObject,   // 配置资源(.asset)
        Other,              // 其它
    }

    /// <summary>
    /// 下拉筛选显示标签（顺序与 AssetFilterType 一致）
    /// </summary>
    private static readonly string[] filterLabels =
    {
        "全部", "文件夹", "预制体", "材质球", "场景", "贴图/精灵",
        "模型", "动画", "音频", "脚本/Shader", "配置(.asset)", "其它",
    };

    // --- 成员变量 ---
    private List<string> assetPaths = new List<string>();
    /// <summary>
    /// 当前筛选后用于展示/操作的路径列表（全部筛选时与 assetPaths 同引用）
    /// </summary>
    private List<string> filteredPaths = new List<string>();
    /// <summary>
    /// 当前选中的筛选类型
    /// </summary>
    private AssetFilterType currentFilter = AssetFilterType.All;
    private ReorderableList reorderableList;
    private Vector2 scrollPosition;

    // 拖拽区域状态
    private bool isDragOver = false;
    
    // GUI样式
    private GUIStyle dropAreaStyle;
    private GUIStyle itemLabelStyle;
    private bool stylesInitialized = false;

    // --- 窗口初始化 ---
    [MenuItem("Custom/工具弹窗/资源收藏夹")]
    public static void ShowWindow()
    {
        var window = GetWindow<ProjectAssetCollectorWindow>("资源收藏夹");
        window.minSize = new Vector2(300, 400);
    }

    private void OnEnable()
    {
        // 加载缓存数据
        LoadCache();
        // 构建筛选列表并初始化ReorderableList
        RebuildFilteredList();
    }

    private void OnDisable()
    {
        // 保存缓存数据
        SaveCache();
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        // 拖拽区域样式
        dropAreaStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };
        dropAreaStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.gray;
        
        // 列表项标签样式
        itemLabelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            richText = true
        };
        
        stylesInitialized = true;
    }

    private void InitializeReorderableList()
    {
        // 仅在“全部”筛选下允许拖拽排序（此时列表与主列表同引用，排序可直接落到缓存）
        bool draggable = currentFilter == AssetFilterType.All;
        reorderableList = new ReorderableList(filteredPaths, typeof(string), draggable, true, false, true)
        {
            drawHeaderCallback = DrawListHeader,
            drawElementCallback = DrawListElement,
            onRemoveCallback = OnRemoveItem,
            onReorderCallback = OnReorderItems,
            elementHeight = ITEM_HEIGHT
        };
    }

    /// <summary>
    /// 根据当前筛选类型重建 filteredPaths，并重新绑定 ReorderableList。
    /// “全部”时直接引用 assetPaths，保证拖拽排序落到主列表；其它类型则为筛选副本。
    /// </summary>
    private void RebuildFilteredList()
    {
        if (currentFilter == AssetFilterType.All)
        {
            filteredPaths = assetPaths;
        }
        else
        {
            filteredPaths = assetPaths.FindAll(p => GetAssetFilterType(p) == currentFilter);
        }
        InitializeReorderableList();
    }

    /// <summary>
    /// 按资源路径判定其细分类型：优先判断文件夹，再按扩展名归类。
    /// </summary>
    private AssetFilterType GetAssetFilterType(string path)
    {
        if (string.IsNullOrEmpty(path)) return AssetFilterType.Other;
        if (AssetDatabase.IsValidFolder(path)) return AssetFilterType.Folder;

        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        switch (ext)
        {
            case ".prefab":
                return AssetFilterType.Prefab;
            case ".mat":
                return AssetFilterType.Material;
            case ".unity":
                return AssetFilterType.Scene;
            case ".png": case ".jpg": case ".jpeg": case ".tga": case ".psd":
            case ".gif": case ".bmp": case ".tif": case ".tiff": case ".exr":
                return AssetFilterType.Texture;
            case ".fbx": case ".obj": case ".blend": case ".dae": case ".3ds": case ".max":
                return AssetFilterType.Model;
            case ".anim": case ".controller": case ".overridecontroller": case ".playable":
                return AssetFilterType.Animation;
            case ".wav": case ".mp3": case ".ogg": case ".aiff": case ".aif":
                return AssetFilterType.Audio;
            case ".cs": case ".js": case ".shader": case ".cginc": case ".hlsl": case ".compute":
                return AssetFilterType.Script;
            case ".asset":
                return AssetFilterType.ScriptableObject;
            default:
                return AssetFilterType.Other;
        }
    }

    // --- UI 绘制 ---
    private void OnGUI()
    {
        InitializeStyles();
        
        // 标题
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("资源收藏夹", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // 拖拽区域
        DrawDropArea();
        EditorGUILayout.Space(5);

        // 类型筛选下拉
        DrawFilterDropdown();
        EditorGUILayout.Space(5);

        // 操作按钮
        DrawOperationButtons();
        EditorGUILayout.Space(5);
        
        // 资源列表
        DrawAssetList();
        
        // 底部信息
        DrawFooterInfo();
    }

    private void DrawDropArea()
    {
        // 绘制拖拽区域
        Rect dropArea = GUILayoutUtility.GetRect(0, DROP_AREA_HEIGHT, GUILayout.ExpandWidth(true));
        
        // 根据拖拽状态改变背景颜色
        Color originalColor = GUI.backgroundColor;
        if (isDragOver)
        {
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f, 1f);
        }
        
        string dropText = isDragOver ? "松开鼠标添加资源" : "将Project资源拖入此处";
        GUI.Box(dropArea, dropText, dropAreaStyle);
        GUI.backgroundColor = originalColor;
        
        // 处理拖拽事件
        HandleDragAndDrop(dropArea);
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                {
                    isDragOver = false;
                    return;
                }
                
                isDragOver = true;
                
                // 检查是否有有效的拖拽对象
                bool hasValidObjects = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(path))
                    {
                        hasValidObjects = true;
                        break;
                    }
                }
                
                if (hasValidObjects)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        AddDraggedAssets(DragAndDrop.objectReferences);
                        isDragOver = false;
                    }
                }
                else
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                }
                
                evt.Use();
                Repaint();
                break;
                
            case EventType.DragExited:
                isDragOver = false;
                Repaint();
                break;
        }
    }

    private void AddDraggedAssets(Object[] objects)
    {
        bool hasNewAsset = false;
        
        foreach (var obj in objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && !assetPaths.Contains(path))
            {
                assetPaths.Add(path);
                hasNewAsset = true;
            }
        }
        
        if (hasNewAsset)
        {
            RebuildFilteredList();
            SaveCache();
            Repaint();
        }
    }

    /// <summary>
    /// 绘制资源类型筛选下拉，切换时重建筛选列表。
    /// </summary>
    private void DrawFilterDropdown()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("筛选类型", GUILayout.Width(60));
        AssetFilterType newFilter = (AssetFilterType)EditorGUILayout.Popup((int)currentFilter, filterLabels);
        if (newFilter != currentFilter)
        {
            currentFilter = newFilter;
            RebuildFilteredList();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawOperationButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        // 清空列表按钮
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f, 1f);
        if (GUILayout.Button("清空列表", GUILayout.Height(25)))
        {
            if (assetPaths.Count == 0 || EditorUtility.DisplayDialog("确认清空", "确定要清空所有收藏的资源吗？", "确定", "取消"))
            {
                assetPaths.Clear();
                RebuildFilteredList();
                SaveCache();
            }
        }
        GUI.backgroundColor = Color.white;
        
        // 刷新列表按钮（移除无效路径）
        if (GUILayout.Button("刷新列表", GUILayout.Height(25)))
        {
            RefreshList();
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void RefreshList()
    {
        int removedCount = assetPaths.RemoveAll(path => 
        {
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            return obj == null;
        });
        
        if (removedCount > 0)
        {
            RebuildFilteredList();
            SaveCache();
            EditorUtility.DisplayDialog("刷新完成", $"已移除 {removedCount} 个无效的资源路径", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("刷新完成", "所有资源路径均有效", "确定");
        }
        
        Repaint();
    }

    private void DrawAssetList()
    {
        if (assetPaths.Count == 0)
        {
            EditorGUILayout.HelpBox("暂无收藏的资源\n请将Project中的资源拖入上方区域", MessageType.Info);
            return;
        }

        if (filteredPaths.Count == 0)
        {
            EditorGUILayout.HelpBox($"当前筛选（{filterLabels[(int)currentFilter]}）下暂无资源", MessageType.Info);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        reorderableList.DoLayoutList();
        EditorGUILayout.EndScrollView();
    }

    private void DrawListHeader(Rect rect)
    {
        // 全部时仅显示总数；筛选时显示“筛选类型 命中数/总数”
        if (currentFilter == AssetFilterType.All)
        {
            EditorGUI.LabelField(rect, $"收藏列表 ({assetPaths.Count}个)");
        }
        else
        {
            EditorGUI.LabelField(rect, $"收藏列表 - {filterLabels[(int)currentFilter]} ({filteredPaths.Count}/{assetPaths.Count}个)");
        }
    }

    private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (index < 0 || index >= filteredPaths.Count) return;

        string path = filteredPaths[index];
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        
        // 计算各区域的Rect
        float iconSize = ITEM_HEIGHT - 4;
        float buttonWidth = 60;
        float pingButtonWidth = 50;
        float spacing = 5;
        
        Rect iconRect = new Rect(rect.x, rect.y + 2, iconSize, iconSize);
        Rect labelRect = new Rect(rect.x + iconSize + spacing, rect.y, 
            rect.width - iconSize - buttonWidth - pingButtonWidth - spacing * 3, rect.height);
        Rect pingButtonRect = new Rect(rect.xMax - buttonWidth - pingButtonWidth - spacing, rect.y + 1, pingButtonWidth, rect.height - 2);
        Rect selectButtonRect = new Rect(rect.xMax - buttonWidth, rect.y + 1, buttonWidth, rect.height - 2);
        
        // 绘制图标
        if (asset != null)
        {
            Texture icon = AssetDatabase.GetCachedIcon(path);
            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }
        }
        else
        {
            // 无效资源显示警告图标
            GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("console.warnicon.sml").image, ScaleMode.ScaleToFit);
        }
        
        // 绘制路径标签
        string displayName = System.IO.Path.GetFileName(path);
        string tooltip = path;
        
        if (asset == null)
        {
            // 无效资源用红色显示
            displayName = $"<color=red>{displayName} (无效)</color>";
        }
        
        GUIContent labelContent = new GUIContent(displayName, tooltip);
        
        // 点击标签区域也可以定位
        if (GUI.Button(labelRect, labelContent, itemLabelStyle))
        {
            PingAsset(path);
        }
        
        // Ping按钮
        if (GUI.Button(pingButtonRect, "定位"))
        {
            PingAsset(path);
        }
        
        // 选中按钮
        if (GUI.Button(selectButtonRect, "选中"))
        {
            SelectAsset(path);
        }
    }

    private void OnRemoveItem(ReorderableList list)
    {
        if (list.index >= 0 && list.index < filteredPaths.Count)
        {
            // 按路径从主列表移除，兼容筛选副本与主列表两种情形
            string path = filteredPaths[list.index];
            assetPaths.Remove(path);
            RebuildFilteredList();
            SaveCache();
        }
    }

    private void OnReorderItems(ReorderableList list)
    {
        // 仅在“全部”筛选下可拖拽，此时 filteredPaths 与 assetPaths 同引用，排序已落到主列表
        SaveCache();
    }

    private void PingAsset(string path)
    {
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
        }
        else
        {
            EditorUtility.DisplayDialog("资源不存在", $"找不到资源：\n{path}", "确定");
        }
    }

    private void SelectAsset(string path)
    {
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (asset != null)
        {
            // 检查是否为预制体，如果是则在Hierarchy中打开
            if (asset is GameObject && PrefabUtility.GetPrefabAssetType(asset) != PrefabAssetType.NotAPrefab)
            {
                // 使用 AssetDatabase.OpenAsset 打开预制体编辑模式
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }
        else
        {
            EditorUtility.DisplayDialog("资源不存在", $"找不到资源：\n{path}", "确定");
        }
    }

    private void DrawFooterInfo()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("提示：全部筛选下可拖动列表项调整顺序", EditorStyles.miniLabel, GUILayout.Width(240));
        EditorGUILayout.EndHorizontal();
    }

    // --- 缓存管理 ---
    /// <summary>
    /// 获取当前项目专属的缓存键。
    /// EditorPrefs 是按机器（注册表）全局存储的，多个项目共用同一前缀键会相互覆盖；
    /// 这里以项目路径（Application.dataPath，指向各项目的 Assets 目录）的哈希作为后缀，
    /// 保证不同项目的收藏数据彼此独立保存。
    /// </summary>
    private static string GetCacheKey()
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            // Application.dataPath 在不同项目下唯一，用其哈希生成稳定且无特殊字符的项目标识
            int projectHash = Application.dataPath.GetHashCode();
            cacheKey = $"{CACHE_KEY_PREFIX}_{projectHash:X8}";
        }
        return cacheKey;
    }

    private void SaveCache()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < assetPaths.Count; i++)
        {
            sb.Append(assetPaths[i]);
            if (i < assetPaths.Count - 1)
            {
                sb.Append("|");
            }
        }
        EditorPrefs.SetString(GetCacheKey(), sb.ToString());
    }

    private void LoadCache()
    {
        assetPaths.Clear();
        
        string cached = EditorPrefs.GetString(GetCacheKey(), string.Empty);
        if (!string.IsNullOrEmpty(cached))
        {
            string[] paths = cached.Split('|');
            foreach (string path in paths)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    assetPaths.Add(path);
                }
            }
        }
    }
}
