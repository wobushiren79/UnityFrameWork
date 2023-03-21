using UnityEngine;
using UnityEditor;

public interface ISteamLeaderboardCreateCallBack 
{
    /// <summary>
    /// 创建排行榜成功
    /// </summary>
    /// <param name="leadboard"></param>
    void CreateSteamLeaderboardSuccess(ulong leaderboard);

    /// <summary>
    /// 创建排行榜失败
    /// </summary>
    /// <param name="msg"></param>
    void CreateSteamLeaderboardFail(SteamLeaderboardImpl.SteamLeaderboardFailEnum msg);
}