using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Mod 管理器 - 负责 Mod 资源的加载、缓存和卸载
/// 使用 LoadAssetBundleUtil 进行底层 Bundle 操作
/// </summary>
public partial class ModManager : BaseManager
{
    /// <summary>
    /// 已加载的Mod Catalog信息
    /// key: modName, value: ResourceLocator
    /// </summary>
    private Dictionary<string, IResourceLocator> dicModLocators = new Dictionary<string, IResourceLocator>();

    /// <summary>
    /// 已加载的Mod Catalog句柄（用于释放）
    /// key: modName
    /// </summary>
    private Dictionary<string, AsyncOperationHandle<IResourceLocator>> dicCatalogHandles = new Dictionary<string, AsyncOperationHandle<IResourceLocator>>();

    /// <summary>
    /// 已加载的单个资源句柄缓存（用于释放）
    /// key: GetCacheKey(modName, assetKey)
    /// </summary>
    private Dictionary<string, AsyncOperationHandle> dicAssetHandles = new Dictionary<string, AsyncOperationHandle>();

    /// <summary>
    /// 已加载的单个资源缓存
    /// key: GetCacheKey(modName, assetKey)
    /// </summary>
    private Dictionary<string, UnityEngine.Object> dicAssetCache = new Dictionary<string, UnityEngine.Object>();

    /// <summary>
    /// 已加载的批量资源句柄缓存（用于释放）
    /// key: GetCacheKey(modName, label)
    /// </summary>
    private Dictionary<string, AsyncOperationHandle> dicListAssetHandles = new Dictionary<string, AsyncOperationHandle>();

    /// <summary>
    /// 已加载的Mod资源Key集合
    /// key: modName, value: 该Mod包含的所有资源地址集合（string类型Key）
    /// </summary>
    private Dictionary<string, HashSet<string>> dicModAssetKeys = new Dictionary<string, HashSet<string>>();

    /// <summary>
    /// 记录Mod的所有资源Key（string类型），加载Catalog成功后调用
    /// </summary>
    private void RecordModAssetKeys(string modName, IResourceLocator locator)
    {
        var keys = new HashSet<string>();
        foreach (var key in locator.Keys)
        {
            if (key is string strKey)
                keys.Add(strKey);
        }
        dicModAssetKeys[modName] = keys;
    }

