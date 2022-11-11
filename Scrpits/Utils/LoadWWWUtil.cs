using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections;
using UnityEngine.Networking;

public class LoadWWWUtil 
{

    /// <summary>
    /// 同步-WWW加载
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="resPath"> 本地_ex: file://E:/Texture/icon_1.jpg 网络_ex:http://www.baidu.com </param>
    /// <returns></returns>
    //public static WWW SyncLoadData(string httpPath)
    //{
    //    WWW www = new WWW(httpPath);
    //    return www;
    //}
    public static UnityWebRequest SyncLoadData(string httpPath)
    {
        UnityWebRequest webRequest = new UnityWebRequest(httpPath);
        return webRequest;
    }

    /// <summary>
    /// 异步-WWW加载
    /// </summary>
    /// <param name="resPath"> 本地_ex: file://E:/Texture/icon_1.jpg 网络_ex:http://www.baidu.com </param>
    /// <returns></returns>
    //public static IEnumerator AsyncLoadData(string resPath, ILoadCallBack<WWW> callBack)
    //{
    //    WWW www = new WWW(resPath);
    //    yield return www;
    //    if (callBack != null)
    //        callBack.LoadSuccess(www);
    //}
    public static IEnumerator AsyncLoadData(string resPath, ILoadCallBack<UnityWebRequest> callBack)
    {
        UnityWebRequest webRequest = new UnityWebRequest(resPath);
        yield return webRequest;
        if (callBack != null)
            callBack.LoadSuccess(webRequest);
    }


    /// <summary>
    /// 异步-WWW加载 获取Sprite
    /// </summary>
    /// <param name="resPath"> 本地_ex: file://E:/Texture/icon_1.jpg 网络_ex:http://www.baidu.com </param>
    /// <returns></returns>
    //public static IEnumerator AsyncLoadDataToSprite(string resPath, ILoadCallBack<Sprite> callBack)
    //{
    //    WWW www = new WWW(resPath);
    //    yield return www;
    //    Sprite imgSp = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
    //    if (callBack != null)
    //        callBack.LoadSuccess(imgSp);
    //}



    /// <summary>
    /// 异步-WWW加载 获取Sprite
    /// </summary>
    /// <param name="resPath"> 本地_ex: file://E:/Texture/icon_1.jpg 网络_ex:http://www.baidu.com </param>
    /// <returns></returns>
    //public static IEnumerator AsyncLoadDataAndSetImage(string resPath, Image image,ILoadCallBack<Image> callBack)
    //{
    //    WWW www = new WWW(resPath);
    //    yield return www;
    //    image.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
    //    if (callBack != null)
    //        callBack.LoadSuccess(image);
    //}
    //public static IEnumerator AsyncLoadDataAndSetImage(string resPath, Image image)
    //{
    //     return AsyncLoadDataAndSetImage(resPath, image, null);
    //}
}