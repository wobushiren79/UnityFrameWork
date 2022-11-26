using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public class BaseUIInit : BaseMonoBehaviour
{
    protected List<string> listEvents = new List<string>();

    public virtual void Awake()
    {
        AutoLinkUI();
        RegisterButtons();
    }

    public virtual void OnDestroy()
    {
        UnRegisterInputAction();
    }

    public virtual void OnDisable()
    {

    }
    public virtual void OnEnable()
    {

    }

    public virtual void OpenUI()
    {
        gameObject.ShowObj(true);
        RefreshUI(true);
    }

    public virtual void CloseUI()
    {
        gameObject.ShowObj(false);
        //注销所有事件
        for (int i = 0; i < listEvents.Count; i++)
        {
            string itemEvent = listEvents[i];
            UnRegisterEvent(itemEvent);
        }
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
        Button[] buttonArray = gameObject.GetComponentsInChildren<Button>();
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



    #region 注册事件
    public virtual void RegisterEvent(string eventName, Action action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }

    public virtual void RegisterEvent<A>(string eventName, Action<A> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }
    public virtual void RegisterEvent<A, B>(string eventName, Action<A, B> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }
    public virtual void UnRegisterEvent(string eventName)
    {
        EventHandler.Instance.UnRegisterEvent(eventName);
        listEvents.Remove(eventName);
    }

    public virtual void TriggerEvent(string eventName)
    {
        EventHandler.Instance.TriggerEvent(eventName);
    }
    public virtual void TriggerEvent<A>(string eventName, A data)
    {
        EventHandler.Instance.TriggerEvent(eventName, data);
    }
    public virtual void TriggerEvent<A, B>(string eventName, A dataA, B dataB)
    {
        EventHandler.Instance.TriggerEvent(eventName, dataA, dataB);
    }
    #endregion
}