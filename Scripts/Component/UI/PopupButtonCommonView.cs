using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PopupButtonCommonView : BaseUIView, IPointerEnterHandler, IPointerExitHandler
{    
    //延迟展示
    public float timeDelayShow = 0;
    protected float timeDelayShowUpdate = 0;
    protected bool timeDelayShowStart = false;
    protected Button btnTarget;
    //弹窗数据
    protected PopupEnum popupEnum;
    //实际展示中的弹窗类型:展示后 popupEnum 可能被 SetData 改写(如装备卸下后槽位改显部位名 Text 弹窗),隐藏须用展示时记录的类型,避免关错弹窗致原弹窗残留
    protected PopupEnum popupEnumShowing;
    //当前是否有弹窗展示中,用于 ClearData 判定是否需要隐藏
    protected bool isPopupShowing = false;

    protected object targetData;

    //回调进入
    protected Action<PopupButtonCommonView> actionForEnter;
    //回调离开
    protected Action<PopupButtonCommonView> actionForExit;

    public void Start()
    {
        btnTarget = transform.GetComponent<Button>();
        if (btnTarget != null)
            btnTarget.onClick.AddListener(OnClickForTarget);
    }

    public void Update()
    {
        if(timeDelayShowStart && timeDelayShow > 0)
        {
            timeDelayShowUpdate += Time.deltaTime;
            if(timeDelayShowUpdate > timeDelayShow)
            {
                ShowPopupUI();
                timeDelayShowStart = false;
                timeDelayShowUpdate = 0;
            }
        }
    }

    public void SetData(object targetData, PopupEnum popupEnum)
    {
        this.targetData = targetData;
        this.popupEnum = popupEnum;
    }

    public void ShowPopupUI()
    {
        PopupShowView popupShowView = UIHandler.Instance.ShowPopup(new PopupBean(popupEnum));
        //把当前按钮注册为弹窗的触发对象，弹窗会自检该对象是否还激活；
        //一旦按钮意外关闭（OnPointerExit不会被触发），弹窗会通过该回调自动调用ClearData隐藏自身
        popupShowView.SetTrigger(gameObject, ClearData);
        PopupShowCommonView popupShowCommonView = popupShowView as PopupShowCommonView;
        popupShowCommonView.SetData(targetData);
        //记录实际展示的弹窗类型,隐藏时以此为准(此后 popupEnum 若被改写不影响正确关闭)
        popupEnumShowing = popupEnum;
        isPopupShowing = true;
    }

    #region 监听相关
    public void AddListenerForEnter(Action<PopupButtonCommonView> actionForEnter)
    {
        this.actionForEnter += actionForEnter;
    }
     public void ClearListenerForEnter(Action<PopupButtonCommonView> actionForEnter)
    {
        this.actionForEnter -= actionForEnter;
    }

    public void AddListenerForExit(Action<PopupButtonCommonView> actionForExit)
    {
        this.actionForExit += actionForExit;
    }

    public void ClearListenerForExit(Action<PopupButtonCommonView> actionForExit)
    {
        this.actionForExit -= actionForExit;
    }
    #endregion

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetData == null)
        {
            actionForEnter?.Invoke(this);
            return;
        }
        if (timeDelayShow == 0)
        {
            ShowPopupUI();
        }
        else
        {
            timeDelayShowStart = true;
        }
        actionForEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearData();
        actionForExit?.Invoke(this);
    }

    /// <summary>
    /// 点击触发对象:隐藏当前悬浮弹窗(与 PopupButtonView.ButtonClick 行为一致)。修复点击卸下装备等改变槽位数据的操作后,悬浮弹窗残留不关闭的问题。
    /// </summary>
    public void OnClickForTarget()
    {
        ClearData();
    }

    /// <summary>
    /// 清除数据:隐藏展示中的弹窗(以展示时记录的类型为准,避免 popupEnum 被改写后关错弹窗)
    /// </summary>
    public virtual void ClearData()
    {
        timeDelayShowUpdate = 0;
        timeDelayShowStart = false;
        if (!isPopupShowing)
            return;
        UIHandler.Instance.HidePopup(popupEnumShowing);
        isPopupShowing = false;
    }

}