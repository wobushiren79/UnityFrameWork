using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using System.IO;

public class LoadAssetUtil
{
    public static readonly string PathURL = Application.streamingAssetsPath + "/AssetBundles/";

#if UNITY_EDITOR
    /// <summary>
    /// 加载资源-editor可用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public static T LoadAssetAtPathForEditor<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
    }
#endif

#if UNITY_EDITOR
    /// <summary>
    /// 加载资源-editor可用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public static List<T> LoadAllAssetAtPathForEditor<T>(string path) where T : Object
    {
        List<T> listData = new List<T>();
        DirectoryInfo direction = new DirectoryInfo(path);
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].Name.EndsWith(".meta"))
            {
                continue;
            }
            T itemData = LoadAssetAtPathForEditor<T>(path + "/" + files[i].Name);
            listData.Add(itemData);
        }
        return listData;
    }
#endif

    /// <summary>
    /// 同步-加载asset资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetPath">全路径</param>
    /// <param name="objName"></param>
    /// <param name="callBack"></param>
    /// <returns></returns>
    public static T SyncLoadAsset<T>(string assetPath, string objName) where T : Object
    {
        assetPath = assetPath.ToLower();
        assetPath = PathURL + assetPath;
        AssetBundle assetBundle = AssetBundle.LoadFromFile(assetPath);
        T data = assetBundle.LoadAsset<T>(objName);
        assetBundle.Unload(false);
        return data;
    }

    /// <summary>
    /// 同步-加载asset资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetPath"></param>
    /// <param name="listObjName"></param>
    /// <returns></returns>
    public static List<T> SyncLoadAsset<T>(string assetPath, List<string> listObjName) where T : Object
    {
        assetPath = assetPath.ToLower();
        assetPath = PathURL + assetPath;
        AssetBundle assetBundle = AssetBundle.LoadFromFile(assetPath);
        List<T> listData = new List<T>();
        for (int i = 0; i < listObjName.Count; i++)
        {
            string objName = listObjName[i];
            T data = assetBundle.LoadAsset<T>(objName);
            if (data != null)
            {
                listData.Add(data);
            }
        }
        assetBundle.Unload(false);
        return listData;
    }

    /// <summary>
    /// 同步-加载所有asset资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public static List<T> SyncLoadAllAsset<T>(string assetPath) where T : Object
    {
        assetPath = assetPath.ToLower();
        assetPath = PathURL + assetPath;
        AssetBundle assetBundle = AssetBundle.LoadFromFile(assetPath);
        T[] dataArray = assetBundle.LoadAllAssets<T>();
        assetBundle.Unload(false);
        return dataArray.ToList();
    }

    /// <summary>
    /// 同步-加载asset资源 TextAsset类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetPath"></param>
    /// <param name="objName"></param>
    /// <param name="image"></param>
    public static TextAsset SyncLoadAssetToBytes(string assetPath, string objName)
    {
        TextAsset textAsset = SyncLoadAsset<TextAsset>(assetPath, objName);
        return textAsset;
    }



    /// <summary>
    /// 同步-加载asset TextAsset 资源并设置图片
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetPath"></param>
    /// <param name="objName"></param>
    /// <param name="image"></param>
    public static Texture2D SyncLoadAssetToBytesForTexture2D(string assetPath, string objName)
    {
        TextAsset textAsset = SyncLoadAssetToBytes(assetPath, objName);
        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(textAsset.bytes);
        return texture;
    }



    /// <summary>
    /// 异步加载asset资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetPath"></param>
    /// <param name="objName"></param>
    /// <param name="callBack"></param>
    /// <returns></returns>
    public static IEnumerator AsyncLoadAsset<T>(string assetPath, string objName, ILoadCallBack<T> callBack) where T : UnityEngine.Object
    {
        assetPath = assetPath.ToLower();
        assetPath = PathURL + assetPath;
        AssetBundleCreateRequest assetRequest = AssetBundle.LoadFromFileAsync(assetPath);
        yield return assetRequest;
        if (assetRequest == null && callBack != null)
            callBack.LoadFail("加载失败：指定assetPath下没有该资源");
        AssetBundleRequest objRequest = assetRequest.assetBundle.LoadAssetAsync<T>(objName);
        yield return objRequest;
        assetRequest.assetBundle.Unload(false);
        if (objRequest == null && callBack != null)
            callBack.LoadFail("加载失败：指定assetPath下没有该名字的obj");
        T obj = objRequest.asset as T;
        if (obj != null && callBack != null)
            callBack.LoadSuccess(obj);
    }

    /// <summary>
    /// 异步加载aaset资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetPath"></param>
    /// <param name="objName"></param>
    /// <param name="callBackSuccess"></param>
    /// <param name="callBackFail"></param>
    /// <returns></returns>
    public static IEnumerator AsyncLoadAsset<T>(string assetPath, string objName, System.Action<T> callBackSuccess, System.Action<string> callBackFail = null) where T : Object
    {
        assetPath = assetPath.ToLower();
        assetPath = PathURL + assetPath;
        AssetBundleCreateRequest assetRequest = AssetBundle.LoadFromFileAsync(assetPath);
        yield return assetRequest;
        if (assetRequest == null)
        {
            callBackFail?.Invoke("加载失败：" + assetPath + "路径不存在");
        }
        else
        {
            AssetBundleRequest objRequest = assetRequest.assetBundle.LoadAssetAsync<T>(objName);
            yield return objRequest;
            if (objRequest == null)
            {
                callBackFail?.Invoke("加载失败：" + assetPath + "中没有名字为" + objName + "的资源");
            }
            else
            {
                T asset = objRequest.asset as T;
                callBackSuccess?.Invoke(asset);
            }
            assetRequest.assetBundle.Unload(false);
        }
    }

    public static IEnumerator AsyncLoadAsset<T>(string assetPath, List<string> listObjName, System.Action<List<T>> callBackSuccess, System.Action<string> callBackFail = null) where T : Object
    {
        assetPath = assetPath.ToLower();
        assetPath = PathURL + assetPath;
        AssetBundleCreateRequest assetRequest = AssetBundle.LoadFromFileAsync(assetPath);
        yield return assetRequest;
        if (assetRequest == null)
        {
            callBackFail?.Invoke("加载失败：" + assetPath + "路径不存在");
        }
        else
        {
            List<T> listData = new List<T>();
            for (int i = 0; i < listObjName.Count; i++)
            {
                string objName = listObjName[i];
                AssetBundleRequest objRequest = assetRequest.assetBundle.LoadAssetAsync<T>(objName);
                yield return objRequest;
                if (objRequest != null)
                {
                    listData.Add(objRequest.asset as T);
                }
            }
            callBackSuccess?.Invoke(listData);
            assetRequest.assetBundle.Unload(false);
        }
    }

    public static IEnumerator AsyncLoadAllAsset<T>(string assetPath, string objName, System.Action<List<T>> callBackSuccess, System.Action<string> callBackFail = null) where T : Object
    {
        assetPath = assetPath.ToLower();
        assetPath = PathURL + assetPath;
        AssetBundleCreateRequest assetRequest = AssetBundle.LoadFromFileAsync(assetPath);
        yield return assetRequest;
        if (assetRequest == null)
        {
            callBackFail?.Invoke("加载失败：" + assetPath + "路径不存在");
        }
        else
        {
            AssetBundleRequest objRequest = assetRequest.assetBundle.LoadAllAssetsAsync<T>();
            yield return objRequest;
            if (objRequest == null)
            {
                callBackFail?.Invoke("加载失败：" + assetPath + "中没有名字为" + objName + "的资源");
            }
            else
            {
                Object[] objArray = objRequest.allAssets;
                List<T> list = new List<T>();
                for (int i = 0; i < objArray.Length; i++)
                {
                    Object itemObj = objArray[i];
                    list.Add(itemObj as T);
                }
                T asset = objRequest.asset as T;
                callBackSuccess?.Invoke(list);
            }
            assetRequest.assetBundle.Unload(false);
        }
    }
}