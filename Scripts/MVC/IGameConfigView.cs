
using UnityEngine;
using UnityEditor;

public interface IGameConfigView 
{
    /// <summary>
    /// 获取游戏设置数据成功
    /// </summary>
    /// <param name="configBean"></param>
    void GetGameConfigSuccess(GameConfigBean configBean);

    /// <summary>
    /// 获取游戏设置数据失败
    /// </summary>
    void GetGameConfigFail();

    /// <summary>
    /// 设置游戏数据成功
    /// </summary>
    /// <returns></returns>
    void SetGameConfigSuccess(GameConfigBean configBean);

    /// <summary>
    /// 设置游戏数据失败
    /// </summary>
    void SetGameConfigFail();
}