using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ScrollGridBaseContent : BaseMonoBehaviour
{
    [Header("Mask状态 0:mask 1:RectMask2D")]
    public int maskState = 0;
    public GameObject tempCell;//模板cell，以此为目标，克隆出每个cell。
    public ScrollRect.MovementType movementType = ScrollRect.MovementType.Elastic;

    public float cellSpaceX = 0;
    public float cellSpaceY = 0;

    protected int cellCount;//要显示数据的总数。
    protected float cellWidth;
    protected float cellHeight;
    protected List<System.Action<ScrollGridCell>> onCellUpdateList = new List<System.Action<ScrollGridCell>>();

    protected ScrollRect scrollRect;

    protected int row;//克隆cell的GameObject数量的行。
    protected int col;//克隆cell的GameObject数量的列。

    protected bool inited;
    protected List<ScrollGridCell> cellList = new List<ScrollGridCell>();

    protected GameObject viewport;
    protected GameObject content;

    protected int contentType;


    /// <summary>
    /// 获取所有子控件
    /// </summary>
    public List<ScrollGridCell> GetAllCell()
    {
        return cellList;
    }

    /// <summary>
    /// 清空所有元素
    /// </summary>
    public void ClearAllCell()
    {
        this.cellCount = 0;
        for (int i = 0; i < cellList.Count; i++)
        {
            var itemCell = cellList[i];
            Destroy(itemCell.gameObject);
        }
        cellList.Clear();
        this.scrollRect.content.offsetMin = new Vector2(0, 0);
        viewport.transform.rotation = new Quaternion();
    }

    public void AddCellListener(System.Action<ScrollGridCell> call)
    {
        this.onCellUpdateList.Add(call);
        this.RefreshAllCells();
    }
    public void RemoveCellListener(System.Action<ScrollGridCell> call)
    {
        this.onCellUpdateList.Remove(call);
    }

    /// <summary>
    /// 设置ScrollGrid要显示的数据数量。
    /// </summary>
    protected virtual void SetCellCount(Scrollbar scrollbar, int count, int contentType)
    {
        this.contentType = contentType;
        this.cellCount = Mathf.Max(0, count);
        if (this.inited == false)
        {
            this.Init(scrollbar);
        }

        if (contentType == 0)
        {
            //重新调整content的高度，保证能够包含范围内的cell的anchoredPosition3D，这样才有机会显示。
            float newContentWidth = this.cellWidth * Mathf.CeilToInt((float)this.cellCount / this.row);
            float newMaxX = newContentWidth - this.scrollRect.viewport.rect.width;//当minX==0时maxX的位置
            float minX = this.scrollRect.content.offsetMin.x;
            newMaxX += minX;//保持位置
            newMaxX = Mathf.Max(minX, newMaxX);//保证不小于viewport的高度。
            this.scrollRect.content.offsetMax = new Vector2(newMaxX, 0);
 
        }
        else
        {
            //重新调整content的高度，保证能够包含范围内的cell的anchoredPosition3D，这样才有机会显示。
            float newContentHeight = this.cellHeight * Mathf.CeilToInt((float)cellCount / this.col);
            float newMinY = -newContentHeight + this.scrollRect.viewport.rect.height;
            float maxY = this.scrollRect.content.offsetMax.y;
            newMinY += maxY;//保持位置
            newMinY = Mathf.Min(maxY, newMinY);//保证不小于viewport的高度。
            this.scrollRect.content.offsetMin = new Vector2(0, newMinY);
        }
        this.CreateCells();
        viewport.transform.rotation = new Quaternion();
    }

    public virtual void Init(Scrollbar scrollbar)
    {
        if (tempCell == null)
        {
            Debug.LogError("tempCell不能为空！");
            return;
        }
        this.inited = true;
        this.tempCell.SetActive(false);

        this.scrollRect = gameObject.AddComponent<ScrollRect>();

        this.scrollRect.verticalScrollbar = scrollbar;
        this.scrollRect.scrollSensitivity = 30;
        this.scrollRect.movementType = movementType;

        viewport = new GameObject("viewport", typeof(RectTransform));
        viewport.transform.SetParent(transform);
        this.scrollRect.viewport = viewport.GetComponent<RectTransform>();
        content = new GameObject("content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform);
        this.scrollRect.content = content.GetComponent<RectTransform>();

        //设置视野viewport的宽高和根节点一致。
        this.scrollRect.viewport.localScale = Vector3.one;
        this.scrollRect.viewport.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
        this.scrollRect.viewport.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
        this.scrollRect.viewport.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 0);
        this.scrollRect.viewport.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, 0);
        this.scrollRect.viewport.anchorMin = Vector2.zero;
        this.scrollRect.viewport.anchorMax = Vector2.one;
        this.scrollRect.viewport.anchoredPosition3D = Vector3.zero;
        this.scrollRect.viewport.eulerAngles = Vector3.zero;

        Image image = this.scrollRect.viewport.gameObject.AddComponent<Image>();
        Rect viewRect = this.scrollRect.viewport.rect;
        image.sprite = Sprite.Create(new Texture2D(1, 1), new Rect(Vector2.zero, Vector2.one), Vector2.zero);

        //设置viewpoint的mask。
        if (maskState == 0)
        {
            this.scrollRect.viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            image.color = new Color(1, 1, 1, 1f);
        }
        else if (maskState == 1)
        {
            this.scrollRect.viewport.gameObject.AddComponent<RectMask2D>();
            image.color = new Color(0, 0, 0, 0f);
        }

        //获取模板cell的宽高。
        Rect tempRect = tempCell.GetComponent<RectTransform>().rect;
        this.cellWidth = tempRect.width * tempCell.transform.localScale.x + cellSpaceX / 2;
        this.cellHeight = tempRect.height * tempCell.transform.localScale.y + cellSpaceY / 2;

        //横
        if (contentType == 0)
        {
            this.scrollRect.vertical = false;
            this.scrollRect.horizontal = true;

            this.row = Mathf.FloorToInt(this.scrollRect.viewport.rect.height / this.cellHeight);
            this.row = Mathf.Max(1, this.row);
            this.col = Mathf.CeilToInt(this.scrollRect.viewport.rect.width / this.cellWidth);
        }
        //竖
        else
        {
            this.scrollRect.vertical = true;
            this.scrollRect.horizontal = false;

            //设置viewpoint约束范围内的cell的GameObject的行列数。
            this.col = (int)(this.scrollRect.viewport.rect.width / this.cellWidth);
            this.col = Mathf.Max(1, this.col);
            this.row = Mathf.CeilToInt(this.scrollRect.viewport.rect.height / this.cellHeight);
        }

        //初始化content。
        this.scrollRect.content.localScale = Vector3.one;
        this.scrollRect.content.offsetMax = new Vector2(0, 0);
        this.scrollRect.content.offsetMin = new Vector2(0, 0);
        this.scrollRect.content.anchorMin = Vector2.zero;
        this.scrollRect.content.anchorMax = Vector2.one;
        this.scrollRect.onValueChanged.AddListener(this.OnValueChange);
        this.CreateCells();

    }

    /// <summary>
    /// 刷新每个cell的数据
    /// </summary>
    public virtual void RefreshAllCells()
    {
        foreach (ScrollGridCell cell in this.cellList)
        {
            this.CellUpdate(cell);
        }
    }

    /// <summary>
    /// 刷新单个数据
    /// </summary>
    public virtual void RefreshCell(int index)
    {
        foreach (ScrollGridCell cell in this.cellList)
        {
            if (cell.index == index)
            {
                this.CellUpdate(cell);
                return;
            }
        }
    }

    protected void CreateCells()
    {
        Action<int,int> actionForItem = (r,l) =>
        {
            int index = r * this.col + l;
            if (index < this.cellCount)
            {
                if (this.cellList.Count <= index)
                {
                    GameObject newcellObj = GameObject.Instantiate<GameObject>(this.tempCell);
                    newcellObj.SetActive(true);
                    RectTransform cellRect = newcellObj.GetComponent<RectTransform>();
                    RectTransform rfTempCell = ((RectTransform)tempCell.transform);
                    cellRect.anchorMin = new Vector2(0, 1);
                    cellRect.anchorMax = new Vector2(0, 1);
                    //cellRect.anchorMin = rfTempCel).anchorMin;
                    //cellRect.anchorMax = rfTempCell.anchorMax;
                    cellRect.sizeDelta = new Vector2(rfTempCell.rect.width, rfTempCell.rect.height);
                    float x = this.cellWidth / 2 + l * this.cellWidth;
                    float y = -r * this.cellHeight - this.cellHeight / 2;
                    cellRect.SetParent(this.scrollRect.content);
                    cellRect.localScale = new Vector3(rfTempCell.localScale.x, rfTempCell.localScale.y, rfTempCell.localScale.z);
                    cellRect.anchoredPosition3D = new Vector3(x, y, 0);
                    var cell = newcellObj.AddComponent<ScrollGridCell>();
                    cell.SetObjIndex(index);
                    this.cellList.Add(cell);
                }
            }
        };

        if (contentType == 0)
        {
            for (int r = 0; r < this.row; r++)
            {
                for (int l = 0; l < this.col + 1; l++)
                {
                    actionForItem?.Invoke(r,l);
                }
            }
        }
        else
        {
            for (int r = 0; r < this.row + 1; r++)
            {
                for (int l = 0; l < this.col; l++)
                {
                    actionForItem?.Invoke(r, l);
                }
            }
        }
        this.RefreshAllCells();
    }

    /// <summary>
    /// 滚动过程中，重复利用cell
    /// </summary>
    protected void OnValueChange(Vector2 pos)
    {
        if (contentType == 0)
        {
            foreach (ScrollGridCell cell in this.cellList)
            {
                RectTransform cellRect = (RectTransform)cell.transform;
                float dist = this.scrollRect.content.offsetMin.x + cellRect.anchoredPosition3D.x;
                float minLeft = -this.cellWidth / 2;
                float maxRight = this.col * this.cellWidth + this.cellWidth / 2;
                //限定复用边界
                if (dist < minLeft)
                {
                    //控制cell的anchoredPosition3D在content的范围内才重复利用。
                    float newX = cellRect.anchoredPosition3D.x + (this.col + 1) * this.cellWidth;
                    if (newX < this.scrollRect.content.rect.width)
                    {
                        cellRect.anchoredPosition3D = new Vector3(newX, cellRect.anchoredPosition3D.y, 0);
                        this.CellUpdate(cell);
                    }
                }
                if (dist > maxRight)
                {
                    float newX = cellRect.anchoredPosition3D.x - (this.col + 1) * this.cellWidth;
                    if (newX > 0)
                    {
                        cellRect.anchoredPosition3D = new Vector3(newX, cellRect.anchoredPosition3D.y, 0);
                        this.CellUpdate(cell);
                    }
                }
            }
        }
        else
        {
            foreach (ScrollGridCell cell in this.cellList)
            {
                RectTransform cellRect = (RectTransform)cell.transform;
                float dist = this.scrollRect.content.offsetMax.y + cellRect.anchoredPosition3D.y;
                float maxTop = this.cellHeight / 2;
                float minBottom = -((this.row + 1) * this.cellHeight) + this.cellHeight / 2;
                if (dist > maxTop)
                {
                    float newY = cellRect.anchoredPosition3D.y - (this.row + 1) * this.cellHeight;
                    //保证cell的anchoredPosition3D只在content的高的范围内活动，下同理
                    if (newY > -this.scrollRect.content.rect.height)
                    {
                        //重复利用cell，重置位置到视野范围内。
                        cellRect.anchoredPosition3D = new Vector3(cellRect.anchoredPosition3D.x, newY, 0);
                        this.CellUpdate(cell);
                    }

                }
                else if (dist < minBottom)
                {
                    float newY = cellRect.anchoredPosition3D.y + (this.row + 1) * this.cellHeight;
                    if (newY < 0)
                    {
                        cellRect.anchoredPosition3D = new Vector3(cellRect.anchoredPosition3D.x, newY, 0);
                        this.CellUpdate(cell);
                    }
                }
            }
        }

    }


    /// <summary>
    /// 所有的数据的真实行数
    /// </summary>
    protected int allRow { get { return Mathf.CeilToInt((float)this.cellCount / this.col); } }
    protected int allCol { get { return Mathf.CeilToInt((float)this.cellCount / this.row); } }


    /// <summary>
    /// cell被刷新时调用，算出cell的位置并调用监听的回调方法（Action）。
    /// </summary>
    protected void CellUpdate(ScrollGridCell cell)
    {
        RectTransform cellRect = cell.GetComponent<RectTransform>();
        int x = Mathf.CeilToInt((cellRect.anchoredPosition3D.x - cellWidth / 2) / cellWidth);
        int y = Mathf.Abs(Mathf.CeilToInt((cellRect.anchoredPosition3D.y + cellHeight / 2) / cellHeight));

        if (contentType == 0)
        {
            int index = y * allCol + x;
            cell.UpdatePos(x, y, index);
            if (index >= cellCount || x >= this.allCol)
            {
                cell.gameObject.SetActive(false);
            }
            else
            {
                if (cell.gameObject.activeSelf == false)
                {
                    cell.gameObject.SetActive(true);
                }
                foreach (var call in this.onCellUpdateList)
                {
                    call(cell);
                }
            }
        }
        else
        {
            int index = y * this.col + x;
            cell.UpdatePos(x, y, index);
            if (index >= cellCount || y >= this.allRow)
            {
                //超出数据范围
                cell.gameObject.SetActive(false);
            }
            else
            {
                if (cell.gameObject.activeSelf == false)
                {
                    cell.gameObject.SetActive(true);
                }
                foreach (var call in this.onCellUpdateList)
                {
                    call(cell);
                }
            }
        }
    }
}