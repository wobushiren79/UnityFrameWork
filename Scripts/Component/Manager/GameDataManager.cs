using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public partial class GameDataManager : BaseManager, IGameConfigView, IModIdMapView
{
    //游戏设置
    public GameConfigBean gameConfig;
    public GameConfigController controllerForGameConfig;

    //ModID映射
    public ModIdMapBean modIdMapBean;
    public ModIdMapController controllerForModIdMap;

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

    /// <summary>
    /// 获取ModID映射
    /// </summary>
    public ModIdMapBean GetModIdMap()
    {
        if (modIdMapBean == null)
            modIdMapBean = controllerForModIdMap.GetModIdMapData();
        if (modIdMapBean == null)
            modIdMapBean = new ModIdMapBean();
        return modIdMapBean;
    }

    /// <summary>
    /// 保存ModID映射
    /// </summary>
    public void SaveModIdMap()
    {
        if (modIdMapBean != null)
            controllerForModIdMap.SaveModIdMapData(modIdMapBean);
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

    public void GetModIdMapFail()
    {

    }

    public void GetModIdMapSuccess(ModIdMapBean bean)
    {
        modIdMapBean = bean;
    }

    public void SetModIdMapFail()
    {

    }

    public void SetModIdMapSuccess(ModIdMapBean bean)
    {

    }
    #endregion
}