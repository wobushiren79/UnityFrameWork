using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AIBaseCommon
{

    /// <summary>
    /// 视线搜索-圆形范围
    /// </summary>
    /// <param name="sourcePosition">眼睛位置</param>
    /// <param name="searchRadius">搜索半径</param>
    /// <param name="layerSearchTarget">搜索物体的层级</param>
    /// <param name="layerObstacles">遮挡物体层级</param>
    public static List<Collider> SightSearchCircle(Vector3 sourcePosition, float searchRadius, int layerSearchTarget, int layerObstacles)
    {
        List<Collider> listData = new List<Collider>();
        //搜索圆形范围内的目标物体
        Collider[] searchTarget = RayUtil.OverlapToSphere(sourcePosition, searchRadius, layerSearchTarget);
        if (searchTarget.IsNull())
            return listData;

        for (int i = 0; i < searchTarget.Length; i++)
        {
            Collider itemTargetCollider = searchTarget[i];
            //修正一下目标位置
            Vector3 fixTargetPosition = itemTargetCollider.transform.position + new Vector3(0, 0.5f, 0);
            //获取目标距离
            float disTarget = Vector3.Distance(sourcePosition, fixTargetPosition);
            //发射一条射线 检测是否有视野遮挡（距离-0.5 防止射到地面）
            bool hasObstacles = RayUtil.CheckToCast(sourcePosition, fixTargetPosition - sourcePosition, disTarget - 0.5f, layerObstacles);
            if (!hasObstacles)
            {
                listData.Add(itemTargetCollider);
            }
        }
        return listData;
    }
}