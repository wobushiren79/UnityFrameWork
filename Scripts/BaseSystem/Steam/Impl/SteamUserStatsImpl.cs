using UnityEngine;
using UnityEditor;
using Steamworks;

public class SteamUserStatsImpl : ISteamUserStats
{
    /// <summary>
    /// 初始化数据
    /// </summary>
    public static void InitUserStats()
    {
        CallResult<UserStatsReceived_t> call = CallResult<UserStatsReceived_t>.Create(OnUserStatsReceived);
        SteamAPICall_t steamAPICall_T = SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
        call.Set(steamAPICall_T);
    }

    /// <summary>
    /// 初始化回调
    /// </summary>
    /// <param name="pCallback"></param>
    /// <param name="bIOFailure"></param>
    [AOT.MonoPInvokeCallbackAttribute(typeof(UserStatsReceived_t))]
    public static void OnUserStatsReceived(UserStatsReceived_t pCallback, bool bIOFailure)
    {

    }


    /// <summary>
    /// 改变用户统计数据 继而完成成就
    /// </summary>
    /// <param name="apiName"></param>
    /// <param name="changeNumber"></param>
    public void UserCompleteNumberChange(string apiName, int changeNumber)
    {
        if (SteamManager.Initialized)
        {
            bool isSetStat = SteamUserStats.SetStat(apiName, changeNumber);
            bool isUpdateStat = SteamUserStats.StoreStats();
        }
    }

    /// <summary>
    ///  改变用户统计数据 继而完成成就
    /// </summary>
    /// <param name="apiName"></param>
    /// <param name="changeNumber"></param>
    public void UserCompleteNumberChange(string apiName, float changeNumber)
    {
        if (SteamManager.Initialized)
        {
            bool isSetStat = SteamUserStats.SetStat(apiName, changeNumber);
            bool isUpdateStat = SteamUserStats.StoreStats();
        }
    }

    /// <summary>
    /// 直接激活成就
    /// </summary>
    /// <param name="pchName"></param>
    public void UserCompleteAchievement(string pchName)
    {
        if (SteamManager.Initialized)
        {
            bool isSetAchievement = SteamUserStats.SetAchievement(pchName);
            bool isUpdateStat = SteamUserStats.StoreStats();
        }
    }


    /// <summary>
    /// 充值所有统计数据
    /// </summary>
    public void ResetAllStats()
    {
        if (SteamManager.Initialized)
        {
            bool isResetAll = SteamUserStats.ResetAllStats(true);
        }
    }
}