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

}