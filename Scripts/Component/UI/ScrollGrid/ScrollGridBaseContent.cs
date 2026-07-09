using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScrollGrid交叉轴对齐方式。
/// 纵向滚动：控制列在水平方向的对齐(Start=左/Center=中/End=右)；
/// 横向滚动：控制行在垂直方向的对齐(Start=上/Center=中/End=下)。
/// </summary>
public enum ScrollGridCrossAlign
{
    /// <summary>
    /// 起始对齐（纵向=左，横向=上）。
    /// </summary>
    Start = 0,

    /// <summary>
    /// 居中对齐。
    /// </summary>
    Center = 1,

    /// <summary>
    /// 末端对齐（纵向=右，横向=下）。
    /// </summary>
    End = 2,
}

public class ScrollGridBaseContent : BaseMonoBehaviour
{
    #region 序列化字段（Inspector可配置）

    /// <summary>
    /// Mask遮罩方式。0:Mask；1:RectMask2D。
    /// </summary>
    [Header("Mask状态 0:Mask 1:RectMask2D")]
    public int maskState = 1;

    /// <summary>
    /// 模板cell：以此为目标，克隆出每个cell。
    /// </summary>
    public GameObject tempCell;

    /// <summary>
    /// ScrollRect的边界移动类型（弹性/受限/无限）。
    /// </summary>
    public ScrollRect.MovementType movementType = ScrollRect.MovementType.Elastic;

    /// <summary>
    /// cell之间的水平间距。
    /// </summary>
    public float cellSpaceX = 0;

    /// <summary>
    /// cell之间的垂直间距。
    /// </summary>
    public float cellSpaceY = 0;

    /// <summary>
    /// 左内边距：cell网格与viewport左边的距离。
    /// </summary>
    [Header("内边距 Padding")]
    public float paddingLeft = 0;

    /// <summary>
    /// 右内边距：cell网格与viewport右边的距离（横向滚动时计入滚动总长）。
    /// </summary>
    public float paddingRight = 0;

    /// <summary>
    /// 上内边距：cell网格与viewport上边的距离。
    /// </summary>
    public float paddingTop = 0;

    /// <summary>
    /// 下内边距：cell网格与viewport下边的距离（纵向滚动时计入滚动总长）。
    /// </summary>
    public float paddingBottom = 0;

    /// <summary>
    /// 交叉轴对齐方式：纵向滚动时控制列的水平对齐；横向滚动时控制行的垂直对齐。
    /// </summary>
    [Header("交叉轴对齐 CrossAlign")]
    public ScrollGridCrossAlign crossAlign = ScrollGridCrossAlign.Start;

    #endregion

    #region 运行时字段（内部状态）

    /// <summary>
    /// 要显示数据的总数。
    /// </summary>
    protected int cellCount;

    /// <summary>
    /// 单个cell的宽度（含水平间距）。
    /// </summary>
    protected float cellWidth;

    /// <summary>
    /// 单个cell的高度（含垂直间距）。
    /// </summary>
    protected float cellHeight;

    /// <summary>
    /// cell数据刷新回调列表：cell复用/刷新时逐个回调，用于填充cell内容。
    /// </summary>
    protected List<System.Action<ScrollGridCell>> onCellUpdateList = new List<System.Action<ScrollGridCell>>();

    /// <summary>
    /// 内部使用的ScrollRect组件（Init时动态添加到自身）。
    /// </summary>
    protected ScrollRect scrollRect;

    /// <summary>
    /// 实际克隆的cell的GameObject行数（视野约束内）。
    /// </summary>
    protected int row;

    /// <summary>
    /// 实际克隆的cell的GameObject列数（视野约束内）。
    /// </summary>
    protected int col;

    /// <summary>
    /// 是否已初始化（Init是否已执行）。
    /// </summary>
    protected bool inited;

    /// <summary>
    /// 已克隆出的cell列表（复用对象池）。
    /// </summary>
    protected List<ScrollGridCell> cellList = new List<ScrollGridCell>();

    /// <summary>
    /// 视野节点（承载Mask/RectMask2D，Init时动态创建）。
    /// </summary>
    protected GameObject viewport;

    /// <summary>
    /// 内容节点（承载所有cell，Init时动态创建）。
    /// </summary>
    protected GameObject content;

    /// <summary>
    /// 滚动方向。0:横向；1:纵向。
    /// </summary>
    protected int contentType;

    /// <summary>
    /// cell网格起点X偏移：含左内边距；纵向滚动时还含交叉轴(列)对齐量。
    /// </summary>
    protected float cellOffsetX;

