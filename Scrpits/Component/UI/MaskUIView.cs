using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaskUIView : BaseMonoBehaviour
{

    [SerializeField]
    public Color defaultColor = new Color(0.78f, 0.78f, 0.78f);

    [Header("目标UI")]
    public Graphic[] targetGraphics;
    [Header("原始Color")]
    public Color[] targetColors;

    /// <summary>
    ///  展示遮罩
    /// </summary>
    public void ShowMask()
    {
        if (targetGraphics != null && targetColors != null)
        {
            HideMask();
        }
        for (int i = 0; i < targetGraphics.Length; i++)
        {
            var targetGraphic = targetGraphics[i];
            var targetColor = targetColors[i];
            if (targetGraphic == null || targetColor == null)
                continue;
            //如果是文本 则再原来的颜色上叠加而不是替换
            targetGraphic.color = targetColor * defaultColor;
        }
    }

    /// <summary>
    /// 隐藏遮罩
    /// </summary>
    public void HideMask()
    {
        if (targetColors == null)
            return;
        for (int i = 0; i < targetColors.Length; i++)
        {
            var targetGraphic = targetGraphics[i];
            var targetColor = targetColors[i];
            if (targetGraphic == null || targetColor == null)
                continue;
            targetGraphic.color = targetColor;
        }
    }

    /// <summary>
    /// 修改指定控件的默认颜色
    /// </summary>
    public void ChangeDefColor(Graphic graphic,Color changeColor)
    {
        if (targetGraphics == null)
            return;
        for (int i = 0; i < targetGraphics.Length; i++)
        {
            var targetGraphic = targetGraphics[i];
            if (graphic == targetGraphic)
            {
                targetColors[i] = changeColor;
            }
        }
    }

    /// <summary>
    /// 收集所有的UI
    /// </summary>
    public void CollectAllGraphic()
    {
        targetGraphics = GetComponentsInChildren<Graphic>(true);
        targetColors = new Color[targetGraphics.Length];
        for (int i = 0; i < targetGraphics.Length; i++)
        {
            if (targetGraphics[i] == null)
                continue;
            targetColors[i] = targetGraphics[i].color;
        }
    }
}

