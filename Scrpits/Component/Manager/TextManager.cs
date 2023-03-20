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
        UITextBean uiText = UITextCfg.GetItemData(id);
        if (uiText != null)
        {
            GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
            switch (gameConfig.GetLanguage())
            {
                case LanguageEnum.cn:
                    return uiText.content_cn;
                case LanguageEnum.en:
                    return uiText.content_en;
            }
            return null;
        }
        else
        {
            LogUtil.LogError("没有找到ID为" + id + "的UI内容");
            return null;
        }
    }
}