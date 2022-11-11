using UnityEngine;
using System.Collections;
using Steamworks;
using System.Threading;
using System.Collections.Generic;

public class SteamLeaderboardImpl : ISteamLeaderboard
{
    public enum SteamLeaderboardFailEnum
    {
        CREATE_FAIL,//创建失败
        FIND_FAIL,//查询排行榜失败
        GETLIST_FAIL,//获取数据失败
        UPDATE_FAIL,//更新数据失败
    }

    //创建排行榜回调
    private ISteamLeaderboardCreateCallBack mCreateCallBack;
    //查找排行榜回调
    private ISteamLeaderboardFindCallBack mFindCallBack;
    //查找排名数据回调
    private ISteamLeaderboardEntriesCallBack mEntriesCallBack;
    //更新排行榜数据回调
    private ISteamLeaderboardUpdateCallBack mUpdateCallBack;

    /// <summary>
    /// 创建排行榜
    /// </summary>
    /// <param name="leaderboardName">排行榜名字</param>
    /// <param name="sortMethod">排行榜排序方式 k_ELeaderboardSortMethodAscending：约小的数字约在前  k_ELeaderboardSortMethodDescending：约大的数字约在前</param>
    /// <param name="displayType"></param>
    public void FindOrCreateLeaderboard(string leaderboardName, ELeaderboardSortMethod sortMethod, ELeaderboardDisplayType displayType, ISteamLeaderboardCreateCallBack callBack)
    {
        this.mCreateCallBack = callBack;
        CallResult<LeaderboardFindResult_t> callResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardCreateResult);
        SteamAPICall_t handle = SteamUserStats.FindOrCreateLeaderboard(leaderboardName, sortMethod, displayType);
        callResult.Set(handle);
    }

    /// <summary>
    /// 根据排行榜姓名查询ID
    /// </summary>
    /// <param name="leaderboardName"></param>
    /// <param name="callBack"></param>
    public void FindLeaderboard(string leaderboardName, ISteamLeaderboardFindCallBack callBack)
    {
        this.mFindCallBack = callBack;
        CallResult<LeaderboardFindResult_t> callResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFindResult);
        SteamAPICall_t handle = SteamUserStats.FindLeaderboard(leaderboardName);
        callResult.Set(handle);
    }

    /// <summary>
    /// 查询排行榜数据-全球
    /// </summary>
    /// <param name="leaderboardId"></param>
    /// <param name="startRange"></param>
    /// <param name="endRange"></param>
    /// <param name="type"></param>
    /// <param name="callBack"></param>
    public void FindLeaderboardEntries(ulong leaderboardId, int startRange, int endRange, ELeaderboardDataRequest type,ISteamLeaderboardEntriesCallBack callBack)
    {

        this.mEntriesCallBack = callBack;
        SteamLeaderboard_t tempBean = new SteamLeaderboard_t
        {
            m_SteamLeaderboard = leaderboardId
        };
        CallResult<LeaderboardScoresDownloaded_t> callResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnLeaderboardScoresDownloaded);
        SteamAPICall_t handle = SteamUserStats.DownloadLeaderboardEntries(tempBean, type, startRange, endRange);
        //TODO  必须要延迟才能设置回调
        //Thread.Sleep(1000);
        callResult.Set(handle);
    }
    
    /// <summary>
    /// 查询指定用户组所在的全球排名数据
    /// </summary>
    /// <param name="leaderboardId"></param>
    /// <param name="userList"></param>
    /// <param name="callBack"></param>
    public void FindLeaderboardEntriesForUserList(ulong leaderboardId, CSteamID[] userList, ISteamLeaderboardEntriesCallBack callBack)
    {
        this.mEntriesCallBack = callBack;
        SteamLeaderboard_t tempBean = new SteamLeaderboard_t
        {
            m_SteamLeaderboard = leaderboardId
        };
        CallResult<LeaderboardScoresDownloaded_t> callResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnLeaderboardScoresDownloadedForUserList);
        SteamAPICall_t handle = SteamUserStats.DownloadLeaderboardEntriesForUsers(tempBean, userList, userList.Length);
        //TODO  必须要延迟才能设置回调
        //Thread.Sleep(1000);
        callResult.Set(handle);
    }

    /// <summary>
    /// 根据排行榜ID更新排行榜该用户的分数
    /// </summary>
    /// <param name="leaderboardId"></param>
    /// <param name="score"></param>
    public void UpdateLeaderboardScore(ulong leaderboardId, int score, ISteamLeaderboardUpdateCallBack callBack)
    {
        UpdateLeaderboardScore(leaderboardId, score, null, 0, callBack);
    }
    public void UpdateLeaderboardScore(ulong leaderboardId, int score,int[] scoreDetails,int scoreDetailsMax, ISteamLeaderboardUpdateCallBack callBack)
    {
        this.mUpdateCallBack = callBack;
        SteamLeaderboard_t m_SteamLeaderboard = new SteamLeaderboard_t
        {
            m_SteamLeaderboard = leaderboardId
        };
        CallResult<LeaderboardScoreUploaded_t> callResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnLeaderboardScoreUploaded);
        SteamAPICall_t handle = SteamUserStats.UploadLeaderboardScore(m_SteamLeaderboard, ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodForceUpdate, score, scoreDetails, scoreDetailsMax);
        callResult.Set(handle);
    }
    //-------------------------------------------------------------------------------------------------------
    //---------------------------------------------私有------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 创建排行榜回调
    /// </summary>
    /// <param name="itemResult"></param>
    /// <param name="bIOFailure"></param>
    private void OnLeaderboardCreateResult(LeaderboardFindResult_t itemResult, bool bIOFailure)
    {
        if (bIOFailure|| itemResult.m_bLeaderboardFound==0)
        {
            if (this.mCreateCallBack != null)
                this.mCreateCallBack.CreateSteamLeaderboardFail(SteamLeaderboardFailEnum.CREATE_FAIL);
            return;
        }
        if (this.mCreateCallBack != null)
            this.mCreateCallBack.CreateSteamLeaderboardSuccess(itemResult.m_hSteamLeaderboard.m_SteamLeaderboard);
    }

    /// <summary>
    /// 查询排行榜回调
    /// </summary>
    /// <param name="pCallback"></param>
    /// <param name="bIOFailure"></param>
    private void OnLeaderboardFindResult(LeaderboardFindResult_t itemResult, bool bIOFailure)
    {
        if (bIOFailure || itemResult.m_bLeaderboardFound == 0)
        {
            if(mFindCallBack!=null)
                mFindCallBack.FindLeaderboardFail(SteamLeaderboardFailEnum.FIND_FAIL);
            return;
        }
        if (mFindCallBack != null)
            mFindCallBack.FindLeaderboardSuccess(itemResult.m_hSteamLeaderboard.m_SteamLeaderboard);
    }

    /// <summary>
    /// 查询排行榜数据回调
    /// </summary>
    /// <param name="pCallback"></param>
    /// <param name="bIOFailure"></param>
    private void OnLeaderboardScoresDownloaded(LeaderboardScoresDownloaded_t itemResult, bool bIOFailure)
    {
        if (bIOFailure)
        {
            if (mEntriesCallBack != null)
                mEntriesCallBack.GetEntriesFail(SteamLeaderboardFailEnum.GETLIST_FAIL);
            return;
        }
        SteamLeaderboardEntries_t entriesData = itemResult.m_hSteamLeaderboardEntries;
   
        List<SteamLeaderboardEntryBean> listData = new List<SteamLeaderboardEntryBean>();
        for (int i = 0; i < itemResult.m_cEntryCount; i++)
        {
            LeaderboardEntry_t entry_T;
            int[] detailsInt = new int[64];
            SteamUserStats.GetDownloadedLeaderboardEntry(entriesData, i, out entry_T, detailsInt, 64);
            SteamLeaderboardEntryBean itemData = new SteamLeaderboardEntryBean
            {
                score = entry_T.m_nScore,
                rank = entry_T.m_nGlobalRank,
                steamID = entry_T.m_steamIDUser,
                details = detailsInt
            };
            listData.Add(itemData);
        }
        if (mEntriesCallBack != null)
            mEntriesCallBack.GetEntriesSuccess(itemResult.m_hSteamLeaderboard.m_SteamLeaderboard, listData);
    }
    private void OnLeaderboardScoresDownloadedForUserList(LeaderboardScoresDownloaded_t itemResult, bool bIOFailure)
    {
        if (bIOFailure)
        {
            if (mEntriesCallBack != null)
                mEntriesCallBack.GetEntriesForUserListFail(SteamLeaderboardFailEnum.GETLIST_FAIL);
            return;
        }
        SteamLeaderboardEntries_t entriesData = itemResult.m_hSteamLeaderboardEntries;

        List<SteamLeaderboardEntryBean> listData = new List<SteamLeaderboardEntryBean>();
        for (int i = 0; i < itemResult.m_cEntryCount; i++)
        {
            LeaderboardEntry_t entry_T;
            int[] detailsInt = new int[64];
            SteamUserStats.GetDownloadedLeaderboardEntry(entriesData, i, out entry_T, detailsInt, 64);
            SteamLeaderboardEntryBean itemData = new SteamLeaderboardEntryBean
            {
                score = entry_T.m_nScore,
                rank = entry_T.m_nGlobalRank,
                steamID = entry_T.m_steamIDUser,
                details = detailsInt
            };
            listData.Add(itemData);
        }
        if (mEntriesCallBack != null)
            mEntriesCallBack.GetEntriesForUserListSuccess(itemResult.m_hSteamLeaderboard.m_SteamLeaderboard, listData);
    }
    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="pCallback"></param>
    /// <param name="bIOFailure"></param>
    private void OnLeaderboardScoreUploaded(LeaderboardScoreUploaded_t pCallback, bool bIOFailure)
    {
        if (bIOFailure)
        {
            if (mUpdateCallBack != null)
                mUpdateCallBack.UpdateLeaderboardFail(SteamLeaderboardFailEnum.UPDATE_FAIL);
            return;
        }
        if (mUpdateCallBack != null)
            mUpdateCallBack.UpdateLeaderboardSucess();
    }

}

