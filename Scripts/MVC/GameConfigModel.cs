using UnityEngine;
using UnityEditor;

public class GameConfigModel : BaseMVCModel
{
    protected GameConfigService serviceGameConfig;

    public override void InitData()
    {
        serviceGameConfig = new GameConfigService();
    }

    /// <summary>
    /// 获取游戏配置数据
    /// </summary>
    /// <returns></returns>
    public GameConfigBean GetGameConfigData()
    {
        GameConfigBean configBean = serviceGameConfig.QueryData();
        if (configBean == null)
            configBean = new GameConfigBean();
        return configBean;
    }

    /// <summary>
    /// 保存游戏配置数据
    /// </summary>
    /// <param name="data"></param>
    public void SetGameConfigData(GameConfigBean data)
    {
        serviceGameConfig.UpdateData(data);
    }
}