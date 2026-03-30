using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// AssetBundle 直接加载资源管理工具类
/// 支持同时加载多个 Bundle 根路径（basePath）下的资源
/// 每个 basePath 拥有独立的 Bundle 缓存和引用计数，互不干扰
/// 通过 AssetBundle.LoadFromFile 直接加载 Bundle 文件
/// 适配 Unity 6.3
///
/// 注意：所有方法必须在 Unity 主线程调用，不支持多线程并发访问
///
/// 使用方式：加载/卸载时传入对应的 basePath
///   LoadAssetSync&lt;T&gt;(bundleName, assetName, basePath: "路径/")
///   LoadBundleAsync(bundleName, basePath: "路径/")
/// 不传 basePath 时使用默认根路径（StreamingAssets/AssetBundles/）
/// </summary>
public static class LoadAssetBundleUtil
{
    /// <summary>
    /// Bundle 缓存条目
    /// </summary>
    private class BundleCacheEntry
    {
        public AssetBundle Bundle;
        public int RefCount;
        public float LastAccessTime;

        public BundleCacheEntry(AssetBundle bundle)
        {
            Bundle = bundle;
            RefCount = 1;
            LastAccessTime = Time.realtimeSinceStartup;
        }

        public void Retain()
        {
            RefCount++;
            LastAccessTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// 引用计数减1，返回true表示已归零
        /// </summary>
        public bool Release()
        {
            RefCount--;
            return RefCount <= 0;
        }
    }

    // 默认 Bundle 根路径（懒加载，避免静态构造时 Unity API 尚未就绪）
    private static string _defaultBasePath;

    // Bundle 缓存："{basePath}|{bundleName}" -> CacheEntry
    private static readonly Dictionary<string, BundleCacheEntry> _bundleCache = new Dictionary<string, BundleCacheEntry>();

    // 正在加载中的 Bundle（防止重复加载）：key = "{basePath}|{bundleName}"
    private static readonly HashSet<string> _loadingBundles = new HashSet<string>();

    // 异步加载等待队列：key = "{basePath}|{bundleName}" -> 等待回调列表
    private static readonly Dictionary<string, List<Action<AssetBundle>>> _waitingCallbacks = new Dictionary<string, List<Action<AssetBundle>>>();

    // ReleaseAll/ReleaseBasePath 版本号，用于丢弃 Release 之后异步任务写入的陈旧结果
    private static int _releaseVersion;

    // 缓存过期时间（秒）
    private static float _cacheExpireTime = 300f;

    #region 工具方法

    /// <summary>
    /// 将 Unity AsyncOperation 转换为 Task
    /// </summary>
    private static Task<AssetBundleCreateRequest> ToTask(AssetBundleCreateRequest request)
    {
        var tcs = new TaskCompletionSource<AssetBundleCreateRequest>();
        request.completed += _ => tcs.TrySetResult(request);
        return tcs.Task;
    }

    /// <summary>
    /// 将 Unity AssetBundleRequest 转换为 Task
    /// </summary>
    private static Task<AssetBundleRequest> ToTask(AssetBundleRequest request)
    {
        var tcs = new TaskCompletionSource<AssetBundleRequest>();
        request.completed += _ => tcs.TrySetResult(request);
        return tcs.Task;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return _defaultBasePath ??= $"{Application.streamingAssetsPath}/AssetBundles/";
        return $"{path.TrimEnd('/', '\\')}/";
    }

    private static string CacheKey(string normalizedBasePath, string bundleName)
    {
        return $"{normalizedBasePath}|{bundleName}";
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 设置默认 Bundle 根路径（默认为 StreamingAssets/AssetBundles/）
    /// </summary>
    public static void SetBundleBasePath(string path)
    {
        _defaultBasePath = NormalizePath(path);
    }

    /// <summary>
    /// 获取默认 Bundle 根路径
    /// </summary>
    public static string GetBundleBasePath()
    {
        return _defaultBasePath;
    }

    /// <summary>
    /// 设置缓存过期时间（秒）
    /// </summary>
    public static void SetCacheExpireTime(float seconds)
    {
        _cacheExpireTime = seconds;
    }

    /// <summary>
    /// 获取缓存过期时间（秒）
    /// </summary>
    public static float GetCacheExpireTime()
    {
        return _cacheExpireTime;
    }

    #endregion

    #region 同步加载

    /// <summary>
    /// 同步加载 Bundle（带缓存、引用计数）
    /// basePath 可选，不同 basePath 的 Bundle 完全独立缓存
    /// </summary>
    public static AssetBundle LoadBundleSync(string bundleName, string basePath = null)
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            LogUtil.LogError("LoadAssetBundleUtil: bundleName 不能为空");
            return null;
        }

