using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Mod 处理器 - 提供 Mod 系统的逻辑接口
/// 继承 BaseHandler，自动关联 ModManager
/// </summary>
public partial class ModHandler : BaseHandler<ModHandler, ModManager>
{
    #region Catalog 加载 / 卸载

    /// <summary>
    /// 检查指定Mod是否已加载
    /// </summary>
    public bool IsModLoaded(string modName)
    {
        return manager.IsModLoaded(modName);
    }

    /// <summary>
    /// 同步加载Mod的Content Catalog
    /// </summary>
    public bool LoadModCatalogSync(string modName)
    {
        return manager.LoadModCatalogSync(modName);
    }

    /// <summary>
    /// 异步加载Mod的Content Catalog（回调）
    /// </summary>
    public void LoadModCatalog(string modName, Action<bool> callBack)
    {
        manager.LoadModCatalog(modName, callBack);
    }

    /// <summary>
    /// 异步加载Mod的Content Catalog（await）
    /// </summary>
    public async Task<bool> LoadModCatalogAsync(string modName)
    {
        return await manager.LoadModCatalogAsync(modName);
    }

    /// <summary>
    /// 卸载指定Mod的所有资源和Catalog
    /// </summary>
    public void UnloadMod(string modName)
    {
        manager.UnloadMod(modName);
    }

    /// <summary>
    /// 卸载所有Mod
    /// </summary>
    public void UnloadAllMods()
    {
        manager.UnloadAllMods();
    }

    #endregion

    #region 资源加载

    /// <summary>
    /// 同步加载Mod资源
    /// </summary>
    public T LoadAssetSync<T>(string modName, string assetKey) where T : UnityEngine.Object
    {
        return manager.LoadModAssetSync<T>(modName, assetKey);
    }

    /// <summary>
    /// 异步加载Mod资源（回调）
    /// </summary>
    public void LoadAsset<T>(string modName, string assetKey, Action<T> callBack) where T : UnityEngine.Object
    {
        manager.LoadModAsset(modName, assetKey, callBack);
    }

    /// <summary>
    /// 异步加载Mod资源（await）
    /// </summary>
    public async Task<T> LoadAssetAsync<T>(string modName, string assetKey) where T : UnityEngine.Object
    {
        return await manager.LoadModAssetAsync<T>(modName, assetKey);
    }

    /// <summary>
    /// 异步加载Mod多个资源（通过Label/Key回调）
    /// </summary>
    public void LoadAssets<T>(string modName, string label, Action<IList<T>> callBack) where T : UnityEngine.Object
    {
        manager.LoadModAssets(modName, label, callBack);
    }

    #endregion

    #region 资源释放

    /// <summary>
    /// 释放指定Mod的单个资源
    /// </summary>
    public void ReleaseAsset(string modName, string assetKey)
    {
        manager.ReleaseModAsset(modName, assetKey);
    }

    /// <summary>
    /// 释放指定Mod的批量资源（通过Label加载的资源）
    /// </summary>
    public void ReleaseAssets(string modName, string label)
    {
        manager.ReleaseModAssets(modName, label);
    }

    #endregion

    #region 查询

    /// <summary>
    /// 获取所有已加载的Mod名称
    /// </summary>
    public List<string> GetLoadedModNames()
    {
        return manager.GetLoadedModNames();
    }

    /// <summary>
    /// 获取Mods目录下所有可用的Mod名称（存在catalog.bin的目录）
    /// </summary>
    public List<string> GetAvailableModNames()
    {
        return manager.GetAvailableModNames();
    }

    /// <summary>
    /// 获取指定Mod的目录路径
    /// </summary>
    public string GetModPath(string modName)
    {
        return manager.GetModPath(modName);
    }

    #endregion
}
