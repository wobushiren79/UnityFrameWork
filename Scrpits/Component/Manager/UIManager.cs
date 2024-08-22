using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class UIManager : BaseUIManager
{
    //所有的UI控件
    public List<BaseUIComponent> uiList = new List<BaseUIComponent>();
    //所有的dialog列表
    public List<DialogView> dialogList = new List<DialogView>();
    //所有的popup
    public Dictionary<PopupEnum, PopupShowView> popupList = new Dictionary<PopupEnum, PopupShowView>();

    //所有的dialog模型
    public Dictionary<string, GameObject> dicDialogModel = new Dictionary<string, GameObject>();
    //所有的Toast模型
    public Dictionary<string, GameObject> dicToastModel = new Dictionary<string, GameObject>();
    //所有Popup模型
    public Dictionary<string, GameObject> dicPopupModel = new Dictionary<string, GameObject>();

    //是否能点击UI按钮
    public bool CanClickUIButtons = true;
    public bool CanInputActionStarted = true;

    /// <summary>
    /// 获取弹窗模型
    /// </summary>
    /// <param name="dialogName"></param>
    /// <returns></returns>
    public GameObject GetDialogModel(string dialogName)
    {
        return GetModelForResources(dicDialogModel, $"UI/Dialog/UIDialog{dialogName}");
    }

    /// <summary>
    /// 获取toast模型
    /// </summary>
    /// <returns></returns>
    public GameObject GetToastModel(string toastName)
    {
        return GetModelForResources(dicToastModel, $"UI/Toast/UIToast{toastName}");
    }

    /// <summary>
    /// 获取弹窗模型
    /// </summary>
    /// <param name="dialogName"></param>
    /// <returns></returns>
    public GameObject GetPopupModel(string popupName)
    {
        return GetModelForResources(dicPopupModel, $"UI/Popup/UIPopup{popupName}");
    }

    /// <summary>
    /// 创建UI
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="uiName"></param>
    /// <returns></returns>
    public T CreateUI<T>(string uiName, int layer = -1, UITypeEnum uiType = UITypeEnum.UIBase) where T : BaseUIComponent
    {
        //GameObject uiModel = LoadAssetUtil.SyncLoadAsset<GameObject>("ui/ui", uiName);
        BaseUIComponent uiModel = LoadResourcesUtil.SyncLoadData<BaseUIComponent>($"UI/{uiName}");
        if (uiModel)
        {
            Transform tfContainer = GetUITypeContainer(uiType);
            GameObject objUIComponent = Instantiate(tfContainer.gameObject, uiModel.gameObject);
            objUIComponent.SetActive(false);
            objUIComponent.name = objUIComponent.name.Replace("(Clone)", "");
            if (layer >= 0)
            {
                //设置层级
                objUIComponent.transform.SetSiblingIndex(layer);
            }
            T uiComponent = objUIComponent.GetComponent<T>();
            uiList.Add(uiComponent);
            return uiComponent;
        }
        else
        {
            LogUtil.LogError("没有找到指定UI：" + "UI/" + uiName);
            return null;
        }
    }

    /// <summary>
    /// 创建弹窗
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dialogBean"></param>
    /// <param name="delayDelete"></param>
    /// <returns></returns>
    public T CreateDialog<T>(DialogBean dialogBean) where T : DialogView
    {
        string dialogName = dialogBean.dialogType.GetEnumName();
        GameObject objDialogModel = GetDialogModel(dialogName);
        if (objDialogModel == null)
        {
            LogUtil.LogError("没有找到指定Dialog：" + dialogName);
            return null;
        }
        Transform tfContainer = GetUITypeContainer(UITypeEnum.Dialog);
        GameObject objDialog = Instantiate(tfContainer.gameObject, objDialogModel);
        if (objDialog)
        {
            DialogView dialogView = objDialog.GetComponent<DialogView>();
            if (dialogView == null)
                Destroy(objDialog);
            dialogView.SetCallBack(dialogBean.callBack);
            dialogView.SetAction(dialogBean.actionSubmit, dialogBean.actionCancel);
            dialogView.SetData(dialogBean);
            if (dialogBean.delayDelete != 0)
                dialogView.SetDelayDelete(dialogBean.delayDelete);

            //改变焦点
            EventSystem.current.SetSelectedGameObject(objDialog);
            dialogList.Add(dialogView);
            return dialogView as T;
        }
        else
        {
            LogUtil.LogError("没有实例化Dialog成功：" + dialogName);
            return null;
        }
    }

    /// <summary>
    /// 移除弹窗
    /// </summary>
    /// <param name="dialogView"></param>
    public void RemoveDialog(DialogView dialogView)
    {
        if (dialogView != null && dialogList.Contains(dialogView))
            dialogList.Remove(dialogView);
    }

    /// <summary>
    /// 关闭所有弹窗
    /// </summary>
    public void CloseAllDialog()
    {
        for (int i = 0; i < dialogList.Count; i++)
        {
            DialogView dialogView = dialogList[i];
            if (dialogView != null)
                dialogView.DestroyDialog();
        }
        dialogList.Clear();
    }

    /// <summary>
    /// 创建toast
    /// </summary>
    /// <param name="toastType"></param>
    /// <param name="toastIconSp"></param>
    /// <param name="toastContentStr"></param>
    /// <param name="destoryTime"></param>
    public T CreateToast<T>(ToastBean toastData) where T : ToastView
    {
        string toastName = toastData.toastType.GetEnumName();
        GameObject objToastModel = GetToastModel(toastName);
        if (objToastModel == null)
        {
            LogUtil.LogError("没有找到指定Toast：" + toastName);
            return null;
        }
        Transform objToastContainer = GetUITypeContainer(UITypeEnum.Toast);
        Transform objToastContainerList = objToastContainer.Find("UIToastList").Find("Container");
        GameObject objToast = Instantiate(objToastContainerList.gameObject, objToastModel);
        if (objToast)
        {
            T toastView = objToast.GetComponent<T>();
            toastView.SetData(toastData);
            return toastView;
        }
        else
        {
            LogUtil.LogError("实例化Toast失败" + toastName);
            return null;
        }
    }

    /// <summary>
    /// 创建气泡
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="popupName"></param>
    /// <returns></returns>
    public T CreatePopup<T>(PopupBean popopData) where T : PopupShowView
    {
        string popupName = popopData.PopupType.GetEnumName();
        GameObject objModel = GetPopupModel(popupName);
        if (objModel == null)
        {
            LogUtil.LogError("没有找到指定popup：" + popupName);
            return default(T);
        }
        Transform objPopupContainer = GetUITypeContainer(UITypeEnum.Popup);
        GameObject objPopup = Instantiate(objPopupContainer.gameObject, objModel);
        T popup = objPopup.GetComponent<T>();
        popupList.Add(popopData.PopupType, popup);
        return popup;
    }

}