using UnityEngine;
using UnityEditor;

public interface ISteamLeaderboardFindCallBack
{
    /// <summary>
    /// 查找排行榜成功
    /// </summary>
    /// <param name="leaderboard"></param>
    void FindLeaderboardSuccess(ulong leaderboard);

    /// <summary>
    /// 查询排行榜失败
    /// </summary>
    /// <param name="msg"></param>
    void FindLeaderboardFail(SteamLeaderboardImpl.SteamLeaderboardFailEnum msg);
}