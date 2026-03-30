using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DG.Tweening;

public class CartogramBarForItem : BaseMonoBehaviour
{
    public CartogramDataBean cartogramData;

    public Text tvHData;
    public Text tvVData;
    public Image ivBar;

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="cartogramData"></param>
    /// <param name="itemWidth"></param>
    /// <param name="itemHeight"></param>
    public void SetData(CartogramDataBean cartogramData, float itemWidth, float itemHeight)
    {
        this.cartogramData = cartogramData;

        SetBar(itemWidth, itemHeight);
        SetHData(cartogramData.key + "");
        SetVData(cartogramData.value_1 + "");
    }

    /// <summary>
    /// 设置横坐标数据
    /// </summary>
    /// <param name="data"></param>
    public virtual void SetHData(string data)
    {
        if (tvHData != null)
        {
            tvHData.text = data;
        }
    }

    /// <summary>
    /// 设置纵坐标数据
    /// </summary>
    /// <param name="data"></param>
    public virtual void SetVData(string data)
    {
        if (tvVData != null)
        {
            tvVData.text = data;
        }
    }

    /// <summary>
    /// 设置柱状条
    /// </summary>
    /// <param name="itemWidth"></param>
    /// <param name="itemHeight"></param>
    public virtual void SetBar(float itemWidth, float itemHeight)
    {
        if (ivBar != null)
            ((RectTransform)ivBar.transform).sizeDelta = new Vector2(itemWidth, itemHeight);
    }

    /// <summary>
    /// 初始化动画
    /// </summary>
    /// <param name="position"></param>
    /// <param name="itemHeight"></param>
    public void AnimForInit(int position)
    {
        ivBar.transform.DOScaleY(0, 0.5f).From().SetEase(Ease.OutBack).SetDelay(0.01f * position);
    }
}