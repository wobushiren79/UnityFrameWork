using UnityEngine;
using UnityEditor;

public class GameConfigController : BaseMVCController<GameConfigModel, IGameConfigView>
{
    public GameConfigController(BaseMonoBehaviour content, IGameConfigView view) : base(content, view)
    {
    }

    public override void InitData()
    {
       
    }

    /// <summary>
    /// 保存配置数据
    /// </summary>
    /// <param name="configBean"></param>
    /// <returns></returns>
    public void SaveGameConfigData(GameConfigBean configBean)
    {
        if (configBean == null) {
            GetView().SetGameConfigFail();
            return;
        } 
        GetModel().SetGameConfigData(configBean);
        GetView().SetGameConfigSuccess(configBean);
    }

    /// <summary>
    /// 获取游戏配置数据
    /// </summary>
    /// <returns></returns>
    public GameConfigBean GetGameConfigData()
    {
       GameConfigBean configBean= GetModel().GetGameConfigData();
        if (configBean == null) {
            GetView().GetGameConfigFail();
            return configBean;
        }
        GetView().GetGameConfigSuccess(configBean);
        return configBean;
    }
}