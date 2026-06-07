using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ScrollGridHorizontal : ScrollGridBaseContent
{
    public Scrollbar horizontalScrollbar;

    public void SetCellCount(int count)
    {
        SetCellCount(horizontalScrollbar,count, 0);
    }

    /// <summary>
    /// 编辑器预览复用：用水平Scrollbar与横向(contentType=0)生成cell。
    /// </summary>
    protected override void SetCellCountInternal(int count)
    {
        SetCellCount(horizontalScrollbar, count, 0);
    }

}