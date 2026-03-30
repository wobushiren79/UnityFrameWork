using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TextManager : BaseManager
{
    /// <summary>
    /// 根据ID获取文字内容
    /// </summary>
    /// <returns></returns>
    public string GetTextById(string cfgName, long id)
    {
        LanguageBean languageCfg = LanguageCfg.GetItemData(cfgName, id);
        if (languageCfg != null && !languageCfg.content.IsNull())
        {
            return languageCfg.content;
        }
        else
        {
            LogUtil.LogError($"没有找到类名为{cfgName} ID为{id}的文本");
            return $"Error:{id}";
        }
    }
}