using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class SteamWebImpl 
{
    /// <summary>
    /// 获取用户摘要
    /// </summary>
    /// <param name="steamId">多个用户可用，分隔，最多100 </param>
    /// <param name="callBack"></param>
    /// <returns></returns>
    public IEnumerator GetPlayerSummaries(string steamId,IWebRequestCallBack<SteamWebPlaySummariesBean> callBack)
    {
        string https = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2";
        WebRequest webRequest = new WebRequest();
        Dictionary<string, string> mapData = new Dictionary<string, string>();
        mapData.Add("key", ProjectConfigInfo.STEAM_KEY_ALL);
        mapData.Add("steamids", steamId);
        yield return webRequest.Get(https, mapData, callBack);
    }
}