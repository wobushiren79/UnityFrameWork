using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UGUIUtil
{
    public static Vector3 GetRootPos(
        Transform newParent, 
        Transform originalTF)
    {
        return GetRootPos(newParent, originalTF.position);
    }

    public static Vector3 GetRootPos(
        Transform newParent, 
        Vector3 originalPosition)
    {
        return newParent.InverseTransformPoint(originalPosition);
    }

    public static Vector2 GetRootPosForUI(
        RectTransform originalRTF, 
        RectTransform newParent, 
        Camera cameraParent = null)
    {
        return GetRootPosForUI(originalRTF.position, newParent, cameraParent);
    }

    public static Vector2 GetRootPosForUI(
        Vector3 originalWorldPosition, 
        RectTransform newParent, 
        Camera cameraParent = null)
    {
        // 获取子控件的屏幕坐标
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cameraParent, originalWorldPosition);
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