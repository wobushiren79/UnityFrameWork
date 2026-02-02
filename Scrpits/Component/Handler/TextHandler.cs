using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        return manager.GetTextById(cfgName, id);
    }

    /// <summary>
    /// 通过ID获取文本
    /// </summary>
    public string GetTextByIdNoBreakingSpace(string cfgName, long id)
    {
        return manager.GetTextById(cfgName, id).Replace(" ", noBreakingSpace);
    }

    /// <summary>
    /// 获取替换文本
    /// </summary>
    public string GetTextReplace(long id, Dictionary<TextReplaceEnum, string> dicReplace)
    {
        string originText = manager.GetTextById(UITextCfg.fileName, id);
        return GetTextReplace(originText, dicReplace);
    }

    /// <summary>
    /// 获取替换文本
    /// </summary>
    public string GetTextReplace(string originText, Dictionary<TextReplaceEnum, string> dicReplace)
    {
        if (string.IsNullOrEmpty(originText) || dicReplace == null || dicReplace.Count == 0)
            return originText;

        // 预编译占位符格式
        var result = new StringBuilder(originText);

        foreach (var kvp in dicReplace)
        {
            // 使用nameof避免ToString()调用
            string placeholder = $"{{{kvp.Key}}}";
            result.Replace(placeholder, kvp.Value ?? string.Empty);
        }

        return result.ToString();
    }
}