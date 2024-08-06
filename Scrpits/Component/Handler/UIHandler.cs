using System;
using System.Collections.Generic;
using UnityEngine;

public partial class UIHandler : BaseUIHandler<UIHandler, UIManager>
{

    /// <summary>
    /// 获取打开的UI
    /// </summary>
    /// <returns></returns>
    public BaseUIComponent GetOpenUI(int layer = -1)
    {
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (itemUI.gameObject.activeSelf)
            {
                //设置层级
                if (layer >= 0)
                {
                    itemUI.transform.SetSiblingIndex(layer);
                }
                return itemUI;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取打开UI的名字
    /// </summary>
    /// <returns></returns>
    public string GetOpenUIName()
    {
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (itemUI.gameObject.activeSelf)
            {
                return itemUI.name;
            }
        }
        return null;
    }

    /// <summary>
    /// 根据UI的名字获取UI
    /// </summary>
    /// <param name="uiName"></param>
    /// <returns></returns>
    public T GetUI<T>(int layer = -1, string uiNameIn = null) where T : BaseUIComponent
    {
        string uiName = uiNameIn.IsNull() ? typeof(T).Name : uiNameIn;
        if (manager.uiList == null || uiName.IsNull())
            return null;
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (itemUI.name.Equals(uiName))
            {
                //设置层级
                if (layer >= 0)
                {
                    itemUI.transform.SetSiblingIndex(layer);
                }
                return itemUI as T;
            }
        }
        T uiComponent = manager.CreateUI<T>(uiName, layer);
        if (uiComponent)
        {
            return uiComponent as T;
        }
        return null;
    }

