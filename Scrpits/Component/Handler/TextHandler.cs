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
    /// 通过ID获取文本
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public string GetTextById(long id)
    {
        return manager.GetTextById(id).Replace(" ", noBreakingSpace);
    }

}