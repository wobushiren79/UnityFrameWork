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
        // 行本身缺失 → 真错误（配置漏配/ID 写错）
        if (languageCfg == null)
        {
            LogUtil.LogError($"没有找到类名为{cfgName} ID为{id}的文本");
            return $"Error:{id}";
        }
        // 行存在，内容允许为空 → 静默返回空串，不报错（如日历等大量留空的表）
        switch (contentIndex)
        {
            case 1:
                return languageCfg.content_1 ?? "";
            case 2:
                return languageCfg.content_2 ?? "";
            default:
                return languageCfg.content ?? "";
        }
    }
}