        string bp = NormalizePath(basePath);
        bundleName = bundleName.ToLower();
        string cacheKey = CacheKey(bp, bundleName);

        if (_bundleCache.TryGetValue(cacheKey, out BundleCacheEntry entry))
        {
            entry.Retain();
            return entry.Bundle;
        }

        string fullPath = $"{bp}{bundleName}";
        AssetBundle bundle = AssetBundle.LoadFromFile(fullPath);
        if (bundle == null)
        {
            LogUtil.LogError($"LoadAssetBundleUtil: Bundle 加载失败: {fullPath}");
            return null;
        }

        _bundleCache[cacheKey] = new BundleCacheEntry(bundle);
        return bundle;
    }

    /// <summary>
    /// 同步加载 Bundle 中的单个资源
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static T LoadAssetSync<T>(string bundleName, string assetName, string basePath = null) where T : UnityEngine.Object
    {
        AssetBundle bundle = LoadBundleSync(bundleName, basePath);
        if (bundle == null) return null;
        var target = bundle.GetAllAssetNames();
        T asset = bundle.LoadAsset<T>(assetName);
        if (asset == null)
            LogUtil.LogError($"LoadAssetBundleUtil: 资源不存在: {bundleName}/{assetName}");
        return asset;
    }

    /// <summary>
    /// 同步加载 Bundle 中的所有资源
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static T[] LoadAllAssetsSync<T>(string bundleName, string basePath = null) where T : UnityEngine.Object
    {
        AssetBundle bundle = LoadBundleSync(bundleName, basePath);
        if (bundle == null) return null;
        return bundle.LoadAllAssets<T>();
    }

    /// <summary>
    /// 同步加载 Bundle 中的多个指定资源
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static List<T> LoadAssetsSync<T>(string bundleName, List<string> assetNames, string basePath = null) where T : UnityEngine.Object
    {
        AssetBundle bundle = LoadBundleSync(bundleName, basePath);
        if (bundle == null) return null;

        List<T> result = new List<T>(assetNames.Count);
        for (int i = 0; i < assetNames.Count; i++)
        {
            T asset = bundle.LoadAsset<T>(assetNames[i]);
            if (asset != null) result.Add(asset);
        }
        return result;
    }

    #endregion

    #region 异步加载（回调方式）

