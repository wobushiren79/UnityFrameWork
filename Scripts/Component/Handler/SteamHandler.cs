using UnityEngine;
using UnityEditor;
using System.Collections;
using Steamworks;

public partial class SteamHandler : BaseHandler<SteamHandler, BaseManager>
{
    public SteamUserStatsImpl steamUserStats;
    public SteamLeaderboardImpl steamLeaderboard;
    public SteamWebImpl steamWeb;

    public SteamManager steamManager;
    public override void Awake()
    {
        base.Awake();
        steamManager = gameObject.AddComponent<SteamManager>();
    }

    public void Start()
    {
        SteamUserStatsImpl.InitUserStats();
    }


    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="steamId"></param>
    /// <param name="callBack"></param>
    /// <returns></returns>
    public IEnumerator GetUserInfo(string steamId, IWebRequestCallBack<SteamWebPlaySummariesBean> callBack)
    {
        if (steamWeb == null)
            steamWeb = new SteamWebImpl();
        yield return steamWeb.GetPlayerSummaries(steamId, callBack);
    }

    /// <summary>
    /// 解锁用户成就
    /// </summary>
    public void UnLockAchievement(long achId)
    {
        if (!SteamManager.Initialized)
            return;
        if (steamUserStats == null)
        {
            steamUserStats = new SteamUserStatsImpl();
            SteamUserStatsImpl.InitUserStats();
        }
        steamUserStats.UserCompleteAchievement(achId + "");
    }

    /// <summary>
    /// 设置排行榜数据
    /// </summary>
    /// <param name="leaderboardId"></param>
    /// <param name="score"></param>
    /// <param name="details"></param>
    /// <param name="callBack"></param>
    public void SetGetLeaderboardData(ulong leaderboardId, int score, string details, ISteamLeaderboardUpdateCallBack callBack)
    {
        if (!SteamManager.Initialized)
            return;
        if (steamLeaderboard == null)
        {
            steamLeaderboard = new SteamLeaderboardImpl();
        }
        if (details.Length > 64)
        {
            details = details.Substring(0, 64);
        }
        int[] intDetails = TypeConversionUtil.StringToInt32(details);
        int[] intDetailsData = new int[64];
        intDetails.CopyTo(intDetailsData, 0);
        steamLeaderboard.UpdateLeaderboardScore(leaderboardId, score, intDetailsData, 64, callBack);
    }

    /// <summary>
    /// 根据名字查询排行榜ID
    /// </summary>
    /// <param name="leaderboardName"></param>
    /// <param name="callBack"></param>
    public void GetLeaderboardId(string leaderboardName, ISteamLeaderboardFindCallBack callBack)
    {
        if (!SteamManager.Initialized)
            return;
        if (steamLeaderboard == null)
        {
            steamLeaderboard = new SteamLeaderboardImpl();
        }
        steamLeaderboard.FindLeaderboard(leaderboardName, callBack);
    }

    /// <summary>
    /// 查询全球排名
    /// </summary>
    /// <param name="leaderboardId"></param>
    /// <param name="startRank"></param>
    /// <param name="endRank"></param>
    public void GetLeaderboardDataForGlobal(ulong leaderboardId, int startRank, int endRank, ISteamLeaderboardEntriesCallBack callBack)
    {
        if (!SteamManager.Initialized)
            return;
        if (steamLeaderboard == null)
        {
            steamLeaderboard = new SteamLeaderboardImpl();
        }
        steamLeaderboard.FindLeaderboardEntries(leaderboardId, startRank, endRank, Steamworks.ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, callBack);
    }

    /// <summary>
    /// 查询用户排行榜
    /// </summary>
    /// <param name="leaderboardId"></param>
    /// <param name="userList"></param>
    /// <param name="callBack"></param>
    public void GetLeaderboardDataForUserList(ulong leaderboardId, CSteamID[] userList, ISteamLeaderboardEntriesCallBack callBack)
    {
        if (!SteamManager.Initialized)
            return;
        if (steamLeaderboard == null)
        {
            steamLeaderboard = new SteamLeaderboardImpl();
        }
        steamLeaderboard.FindLeaderboardEntriesForUserList(leaderboardId, userList, callBack);
    }

    public void GetLeaderboardDataForUser(ulong leaderboardId, ISteamLeaderboardEntriesCallBack callBack)
    {
        if (!SteamManager.Initialized)
            return;
        if (steamLeaderboard == null)
        {
            steamLeaderboard = new SteamLeaderboardImpl();
        }
        steamLeaderboard.FindLeaderboardEntriesForUserList(leaderboardId, new CSteamID[] { SteamUser.GetSteamID() }, callBack);
    }

    public void GetLeaderboardDataForUser(ulong leaderboardId, CSteamID userId, ISteamLeaderboardEntriesCallBack callBack)
    {
        if (!SteamManager.Initialized)
            return;
        if (steamLeaderboard == null)
        {
            steamLeaderboard = new SteamLeaderboardImpl();
        }
        steamLeaderboard.FindLeaderboardEntriesForUserList(leaderboardId, new CSteamID[] { userId }, callBack);
    }
}