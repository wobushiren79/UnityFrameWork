using UnityEngine;
using UnityEditor;

public interface IWebRequestCallBack<T>
{

    void WebRequestGetSuccess(string url,T data);

    void WebRequestGetFail(string url, string fail);

}