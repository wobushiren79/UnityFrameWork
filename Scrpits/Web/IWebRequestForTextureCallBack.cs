using UnityEngine;
using UnityEditor;

public interface IWebRequestForTextureCallBack 
{
    void WebRequestForTextureSuccess(string url, Texture2D texture2D);

    void WebRequestForTextureFail(string url, string fail);
}