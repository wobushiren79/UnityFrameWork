

using Unity.Cinemachine;
using UnityEngine;

public partial class CameraHandler : BaseHandler<CameraHandler, CameraManager>
{
    /// <summary>
    /// 根据摄像头角度 改变物体角度
    /// </summary>
    /// <param name="target"></param>
    public void ChangeAngleForCamera(Transform target)
    {
        if (target != null)
        {
            //target.eulerAngles = CameraHandler.Instance.manager.mainCamera.transform.eulerAngles;
            target.eulerAngles = Vector3.zero;
        }
    }

    /// <summary>
    /// 获取跟随物体距离
    /// </summary>
    /// <param name="virtualCamera"></param>
    /// <returns></returns>
    public float GetDistanceFollow(CinemachineCamera cinemachineCamera)
    {
        if (cinemachineCamera == null)
        {
            LogUtil.LogError($" 获取跟随物体距离失败 virtualCamera为null");
            return 0;
        }
        // 获取 Transposer 组件
        CinemachineFollow cinemachineFollow = cinemachineCamera.GetComponent<CinemachineFollow>();
        Vector3 followOffset = cinemachineFollow.FollowOffset;
        float disFollow = Vector3.Distance(followOffset, Vector3.zero);
        return disFollow;
    }
}
