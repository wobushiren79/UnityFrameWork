﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;
using DG.Tweening;

public class BaseUIInit : BaseMonoBehaviour
{
    protected BaseEvent baseEvent = new BaseEvent();

    public UIOpenAnimEnum uiOpenAnimType = UIOpenAnimEnum.None;
    public Transform uiOpenAnimTarget;

    public virtual void Awake()
    {
        AutoLinkUI();
        RegisterButtons();
    }

    public virtual void OnDestroy()
    {        
        ClearOpenUIAnim();
        UnRegisterInputAction();
        UnRegisterAllEvent();
    }

    public virtual void OnDisable()
    {
        ClearOpenUIAnim();
    }
    public virtual void OnEnable()
    {

    }

    public virtual void OpenUI()
    {
        gameObject.ShowObj(true);
        RefreshUI(true);
        HandleOpenUIAnim();
    }

    public virtual void CloseUI()
    {        
        ClearOpenUIAnim();
        gameObject.ShowObj(false);
        UnRegisterAllEvent();
    }

    /// <summary>
    /// 刷新UI大小
    /// </summary>
    public virtual void RefreshUI(bool isOpenInit = false)
    {

    }

    /// <summary>
    /// 初始化所有按钮点击事件
    /// </summary>
    public void RegisterButtons()
    {
        Button[] buttonArray = gameObject.GetComponentsInChildren<Button>(true);
        if (buttonArray.IsNull())
            return;
        for (int i = 0; i < buttonArray.Length; i++)
        {
            Button itemButton = buttonArray[i];
            RegisterButton(itemButton);
        }
    }

    public void RegisterButton(Button button)
    {
        button.onClick.AddListener(() =>
        {
            if (!UIHandler.Instance.manager.CanClickUIButtons)
                return;
            OnClickForButton(button);
        });
    }

    /// <summary>
    /// 初始化所有输入事件
    /// </summary>
    public virtual void RegisterInputAction()
    {
        Dictionary<InputActionUIEnum, InputAction> dicUIData = InputHandler.Instance.manager.dicInputUI;
        foreach (var itemData in dicUIData)
        {
            InputActionUIEnum itemKey = itemData.Key;
            InputAction itemValue = itemData.Value;
            itemValue.started += CallBackForInputActionStarted;
        }
    }

    /// <summary>
    /// 注销所有输入事件
    /// </summary>
    public virtual void UnRegisterInputAction()
    {
        Dictionary<InputActionUIEnum, InputAction> dicUIData = InputHandler.Instance.manager.dicInputUI;
        foreach (var itemData in dicUIData)
        {
            InputActionUIEnum itemKey = itemData.Key;
            InputAction itemValue = itemData.Value;
            itemValue.started -= CallBackForInputActionStarted;
        }
    }

    /// <summary>
    /// 回调-输入时间反馈
    /// </summary>
    /// <param name="callback"></param>
    protected virtual void CallBackForInputActionStarted(CallbackContext callback)
    {
        if (callback.action.name.IsNull())
            return;
        if (!UIHandler.Instance.manager.CanInputActionStarted)
            return;
        if (gameObject.activeInHierarchy && gameObject.activeSelf)
        {
            //检测是否有弹窗 如果有的话就不执行快捷键操作
            if (UIHandler.Instance.manager.dialogList.Count > 0)
                return;
            this.WaitExecuteEndOfFrame(1, () =>
            {
                if (gameObject.activeInHierarchy && gameObject.activeSelf)
                    OnInputActionForStarted(callback.action.name.GetEnum<InputActionUIEnum>(), callback);
            });
        }
    }


    /// <summary>
    /// 按钮点击
    /// </summary>
    public virtual void OnClickForButton(Button viewButton)
    {

    }


    public virtual void OnInputActionForStarted(InputActionUIEnum inputType, CallbackContext callback)
    {

    }

    /// <summary>
    /// 处理UI打开动画
    /// </summary>
    public virtual void HandleOpenUIAnim()
    {
        if (uiOpenAnimTarget == null)
            return;
        ClearOpenUIAnim();
        switch (uiOpenAnimType)
        {
            case UIOpenAnimEnum.ScaleAnim:
                uiOpenAnimTarget.localScale = Vector3.zero;
                uiOpenAnimTarget
                    .DOScale(Vector3.one, 0.2f)
                    .SetEase(Ease.OutBack);
                break;
            default:
                uiOpenAnimTarget.localScale = Vector3.one;
                break;
        }
    }

    /// <summary>
    /// 清理UI打开动画
    /// </summary>
    public virtual void ClearOpenUIAnim()
    {
        if (uiOpenAnimTarget == null)
            return;
        uiOpenAnimTarget.DOKill();
        uiOpenAnimTarget.localScale = Vector3.one;
    }

    #region 注册事件
    public virtual void RegisterEvent(string eventName, Action action)
    {
        baseEvent.RegisterEvent(eventName, action);
    }

    public virtual void RegisterEvent<A>(string eventName, Action<A> action)
    {
        baseEvent.RegisterEvent(eventName, action);
    }
    public virtual void RegisterEvent<A, B>(string eventName, Action<A, B> action)
    {
        baseEvent.RegisterEvent(eventName, action);
    }
    public virtual void RegisterEvent<A, B, C>(string eventName, Action<A, B, C> action)
    {
        baseEvent.RegisterEvent(eventName, action);
    }
    public virtual void RegisterEvent<A, B, C, D>(string eventName, Action<A, B, C, D> action)
    {
        baseEvent.RegisterEvent(eventName, action);
    }
    
    public virtual void UnRegisterAllEvent()
    {
        baseEvent.UnRegisterAllEvent();
    }

    public virtual void TriggerEvent(string eventName)
    {
        baseEvent.TriggerEvent(eventName);
    }
    public virtual void TriggerEvent<A>(string eventName, A data)
    {
        baseEvent.TriggerEvent(eventName, data);
    }
    public virtual void TriggerEvent<A, B>(string eventName, A dataA, B dataB)
    {
        baseEvent.TriggerEvent(eventName, dataA, dataB);
    }
    public virtual void TriggerEvent<A, B, C>(string eventName, A dataA, B dataB, C dataC)
    {
        baseEvent.TriggerEvent(eventName, dataA, dataB, dataC);
    }
    public virtual void TriggerEvent<A, B, C, D>(string eventName, A dataA, B dataB, C dataC, D dataD)
    {
        baseEvent.TriggerEvent(eventName, dataA, dataB, dataC, dataD);
    }
    #endregion
}