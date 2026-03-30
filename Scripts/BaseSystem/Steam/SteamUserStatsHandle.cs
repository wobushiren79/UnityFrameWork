using UnityEngine;
using UnityEditor;

public class SteamUserStatsHandle : ScriptableObject
{
    /// <summary>
    /// 初始化用户数据 如要调用相关Stats方法 必须初始化  不过已在SteamManager里初始化过
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
}