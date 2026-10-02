using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

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
    /// 记录Mod的JsonText文件
    /// key: modName, value: 该Mod下JsonText文件夹中的txt文件名集合（不含扩展名）
    /// </summary>
    private Dictionary<string, HashSet<string>> dicModJsonTextFiles = new Dictionary<string, HashSet<string>>();

    /// <summary>
    /// 强制全部Mod视为开启（测试模式用：LauncherTest 启动时置 true，FilterEnabledMods 跳过 GameConfig.listModEnable 过滤；
    /// 仅内存标记不持久化，正式游戏 LauncherGame 不置位、不受影响的设置项与存档不变）
    /// </summary>
    public bool isForceAllModsEnabled = false;

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
    /// 过滤出已开启的Mod：以 GameConfig.listModEnable 为准，未记录的Mod（含新Mod）默认关闭不加载；
    /// isForceAllModsEnabled=true（测试模式）时跳过过滤全量加载
    /// </summary>
    protected List<string> FilterEnabledMods(List<string> modNames)
    {
        if (modNames.Count == 0)
            return modNames;
        //测试模式强制全开（LauncherTest 启动时置位，仅内存不持久化）：卡片/幻化药等测试直接用全部Mod，免逐一手动开启+重启
        if (isForceAllModsEnabled)
        {
            LogUtil.Log($"[Mod] 测试模式强制全开，跳过开启过滤，共 {modNames.Count} 个Mod");
            return modNames;
        }
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        var enabledMods = new List<string>();
        foreach (var modName in modNames)
        {
            if (gameConfig.IsModEnable(modName))
                enabledMods.Add(modName);
            else
                LogUtil.Log($"[Mod] Mod未开启，跳过加载: {modName}");
        }
        return enabledMods;
    }

    /// <summary>
    /// 初始化所有已开启的Mod：扫描ModRoot目录，加载开启状态Mod的Catalog并记录资源Key（异步回调）
    /// </summary>
    public void InitializeAllMods(Action<bool> callBack)
    {
        var availableMods = FilterEnabledMods(GetAvailableModNames());
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
            ScanModJsonTextFiles(modName);
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
    /// 初始化所有已开启的Mod：扫描ModRoot目录，加载开启状态Mod的Catalog并记录资源Key（异步await）
    /// </summary>
    public async Task<bool> InitializeAllModsAsync()
    {
        var availableMods = FilterEnabledMods(GetAvailableModNames());
        if (availableMods.Count == 0)
        {
            LogUtil.Log("[Mod] 未发现可用Mod");
            return true;
        }

        bool allSuccess = true;
        foreach (var modName in availableMods)
        {
            ScanModJsonTextFiles(modName);
            bool success = await LoadModCatalogAsync(modName);
            if (!success) allSuccess = false;
        }

        LogUtil.Log($"[Mod] 初始化完成，共处理 {availableMods.Count} 个Mod，成功: {allSuccess}");
        return allSuccess;
    }

    /// <summary>
    /// 初始化所有已开启的Mod：扫描ModRoot目录，加载开启状态Mod的Catalog并记录资源Key（同步）
    /// </summary>
    public bool InitializeAllModsSync()
    {
        var availableMods = FilterEnabledMods(GetAvailableModNames());
        if (availableMods.Count == 0)
        {
            LogUtil.Log("[Mod] 未发现可用Mod");
            return true;
        }

        bool allSuccess = true;
        foreach (var modName in availableMods)
        {
            ScanModJsonTextFiles(modName);
            bool success = LoadModCatalogSync(modName);
            if (!success) allSuccess = false;
        }

        LogUtil.Log($"[Mod] 初始化完成，共处理 {availableMods.Count} 个Mod，成功: {allSuccess}");
        return allSuccess;
    }

    #endregion

    #region InternalId 转换钩子（相对路径还原 + monoscripts Bundle 去重）

    /// <summary>monoscripts Bundle 去重表：Bundle文件名（含内容哈希，同名即同内容）→ 首个加载的完整InternalId</summary>
    private static readonly Dictionary<string, string> s_MonoScriptBundleCanonicalIds = new Dictionary<string, string>();

    /// <summary>已打印过共享日志的monoscripts Bundle文件名（避免重复刷日志）</summary>
    private static readonly HashSet<string> s_MonoScriptBundleSharedLogged = new HashSet<string>();

    /// <summary>InternalId 转换钩子是否已安装</summary>
    private static bool s_IsInternalIdTransformInstalled;

    /// <summary>InternalId路径分隔符（可能是URL或含两种分隔符的相对/绝对路径）</summary>
    private static readonly char[] s_PathSeparators = { '/', '\\' };

    /// <summary>
    /// 安装 InternalId 转换钩子（幂等，在每次加载Mod Catalog前调用）。钩子链依次执行：
    /// ① 相对路径还原（ResolveRelativeBundlePath）：Mod Catalog 中的 Bundle InternalId 若是相对路径
    ///    （如 Mods\Xxx\a.bundle），转为以游戏根目录为基准的绝对路径，避免打包后按进程工作目录解析失败；
    /// ② monoscripts Bundle 去重（DedupMonoScriptBundleId）：多个Mod用同一构建环境（如同一Spine版本）构建时
    ///    会产出内容完全相同的monoscripts Bundle，而Unity禁止两个不同Bundle包含相同资产文件（MonoScript的GUID相同），
    ///    后加载者报"another AssetBundle with the same files is already loaded"并导致该Mod资源加载失败；
    ///    通过InternalIdTransformFunc把同名Bundle重定向到首个已加载实例的InternalId，
    ///    Addressables的AssetBundleProvider按转换后ID作缓存键，直接复用已加载Bundle，规避冲突。
    /// </summary>
    private static void EnsureInternalIdTransformInstalled()
    {
        if (s_IsInternalIdTransformInstalled)
            return;
        s_IsInternalIdTransformInstalled = true;

        // InternalIdTransformFunc为全局单点，链式保留已有转换（当前工程无其他设置者，防御性处理）
        var existing = Addressables.InternalIdTransformFunc;
        Addressables.InternalIdTransformFunc = (location) =>
        {
            string id = existing != null ? existing(location) : location?.InternalId;
            id = ResolveRelativeBundlePath(id);
            return DedupMonoScriptBundleId(id);
        };
    }

    /// <summary>
    /// 相对路径还原：相对路径的Bundle文件InternalId转为以游戏根目录（Application.dataPath/..）为基准的绝对路径。
    /// 原因：相对路径按进程当前工作目录(CWD)解析——编辑器下CWD=项目根恰好可用；打包后CWD取决于启动方式
    /// （快捷方式起始位置/启动器/命令行所在目录），不保证是exe目录，导致找不到Bundle
    /// （Unable to open archive file / Invalid path in AssetBundleProvider）。
    /// 注意：仅处理 .bundle 结尾的文件路径——InternalIdTransformFunc 对所有 location 生效，
    /// BundledAssetProvider 会把转换结果当作 Bundle 内资源名去 LoadAssetAsync（见其 InternalOp 第108/158行），
    /// 若误改 Bundle 内资源路径（如 Assets/Xxx.prefab）会导致按名取资源失败（Unable to load asset of type ...）。
    /// </summary>
    private static string ResolveRelativeBundlePath(string internalId)
    {
        if (string.IsNullOrEmpty(internalId))
            return internalId;
        //仅处理Bundle文件路径：Bundle内资源路径/运行时变量({UnityEngine.Application.XXX})/网络地址(含://)/绝对路径一律原样放行
        if (!internalId.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase)
            || internalId[0] == '{' || internalId.Contains("://") || Path.IsPathRooted(internalId))
            return internalId;
        try
        {
            string gameRoot = Path.Combine(Application.dataPath, "..");
            return Path.GetFullPath(Path.Combine(gameRoot, internalId)).Replace("\\", "/");
        }
        catch (Exception)
        {
            //含非法字符等异常情况保持原样，交由后续加载流程按原逻辑报错
            return internalId;
        }
    }

    /// <summary>
    /// monoscripts Bundle去重：同名Bundle重定向到首个已加载实例的InternalId，首次出现则登记为规范来源
    /// </summary>
    private static string DedupMonoScriptBundleId(string internalId)
    {
        if (string.IsNullOrEmpty(internalId) || !internalId.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase))
            return internalId;

        int sepIndex = internalId.LastIndexOfAny(s_PathSeparators);
        string fileName = sepIndex >= 0 ? internalId.Substring(sepIndex + 1) : internalId;
        if (!fileName.Contains("_monoscripts_"))
            return internalId;

        if (s_MonoScriptBundleCanonicalIds.TryGetValue(fileName, out string canonicalId))
        {
            if (string.Equals(canonicalId, internalId, StringComparison.Ordinal))
                return internalId;
            if (s_MonoScriptBundleSharedLogged.Add(fileName))
                LogUtil.Log($"[Mod] monoscripts Bundle内容相同，共享已加载实例: {fileName}");
            return canonicalId;
        }

        s_MonoScriptBundleCanonicalIds[fileName] = internalId;
        return internalId;
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
        EnsureInternalIdTransformInstalled();
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
                        RebuildModIds();
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
        EnsureInternalIdTransformInstalled();
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
                RebuildModIds();
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
        EnsureInternalIdTransformInstalled();
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
                RebuildModIds();
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

        dicModJsonTextFiles.Remove(modName);
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

        dicModJsonTextFiles.Clear();
        dicModAssetKeys.Clear();
        modIdMap.Clear();
        occupiedModIds.Clear();
        // 注意：不删除持久化的ModIdMap.json，保证下次加载时旧ModID不变
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

    #region JsonText

    /// <summary>
    /// 扫描指定Mod目录下的JsonText文件夹，记录txt文件名（不含扩展名）
    /// </summary>
    private void ScanModJsonTextFiles(string modName)
    {
        string jsonTextPath = Path.Combine(GetModPath(modName), "JsonText").Replace("\\", "/");
        if (!Directory.Exists(jsonTextPath))
            return;

        var files = new HashSet<string>();
        foreach (var filePath in Directory.GetFiles(jsonTextPath, "*.txt"))
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            files.Add(fileNameWithoutExt);
        }

        if (files.Count > 0)
            dicModJsonTextFiles[modName] = files;
    }

    /// <summary>
    /// modId上限（含）。约束来自BaseCfg.CombineModId：组合id = modId*10^14 + selfId（selfId最大14位），
    /// 92232是保证任意14位selfId都不溢出long的最大值（92232*10^14+99999999999999 = 9223299999999999999 &lt; long.MaxValue = 9223372036854775807）
    /// </summary>
    public const int MaxModId = 92232;

    /// <summary>
    /// Mod名称到已分配modId的映射（运行时内存缓存）
    /// </summary>
    private Dictionary<string, int> modIdMap = new Dictionary<string, int>();

    /// <summary>
    /// 已占用的modId集合（运行时内存缓存，用于冲突检测）
    /// </summary>
    private HashSet<int> occupiedModIds = new HashSet<int>();

    /// <summary>
    /// 根据当前已加载的所有Mod重新构建modId映射，按Mod名称排序后统一分配。
    /// 通过GameDataManager持久化映射保证：同一modName跨会话始终获得相同ID、
    /// 新增Mod不会导致旧ModID变化。
    /// </summary>
    private void RebuildModIds()
    {
        ModIdMapBean modIdMapBean = null;
        if (GameDataHandler.Instance != null && GameDataHandler.Instance.manager != null)
            modIdMapBean = GameDataHandler.Instance.manager.GetModIdMap();

        if (modIdMapBean != null && modIdMapBean.modIdMap != null)
        {
            modIdMap = new Dictionary<string, int>(modIdMapBean.modIdMap);
            occupiedModIds = new HashSet<int>(modIdMap.Values);
        }
        else
        {
            modIdMap = new Dictionary<string, int>();
            occupiedModIds = new HashSet<int>();
        }

        var loadedModNames = new List<string>(dicModLocators.Keys);
        loadedModNames.Sort(StringComparer.Ordinal);

        bool changed = false;
        foreach (var modName in loadedModNames)
        {
            // 已在持久化映射中的Mod直接保留原ID
            if (modIdMap.ContainsKey(modName))
                continue;

            // 新Mod顺序分配第一个空闲ID
            int finalId = 1;
            while (occupiedModIds.Contains(finalId))
                finalId++;

            // 超过上限拒绝分配：该Mod不进入映射，其JsonText配置不合并（资源仍可按modName加载），避免CombineModId溢出long
            if (finalId > MaxModId)
            {
                LogUtil.LogError($"[Mod] modId已达上限({MaxModId})，新Mod不分配ID、配置不生效: {modName}");
                continue;
            }

            modIdMap[modName] = finalId;
            occupiedModIds.Add(finalId);
            changed = true;
        }

        if (changed)
        {
            if (modIdMapBean == null)
                modIdMapBean = new ModIdMapBean();
            modIdMapBean.modIdMap = new Dictionary<string, int>(modIdMap);
            GameDataHandler.Instance.manager.modIdMapBean = modIdMapBean;
            GameDataHandler.Instance.manager.SaveModIdMap();
        }
    }

    /// <summary>
    /// 获取指定Mod的modId
    /// </summary>
    /// <param name="modName">Mod名称</param>
    /// <returns>modId（1~MaxModId），若未分配（或超限被拒）则返回1</returns>
    public int GetModId(string modName)
    {
        if (modIdMap.TryGetValue(modName, out int modId))
            return modId;
        return 1;
    }

    /// <summary>
    /// 检查是否有已加载的Mod包含指定fileName的JsonText文件
    /// </summary>
    public bool HasModJsonTextFile(string fileName)
    {
        foreach (var files in dicModJsonTextFiles.Values)
        {
            if (files.Contains(fileName))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取包含指定fileName的所有Mod的JsonText文件路径和mod信息
    /// 未分配modId的Mod（如超过MaxModId被拒）直接跳过——不能用GetModId的默认回退1，否则会污染modId=1的号段
    /// </summary>
    public List<(int modId, string modName, string filePath)> GetModJsonTextFileInfos(string fileName)
    {
        var result = new List<(int modId, string modName, string filePath)>();
        foreach (var kvp in dicModJsonTextFiles)
        {
            if (kvp.Value.Contains(fileName))
            {
                string modName = kvp.Key;
                if (!modIdMap.TryGetValue(modName, out int modId))
                    continue;
                string filePath = Path.Combine(GetModPath(modName), "JsonText", $"{fileName}.txt").Replace("\\", "/");
                if (File.Exists(filePath))
                    result.Add((modId, modName, filePath));
            }
        }
        return result;
    }

    #endregion
}
