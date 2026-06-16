using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FrameWork/UI/Shader_UI_ImageEffect 的自定义材质面板。
/// 把各项特效折叠成分组：勾选开启后才展开对应子参数，避免一长串平铺参数看不清。
/// </summary>
public class ShaderGUIImageEffect : ShaderGUI
{
    #region 字段
    /// <summary>当前材质面板编辑器</summary>
    private MaterialEditor _materialEditor;
    /// <summary>当前材质的全部 Shader 属性</summary>
    private MaterialProperty[] _props;

    /// <summary>“基础设置”分组是否展开</summary>
    private bool _foldBase = true;
    /// <summary>“UGUI 遮罩(模板)”分组是否展开</summary>
    private bool _foldStencil = false;

    /// <summary>各特效分组的折叠状态(按 Toggle 属性名缓存)</summary>
    private readonly Dictionary<string, bool> _foldStates = new Dictionary<string, bool>();

    /// <summary>分组外框样式(延迟初始化)</summary>
    private GUIStyle _boxStyle;
    /// <summary>分组标题样式(延迟初始化)</summary>
    private GUIStyle _headerStyle;
    #endregion

    #region 生命周期
    /// <summary>
    /// 绘制整个材质面板。
    /// </summary>
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        _materialEditor = materialEditor;
        _props = properties;
        InitStyles();

        // 基础设置
        _foldBase = DrawFoldoutGroup("基础设置", _foldBase, () =>
        {
            Prop("_MainTex", "主纹理");
            Prop("_Color", "整体着色(乘法)");
            Prop("_MainAlpha", "整体透明度");
            EditorGUILayout.Space(2);
            Prop("_SrcBlend", "源混合因子");
            Prop("_DstBlend", "目标混合因子");
            EditorGUILayout.Space(2);
            Prop("_InternalTime", "使用内置时间");
            if (GetFloat("_InternalTime") < 0.5f)
            {
                EditorGUI.indentLevel++;
                Prop("_DeltaTime", "外部驱动时间(秒)");
                EditorGUI.indentLevel--;
            }
            Prop("_UseUIClipRect", "启用UI矩形裁剪");
            Prop("_UseUIAlphaClip", "启用UI透明裁剪");
        });

        EditorGUILayout.Space(4);

        // 各特效模块：勾选开启后才展开(顺序与片元处理顺序一致)
        DrawToggleGroup("主纹理UV变换 (旋转/流动)", "_MainUVOn", "_MAINUV_ON", () =>
        {
            Prop("_MainTexRotation", "主纹理旋转");
            Prop("_ScrollSpeedX", "横向流动速度");
            Prop("_ScrollSpeedY", "纵向流动速度");
        });

        DrawToggleGroup("极坐标 (径向/漩涡)", "_PolarOn", "_POLAR_ON", () =>
        {
            Prop("_PolarRotateSpeed", "旋转速度");
            Prop("_PolarScale", "缩放(半径,角度)");
            Prop("_PolarOffset", "偏移(半径,角度)");
        });

        DrawToggleGroup("扭曲 (UV 变形)", "_DistortOn", "_DISTORT_ON", () =>
        {
            Prop("_DistortTex", "扭曲纹理(噪声)");
            Prop("_DistortChannel", "取样通道");
            Prop("_DistortStrength", "扭曲强度");
            Prop("_DistortRotation", "纹理旋转");
            Prop("_DistortScrollX", "横向流动速度");
            Prop("_DistortScrollY", "纵向流动速度");
        });

        DrawToggleGroup("遮罩纹理", "_MaskOn", "_MASKTEX_ON", () =>
        {
            Prop("_MaskTex", "遮罩纹理");
            Prop("_MaskChannel", "遮罩取样通道");
            Prop("_MaskRotation", "遮罩旋转");
            Prop("_MaskScrollX", "横向流动速度");
            Prop("_MaskScrollY", "纵向流动速度");
        });

