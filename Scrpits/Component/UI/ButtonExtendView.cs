using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonExtendView : Button
{
    //长按
    public Action actionLongClick;
    //是否长按
    protected bool isLongClick = false;

    protected void Update()
    {
        if (isLongClick)
        {
            actionLongClick?.Invoke();
        }
    }

    /// <summary>
    /// 增加长按监听
    /// </summary>
    /// <param name="actionLongClick"></param>
    public void AddLongClickListener(Action actionLongClick)
    {
        this.actionLongClick += actionLongClick;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        isLongClick = true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        isLongClick = false;
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);

    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
    }

}