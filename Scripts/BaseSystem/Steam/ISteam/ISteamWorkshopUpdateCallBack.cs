using UnityEngine;
using UnityEditor;
using Steamworks;

public interface ISteamWorkshopUpdateCallBack
{
    /// <summary>
    /// 上传数据成功
    /// </summary>
    void UpdateSuccess();

    /// <summary>
    /// 上传数据失败
    /// </summary>
    void UpdateFail(SteamWorkshopUpdateImpl.SteamWorkshopUpdateFailEnum failEnum);

    /// <summary>
    /// 上传进度
    /// </summary>
    /// <param name="status"></param>
    /// <param name="progressBytes"></param>
    /// <param name="totalBytes"></param>
    void UpdateProgress(EItemUpdateStatus status,ulong progressBytes,ulong totalBytes);
}