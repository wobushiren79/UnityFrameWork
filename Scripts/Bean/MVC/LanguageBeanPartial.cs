using System;
using System.Collections.Generic;

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
