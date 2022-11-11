//https://github.com/SaladLab/Json.Net.Unity3D/releases
using Newtonsoft.Json;
using UnityEngine;

public class JsonUtil : ScriptableObject
{
    /// <summary>
    /// Json转换成类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="strData"></param>
    /// <returns></returns>
    public static T FromJson<T>(string strData)
    {
       T dataBean = JsonUtility.FromJson<T>(strData);
        return dataBean;
    }

    /// <summary>
    /// Json转换成类(相对于原生JsonUtility 慢了大概6倍)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="strData"></param>
    /// <returns></returns>
    public static T FromJsonByNet<T>(string strData)
    {
        T dataBean = JsonConvert.DeserializeObject<T>(strData);
        return dataBean;
    }

    /// <summary>
    /// 类转换成Json
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dataBean"></param>
    /// <returns></returns>
    public static string ToJson<T>(T dataBean)
    {
        string json = JsonUtility.ToJson(dataBean);
        return json;
    }

    /// <summary>
    /// 类转换成Json(相对于原生JsonUtility 慢了大概6倍)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dataBean"></param>
    /// <returns></returns>
    public static string ToJsonByNet<T>(T dataBean)
    {
        string json = JsonConvert.SerializeObject(dataBean);
        return json;
    }

}