    /// <summary>
    /// 异步加载 Bundle（回调方式，带缓存、防重复）
    /// basePath 可选，不同 basePath 的 Bundle 完全独立缓存
    /// </summary>
    public static async void LoadBundleAsync(string bundleName, Action<AssetBundle> onSuccess, Action<string> onFail = null, string basePath = null)
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            onFail?.Invoke("bundleName 不能为空");
            return;
        }
        try
        {
            AssetBundle bundle = await LoadBundleAsync(bundleName, basePath);
            if (bundle != null)
                onSuccess?.Invoke(bundle);
            else
                onFail?.Invoke($"Bundle 加载失败: {bundleName}");
        }
        catch (Exception ex)
        {
            onFail?.Invoke($"Bundle 加载异常: {bundleName}, {ex.Message}");
        }
    }

    /// <summary>
    /// 异步加载 Bundle 中的单个资源（回调方式）
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static void LoadAssetAsync<T>(string bundleName, string assetName, Action<T> onSuccess, Action<string> onFail = null, string basePath = null) where T : UnityEngine.Object
    {
        LoadBundleAsync(bundleName, (bundle) =>
        {
            var request = bundle.LoadAssetAsync<T>(assetName);
            request.completed += (_) =>
            {
                T asset = request.asset as T;
                if (asset != null)
                    onSuccess?.Invoke(asset);
                else
                {
                    string error = $"资源不存在: {bundleName}/{assetName}";
                    LogUtil.LogError($"LoadAssetBundleUtil: {error}");
                    onFail?.Invoke(error);
                }
            };
        }, onFail, basePath);
    }

    /// <summary>
    /// 异步加载 Bundle 中的所有资源（回调方式）
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static void LoadAllAssetsAsync<T>(string bundleName, Action<T[]> onSuccess, Action<string> onFail = null, string basePath = null) where T : UnityEngine.Object
    {
        LoadBundleAsync(bundleName, (bundle) =>
        {
            var request = bundle.LoadAllAssetsAsync<T>();
            request.completed += (op) =>
            {
                UnityEngine.Object[] allAssets = request.allAssets;
                T[] result = new T[allAssets.Length];
                for (int i = 0; i < allAssets.Length; i++)
                    result[i] = allAssets[i] as T;
                onSuccess?.Invoke(result);
            };
        }, onFail, basePath);
    }

    /// <summary>
    /// 通知等待回调
    /// retainPerCallback=true：为每个等待者调用 Retain（适用于等待者不会自行 Retain 的场景，如依赖等待）
    /// retainPerCallback=false：不额外 Retain（适用于 Task 等待者会自行 Retain 的场景）
    /// </summary>
    private static void NotifyWaitingCallbacks(string cacheKey, AssetBundle bundle, bool retainPerCallback = true)
    {
        if (_waitingCallbacks.TryGetValue(cacheKey, out List<Action<AssetBundle>> callbacks))
        {
            if (retainPerCallback && bundle != null && _bundleCache.TryGetValue(cacheKey, out BundleCacheEntry entry))
            {
                for (int i = 0; i < callbacks.Count; i++)
                    entry.Retain();
            }

            for (int i = 0; i < callbacks.Count; i++)
                callbacks[i]?.Invoke(bundle);
            _waitingCallbacks.Remove(cacheKey);
        }
    }

    #endregion

    #region 异步加载（Task 方式）

    /// <summary>
    /// 异步加载 Bundle（async/await 方式）
    /// basePath 可选，不同 basePath 的 Bundle 完全独立缓存
    /// </summary>
    public static async Task<AssetBundle> LoadBundleAsync(string bundleName, string basePath = null)
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            LogUtil.LogError("LoadAssetBundleUtil: bundleName 不能为空");
            return null;
        }

        int capturedVersion = _releaseVersion;
        string bp = NormalizePath(basePath);
        bundleName = bundleName.ToLower();
        string cacheKey = CacheKey(bp, bundleName);

        if (_bundleCache.TryGetValue(cacheKey, out BundleCacheEntry entry))
        {
            entry.Retain();
            return entry.Bundle;
        }

        if (_loadingBundles.Contains(cacheKey))
        {
            AssetBundle waitedBundle = await WaitForBundleLoaded(cacheKey);
            if (waitedBundle != null && _bundleCache.TryGetValue(cacheKey, out BundleCacheEntry waitedEntry))
                waitedEntry.Retain();
            return waitedBundle;
        }

        _loadingBundles.Add(cacheKey);

        try
        {
            string fullPath = $"{bp}{bundleName}";
            var request = AssetBundle.LoadFromFileAsync(fullPath);
            await ToTask(request);

            _loadingBundles.Remove(cacheKey);
            AssetBundle bundle = request.assetBundle;

            if (bundle != null)
            {
                if (_releaseVersion != capturedVersion)
                {
                    bundle.Unload(true);
                    NotifyWaitingCallbacks(cacheKey, null, retainPerCallback: false);
                    return null;
                }
                _bundleCache[cacheKey] = new BundleCacheEntry(bundle);
                NotifyWaitingCallbacks(cacheKey, bundle, retainPerCallback: false);
                return bundle;
            }
            else
            {
                LogUtil.LogError($"LoadAssetBundleUtil: Bundle 异步加载失败: {fullPath}");
                NotifyWaitingCallbacks(cacheKey, null, retainPerCallback: false);
                return null;
            }
        }
        catch (Exception ex)
        {
            _loadingBundles.Remove(cacheKey);
            LogUtil.LogError($"LoadAssetBundleUtil: Bundle 异步加载异常: {bundleName}, {ex.Message}");
            NotifyWaitingCallbacks(cacheKey, null, retainPerCallback: false);
            return null;
        }
    }

    /// <summary>
    /// 异步加载 Bundle 中的单个资源（async/await 方式）
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static async Task<T> LoadAssetAsync<T>(string bundleName, string assetName, string basePath = null) where T : UnityEngine.Object
    {
        AssetBundle bundle = await LoadBundleAsync(bundleName, basePath);
        if (bundle == null) return null;

        var request = bundle.LoadAssetAsync<T>(assetName);
        await ToTask(request);

        T asset = request.asset as T;
        if (asset == null)
            LogUtil.LogError($"LoadAssetBundleUtil: 资源不存在: {bundleName}/{assetName}");
        return asset;
    }

    /// <summary>
    /// 异步加载 Bundle 中的所有资源（async/await 方式）
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static async Task<T[]> LoadAllAssetsAsync<T>(string bundleName, string basePath = null) where T : UnityEngine.Object
    {
        AssetBundle bundle = await LoadBundleAsync(bundleName, basePath);
        if (bundle == null) return null;

        var request = bundle.LoadAllAssetsAsync<T>();
        await ToTask(request);

        UnityEngine.Object[] allAssets = request.allAssets;
        T[] result = new T[allAssets.Length];
        for (int i = 0; i < allAssets.Length; i++)
            result[i] = allAssets[i] as T;
        return result;
    }

    private static Task<AssetBundle> WaitForBundleLoaded(string cacheKey)
    {
        var tcs = new TaskCompletionSource<AssetBundle>();
        if (!_waitingCallbacks.ContainsKey(cacheKey))
            _waitingCallbacks[cacheKey] = new List<Action<AssetBundle>>();
        _waitingCallbacks[cacheKey].Add((bundle) => tcs.TrySetResult(bundle));
        return tcs.Task;
    }

