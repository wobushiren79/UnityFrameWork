using UnityEditor;
using UnityEngine;

public class RayUtil
{

    /// <summary>
    /// 屏幕中心射线检测
    /// </summary>
    /// <param name="maxDistance"></param>
    /// <param name="layerMask"></param>
    /// <param name="isCollider"></param>
    /// <param name="hit"></param>
    /// <param name="camera"></param>
    public static void RayToScreenPointForScreenCenter(float maxDistance, int layerMask, out bool isCollider, out RaycastHit hit, Camera camera = null)
    {
        RayToScreenPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0), maxDistance, layerMask, out isCollider, out hit, camera);
    }

    public static void RayAllToScreenPointForScreenCenter(float maxDistance, int layerMask, out RaycastHit[] arrayHit, Camera camera = null)
    {
        RayAllToScreenPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0), maxDistance, layerMask, out arrayHit, camera);
    }

    public static bool RayCheckToScreenPointForScreenCenter(float maxDistance, int layerMask, Camera camera = null)
    {
        return RayCheckToScreenPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0), maxDistance, layerMask, camera);
    }

    /// <summary>
    /// 屏幕点击射线检测
    /// </summary>
    /// <param name="maxDistance"></param>
    /// <param name="layerMask"></param>
    /// <param name="isCollider"></param>
    /// <param name="hit"></param>
    /// <param name="camera"></param>
    public static void RayToScreenPointForMousePosition(float maxDistance, int layerMask, out bool isCollider, out RaycastHit hit)
    {
        RayToScreenPoint(Input.mousePosition, maxDistance, layerMask, out isCollider, out hit);
    }
    public static void RayAllToScreenPointForMousePosition(float maxDistance, int layerMask, out RaycastHit[] arrayHit)
    {
        RayAllToScreenPoint(Input.mousePosition, maxDistance, layerMask, out arrayHit);
    }

    /// <summary>
    /// 屏幕位置发射射线检测
    /// </summary>
    /// <param name="screenPoint"></param>
    /// <param name="maxDistance"></param>
    /// <param name="layerMask"></param>
    /// <param name="isCollider"></param>
    /// <param name="hit"></param>
    /// <param name="camera"></param>
    public static void RayToScreenPoint(Vector3 screenPoint, float maxDistance, int layerMask, out bool isCollider, out RaycastHit hit, Camera camera = null)
    {
        if (camera == null) camera = Camera.main;
        Ray ray = camera.ScreenPointToRay(screenPoint);
        isCollider = Physics.Raycast(ray, out hit, maxDistance, layerMask);
    }

    public static void RayAllToScreenPoint(Vector3 screenPoint, float maxDistance, int layerMask, out RaycastHit[] arrayHit, Camera camera = null)
    {
        if (camera == null) camera = Camera.main;
        Ray ray = camera.ScreenPointToRay(screenPoint);
        arrayHit = Physics.RaycastAll(ray, maxDistance, layerMask);
    }
    public static bool RayCheckToScreenPoint(Vector3 screenPoint, float maxDistance, int layerMask, Camera camera = null)
    {
        if (camera == null) camera = Camera.main;
        Ray ray = camera.ScreenPointToRay(screenPoint);
        return Physics.Raycast(ray, maxDistance, layerMask);
    }

    /// <summary>
    /// 球体-球体范围内
    /// </summary>
    /// <param name="centerPosition"></param>
    /// <param name="radius"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static Collider[] OverlapToSphere(Vector3 centerPosition, float radius, int layer)
    {
        return Physics.OverlapSphere(centerPosition, radius, layer);
    }

    /// <summary>
    /// 球体-球体范围内
    /// </summary>
    /// <param name="centerPosition"></param>
    /// <param name="radius"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static bool CheckToSphere(Vector3 centerPosition, float radius, int layer)
    {
        return Physics.CheckSphere(centerPosition, radius, layer);
    }

    /// <summary>
    /// 方块
    /// </summary>
    /// <param name="centerPosition"></param>
    /// <param name="halfEx"></param>
    /// <param name="quaternion"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static Collider[] OverlapToBox(Vector3 centerPosition, Vector3 halfEx, Quaternion quaternion, int layer)
    {
        return Physics.OverlapBox(centerPosition, halfEx, quaternion, layer);
    }
    public static Collider[] OverlapToBox(Vector3 centerPosition, Vector3 halfEx, int layer)
    {
        return Physics.OverlapBox(centerPosition, halfEx, Quaternion.identity, layer);
    }

    /// <summary>
    /// 射线
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    /// <param name="maxDistance"></param>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    public static RaycastHit[] RayToCastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
    {
        return Physics.RaycastAll(origin, direction, maxDistance, layerMask);
    }

    /// <summary>
    /// 射线
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    /// <param name="maxDistance"></param>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    public static bool RayToCast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, out RaycastHit hit)
    {
        return Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
    }

    public static bool CheckToCast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
    {
        return Physics.Raycast(origin, direction, maxDistance, layerMask);
    }
}