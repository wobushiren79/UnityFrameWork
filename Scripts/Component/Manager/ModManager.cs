using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Mod 管理器 - 负责 Mod 资源的加载、缓存和卸载
/// 使用 LoadAssetBundleUtil 进行底层 Bundle 操作
/// </summary>
public partial class ModManager : BaseManager
{
    // Mod 资源根路径
    private string _modBasePath;

    // 已加载的 Mod 缓存：modName -> ModCacheEntry
    private Dictionary<string, ModCacheEntry> _modCache = new Dictionary<string, ModCacheEntry>();

    // 是否已初始化
    private bool _isInitialized = false;

    /// <summary>
    /// Mod 缓存条目
    /// </summary>
    private class ModCacheEntry
    {
        public string ModName;
        public AssetBundle Bundle;
        public List<UnityEngine.Object> LoadedAssets;
        public float LastAccessTime;

        public ModCacheEntry(string modName, AssetBundle bundle)
        {
            ModName = modName;
            Bundle = bundle;
            LoadedAssets = new List<UnityEngine.Object>();
            LastAccessTime = Time.realtimeSinceStartup;
        }
    }

    /// <summary>
    /// 获取 Mod 资源根路径
    /// Editor: Library/com.unity.addressables/aa/Mods/StandaloneWindows64
    /// Runtime: Application.dataPath + "/Mods/"
    /// </summary>
    public string GetModBasePath()
    {
        if (!string.IsNullOrEmpty(_modBasePath))
            return _modBasePath;

#if UNITY_EDITOR
        // Editor 模式下使用 Addressables 缓存目录
        _modBasePath = Path.Combine(Application.dataPath, "../Library/com.unity.addressables/aa/Mods/StandaloneWindows64").Replace('\\', '/');
#else
        // 运行时打包目录
        _modBasePath = $"{Application.dataPath}/Mods/";
#endif
        return _modBasePath;
    }

    /// <summary>
    /// 设置自定义 Mod 根路径
    /// </summary>
    public void SetModBasePath(string path)
    {
        _modBasePath = path?.TrimEnd('/', '\\') + "/";
    }

    /// <summary>
    /// 初始化 Mod 系统（自动调用）
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;

        string basePath = GetModBasePath();
        if (!Directory.Exists(basePath))
        {
            LogUtil.LogWarning($"ModManager: Mod 目录不存在: {basePath}");
        }