#endregion

    #region 协程加载

    /// <summary>
    /// 协程等待 Bundle 加载完成
    /// </summary>
    private static IEnumerator WaitForBundleLoadingCoroutine(string cacheKey, Action<AssetBundle> onComplete)
    {
        while (_loadingBundles.Contains(cacheKey))
            yield return null;

        _bundleCache.TryGetValue(cacheKey, out BundleCacheEntry entry);
        onComplete?.Invoke(entry?.Bundle);
    }

    /// <summary>
    /// 协程方式异步加载 Bundle 中的单个资源
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static IEnumerator LoadAssetCoroutine<T>(string bundleName, string assetName, Action<T> onSuccess, Action<string> onFail = null, string basePath = null) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            onFail?.Invoke("bundleName 不能为空");
            yield break;
        }

        string bp = NormalizePath(basePath);
        bundleName = bundleName.ToLower();
        string cacheKey = CacheKey(bp, bundleName);

        if (_bundleCache.TryGetValue(cacheKey, out BundleCacheEntry cacheEntry))
        {
            cacheEntry.Retain();
            var cachedRequest = cacheEntry.Bundle.LoadAssetAsync<T>(assetName);
            yield return cachedRequest;
            T cachedAsset = cachedRequest.asset as T;
            if (cachedAsset != null) onSuccess?.Invoke(cachedAsset);
            else onFail?.Invoke($"资源不存在: {bundleName}/{assetName}");
            yield break;
        }

        if (_loadingBundles.Contains(cacheKey))
        {
            AssetBundle waitedBundle = null;
            yield return WaitForBundleLoadingCoroutine(cacheKey, (b) => waitedBundle = b);
            if (waitedBundle != null && _bundleCache.TryGetValue(cacheKey, out BundleCacheEntry waitedEntry))
            {
                waitedEntry.Retain();
                var cachedRequest = waitedBundle.LoadAssetAsync<T>(assetName);
                yield return cachedRequest;
                T cachedAsset = cachedRequest.asset as T;
                if (cachedAsset != null) onSuccess?.Invoke(cachedAsset);
                else onFail?.Invoke($"资源不存在: {bundleName}/{assetName}");
            }
            else
            {
                onFail?.Invoke($"Bundle 加载失败（等待中）: {bundleName}");
            }
            yield break;
        }

        _loadingBundles.Add(cacheKey);

        string fullPath = $"{bp}{bundleName}";
        var bundleRequest = AssetBundle.LoadFromFileAsync(fullPath);
        yield return bundleRequest;
        _loadingBundles.Remove(cacheKey);

        AssetBundle bundle = bundleRequest.assetBundle;
        if (bundle == null)
        {
            NotifyWaitingCallbacks(cacheKey, null, retainPerCallback: false);
            onFail?.Invoke($"Bundle 加载失败: {fullPath}");
            yield break;
        }

        _bundleCache[cacheKey] = new BundleCacheEntry(bundle);
        NotifyWaitingCallbacks(cacheKey, bundle, retainPerCallback: false);

        var assetRequest = bundle.LoadAssetAsync<T>(assetName);
        yield return assetRequest;

        T asset = assetRequest.asset as T;
        if (asset != null) onSuccess?.Invoke(asset);
        else onFail?.Invoke($"资源不存在: {bundleName}/{assetName}");
    }

    /// <summary>
    /// 协程方式异步加载 Bundle 中的所有资源
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static IEnumerator LoadAllAssetsCoroutine<T>(string bundleName, Action<T[]> onSuccess, Action<string> onFail = null, string basePath = null) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            onFail?.Invoke("bundleName 不能为空");
            yield break;
        }

        string bp = NormalizePath(basePath);
        bundleName = bundleName.ToLower();
        string cacheKey = CacheKey(bp, bundleName);

        if (_bundleCache.TryGetValue(cacheKey, out BundleCacheEntry cacheEntry))
        {
            cacheEntry.Retain();
            var cachedRequest = cacheEntry.Bundle.LoadAllAssetsAsync<T>();
            yield return cachedRequest;
            UnityEngine.Object[] allCached = cachedRequest.allAssets;
            T[] cachedResult = new T[allCached.Length];
            for (int i = 0; i < allCached.Length; i++)
                cachedResult[i] = allCached[i] as T;
            onSuccess?.Invoke(cachedResult);
            yield break;
        }

        if (_loadingBundles.Contains(cacheKey))
        {
            AssetBundle waitedBundle = null;
            yield return WaitForBundleLoadingCoroutine(cacheKey, (b) => waitedBundle = b);
            if (waitedBundle != null && _bundleCache.TryGetValue(cacheKey, out BundleCacheEntry waitedEntry))
            {
                waitedEntry.Retain();
                var cachedRequest = waitedBundle.LoadAllAssetsAsync<T>();
                yield return cachedRequest;
                UnityEngine.Object[] allCached = cachedRequest.allAssets;
                T[] cachedResult = new T[allCached.Length];
                for (int i = 0; i < allCached.Length; i++)
                    cachedResult[i] = allCached[i] as T;
                onSuccess?.Invoke(cachedResult);
            }
            else
            {
                onFail?.Invoke($"Bundle 加载失败（等待中）: {bundleName}");
            }
            yield break;
        }

        _loadingBundles.Add(cacheKey);

        string fullPath = $"{bp}{bundleName}";
        var bundleRequest = AssetBundle.LoadFromFileAsync(fullPath);
        yield return bundleRequest;
        _loadingBundles.Remove(cacheKey);

        AssetBundle bundle = bundleRequest.assetBundle;
        if (bundle == null)
        {
            NotifyWaitingCallbacks(cacheKey, null, retainPerCallback: false);
            onFail?.Invoke($"Bundle 加载失败: {fullPath}");
            yield break;
        }

        _bundleCache[cacheKey] = new BundleCacheEntry(bundle);
        NotifyWaitingCallbacks(cacheKey, bundle, retainPerCallback: false);

        var assetRequest = bundle.LoadAllAssetsAsync<T>();
        yield return assetRequest;

        UnityEngine.Object[] assets = assetRequest.allAssets;
        T[] result = new T[assets.Length];
        for (int i = 0; i < assets.Length; i++)
            result[i] = assets[i] as T;
        onSuccess?.Invoke(result);
    }

    #endregion

    #region 资源卸载

    /// <summary>
    /// 释放 Bundle（引用计数减1，归零时卸载）
    /// basePath 需与加载时一致
    /// </summary>
    public static void ReleaseBundle(string bundleName, bool unloadAllLoadedObjects = false, string basePath = null)
    {
        if (string.IsNullOrEmpty(bundleName)) return;

        string bp = NormalizePath(basePath);
        bundleName = bundleName.ToLower();
        string cacheKey = CacheKey(bp, bundleName);

        if (_bundleCache.TryGetValue(cacheKey, out BundleCacheEntry entry))
        {
            if (entry.Release())
            {
                if (entry.Bundle != null)
                    entry.Bundle.Unload(unloadAllLoadedObjects);
                _bundleCache.Remove(cacheKey);
            }
        }
    }

    /// <summary>
    /// 强制卸载 Bundle（忽略引用计数）
    /// basePath 需与加载时一致
    /// </summary>
    public static void ForceReleaseBundle(string bundleName, bool unloadAllLoadedObjects = true, string basePath = null)
    {
        if (string.IsNullOrEmpty(bundleName)) return;

        string bp = NormalizePath(basePath);
        bundleName = bundleName.ToLower();
        string cacheKey = CacheKey(bp, bundleName);

        if (_bundleCache.TryGetValue(cacheKey, out BundleCacheEntry entry))
        {
            if (entry.Bundle != null)
                entry.Bundle.Unload(unloadAllLoadedObjects);
            _bundleCache.Remove(cacheKey);
        }
    }

    /// <summary>
    /// 释放指定 basePath 下的所有 Bundle
    /// </summary>
    public static void ReleaseBasePath(string basePath, bool unloadAllLoadedObjects = true)
    {
        string bp = NormalizePath(basePath);
        string prefix = $"{bp}|";

        List<string> bundleKeys = new();
        foreach (var key in _bundleCache.Keys)
        {
            if (key.StartsWith(prefix))
                bundleKeys.Add(key);
        }
        for (int i = 0; i < bundleKeys.Count; i++)
        {
            if (_bundleCache.TryGetValue(bundleKeys[i], out BundleCacheEntry entry))
            {
                if (entry.Bundle != null)
                    entry.Bundle.Unload(unloadAllLoadedObjects);
                _bundleCache.Remove(bundleKeys[i]);
            }
        }

        List<string> loadingKeys = new();
        foreach (var key in _loadingBundles)
        {
            if (key.StartsWith(prefix)) loadingKeys.Add(key);
        }
        for (int i = 0; i < loadingKeys.Count; i++)
            _loadingBundles.Remove(loadingKeys[i]);

        List<string> waitingKeys = new();
        foreach (var key in _waitingCallbacks.Keys)
        {
            if (key.StartsWith(prefix)) waitingKeys.Add(key);
        }
        // 通知等待中的回调（传 null），防止 Task 永久挂起
        for (int i = 0; i < waitingKeys.Count; i++)
        {
            if (_waitingCallbacks.TryGetValue(waitingKeys[i], out var callbacks))
            {
                for (int j = 0; j < callbacks.Count; j++)
                    callbacks[j]?.Invoke(null);
            }
            _waitingCallbacks.Remove(waitingKeys[i]);
        }

        // 递增版本号，使所有正在进行的异步加载丢弃结果，防止写回已清空的缓存
        _releaseVersion++;

        LogUtil.Log($"LoadAssetBundleUtil: 已释放 basePath [{bp}] 下的所有 Bundle");
    }