    /// <summary>
    /// 判断指定assetKey是否属于已加载的某个Mod
    /// </summary>
    public bool IsModAsset(string assetKey)
    {
        foreach (var keys in dicModAssetKeys.Values)
        {
            if (keys.Contains(assetKey))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取包含指定assetKey的Mod名称，未找到返回null
    /// </summary>
    public string GetModNameForAsset(string assetKey)
    {
        foreach (var kvp in dicModAssetKeys)
        {
            if (kvp.Value.Contains(assetKey))
                return kvp.Key;
        }
        return null;
    }

    #region 初始化

    /// <summary>
    /// 初始化所有Mod：扫描ModRoot目录，加载所有可用Mod的Catalog并记录资源Key（异步回调）
    /// </summary>
    public void InitializeAllMods(Action<bool> callBack)
    {
        var availableMods = GetAvailableModNames();
        if (availableMods.Count == 0)
        {
            LogUtil.Log("[Mod] 未发现可用Mod");
            callBack?.Invoke(true);
            return;
        }

        int total = availableMods.Count;
        int completed = 0;
        bool allSuccess = true;

        foreach (var modName in availableMods)
        {
            LoadModCatalog(modName, (success) =>
            {
                if (!success) allSuccess = false;
                completed++;
                if (completed == total)
                {
                    LogUtil.Log($"[Mod] 初始化完成，共加载 {completed} 个Mod，成功: {allSuccess}");
                    callBack?.Invoke(allSuccess);
                }
            });
        }
    }

    /// <summary>
    /// 初始化所有Mod：扫描ModRoot目录，加载所有可用Mod的Catalog并记录资源Key（异步await）
    /// </summary>
    public async Task<bool> InitializeAllModsAsync()
    {
        var availableMods = GetAvailableModNames();
        if (availableMods.Count == 0)
        {
            LogUtil.Log("[Mod] 未发现可用Mod");
            return true;
        }

        bool allSuccess = true;
        foreach (var modName in availableMods)
        {
            bool success = await LoadModCatalogAsync(modName);
            if (!success) allSuccess = false;
        }

        LogUtil.Log($"[Mod] 初始化完成，共处理 {availableMods.Count} 个Mod，成功: {allSuccess}");
        return allSuccess;
    }

    /// <summary>
    /// 初始化所有Mod：扫描ModRoot目录，加载所有可用Mod的Catalog并记录资源Key（同步）
    /// </summary>
    public bool InitializeAllModsSync()
    {
        var availableMods = GetAvailableModNames();
        if (availableMods.Count == 0)
        {
            LogUtil.Log("[Mod] 未发现可用Mod");
            return true;
        }

        bool allSuccess = true;
        foreach (var modName in availableMods)
        {
            bool success = LoadModCatalogSync(modName);
            if (!success) allSuccess = false;
        }

        LogUtil.Log($"[Mod] 初始化完成，共处理 {availableMods.Count} 个Mod，成功: {allSuccess}");
        return allSuccess;
    }

    #endregion

    /// <summary>
    /// 获取Mods根目录
    /// 编辑器模式与打包项目路径相同：与 Assets / GameName_Data 同级的 Mods 目录
    /// </summary>
    public string GetModsRootPath()
    {
        return Path.Combine(Application.dataPath, "..", "Mods").Replace("\\", "/");
    }

    /// <summary>
    /// 获取指定Mod目录路径
    /// </summary>
    public string GetModPath(string modName)
    {
        return Path.Combine(GetModsRootPath(), modName).Replace("\\", "/");
    }

    /// <summary>
    /// 获取指定Mod的Catalog路径
    /// </summary>
    public string GetModCatalogPath(string modName)
    {
        return Path.Combine(GetModPath(modName), "catalog.bin").Replace("\\", "/");
    }

    /// <summary>
    /// 检查Mod是否已加载
    /// </summary>
    public bool IsModLoaded(string modName)
    {
        return dicModLocators.ContainsKey(modName);
    }

    /// <summary>
    /// 获取资源缓存Key（使用 | 分隔符避免与含下划线的modName/assetKey冲突）
    /// </summary>
    private string GetCacheKey(string modName, string assetKey)
    {
        return $"{modName}|{assetKey}";
    }

    /// <summary>
    /// 加载Mod的Content Catalog（异步回调）
    /// 仅在 Catalog + 依赖均成功后才写入缓存字典
    /// </summary>
    public void LoadModCatalog(string modName, Action<bool> callBack)
    {
        if (IsModLoaded(modName))
        {
            LogUtil.Log($"[Mod] Mod已加载: {modName}");
            callBack?.Invoke(true);
            return;
        }

        string catalogPath = GetModCatalogPath(modName);
        if (!File.Exists(catalogPath))
        {
            LogUtil.LogError($"[Mod] Catalog文件不存在: {catalogPath}");
            callBack?.Invoke(false);
            return;
        }

        Addressables.LoadContentCatalogAsync(catalogPath, false).Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var locator = handle.Result;
                var keys = new List<object>(locator.Keys);
                var depHandle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union);
                depHandle.Completed += (dh) =>
                {
                    // 先读取 Status，再 Release，避免 Release 后句柄失效
                    var depStatus = dh.Status;
                    var depException = dh.OperationException;
                    Addressables.Release(dh);

                    if (depStatus == AsyncOperationStatus.Succeeded)
                    {
                        // 依赖下载成功后才写入缓存，避免半初始化状态
                        dicModLocators[modName] = locator;
                        dicCatalogHandles[modName] = handle;
                        RecordModAssetKeys(modName, locator);
                        LogUtil.Log($"[Mod] Catalog+依赖加载成功: {modName}");
                        callBack?.Invoke(true);
                    }
                    else
                    {
                        // 依赖失败，释放已加载的 Catalog 句柄
                        Addressables.Release(handle);
                        LogUtil.LogError($"[Mod] 依赖加载失败: {modName}, Error: {depException}");
                        callBack?.Invoke(false);
                    }
                };
            }
            else
            {
                LogUtil.LogError($"[Mod] Catalog加载失败: {modName}, Error: {handle.OperationException}");
                callBack?.Invoke(false);
            }
        };
    }

    /// <summary>
    /// 加载Mod的Content Catalog（异步await）
    /// 仅在 Catalog + 依赖均成功后才写入缓存字典
    /// </summary>
    public async Task<bool> LoadModCatalogAsync(string modName)
    {
        if (IsModLoaded(modName))
        {
            LogUtil.Log($"[Mod] Mod已加载: {modName}");
            return true;
        }

        string catalogPath = GetModCatalogPath(modName);
        if (!File.Exists(catalogPath))
        {
            LogUtil.LogError($"[Mod] Catalog文件不存在: {catalogPath}");
            return false;
        }

        try
        {
            var handle = Addressables.LoadContentCatalogAsync(catalogPath, false);
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                LogUtil.LogError($"[Mod] Catalog加载失败: {modName}");
                return false;
            }

            var locator = handle.Result;
            var keys = new List<object>(locator.Keys);
            var depHandle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union);
            await depHandle.Task;

            // 先读取 Status，再 Release
            var depStatus = depHandle.Status;
            Addressables.Release(depHandle);

            if (depStatus == AsyncOperationStatus.Succeeded)
            {
                // 依赖下载成功后才写入缓存
                dicModLocators[modName] = locator;
                dicCatalogHandles[modName] = handle;
                RecordModAssetKeys(modName, locator);
                LogUtil.Log($"[Mod] Catalog+依赖加载成功: {modName}");
                return true;
            }
            else
            {
                Addressables.Release(handle);
                LogUtil.LogError($"[Mod] 依赖加载失败: {modName}");
                return false;
            }
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"[Mod] Catalog加载异常: {modName}, Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 加载Mod的Content Catalog（同步）
    /// 仅在 Catalog + 依赖均成功后才写入缓存字典
    /// </summary>
    public bool LoadModCatalogSync(string modName)
    {
        if (IsModLoaded(modName))
        {
            LogUtil.Log($"[Mod] Mod已加载: {modName}");
            return true;
        }

        string catalogPath = GetModCatalogPath(modName);
        if (!File.Exists(catalogPath))
        {
            LogUtil.LogError($"[Mod] Catalog文件不存在: {catalogPath}");
            return false;
        }

        try
        {
            var handle = Addressables.LoadContentCatalogAsync(catalogPath, false);
            var locator = handle.WaitForCompletion();
            if (locator == null)
            {
                LogUtil.LogError($"[Mod] Catalog同步加载失败: {modName}");
                return false;
            }

            var keys = new List<object>(locator.Keys);
            var depHandle = Addressables.DownloadDependenciesAsync(keys, Addressables.MergeMode.Union);
            depHandle.WaitForCompletion();

            // 先读取 Status，再 Release
            var depStatus = depHandle.Status;
            Addressables.Release(depHandle);

            if (depStatus == AsyncOperationStatus.Succeeded)
            {
                // 依赖下载成功后才写入缓存
                dicModLocators[modName] = locator;
                dicCatalogHandles[modName] = handle;
                RecordModAssetKeys(modName, locator);
                LogUtil.Log($"[Mod] Catalog+依赖同步加载成功: {modName}");
                return true;
            }
            else
            {
                Addressables.Release(handle);
                LogUtil.LogError($"[Mod] 依赖同步加载失败: {modName}");
                return false;
            }
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"[Mod] Catalog同步加载异常: {modName}, Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 同步加载Mod资源
    /// </summary>
    public T LoadModAssetSync<T>(string modName, string assetKey) where T : UnityEngine.Object
    {
        string cacheKey = GetCacheKey(modName, assetKey);

        if (dicAssetCache.TryGetValue(cacheKey, out var cached))
            return cached as T;

        if (!IsModLoaded(modName))
        {
            if (!LoadModCatalogSync(modName))
                return null;
        }

        try
        {
            var handle = Addressables.LoadAssetAsync<T>(assetKey);
            T result = handle.WaitForCompletion();
            if (result != null)
            {
                dicAssetHandles[cacheKey] = handle;
                dicAssetCache[cacheKey] = result;
            }
            else
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
            return result;
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"[Mod] 资源同步加载失败: {modName}/{assetKey}, Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 异步加载Mod资源（回调）
    /// </summary>
    public void LoadModAsset<T>(string modName, string assetKey, Action<T> callBack) where T : UnityEngine.Object
    {
        string cacheKey = GetCacheKey(modName, assetKey);

        if (dicAssetCache.TryGetValue(cacheKey, out var cached))
        {
            callBack?.Invoke(cached as T);
            return;
        }

        if (!IsModLoaded(modName))
        {
            LoadModCatalog(modName, (success) =>
            {
                if (!success) { callBack?.Invoke(null); return; }
                LoadAndCacheAsset(modName, assetKey, cacheKey, callBack);
            });
            return;
        }

        LoadAndCacheAsset(modName, assetKey, cacheKey, callBack);
    }

    /// <summary>
    /// 提取单资源异步加载+缓存的公共逻辑，消除 LoadModAsset 中的代码重复
    /// </summary>
    private void LoadAndCacheAsset<T>(string modName, string assetKey, string cacheKey, Action<T> callBack) where T : UnityEngine.Object
    {
        LoadAddressablesUtil.LoadAssetAsync<T>(assetKey, (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                if (!dicAssetCache.ContainsKey(cacheKey))
                {
                    dicAssetHandles[cacheKey] = handle;
                    dicAssetCache[cacheKey] = handle.Result;
                }
                callBack?.Invoke(handle.Result);
            }
            else
            {
                LogUtil.LogError($"[Mod] 资源异步加载失败: {modName}/{assetKey}");
                callBack?.Invoke(null);
            }
        });
    }

    /// <summary>
    /// 异步加载Mod资源（await）
    /// </summary>
    public async Task<T> LoadModAssetAsync<T>(string modName, string assetKey) where T : UnityEngine.Object
    {
        string cacheKey = GetCacheKey(modName, assetKey);

        if (dicAssetCache.TryGetValue(cacheKey, out var cached))
            return cached as T;

        if (!IsModLoaded(modName))
        {
            if (!await LoadModCatalogAsync(modName))
                return null;
        }

        try
        {
            AsyncOperationHandle<T> handle = await LoadAddressablesUtil.LoadAssetAsync<T>(assetKey);
            if (handle.IsValid() && handle.Result != null)
            {
                if (!dicAssetCache.ContainsKey(cacheKey))
                {
                    dicAssetHandles[cacheKey] = handle;
                    dicAssetCache[cacheKey] = handle.Result;
                }
                return handle.Result;
            }

            // handle 有效但 Result 为 null，需要释放防止泄漏
            if (handle.IsValid())
                Addressables.Release(handle);
            return null;
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"[Mod] 资源异步加载失败: {modName}/{assetKey}, Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 异步加载Mod多个资源（回调），结果缓存以避免重复加载
    /// </summary>
    public void LoadModAssets<T>(string modName, string label, Action<IList<T>> callBack) where T : UnityEngine.Object
    {
        string cacheKey = GetCacheKey(modName, label);

        // 检查批量句柄缓存
        if (dicListAssetHandles.TryGetValue(cacheKey, out var existingHandle)
            && existingHandle.IsValid()
            && existingHandle.Status == AsyncOperationStatus.Succeeded)
        {
            callBack?.Invoke(existingHandle.Result as IList<T>);
            return;
        }

        if (!IsModLoaded(modName))
        {
            LoadModCatalog(modName, (success) =>
            {
                if (!success) { callBack?.Invoke(null); return; }
                LoadAndCacheAssets(modName, label, cacheKey, callBack);
            });
            return;
        }

        LoadAndCacheAssets(modName, label, cacheKey, callBack);
    }

    /// <summary>
    /// 提取批量异步加载+缓存的公共逻辑
    /// </summary>
    private void LoadAndCacheAssets<T>(string modName, string label, string cacheKey, Action<IList<T>> callBack) where T : UnityEngine.Object
    {
        LoadAddressablesUtil.LoadAssetsAsync<T>(label, (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (!dicListAssetHandles.ContainsKey(cacheKey))
                    dicListAssetHandles[cacheKey] = handle;
                callBack?.Invoke(handle.Result);
            }
            else
            {
                LogUtil.LogError($"[Mod] 批量资源加载失败: {modName}/{label}");
                callBack?.Invoke(null);
            }
        });
    }

    /// <summary>
    /// 释放指定Mod的单个资源
    /// </summary>
    public void ReleaseModAsset(string modName, string assetKey)
    {
        string cacheKey = GetCacheKey(modName, assetKey);
        if (dicAssetHandles.TryGetValue(cacheKey, out var handle))
        {
            if (handle.IsValid())
                Addressables.Release(handle);
            dicAssetHandles.Remove(cacheKey);
        }
        dicAssetCache.Remove(cacheKey);
    }

    /// <summary>
    /// 释放指定Mod的批量资源（通过Label加载的资源）
    /// </summary>
    public void ReleaseModAssets(string modName, string label)
    {
        string cacheKey = GetCacheKey(modName, label);
        if (dicListAssetHandles.TryGetValue(cacheKey, out var handle))
        {
            if (handle.IsValid())
                Addressables.Release(handle);
            dicListAssetHandles.Remove(cacheKey);
        }
    }

    /// <summary>
    /// 卸载指定Mod的所有资源和Catalog
    /// </summary>
    public void UnloadMod(string modName)
    {
        // 使用 | 分隔符构造前缀，与 GetCacheKey 保持一致，避免误匹配含相同前缀的其他Mod
        string prefix = $"{modName}|";

        // 释放该Mod下所有单个资源句柄
        var keysToRemove = new List<string>();
        foreach (var kvp in dicAssetHandles)
        {
            if (kvp.Key.StartsWith(prefix))
            {
                if (kvp.Value.IsValid())
                    Addressables.Release(kvp.Value);
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            dicAssetHandles.Remove(key);
            dicAssetCache.Remove(key);
        }

        // 释放该Mod下所有批量资源句柄
        var listKeysToRemove = new List<string>();
        foreach (var kvp in dicListAssetHandles)
        {
            if (kvp.Key.StartsWith(prefix))
            {
                if (kvp.Value.IsValid())
                    Addressables.Release(kvp.Value);
                listKeysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in listKeysToRemove)
            dicListAssetHandles.Remove(key);

        // 移除ResourceLocator并释放Catalog句柄
        if (dicModLocators.TryGetValue(modName, out var locator))
        {
            Addressables.RemoveResourceLocator(locator);
            dicModLocators.Remove(modName);
        }
        if (dicCatalogHandles.TryGetValue(modName, out var catalogHandle))
        {
            if (catalogHandle.IsValid())
                Addressables.Release(catalogHandle);
            dicCatalogHandles.Remove(modName);
        }

        dicModAssetKeys.Remove(modName);
        LogUtil.Log($"[Mod] Mod已卸载: {modName}");
    }

    /// <summary>
    /// 卸载所有Mod
    /// </summary>
    public void UnloadAllMods()
    {
        foreach (var kvp in dicAssetHandles)
        {
            if (kvp.Value.IsValid())
                Addressables.Release(kvp.Value);
        }
        dicAssetHandles.Clear();
        dicAssetCache.Clear();

        foreach (var kvp in dicListAssetHandles)
        {
            if (kvp.Value.IsValid())
                Addressables.Release(kvp.Value);
        }
        dicListAssetHandles.Clear();

        foreach (var kvp in dicModLocators)
            Addressables.RemoveResourceLocator(kvp.Value);
        dicModLocators.Clear();

        foreach (var kvp in dicCatalogHandles)
        {
            if (kvp.Value.IsValid())
                Addressables.Release(kvp.Value);
        }
        dicCatalogHandles.Clear();

        dicModAssetKeys.Clear();
        LogUtil.Log("[Mod] 所有Mod已卸载");
    }

    /// <summary>
    /// 获取所有已加载的Mod名称
    /// </summary>
    public List<string> GetLoadedModNames()
    {
        return new List<string>(dicModLocators.Keys);
    }

    /// <summary>
    /// 获取Mods目录下所有可用的Mod名称
    /// </summary>
    public List<string> GetAvailableModNames()
    {
        List<string> modNames = new List<string>();
        string modsRoot = GetModsRootPath();
        if (!Directory.Exists(modsRoot))
            return modNames;

        foreach (var dir in Directory.GetDirectories(modsRoot))
        {
            string modName = Path.GetFileName(dir);
            if (File.Exists(GetModCatalogPath(modName)))
                modNames.Add(modName);
        }
        return modNames;
    }
}
