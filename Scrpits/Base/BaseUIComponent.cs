using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public class BaseUIComponent : BaseUIInit
{
    //UI管理
    [HideInInspector]
    public BaseUIManager uiManager;
    //备注数据
    public string remarkData;

    public override void Awake()
    {
        if (uiManager == null)
            uiManager = GetComponentInParent<BaseUIManager>();
        base.Awake();
    }

    /// <summary>
    /// 开启UI
    /// </summary>
    public override void OpenUI()
    {
        base.OpenUI();
        RegisterInputAction();
    }

    /// <summary>
    /// 关闭UI
    /// </summary>
    public override void CloseUI()
    {
        base.CloseUI();
        StopAllCoroutines();
        UnRegisterInputAction();
    }

    /// <summary>
    /// 设置备用数据
    /// </summary>
    /// <param name="remarkData"></param>
    public virtual void SetRemarkData(string remarkData)
    {
        this.remarkData = remarkData;
    }

    public T GetUIManager<T>() where T : BaseUIManager
    {
        return uiManager as T;
    }
}
