using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class LongPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isPointerDown = false;
    private float pointerDownTimer = 0f;
    private float eventTimer = 0f;

    [Header("长按所需时间")]
    public float longHoldTime = 1f;

    [Header("长按触发事件间隔时间")]
    public float longHoldEventTime = 0.1f;

    public Action actionForLongHoldEvent;

    private void Update()
    {
        if (isPointerDown)
        {
            pointerDownTimer += Time.deltaTime;
            if (pointerDownTimer >= longHoldTime)
            {
                eventTimer += Time.deltaTime;
                if (eventTimer >= longHoldEventTime)
                {
                    eventTimer = 0;
                    actionForLongHoldEvent?.Invoke();
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Reset();
        isPointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Reset();
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    private void Reset()
    {
        isPointerDown = false;
        pointerDownTimer = 0f;
        //第一次进的时候要先触发一次
        eventTimer = longHoldEventTime;
    }

    /// <summary>
    /// 添加事件
    /// </summary>
    public void AddLongEventAction(Action action)
    {
        actionForLongHoldEvent += action;
    }
}
