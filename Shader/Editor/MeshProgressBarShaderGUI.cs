using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FrameWork/URP/MeshProgressBar 进度条 shader 的材质面板。
/// 把参数按 形状/进度/背景/背景渐变/填充/合成/圆形/旋转/高光/光照/渲染面 分组为可折叠区块显示(轻量版 PaletteFX 风格)，
/// 带一级开关的分组在开关关闭时置灰组内参数。按"属性存在才画"自适应，末尾兜底"其他"组避免漏显示。
/// </summary>
public class MeshProgressBarShaderGUI : ShaderGUI
{
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

    // 按分组顺序排列；圆形/旋转相关分组仅对圆形模式有意义, 标签中已注明。
    private static readonly Section[] sections = new Section[]
    {
        new Section("形状",         null, new[] { "_ShapeType" }),
        new Section("进度",         null, new[] { "_Progress", "_FillDirection", "_EdgeSoftness" }),
        new Section("背景",         null, new[] { "_BgMap", "_BgColor" }),
        new Section("背景渐变",     "_BgGradientEnable", new[] { "_BgColor2", "_BgGradientDirection" }),
        new Section("进度填充",     null, new[] { "_FillMap", "_FillColor" }),
        new Section("合成",         null, new[] { "_FillShowThrough" }),
        new Section("圆形",         null, new[] { "_RadialDirection", "_RadialStartAngle" }),
        new Section("圆形-背景旋转", "_BgRotateEnable",   new[] { "_BgRotateSpeed", "_BgRotateDirection" }),
        new Section("圆形-进度旋转", "_FillRotateEnable", new[] { "_FillRotateSpeed", "_FillRotateDirection" }),
        new Section("高光",         "_HighlightEnable",  new[] { "_BgHighlight", "_FillHighlight" }),
        new Section("光照",         "_LitEnable", new string[0]),
        new Section("渲染面",       null, new[] { "_Cull" }),
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

    /// <summary>绘制整个材质面板：分组折叠区块 + 底部渲染队列/实例化。</summary>
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        HashSet<string> drawn = new HashSet<string>();

        foreach (Section section in sections)
        {
            DrawSection(materialEditor, properties, section, drawn);
        }

        DrawRemaining(materialEditor, properties, drawn);

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

        // 一级开关([Toggle]/[Toggle(_LIT_ON)] 时 ShaderProperty 自动切 keyword)
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
        string key = "MeshProgressBarShaderGUI_Foldout_" + title;
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
