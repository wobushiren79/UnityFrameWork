using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public abstract class PopupButtonView<T> : BaseUIView,
    IPointerEnterHandler,
    IPointerExitHandler
    where T : PopupShowView
{

    //目标按钮
    public Button btnTarget;
    //弹窗数据
    public PopupEnum popupType;

    protected T popupShow;

    public void Start()
    {
        if (btnTarget != null)
            btnTarget.onClick.AddListener(ButtonClick);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        CleanData();
    }

    public void ButtonClick()
    {
        CleanData();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        popupShow = UIHandler.Instance.ShowPopup<T>(new PopupBean(popupType));
        popupShow.RefreshViewSize();
        PopupShow();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        CleanData();
    }
    
    /// <summary>
    /// 清除数据
    /// </summary>
    public virtual void CleanData()
    {
        if (popupShow == null)
            return;
        UIHandler.Instance.HidePopup(popupType);
        PopupHide();
    }

    public abstract void PopupShow();
    public abstract void PopupHide();
}