using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public partial class GameDataManager : BaseManager, IGameConfigView
{
    //游戏设置
    public GameConfigBean gameConfig;
    public GameConfigController controllerForGameConfig;

    /// <summary>
    /// 获取游戏设置
    /// </summary>
    /// <returns></returns>
    public GameConfigBean GetGameConfig()
    {
        if (gameConfig == null)
            gameConfig = new GameConfigBean();
        return gameConfig;
    }

    /// <summary>
    /// 保存游戏设置
    /// </summary>
    public void SaveGameConfig()
    {
        controllerForGameConfig.SaveGameConfigData(gameConfig);
    }


    #region 回调
    public void GetGameConfigFail()
    {

    }

    public void GetGameConfigSuccess(GameConfigBean configBean)
    {
        gameConfig = configBean;
    }

    public void SetGameConfigFail()
    {

    }

    public void SetGameConfigSuccess(GameConfigBean configBean)
    {

    }
    #endregion
}