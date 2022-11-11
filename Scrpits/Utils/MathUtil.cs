
using System;
using UnityEngine;

public class MathUtil
{

    /// <summary>
    /// 获取唯一下标
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static int GetSingleIndexForTwo(int a, int b, int size)
    {
        return a * size + b;
    }


    public static int GetSingleIndexForThree(Vector3Int data, int size1, int size2)
    {
        return GetSingleIndexForThree(data.x, data.y, data.z, size1, size2);
    }

    public static int GetSingleIndexForThree(int a, int b, int c, int size1, int size2)
    {
        return a * size1 * size2 + b * size1 + c;
    }

    /// <summary>
    /// 获取个位
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static int GetUnit(int data)
    {
        return data % 10;
    }

    /// <summary>
    /// 获取数字的十位
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static int GetUnitTen(int data)
    {
        return (data % 100) / 10;
    }

    /// <summary>
    /// 获取数字的百位
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static int GetUnitHundred(int data)
    {
        return (data % 1000) / 100;
    }

    /// <summary>
    /// 获取贝塞尔曲线上一点
    /// </summary>
    /// <param name="t">0到1的值，0获取曲线的起点，1获得曲线的终点</param>
    /// <param name="start">曲线的起始位置</param>
    /// <param name="center">决定曲线形状的控制点</param>
    /// <param name="end">曲线的终点</param>
    /// <returns></returns>
    public static Vector3 GetBezierPoint(float t, Vector3 start, Vector3 center, Vector3 end)
    {
        return (1 - t) * (1 - t) * start + 2 * t * (1 - t) * center + t * t * end;
    }

    /// <summary>
    /// 获取贝塞尔曲线
    /// </summary>
    /// <returns></returns>
    public static Vector3[] GetBezierPoints(int number, Vector3 start, Vector3 center, Vector3 end)
    {
        Vector3[] pointArray = new Vector3[number];
        for (int i = 0; i < number; i++)
        {
            float pro = (i + 0.5f) / number;
            GetBezierPoint(pro, start, center, end);
        }
        return pointArray;
    }

    /// <summary>
    /// 获取贝塞尔曲线
    /// </summary>
    /// <returns></returns>
    public static Vector3[] GetBezierPoints(int number, Vector3 start, Vector3 end, float hight)
    {
        Vector3 centerPoint = ((end - start) / 2) + new Vector3(0, hight, 0);
        return GetBezierPoints(number, start, end, centerPoint);
    }

    /// <summary>
    /// 获取六边形位置
    /// </summary>
    /// <param name="x">下标记数</param>
    /// <param name="y">下标记数</param>
    /// <param name="originPosition">起始点</param>
    /// <param name="sideLength">边长</param>
    /// <param name="sideHeight">高，如果是等边 则为(Mathf.Sqrt(3) / 2f) * sideLength;</param>
    /// <returns></returns>
    public static Vector3 GetHexagonWorldPos(int x, int y, Vector3 originPosition, float sideLength, float sideHeight)
    {
        float wx = x * (sideLength * 1.5f);
        float wz = (x % 2) * sideHeight + y * sideHeight * 2;
        return originPosition + new Vector3(wx, 0, wz);
    }

    /// <summary>
    /// 获取六边形下标计数
    /// </summary>
    /// <param name="worldX"></param>
    /// <param name="worldY"></param>
    /// <param name="originPosition"></param>
    /// <param name="sideLength"></param>
    /// <param name="sideHeight"></param>
    /// <returns></returns>
    public static Vector2Int GetHexagonIndex(float x, float y, Vector3 originPosition, float sideLength, float sideHeight)
    {
        float offsetPositionX = x - originPosition.x;
        float offsetPositionY = y - originPosition.y;
        int indexX;
        int indexY;
        int addIndexX;
        int addIndexY;
        if (offsetPositionX >= 0)
        {
            indexX = Mathf.FloorToInt(offsetPositionX / (sideLength * 1.5f));
            addIndexX = 1;
        }
        else
        {
            indexX = Mathf.CeilToInt(offsetPositionX / (sideLength * 1.5f));
            addIndexX = -1;
        }
        if (offsetPositionY >= 0)
        {
            indexY = Mathf.FloorToInt(offsetPositionY / (sideHeight * 2));
            addIndexY = 1;
        }
        else
        {
            indexY = Mathf.CeilToInt(offsetPositionY / (sideHeight * 2));
            addIndexY = -1;
        }


        //获取上下左右4个区块的位置
        Vector3 worldPos1 = GetHexagonWorldPos(indexX, indexY, originPosition, sideLength, sideHeight);
        Vector3 worldPos2 = GetHexagonWorldPos(indexX + addIndexX, indexY, originPosition, sideLength, sideHeight);
        Vector3 worldPos3 = GetHexagonWorldPos(indexX, indexY + addIndexY, originPosition, sideLength, sideHeight);
        Vector3 worldPos4 = GetHexagonWorldPos(indexX + addIndexX, indexY + addIndexY, originPosition, sideLength, sideHeight);

        Vector3 targetPos = Vector3.zero;
        float disMin = float.MaxValue;
        float disPos1 = Vector3.Distance(worldPos1, new Vector3(x, 0, y));
        if (disPos1 < disMin)
        {
            disMin = disPos1;
            targetPos = worldPos1;
        }
        float disPos2 = Vector3.Distance(worldPos2, new Vector3(x, 0, y));
        if (disPos2 < disMin)
        {
            disMin = disPos2;
            targetPos = worldPos2;
        }
        float disPos3 = Vector3.Distance(worldPos3, new Vector3(x, 0, y));
        if (disPos3 < disMin)
        {
            disMin = disPos3;
            targetPos = worldPos3;
        }
        float disPos4 = Vector3.Distance(worldPos4, new Vector3(x, 0, y));
        if (disPos4 < disMin)
        {
            disMin = disPos4;
            targetPos = worldPos4;
        }

        var tx = (targetPos.x - originPosition.x) / (sideLength * 1.5f);
        int cx = Mathf.RoundToInt(tx);
        var ty = ((targetPos.z - originPosition.z) / sideHeight - cx % 2) / 2f;
        int cy = Mathf.RoundToInt(ty);
        return new Vector2Int(cx, cy);
    }
}