        _isInitialized = true;
        LogUtil.Log($"ModManager: 已初始化，Mod 路径: {basePath}");
    }

    #region 同步加载

    /// <summary>
    /// 同步加载 Mod Bundle
    /// </summary>
    /// <param name="modName">Mod 名称（Bundle 文件名，不含扩展名）</param>
    /// <returns>加载的 AssetBundle，失败返回 null</returns>
    public AssetBundle LoadModSync(string modName)
    {
        EnsureInitialized();

        if (string.IsNullOrEmpty(modName))
        {
            LogUtil.LogError("ModManager: modName 不能为空");
            return null;
        }

        // 检查缓存
        if (_modCache.TryGetValue(modName, out ModCacheEntry cachedEntry))
        {
            cachedEntry.LastAccessTime = Time.realtimeSinceStartup;
            return cachedEntry.Bundle;
        }

        // 使用 LoadAssetBundleUtil 加载
        AssetBundle bundle = LoadAssetBundleUtil.LoadBundleSync(modName, GetModBasePath());
        if (bundle == null)
        {
            LogUtil.LogError($"ModManager: 加载 Mod 失败: {modName}");
            return null;
        }

        _modCache[modName] = new ModCacheEntry(modName, bundle);
        LogUtil.Log($"ModManager: 成功加载 Mod: {modName}");
        return bundle;
    }

    /// <summary>
    /// 同步加载 Mod 中的单个资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <param name="assetName">资源名称</param>
    /// <returns>加载的资源，失败返回 null</returns>
    public T LoadAssetSync<T>(string modName, string assetName) where T : UnityEngine.Object
    {
        EnsureInitialized();

        T asset = LoadAssetBundleUtil.LoadAssetSync<T>(modName, assetName, GetModBasePath());
        if (asset != null && _modCache.TryGetValue(modName, out ModCacheEntry entry))
        {
            entry.LoadedAssets.Add(asset);
            entry.LastAccessTime = Time.realtimeSinceStartup;
        }
        return asset;
    }

    /// <summary>
    /// 同步加载 Mod 中的所有资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <returns>资源数组，失败返回 null</returns>
    public T[] LoadAllAssetsSync<T>(string modName) where T : UnityEngine.Object
    {
        EnsureInitialized();

        T[] assets = LoadAssetBundleUtil.LoadAllAssetsSync<T>(modName, GetModBasePath());
        if (assets != null && _modCache.TryGetValue(modName, out ModCacheEntry entry))
        {
            entry.LoadedAssets.AddRange(assets);
            entry.LastAccessTime = Time.realtimeSinceStartup;
        }
        return assets;
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
        EnsureInitialized();

        if (string.IsNullOrEmpty(modName))
        {
            onFail?.Invoke("modName 不能为空");
            return;
        }

        // 检查缓存
        if (_modCache.TryGetValue(modName, out ModCacheEntry cachedEntry))
        {
            cachedEntry.LastAccessTime = Time.realtimeSinceStartup;
            onSuccess?.Invoke(cachedEntry.Bundle);
            return;
        }

        // 使用 LoadAssetBundleUtil 异步加载
        LoadAssetBundleUtil.LoadBundleAsync(modName, (bundle) =>
        {
            if (bundle != null)
            {
                _modCache[modName] = new ModCacheEntry(modName, bundle);
                LogUtil.Log($"ModManager: 成功异步加载 Mod: {modName}");
            }
            onSuccess?.Invoke(bundle);
        }, (error) =>
        {
            LogUtil.LogError($"ModManager: 异步加载 Mod 失败: {modName}, {error}");
            onFail?.Invoke(error);
        }, GetModBasePath());
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
        EnsureInitialized();

        LoadAssetBundleUtil.LoadAssetAsync<T>(modName, assetName, (asset) =>
        {
            if (asset != null && _modCache.TryGetValue(modName, out ModCacheEntry entry))
            {
                entry.LoadedAssets.Add(asset);
                entry.LastAccessTime = Time.realtimeSinceStartup;
            }
            onSuccess?.Invoke(asset);
        }, (error) =>
        {
            LogUtil.LogError($"ModManager: 异步加载资源失败: {modName}/{assetName}, {error}");
            onFail?.Invoke(error);
        }, GetModBasePath());
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
        EnsureInitialized();

        LoadAssetBundleUtil.LoadAllAssetsAsync<T>(modName, (assets) =>
        {
            if (assets != null && _modCache.TryGetValue(modName, out ModCacheEntry entry))
            {
                entry.LoadedAssets.AddRange(assets);
                entry.LastAccessTime = Time.realtimeSinceStartup;
            }
            onSuccess?.Invoke(assets);
        }, (error) =>
        {
            LogUtil.LogError($"ModManager: 异步加载所有资源失败: {modName}, {error}");
            onFail?.Invoke(error);
        }, GetModBasePath());
    }

    #endregion

    #region 异步加载（Task/协程方式）

    /// <summary>
    /// 异步加载 Mod Bundle（Task 方式，支持 await）
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <returns>Task 返回 AssetBundle</returns>
    public async Task<AssetBundle> LoadModAsync(string modName)
    {
        EnsureInitialized();

        if (string.IsNullOrEmpty(modName))
        {
            LogUtil.LogError("ModManager: modName 不能为空");
            return null;
        }

        // 检查缓存
        if (_modCache.TryGetValue(modName, out ModCacheEntry cachedEntry))
        {
            cachedEntry.LastAccessTime = Time.realtimeSinceStartup;
            return cachedEntry.Bundle;
        }

        // 使用 LoadAssetBundleUtil 异步加载
        AssetBundle bundle = await LoadAssetBundleUtil.LoadBundleAsync(modName, GetModBasePath());
        if (bundle != null)
        {
            _modCache[modName] = new ModCacheEntry(modName, bundle);
            LogUtil.Log($"ModManager: 成功异步加载 Mod: {modName}");
        }
        return bundle;
    }

    /// <summary>
    /// 异步加载 Mod 中的单个资源（Task 方式，支持 await）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <param name="assetName">资源名称</param>
    /// <returns>Task 返回资源</returns>
    public async Task<T> LoadAssetAsync<T>(string modName, string assetName) where T : UnityEngine.Object
    {
        EnsureInitialized();

        T asset = await LoadAssetBundleUtil.LoadAssetAsync<T>(modName, assetName, GetModBasePath());
        if (asset != null && _modCache.TryGetValue(modName, out ModCacheEntry entry))
        {
            entry.LoadedAssets.Add(asset);
            entry.LastAccessTime = Time.realtimeSinceStartup;
        }
        return asset;
    }

    /// <summary>
    /// 异步加载 Mod 中的所有资源（Task 方式，支持 await）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="modName">Mod 名称</param>
    /// <returns>Task 返回资源数组</returns>
    public async Task<T[]> LoadAllAssetsAsync<T>(string modName) where T : UnityEngine.Object
    {
        EnsureInitialized();

        T[] assets = await LoadAssetBundleUtil.LoadAllAssetsAsync<T>(modName, GetModBasePath());
        if (assets != null && _modCache.TryGetValue(modName, out ModCacheEntry entry))
        {
            entry.LoadedAssets.AddRange(assets);
            entry.LastAccessTime = Time.realtimeSinceStartup;
        }
        return assets;
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
        EnsureInitialized();

        yield return LoadAssetBundleUtil.LoadAssetCoroutine<T>(modName, assetName, (asset) =>
        {
            if (asset != null && _modCache.TryGetValue(modName, out ModCacheEntry entry))
            {
                entry.LoadedAssets.Add(asset);
                entry.LastAccessTime = Time.realtimeSinceStartup;
            }
            onSuccess?.Invoke(asset);
        }, (error) =>
        {
            LogUtil.LogError($"ModManager: 协程加载资源失败: {modName}/{assetName}, {error}");
            onFail?.Invoke(error);
        }, GetModBasePath());
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
        EnsureInitialized();

        yield return LoadAssetBundleUtil.LoadAllAssetsCoroutine<T>(modName, (assets) =>
        {
            if (assets != null && _modCache.TryGetValue(modName, out ModCacheEntry entry))
            {
                entry.LoadedAssets.AddRange(assets);
                entry.LastAccessTime = Time.realtimeSinceStartup;
            }
            onSuccess?.Invoke(assets);
        }, (error) =>
        {
            LogUtil.LogError($"ModManager: 协程加载所有资源失败: {modName}, {error}");
            onFail?.Invoke(error);
        }, GetModBasePath());
    }

    #endregion

    #region 卸载方法

    /// <summary>
    /// 卸载指定 Mod（释放引用计数）
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <param name="unloadAllLoadedObjects">是否卸载已加载的资源实例</param>
    public void UnloadMod(string modName, bool unloadAllLoadedObjects = false)
    {
        if (string.IsNullOrEmpty(modName)) return;

        _modCache.Remove(modName);
        LoadAssetBundleUtil.ReleaseBundle(modName, unloadAllLoadedObjects, GetModBasePath());
        LogUtil.Log($"ModManager: 已卸载 Mod: {modName}");
    }

    /// <summary>
    /// 强制卸载指定 Mod（忽略引用计数）
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <param name="unloadAllLoadedObjects">是否卸载已加载的资源实例</param>
    public void ForceUnloadMod(string modName, bool unloadAllLoadedObjects = true)
    {
        if (string.IsNullOrEmpty(modName)) return;

        _modCache.Remove(modName);
        LoadAssetBundleUtil.ForceReleaseBundle(modName, unloadAllLoadedObjects, GetModBasePath());
        LogUtil.Log($"ModManager: 已强制卸载 Mod: {modName}");
    }

    /// <summary>
    /// 卸载所有 Mod
    /// </summary>
    /// <param name="unloadAllLoadedObjects">是否卸载已加载的资源实例</param>
    public void UnloadAllMods(bool unloadAllLoadedObjects = true)
    {
        _modCache.Clear();
        // 只释放当前 Mod 路径下的 Bundle
        LoadAssetBundleUtil.ReleaseBasePath(GetModBasePath(), unloadAllLoadedObjects);
        LogUtil.Log("ModManager: 已卸载所有 Mod");
    }

    /// <summary>
    /// 清理过期的 Mod 缓存
    /// </summary>
    /// <param name="unloadAllLoadedObjects">是否卸载已加载的资源实例</param>
    public void CleanExpiredCache(bool unloadAllLoadedObjects = false)
    {
        LoadAssetBundleUtil.CleanExpiredCache(unloadAllLoadedObjects);
    }

    #endregion

    #region 查询方法

    /// <summary>
    /// 获取已加载的 Mod 列表
    /// </summary>
    public List<string> GetLoadedModNames()
    {
        return new List<string>(_modCache.Keys);
    }

    /// <summary>
    /// 检查 Mod 是否已加载
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <returns>是否已加载</returns>
    public bool IsModLoaded(string modName)
    {
        return _modCache.ContainsKey(modName);
    }

    /// <summary>
    /// 获取 Mod 的 Bundle 引用计数
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <returns>引用计数</returns>
    public int GetModRefCount(string modName)
    {
        return LoadAssetBundleUtil.GetBundleRefCount(modName, GetModBasePath());
    }

    /// <summary>
    /// 获取 Mod 的依赖列表
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <returns>依赖的 Bundle 名称数组</returns>
    public string[] GetModDependencies(string modName)
    {
        return LoadAssetBundleUtil.GetDependencies(modName, GetModBasePath());
    }

    /// <summary>
    /// 获取指定 Mod 中已加载的资源列表
    /// </summary>
    /// <param name="modName">Mod 名称</param>
    /// <returns>资源列表</returns>
    public List<UnityEngine.Object> GetLoadedAssets(string modName)
    {
        if (_modCache.TryGetValue(modName, out ModCacheEntry entry))
            return new List<UnityEngine.Object>(entry.LoadedAssets);
        return new List<UnityEngine.Object>();
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 获取所有可用的 Mod 文件列表
    /// </summary>
    /// <returns>Mod 文件名列表（不含扩展名）</returns>
    public List<string> GetAvailableMods()
    {
        List<string> mods = new List<string>();
        string basePath = GetModBasePath();

        if (!Directory.Exists(basePath))
            return mods;

        string[] files = Directory.GetFiles(basePath, "*.bundle");
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            mods.Add(fileName);
        }

        return mods;
    }

    /// <summary>
    /// 输出当前 Mod 缓存状态（调试用）
    /// </summary>
    public void LogModCacheStatus()
    {
        LogUtil.Log("===== ModManager 缓存状态 =====");
        LogUtil.Log($"Mod 路径: {GetModBasePath()}");
        LogUtil.Log($"已加载 Mod 数量: {_modCache.Count}");

        foreach (var kvp in _modCache)
        {
            var entry = kvp.Value;
            LogUtil.Log($"[{entry.ModName}] 资源数={entry.LoadedAssets.Count}, 最后访问={entry.LastAccessTime:F1}s");
        }

        LogUtil.Log("================================");
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
            Initialize();
    }

    #endregion
}
