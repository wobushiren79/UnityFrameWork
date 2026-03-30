using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Mod 处理器 - 提供 Mod 系统的逻辑接口
/// 继承 BaseHandler，自动关联 ModManager
/// </summary>
public partial class ModHandler : BaseHandler<ModHandler, ModManager>
{
    /// <summary>
    /// 初始化 Mod 系统
    /// </summary>
    public void Initialize()
    {
        manager.Initialize();
    }

    /// <summary>
    /// 设置自定义 Mod 根路径
    /// </summary>
    public void SetModBasePath(string path)
    {
        manager.SetModBasePath(path);
    }

    #region 同步加载

    /// <summary>
    /// 同步加载 Mod Bundle
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <returns>加载的 AssetBundle</returns>
    public AssetBundle LoadMod(string modName)
    {
        return manager.LoadModSync(modName);
    }

    /// <summary>
    /// 同步加载 Mod 中的单个资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <param name="assetName">资源名称</param>
    /// <returns>加载的资源</returns>
    public T LoadAsset<T>(string modName, string assetName) where T : UnityEngine.Object
    {
        return manager.LoadAssetSync<T>(modName, assetName);
    }

    /// <summary>
    /// 同步加载 Mod 中的所有资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <returns>资源数组</returns>
    public T[] LoadAllAssets<T>(string modName) where T : UnityEngine.Object
    {
        return manager.LoadAllAssetsSync<T>(modName);
    }

    #endregion

    #region 异步加载（回调方式）

    /// <summary>
    /// 异步加载 Mod Bundle（回调方式）
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <param name="onSuccess">成功回调</param>
    /// <param name="onFail">失败回调</param>
    public void LoadModAsync(string modName, Action<AssetBundle> onSuccess, Action<string> onFail = null)
    {
        manager.LoadModAsync(modName, onSuccess, onFail);
    }

    /// <summary>
    /// 异步加载 Mod 中的单个资源（回调方式）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <param name="assetName">资源名称</param>
    /// <param name="onSuccess">成功回调</param>
    /// <param name="onFail">失败回调</param>
    public void LoadAssetAsync<T>(string modName, string assetName, Action<T> onSuccess, Action<string> onFail = null) where T : UnityEngine.Object
    {
        manager.LoadAssetAsync<T>(modName, assetName, onSuccess, onFail);
    }

    /// <summary>
    /// 异步加载 Mod 中的所有资源（回调方式）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <param name="onSuccess">成功回调</param>
    /// <param name="onFail">失败回调</param>
    public void LoadAllAssetsAsync<T>(string modName, Action<T[]> onSuccess, Action<string> onFail = null) where T : UnityEngine.Object
    {
        manager.LoadAllAssetsAsync<T>(modName, onSuccess, onFail);
    }

    #endregion

    #region 异步加载（Task/协程方式）

    /// <summary>
    /// 异步加载 Mod Bundle（Task 方式，支持 await）
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <returns>Task 返回 AssetBundle</returns>
    public Task<AssetBundle> LoadModTaskAsync(string modName)
    {
        return manager.LoadModAsync(modName);
    }

    /// <summary>
    /// 异步加载 Mod 中的单个资源（Task 方式，支持 await）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <param name="assetName">资源名称</param>
    /// <returns>Task 返回资源</returns>
    public Task<T> LoadAssetTaskAsync<T>(string modName, string assetName) where T : UnityEngine.Object
    {
        return manager.LoadAssetAsync<T>(modName, assetName);
    }

    /// <summary>
    /// 异步加载 Mod 中的所有资源（Task 方式，支持 await）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <returns>Task 返回资源数组</returns>
    public Task<T[]> LoadAllAssetsTaskAsync<T>(string modName) where T : UnityEngine.Object
    {
        return manager.LoadAllAssetsAsync<T>(modName);
    }

    /// <summary>
    /// 协程方式异步加载 Mod 中的单个资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <param name="assetName">资源名称</param>
    /// <param name="onSuccess">成功回调</param>
    /// <param name="onFail">失败回调</param>
    /// <returns>协程 IEnumerator</returns>
    public IEnumerator LoadAssetCoroutine<T>(string modName, string assetName, Action<T> onSuccess, Action<string> onFail = null) where T : UnityEngine.Object
    {
        return manager.LoadAssetCoroutine<T>(modName, assetName, onSuccess, onFail);
    }

    /// <summary>
    /// 协程方式异步加载 Mod 中的所有资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <param name="onSuccess">成功回调</param>
    /// <param name="onFail">失败回调</param>
    /// <returns>协程 IEnumerator</returns>
    public IEnumerator LoadAllAssetsCoroutine<T>(string modName, Action<T[]> onSuccess, Action<string> onFail = null) where T : UnityEngine.Object
    {
        return manager.LoadAllAssetsCoroutine<T>(modName, onSuccess, onFail);
    }

    #endregion

    #region 批量加载

    /// <summary>
    /// 批量加载多个 Mod（同步）
    /// </summary>
    /// <param name="modNames">Mod 名称列表</param>
    /// <returns>成功加载的 Mod 名称列表</returns>
    public List<string> LoadMods(IEnumerable<string> modNames)
    {
        List<string> loaded = new List<string>();
        foreach (string modName in modNames)
        {
            if (LoadMod(modName) != null)
                loaded.Add(modName);
        }
        return loaded;
    }

