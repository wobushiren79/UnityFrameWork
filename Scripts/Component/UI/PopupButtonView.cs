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
    protected PopupEnum popupType;

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
        string tName = GetType().Name;
        tName = tName.Replace("UI", "").Replace("Popup","").Replace("Button","");
        popupType = tName.GetEnum<PopupEnum>();
        popupShow = UIHandler.Instance.ShowPopup<T>(new PopupBean(popupType));
        //把当前按钮注册为弹窗的触发对象，弹窗会自检该对象是否还激活；
        //一旦按钮意外关闭（OnPointerExit不会被触发），弹窗会通过该回调自动调用CleanData隐藏自身
        popupShow.SetTrigger(gameObject, CleanData);
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
        //重置引用，避免重复调用HidePopup以及在按钮被重新启用时执行旧的隐藏逻辑
        popupShow = null;
    }

    public abstract void PopupShow();
    public abstract void PopupHide();
}