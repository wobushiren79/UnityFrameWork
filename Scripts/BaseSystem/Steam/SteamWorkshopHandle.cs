using UnityEngine;
using UnityEditor;

public class SteamWorkshopHandle
{
    /// <summary>
    /// 创建创意工坊物品
    /// </summary>
    /// <param name="content"></param>
    /// <param name="updateBean"></param>
    /// <param name="callBack"></param>
    public static void CreateWorkshopItem(BaseMonoBehaviour content, SteamWorkshopUpdateBean updateBean, ISteamWorkshopUpdateCallBack callBack)
    {
        if (SteamManager.Initialized)
        {
            ISteamWorkshopUpdate update = new SteamWorkshopUpdateImpl(content);
            update.CreateWorkshopItem(updateBean, callBack);
        }
    }

    /// <summary>
    /// 查询创意工坊本地安装文件信息
    /// </summary>
    /// <param name="content"></param>
    /// <param name="pageNumber">页数 初始页为1 每页查询50条数据</param>
    /// <param name="callBack"></param>
    public static void QueryInstallInfo(BaseMonoBehaviour content, uint pageNumber, ISteamWorkshopQueryInstallInfoCallBack callBack)
    {
        if (SteamManager.Initialized)
        {
            ISteamWorkshopQuery query = new SteamWorkshopQueryImpl(content);
            query.QueryInstallInfo(pageNumber, callBack);
        }
    }
}