    /// <summary>
    /// cell网格起点Y偏移（向下为负）：含上内边距；横向滚动时还含交叉轴(行)对齐量。
    /// </summary>
    protected float cellOffsetY;

    #endregion

    #region 公有方法（外部调用）

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
            DestroyObj(itemCell.gameObject);
        }
        cellList.Clear();
        //未初始化(Init未执行)时scrollRect/viewport为空，直接返回避免空引用
        if (this.inited == false || this.scrollRect == null)
        {
            return;
        }
        this.scrollRect.content.offsetMin = new Vector2(0, 0);
        viewport.transform.rotation = new Quaternion();
    }

    /// <summary>
    /// 添加cell数据刷新回调，并立即刷新一次所有cell。
    /// </summary>
    public void AddCellListener(System.Action<ScrollGridCell> call)
    {
        this.onCellUpdateList.Add(call);
        this.RefreshAllCells();
    }

    /// <summary>
    /// 移除cell数据刷新回调。
    /// </summary>
    public void RemoveCellListener(System.Action<ScrollGridCell> call)
    {
        this.onCellUpdateList.Remove(call);
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

    #endregion

    #region 核心逻辑（初始化 / 生成 / 复用）

    /// <summary>
    /// 销毁对象：运行模式用Destroy，编辑模式（预览）用DestroyImmediate，避免编辑器下Destroy无效。
    /// </summary>
    protected void DestroyObj(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(obj);
            return;
        }
#endif
        Destroy(obj);
    }

    /// <summary>
    /// 由子类实现：用各自的scrollbar与方向(contentType)调用SetCellCount，供编辑器预览复用。
    /// </summary>
    protected virtual void SetCellCountInternal(int count)
    {
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
            //重新调整content的宽度，保证能够包含范围内的cell的anchoredPosition3D，这样才有机会显示。（含左右内边距）
            float newContentWidth = this.cellWidth * Mathf.CeilToInt((float)this.cellCount / this.row) + paddingLeft + paddingRight;
            float newMaxX = newContentWidth - this.scrollRect.viewport.rect.width;//当minX==0时maxX的位置
            float minX = this.scrollRect.content.offsetMin.x;
            newMaxX += minX;//保持位置
            newMaxX = Mathf.Max(minX, newMaxX);//保证不小于viewport的高度。
            this.scrollRect.content.offsetMax = new Vector2(newMaxX, 0);

        }
        else
        {
            //重新调整content的高度，保证能够包含范围内的cell的anchoredPosition3D，这样才有机会显示。（含上下内边距）
            float newContentHeight = this.cellHeight * Mathf.CeilToInt((float)cellCount / this.col) + paddingTop + paddingBottom;
            float newMinY = -newContentHeight + this.scrollRect.viewport.rect.height;
            float maxY = this.scrollRect.content.offsetMax.y;
            newMinY += maxY;//保持位置
            newMinY = Mathf.Min(maxY, newMinY);//保证不小于viewport的高度。
            this.scrollRect.content.offsetMin = new Vector2(0, newMinY);
        }
        this.CreateCells();
        viewport.transform.rotation = new Quaternion();
    }

    /// <summary>
    /// 初始化：动态创建ScrollRect/viewport/content，计算cell宽高与行列数，并生成首批cell。
    /// </summary>
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

            //交叉轴(行)数量受上下内边距约束。
            this.row = Mathf.FloorToInt((this.scrollRect.viewport.rect.height - paddingTop - paddingBottom) / this.cellHeight);
            this.row = Mathf.Max(1, this.row);
            this.col = Mathf.CeilToInt(this.scrollRect.viewport.rect.width / this.cellWidth);
        }
        //竖
        else
        {
            this.scrollRect.vertical = true;
            this.scrollRect.horizontal = false;

            //设置viewpoint约束范围内的cell的GameObject的行列数。交叉轴(列)数量受左右内边距约束。
            this.col = (int)((this.scrollRect.viewport.rect.width - paddingLeft - paddingRight) / this.cellWidth);
            this.col = Mathf.Max(1, this.col);
            this.row = Mathf.CeilToInt(this.scrollRect.viewport.rect.height / this.cellHeight);
        }

        //计算cell网格起点偏移（内边距 + 交叉轴对齐）。
        this.CalcCellGridOffset();

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
    /// 计算cell网格起点偏移：左/上内边距 + 交叉轴对齐的剩余空间分配。需在row/col确定后调用。
    /// </summary>
    protected void CalcCellGridOffset()
    {
        float viewW = this.scrollRect.viewport.rect.width;
        float viewH = this.scrollRect.viewport.rect.height;
        float crossExtraX = 0f;
        float crossExtraY = 0f;
        if (contentType == 0)
        {
            //横向滚动：交叉轴=Y(行)，按行做垂直对齐。
            float availH = viewH - paddingTop - paddingBottom;
            float gridH = this.row * this.cellHeight;
            crossExtraY = GetCrossAlignExtra(Mathf.Max(0f, availH - gridH));
        }
        else
        {
            //纵向滚动：交叉轴=X(列)，按列做水平对齐。
            float availW = viewW - paddingLeft - paddingRight;
            float gridW = this.col * this.cellWidth;
            crossExtraX = GetCrossAlignExtra(Mathf.Max(0f, availW - gridW));
        }
        this.cellOffsetX = paddingLeft + crossExtraX;
        this.cellOffsetY = -(paddingTop + crossExtraY);
    }

    /// <summary>
    /// 根据交叉轴对齐方式，计算剩余空间的起点附加偏移量。
    /// </summary>
    protected float GetCrossAlignExtra(float leftover)
    {
        switch (crossAlign)
        {
            case ScrollGridCrossAlign.Center:
                return leftover / 2f;
            case ScrollGridCrossAlign.End:
                return leftover;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// 创建/补齐视野约束范围内的cell（按行列克隆tempCell并定位）。
    /// </summary>
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
                    float x = this.cellOffsetX + this.cellWidth / 2 + l * this.cellWidth;
                    float y = this.cellOffsetY - r * this.cellHeight - this.cellHeight / 2;
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
    /// 滚动过程中重复利用cell：把滚出视野的cell整页搬到另一端复用。
    /// 用while按整页反复搬移(而非只搬一页)，以兼容快速拖拽下拉条一次跳过多页的情形——
    /// 否则cell只搬一页仍在视野外，导致列表空白、道具消失。
    /// </summary>
    protected void OnValueChange(Vector2 pos)
    {
        if (contentType == 0)
        {
            float pageWidth = (this.col + 1) * this.cellWidth;
            foreach (ScrollGridCell cell in this.cellList)
            {
                RectTransform cellRect = (RectTransform)cell.transform;
                float minLeft = -this.cellWidth / 2;
                float maxRight = this.col * this.cellWidth + this.cellWidth / 2;
                float dist = this.scrollRect.content.offsetMin.x + cellRect.anchoredPosition3D.x;
                bool moved = false;
                //滚出左侧：整页右移，直到回到复用窗口内(content右边界兜底防越界)
                while (dist < minLeft)
                {
                    float newX = cellRect.anchoredPosition3D.x + pageWidth;
                    if (newX >= this.scrollRect.content.rect.width)
                    {
                        break;
                    }
                    cellRect.anchoredPosition3D = new Vector3(newX, cellRect.anchoredPosition3D.y, 0);
                    dist = this.scrollRect.content.offsetMin.x + newX;
                    moved = true;
                }
                //滚出右侧：整页左移，直到回到复用窗口内(content左边界兜底防越界)
                while (dist > maxRight)
                {
                    float newX = cellRect.anchoredPosition3D.x - pageWidth;
                    if (newX <= 0)
                    {
                        break;
                    }
                    cellRect.anchoredPosition3D = new Vector3(newX, cellRect.anchoredPosition3D.y, 0);
                    dist = this.scrollRect.content.offsetMin.x + newX;
                    moved = true;
                }
                if (moved)
                {
                    this.CellUpdate(cell);
                }
            }
        }
        else
        {
            float pageHeight = (this.row + 1) * this.cellHeight;
            foreach (ScrollGridCell cell in this.cellList)
            {
                RectTransform cellRect = (RectTransform)cell.transform;
                float maxTop = this.cellHeight / 2;
                float minBottom = -((this.row + 1) * this.cellHeight) + this.cellHeight / 2;
                float dist = this.scrollRect.content.offsetMax.y + cellRect.anchoredPosition3D.y;
                bool moved = false;
                //滚出上方：整页下移，直到回到复用窗口内(content下边界兜底防越界)
                while (dist > maxTop)
                {
                    float newY = cellRect.anchoredPosition3D.y - pageHeight;
                    if (newY <= -this.scrollRect.content.rect.height)
                    {
                        break;
                    }
                    cellRect.anchoredPosition3D = new Vector3(cellRect.anchoredPosition3D.x, newY, 0);
                    dist = this.scrollRect.content.offsetMax.y + newY;
                    moved = true;
                }
                //滚出下方：整页上移，直到回到复用窗口内(content上边界兜底防越界)
                while (dist < minBottom)
                {
                    float newY = cellRect.anchoredPosition3D.y + pageHeight;
                    if (newY >= 0)
                    {
                        break;
                    }
                    cellRect.anchoredPosition3D = new Vector3(cellRect.anchoredPosition3D.x, newY, 0);
                    dist = this.scrollRect.content.offsetMax.y + newY;
                    moved = true;
                }
                if (moved)
                {
                    this.CellUpdate(cell);
                }
            }
        }

    }


    /// <summary>
    /// 所有数据按当前列数换算的真实行数。
    /// </summary>
    protected int allRow { get { return Mathf.CeilToInt((float)this.cellCount / this.col); } }

    /// <summary>
    /// 所有数据按当前行数换算的真实列数。
    /// </summary>
    protected int allCol { get { return Mathf.CeilToInt((float)this.cellCount / this.row); } }


    /// <summary>
    /// cell被刷新时调用，算出cell的位置并调用监听的回调方法（Action）。
    /// </summary>
    protected void CellUpdate(ScrollGridCell cell)
    {
        RectTransform cellRect = cell.GetComponent<RectTransform>();
        int x = Mathf.CeilToInt((cellRect.anchoredPosition3D.x - this.cellOffsetX - cellWidth / 2) / cellWidth);
        int y = Mathf.Abs(Mathf.CeilToInt((cellRect.anchoredPosition3D.y - this.cellOffsetY + cellHeight / 2) / cellHeight));

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

    #endregion

#if UNITY_EDITOR

    #region 编辑器预览

    [Header("编辑器预览（仅用于Editor，运行时无效）")]
    [Tooltip("编辑器预览时模拟的数据数量")]
    public int editorPreviewCount = 20;

    /// <summary>
    /// 记录预览前tempCell的激活状态，清除预览时恢复。
    /// </summary>
    private bool editorPreviewTempCellActive = true;

    /// <summary>
    /// 【仅编辑器】重建预览：在编辑模式下按 editorPreviewCount 实际克隆tempCell生成cell布局。
    /// 注意：cell内容需运行时数据回调填充，预览只展示布局与位置（cell外观取决于tempCell）。
    /// </summary>
    public void EditorRebuildPreview()
    {
        EditorClearPreview();
        if (tempCell == null)
        {
            Debug.LogError("tempCell不能为空！");
            return;
        }
        editorPreviewTempCellActive = tempCell.activeSelf;
        SetCellCountInternal(Mathf.Max(0, editorPreviewCount));
        MarkPreviewObjectsDontSave();
    }

    /// <summary>
    /// 【仅编辑器】清除预览：移除预览生成的viewport/content/cell与ScrollRect并复位状态。
    /// </summary>
    public void EditorClearPreview()
    {
        ClearAllCell();
        if (viewport != null)
        {
            DestroyImmediate(viewport);
        }
        ScrollRect existScrollRect = GetComponent<ScrollRect>();
        if (existScrollRect != null)
        {
            DestroyImmediate(existScrollRect);
        }
        this.viewport = null;
        this.content = null;
        this.scrollRect = null;
        this.inited = false;
        if (tempCell != null && editorPreviewTempCellActive)
        {
            tempCell.SetActive(true);
        }
    }

    /// <summary>
    /// 【仅编辑器】把预览生成的对象与组件标记为不保存(DontSave)，避免误存入预制体/场景。
    /// </summary>
    private void MarkPreviewObjectsDontSave()
    {
        if (viewport != null)
        {
            SetHideFlagsRecursively(viewport.transform, HideFlags.DontSave);
        }
        if (scrollRect != null)
        {
            scrollRect.hideFlags = HideFlags.DontSave;
        }
    }

    /// <summary>
    /// 【仅编辑器】递归设置transform及其所有子节点GameObject的HideFlags。
    /// </summary>
    private void SetHideFlagsRecursively(Transform root, HideFlags flags)
    {
        if (root == null)
        {
            return;
        }
        root.gameObject.hideFlags = flags;
        for (int i = 0; i < root.childCount; i++)
        {
            SetHideFlagsRecursively(root.GetChild(i), flags);
        }
    }

    /// <summary>
    /// 【仅编辑器】当前是否处于预览状态（已生成viewport）。
    /// </summary>
    public bool EditorIsPreviewing
    {
        get { return viewport != null; }
    }

    /// <summary>
    /// 【仅编辑器】获取模板cell(tempCell)的RectTransform，供Inspector读取/调整子控件尺寸。
    /// </summary>
    public RectTransform EditorGetCellRectTransform()
    {
        if (tempCell == null)
        {
            return null;
        }
        return tempCell.transform as RectTransform;
    }

    #endregion

#endif
}