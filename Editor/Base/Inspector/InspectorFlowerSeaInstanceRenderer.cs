using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FlowerSeaInstanceRenderer 的自定义 Inspector：
/// <para>① 全部参数中文化标注（字段名→中文 GUIContent 映射 + 悬停中文提示；枚举弹窗同步中文化）；</para>
/// <para>② 贴图区按 textureMode 条件显示——图集模式只显示图集字段（关闭「使用图集全部格子」时展开行列 toggle 网格，点选要用的格子），单图模式只显示单图字段；</para>
/// <para>③ 底部提供「重新生成花海 / 重置全部消散 / 测试踩踏」手动按钮（组件本身已对 Inspector 改动做自动实时刷新，按钮作兜底）；</para>
/// <para>④ 状态行实时显示花朵总数/已消散数。</para>
/// </summary>
[CustomEditor(typeof(FlowerSeaInstanceRenderer))]
public class InspectorFlowerSeaInstanceRenderer : Editor
{
    #region 常量

    //贴图区字段名（主循环跳过，改由 DrawTextureModeFields 按模式条件绘制）
    private static readonly string[] textureFieldNames =
    {
        "atlasTexture", "atlasGrid", "atlasUseAllCells", "atlasSelectedCells", "manualRects", "textureList", "packAtlasSize", "packPadding",
    };

    //地形区字段名（主循环跳过，改由 DrawHeightModeFields 按高度模式条件绘制）
    private static readonly string[] terrainFieldNames =
    {
        "terrainLayer", "rayStartHeight", "rayMaxDistance",
        "terrainRenderer", "terrainHeightMap", "terrainHeightScale", "terrainHeightInvert",
    };

