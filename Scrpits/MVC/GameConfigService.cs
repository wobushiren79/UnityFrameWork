using UnityEngine;
using UnityEditor;

public class GameConfigService : BaseDataStorage
{
    protected readonly string saveFileName;

    public GameConfigService()
    {
        saveFileName = "GameConfig";
    }

    /// <summary>
    /// 查询游戏配置数据
    /// </summary>
    /// <returns></returns>
    public GameConfigBean QueryData()
    {
        return BaseLoadData<GameConfigBean>(saveFileName);
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="gameConfig"></param>
    public void UpdateData(GameConfigBean gameConfig)
    {
        BaseSaveData(saveFileName, gameConfig);
    }
}