/// <summary>
    /// 清理所有引用计数为0且超过过期时间的缓存
    /// 注意：此方法有一定开销，建议低频调用（如每隔数分钟调用一次），不应每帧调用
    /// </summary>
    public static void CleanExpiredCache(bool unloadAllLoadedObjects = false)
    {
        float currentTime = Time.realtimeSinceStartup;

        List<string> keysToRemove = new();
        foreach (var kvp in _bundleCache)
        {
            if (kvp.Value.RefCount <= 0
                && (currentTime - kvp.Value.LastAccessTime) > _cacheExpireTime)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            string key = keysToRemove[i];
            if (_bundleCache.TryGetValue(key, out BundleCacheEntry entry))
            {
                if (entry.Bundle != null)
                    entry.Bundle.Unload(unloadAllLoadedObjects);
                _bundleCache.Remove(key);
            }
        }

        if (keysToRemove.Count > 0)
            LogUtil.Log($"LoadAssetBundleUtil: 清理了 {keysToRemove.Count} 个过期缓存");
    }

    /// <summary>
    /// 释放所有 basePath 下的所有缓存 Bundle
    /// </summary>
    public static void ReleaseAll(bool unloadAllLoadedObjects = true)
    {
        // 递增版本号，使所有正在进行的异步加载完成后丢弃结果，防止写回已清空的字典
        _releaseVersion++;

        foreach (var kvp in _bundleCache)
        {
            if (kvp.Value.Bundle != null)
                kvp.Value.Bundle.Unload(unloadAllLoadedObjects);
        }
        _bundleCache.Clear();
        _loadingBundles.Clear();

        // 通知所有等待者（传 null），避免 TaskCompletionSource 永久挂起
        foreach (var callbacks in _waitingCallbacks.Values)
            foreach (var cb in callbacks)
                cb?.Invoke(null);
        _waitingCallbacks.Clear();

        LogUtil.Log("LoadAssetBundleUtil: 已释放所有缓存 Bundle");
    }

    #endregion

    #region 查询与调试

    /// <summary>
    /// Bundle 是否已缓存（basePath 需与加载时一致）
    /// </summary>
    public static bool IsBundleCached(string bundleName, string basePath = null)
    {
        if (string.IsNullOrEmpty(bundleName)) return false;
        return _bundleCache.ContainsKey(CacheKey(NormalizePath(basePath), bundleName.ToLower()));
    }

    /// <summary>
    /// Bundle 是否正在加载中（basePath 需与加载时一致）
    /// </summary>
    public static bool IsBundleLoading(string bundleName, string basePath = null)
    {
        if (string.IsNullOrEmpty(bundleName)) return false;
        return _loadingBundles.Contains(CacheKey(NormalizePath(basePath), bundleName.ToLower()));
    }

    /// <summary>
    /// 获取 Bundle 引用计数（basePath 需与加载时一致）
    /// </summary>
    public static int GetBundleRefCount(string bundleName, string basePath = null)
    {
        if (string.IsNullOrEmpty(bundleName)) return 0;
        if (_bundleCache.TryGetValue(CacheKey(NormalizePath(basePath), bundleName.ToLower()), out BundleCacheEntry entry))
            return entry.RefCount;
        return 0;
    }

    /// <summary>
    /// 获取当前所有 basePath 下缓存的 Bundle 总数
    /// </summary>
    public static int GetCacheCount()
    {
        return _bundleCache.Count;
    }

    /// <summary>
    /// 输出当前缓存状态（调试用）
    /// </summary>
    public static void LogCacheStatus()
    {
        LogUtil.Log("===== LoadAssetBundleUtil 缓存状态 =====");
        LogUtil.Log($"缓存 Bundle 总数: {_bundleCache.Count}, 加载中: {_loadingBundles.Count}");

        foreach (var kvp in _bundleCache)
        {
            LogUtil.Log($"[{kvp.Key}] RefCount={kvp.Value.RefCount}, LastAccess={kvp.Value.LastAccessTime:F1}s, Valid={kvp.Value.Bundle != null}");
        }

        LogUtil.Log("========================================");
    }

    #endregion
}
