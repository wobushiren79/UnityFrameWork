using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class CartogramBarView : CartogramBaseView
{
    public GameObject objItemContainer;
    public GameObject objItemModel;

    public override void InitCartogram()
    {
        base.InitCartogram();
        CreateBar();
    }

    /// <summary>
    /// 创建柱状图
    /// </summary>
    public void CreateBar()
    {
        CptUtil.RemoveChildsByActive(objItemContainer);
        if (listCartogramData == null)
            return;
        if (listCartogramData.Count == 0)
        {

            return;
        }
        RectTransform rtContent = (RectTransform)objItemContainer.transform;
        //设置单个宽度
        float itemWidth = (rtContent.rect.width - 40) / listCartogramData.Count;
        //限制大小
        if (itemWidth <= 0)
            itemWidth = 1;
        if (itemWidth > 50)
            itemWidth = 50;
        //设置单个高度
        //获取列表中最高的值
        float itemMaxValue = listCartogramData.Max(data => data.value_1);
        if (itemMaxValue == 0)
            itemMaxValue = 1;
        float itemMaxHeight = (rtContent.rect.height) - 40;
        for (int i = 0; i < listCartogramData.Count; i++)
        {
            CreateItemBar(i, itemWidth, itemMaxValue, itemMaxHeight);
        }
    }

    /// <summary>
    /// 创建单个柱状
    /// </summary>
    /// <param name="position"></param>
    /// <param name="itemWidth"></param>
    /// <param name="itemMaxValue"></param>
    /// <param name="itemMaxHeight"></param>
    /// <returns></returns>
    public virtual CartogramBarForItem CreateItemBar(int position, float itemWidth, float itemMaxValue, float itemMaxHeight)
    {
        CartogramDataBean itemData = listCartogramData[position];
        GameObject objItem = Instantiate(objItemContainer, objItemModel);
        //设置大小
        //计算单个高度
        float itemHeight = (itemData.value_1 / itemMaxValue) * itemMaxHeight;
        ((RectTransform)objItem.transform).sizeDelta = new Vector2(itemWidth, itemHeight);

        CartogramBarForItem itemCpt = objItem.GetComponent<CartogramBarForItem>();
        itemCpt.SetData(itemData, itemWidth, itemHeight);
        itemCpt.AnimForInit(position);

        return itemCpt;
    }

}