using UnityEngine;
using UnityEditor;
using System;

[Serializable]
public class GameConfigBean
{
    //屏幕模式 0窗口  1全屏
    public int window = 0;
    //屏幕分辨率
    public string screenResolution = "1920x1080";
    //语言
    public string language = "cn";
    //音效大小
    public float soundVolume = 0.5f;
    //音乐大小
    public float musicVolume = 0.5f;
    //环境音乐大小
    public float environmentVolume = 0.5f;

    //自动保存时间
    public float autoSaveTime = 30;
    //UI大小
    public float uiSize = 1f;

    //帧数限制开启 1开启 0关闭
    public int stateForFrames = 1;
    public int frames = 120;
    //视野
    public int cameraFOV = 60;

    //是否展示帧数
    public bool framesShow = false;
    //阴影距离
    public float shadowDis = 50;
    //阴影质量等级
    public int shadowResolutionLevel = 3;

    //抗锯齿模式
    public int antialiasingMode = 0;
    //抗锯齿质量
    public int antialiasingQualityLevel = 0;

    //角色X轴镜头移动速度
    public float speedForPlayerCameraMoveX = 2f;
    //角色Y轴镜头移动速度
    public float speedForPlayerCameraMoveY = 2f;

    //世界刷新范围
    public int worldRefreshRange = 5;
    //世界删除范围
    public int worldDestoryRange = 5;

    /// <summary>
    /// 获取抗锯齿模式
    /// </summary>
    /// <returns></returns>
    public AntialiasingEnum GetAntialiasingMode()
    {
        return (AntialiasingEnum)antialiasingMode;
    }

    /// <summary>
    /// 设置抗锯齿模式
    /// </summary>
    /// <param name="antialiasing"></param>
    public void SetAntialiasingMode(AntialiasingEnum antialiasing)
    {
        antialiasingMode = (int)antialiasing;
    }

    /// <summary>
    /// 获取当前语言
    /// </summary>
    /// <returns></returns>
    public LanguageEnum GetLanguage()
    {
        return EnumExtension.GetEnum<LanguageEnum>(language);
    }

    /// <summary>
    /// 设置语言
    /// </summary>
    /// <param name="language"></param>
    public void SetLanguage(LanguageEnum language)
    {
        this.language = EnumExtension.GetEnumName(language);
    }

    /// <summary>
    /// 获取屏幕分辨率
    /// </summary>
    /// <param name="w"></param>
    /// <param name="h"></param>
    public void GetScreenResolution(out int w, out int h)
    {
        if (screenResolution.IsNull())
        {
            w = 0;
            h = 0;
        }
        else
        {
            int[] data = screenResolution.SplitForArrayInt('x');
            w = data[0];
            h = data[0];
        }
    }
}