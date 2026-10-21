using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollGridCell : MonoBehaviour
{
    private int _x;
    private int _y;
    private int _index;
    private int _objIndex;

    #region 布局位置

    /// <summary>
    /// 不含末行对齐偏移的规范布局位置（锚点坐标）。
    /// 滚动复用的搬移与行列反算一律以此为准：视觉上的 anchoredPosition3D 可能带末行对齐偏移，
    /// 若直接拿它反算列号，ceil 会把偏移舍入成多一列，导致索引错位。
    /// </summary>
    public Vector3 layoutPosition;

    #endregion

    public int X { get { return _x; } }
    public int Y { get { return _y; } }
    /// <summary>
    /// ScrollView滑动时，根据所显示数据而刷新变化的数据索引index，不断变化。
    /// </summary>
    public int index { get { return _index; } }
    /// <summary>
    /// 每个克隆出来的cell的GameObject所标记的唯一且固定的objIndex，确定后不再变化。
    /// </summary>
    public int objIndex { get { return _objIndex; } }

    /// <summary>
    /// 更新cell所滑动到的新的位置。
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="index"></param>
    public void UpdatePos(int x, int y, int index)
    {
        this._x = x;
        this._y = y;
        this._index = index;
    }
    public void SetObjIndex(int objIndex)
    {
        this._objIndex = objIndex;
    }
}