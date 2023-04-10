using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
public class GameUtil
{
    /// <summary>
    /// 获取tf里随机一个点
    /// </summary>
    /// <param name="tfTarget"></param>
    /// <returns></returns>
    /// 
    public static Vector3 GetTransformInsidePosition2D(Transform tfTarget)
    {
        return GetTransformInsidePosition2D(tfTarget, 1);
    }
    public static Vector3 GetTransformInsidePosition2D(Transform tfTarget, float size)
    {
        if (tfTarget == null)
            return Vector3.zero;
        float tempX = tfTarget.localScale.x / 2f;
        float tempY = tfTarget.localScale.y / 2f;
        //修正避免太靠边
        //tempX -= 0.5f;
        //tempY -= 0.5f;

        float randomXoff = Random.Range(-tempX, tempX);
        float randomYoff = Random.Range(-tempY, tempY);
        return new Vector3(tfTarget.position.x + randomXoff * size, tfTarget.position.y + randomYoff * size);
    }

    /// <summary>
    /// 获取屏幕宽
    /// </summary>
    /// <returns></returns>
    public static float GetScreenWith()
    {
        float leftBorder;
        float rightBorder;
        float topBorder;
        float downBorder;
        Vector3 cornerPos = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, Mathf.Abs(Camera.main.transform.position.z)));

        leftBorder = Camera.main.transform.position.x - (cornerPos.x - Camera.main.transform.position.x);
        rightBorder = cornerPos.x;
        topBorder = cornerPos.y;
        downBorder = Camera.main.transform.position.y - (cornerPos.y - Camera.main.transform.position.y);

        return rightBorder - leftBorder;
    }
    /// <summary>
    /// 获取屏幕高
    /// </summary>
    /// <returns></returns>
    public static float GetScreenHeight()
    {
        float leftBorder;
        float rightBorder;
        float topBorder;
        float downBorder;
        Vector3 cornerPos = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, Mathf.Abs(Camera.main.transform.position.z)));

        leftBorder = Camera.main.transform.position.x - (cornerPos.x - Camera.main.transform.position.x);
        rightBorder = cornerPos.x;
        topBorder = cornerPos.y;
        downBorder = Camera.main.transform.position.y - (cornerPos.y - Camera.main.transform.position.y);

        return topBorder - downBorder;
    }

    /// <summary>
    /// 刷新UI
    /// </summary>
    /// <param name="rectTransform"></param>
    public static void RefreshRectTransform(RectTransform rectTransform)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }
    public static void RefreshRectTransform(GameObject obj)
    {
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            RefreshRectTransform(rectTransform);
        }
    }

    /// <summary>
    /// 鼠标位置转为屏幕UGUI位置
    /// </summary>
    /// <param name="camera">如果画布是screenspace-overlay 则直接设置相机为NULL</param>
    /// <param name="uiParent"></param>
    /// <returns></returns>
    public static Vector2 MousePointToUGUIPoint(Camera camera, RectTransform uiParent)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiParent, Input.mousePosition, camera, out Vector2 vecMouse);
        return vecMouse;
    }

    /// <summary>
    /// 世界坐标转UI坐标
    /// </summary>
    /// <param name="parentUI"></param>
    /// <param name="worldPosition"></param>
    /// <param name="originCamera"></param>
    /// <param name="targetCamera"></param>
    /// <returns></returns>
    public static Vector2 WorldPointToUILocalPoint(RectTransform parentUI, Vector3 worldPosition, Camera originCamera = null, Camera uiCamera = null)
    {
        if (originCamera)
            originCamera = Camera.main;
        Vector3 screenPoint = originCamera.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentUI, screenPoint, uiCamera, out Vector2 vecMouse);
        return vecMouse;
    }

    /// <summary>
    /// 获取物体LookAt后的旋转值
    /// </summary>
    /// <param name="originalObj"></param>
    /// <param name="targetPoint"></param>
    /// <returns></returns>
    public static Vector3 GetLookAtEuler(Vector3 originalPoint, Vector3 targetPoint)
    {
        //计算物体在朝向某个向量后的正前方
        Vector3 forwardDir = targetPoint - originalPoint;
        //计算朝向这个正前方时的物体四元数值
        Quaternion lookAtRot = Quaternion.LookRotation(forwardDir);
        //把四元数值转换成角度
        Vector3 resultEuler = lookAtRot.eulerAngles;
        return resultEuler;
    }

    /// <summary>
    /// 离开游戏
    /// </summary>
    public static void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
    }


}