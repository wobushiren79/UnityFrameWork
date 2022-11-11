using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadResourcesUtil
{

    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="resPath"> ex:Texture/btn_icon </param>
    /// <returns></returns>
    public static T SyncLoadData<T>(string resPath) where T : Object
    {
        T resData = Resources.Load(resPath, typeof(T)) as T;
        return resData;
    }

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="resPath"></param>
    /// <returns></returns>
    public static IEnumerator AsyncLoadData<T>(string resPath) where T : Object
    {
        T resData = Resources.LoadAsync(resPath, typeof(T)) as T;
        yield return resData;
    }


    /// <summary>
    /// 异步加载资源图片
    /// </summary>
    /// <param name="imagePath"></param>
    /// <param name="image"></param>
    /// <returns></returns>
    public static IEnumerator AsyncLoadDataAndSetImage(string imagePath, Image image)
    {
        ResourceRequest res = Resources.LoadAsync<Sprite>(imagePath);
        yield return res;
        Sprite imageSp = res.asset as Sprite;
        image.sprite = imageSp;
    }

}