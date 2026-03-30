using UnityEngine;
using UnityEditor;

public interface ISteamLeaderboardUpdateCallBack 
{
    /// <summary>
    /// 更新排行榜数据成功
    /// </summary>
    void UpdateLeaderboardSucess();

    /// <summary>
    /// 更新排行榜数据失败
    /// </summary>
    /// <param name="msg"></param>
    void UpdateLeaderboardFail(SteamLeaderboardImpl.SteamLeaderboardFailEnum msg);
}