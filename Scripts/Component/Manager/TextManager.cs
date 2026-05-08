using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TextManager : BaseManager
{
    /// <summary>
    /// 根据ID获取文字内容
    /// </summary>
    /// <returns></returns>
    public string GetTextById(string cfgName, long id, int contentIndex = 0)
    {
        LanguageBean languageCfg = LanguageCfg.GetItemData(cfgName, id);
        if (languageCfg != null && !languageCfg.content.IsNull())
        {
            switch (contentIndex)
            {
                case 0:
                    return languageCfg.content;
                case 1:
                    return languageCfg.content_1;
                case 2:
                    return languageCfg.content_2;
                default:
                    return languageCfg.content;
            }
        }
        else
        {
            LogUtil.LogError($"没有找到类名为{cfgName} ID为{id}的文本");
            return $"Error:{id}";
        }
    }
}