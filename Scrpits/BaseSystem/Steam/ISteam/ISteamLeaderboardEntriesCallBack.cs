using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public interface ISteamLeaderboardEntriesCallBack 
{
    /// <summary>
    /// 获取数据成功
    /// </summary>
    /// <param name="listData"></param>
    void GetEntriesSuccess(ulong leaderboardID, List<SteamLeaderboardEntryBean> listData);
    void GetEntriesForUserListSuccess(ulong leaderboardID, List<SteamLeaderboardEntryBean> listData);

    /// <summary>
    /// 获取数据失败
    /// </summary>
    /// <param name="msg"></param>
    void GetEntriesFail( SteamLeaderboardImpl.SteamLeaderboardFailEnum msg);
    void GetEntriesForUserListFail( SteamLeaderboardImpl.SteamLeaderboardFailEnum msg);
}