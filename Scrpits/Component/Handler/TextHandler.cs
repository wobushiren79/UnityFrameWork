using System.Collections;
using UnityEngine;

public partial class TextHandler : BaseHandler<TextHandler, TextManager>
{
    //空格不换行
    public string noBreakingSpace = "\u00A0";

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