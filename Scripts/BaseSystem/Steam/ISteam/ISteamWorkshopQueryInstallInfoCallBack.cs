using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public interface ISteamWorkshopQueryInstallInfoCallBack 
{
    /// <summary>
    /// 获取本地安装创意工坊物品信息 成功
    /// </summary>
    /// <param name="listData"></param>
    void GetInstallInfoSuccess(List<SteamWorkshopQueryInstallInfoBean> listData);

    /// <summary>
    /// 获取本地安装创意工坊物品信息 失败
    /// </summary>
    /// <param name="failEnum"></param>
    void GetInstallInfoFail(SteamWorkshopQueryImpl.SteamWorkshopQueryFailEnum failEnum);
}