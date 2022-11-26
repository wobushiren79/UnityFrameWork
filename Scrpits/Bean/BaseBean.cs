using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

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

    public string GetContent()
    {
        return GetBaseText("content").Replace(" ", TextHandler.Instance.noBreakingSpace);
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

public class BaseCfg<E, T> where T : BaseBean
{
    protected static T GetItemData(E key, Dictionary<E, T> dicData)
    {
        if (dicData.TryGetValue(key, out T value))
        {
            return value;
        }
        return null;
    }

    protected static T[] GetInitData(string fileName)
    {
        if (fileName == null)
        {
            LogUtil.Log($"读取文件失败-没有文件名称{fileName}");
            return null;
        }
        TextAsset textAsset = LoadResourcesUtil.SyncLoadData<TextAsset>($"JsonText/{fileName}");
        if (textAsset == null || textAsset.text == null)
            return null;
        T[] arrayData = JsonUtil.FromJsonByNet<T[]>(textAsset.text);
        return arrayData;
    }
}