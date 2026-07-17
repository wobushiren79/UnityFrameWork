using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 网格通用 Shader(FrameWork/URP/MeshCommon1) 的材质面板：
/// 把参数按 表面/光照/描边/变换/自动旋转 分组为可折叠区块，末尾的 表面类型/渲染模式/Alpha 裁剪/渲染面
/// 由通用助手 <see cref="SurfaceOptionsGUI"/> 合并成一个"渲染设置"折叠组绘制并同步混合/深度状态
/// (表面类型可设不透明/透明)；再追加一个仿 URP Lit 的「优先级(Sorting Priority)」滑条，
/// 以相对基础队列(AlphaTest 2450)的偏移量直接改写 material.renderQueue，控制同深度/共面时的渲染先后。
/// 注意：队列由本面板的"优先级"滑条独占管理，故对 SurfaceOptionsGUI.Sync 传 manageQueue=false，
/// 避免切换表面类型时 Sync 把滑条设置的队列(如地毯 3000)重置掉。
/// </summary>
public class MeshCommonShaderGUI : ShaderGUI
{
    #region 常量

    /// <summary>优先级偏移的绝对范围：±1000，覆盖 Geometry(2000)~Transparent(3000+) 整个分层带，使地毯/贴花能压过道路等高队列物</summary>
    private const int QueueOffsetRange = 1000;

    /// <summary>不透明裁剪表面的基础渲染队列(RenderQueue.AlphaTest = 2450)</summary>
    private const int BaseAlphaTestQueue = (int)RenderQueue.AlphaTest;

    #endregion

    #region 分组定义

    /// <summary>一个可折叠分组：标题 + 可选一级开关 + 组内参数名列表(开关关闭时组内参数置灰)。</summary>
    private class Section
    {
        public string title;
        public string toggle;      // 一级开关属性名(为 null 表示无开关)
        public string[] props;     // 组内参数名(按显示顺序)

        public Section(string title, string toggle, string[] props)
        {
            this.title = title;
            this.toggle = toggle;
            this.props = props;
        }
    }

    // 表面类型/渲染模式/Alpha 裁剪/渲染面 不在此列, 交由 SurfaceOptionsGUI 统一绘制成"渲染设置"组。
    // 组内参数在当前 shader 上不存在时整组自动跳过(DrawSection 的 present.Count 判定), 故本数组是"全部宿主 shader 的参数并集":
    // MeshCommon1 不显示火球/火星组、MeshFireBallInstanced1 不显示光照/描边组，各取所需。
    private static readonly Section[] sections = new Section[]
    {
        new Section("表面", null,            new[] { "_BaseMap", "_BaseColor" }),
        new Section("光照", "_LitEnable",    new string[0]),
        new Section("描边", "_OutlineEnable", new[] { "_OutlineColor", "_OutlineSize" }),
        new Section("变换", null,            new[] { "_VertexScale", "_VertexOffset", "_VertexRotation" }),
        new Section("自动旋转", "_AutoRotateEnable", new[] { "_RotateDirection", "_RotateSpeed" }),
        // 火球(MeshFireBallInstanced1 专属)：**中心火球在前, 火星在后**(本数组顺序 = Inspector 显示顺序,
        // 与该 shader 的 Properties/Header 排列保持一致; 改这里就是改面板上下顺序)
        new Section("中心火球", "_CoreEnable", new[] { "_CoreMap", "_CoreSize", "_CoreColorHot", "_CoreColorCold",
                                                      "_CoreNoiseScale", "_CoreNoiseSpeed", "_CoreNoiseStrength",
                                                      "_CoreEdgeSoft", "_CorePulseAmount", "_CorePulseSpeed" }),
        new Section("火星运动", null, new[] { "_SparkRate", "_SparkDistance", "_SparkEase", "_SparkGravity", "_SparkOriginRadius" }),
        new Section("火星外观", null, new[] { "_SparkSizeStart", "_SparkSizeEnd", "_SparkColorStart", "_SparkColorEnd", "_SparkFadeIn", "_SparkFadeOut" }),
    };

    #endregion

    #region 样式缓存

    private static GUIStyle foldoutStyle;

    /// <summary>惰性构建加粗折叠标题样式。</summary>
    private static GUIStyle FoldoutStyle
    {
        get
        {
            if (foldoutStyle == null)
            {
                foldoutStyle = new GUIStyle(EditorStyles.foldout);
                foldoutStyle.fontStyle = FontStyle.Bold;
            }
            return foldoutStyle;
        }
    }

    #endregion

    #region 面板绘制

    /// <summary>SurfaceOptionsGUI 用的渲染状态签名(表面类型+Alpha 裁剪), 用于检测切换以重设 RenderType 标签。</summary>
    private float lastSurfaceStateKey = float.NaN;

    /// <summary>绘制整个材质面板：分组折叠区块 + 通用"渲染设置"组 + 优先级滑条 + 实例化。</summary>
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        HashSet<string> drawn = new HashSet<string>();

        foreach (Section section in sections)
        {
            DrawSection(materialEditor, properties, section, drawn);
        }

