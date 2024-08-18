using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaskUIView : BaseMonoBehaviour
{

    [SerializeField]
    public Color defaultColor = new Color(0.78f, 0.78f, 0.78f);

    [Header("Ŀ��UI")]
    public Graphic[] targetGraphics;
    [Header("ԭʼColor")]
    public Color[] targetColors;

    /// <summary>
    ///  չʾ����
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
            //������ı� ����ԭ������ɫ�ϵ��Ӷ������滻
            targetGraphic.color = targetColor * defaultColor;
        }
    }

    /// <summary>
    /// ��������
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
    /// �޸�ָ���ؼ���Ĭ����ɫ
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
    /// �ռ����е�UI
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

