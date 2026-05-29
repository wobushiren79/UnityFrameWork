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
    public static T FromJson<T>(string strData, JsonTypeEnum jsonType = JsonTypeEnum.System)
    {
        switch (jsonType)
        {
            case JsonTypeEnum.System:
                return FromJsonBySystem<T>(strData);
            case JsonTypeEnum.Net:
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
    public static string ToJson<T>(T dataBean, JsonTypeEnum jsonType = JsonTypeEnum.System)
    {
        switch (jsonType)
        {
            case JsonTypeEnum.System:
                return ToJsonBySystem(dataBean);
            case JsonTypeEnum.Net:
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
    /// 注意:
    /// 1. MaxDepth = 64 防止 Unity Vector2/Vector3 的 normalized 递归属性把栈跑爆(StackOverflowException 不可 catch)
    /// 2. Error 回调收集每一处出错的成员路径,失败时一并输出
    /// </summary>
    public static string ToJsonByNet<T>(T dataBean)
    {
        //记录出错的成员路径,便于定位"哪个类的哪个属性"
        List<string> errorPaths = new();
        var settings = new JsonSerializerSettings
        {
            MaxDepth = 64,
            Error = (sender, args) =>
            {
                var ctx = args.ErrorContext;
                string ownerType = ctx.OriginalObject != null ? ctx.OriginalObject.GetType().FullName : "(null)";
                string memberName = ctx.Member != null ? ctx.Member.ToString() : "(unknown)";
                string path = string.IsNullOrEmpty(ctx.Path) ? "(root)" : ctx.Path;
                errorPaths.Add($"  ▸ 类型={ownerType} 成员={memberName} 路径={path} 错误={ctx.Error.Message}");
                //不在回调里 Handled,让异常继续抛出到下面的 catch,但已经把信息记下了
            }
        };

        try
        {
            return JsonConvert.SerializeObject(dataBean, settings);
        }
        catch (Exception ex)
        {
            string detail = errorPaths.Count > 0 ? string.Join("\n", errorPaths) : "  ▸ (无成员级错误,可能是栈溢出/达到 MaxDepth)";
            LogUtil.LogError(
                $"[JsonUtil.ToJsonByNet] 序列化失败\n" +
                $"  根类型: {typeof(T).FullName}\n" +
                $"  异常类型: {ex.GetType().Name}\n" +
                $"  异常信息: {ex.Message}\n" +
                $"  出错成员:\n{detail}");
            throw;
        }
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