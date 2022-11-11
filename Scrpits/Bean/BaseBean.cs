using UnityEngine;
using UnityEditor;
using System;

[Serializable]
public class BaseBean 
{
    public int id;//id
    public int valid;//是否有效

    public string name_cn;
    public string name_en;

    public string GetName()
    {
        return GetBaseText("name").Replace(" ", TextHandler.Instance.noBreakingSpace);
    }

    /// <summary>
    /// 获取字段值
    /// </summary>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    public string GetBaseText(string name)
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        string fieldName = $"{name}_{gameConfig.GetLanguage().GetEnumName()}";
        string data = (string)this.GetType().GetField(fieldName).GetValue(this);
        if (data == null)
        {
            fieldName = $"{name}_en";
            data = (string)this.GetType().GetField(fieldName).GetValue(this);
        }
        return data;
    }
}