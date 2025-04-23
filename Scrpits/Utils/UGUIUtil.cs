using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UGUIUtil
{
    /// <summary>
    /// 获取TF在指定UIroot下的坐标
    /// </summary>
    public static Vector3 GetRootPos(Transform tfRoot, Transform targetTF)
    {
        return GetRootPos(tfRoot, targetTF.position);
    }

    public static Vector3 GetRootPos(Transform tfRoot, Vector3 targetPosition)
    {
        return tfRoot.InverseTransformPoint(targetPosition);
    }

    /// <summary>
    /// 未验证
    /// </summary>
    public static Vector2 GetRootPosForUI(RectTransform child, RectTransform newParent, Camera cameraParent)
    {
        // 获取子控件的屏幕坐标
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cameraParent, child.position);

        // 转换为目标父控件的局部坐标
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            newParent,
            screenPos,
            cameraParent,
            out localPos
        );

        return localPos;
    }

    public static Vector2 GetRootPosForUI(RectTransform child, RectTransform originalParent, RectTransform newParent)
    {
        return GetRootPosForUI(child.anchoredPosition,  originalParent,  newParent);
    }

    public static Vector2 GetRootPosForUI(Vector2 childPosition, RectTransform originalParent, RectTransform newParent)
    {
        // 1. 获取子控件在世界空间中的位置
        Vector3 worldPosition = originalParent.TransformPoint(childPosition);

        // 2. 将世界坐标转换为新父节点的局部空间
        Vector3 localPosition = newParent.InverseTransformPoint(worldPosition);

        return localPosition;
    }
    
    /// <summary>
    /// 是否点击到了UI
    /// </summary>
    /// <returns></returns>
    public static bool IsPointerUI()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Mouse.current.position.ReadValue();
        List<RaycastResult> raycastResultsList = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResultsList);
        for (int i = 0; i < raycastResultsList.Count; i++)
        {
            if (raycastResultsList[i].gameObject.GetType() == typeof(GameObject))
            {
                return true;
            }
        }
        return false;

        ////只有在电脑上有用 手机没用
        //if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        //{
        //    //点击到了UI
        //    return true;
        //}
        //else
        //{
        //    //没有点击到UI
        //    return false;
        //}
    }

    /// <summary>
    /// 获得当前点击到的UI物体
    /// </summary>
    public static GameObject GetUICurrentSelect()
    {
        GameObject obj = null;

        GraphicRaycaster[] graphicRaycasters = GameObject.FindObjectsOfType<GraphicRaycaster>();

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.pressPosition = Input.mousePosition;
        eventData.position = Input.mousePosition;
        List<RaycastResult> list = new List<RaycastResult>();

        foreach (var item in graphicRaycasters)
        {
            item.Raycast(eventData, list);
            if (list.Count > 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    obj = list[i].gameObject;
                }
            }
        }

        return obj;
    }

    /// <summary>
    /// 刷新UI大小
    /// </summary>
    /// <param name="rectTransform"></param>
    public static void RefreshUISize(RectTransform rectTransform)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }
}