        // 表面类型/渲染模式/Alpha 裁剪/渲染面 合并组(复用宿主的折叠绘制器保持风格统一)
        SurfaceOptionsGUI.Draw(materialEditor, properties, drawn, Foldout);

        DrawRemaining(materialEditor, properties, drawn);

        // 同步混合/深度/RenderType 到实际渲染状态；队列交给下面的"优先级"滑条独占管理
        SurfaceOptionsGUI.Sync(materialEditor, properties, ref lastSurfaceStateKey, manageQueue: false);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("渲染选项", EditorStyles.boldLabel);
        DrawSortingPriority(materialEditor, properties);
        materialEditor.EnableInstancingField();
    }

    /// <summary>绘制单个分组：无命中参数则整组跳过；有一级开关时开关关闭则组内其余参数置灰。</summary>
    private void DrawSection(MaterialEditor editor, MaterialProperty[] properties, Section section, HashSet<string> drawn)
    {
        MaterialProperty toggleProp = section.toggle != null ? FindProperty(section.toggle, properties, false) : null;

        // 收集组内实际存在(且未隐藏)的参数
        List<MaterialProperty> present = new List<MaterialProperty>();
        foreach (string name in section.props)
        {
            MaterialProperty p = FindProperty(name, properties, false);
            if (p != null && !IsHidden(p))
            {
                present.Add(p);
            }
        }

        // 开关和参数都不存在则整组不显示
        if (toggleProp == null && present.Count == 0)
        {
            return;
        }

        if (!Foldout(section.title))
        {
            // 折叠状态下仍登记已处理，避免落入"其他"兜底组
            if (toggleProp != null) drawn.Add(toggleProp.name);
            foreach (MaterialProperty p in present) drawn.Add(p.name);
            return;
        }

        EditorGUI.indentLevel++;

        // 一级开关([Toggle(_LIT_ON)]/[Toggle(_OUTLINE_ON)] 时 ShaderProperty 自动切 keyword)
        bool enabled = true;
        if (toggleProp != null)
        {
            editor.ShaderProperty(toggleProp, toggleProp.displayName);
            drawn.Add(toggleProp.name);
            enabled = toggleProp.floatValue > 0.5f;
        }

        // 组内参数：开关关闭时置灰(不隐藏，方便预览已配的值)
        using (new EditorGUI.DisabledScope(!enabled))
        {
            foreach (MaterialProperty p in present)
            {
                editor.ShaderProperty(p, p.displayName);
                drawn.Add(p.name);
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(2);
    }

    /// <summary>兜底：任何未被分组覆盖的可见参数，统一放到"其他"组，避免漏显示。</summary>
    private void DrawRemaining(MaterialEditor editor, MaterialProperty[] properties, HashSet<string> drawn)
    {
        List<MaterialProperty> rest = new List<MaterialProperty>();
        foreach (MaterialProperty p in properties)
        {
            if (!drawn.Contains(p.name) && !IsHidden(p))
            {
                rest.Add(p);
            }
        }

        if (rest.Count == 0) return;
        if (!Foldout("其他")) return;

        EditorGUI.indentLevel++;
        foreach (MaterialProperty p in rest)
        {
            editor.ShaderProperty(p, p.displayName);
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>绘制「优先级」滑条，变更时把 2450+偏移 写入所选材质的 renderQueue</summary>
    private void DrawSortingPriority(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty queueOffsetProp = FindProperty("_QueueOffset", properties, false);
        if (queueOffsetProp == null)
            return;

        GUIContent label = new GUIContent(
            "优先级 (Sorting Priority)",
            "相对基础队列(2450)的偏移：值越大越晚渲染、同深度/共面时越显示在前。最终 renderQueue = 2450 + 该值，范围 -1000~1000(可压过道路2999等高队列物)。");

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = queueOffsetProp.hasMixedValue;   // 多选材质取值不一致时显示 dash
        int newOffset = EditorGUILayout.IntSlider(label, (int)queueOffsetProp.floatValue, -QueueOffsetRange, QueueOffsetRange);
        EditorGUI.showMixedValue = false;

        if (EditorGUI.EndChangeCheck())
        {
            queueOffsetProp.floatValue = newOffset;
            // 逐个所选材质写入队列(支持多选批量修改)
            foreach (Object target in materialEditor.targets)
            {
                if (target is Material material)
                    material.renderQueue = BaseAlphaTestQueue + newOffset;
            }
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>带 EditorPrefs 持久化的折叠标题(默认展开)。</summary>
    private bool Foldout(string title)
    {
        string key = "MeshCommonShaderGUI_Foldout_" + title;
        bool state = EditorPrefs.GetBool(key, true);
        bool next = EditorGUILayout.Foldout(state, title, true, FoldoutStyle);
        if (next != state) EditorPrefs.SetBool(key, next);
        return next;
    }

    /// <summary>属性是否带 HideInInspector 标记。</summary>
    private static bool IsHidden(MaterialProperty p)
    {
        return (p.propertyFlags & ShaderPropertyFlags.HideInInspector) != 0;
    }

    #endregion
}
