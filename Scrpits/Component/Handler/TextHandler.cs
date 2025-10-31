using System.Collections;
using UnityEngine;

public partial class TextHandler : BaseHandler<TextHandler, TextManager>
{
    //空格不换行
    public string noBreakingSpace = "\u00A0";

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        ChangeLanguageEnum(gameConfig.GetLanguage());
    }

    /// <summary>
    /// 改变多语言
    /// </summary>
    /// <param name="language"></param>
    public void ChangeLanguageEnum(LanguageEnum language)
    {
        LanguageCfg.ChangeLanguageData(language);
    }

    /// <summary>
    /// 通过ID获取文本-UIText
    /// </summary>
    public string GetTextById(long id)
    {
        return manager.GetTextById(UITextCfg.fileName, id);
    }
    
    /// <summary>
    /// 通过ID获取文本
    /// </summary>
    public string GetTextById(string cfgName, long id)
    {
        return manager.GetTextById(cfgName,id);
    }

    /// <summary>
    /// 通过ID获取文本
    /// </summary>
    public string GetTextByIdNoBreakingSpace(string cfgName, long id)
    {
        return manager.GetTextById(cfgName,id).Replace(" ",noBreakingSpace);
    }
}