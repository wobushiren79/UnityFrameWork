using UnityEngine;
using UnityEditor;

public class LogUtil
{
    public enum LogEnum
    {
        Normal,//普通日志
        Error,//错误日志
        Warning//警告日志
    }

    /// <summary>
    /// 普通打印日志
    /// </summary>
    /// <param name="msg">日志说明</param>
    public static string Log(string msg)
    {
        return BaseDebugLog("日志输出-正常||", msg, LogEnum.Normal);
    }

    /// <summary>
    /// 打印警告日志
    /// </summary>
    /// <param name="msg">日志说明</param>
    public static string LogWarning(string msg)
    {
        return BaseDebugLog("日志输出-警告||", msg, LogEnum.Warning);
    }

    /// <summary>
    /// 打印错误日志
    /// </summary>
    /// <param name="msg">日志说明</param>
    public static string LogError(string msg)
    {
        return BaseDebugLog("日志输出-错误||", msg, LogEnum.Error);
    }

    /// <summary>
    /// 基础日志答应
    /// </summary>
    /// <param name="title">标题提示</param>
    /// <param name="content">内容</param>
    /// <param name="type">类型 详情见LogEnum</param>
    /// <returns></returns>
    private static string BaseDebugLog(string title, string content, LogEnum type)
    {
        //如果没有开启日志输出 则放弃打印
        if (!ProjectConfigInfo.IS_OPEN_LOG_MSG)
            return null;
        string logMsg = title + content;
        //根据不同的日志类型答应不同的日志
        switch (type)
        {
            case LogEnum.Normal:
                Debug.Log(logMsg);
                break;
            case LogEnum.Warning:
                Debug.LogWarning(logMsg);
                break;
            case LogEnum.Error:
                Debug.LogError(logMsg);
                break;
        }
        return logMsg;
    }
}