using UnityEngine;
using System.Collections.Generic;

public abstract class BaseDataStorage<T>
{
    //数据保存路径
    protected string dataStoragePath;

    /// <summary>
    /// 初始化参数
    /// </summary>
    public BaseDataStorage()
    {
        dataStoragePath = Application.persistentDataPath;
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
    /// 基础-保存单个数据
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="dataBean"></param>
    public void BaseSaveData(string fileName, T dataBean)
    {
        if (fileName.IsNull())
        {
            LogUtil.Log("保存文件失败-没有文件名称");
            return;
        }
        if (dataBean == null)
        {
            LogUtil.Log("保存文件失败-没有数据");
            return;
        }
        string strData = JsonUtil.ToJson(dataBean);
        FileUtil.CreateTextFile(dataStoragePath, fileName, strData);
    }

    /// <summary>
    /// 基础-删除文件
    /// </summary>
    /// <param name="fileName"></param>
    public void BaseDeleteFile(string fileName)
    {
        if (fileName.IsNull())
        {
            LogUtil.Log("删除文件失败-没有文件路径");
            return;
        }
        FileUtil.DeleteFile(dataStoragePath + "/" + fileName);
    }

    /// <summary>
    /// 基础-保存数据列表
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="dataBeanList"></param>
    public void BaseSaveDataForList(string fileName, List<T> dataBeanList)
    {
        if (fileName == null)
        {
            LogUtil.Log("保存文件失败-没有文件名称");
            return;
        }
        if (dataBeanList == null)
        {
            LogUtil.Log("保存文件失败-没有数据");
            return;
        }
        DataStorageListBean<T> handBean = new DataStorageListBean<T>();
        handBean.listData = dataBeanList;
        string strData = JsonUtil.ToJson(handBean);
        FileUtil.CreateTextFile(dataStoragePath, fileName, strData);
    }

    /// <summary>
    /// 基础-读取单个数据
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public T BaseLoadData(string fileName)
    {
        if (fileName == null)
        {
            LogUtil.Log("读取文件失败-没有文件名称");
            return default;
        }
        return BaseLoadDataByPath($"{dataStoragePath}/{fileName}");
    }

    public T BaseLoadDataByPath(string path)
    {
        string strData = FileUtil.LoadTextFile(path);
        if (strData == null)
            return default;
        T data = JsonUtil.FromJson<T>(strData);
        return data;
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
        string strData = FileUtil.LoadTextFile(dataStoragePath + "/" + fileName);
        if (strData == null)
            return null;
        DataStorageListBean<T> handBean = JsonUtil.FromJson<DataStorageListBean<T>>(strData);
        if (handBean == null)
            return null;
        return handBean.listData;
    }

    /// <summary>
    /// 基础-删除文件夹
    /// </summary>
    public void BaseDeleteFolder(string folderName)
    {
        FileUtil.DeleteDirectory(dataStoragePath + "/" + folderName);
    }
}