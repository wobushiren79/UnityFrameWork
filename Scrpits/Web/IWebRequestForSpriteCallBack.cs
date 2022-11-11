using UnityEngine;
using UnityEditor;

public interface IWebRequestForSpriteCallBack 
{
    void WebRequestForSpriteSuccess(string url, Sprite sprite);

    void WebRequestForSpriteFail(string url, string fail);
}