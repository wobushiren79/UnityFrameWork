using System;
using System.Collections.Generic;
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
        Addressables.Release(op);
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
    /// <param name="callBack"></param>
    public static void LoadAssetsAsync<T>(string keyName, Action<AsyncOperationHandle<IList<T>>> callBack)
    {
        Addressables.LoadAssetsAsync<T>(keyName,null).Completed += callBack;
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
}