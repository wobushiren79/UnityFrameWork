using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public partial class GameDataManager : BaseManager
{
    public GameConfigBean gameConfig;
    private BaseDataService<GameConfigBean> gameConfigService;

    public ModIdMapBean modIdMapBean;
    private BaseDataService<ModIdMapBean> modIdMapService;

    /// <summary>
    /// 获取游戏设置
    /// </summary>
    public GameConfigBean GetGameConfig()
    {
        if (gameConfig == null)
        {
            gameConfigService ??= new BaseDataService<GameConfigBean>("GameConfig")
            {
                JsonType = JsonTypeEnum.System
            };
            gameConfig = gameConfigService.Load();
            if (gameConfig == null)
                gameConfig = new GameConfigBean();
        }
        return gameConfig;
    }

    /// <summary>
    /// 保存游戏设置
    /// </summary>
    public void SaveGameConfig()
    {
        if (gameConfig != null)
        {
            gameConfigService ??= new BaseDataService<GameConfigBean>("GameConfig")
            {
                JsonType = JsonTypeEnum.System
            };
            gameConfigService.Save(gameConfig);
        }
    }

    #region ModID映射持久化
    /// <summary>
    /// 获取ModID映射；仅首次文件不存在时创建空映射，已存在文件读取失败、空数据或空字典均抛出异常，禁止触发自动重排覆盖
    /// </summary>
    /// <returns>已缓存或从磁盘成功读取的非空映射</returns>
    /// <exception cref="IOException">文件已存在但读取失败或反序列化结果为null</exception>
    /// <exception cref="InvalidDataException">映射字典为null或空，疑似损坏/被清空</exception>
    public ModIdMapBean GetModIdMap()
    {
        if (modIdMapBean == null)
        {
            modIdMapService ??= new BaseDataService<ModIdMapBean>("ModIdMap");
            string path = $"{modIdMapService.StoragePath}/{modIdMapService.FileName}";
            try
            {
                string json = File.ReadAllText(path);
                ModIdMapBean loadedData = JsonUtil.FromJson<ModIdMapBean>(json, modIdMapService.JsonType);
                if (loadedData == null)
                    throw new IOException($"ModID映射文件已存在但反序列化结果为null，已阻止空表重建:{path}");
                if (loadedData.modIdMap == null)
                    throw new InvalidDataException($"ModID映射字典为null，已阻止自动重排:{path}");
                if (loadedData.modIdMap.Count == 0)
                    throw new InvalidDataException($"ModID映射字典为空，已阻止自动重排:{path}");
                modIdMapBean = loadedData;
            }
            catch (FileNotFoundException)
            {
                modIdMapBean = new ModIdMapBean();
            }
            catch (DirectoryNotFoundException)
            {
                modIdMapBean = new ModIdMapBean();
            }
        }
        return modIdMapBean;
    }

    /// <summary>
    /// 原子保存ModID映射；成功替换前保留上一份为 ModIdMap_Backup，失败返回false且不主动删除旧文件
    /// </summary>
    /// <returns>原子写入成功返回true，无数据或写入失败返回false</returns>
    public bool SaveModIdMap()
    {
        if (modIdMapBean == null)
        {
            LogUtil.LogWarning("保存ModID映射失败-没有数据");
            return false;
        }
        modIdMapService ??= new BaseDataService<ModIdMapBean>("ModIdMap");
        return modIdMapService.SaveAtomic(modIdMapBean);
    }
    #endregion
}