        DrawToggleGroup("流光扫光", "_ShineOn", "_SHINE_ON", () =>
        {
            Prop("_ShineColor", "流光颜色");
            Prop("_ShineAngle", "流光方向角度");
            Prop("_ShineWidth", "流光宽度");
            Prop("_ShineSoftness", "流光边缘柔和度");
            Prop("_ShineSpeed", "流光速度");
            Prop("_ShineInterval", "流光间隔(停顿)");
            Prop("_ShineIntensity", "流光强度");
            Prop("_ShineMaskByAlpha", "仅在不透明区域显示");
        });

        DrawToggleGroup("渐变叠色", "_GradientOn", "_GRADIENT_ON", () =>
        {
            Prop("_GradientColorA", "渐变起始色");
            Prop("_GradientColorB", "渐变结束色");
            Prop("_GradientAngle", "渐变方向角度");
            Prop("_GradientScale", "渐变缩放(对比度)");
            Prop("_GradientOffset", "渐变偏移");
            Prop("_GradientScrollSpeed", "渐变流动速度");
            Prop("_GradientBlend", "渐变混合模式");
            Prop("_GradientIntensity", "渐变混合强度");
        });

        DrawToggleGroup("色相流动(彩虹)", "_HueOn", "_HUE_ON", () =>
        {
            Prop("_HueShiftSpeed", "色相流动速度");
            Prop("_HueShiftOffset", "色相起始偏移");
            Prop("_HueRangeScale", "色相空间渐变");
            Prop("_HueAngle", "色相铺开方向角度");
            Prop("_HueSaturation", "饱和度倍率");
        });

        DrawToggleGroup("溶解", "_DissolveOn", "_DISSOLVE_ON", () =>
        {
            Prop("_DissolveTex", "溶解噪声图");
            Prop("_DissolveChannel", "取样通道");
            Prop("_DissolveAmount", "溶解程度");
            Prop("_DissolveEdgeWidth", "边缘宽度");
            Prop("_DissolveSoftness", "边缘柔和度");
            Prop("_DissolveEdgeColor", "边缘发光色");
            Prop("_DissolveRotation", "纹理旋转");
            Prop("_DissolveScrollX", "横向流动速度");
            Prop("_DissolveScrollY", "纵向流动速度");
        });

        DrawToggleGroup("呼吸闪烁", "_PulseOn", "_PULSE_ON", () =>
        {
            Prop("_PulseSpeed", "呼吸频率");
            Prop("_PulseMin", "呼吸最小值");
            Prop("_PulseMax", "呼吸最大值");
            Prop("_PulseTarget", "呼吸作用对象");
        });

        EditorGUILayout.Space(4);

        // UGUI 遮罩(模板)：高级，默认折叠，由 Mask 组件自动管理
        _foldStencil = DrawFoldoutGroup("UGUI 遮罩(模板) · 自动管理无需手改", _foldStencil, () =>
        {
            EditorGUILayout.HelpBox("以下参数由 UGUI Mask / RectMask2D 组件在运行时自动写入，通常无需手动修改；保留它们是为了让本特效能被父级 Mask 正确裁剪。", MessageType.Info);
            Prop("_Stencil", "模板ID");
            Prop("_StencilComp", "模板比较方式");
            Prop("_StencilOp", "模板操作");
            Prop("_StencilWriteMask", "模板写入掩码");
            Prop("_StencilReadMask", "模板读取掩码");
            Prop("_ColorMask", "颜色通道掩码");
        });

