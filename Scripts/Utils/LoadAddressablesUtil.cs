using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LoadAddressablesUtil
{

    /// <summary>
    /// 同步加载数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T LoadAssetSync<T>(string key)
    {
        var op = Addressables.LoadAssetAsync<T>(key);
        T go = op.WaitForCompletion();
        return go;
    }

    /// <summary>
    /// 根据KEY 异步读取 读取之后还需要实例化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keyName"></param>
    /// <param name="callBack"></param>
    public static void LoadAssetAsync<T>(string keyName, Action<AsyncOperationHandle<T>> callBack)
    {
        Addressables.LoadAssetAsync<T>(keyName).Completed += callBack;
    }

    /// <summary>
    /// 根据KEY 异步读取 读取之后还需要实例化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keyName"></param>
    public static async Task<AsyncOperationHandle<T>> LoadAssetAsync<T>(string keyName)
    {
        try
        {
            // 使用await等待资源加载完成
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(keyName);
            T result = await handle.Task;
            return handle;
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"资源加载失败: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// 根据KEY 异步读取 读取之后还需要实例化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keyName"></param>
    /// <param name="callBack"></param>
    public static void LoadAssetsAsync<T>(string keyName, Action<AsyncOperationHandle<IList<T>>> callBack)
    {
        Addressables.LoadAssetsAsync<T>(keyName, null).Completed += callBack;
    }

    /// <summary>
    /// 根据KEY LIST 异步读取 读取之后还需要实例化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listKey"></param>
    /// <param name="callBack"></param>
    public static void LoadAssetsAsync<T>(List<string> listKey, Action<AsyncOperationHandle<IList<T>>> callBack)
    {
        Addressables.LoadAssetsAsync<T>(listKey, null, Addressables.MergeMode.Intersection).Completed += callBack;
    }
    
    /// <summary>
    /// 根据KEY LIST 异步读取 读取之后还需要实例化
    /// </summary>
    public static async Task<AsyncOperationHandle<IList<T>>> LoadAssetsAsync<T>(List<string> listKey)
    {
        try
        {
            //使用await等待资源加载完成
            AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(listKey, null, Addressables.MergeMode.Intersection);
            IList<T> result = await handle.Task;
            return handle;
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"资源加载失败: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// 根据KEY 异步读取并且实例化对象
    /// </summary>
    /// <param name="keyName"></param>
    /// <param name="callBack"></param>
    public static void LoadAssetAndInstantiateAsync(string keyName, Action<AsyncOperationHandle<GameObject>> callBack)
    {
        Addressables.InstantiateAsync(keyName).Completed += callBack;
    }

    /// <summary>
    /// 销毁对象
    /// </summary>
    /// <param name="obj"></param>
    public static void ReleaseInstance(GameObject obj)
    {
        Addressables.ReleaseInstance(obj);
    }

    /// <summary>
    /// 销毁对象 例子：只能释放句柄AsyncOperationHandle<Material> 
    /// </summary>
    /// <param name="obj"></param>
    public static void Release<T>(T target)
    {
        Addressables.Release(target);
    }
}