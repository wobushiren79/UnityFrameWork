using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class GameDataManager : BaseManager,
    IGameConfigView, IChunkSaveView, IUserDataView, IBaseDataView,IBiomeSaveView
{
    //游戏设置
    public GameConfigBean gameConfig;
    public UserDataBean userData;

    public GameConfigController controllerForGameConfig;
    public ChunkSaveController controllerForChunkSave;
    public UserDataController controllerForUserData;
    public BaseDataController controllerForBase;
    public BiomeSaveController controllerForBiomeSave;

    //保存的生态信息
    public Dictionary<WorldTypeEnum, BiomeSaveBean> dicBiomeSaveData = new Dictionary<WorldTypeEnum, BiomeSaveBean>();

    protected static object lockForSaveData = new object();

    protected void Awake()
    {
        controllerForGameConfig = new GameConfigController(this, this);
        controllerForChunkSave = new ChunkSaveController(this, this);
        controllerForUserData = new UserDataController(this, this);
        controllerForBase = new BaseDataController(this, this);
        controllerForBiomeSave = new BiomeSaveController(this,this);
        controllerForGameConfig.GetGameConfigData();
    }

    /// <summary>
    /// 获取所有的用户数据
    /// </summary>
    /// <returns></returns>
    public void GetAllUserData(Action<List<UserDataBean>> action)
    {
        controllerForUserData.GetAllUserDataData(action);
    }

    /// <summary>
    /// 获取游戏数据
    /// </summary>
    /// <returns></returns>
    public UserDataBean GetUserData()
    {
        if (userData == null)
        {
            //如果没有数据 则使用测试数据
            userData = new UserDataBean();
            userData.timeForGame.hour = 7;
            userData.userId = "Test";
            userData.seed = 132349;
        }
        return userData;
    }

    /// <summary>
    /// 使用该数据
    /// </summary>
    public void UseUserData(UserDataBean userData)
    {
        //需要先清除之前账号的数据
        dicBiomeSaveData.Clear();

        this.userData = userData;
    }

    /// <summary>
    /// 获取世界数据
    /// </summary>
    /// <returns></returns>
    public ChunkSaveBean GetChunkSaveData(string userId, WorldTypeEnum worldType, Vector3Int position)
    {
        return controllerForChunkSave.GetChunkSaveData(userId, worldType, position, null);
    }

    /// <summary>
    /// 获取保存的生态信息
    /// </summary>
    public BiomeSaveBean GetBiomeSaveData(string userId, WorldTypeEnum worldType)
    {
        if(dicBiomeSaveData.TryGetValue(worldType,out BiomeSaveBean biomeSaveData))
        {
            return biomeSaveData;
        }
        else
        {
            biomeSaveData = controllerForBiomeSave.GetBiomeSaveData(userId, worldType, null);
            dicBiomeSaveData.Add(worldType, biomeSaveData);
            return biomeSaveData;
        }
    }

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
    /// 保存生态数据
    /// </summary>
    public void SaveBiomeData()
    {
        foreach (var itemData in dicBiomeSaveData)
        {
            controllerForBiomeSave.SetBiomeSaveData(itemData.Value,null);
        }
    }

    /// <summary>
    /// 保存游戏设置
    /// </summary>
    public void SaveGameConfig()
    {
        controllerForGameConfig.SaveGameConfigData(gameConfig);
    }

    /// <summary>
    /// 异步保存游戏数据
    /// </summary>
    public async void SaveGameDataAsync(ChunkSaveBean chunkSaveData)
    {
        Player player = GameHandler.Instance.manager.player;
        Vector3 playerPosition = player.transform.position;
        WorldTypeEnum worldType = WorldCreateHandler.Instance.manager.worldType;

        await Task.Run(() =>
        {
            lock (lockForSaveData)
            {
                //保存用户数据
                SaveUserData(worldType, playerPosition);
                //保存区块数据
                chunkSaveData.SaveData();
                controllerForChunkSave.SetChunkSaveData(chunkSaveData, null);
            }
        });
    }

    /// <summary>
    /// 保存用户数据
    /// </summary>
    /// <param name="userData"></param>
    public void SaveUserData(UserDataBean userData)
    {
        controllerForUserData.SetUserData(userData);
    }

    public void SaveUserData(WorldTypeEnum worldType, Vector3 playerPosition)
    {
        userData.userExitPosition.SetWorldType(worldType);
        userData.userExitPosition.SetPosition(playerPosition);
        SaveUserData(userData);
    }

    public void SaveUserData()
    {
        Player player = GameHandler.Instance.manager.player;
        Vector3 playerPosition = player.transform.position;
        WorldTypeEnum worldType = WorldCreateHandler.Instance.manager.worldType;
        SaveUserData(worldType, playerPosition);
    }

    /// <summary>
    /// 删除游戏数据
    /// </summary>
    /// <param name="userId"></param>
    public void DeletGameData(string userId)
    {
        controllerForUserData.RemoveUserData(userId);
    }

    public void DeletGameData()
    {
        DeletGameData(userData.userId);
    }

    #region 游戏设置数据回掉
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

    public void GetUserDataSuccess<T>(T data, Action<T> action)
    {
        action?.Invoke(data);
    }

    public void GetUserDataFail(string failMsg, Action action)
    {

    }

    public void GetAllBaseDataSuccess(List<BaseDataBean> listData)
    {

    }

    public void GetAllBaseDataFail(string failMsg)
    {

    }

    public void GetChunkSaveSuccess<T>(T data, Action<T> action)
    {
        action?.Invoke(data);
    }

    public void GetChunkSaveFail(string failMsg, Action action)
    {

    }

    public void GetBiomeSaveSuccess<T>(T data, Action<T> action)
    {
        action?.Invoke(data);
    }

    public void GetBiomeSaveFail(string failMsg, Action action)
    {

    }
    #endregion
}