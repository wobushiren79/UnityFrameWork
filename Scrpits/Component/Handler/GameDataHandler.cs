using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class GameDataHandler : BaseHandler<GameDataHandler, GameDataManager>
{
    /// <summary>
    /// 获取基础信息
    /// </summary>
    /// <param name="baseInfoId"></param>
    /// <returns></returns>
    public string GetBaseInfoStr(long baseInfoId)
    {
        BaseInfoBean baseInfo = manager.controllerForBase.GetBaseData(baseInfoId);
        return baseInfo.content;
    }
    public int GetBaseInfoInt(long baseInfoId)
    {
        BaseInfoBean baseInfo = manager.controllerForBase.GetBaseData(baseInfoId);
        return int.Parse(baseInfo.content);
    }
    public long GetBaseInfoLong(long baseInfoId)
    {
        BaseInfoBean baseInfo = manager.controllerForBase.GetBaseData(baseInfoId);
        return long.Parse(baseInfo.content);
    }
    public float GetBaseInfoFloat(long baseInfoId)
    {
        BaseInfoBean baseInfo = manager.controllerForBase.GetBaseData(baseInfoId);
        return float.Parse(baseInfo.content);
    }
    public List<long> GetBaseInfoListLong(long baseInfoId)
    {
        string dataStr = GetBaseInfoStr(baseInfoId);
        long[] arrayData = dataStr.SplitForArrayLong(',');
        return arrayData.ToList();
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        GameConfigBean gameConfig = manager.GetGameConfig();
        //设置全屏
        Screen.fullScreen = gameConfig.window == 1 ? true : false;
        //环境参数初始化
        VolumeHandler.Instance.InitData();
        //设置FPS
        FPSHandler.Instance.SetData(gameConfig.stateForFrames, gameConfig.frames);
        //修改抗锯齿
        CameraHandler.Instance.ChangeAntialiasing(gameConfig.GetAntialiasingMode(),gameConfig.antialiasingQualityLevel);
    }
}