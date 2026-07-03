using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 热浪扭曲 Shader(FrameWork/Effect/HeatHaze) 的材质面板：
/// 正常绘制全部可见参数后，追加一个仿 URP Lit 的「优先级(Sorting Priority)」滑条，
/// 以相对透明基础队列(3000)的偏移量直接改写 material.renderQueue，控制热浪相对其它透明物的渲染先后。
/// </summary>
public class HeatHazeShaderGUI : ShaderGUI
{
    #region 常量

    /// <summary>优先级偏移的绝对范围(与 URP Lit 一致，-50~50)</summary>
    private const int QueueOffsetRange = 50;

    /// <summary>透明表面的基础渲染队列(RenderQueue.Transparent = 3000)</summary>
    private const int BaseTransparentQueue = (int)RenderQueue.Transparent;

    #endregion

    #region 生命周期

    /// <summary>绘制材质面板：先画常规参数，再画优先级滑条并应用到渲染队列</summary>
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // 常规参数([HideInInspector] 的 _QueueOffset 会被自动跳过)
        base.OnGUI(materialEditor, properties);

        DrawSortingPriority(materialEditor, properties);
    }

    #endregion

    #region 私有方法

    /// <summary>绘制「优先级」滑条，变更时把 3000+偏移 写入所选材质的 renderQueue</summary>
    private void DrawSortingPriority(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty queueOffsetProp = FindProperty("_QueueOffset", properties, false);
        if (queueOffsetProp == null)
            return;

        GUIContent label = new GUIContent(
            "优先级 (Sorting Priority)",
            "相对透明基础队列(3000)的偏移：值越大越晚渲染。最终 renderQueue = 3000 + 该值，范围 -50~50。");

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
                    material.renderQueue = BaseTransparentQueue + newOffset;
            }
        }
    }

    #endregion
}
