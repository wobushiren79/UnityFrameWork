using UnityEditor;
using UnityEngine;

/// <summary>
/// ScrollGridBaseContent（含 ScrollGridHorizontal / ScrollGridVertical）的自定义Inspector。
/// 提供编辑器模式下的“重建预览/清除预览”按钮，以及“子控件(cell)尺寸调整与保存”功能，
/// 便于在不运行游戏的情况下查看实际生成的cell布局并直接调整、保存模板cell尺寸。
/// </summary>
[CustomEditor(typeof(ScrollGridBaseContent), true)]
public class InspectorScrollGridBaseContent : Editor
{
    #region 字段

    /// <summary>
    /// Inspector中正在编辑的子控件(cell)尺寸缓存。
    /// </summary>
    private Vector2 editCellSize;

    /// <summary>
    /// 是否已从目标读取过一次cell尺寸（避免每帧覆盖正在编辑的值）。
    /// </summary>
    private bool cellSizeLoaded;

    #endregion

    #region 生命周期

    /// <summary>
    /// 选中目标变化时重置尺寸读取标记，下一帧从目标重新读取。
    /// </summary>
    private void OnEnable()
    {
        cellSizeLoaded = false;
    }

    #endregion

    #region 绘制

    /// <summary>
    /// 绘制Inspector：默认字段 + 预览操作区 + 子控件尺寸调整区。
    /// </summary>
    public override void OnInspectorGUI()
    {
        // 绘制默认的Inspector（含 editorPreviewCount、tempCell 等字段）
        base.OnInspectorGUI();

        ScrollGridBaseContent content = (ScrollGridBaseContent)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("编辑器预览", EditorStyles.boldLabel);

        // 运行模式下由游戏逻辑驱动，禁用手动预览，避免干扰运行时状态
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("运行模式下由游戏逻辑驱动，预览/编辑按钮已禁用。", MessageType.Info);
            return;
        }

        DrawPreviewSection(content);
        DrawCellSizeSection(content);
    }

    /// <summary>
    /// 绘制预览操作区（重建/清除预览）。
    /// </summary>
    private void DrawPreviewSection(ScrollGridBaseContent content)
    {
        EditorGUILayout.HelpBox(
            "按【editorPreviewCount】克隆tempCell生成实际布局；cell内容需运行时数据回调填充，" +
            "预览仅展示布局与位置（外观取决于tempCell）。预览对象已标记为不保存(DontSave)，不会被存入预制体/场景。",
            MessageType.None);

        using (new GUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("🔄 重建预览", GUILayout.Height(30)))
            {
                content.EditorRebuildPreview();
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.4f);
            if (GUILayout.Button("🗑 清除预览", GUILayout.Height(30)))
            {
                content.EditorClearPreview();
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = Color.white;
        }
    }

    /// <summary>
    /// 绘制子控件(cell)尺寸调整区：可直接修改模板cell尺寸并应用预览或保存到预制体/场景。
    /// </summary>
    private void DrawCellSizeSection(ScrollGridBaseContent content)
    {
        RectTransform cellRect = content.EditorGetCellRectTransform();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("子控件(cell)尺寸调整", EditorStyles.boldLabel);

        if (cellRect == null)
        {
            EditorGUILayout.HelpBox("未设置tempCell（或tempCell不是UI控件），无法调整子控件尺寸。", MessageType.Warning);
            return;
        }

        // 首次或切换目标后，从模板cell读取当前尺寸
        if (!cellSizeLoaded)
        {
            editCellSize = cellRect.sizeDelta;
            cellSizeLoaded = true;
        }

        EditorGUILayout.HelpBox(
            "直接调整模板cell(tempCell)的尺寸：【应用并预览】实时查看效果（可撤销）；" +
            "【保存尺寸】写入并保存到预制体/场景。",
            MessageType.None);

        editCellSize = EditorGUILayout.Vector2Field("尺寸 (宽 x 高)", editCellSize);

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("↺ 读取当前尺寸", GUILayout.Height(26)))
            {
                editCellSize = cellRect.sizeDelta;
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("▶ 应用并预览", GUILayout.Height(26)))
            {
                ApplyCellSize(content, cellRect, editCellSize, false);
            }

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("💾 保存尺寸", GUILayout.Height(26)))
            {
                ApplyCellSize(content, cellRect, editCellSize, true);
            }
            GUI.backgroundColor = Color.white;
        }
    }

    #endregion

    #region 内部逻辑

    /// <summary>
    /// 把编辑中的尺寸应用到模板cell：记录Undo并刷新预览；save为true时额外标记脏并保存到磁盘。
    /// </summary>
    private void ApplyCellSize(ScrollGridBaseContent content, RectTransform cellRect, Vector2 size, bool save)
    {
        if (cellRect == null)
        {
            return;
        }

        // 记录Undo，支持Ctrl+Z回退
        Undo.RecordObject(cellRect, "Adjust ScrollGrid Cell Size");
        cellRect.sizeDelta = size;
        EditorUtility.SetDirty(cellRect);

        if (save)
        {
            // 标记组件与cell为脏，并落盘保存（与项目其它Inspector保存方式一致）
            EditorUtility.SetDirty(content);
            EditorUtility.SetDirty(cellRect.gameObject);
            // 场景中的预制体实例：记录属性修改，确保改动归属实例
            if (PrefabUtility.IsPartOfPrefabInstance(content))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(cellRect);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ScrollGrid] 已保存子控件尺寸: {size}");
        }

        // 正在预览时重建以即时反映新尺寸
        if (content.EditorIsPreviewing)
        {
            content.EditorRebuildPreview();
        }
        SceneView.RepaintAll();
    }

    #endregion
}