    /// <summary>
    /// 批量加载多个 Mod（异步回调方式）
    /// </summary>
    /// <param name="modNames">Mod 名称列表</param>
    /// <param name="onComplete">完成回调（参数：成功加载的 Mod 名称列表）</param>
    /// <param name="onProgress">进度回调（参数：当前进度 0-1）</param>
    public void LoadModsAsync(IEnumerable<string> modNames, Action<List<string>> onComplete, Action<float> onProgress = null)
    {
        List<string> modList = new List<string>(modNames);
        List<string> loaded = new List<string>();
        int total = modList.Count;
        int completed = 0;

        if (total == 0)
        {
            onComplete?.Invoke(loaded);
            return;
        }

        foreach (string modName in modList)
        {
            LoadModAsync(modName, (bundle) =>
            {
                if (bundle != null)
                    loaded.Add(modName);

                completed++;
                onProgress?.Invoke((float)completed / total);

                if (completed >= total)
                    onComplete?.Invoke(loaded);
            }, (error) =>
            {
                completed++;
                onProgress?.Invoke((float)completed / total);

                if (completed >= total)
                    onComplete?.Invoke(loaded);
            });
        }
    }

    /// <summary>
    /// 批量加载多个 Mod（异步 Task 方式）
    /// </summary>
    /// <param name="modNames">Mod 名称列表</param>
    /// <param name="onProgress">进度回调（参数：当前进度 0-1）</param>
    /// <returns>Task 返回成功加载的 Mod 名称列表</returns>
    public async Task<List<string>> LoadModsTaskAsync(IEnumerable<string> modNames, Action<float> onProgress = null)
    {
        List<string> modList = new List<string>(modNames);
        List<string> loaded = new List<string>();
        int total = modList.Count;

        for (int i = 0; i < total; i++)
        {
            AssetBundle bundle = await manager.LoadModAsync(modList[i]);
            if (bundle != null)
                loaded.Add(modList[i]);

            onProgress?.Invoke((float)(i + 1) / total);
        }

        return loaded;
    }

    #endregion

    #region 卸载方法

    /// <summary>
    /// 卸载指定 Mod
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <param name="unloadAllLoadedObjects">是否卸载已加载的资源实例</param>
    public void UnloadMod(string modName, bool unloadAllLoadedObjects = false)
    {
        manager.UnloadMod(modName, unloadAllLoadedObjects);
    }

    /// <summary>
    /// 批量卸载多个 Mod
    /// </summary>
    /// <param name="modNames">Mod 名称列表</param>
    /// <param name="unloadAllLoadedObjects">是否卸载已加载的资源实例</param>
    public void UnloadMods(IEnumerable<string> modNames, bool unloadAllLoadedObjects = false)
    {
        foreach (string modName in modNames)
        {
            manager.UnloadMod(modName, unloadAllLoadedObjects);
        }
    }

    /// <summary>
    /// 强制卸载指定 Mod（忽略引用计数）
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <param name="unloadAllLoadedObjects">是否卸载已加载的资源实例</param>
    public void ForceUnloadMod(string modName, bool unloadAllLoadedObjects = true)
    {
        manager.ForceUnloadMod(modName, unloadAllLoadedObjects);
    }

    /// <summary>
    /// 卸载所有 Mod
    /// </summary>
    /// <param name="unloadAllLoadedObjects">是否卸载已加载的资源实例</param>
    public void UnloadAllMods(bool unloadAllLoadedObjects = true)
    {
        manager.UnloadAllMods(unloadAllLoadedObjects);
    }

    #endregion

    #region 查询方法

    /// <summary>
    /// 获取已加载的 Mod 列表
    /// </summary>
    public List<string> GetLoadedModNames()
    {
        return manager.GetLoadedModNames();
    }

    /// <summary>
    /// 检查 Mod 是否已加载
    /// </summary>
    public bool IsModLoaded(string modName)
    {
        return manager.IsModLoaded(modName);
    }

    /// <summary>
    /// 获取所有可用的 Mod 文件列表
    /// </summary>
    public List<string> GetAvailableMods()
    {
        return manager.GetAvailableMods();
    }

    /// <summary>
    /// 获取 Mod 的 Bundle 引用计数
    /// </summary>
    public int GetModRefCount(string modName)
    {
        return manager.GetModRefCount(modName);
    }

    /// <summary>
    /// 获取 Mod 的依赖列表
    /// </summary>
    public string[] GetModDependencies(string modName)
    {
        return manager.GetModDependencies(modName);
    }

    /// <summary>
    /// 获取指定 Mod 中已加载的资源列表
    /// </summary>
    public List<UnityEngine.Object> GetLoadedAssets(string modName)
    {
        return manager.GetLoadedAssets(modName);
    }

    /// <summary>
    /// 获取 Mod 资源根路径
    /// </summary>
    public string GetModBasePath()
    {
        return manager.GetModBasePath();
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 输出当前 Mod 缓存状态（调试用）
    /// </summary>
    public void LogModCacheStatus()
    {
        manager.LogModCacheStatus();
    }

    /// <summary>
    /// 清理过期的 Mod 缓存
    /// </summary>
    public void CleanExpiredCache(bool unloadAllLoadedObjects = false)
    {
        manager.CleanExpiredCache(unloadAllLoadedObjects);
    }

    #endregion
}
