using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CheckExtension
{
    /// <summary>
    /// 检测 list是否为null
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static bool IsNull<T>(this Queue<T> selfList)
    {
        if (selfList == null || selfList.Count == 0)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 是否是null或者长度为0
    /// </summary>
    /// <param name="selfStr"></param>
    /// <returns></returns>
    public static bool IsNull(this string selfStr)
    {
        if (selfStr == null || selfStr.Length == 0)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检测 list是否为null
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static bool IsNull<T>(this List<T> selfList)
    {
        if (selfList == null || selfList.Count == 0)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检测字典是否为Null
    /// </summary>
    /// <typeparam name="A"></typeparam>
    /// <typeparam name="B"></typeparam>
    /// <param name="self"></param>
    /// <returns></returns>
    public static bool IsNull<A,B>(this Dictionary<A,B> self)
    {
        if (self == null || self.Count == 0)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检测Array是否为Null
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    public static bool IsNull<T>(this T[] selfArray)
    {
        if (selfArray == null || selfArray.Length == 0)
        {
            return true;
        }
        return false;
    }
}