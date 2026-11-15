using UnityEngine;

/// <summary>
/// 泛型数据服务基类，封装 JSON 文件的 Load / Save / Delete
/// 替代传统 MVC 中的 Model + Controller + IView 层，让 Manager 直接操作数据
/// </summary>
public class BaseDataService<T> where T : class, new()
{
    public string FileName { get; protected set; }
    public JsonTypeEnum JsonType { get; set; } = JsonTypeEnum.Net;
    public string StoragePath { get; set; } = Application.persistentDataPath;

    public BaseDataService(string fileName)
    {
        FileName = fileName;
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    public virtual T Load(bool isShowLog = true)
    {
        string path = $"{StoragePath}/{FileName}";
        string json = FileUtil.LoadTextFile(path);
        if (json == null)
        {
            if (isShowLog)
                LogUtil.Log($"读取文件失败-没有数据: {path}");
            return null;
        }
        return JsonUtil.FromJson<T>(json, JsonType);
    }

    /// <summary>
    /// 保存数据
    /// </summary>
    public virtual void Save(T data)
    {
        if (FileName.IsNull())
        {
            LogUtil.Log("保存文件失败-没有文件名称");
            return;
        }
        if (data == null)
        {
            LogUtil.Log("保存文件失败-没有数据");
            return;
        }
        string json = JsonUtil.ToJson(data, JsonType);
        FileUtil.CreateTextFile(StoragePath, FileName, json);
    }

    #region 原子保存
    /// <summary>以原子替换方式保存数据；旧文件保留为 文件名_Backup，失败返回 false 且原文件不被主动删除</summary>
    /// <param name="data">要完整序列化保存的数据</param>
    /// <returns>序列化并原子替换成功返回 true，参数或写入失败返回 false</returns>
    public virtual bool SaveAtomic(T data)
    {
        if (FileName.IsNull())
        {
            LogUtil.Log("保存文件失败-没有文件名称");
            return false;
        }
        if (data == null)
        {
            LogUtil.Log("保存文件失败-没有数据");
            return false;
        }
        string json = JsonUtil.ToJson(data, JsonType);
        return FileUtil.TryCreateTextFileAtomic(StoragePath, FileName, json);
    }
    #endregion

    /// <summary>
    /// 删除数据文件
    /// </summary>
    public virtual void Delete()
    {
        if (FileName.IsNull())
        {
            LogUtil.Log("删除文件失败-没有文件路径");
            return;
        }
        FileUtil.DeleteFile($"{StoragePath}/{FileName}");
    }
}
