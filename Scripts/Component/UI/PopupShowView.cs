using UnityEngine;
using UnityEditor;
using System;
using DG.Tweening;
using UnityEngine.UI;

public class PopupShowView : BaseUIView
{
    //鼠标位置和弹窗偏移量
    public float offsetX = 0;
    public float offsetY = 0;
    public Vector2 offsetPivot = Vector2.zero;
    //是否实时更新位置
    public bool isUpdatePosition = true;

    protected Direction2DEnum mouseAreaLeftRight =  Direction2DEnum.Left;
    protected Direction2DEnum mouseAreaUpDown = Direction2DEnum.Down;

    //触发该弹窗的对象（通常为PopupButton所在的GameObject），用于检测触发对象意外失活时自动隐藏
    protected GameObject triggerObj;
    //触发对象失效时的回调（一般为触发按钮的CleanData）
    protected Action onTriggerInvalid;

    public override void Awake()
    {
        base.Awake();
    }

    public virtual void Update()
    {
        if (rectTransform == null)
            return;
        //如果显示Popup 则调整位置为鼠标位置
        InitPosition();
        //检测触发对象是否还有效，若已失活则自动隐藏弹窗
        CheckTriggerValid();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        InitPosition();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        //清空触发器引用，避免下次启用时执行旧回调导致误关闭
        triggerObj = null;
        onTriggerInvalid = null;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(0, 0);
        }
    }

    /// <summary>
    /// 设置触发该弹窗的对象与失效回调
    /// 弹窗会在Update中检测该对象是否还激活；若意外失活（被禁用或销毁），则触发回调以隐藏自身，
    /// 防止PopupButton意外关闭后弹窗一直存在的BUG
    /// </summary>
    /// <param name="obj">触发该弹窗的GameObject，通常为PopupButton所在的GameObject</param>
    /// <param name="onInvalid">触发对象失效时执行的回调（一般为触发按钮的CleanData）</param>
    public virtual void SetTrigger(GameObject obj, Action onInvalid)
    {
        triggerObj = obj;
        onTriggerInvalid = onInvalid;
    }

    /// <summary>
    /// 检测触发对象是否还有效，若已被禁用/销毁则触发回调以隐藏弹窗
    /// </summary>
    protected virtual void CheckTriggerValid()
    {
        //未注册回调时跳过检测，避免误触发
        if (onTriggerInvalid == null)
            return;
        //触发对象被销毁或被禁用时，调用一次回调并清理引用
        if (triggerObj == null || !triggerObj.activeInHierarchy)
        {
            Action callback = onTriggerInvalid;
            triggerObj = null;
            onTriggerInvalid = null;
            callback.Invoke();
        }
    }


    public virtual void InitPosition()
    {
        if (isUpdatePosition && gameObject.activeSelf)
        {
            Transform tfContainer = UIHandler.Instance.manager.GetUITypeContainer(UITypeEnum.Popup);
            //屏幕坐标转换为UI坐标
            Vector2 outPosition = GameUtil.MousePointToUGUIPoint(null,(RectTransform)tfContainer);
            float moveX = outPosition.x;
            float moveY = outPosition.y;

            transform.localPosition = new Vector3(moveX + offsetX, moveY + offsetY, transform.localPosition.z);

            float offsetTotalX;
            float offsetTotalY;
            //判断鼠标在屏幕的左右
            if (Input.mousePosition.x <= (Screen.width / 2))
            {    
                //左
                offsetTotalX = 0 - offsetPivot.x;
                mouseAreaLeftRight = Direction2DEnum.Left;
            }
            else
            {  
                //右
                offsetTotalX = 1 + offsetPivot.x;
                mouseAreaLeftRight = Direction2DEnum.Right;
            }

            //屏幕上下修正
            if (Input.mousePosition.y <= (Screen.height / 2))
            {
                //下
                offsetTotalY = 0 + offsetPivot.y;
                mouseAreaUpDown = Direction2DEnum.Down;
            }
            else
            {
                //上
                offsetTotalY = 1 + offsetPivot.y;
                mouseAreaUpDown = Direction2DEnum.Up;
            }
            rectTransform.pivot = new Vector2(offsetTotalX, offsetTotalY);
        }
    }

    /// <summary>
    /// 刷新控件大小
    /// </summary>
    public void RefreshViewSize()
    {
        UGUIUtil.RefreshUISize(rectTransform);
    }
}