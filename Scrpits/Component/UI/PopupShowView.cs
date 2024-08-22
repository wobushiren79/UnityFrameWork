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
    }

    public override void OnEnable()
    {
        base.OnEnable();
        InitPosition();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(0, 0);
        }
    }


    public void InitPosition()
    {
        if (gameObject.activeSelf)
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
            }
            else
            {  
                //右
                offsetTotalX = 1 + offsetPivot.x;
            }

            //屏幕上下修正
            if (Input.mousePosition.y <= (Screen.height / 2))
            {
                offsetTotalY = 0 + offsetPivot.y;
            }
            else
            {
                offsetTotalY = 1 + offsetPivot.y;
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