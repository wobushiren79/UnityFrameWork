using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class BaseDataRead<T>
{
    //数据保存路径
    protected string dataStoragePath;

    /// <summary>
    /// 初始化参数
    /// </summary>
    public BaseDataRead()
    {
        dataStoragePath = Application.streamingAssetsPath + "/JsonText";
    }

    /// <summary>
    /// 获取数据保存路径
    /// </summary>
    /// <returns></returns>
    public string GetDataStoragePath()
    {
        return dataStoragePath;
    }

    /// <summary>
    /// 设置数据保存路径
    /// </summary>
    /// <param name="dataStoragePath"></param>
    public void SetDataStoragePath(string dataStoragePath)
    {
        this.dataStoragePath = dataStoragePath;
    }

    /// <summary>
    /// 基础-读取列表数据
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public List<T> BaseLoadDataForList(string fileName)
    {
        if (fileName == null)
        {
            LogUtil.Log("读取文件失败-没有文件名称");
            return null;
        }
        TextAsset textAsset = LoadResourcesUtil.SyncLoadData<TextAsset>($"JsonText/{fileName}");
        //string strData = FileUtil.LoadTextFile(dataStoragePath + "/" + fileName);
        if (textAsset == null || textAsset.text == null)
            return null;
        List<T> listData = JsonUtil.FromJsonByNet<List<T>>(textAsset.text);
        return listData;
    }
}