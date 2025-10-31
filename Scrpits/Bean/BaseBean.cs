using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[Serializable]
public class BaseBean 
{
    public long id;//id
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