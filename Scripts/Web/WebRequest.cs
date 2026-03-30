using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class WebRequest
{
    public IEnumerator Get<T>(string https, Dictionary<string, string> mapData, IWebRequestCallBack<T> callBack)
    {
        string data = "";
        if (mapData != null && mapData.Count != 0)
        {
            data += "?";
            foreach (var itemData in mapData)
            {
                data += (itemData.Key + "=" + itemData.Value + "&");
            }
        }

        UnityWebRequest webRequest = UnityWebRequest.Get(https + data);
        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ProtocolError || webRequest.result == UnityWebRequest.Result.ConnectionError)
        {

            callBack.WebRequestGetFail(https, webRequest.error);
        }
        else
        {
            callBack.WebRequestGetSuccess(https, JsonUtil.FromJson<T>(webRequest.downloadHandler.text));
        }
    }

    public IEnumerator Post<T>(string https, WWWForm form, IWebRequestCallBack<T> callBack)
    {
        UnityWebRequest webRequest = UnityWebRequest.Post(https, form);
        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ProtocolError || webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            callBack.WebRequestGetFail(https, webRequest.error);
        }
        else
        {
            callBack.WebRequestGetSuccess(https, JsonUtil.FromJson<T>(webRequest.downloadHandler.text));
        }
    }

    public IEnumerator GetSprice(string url, IWebRequestForSpriteCallBack callBack)
    {
        UnityWebRequest webRequest = new UnityWebRequest(url);
        DownloadHandlerTexture texDl = new DownloadHandlerTexture(true);
        webRequest.downloadHandler = texDl;
        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ProtocolError || webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            callBack.WebRequestForSpriteFail(url, webRequest.error);
        }
        else
        {
            Texture2D tex = texDl.texture;
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            callBack.WebRequestForSpriteSuccess(url, sprite);
        }

    }

    public IEnumerator GetTexture2D(string url, IWebRequestForTextureCallBack callBack)
    {
        UnityWebRequest webRequest = new UnityWebRequest(url);
        DownloadHandlerTexture texDl = new DownloadHandlerTexture(true);
        webRequest.downloadHandler = texDl;
        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ProtocolError || webRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            callBack.WebRequestForTextureFail(url, webRequest.error);
        }
        else
        {
            callBack.WebRequestForTextureSuccess(url, texDl.texture);
        }
    }
}