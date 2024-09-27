using UnityEngine;
using UnityEditor;

public class VectorUtil
{
    /// <summary>
    /// 获取两个点的夹角 vec2相对于vec1 的夹角
    /// </summary>
    /// <param name="vec1">坐标原点</param>
    /// <param name="vec2"></param>
    /// <returns></returns>
    public static float GetAngle(Vector3 vec1, Vector3 vec2)
    {
        //Vector3 v3 = Vector3.Cross(from_, to_);
        //if (v3.z > 0)
        //    return Vector3.Angle(from_, to_);
        //else
        //    return 360 - Vector3.Angle(from_, to_);
        Vector2 diference = vec2 - vec1;
        float sign = (vec2.y < vec1.y) ? -1.0f : 1.0f;
        return Vector2.Angle(Vector2.right, diference) * sign;
    }

    /// <summary>
    ///  计算直线AB相对于X轴的倾斜角度
    /// </summary>
    public static float GetAngleForXLine(Vector2 pointA, Vector2 pointB)
    {
        float angle = Mathf.Atan2(pointB.y - pointA.y, pointB.x - pointA.x) * Mathf.Rad2Deg;
        return angle;
    }

    /// <summary>
    /// 获取圆上一点 逆时针
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="centerPosition"></param>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static Vector2 GetCirclePosition(Vector2 startPosition, Vector2 centerPosition, float angle)
    {
        Vector2 circlePosition;
        angle = (float)(angle / 180.0) * Mathf.PI;
        float a = Mathf.Cos(angle);
        float b = Mathf.Sin(angle);
        circlePosition.x = (startPosition.x - centerPosition.x) * a + (startPosition.y - centerPosition.y) * b + centerPosition.x;
        circlePosition.y = -(startPosition.x - centerPosition.x) * b + (startPosition.y - centerPosition.y) * a + centerPosition.y;
        return circlePosition;
    }

    /// <summary>
    /// 获取圆上一点坐标 顺时针
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="centerPosition"></param>
    /// <param name="r"></param>
    /// <returns></returns>
    public static Vector2 GetCirclePosition(float angle, Vector2 centerPosition, float r)
    {
        float x = centerPosition.x + r * Mathf.Cos(angle * Mathf.PI / 180f);
        float y = centerPosition.y + r * Mathf.Sin(angle * Mathf.PI / 180f);
        Vector2 circlePosition = new Vector2(x, y);
        return circlePosition;
    }

    /// <summary>
    /// 获取圆上几点 逆时针
    /// </summary>
    /// <param name="number"></param>
    /// <param name="startPosition"></param>
    /// <param name="centerPosition"></param>
    /// <param name="angle"></param>
    /// <param name="isLoop"></param>
    /// <returns></returns>
    public static Vector2[] GetListCirclePosition(int number, Vector2 startPosition, Vector2 centerPosition, float angle, bool isLoop = false)
    {
        int numberTotal = (isLoop ? number + 1 : number);
        Vector2[] listData = new Vector2[numberTotal];
        float itemAngle = 360f / number;
        angle -= itemAngle;
        for (int i = 0; i < number; i++)
        {
            angle += itemAngle;
            listData[i] = GetCirclePosition(startPosition, centerPosition, angle);
        }
        if (isLoop)
        {
            listData[number] = listData[0];
        }
        return listData;
    }

    /// <summary>
    /// 获取圆上几点 顺时针
    /// </summary>
    /// <param name="number"></param>
    /// <param name="startAngle">0度为最右边</param>
    /// <param name="centerPosition"></param>
    /// <param name="r"></param>
    /// <param name="isLoop">是否是循环，是的话会再加上1个起始点</param>
    /// <returns></returns>
    public static Vector2[] GetListCirclePosition(int number, float startAngle, Vector2 centerPosition, float r, bool isLoop = false)
    {
        int numberTotal = (isLoop ? number + 1 : number);
        Vector2[] listData = new Vector2[numberTotal];
        float itemAngle = 360f / number;
        startAngle -= itemAngle;
        for (int i = 0; i < number; i++)
        {
            startAngle += itemAngle;
            listData[i] = GetCirclePosition(startAngle, centerPosition, r);
        }
        if (isLoop)
        {
            listData[number] = listData[0];
        }
        return listData;
    }


    /// <summary>
    /// 获取绕某点旋转之后的点
    /// </summary>
    /// <param name="centerPosition">中心点</param>
    /// <param name="position">旋转点</param>
    /// <param name="angles">角度</param>
    /// <returns></returns>
    public static Vector3 GetRotatedPosition(Vector3 centerPosition, Vector3 position, Vector3 angles)
    {
        Vector3 direction = position - centerPosition;
        Vector3 rotatedDirection = Quaternion.Euler(angles) * direction;
        return rotatedDirection + centerPosition;
    }

    public static Vector3[] GetRotatedPosition(Vector3 centerPosition, Vector3[] positionArray, Vector3 angles)
    {
        Vector3[] newArray = new Vector3[positionArray.Length];
        for (int i = 0; i < positionArray.Length; i++)
        {
            newArray[i] = GetRotatedPosition(centerPosition, positionArray[i], angles);
        }
        return newArray;
    }

    /// <summary>
    /// 获取某向量的垂直向量
    /// </summary>
    public static Vector3 GetVerticalDir2D(Vector3 _dir)
    {
        ///垂线 = b(y,-x) 
        ///垂线2 = b(-y, x)
        return new Vector3(_dir.y, -_dir.x, 0);
        //return new Vector3(_dir.y / _dir.x, -1);
        //return new Vector3(-_dir.y / _dir.x, 1);
    }

    public static Vector2 GetVerticalDir2D(Vector2 _dir)
    {
        return new Vector2(_dir.y, -_dir.x);
    }
}