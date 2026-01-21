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
    private const string CACHE_KEY = "ProjectAssetCollector_CachedPaths";
    private const float DROP_AREA_HEIGHT = 60f;
    private const float ITEM_HEIGHT = 22f;
    
    // --- 成员变量 ---
    private List<string> assetPaths = new List<string>();
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
        // 初始化ReorderableList
        InitializeReorderableList();
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
        reorderableList = new ReorderableList(assetPaths, typeof(string), true, true, false, true)
        {
            drawHeaderCallback = DrawListHeader,
            drawElementCallback = DrawListElement,
            onRemoveCallback = OnRemoveItem,
            onReorderCallback = OnReorderItems,
            elementHeight = ITEM_HEIGHT
        };
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
            SaveCache();
            Repaint();
        }
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
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        reorderableList.DoLayoutList();
        EditorGUILayout.EndScrollView();
    }

    private void DrawListHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, $"收藏列表 ({assetPaths.Count}个)");
    }

    private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (index < 0 || index >= assetPaths.Count) return;
        
        string path = assetPaths[index];
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
        if (list.index >= 0 && list.index < assetPaths.Count)
        {
            assetPaths.RemoveAt(list.index);
            SaveCache();
        }
    }

    private void OnReorderItems(ReorderableList list)
    {
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
        EditorGUILayout.LabelField("提示：拖动列表项可调整顺序", EditorStyles.miniLabel, GUILayout.Width(180));
        EditorGUILayout.EndHorizontal();
    }

    // --- 缓存管理 ---
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
        EditorPrefs.SetString(CACHE_KEY, sb.ToString());
    }

    private void LoadCache()
    {
        assetPaths.Clear();
        
        string cached = EditorPrefs.GetString(CACHE_KEY, string.Empty);
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
