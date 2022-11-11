using UnityEngine;
using UnityEditor;

public interface ISteamWorkshopQuery 
{
    /// <summary>
    /// 查询本地安装创意工坊物品
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="callBack"></param>
    void QueryInstallInfo(uint pageNumber, ISteamWorkshopQueryInstallInfoCallBack callBack);
}