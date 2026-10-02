using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[Serializable]
public partial class GameConfigBean
{
    //屏幕模式 0窗口  1全屏
    public int window = 0;
    //屏幕分辨率
    public string screenResolution = "1920x1080";
    //语言（留空时由 LanguageCfg.GetInitialLanguage 判定：连上 Steam 则按 Steam 客户端语言，否则默认 en）
    public string language = "";
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
    //垂直同步（默认开启；开启时 QualitySettings.vSyncCount=1，会忽略 targetFrameRate）
    public bool vsync = true;
    //视野
    public int cameraFOV = 60;

    //是否展示帧数
    public bool framesShow = false;
    //是否显示按键提示(UIViewPressCommon 等快捷按键提示)，默认开启
    public bool pressKeyTipShow = true;
    //阴影距离
    public float shadowDis = 50;
    //阴影质量等级
    public int shadowResolutionLevel = 3;

    //抗锯齿模式
    public int antialiasingMode = 0;
    //抗锯齿质量
    public int antialiasingQualityLevel = 0;

    //已开启的Mod名列表（不在列表中的Mod视为关闭；新出现的Mod默认不在列表=默认关闭）
    public List<string> listModEnable = new List<string>();

    /// <summary>
    /// 判断指定Mod是否已开启
    /// </summary>
    public bool IsModEnable(string modName)
    {
        if (listModEnable == null || modName.IsNull())
            return false;
        return listModEnable.Contains(modName);
    }

    /// <summary>
    /// 设置指定Mod的开启状态（开启=加入列表，关闭=移出列表）
    /// </summary>
    public void SetModEnable(string modName, bool isEnable)
    {
        if (modName.IsNull())
            return;
        listModEnable ??= new List<string>();
        if (isEnable)
        {
            if (!listModEnable.Contains(modName))
                listModEnable.Add(modName);
        }
        else
        {
            listModEnable.Remove(modName);
        }
    }


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
    /// 未设置（空串）时按 LanguageCfg.GetInitialLanguage 推断：连上 Steam 用 Steam 客户端语言，否则 en
    /// </summary>
    /// <returns></returns>
    public LanguageEnum GetLanguage()
    {
        string lang = string.IsNullOrEmpty(language) ? LanguageCfg.GetInitialLanguage() : language;
        return EnumExtension.GetEnum<LanguageEnum>(lang);
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
            h = data[1];
        }
    }
}