using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public interface ISteamLeaderboard
{
    /// <summary>
    /// 创建排行榜
    /// </summary>
    /// <param name="leaderboardName"></param>
    /// <param name="sortMethod"></param>
    /// <param name="displayType"></param>
    void FindOrCreateLeaderboard(string leaderboardName, ELeaderboardSortMethod sortMethod, ELeaderboardDisplayType displayType, ISteamLeaderboardCreateCallBack callBack);

    /// <summary>
    /// 查询排行榜
    /// </summary>
    /// <param name="leaderboardName"></param>
    void FindLeaderboard(string leaderboardName, ISteamLeaderboardFindCallBack callBack);

    /// <summary>
    /// 查询全球指定排名排行榜数据
    /// </summary>
    /// <param name="leaderboardId"></param>
    /// <param name="startRange"></param>
    /// <param name="endRange"></param>
    /// <param name="type"></param>
    /// <param name="callBack"></param>
    void FindLeaderboardEntries(ulong leaderboardId, int startRange, int endRange, ELeaderboardDataRequest type, ISteamLeaderboardEntriesCallBack callBack);

    /// <summary>
    /// 查询指定用户组排行榜数据
    /// </summary>
    /// <param name="leaderboardId"></param>
    /// <param name="userList"></param>
    /// <param name="callBack"></param>
    void FindLeaderboardEntriesForUserList(ulong leaderboardId, CSteamID[] userList, ISteamLeaderboardEntriesCallBack callBack);

    /// <summary>
    /// 更新用户排行榜分数
    /// </summary>
    /// <param name="leaderboardId"></param>
    /// <param name="score"></param>
    /// <param name="callBack"></param>
    void UpdateLeaderboardScore(ulong leaderboardId, int score,ISteamLeaderboardUpdateCallBack callBack);
}

