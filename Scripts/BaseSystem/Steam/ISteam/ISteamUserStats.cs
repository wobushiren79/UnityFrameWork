using UnityEngine;
using UnityEditor;

public interface ISteamUserStats
{

    /// <summary>
    /// 改变统计数字
    /// </summary>
    /// <param name="apiName"></param>
    /// <param name="changeNumber"></param>
    void UserCompleteNumberChange(string apiName,int changeNumber);

    /// <summary>
    ///  改变统计数字
    /// </summary>
    /// <param name="apiName"></param>
    /// <param name="changeNumber"></param>
    void UserCompleteNumberChange(string apiName, float changeNumber);

    /// <summary>
    /// 清楚所有统计数据- 清空所有成就
    /// </summary>
    void ResetAllStats();
}