    /// <summary>字段名 → 中文标注（标签 + 悬停提示）；不在表内的字段按原名显示</summary>
    private static readonly Dictionary<string, GUIContent> fieldContents = new Dictionary<string, GUIContent>
    {
        //花海范围
        { "rangeSize", new GUIContent("范围大小(X/Z)", "花海范围(世界单位)，以组件位置为中心") },
        { "flowerCount", new GUIContent("花朵总数", "严格生效；超过格子数时多轮复用格子，密度过大会重叠，请加大范围或调小缩放") },
        { "randomSeed", new GUIContent("随机种子", "同种子生成结果一致") },
        { "gridCells", new GUIContent("抖动网格(列×行)", "按洗牌序分格布点，均匀防扎堆；花朵数超过格子数时多轮复用格子") },
        { "scaleRange", new GUIContent("缩放随机区间", "每朵花的世界缩放随机区间") },
        { "yOffset", new GUIContent("贴地Y偏移", "防 z-fight 的 Y 抬升（战斗道路面惯例 0.0001，花默认略高）") },
        { "generateOnEnable", new GUIContent("激活时自动生成", "关闭则需调用方手动 Generate()") },
        //贴图
        { "textureMode", new GUIContent("贴图来源模式", "图集：一张贴图均分/手动Rect；单图列表：运行时打包(需开Read/Write)") },
        { "atlasTexture", new GUIContent("花图集贴图", "图集模式：整张贴图，按均分/行列子集/手动Rect切片") },
        { "atlasGrid", new GUIContent("图集均分(列×行)", "自动均分的列数×行数") },
        { "atlasUseAllCells", new GUIContent("使用图集全部格子", "开=均分网格全部格子都参与随机；关=只用下方网格中选中的行列格子") },
        { "atlasSelectedCells", new GUIContent("选中格子(列,行)", "仅「使用图集全部格子」关闭时生效；x=列 y=行，0 起，行 0=贴图最下行") },
        { "manualRects", new GUIContent("手动UV Rect列表", "非空时覆盖均分与选中格子（UV 空间 0~1）") },
        { "textureList", new GUIContent("单图列表", "独立贴图列表，运行时 PackTextures 打包（要求每张开 Read/Write）") },
        { "packAtlasSize", new GUIContent("打包图集边长", "单图模式打包图集的边长上限(像素)") },
        { "packPadding", new GUIContent("打包间距(像素)", "单图模式打包时各贴图间距") },
        //地形高度适配
        { "heightMode", new GUIContent("高度来源模式", "固定高度：平地用 / 射线采样：有碰撞体的网格地形 / 高度图地形：GPU 顶点位移地形（如 MeshTerrain，射线打不到位移后的高度）") },
        { "terrainLayer", new GUIContent("地形层", "射线模式：射线命中的 Layer（目标地形网格所在层）") },
        { "rayStartHeight", new GUIContent("射线起点抬升", "射线模式：射线起点相对组件位置的抬升高度") },
        { "rayMaxDistance", new GUIContent("射线最大距离", "射线模式：向下射线的最大检测距离") },
        { "terrainRenderer", new GUIContent("地形网格渲染器", "高度图模式：地形的 MeshRenderer；自动从其材质读取 _HeightMap/_HeightScale/_HeightInvert") },
        { "terrainHeightMap", new GUIContent("手动高度图", "高度图模式：留空则自动读地形材质；无需开 Read/Write（内部 GPU 回读）") },
        { "terrainHeightScale", new GUIContent("起伏高度", "高度图模式：顶点向上位移的世界高度（仅手动高度图时生效）") },
        { "terrainHeightInvert", new GUIContent("高度反转", "高度图模式：开=白为低黑为高（仅手动高度图时生效）") },
        //花朵形态
        { "shape", new GUIContent("花朵形态", "竖直立牌(yaw广告牌朝向镜头，可叠风摆) / 贴地平铺") },
        { "randomYaw", new GUIContent("随机朝向", "每朵花随机 yaw 朝向") },
        //消散
        { "dissolveDuration", new GUIContent("消散时长(秒)", "从被踩到完全消失的时长") },
        { "dissolveNoise", new GUIContent("消散噪声图", "不赋则用内置 Bayer 抖动矩阵（像素颗粒侵蚀）；赋 Perlin/云噪图则是团块状消散") },
        { "dissolveNoiseScale", new GUIContent("噪声平铺密度", "噪声在单朵花 UV 上的平铺密度，越大颗粒越细；花较小时建议 2~6") },
        { "dissolveEdgeBand", new GUIContent("开启消散边缘色带", "消散边缘染 HDR 色带（发光描边效果）") },
        { "dissolveEdgeColor", new GUIContent("消散边缘色(HDR)", "仅开启边缘色带后生效") },
        { "dissolveEdgeWidth", new GUIContent("消散边缘宽度", "仅开启边缘色带后生效") },
        //风摆
        { "windEnable", new GUIContent("开启风摆", "仅竖直立牌模式生效") },
        { "windSpeed", new GUIContent("风速", "风摆整体快慢") },
        { "swayStrength", new GUIContent("摆动幅度", "花头左右摇摆大小") },
        { "swayFrequency", new GUIContent("摆动频率", "摇摆频率") },
        { "stiffness", new GUIContent("茎硬度", "越大根部越不弯、越像硬茎") },
        //阴影
        { "castShadows", new GUIContent("开启阴影投射", "走 shader 内置 ShadowCaster Pass 投射镂空阴影（随消散同步消失）；改动即时生效不重建。Unlit 花海不接收阴影") },
        { "shadowRadius", new GUIContent("阴影显示半径", "以渲染相机为圆心的 XZ 平面距离，半径外的花不投影（顶点退化零开销，仍单次 draw call）；0=不限制全图投影") },
        //自动踩踏轮询
        { "pollTargetsEnable", new GUIContent("开启自动轮询", "开启后按间隔轮询目标位置自动踩踏；关闭时由调用方手动 TrampleAt") },
        { "pollTargets", new GUIContent("轮询目标列表", "把生物/角色的 Transform 拖进来即可") },
        { "pollInterval", new GUIContent("轮询间隔(秒)", "多久检查一次目标位置") },
        { "pollTrampleRadius", new GUIContent("轮询踩踏半径", "每个目标的踩踏半径(世界单位)") },
        //编辑器预览
        { "editModeLivePreview", new GUIContent("编辑模式实时预览", "非 Play 状态强制 Scene 视图按预览帧率重绘（风摆/消散动画可见）；关闭可省 GPU") },
        { "editModePreviewFps", new GUIContent("预览帧率上限", "编辑模式预览重绘的帧率上限（越低越省 GPU，动画流畅度随之下降）") },
    };

    //枚举中文化（按下标对齐声明顺序）
    private static readonly string[] textureModeNames = { "图集", "单图列表" };
    private static readonly string[] shapeNames = { "竖直立牌(广告牌)", "贴地平铺" };
    private static readonly string[] heightModeNames = { "固定高度", "射线采样", "高度图地形" };

    #endregion

    #region Inspector 绘制

