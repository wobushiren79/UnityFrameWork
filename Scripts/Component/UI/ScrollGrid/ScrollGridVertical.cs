using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ScrollGridVertical : ScrollGridBaseContent
{
    public Scrollbar verticalScrollbar;


    public void SetCellCount(int count)
    {
        SetCellCount(verticalScrollbar, count, 1);
    }

    /// <summary>
    /// 编辑器预览复用：用垂直Scrollbar与纵向(contentType=1)生成cell。
    /// </summary>
    protected override void SetCellCountInternal(int count)
    {
        SetCellCount(verticalScrollbar, count, 1);
    }

}