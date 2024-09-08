//https://github.com/SaladLab/Json.Net.Unity3D/releases
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;
using System.Collections.Generic;
public class JsonUtil : ScriptableObject
{
    /// <summary>
    /// 修改json中单独某一项属性
    /// </summary>
    public static string ChangeJson(string jsonStr, string propertyName, int newValue)
    {
        JObject jo = JObject.Parse(jsonStr);
        jo[propertyName] = newValue;
        return Convert.ToString(jo);
    }

    public static string ChangeJson<T>(string jsonStr, string propertyName, string newValue)
    {
        JObject jo = JObject.Parse(jsonStr);
        jo[propertyName] = newValue;
        return Convert.ToString(jo);
    }

    /// <summary>
    /// Json转换成类
    /// </summary>
    public static T FromJson<T>(string strData, JsonType jsonType = JsonType.System)
    {
        switch (jsonType)
        {
            case JsonType.System:
                return FromJsonBySystem<T>(strData);
            case JsonType.Net:
                return FromJsonByNet<T>(strData);
        }
        return default(T);
    }

    /// <summary>
    /// Json转换成类 原生
    /// </summary>
    public static T FromJsonBySystem<T>(string strData)
    {
        T dataBean = JsonUtility.FromJson<T>(strData);
        return dataBean;
    }

    /// <summary>
    /// Json转换成类(相对于原生JsonUtility 慢了大概6倍)
    /// </summary>
    public static T FromJsonByNet<T>(string strData)
    {
        T dataBean = JsonConvert.DeserializeObject<T>(strData);
        return dataBean;
    }

    /// <summary>
    /// 类转换成Json
    /// </summary>
    public static string ToJson<T>(T dataBean, JsonType jsonType = JsonType.System)
    {
        switch (jsonType)
        {
            case JsonType.System:
                return ToJsonBySystem(dataBean);
            case JsonType.Net:
                return ToJsonByNet(dataBean);
        }
        return null;
    }

    /// <summary>
    /// 类转换成Json 原生
    /// </summary>
    public static string ToJsonBySystem<T>(T dataBean)
    {
        string json = JsonUtility.ToJson(dataBean);
        return json;
    }

    /// <summary>
    /// 类转换成Json(相对于原生JsonUtility 慢了大概6倍)
    /// </summary>
    public static string ToJsonByNet<T>(T dataBean)
    {
        string json = JsonConvert.SerializeObject(dataBean);
        return json;
    }

    /// <summary>
    /// 类转换成Json 用于float 精度处理
    /// </summary>
    public static string ToJsonByUnityNewtonsoftJsonSerializer<T>(T dataBean)
    {
        UnityNewtonsoftJsonSerializer unityNewtonsoftJson = new UnityNewtonsoftJsonSerializer();
        string json = unityNewtonsoftJson.Serialize(dataBean);
        return json;
    }
}