    /// <summary>主入口：中文化标注 + 跳过非当前模式的贴图字段，其余按声明顺序绘制（含 Header 分组）</summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty it = serializedObject.GetIterator();
        it.NextVisible(true);
        // m_Script 字段只读绘制
        GUI.enabled = false;
        EditorGUILayout.PropertyField(it);
        GUI.enabled = true;
        while (it.NextVisible(false))
        {
            if (System.Array.IndexOf(textureFieldNames, it.name) >= 0) continue;
            if (System.Array.IndexOf(terrainFieldNames, it.name) >= 0) continue;
            if (it.name == "textureMode")
            {
                DrawHeaderFor(it);
                DrawEnumPopup(it, textureModeNames);
                DrawTextureModeFields();
                continue;
            }
            if (it.name == "heightMode")
            {
                DrawHeaderFor(it);
                DrawEnumPopup(it, heightModeNames);
                DrawHeightModeFields();
                continue;
            }
            if (it.name == "shape")
            {
                DrawHeaderFor(it);
                DrawEnumPopup(it, shapeNames);
                continue;
            }
            EditorGUILayout.PropertyField(it, GetFieldContent(it.name), true);
        }
        serializedObject.ApplyModifiedProperties();

        DrawStatus();
        DrawActionButtons();
        DrawHint();
    }

    /// <summary>取字段中文标注；未登记返回 null（PropertyField 回退默认名）</summary>
    private static GUIContent GetFieldContent(string fieldName)
    {
        return fieldContents.TryGetValue(fieldName, out GUIContent content) ? content : null;
    }

    /// <summary>枚举字段中文弹窗（PropertyField 不支持枚举项中文化，改用 Popup）</summary>
    private void DrawEnumPopup(SerializedProperty property, string[] displayNames)
    {
        int newIndex = EditorGUILayout.Popup(GetFieldContent(property.name), property.enumValueIndex, displayNames);
        if (newIndex != property.enumValueIndex)
            property.enumValueIndex = newIndex;
    }

    /// <summary>手动补画字段的 [Header] 分组标题（Popup 替代 PropertyField 后 Header 装饰不会自动绘制）</summary>
    private void DrawHeaderFor(SerializedProperty property)
    {
        FieldInfo fieldInfo = target.GetType().GetField(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        HeaderAttribute[] headers = fieldInfo?.GetCustomAttributes(typeof(HeaderAttribute), false) as HeaderAttribute[];
        if (headers != null && headers.Length > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(headers[0].header, EditorStyles.boldLabel);
        }
    }

    /// <summary>贴图模式条件字段：Atlas 画图集字段（含行列子集网格），SingleList 画单图三件套（同样中文化）</summary>
    private void DrawTextureModeFields()
    {
        SerializedProperty modeProp = serializedObject.FindProperty("textureMode");
        bool isAtlas = modeProp.enumNames[modeProp.enumValueIndex] == "Atlas";
        EditorGUI.indentLevel++;
        if (isAtlas)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("atlasTexture"), GetFieldContent("atlasTexture"));
            SerializedProperty gridProp = serializedObject.FindProperty("atlasGrid");
            EditorGUILayout.PropertyField(gridProp, GetFieldContent("atlasGrid"));
            SerializedProperty useAllProp = serializedObject.FindProperty("atlasUseAllCells");
            EditorGUILayout.PropertyField(useAllProp, GetFieldContent("atlasUseAllCells"));
            if (!useAllProp.boolValue)
                DrawAtlasCellGrid(serializedObject.FindProperty("atlasSelectedCells"), Mathf.Max(1, gridProp.vector2IntValue.x), Mathf.Max(1, gridProp.vector2IntValue.y));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("manualRects"), GetFieldContent("manualRects"), true);
        }
        else
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textureList"), GetFieldContent("textureList"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("packAtlasSize"), GetFieldContent("packAtlasSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("packPadding"), GetFieldContent("packPadding"));
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>图集行列选择网格：按均分列×行画 toggle 阵列（顶行=贴图最上行，与看图习惯一致），点击即选中/取消该格子</summary>
    private void DrawAtlasCellGrid(SerializedProperty cellsProp, int cols, int rows)
    {
        // 格子总数过大时 toggle 阵列会卡 Inspector，退化为直接画列表
        if (cols * rows > 256)
        {
            EditorGUILayout.PropertyField(cellsProp, GetFieldContent("atlasSelectedCells"), true);
            return;
        }
        // 当前选中集合（下标 = 行*列数+列；越界格子忽略，由组件 OnValidate 统一夹取）
        HashSet<int> selected = new HashSet<int>();
        for (int i = 0; i < cellsProp.arraySize; i++)
        {
            Vector2Int cell = cellsProp.GetArrayElementAtIndex(i).vector2IntValue;
            if (cell.x >= 0 && cell.x < cols && cell.y >= 0 && cell.y < rows)
                selected.Add(cell.y * cols + cell.x);
        }

        EditorGUILayout.LabelField($"点击选择使用的格子（共 {cols}列×{rows}行，顶行=贴图最上行）：", EditorStyles.miniBoldLabel);
        bool dirty = false;
        for (int r = rows - 1; r >= 0; r--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;
                // 列数较少时 toggle 上直接标注行列，列数多挤不下则只留 tooltip
                GUIContent label = cols <= 8 ? new GUIContent($"{c},{r}") : new GUIContent("", $"列 {c} 行 {r}");
                bool newValue = GUILayout.Toggle(selected.Contains(index), label, EditorStyles.miniButton);
                if (newValue) dirty |= selected.Add(index);
                else dirty |= selected.Remove(index);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
        {
            for (int i = 0; i < cols * rows; i++) selected.Add(i);
            dirty = true;
        }
        if (GUILayout.Button("清空", EditorStyles.miniButtonRight, GUILayout.Width(40)))
        {
            selected.Clear();
            dirty = true;
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"已选 {selected.Count}/{cols * rows}", EditorStyles.miniLabel, GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();
        if (dirty)
        {
            // 排序写回保证序列化列表稳定（便于版本对比），行列由下标还原
            List<int> sorted = new List<int>(selected);
            sorted.Sort();
            cellsProp.arraySize = sorted.Count;
            for (int i = 0; i < sorted.Count; i++)
                cellsProp.GetArrayElementAtIndex(i).vector2IntValue = new Vector2Int(sorted[i] % cols, sorted[i] / cols);
        }
        if (selected.Count == 0)
            EditorGUILayout.HelpBox("未选中任何格子：生成时会报错，请至少勾选一个格子或打开「使用图集全部格子」。", MessageType.Warning);
    }

    /// <summary>高度模式条件字段：射线采样画射线三件套，高度图地形画地形四件套（中文化）</summary>
    private void DrawHeightModeFields()
    {
        SerializedProperty modeProp = serializedObject.FindProperty("heightMode");
        string modeName = modeProp.enumNames[modeProp.enumValueIndex];
        EditorGUI.indentLevel++;
        if (modeName == "Raycast")
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainLayer"), GetFieldContent("terrainLayer"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rayStartHeight"), GetFieldContent("rayStartHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rayMaxDistance"), GetFieldContent("rayMaxDistance"));
        }
        else if (modeName == "HeightmapTerrain")
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainRenderer"), GetFieldContent("terrainRenderer"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainHeightMap"), GetFieldContent("terrainHeightMap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainHeightScale"), GetFieldContent("terrainHeightScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainHeightInvert"), GetFieldContent("terrainHeightInvert"));
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>状态行：花朵总数/已消散数（踩踏按钮按下立即有文字反馈）</summary>
    private void DrawStatus()
    {
        FlowerSeaInstanceRenderer renderer = (FlowerSeaInstanceRenderer)target;
        EditorGUILayout.LabelField($"状态：花朵总数 {renderer.FlowerCount}　已消散 {renderer.DissolvedCount}", EditorStyles.miniBoldLabel);
    }

    /// <summary>手动操作按钮：重新生成 / 重置消散 / 测试踩踏（编辑与 Play 模式均可用）</summary>
    private void DrawActionButtons()
    {
        GUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("重新生成花海"))
            foreach (Object t in targets) ((FlowerSeaInstanceRenderer)t).Generate();
        if (GUILayout.Button("重置全部消散"))
            foreach (Object t in targets) ((FlowerSeaInstanceRenderer)t).ResetSea();
        if (GUILayout.Button("测试踩踏(r=2)"))
            foreach (Object t in targets)
            {
                FlowerSeaInstanceRenderer renderer = (FlowerSeaInstanceRenderer)t;
                renderer.TrampleAt(renderer.transform.position, 2f);
            }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>机制提示</summary>
    private void DrawHint()
    {
        EditorGUILayout.HelpBox(
            "参数改动自动实时刷新（结构参数全量重建，表现参数仅刷材质）；非 Play 模式可直接预览。\n" +
            "单图模式要求贴图导入设置开启 Read/Write。",
            MessageType.None);
    }

    #endregion
}
