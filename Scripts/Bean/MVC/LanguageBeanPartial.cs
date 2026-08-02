using System;
using System.Collections.Generic;
using Steamworks;

public partial class LanguageBean
{
}

/// <summary>
/// 多语言缓存结构体，支持语言切换时自动失效
/// </summary>
public struct LanguageCache
{
    private string _value;
    private int _version;

    public string Get(Func<string> getter)
    {
        if (_version != LanguageCfg.LanguageVersion)
        {
            _value = null;
            _version = LanguageCfg.LanguageVersion;
        }
        if (_value == null)
            _value = getter();
        return _value;
    }

    public void Set(string value)
    {
        _value = value;
        _version = LanguageCfg.LanguageVersion;
    }
}

public partial class LanguageCfg
{
    /// <summary>
    /// 当前语言版本号，切换语言时递增
    /// </summary>
    public static int LanguageVersion { get; private set; }

    #region 静态构造
    /// <summary>
    /// 静态构造：在首次访问 LanguageCfg 时为 currentLanguage 设定默认值
    /// 字段初始化器先把 currentLanguage 设为空串，这里随后覆盖为基于 Steam 检测的默认语言
    /// </summary>
    static LanguageCfg()
    {
        currentLanguage = GetInitialLanguage();
    }
    #endregion

    #region 默认语言判定
    /// <summary>
    /// 获取初始语言：连上 Steam 时根据 Steam 客户端语言初始化，未连上则默认 en
    /// 映射：schinese→cn、tchinese→tw、japanese→jp、koreana→kr、german→de、french→fr、russian→ru，其余归为 en
    /// </summary>
    public static string GetInitialLanguage()
    {
        try
        {
            if (SteamManager.Initialized)
            {
                string steamLanguage = SteamApps.GetCurrentGameLanguage();
                if (!string.IsNullOrEmpty(steamLanguage))
                {
                    if (steamLanguage.Equals("schinese", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.cn.GetEnumName();
                    if (steamLanguage.Equals("tchinese", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.tw.GetEnumName();
                    if (steamLanguage.IndexOf("chinese", StringComparison.OrdinalIgnoreCase) >= 0)
                        return LanguageEnum.cn.GetEnumName();
                    if (steamLanguage.Equals("japanese", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.jp.GetEnumName();
                    if (steamLanguage.Equals("koreana", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.kr.GetEnumName();
                    if (steamLanguage.Equals("german", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.de.GetEnumName();
                    if (steamLanguage.Equals("french", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.fr.GetEnumName();
                    if (steamLanguage.Equals("russian", StringComparison.OrdinalIgnoreCase))
                        return LanguageEnum.ru.GetEnumName();
                    return LanguageEnum.en.GetEnumName();
                }
            }
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"读取 Steam 语言失败，回退到默认语言 en：{ex.Message}");
        }
        return LanguageEnum.en.GetEnumName();
    }
    #endregion

    public static void ChangeLanguageData(LanguageEnum languageType)
    {
        ChangeLanguageData(languageType.GetEnumName());
    }

    public static void ChangeLanguageData(string languageType)
    {
        if (currentLanguage != languageType)
        {
            currentLanguage = languageType;
            LanguageVersion++;
            dicData?.Clear();
        }
    }
}
