using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public partial class GameDataManager
{
    public GameConfigBean gameConfig;
    private BaseDataService<GameConfigBean> gameConfigService;

    public ModIdMapBean modIdMapBean;
    private BaseDataService<ModIdMapBean> modIdMapService;

    /// <summary>
    /// 获取游戏设置
    /// </summary>
    public GameConfigBean GetGameConfig()
    {
        if (gameConfig == null)
        {
            gameConfigService ??= new BaseDataService<GameConfigBean>("GameConfig")
            {
                JsonType = JsonTypeEnum.System
            };
            gameConfig = gameConfigService.Load();
            if (gameConfig == null)
                gameConfig = new GameConfigBean();
        }
        return gameConfig;
    }

    /// <summary>
    /// 保存游戏设置
    /// </summary>
    public void SaveGameConfig()
    {
        if (gameConfig != null)
        {
            gameConfigService ??= new BaseDataService<GameConfigBean>("GameConfig")
            {
                JsonType = JsonTypeEnum.System
            };
            gameConfigService.Save(gameConfig);
        }
    }

    /// <summary>
    /// 获取ModID映射
    /// </summary>
    public ModIdMapBean GetModIdMap()
    {
        if (modIdMapBean == null)
        {
            modIdMapService ??= new BaseDataService<ModIdMapBean>("ModIdMap");
            modIdMapBean = modIdMapService.Load();
            if (modIdMapBean == null)
                modIdMapBean = new ModIdMapBean();
        }
        return modIdMapBean;
    }

    /// <summary>
    /// 保存ModID映射
    /// </summary>
    public void SaveModIdMap()
    {
        if (modIdMapBean != null)
        {
            modIdMapService ??= new BaseDataService<ModIdMapBean>("ModIdMap");
            modIdMapService.Save(modIdMapBean);
        }
    }
}
