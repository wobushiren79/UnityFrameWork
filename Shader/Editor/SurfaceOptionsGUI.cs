using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 通用"渲染设置"材质面板助手：把 表面类型 / 渲染模式(混合) / Alpha 裁剪 / 渲染面 合到同一个折叠组，
/// 并把这些预设同步为实际渲染状态(_SrcBlend / _DstBlend / _ZWrite / 渲染队列 / RenderType)。
/// 任意 shader 的 CustomEditor 只需：① 在 Properties 声明下列属性 ② include Common/SurfaceOptions.hlsl
/// ③ 在面板里调用 <see cref="Draw"/> 与 <see cref="Sync"/> 即可复用(与具体效果解耦)。
/// 依赖属性(存在才画/才同步)：_Surface、_BlendMode、_AlphaClip(_ALPHATEST_ON)、_Cutoff、_Cull、_SrcBlend、_DstBlend、_ZWrite。
/// </summary>
public static class SurfaceOptionsGUI
{
    #region 常量

    /// <summary>合并折叠组标题(供宿主 GUI 的折叠绘制器登记 EditorPrefs)。</summary>
    public const string FoldoutTitle = "渲染设置";

    #endregion

    #region 面板绘制

    /// <summary>
    /// 绘制"渲染设置"折叠组：表面类型 → 渲染模式(不透明时置灰) → Alpha 裁剪开关+阈值 → 渲染面。
    /// 命中的属性(含隐藏的 _SrcBlend/_DstBlend/_ZWrite)登记进 drawn，避免落入宿主的"其他"兜底组。
    /// </summary>
    /// <param name="foldout">宿主 GUI 的折叠绘制器(传入标题返回是否展开)，用于保持面板风格统一。</param>
    public static void Draw(MaterialEditor editor, MaterialProperty[] properties, HashSet<string> drawn, Func<string, bool> foldout)
    {
        MaterialProperty surface   = Find("_Surface",   properties);
        MaterialProperty blendMode = Find("_BlendMode", properties);
        MaterialProperty alphaClip = Find("_AlphaClip", properties);
        MaterialProperty cutoff    = Find("_Cutoff",    properties);
        MaterialProperty cull      = Find("_Cull",      properties);

        // 组内一个可见参数都没有则整组不画
        if (surface == null && blendMode == null && alphaClip == null && cull == null)
        {
            return;
        }

        bool open = foldout(FoldoutTitle);

        // 无论展开与否都登记(含隐藏驱动量), 避免被"其他"兜底组重复显示
        Register(drawn, surface); Register(drawn, blendMode); Register(drawn, alphaClip); Register(drawn, cutoff); Register(drawn, cull);
        Register(drawn, Find("_SrcBlend", properties));
        Register(drawn, Find("_DstBlend", properties));
        Register(drawn, Find("_ZWrite",   properties));

        if (!open)
        {
            return;
        }

        EditorGUI.indentLevel++;

        if (surface != null)
        {
            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(surface, surface.displayName);
            // 切到不透明时默认打开 Alpha 裁剪(本项目不透明网格多为镂空 cutout, 需按轮廓裁剪)；用户仍可手动再关闭
            if (EditorGUI.EndChangeCheck() && alphaClip != null && Mathf.RoundToInt(surface.floatValue) == 0)
            {
                SetAlphaClip(editor, alphaClip, true);
            }
        }

        bool opaque = surface != null && Mathf.RoundToInt(surface.floatValue) == 0;

        // 渲染模式(混合预设)仅透明表面有意义, 不透明时置灰
        if (blendMode != null)
        {
            using (new EditorGUI.DisabledScope(opaque))
            {
                editor.ShaderProperty(blendMode, blendMode.displayName);
            }
        }

        // Alpha 裁剪开关([Toggle(_ALPHATEST_ON)] 自动切 keyword) + 阈值(仅开启时可编辑)
        if (alphaClip != null)
        {
            editor.ShaderProperty(alphaClip, alphaClip.displayName);
            if (cutoff != null)
            {
                using (new EditorGUI.DisabledScope(alphaClip.floatValue <= 0.5f))
                {
                    editor.ShaderProperty(cutoff, cutoff.displayName);
                }
            }
        }
        else if (cutoff != null)
        {
            editor.ShaderProperty(cutoff, cutoff.displayName);
        }

        if (cull != null)
        {
            editor.ShaderProperty(cull, cull.displayName);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(2);
    }

    #endregion

    #region 渲染状态同步

    /// <summary>
    /// 把表面类型/渲染模式/Alpha 裁剪预设同步为实际渲染状态到所有选中材质。
    /// _SrcBlend/_DstBlend/_ZWrite 每帧幂等同步(仅值变化才写)；RenderType 标签在渲染状态切换时重设；
    /// 渲染队列仅在 manageQueue=true 时随切换重设(保留手动改的队列)。
    /// </summary>
    /// <param name="lastStateKey">宿主持有的上次渲染状态签名(首帧传 float.NaN)，用于检测切换。</param>
    /// <param name="manageQueue">是否由本助手管理 renderQueue：宿主若另有队列控制(如"优先级"滑条)应传 false，仅同步混合/深度/RenderType。</param>
    public static void Sync(MaterialEditor editor, MaterialProperty[] properties, ref float lastStateKey, bool manageQueue = true)
    {
        MaterialProperty surfaceProp = Find("_Surface", properties);
        if (surfaceProp == null)
        {
            return;
        }

        int surface = Mathf.RoundToInt(surfaceProp.floatValue);
        MaterialProperty clipProp = Find("_AlphaClip", properties);
        bool alphaClipOn = clipProp != null && clipProp.floatValue > 0.5f;

        // 渲染状态签名 = 表面类型*10 + Alpha 裁剪；切换时(且非首帧/非多值)才重设队列与 RenderType
        float stateKey = surface * 10 + (alphaClipOn ? 1 : 0);
        bool stateChanged = !surfaceProp.hasMixedValue && !float.IsNaN(lastStateKey) && stateKey != lastStateKey;
        lastStateKey = stateKey;

        float src, dst, zwrite;
        if (surface == 0)
        {
            // 不透明: 覆盖式绘制 + 写深度
            src = (float)BlendMode.One;
            dst = (float)BlendMode.Zero;
            zwrite = 1f;
        }
        else
        {
            // 透明: 混合因子由渲染模式预设决定, 不写深度
            zwrite = 0f;
            MaterialProperty mode = Find("_BlendMode", properties);
            switch (mode != null ? Mathf.RoundToInt(mode.floatValue) : 0)
            {
                case 1: src = (float)BlendMode.SrcAlpha; dst = (float)BlendMode.One; break;                 // 加法叠加发光
                case 2: src = (float)BlendMode.One;      dst = (float)BlendMode.OneMinusSrcAlpha; break;    // 预乘透明
                default: src = (float)BlendMode.SrcAlpha; dst = (float)BlendMode.OneMinusSrcAlpha; break;   // 标准透明
            }
        }

        foreach (UnityEngine.Object target in editor.targets)
        {
            Material mat = target as Material;
            if (mat == null) continue;

            if (mat.HasProperty("_SrcBlend") && mat.GetFloat("_SrcBlend") != src)    mat.SetFloat("_SrcBlend", src);
            if (mat.HasProperty("_DstBlend") && mat.GetFloat("_DstBlend") != dst)    mat.SetFloat("_DstBlend", dst);
            if (mat.HasProperty("_ZWrite")   && mat.GetFloat("_ZWrite")   != zwrite) mat.SetFloat("_ZWrite", zwrite);

            if (stateChanged)
            {
                // RenderType 标签始终随表面状态重设；renderQueue 仅在本助手托管队列时才重设(否则留给宿主)
                if (surface == 0 && alphaClipOn)
                {
                    // 不透明镂空: 归入 AlphaTest 阶段
                    mat.SetOverrideTag("RenderType", "TransparentCutout");
                    if (manageQueue) mat.renderQueue = (int)RenderQueue.AlphaTest;
                }
                else if (surface == 0)
                {
                    mat.SetOverrideTag("RenderType", "Opaque");
                    if (manageQueue) mat.renderQueue = (int)RenderQueue.Geometry;
                }
                else
                {
                    mat.SetOverrideTag("RenderType", "Transparent");
                    if (manageQueue) mat.renderQueue = (int)RenderQueue.Transparent;
                }
            }
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>按名查找材质属性(本类非 ShaderGUI 子类, 无法用其 protected FindProperty, 故自实现)。找不到返回 null。</summary>
    private static MaterialProperty Find(string name, MaterialProperty[] properties)
    {
        foreach (MaterialProperty p in properties)
        {
            if (p != null && p.name == name) return p;
        }
        return null;
    }

    /// <summary>属性非空则登记进已绘制集合。</summary>
    private static void Register(HashSet<string> drawn, MaterialProperty p)
    {
        if (p != null) drawn.Add(p.name);
    }

    /// <summary>同步设置 Alpha 裁剪的属性值与 _ALPHATEST_ON 关键字(二者须一致，否则面板值与实际变体脱节)。</summary>
    private static void SetAlphaClip(MaterialEditor editor, MaterialProperty alphaClip, bool on)
    {
        alphaClip.floatValue = on ? 1f : 0f;
        foreach (UnityEngine.Object target in editor.targets)
        {
            Material mat = target as Material;
            if (mat == null) continue;
            if (on) mat.EnableKeyword("_ALPHATEST_ON");
            else    mat.DisableKeyword("_ALPHATEST_ON");
        }
    }

    #endregion
}
