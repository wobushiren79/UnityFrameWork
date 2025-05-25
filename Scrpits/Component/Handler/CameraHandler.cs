

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
}
