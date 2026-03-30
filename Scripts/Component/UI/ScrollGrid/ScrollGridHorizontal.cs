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

}