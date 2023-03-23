using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BaseUIManager : BaseManager
{
    public GameObject objUIContainer;

    public Dictionary<UITypeEnum, Transform> dicContainer = new Dictionary<UITypeEnum, Transform>();

    public virtual void Awake()
    {
        InitUI();
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    public void InitUI()
    {
        GameObject uiContainerModel = LoadResourcesUtil.SyncLoadData<GameObject>("UI/Base/UIContainer");
        //实例化UI容器
        objUIContainer = Instantiate(gameObject, uiContainerModel);
        //初始化所有UI容器
        InitUIType();
        //修改一些摄像机
        InitCanvasCamera();
    }

    /// <summary>
    /// 初始化画布的摄像头
    /// </summary>
    /// <param name="camera"></param>
    public void InitCanvasCamera()
    {
        foreach (var itemUIContainer in dicContainer)
        {
            UITypeEnum uiTypeEnum = itemUIContainer.Key;
            switch (uiTypeEnum) 
            {
                case UITypeEnum.UIBase:
                case UITypeEnum.Dialog:
                case UITypeEnum.Toast:
                case UITypeEnum.Popup:
                    Canvas canvas = itemUIContainer.Value.GetComponent<Canvas>();
                    canvas.worldCamera = CameraHandler.Instance.manager.uiCamera;
                    break;
            }
        }
    }

    /// <summary>
    /// 初始化所有UI容器
    /// </summary>
    public void InitUIType()
    {
        //初始化所有UIType容器
        List<UITypeEnum> listUIType = EnumExtension.GetEnumValue<UITypeEnum>();
        foreach (var itemData in listUIType)
        {
            GetUITypeContainer(itemData);
        }
    }

    /// <summary>
    /// 获取UI容器
    /// </summary>
    /// <param name="uiType"></param>
    public virtual Transform GetUITypeContainer(UITypeEnum uiType)
    {
        if (dicContainer.TryGetValue(uiType, out Transform value))
        {
            return value;
        }
        Transform containerForType = objUIContainer.FindChild<Transform>(uiType.GetEnumName());
        if (containerForType == null)
        {
            GameObject newContainer = new GameObject(uiType.GetEnumName());
            newContainer.transform.SetParent(objUIContainer.transform);
        }
        dicContainer.Add(uiType, containerForType);
        return containerForType;
    }

}
