using UnityEngine;
using UnityEditor;

public class SteamUserStatsHandle : ScriptableObject
{
    #region 用户统计

    /// <summary>
    /// 显式请求当前用户的统计信息。Steam 客户端会在游戏启动前自动同步本地统计。
    /// </summary>
    public static void UserStatsInit()
    {
        if (SteamManager.Initialized)
        {
            SteamUserStatsImpl.InitUserStats();
        }
    }

    /// <summary>
    /// 修改用户统计数据
    /// </summary>
    /// <param name="apiName"></param>
    /// <param name="data"></param>
    public static void UserStatsDataUpdate(string apiName, int data)
    {
        if (SteamManager.Initialized)
        {
            SteamUserStatsImpl statsImpl = new SteamUserStatsImpl();
            statsImpl.UserCompleteNumberChange(apiName, data);
        }
    }

    /// <summary>
    /// 修改数据统计数据
    /// </summary>
    /// <param name="apiName"></param>
    /// <param name="data"></param>
    public static void UserStatsDataUpdate(string apiName, float data)
    {
        if (SteamManager.Initialized)
        {
            SteamUserStatsImpl statsImpl = new SteamUserStatsImpl();
            statsImpl.UserCompleteNumberChange(apiName, data);
        }
    }

    #endregion
}