        EditorGUILayout.Space(6);
        // 渲染队列 / GPU Instancing / 双面全局光照等通用项
        _materialEditor.RenderQueueField();
        _materialEditor.EnableInstancingField();
    }
    #endregion

    #region 绘制辅助
    /// <summary>
    /// 绘制一个特效分组：标题栏左侧是可折叠箭头(控制展开/收起)，右侧是启用勾选框。
    /// 折叠状态与启用状态相互独立——启用后仍可随意折叠收起子参数。
    /// </summary>
    /// <param name="title">分组显示标题</param>
    /// <param name="toggleName">控制启用的 Toggle 属性名</param>
    /// <param name="keyword">该 Toggle 对应的 shader_feature 关键字，用于手动同步</param>
    /// <param name="drawBody">展开且启用时绘制子参数的回调</param>
    private void DrawToggleGroup(string title, string toggleName, string keyword, System.Action drawBody)
    {
        MaterialProperty toggle = FindProperty(toggleName, _props, false);
        if (toggle == null)
        {
            return;
        }

        // 首次遇到该分组时：启用的默认展开，未启用的默认收起
        if (!_foldStates.ContainsKey(toggleName))
        {
            _foldStates[toggleName] = toggle.floatValue > 0.5f;
        }

        EditorGUILayout.BeginVertical(_boxStyle);

        // 标题栏：折叠箭头 + 标题(可点)  |  右侧启用勾选框
        EditorGUILayout.BeginHorizontal();
        _foldStates[toggleName] = EditorGUILayout.Foldout(_foldStates[toggleName], title, true, _headerStyle);
        GUILayout.FlexibleSpace();
        bool enabled = toggle.floatValue > 0.5f;
        EditorGUI.BeginChangeCheck();
        bool newEnabled = EditorGUILayout.ToggleLeft("启用", enabled, GUILayout.Width(56));
        if (EditorGUI.EndChangeCheck())
        {
            ApplyToggle(toggle, keyword, newEnabled);
            // 刚启用时自动展开，方便立即调参
            if (newEnabled)
            {
                _foldStates[toggleName] = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        // 展开区
        if (_foldStates[toggleName])
        {
            EditorGUILayout.Space(2);
            EditorGUI.indentLevel++;
            if (toggle.floatValue > 0.5f)
            {
                drawBody();
            }
            else
            {
                EditorGUILayout.HelpBox("勾选右上角“启用”后可调整该特效参数。", MessageType.None);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 同步设置 Toggle 属性的浮点值与材质上的 shader_feature 关键字(支持多选材质 + 撤销)。
    /// </summary>
    /// <param name="toggle">Toggle 对应的 MaterialProperty</param>
    /// <param name="keyword">要开关的 shader 关键字</param>
    /// <param name="enabled">是否启用</param>
    private void ApplyToggle(MaterialProperty toggle, string keyword, bool enabled)
    {
        _materialEditor.RegisterPropertyChangeUndo(toggle.displayName);
        toggle.floatValue = enabled ? 1f : 0f;
        foreach (Object obj in _materialEditor.targets)
        {
            Material mat = obj as Material;
            if (mat == null)
            {
                continue;
            }
            if (enabled)
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }
        }
    }

    /// <summary>
    /// 绘制一个可折叠分组(无开关)，用于基础设置与高级模板参数。
    /// </summary>
    /// <param name="title">分组标题</param>
    /// <param name="expanded">当前展开状态</param>
    /// <param name="drawBody">展开时绘制内容的回调</param>
    /// <returns>新的展开状态</returns>
    private bool DrawFoldoutGroup(string title, bool expanded, System.Action drawBody)
    {
        EditorGUILayout.BeginVertical(_boxStyle);
        expanded = EditorGUILayout.Foldout(expanded, title, true, _headerStyle);
        if (expanded)
        {
            EditorGUILayout.Space(2);
            drawBody();
        }
        EditorGUILayout.EndVertical();
        return expanded;
    }

    /// <summary>
    /// 按属性名绘制单个 Shader 属性，并用自定义标签覆盖默认显示名。属性不存在时跳过。
    /// </summary>
    /// <param name="name">Shader 属性名</param>
    /// <param name="label">自定义中文标签</param>
    private void Prop(string name, string label)
    {
        MaterialProperty p = FindProperty(name, _props, false);
        if (p != null)
        {
            _materialEditor.ShaderProperty(p, new GUIContent(label));
        }
    }

    /// <summary>
    /// 读取某个浮点属性的当前值，不存在时返回 0。
    /// </summary>
    /// <param name="name">Shader 属性名</param>
    /// <returns>属性的浮点值</returns>
    private float GetFloat(string name)
    {
        MaterialProperty p = FindProperty(name, _props, false);
        return p != null ? p.floatValue : 0f;
    }

    /// <summary>
    /// 延迟初始化分组外框与标题的 GUIStyle(需在 OnGUI 内创建)。
    /// </summary>
    private void InitStyles()
    {
        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 2, 2)
            };
        }
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };
        }
    }
    #endregion
}
