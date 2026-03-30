using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// AssetBundle 直接加载资源管理工具类
/// 支持同时加载多个 Bundle 根路径（basePath）下的资源
/// 每个 basePath 拥有独立的 Manifest、Bundle 缓存和引用计数，互不干扰
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

    // 每个 basePath 的 Manifest：key = normalizedBasePath
    private static readonly Dictionary<string, AssetBundleManifest> _manifests = new();
    private static readonly Dictionary<string, AssetBundle> _manifestBundles = new();

    // Fix4: 防止 InitManifestAsync 并发重复加载同一路径
    private static readonly Dictionary<string, Task<bool>> _pendingManifestTasks = new();

    // 防止协程并发重复加载同一路径的 Manifest
    private static readonly HashSet<string> _loadingManifests = new HashSet<string>();

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

    private static string GetManifestName(string normalizedPath)
    {
        string name = System.IO.Path.GetFileName(normalizedPath.TrimEnd('/', '\\'));
        return string.IsNullOrEmpty(name) ? "AssetBundles" : name;
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

    /// <summary>
    /// 初始化指定 basePath 的 Manifest
    /// manifestBundleName 为空时自动使用路径目录名（如 "AssetBundles"）
    /// basePath 为空时使用默认根路径
    /// </summary>
    public static bool InitManifest(string manifestBundleName = null, string basePath = null)
    {
        string normalizedPath = NormalizePath(basePath);
        if (_manifests.ContainsKey(normalizedPath)) return true;

        if (string.IsNullOrEmpty(manifestBundleName))
            manifestBundleName = GetManifestName(normalizedPath);

        string manifestPath = $"{normalizedPath}{manifestBundleName}";
        try
        {
            AssetBundle mb = AssetBundle.LoadFromFile(manifestPath);
            if (mb == null)
            {
                LogUtil.LogError($"LoadAssetBundleUtil: Manifest Bundle 加载失败: {manifestPath}");
                return false;
            }

            AssetBundleManifest m = mb.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (m == null)
            {
                LogUtil.LogError("LoadAssetBundleUtil: AssetBundleManifest 资源不存在");
                mb.Unload(true);
                return false;
            }

            _manifests[normalizedPath] = m;
            _manifestBundles[normalizedPath] = mb;
            return true;
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"LoadAssetBundleUtil: Manifest 初始化异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 异步初始化指定 basePath 的 Manifest
    /// Fix4: 防止并发重复加载——同路径并发调用共享同一个 Task
    /// </summary>
    public static Task<bool> InitManifestAsync(string manifestBundleName = null, string basePath = null)
    {
        string normalizedPath = NormalizePath(basePath);
        if (_manifests.ContainsKey(normalizedPath)) return Task.FromResult(true);

        // Fix4: 已有进行中的加载任务，直接返回相同 Task，避免重复加载
        if (_pendingManifestTasks.TryGetValue(normalizedPath, out Task<bool> pending))
            return pending;

        if (string.IsNullOrEmpty(manifestBundleName))
            manifestBundleName = GetManifestName(normalizedPath);

        var task = DoInitManifestAsync(manifestBundleName, normalizedPath);
        _pendingManifestTasks[normalizedPath] = task;
        // 任务完成后从 pending 表中移除（无论成功或失败）
        task.ContinueWith(_ => _pendingManifestTasks.Remove(normalizedPath),
            TaskScheduler.FromCurrentSynchronizationContext());
        return task;
    }

    private static async Task<bool> DoInitManifestAsync(string manifestBundleName, string normalizedPath)
    {
        int capturedVersion = _releaseVersion;
        string manifestPath = $"{normalizedPath}{manifestBundleName}";
        try
        {
            var request = AssetBundle.LoadFromFileAsync(manifestPath);
            await ToTask(request);

            AssetBundle mb = request.assetBundle;
            if (mb == null)
            {
                LogUtil.LogError($"LoadAssetBundleUtil: Manifest Bundle 异步加载失败: {manifestPath}");
                return false;
            }

            var assetRequest = mb.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
            await ToTask(assetRequest);

            // 检查加载期间是否已触发 ReleaseAll/ReleaseBasePath，若是则丢弃结果避免泄漏
            if (_releaseVersion != capturedVersion)
            {
                mb.Unload(true);
                return false;
            }

            AssetBundleManifest m = assetRequest.asset as AssetBundleManifest;
            if (m == null)
            {
                LogUtil.LogError("LoadAssetBundleUtil: AssetBundleManifest 资源不存在");
                mb.Unload(true);
                return false;
            }

            _manifests[normalizedPath] = m;
            _manifestBundles[normalizedPath] = mb;
            return true;
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"LoadAssetBundleUtil: Manifest 异步初始化异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 确保 basePath 已初始化（同步），返回规范化路径
    /// </summary>
    private static string EnsureInitialized(string basePath)
    {
        string normalizedPath = NormalizePath(basePath);
        if (!_manifests.ContainsKey(normalizedPath))
            InitManifest(null, normalizedPath);
        return normalizedPath;
    }

    /// <summary>
    /// 确保 basePath 已初始化（异步），返回规范化路径
    /// </summary>
    private static async Task<string> EnsureInitializedAsync(string basePath)
    {
        string normalizedPath = NormalizePath(basePath);
        if (!_manifests.ContainsKey(normalizedPath))
            await InitManifestAsync(null, normalizedPath);
        return normalizedPath;
    }

    #endregion

    #region 同步加载

    /// <summary>
    /// 同步加载 Bundle（带缓存、依赖、引用计数）
    /// basePath 可选，不同 basePath 的 Bundle 完全独立缓存
    /// </summary>
    public static AssetBundle LoadBundleSync(string bundleName, string basePath = null)
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            LogUtil.LogError("LoadAssetBundleUtil: bundleName 不能为空");
            return null;
        }

        string bp = EnsureInitialized(basePath);
        bundleName = bundleName.ToLower();
        string cacheKey = CacheKey(bp, bundleName);

        if (_bundleCache.TryGetValue(cacheKey, out BundleCacheEntry entry))
        {
            entry.Retain();
            return entry.Bundle;
        }

        if (!LoadDependenciesSync(bundleName, bp))
        {
            LogUtil.LogError($"LoadAssetBundleUtil: Bundle 依赖加载失败: {bundleName}");
            return null;
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

    /// <summary>
    /// 同步加载依赖，返回是否全部成功；失败时回滚已加载的依赖
    /// </summary>
    private static bool LoadDependenciesSync(string bundleName, string bp)
    {
        if (!_manifests.TryGetValue(bp, out AssetBundleManifest manifest)) return true;

        string[] dependencies = manifest.GetAllDependencies(bundleName);
        List<string> loadedDepKeys = new(dependencies.Length);

        for (int i = 0; i < dependencies.Length; i++)
        {
            string dep = dependencies[i].ToLower();
            string depKey = CacheKey(bp, dep);

            if (_bundleCache.TryGetValue(depKey, out BundleCacheEntry entry))
            {
                entry.Retain();
                loadedDepKeys.Add(depKey);
                continue;
            }

            string depPath = $"{bp}{dep}";
            AssetBundle depBundle = AssetBundle.LoadFromFile(depPath);
            if (depBundle != null)
            {
                _bundleCache[depKey] = new BundleCacheEntry(depBundle);
                loadedDepKeys.Add(depKey);
            }
            else
            {
                LogUtil.LogError($"LoadAssetBundleUtil: 依赖 Bundle 加载失败: {depPath}");
                // 回滚：释放已成功加载的依赖，避免内存泄漏
                for (int j = 0; j < loadedDepKeys.Count; j++)
                    ReleaseSingleDepEntry(loadedDepKeys[j]);
                return false;
            }
        }
        return true;
    }

    #endregion

    #region 异步加载（回调方式）

    /// <summary>
    /// 异步加载 Bundle（回调方式，带缓存、依赖、防重复）
    /// Fix5: 内部委托给 Task 版本，统一使用异步 Manifest 初始化，消除重复实现
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
        string bp = await EnsureInitializedAsync(basePath);
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
            // WaitForBundleLoaded 通过 NotifyWaitingCallbacks(retainPerCallback:false) 获得 bundle
            // 这里需要自行 Retain
            if (waitedBundle != null && _bundleCache.TryGetValue(cacheKey, out BundleCacheEntry waitedEntry))
                waitedEntry.Retain();
            return waitedBundle;
        }

        _loadingBundles.Add(cacheKey);

        try
        {
            await LoadDependenciesAsync(bundleName, bp);

            string fullPath = $"{bp}{bundleName}";
            var request = AssetBundle.LoadFromFileAsync(fullPath);
            await ToTask(request);

            _loadingBundles.Remove(cacheKey);
            AssetBundle bundle = request.assetBundle;

            if (bundle != null)
            {
                // 若加载期间触发了 ReleaseAll/ReleaseBasePath，丢弃结果防止写入已清空的缓存
                if (_releaseVersion != capturedVersion)
                {
                    bundle.Unload(true);
                    NotifyWaitingCallbacks(cacheKey, null, retainPerCallback: false);
                    return null;
                }
                _bundleCache[cacheKey] = new BundleCacheEntry(bundle);
                // Task 等待者自行 Retain，不需要 NotifyWaitingCallbacks 重复 Retain
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

    private static async Task LoadDependenciesAsync(string bundleName, string bp)
    {
        if (!_manifests.TryGetValue(bp, out AssetBundleManifest manifest)) return;

        string[] dependencies = manifest.GetAllDependencies(bundleName);
        if (dependencies.Length == 0) return;

        List<string> retainedKeys = new(dependencies.Length);
        List<Task> tasks = new(dependencies.Length);
        List<string> taskKeys = new(dependencies.Length);

        for (int i = 0; i < dependencies.Length; i++)
        {
            string dep = dependencies[i].ToLower();
            string depKey = CacheKey(bp, dep);

            if (_bundleCache.TryGetValue(depKey, out BundleCacheEntry entry))
            {
                entry.Retain();
                retainedKeys.Add(depKey);
                continue;
            }

            if (_loadingBundles.Contains(depKey))
            {
                tasks.Add(WaitForBundleLoadedTask(depKey));
                taskKeys.Add(depKey);
                continue;
            }

            _loadingBundles.Add(depKey);
            tasks.Add(LoadSingleDependencyAsync(dep, bp, depKey));
            taskKeys.Add(depKey);
        }

        if (tasks.Count == 0) return;

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // 回滚：释放已直接 Retain 的依赖，避免内存泄漏
            for (int i = 0; i < retainedKeys.Count; i++)
                ReleaseSingleDepEntry(retainedKeys[i]);
            // 回滚任务中已成功完成（已 Retain）的依赖
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].IsCompletedSuccessfully)
                    ReleaseSingleDepEntry(taskKeys[i]);
            }
            throw;
        }
    }

    private static async Task LoadSingleDependencyAsync(string dep, string bp, string depKey)
    {
        int capturedVersion = _releaseVersion;
        string depPath = $"{bp}{dep}";
        AssetBundle depBundle;
        try
        {
            var request = AssetBundle.LoadFromFileAsync(depPath);
            await ToTask(request);
            depBundle = request.assetBundle;
        }
        catch (Exception ex)
        {
            _loadingBundles.Remove(depKey);
            LogUtil.LogError($"LoadAssetBundleUtil: 依赖加载异常: {dep}, {ex.Message}");
            NotifyWaitingCallbacks(depKey, null);
            throw;
        }

        _loadingBundles.Remove(depKey);

        if (depBundle == null)
        {
            LogUtil.LogError($"LoadAssetBundleUtil: 依赖 Bundle 异步加载失败: {depPath}");
            NotifyWaitingCallbacks(depKey, null);
            throw new Exception($"依赖 Bundle 加载失败: {depPath}");
        }

        // 若加载期间触发了 ReleaseAll/ReleaseBasePath，丢弃结果防止写入已清空的缓存
        if (_releaseVersion != capturedVersion)
        {
            depBundle.Unload(true);
            NotifyWaitingCallbacks(depKey, null);
            throw new Exception($"路径已释放，依赖加载取消: {depPath}");
        }

        _bundleCache[depKey] = new BundleCacheEntry(depBundle);
        NotifyWaitingCallbacks(depKey, depBundle);
    }

    private static Task<AssetBundle> WaitForBundleLoaded(string cacheKey)
    {
        var tcs = new TaskCompletionSource<AssetBundle>();
        if (!_waitingCallbacks.ContainsKey(cacheKey))
            _waitingCallbacks[cacheKey] = new List<Action<AssetBundle>>();
        _waitingCallbacks[cacheKey].Add((bundle) => tcs.TrySetResult(bundle));
        return tcs.Task;
    }

    private static async Task WaitForBundleLoadedTask(string cacheKey)
    {
        var bundle = await WaitForBundleLoaded(cacheKey);
        if (bundle == null)
            throw new Exception($"Bundle 加载失败: {cacheKey}");
    }

    #endregion

    #region 协程加载

    /// <summary>
    /// Fix5: 协程方式确保 Manifest 已初始化，onResult(true) 表示成功
    /// Fix: 增加并发保护——若同路径已有协程在加载则轮询等待，防止重复加载与泄漏
    /// </summary>
    private static IEnumerator EnsureManifestCoroutine(string normalizedPath, Action<bool> onResult)
    {
        if (_manifests.ContainsKey(normalizedPath))
        {
            onResult?.Invoke(true);
            yield break;
        }

        // 已有协程在加载同一路径，轮询等待其完成
        if (_loadingManifests.Contains(normalizedPath))
        {
            while (_loadingManifests.Contains(normalizedPath))
                yield return null;
            onResult?.Invoke(_manifests.ContainsKey(normalizedPath));
            yield break;
        }

        _loadingManifests.Add(normalizedPath);

        string manifestBundleName = GetManifestName(normalizedPath);
        string manifestPath = $"{normalizedPath}{manifestBundleName}";

        var request = AssetBundle.LoadFromFileAsync(manifestPath);
        yield return request;

        AssetBundle mb = request.assetBundle;
        if (mb == null)
        {
            LogUtil.LogError($"LoadAssetBundleUtil: Manifest Bundle 加载失败: {manifestPath}");
            _loadingManifests.Remove(normalizedPath);
            onResult?.Invoke(false);
            yield break;
        }

        var assetRequest = mb.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
        yield return assetRequest;

        AssetBundleManifest m = assetRequest.asset as AssetBundleManifest;
        if (m == null)
        {
            LogUtil.LogError("LoadAssetBundleUtil: AssetBundleManifest 资源不存在");
            mb.Unload(true);
            _loadingManifests.Remove(normalizedPath);
            onResult?.Invoke(false);
            yield break;
        }

        _manifests[normalizedPath] = m;
        _manifestBundles[normalizedPath] = mb;
        _loadingManifests.Remove(normalizedPath);
        onResult?.Invoke(true);
    }

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
    /// 协程方式加载依赖（共用），onResult 回调参数 true 表示成功
    /// Fix2: 依赖加载成功后调用 NotifyWaitingCallbacks，避免 Task 等待者永久挂起
    /// </summary>
    private static IEnumerator LoadDependenciesCoroutine(string bundleName, string bp, Action<bool> onResult)
    {
        if (!_manifests.TryGetValue(bp, out AssetBundleManifest manifest))
        {
            onResult?.Invoke(true);
            yield break;
        }

        string[] deps = manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < deps.Length; i++)
        {
            string dep = deps[i].ToLower();
            string depKey = CacheKey(bp, dep);
            if (_bundleCache.TryGetValue(depKey, out BundleCacheEntry depEntry))
            {
                depEntry.Retain();
                continue;
            }
            if (_loadingBundles.Contains(depKey))
            {
                AssetBundle depBundle = null;
                yield return WaitForBundleLoadingCoroutine(depKey, (b) => depBundle = b);
                if (depBundle != null && _bundleCache.TryGetValue(depKey, out BundleCacheEntry waitedDepEntry))
                    waitedDepEntry.Retain();
                else if (depBundle == null)
                {
                    LogUtil.LogError($"LoadAssetBundleUtil: 依赖 Bundle 协程等待失败: {dep}");
                    onResult?.Invoke(false);
                    yield break;
                }
                continue;
            }
            _loadingBundles.Add(depKey);
            string depPath = $"{bp}{dep}";
            var depRequest = AssetBundle.LoadFromFileAsync(depPath);
            yield return depRequest;
            _loadingBundles.Remove(depKey);
            if (depRequest.assetBundle != null)
            {
                _bundleCache[depKey] = new BundleCacheEntry(depRequest.assetBundle);
                // Fix2: 通知可能正在等待此依赖的 Task 等待者
                NotifyWaitingCallbacks(depKey, depRequest.assetBundle);
            }
            else
            {
                LogUtil.LogError($"LoadAssetBundleUtil: 依赖 Bundle 协程加载失败: {depPath}");
                NotifyWaitingCallbacks(depKey, null);
                onResult?.Invoke(false);
                yield break;
            }
        }
        onResult?.Invoke(true);
    }

    /// <summary>
    /// 协程方式异步加载 Bundle 中的单个资源
    /// Fix1: 加载完成后调用 NotifyWaitingCallbacks，避免 Task 等待者永久挂起
    /// Fix5: 使用 EnsureManifestCoroutine 替代同步 EnsureInitialized
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static IEnumerator LoadAssetCoroutine<T>(string bundleName, string assetName, Action<T> onSuccess, Action<string> onFail = null, string basePath = null) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            onFail?.Invoke("bundleName 不能为空");
            yield break;
        }

        // Fix5: 协程方式异步初始化 Manifest
        string bp = NormalizePath(basePath);
        bool manifestOk = false;
        yield return EnsureManifestCoroutine(bp, (ok) => manifestOk = ok);
        if (!manifestOk)
        {
            onFail?.Invoke($"Manifest 初始化失败: {basePath}");
            yield break;
        }

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

        bool depLoadOk = false;
        yield return LoadDependenciesCoroutine(bundleName, bp, (ok) => depLoadOk = ok);
        if (!depLoadOk)
        {
            _loadingBundles.Remove(cacheKey);
            NotifyWaitingCallbacks(cacheKey, null, retainPerCallback: false);
            onFail?.Invoke($"Bundle 依赖加载失败: {bundleName}");
            yield break;
        }

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
        // Fix1: 通知可能正在等待此 Bundle 的 Task 等待者（Task 等待者自行 Retain）
        NotifyWaitingCallbacks(cacheKey, bundle, retainPerCallback: false);

        var assetRequest = bundle.LoadAssetAsync<T>(assetName);
        yield return assetRequest;

        T asset = assetRequest.asset as T;
        if (asset != null) onSuccess?.Invoke(asset);
        else onFail?.Invoke($"资源不存在: {bundleName}/{assetName}");
    }

    /// <summary>
    /// 协程方式异步加载 Bundle 中的所有资源
    /// Fix1: 加载完成后调用 NotifyWaitingCallbacks，避免 Task 等待者永久挂起
    /// Fix5: 使用 EnsureManifestCoroutine 替代同步 EnsureInitialized
    /// 注意：每次调用会增加 Bundle 引用计数，使用完毕后须调用 ReleaseBundle 释放
    /// </summary>
    public static IEnumerator LoadAllAssetsCoroutine<T>(string bundleName, Action<T[]> onSuccess, Action<string> onFail = null, string basePath = null) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            onFail?.Invoke("bundleName 不能为空");
            yield break;
        }

        // Fix5: 协程方式异步初始化 Manifest
        string bp = NormalizePath(basePath);
        bool manifestOk = false;
        yield return EnsureManifestCoroutine(bp, (ok) => manifestOk = ok);
        if (!manifestOk)
        {
            onFail?.Invoke($"Manifest 初始化失败: {basePath}");
            yield break;
        }

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

        bool depLoadOk = false;
        yield return LoadDependenciesCoroutine(bundleName, bp, (ok) => depLoadOk = ok);
        if (!depLoadOk)
        {
            _loadingBundles.Remove(cacheKey);
            NotifyWaitingCallbacks(cacheKey, null, retainPerCallback: false);
            onFail?.Invoke($"Bundle 依赖加载失败: {bundleName}");
            yield break;
        }

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
        // Fix1: 通知可能正在等待此 Bundle 的 Task 等待者（Task 等待者自行 Retain）
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
                ReleaseDependencies(bundleName, bp, unloadAllLoadedObjects);
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
            ForceReleaseDependencies(bundleName, bp, unloadAllLoadedObjects);
        }
    }

    /// <summary>
    /// 释放指定 basePath 下的所有 Bundle 和 Manifest
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

        if (_manifestBundles.TryGetValue(bp, out AssetBundle mb))
        {
            if (mb != null) mb.Unload(true);
            _manifestBundles.Remove(bp);
        }
        _manifests.Remove(bp);

        // 递增版本号，使所有正在进行的异步加载（Manifest 与 Bundle）丢弃结果，防止写回已清空的缓存
        // 注意：_releaseVersion 为全局版本，此操作会同时取消其他 basePath 下正在进行的加载
        _releaseVersion++;
        _pendingManifestTasks.Remove(bp);
        _loadingManifests.Remove(bp);

        LogUtil.Log($"LoadAssetBundleUtil: 已释放 basePath [{bp}] 下的所有 Bundle");
    }

    private static void ReleaseDependencies(string bundleName, string bp, bool unloadAllLoadedObjects)
    {
        if (!_manifests.TryGetValue(bp, out AssetBundleManifest manifest)) return;

        string[] dependencies = manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < dependencies.Length; i++)
        {
            string depKey = CacheKey(bp, dependencies[i].ToLower());
            if (_bundleCache.TryGetValue(depKey, out BundleCacheEntry depEntry))
            {
                if (depEntry.Release())
                {
                    if (depEntry.Bundle != null)
                        depEntry.Bundle.Unload(unloadAllLoadedObjects);
                    _bundleCache.Remove(depKey);
                }
            }
        }
    }

    /// <summary>
    /// 强制释放依赖（忽略引用计数，直接卸载）
    /// </summary>
    private static void ForceReleaseDependencies(string bundleName, string bp, bool unloadAllLoadedObjects)
    {
        if (!_manifests.TryGetValue(bp, out AssetBundleManifest manifest)) return;

        string[] dependencies = manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < dependencies.Length; i++)
        {
            string depKey = CacheKey(bp, dependencies[i].ToLower());
            if (_bundleCache.TryGetValue(depKey, out BundleCacheEntry depEntry))
            {
                if (depEntry.Bundle != null)
                    depEntry.Bundle.Unload(unloadAllLoadedObjects);
                _bundleCache.Remove(depKey);
            }
        }
    }

    /// <summary>
    /// 对单个依赖条目执行引用计数释放，归零时卸载并从缓存移除
    /// </summary>
    private static void ReleaseSingleDepEntry(string depKey)
    {
        if (_bundleCache.TryGetValue(depKey, out BundleCacheEntry entry))
        {
            if (entry.Release())
            {
                if (entry.Bundle != null)
                    entry.Bundle.Unload(false);
                _bundleCache.Remove(depKey);
            }
        }
    }

    /// <summary>
    /// 清理所有引用计数为0且超过过期时间的缓存
    /// 会检查依赖关系，不会清理仍被其他 bundle 依赖的条目
    /// 注意：此方法有一定开销，建议低频调用（如每隔数分钟调用一次），不应每帧调用
    /// </summary>
    public static void CleanExpiredCache(bool unloadAllLoadedObjects = false)
    {
        float currentTime = Time.realtimeSinceStartup;

        // 收集所有仍在使用的 bundle 的依赖，形成保护集合
        HashSet<string> protectedKeys = new();
        foreach (var kvp in _bundleCache)
        {
            if (kvp.Value.RefCount > 0)
            {
                // 解析 cacheKey 获取 basePath 和 bundleName，格式为 "{basePath}|{bundleName}"
                int separatorIndex = kvp.Key.IndexOf('|');
                if (separatorIndex < 0) continue;
                string bp = kvp.Key[..separatorIndex];
                string bn = kvp.Key[(separatorIndex + 1)..];

                if (_manifests.TryGetValue(bp, out AssetBundleManifest manifest))
                {
                    string[] deps = manifest.GetAllDependencies(bn);
                    for (int i = 0; i < deps.Length; i++)
                        protectedKeys.Add(CacheKey(bp, deps[i].ToLower()));
                }
            }
        }

        List<string> keysToRemove = new();
        foreach (var kvp in _bundleCache)
        {
            if (kvp.Value.RefCount <= 0
                && (currentTime - kvp.Value.LastAccessTime) > _cacheExpireTime
                && !protectedKeys.Contains(kvp.Key))
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
    /// 释放所有 basePath 下的所有缓存 Bundle 和 Manifest
    /// Fix3: 先通知所有挂起的 Task 等待者（传 null），防止 async 调用方永久挂起
    /// </summary>
    public static void ReleaseAll(bool unloadAllLoadedObjects = true)
    {
        // 递增版本号，使所有正在进行的异步 Manifest 加载完成后丢弃结果，防止写回已清空的字典
        _releaseVersion++;

        foreach (var kvp in _bundleCache)
        {
            if (kvp.Value.Bundle != null)
                kvp.Value.Bundle.Unload(unloadAllLoadedObjects);
        }
        _bundleCache.Clear();
        _loadingBundles.Clear();
        _loadingManifests.Clear();

        // Fix3: 通知所有等待者（传 null），避免 TaskCompletionSource 永久挂起
        foreach (var callbacks in _waitingCallbacks.Values)
            foreach (var cb in callbacks)
                cb?.Invoke(null);
        _waitingCallbacks.Clear();

        // 清理 pending 任务引用（任务本身无法取消，_releaseVersion 会阻止其写入结果）
        _pendingManifestTasks.Clear();

        foreach (var kvp in _manifestBundles)
        {
            if (kvp.Value != null)
                kvp.Value.Unload(true);
        }
        _manifests.Clear();
        _manifestBundles.Clear();

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
    /// 获取 Bundle 的依赖列表
    /// </summary>
    public static string[] GetDependencies(string bundleName, string basePath = null)
    {
        if (string.IsNullOrEmpty(bundleName)) return Array.Empty<string>();
        string bp = NormalizePath(basePath);
        if (!_manifests.TryGetValue(bp, out AssetBundleManifest manifest))
            return Array.Empty<string>();
        return manifest.GetAllDependencies(bundleName.ToLower());
    }

    /// <summary>
    /// 输出当前缓存状态（调试用）
    /// </summary>
    public static void LogCacheStatus()
    {
        LogUtil.Log("===== LoadAssetBundleUtil 缓存状态 =====");
        LogUtil.Log($"已加载 Manifest 数量: {_manifests.Count}");
        foreach (var bp in _manifests.Keys)
            LogUtil.Log($"  Manifest: {bp}");
        LogUtil.Log($"缓存 Bundle 总数: {_bundleCache.Count}, 加载中: {_loadingBundles.Count}");

        foreach (var kvp in _bundleCache)
        {
            LogUtil.Log($"[{kvp.Key}] RefCount={kvp.Value.RefCount}, LastAccess={kvp.Value.LastAccessTime:F1}s, Valid={kvp.Value.Bundle != null}");
        }

        LogUtil.Log("========================================");
    }

    #endregion
}
