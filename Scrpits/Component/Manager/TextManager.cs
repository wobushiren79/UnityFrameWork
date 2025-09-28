using System.Collections;
using UnityEngine;

public partial class TextManager : BaseManager
{

    /// <summary>
    /// 根据ID获取文字内容
    /// </summary>
    /// <returns></returns>
    public string GetTextById(long id)
    {
        LanguageBean languageCfg = LanguageCfg.GetItemData(id);
        if (languageCfg != null && !languageCfg.content.IsNull())
        {
            return languageCfg.content;
        }
        else
        {
            LogUtil.LogError("没有找到ID为" + id + "的UI内容");
            return $"Error:{id}";
        }
    }
}