    /// <summary>
    /// 根据UI的名字获取UI列表
    /// </summary>
    /// <param name="uiName"></param>
    /// <returns></returns>
    public List<BaseUIComponent> GetUIList(string uiName)
    {
        if (manager.uiList == null || uiName.IsNull())
            return null;
        List<BaseUIComponent> tempuiList = new List<BaseUIComponent>();
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (itemUI.name.Equals(uiName))
            {
                tempuiList.Add(itemUI);
            }
        }
        return tempuiList;
    }

    /// <summary>
    /// 通过UI的名字开启UI
    /// </summary>
    /// <param name="uiName"></param>
    public T OpenUI<T>(Action<T> actionBeforeOpen = null, int layer = -1, string uiNameIn = null) where T : BaseUIComponent
    {
        string uiName = uiNameIn.IsNull() ? typeof(T).Name : uiNameIn;
        if (uiName.IsNull())
            return null;
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (itemUI.name.Equals(uiName))
            {
                //设置层级
                if (layer >= 0)
                {
                    itemUI.transform.SetSiblingIndex(layer);
                }
                actionBeforeOpen?.Invoke(itemUI as T);
                itemUI.OpenUI();
                return itemUI as T;
            }
        }
        T uiComponent = manager.CreateUI<T>(uiName, layer);
        if (uiComponent)
        {
            actionBeforeOpen?.Invoke(uiComponent as T);
            uiComponent.OpenUI();
            return uiComponent;
        }
        return null;
    }


    /// <summary>
    /// 通过UI的名字关闭UI
    /// </summary>
    /// <param name="uiName"></param>
    public void CloseUI(string uiName, int layer = -1)
    {
        if (manager.uiList == null || uiName.IsNull())
            return;
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (itemUI.name.Equals(uiName))
            {
                //设置层级
                if (layer >= 0)
                {
                    itemUI.transform.SetSiblingIndex(layer);
                }
                itemUI.CloseUI();
            }
        }
    }

    /// <summary>
    /// 关闭UI
    /// </summary>
    public void CloseUI<T>(int layer = -1) where T : BaseUIComponent
    {
        if (manager.uiList == null)
            return;
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (itemUI as T)
            {
                //设置层级
                if (layer >= 0)
                {
                    itemUI.transform.SetSiblingIndex(layer);
                }
                itemUI.CloseUI();
            }
        }
    }

    /// <summary>
    /// 关闭所有UI
    /// </summary>
    public void CloseAllUI()
    {
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (itemUI.gameObject.activeSelf)
                itemUI.CloseUI();
        }
    }

    /// <summary>
    /// 通过UI的名字开启UI并关闭其他UI
    /// </summary>
    /// <param name="uiName"></param>
    public T OpenUIAndCloseOther<T>(Action<T> actionBeforeOpen = null, int layer = -1, string uiNameIn = null) where T : BaseUIComponent
    {
        string uiName = uiNameIn.IsNull() ? typeof(T).Name : uiNameIn;
        if (manager.uiList == null || uiName.IsNull())
            return null;
        //首先关闭其他UI
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (!itemUI.name.Equals(uiName))
            {
                if (itemUI.gameObject.activeSelf)
                    itemUI.CloseUI();
            }
        }
        return OpenUI<T>(actionBeforeOpen, layer, uiName);
    }

    /// <summary>
    /// 通过UI开启UI并关闭其他UI
    /// </summary>
    /// <param name="uiName"></param>
    public void OpenUIAndCloseOther(BaseUIComponent uiComponent, Action<BaseUIComponent> actionBeforeOpen = null, int layer = -1)
    {
        if (manager.uiList == null || uiComponent == null)
            return;
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (!itemUI == uiComponent)
            {
                itemUI.CloseUI();
            }
        }
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (itemUI == uiComponent)
            {
                //设置层级
                if (layer >= 0)
                {
                    itemUI.transform.SetSiblingIndex(layer);
                }
                actionBeforeOpen?.Invoke(itemUI);
                itemUI.OpenUI();
            }
        }
    }

    /// <summary>
    /// 刷新UI
    /// </summary>
    public void RefreshAllUI()
    {
        if (manager.uiList == null)
            return;
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            itemUI.RefreshUI();
        }
    }

    /// <summary>
    /// 根据名字刷新UI
    /// </summary>
    /// <param name="uiName"></param>
    public void RefreshUI(string uiName, int layer = -1)
    {
        if (manager.uiList == null || uiName.IsNull())
            return;
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            BaseUIComponent itemUI = manager.uiList[i];
            if (itemUI.name.Equals(uiName))
            {
                //设置层级
                if (layer >= 0)
                {
                    itemUI.transform.SetSiblingIndex(layer);
                }
                itemUI.RefreshUI();
            }
        }
    }

    /// <summary>
    /// 刷新打开的UI
    /// </summary>
    public void RefreshUI()
    {
        BaseUIComponent itemUI = GetOpenUI();
        itemUI.RefreshUI();
    }

    /// <summary>
    /// 打开弹窗
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dialogBean"></param>
    /// <param name="delayDelete"></param>
    /// <returns></returns>
    public T ShowDialog<T>(DialogBean dialogBean) where T : DialogView
    {
        return manager.CreateDialog<T>(dialogBean);
    }

    /// <summary>
    /// Toast提示
    /// </summary>
    /// <param name="hintContent"></param>
    public T ToastHint<T>(string hintContent) where T : ToastView
    {
        ToastBean toastData = new ToastBean(ToastEnum.Normal, hintContent);
        return manager.CreateToast<T>(toastData);
    }

    public T ToastHint<T>(string hintContent, float showTime) where T : ToastView
    {
        ToastBean toastData = new ToastBean(ToastEnum.Normal, hintContent, showTime);
        return manager.CreateToast<T>(toastData);
    }

    public T ToastHint<T>(Sprite toastIconSp, string hintContent) where T : ToastView
    {
        ToastBean toastData = new ToastBean(ToastEnum.Normal, hintContent, toastIconSp);
        return manager.CreateToast<T>(toastData);
    }

    public T ToastHint<T>(Sprite toastIconSp, string hintContent, float showTime) where T : ToastView
    {
        ToastBean toastData = new ToastBean(ToastEnum.Normal, hintContent, toastIconSp, showTime);
        return manager.CreateToast<T>(toastData);
    }

    /// <summary>
    /// 展示气泡
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="popup"></param>
    /// <returns></returns>
    public T ShowPopup<T>(PopupBean popupData) where T : PopupShowView
    {
        if (manager.popupList.TryGetValue(popupData.PopupType, out PopupShowView popup))
        {
            popup.ShowObj(true);
            return popup as T;
        }
        else
        {
            T newPopup = manager.CreatePopup<T>(popupData);
            return newPopup;
        }
    }

    /// <summary>
    /// 隐藏气泡
    /// </summary>
    public void HidePopup(PopupEnum popupType)
    {
        if (manager.popupList.TryGetValue(popupType, out PopupShowView popup))
        {
            popup.ShowObj(false);
        }
    }

}