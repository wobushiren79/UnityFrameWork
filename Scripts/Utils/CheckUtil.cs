
//using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class CheckUtil {


    /// <summary>
    /// 判断路径是否有效
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="endPosition"></param>
    /// <returns></returns>
    public static bool CheckPath(Vector3 startPosition, Vector3 endPosition)
    {
        //ABPath path = ABPath.Construct(startPosition, endPosition);
        //path.calculatePartial = true;
        //AstarPath.StartPath(path);
        //AstarPath.BlockUntilCalculated(path);
        //if (path.originalEndPoint == path.endPoint)
        //{
        //    return true;
        //}
        //else
        //{
        //    return false;
        //}
        return true;
    }

    /// <summary>
    /// 是否点击到了UI
    /// </summary>
    /// <returns></returns>
    public static bool CheckIsPointerUI()
    {
        //点击到了UI
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            if (Input.touchCount > 0)
            {
                int fingerId = Input.GetTouch(0).fingerId;
                if (EventSystem.current.IsPointerOverGameObject(fingerId))
                    return true;
            }
        }
        else
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return true;
        }
        return false;
    }

}