using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// GrassWindSway / TreeWindSway 系列 shader 的通用材质面板。
/// 把参数按 表面/光照/描边/风摆/抖动... 分组为可折叠区块显示(轻量版 PaletteFX 风格)。
/// 6 个 shader(粒子 · Mesh · SpriteRenderer × 草/树)共用本 GUI；粒子草/树已把 Lit/Unlit
/// 合并为单 shader、用 _LitEnable(ToggleOff _UNLIT_ON) 切换受光。
/// 表面类型/渲染模式/Alpha 裁剪/渲染面 由通用助手 <see cref="SurfaceOptionsGUI"/> 合并为"渲染设置"
/// 折叠组绘制并把预设同步为实际混合/深度/队列状态(表面类型可设不透明/透明，默认不透明+开启 Alpha 裁剪)。
/// 按"属性存在才画"的方式自适应各 shader 不同的参数集，无需为每个 shader 单独写面板。
/// </summary>
public class WindSwayShaderGUI : ShaderGUI
{
    #region 分组定义

    /// <summary>一个可折叠分组：标题 + 可选一级开关(带 _X_ON keyword) + 组内参数名列表。</summary>
    private class Section
    {
        public string title;
        public string toggle;      // 一级开关属性名(为 null 表示无开关)；开关关闭时其余参数置灰
        public string[] props;     // 组内参数名(按显示顺序)

        public Section(string title, string toggle, string[] props)
        {
            this.title = title;
            this.toggle = toggle;
            this.props = props;
        }
    }

    // 全 shader 参数并集，按分组顺序排列；每个 shader 只会命中自己实际拥有的参数。
    // 表面类型/渲染模式/Alpha 裁剪(_Cutoff)/渲染面 不在此列，交由 SurfaceOptionsGUI 统一绘制成"渲染设置"组。
    private static readonly Section[] sections = new Section[]
    {
        new Section("表面",     null, new[] { "_BaseMap", "_BaseColor" }),
        new Section("光照",     "_LitEnable", new string[0]),
        new Section("描边",     "_OutlineEnable", new[] { "_OutlineColor", "_OutlineSize" }),
        new Section("柔和粒子", "_SoftParticlesEnabled", new[] { "_SoftParticleNearFade", "_SoftParticleFarFade" }),
        new Section("相机淡出", "_CameraFadeEnabled", new[] { "_CameraNearFade", "_CameraFarFade" }),
        new Section("位置偏移", null, new[] { "_PositionOffset" }),
        new Section("风摆",     null, new[] { "_WindSpeed", "_SwayStrength", "_SwayFrequency", "_WindDir", "_BendStrength", "_AnchorBottom", "_Stiffness" }),
        new Section("抖动",     null, new[] { "_FlutterStrength", "_FlutterSpeed" }),
        new Section("阴影",     null, new[] { "_ShadowGIStrength" }),
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

    /// <summary>SurfaceOptionsGUI 用的渲染状态签名(表面类型+Alpha 裁剪)，用于检测切换以重设混合/深度/队列。</summary>
    private float lastSurfaceStateKey = float.NaN;

    /// <summary>绘制整个材质面板：分组折叠区块 + 通用"渲染设置"组 + 底部渲染队列/实例化。</summary>
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

        // 把表面类型/渲染模式预设同步为实际混合/深度/RenderType/队列状态
        SurfaceOptionsGUI.Sync(materialEditor, properties, ref lastSurfaceStateKey);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("渲染选项", EditorStyles.boldLabel);
        materialEditor.RenderQueueField();
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

        // 一级开关(带 [Toggle(_X_ON)] 时 ShaderProperty 自动切 keyword)
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

    #endregion

    #region 辅助方法

    /// <summary>带 EditorPrefs 持久化的折叠标题(默认展开)。</summary>
    private bool Foldout(string title)
    {
        string key = "WindSwayShaderGUI_Foldout_" + title;
        bool state = EditorPrefs.GetBool(key, true);
        bool next = EditorGUILayout.Foldout(state, title, true, FoldoutStyle);
        if (next != state) EditorPrefs.SetBool(key, next);
        return next;
    }

    /// <summary>属性是否带 HideInInspector 标记。</summary>
    private static bool IsHidden(MaterialProperty p)
    {
        return (p.propertyFlags & UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector) != 0;
    }

    #endregion
}
