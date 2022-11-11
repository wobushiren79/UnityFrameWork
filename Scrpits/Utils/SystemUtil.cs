using UnityEngine;
using UnityEditor;
using System;

public class SystemUtil 
{
    public enum UUIDTypeEnum {
        N,// xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx  32位字符串
        D,// xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx 连字符分隔的32位字符串
        B,// {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}在大括号中、由连字符分隔的32位字符串
        P,// (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx) 在圆括号中、由连字符分隔的32位字符串
    }

    /// <summary>
    /// 获取唯一ID
    /// </summary>
    /// <returns></returns>
    public static string GetUUID(UUIDTypeEnum type)
    {
        string uuidMark;
        switch (type)
        {
            case UUIDTypeEnum.N:
                uuidMark = "N";
                break;
            case UUIDTypeEnum.D:
                uuidMark = "D";
                break;
            case UUIDTypeEnum.P:
                uuidMark = "P";
                break;
            case UUIDTypeEnum.B:
                uuidMark = "B";
                break;
            default:
                uuidMark = "N";
                break;
        }
        return System.Guid.NewGuid().ToString(uuidMark);
    }

    /// <summary>
    /// 垃圾回收
    /// </summary>
    public static void GCCollect()
    {
        //资源卸载
        Resources.UnloadUnusedAssets();
        //垃圾回收
        System.GC.Collect();
    }
}