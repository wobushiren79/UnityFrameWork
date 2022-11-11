using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EnumExtension 
{
    /// <summary>
    /// 获取枚举名字
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string GetEnumName<T>(this T data)
    {
        return data.ToString();
    }

    /// <summary>
    /// 获取枚举
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static T GetEnum<T>(this string data)
    {
        return (T)Enum.Parse(typeof(T), data);
    }

    /// <summary>
    /// 获取枚举
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="type"></param>
    /// <returns></returns>
    public static E GetEnum<E>(this int type)
    {
        return (E)Enum.ToObject(typeof(E), type);
    }


    /// <summary>
    /// 获取枚举最大值
    /// </summary>
    /// <param name="enumType"></param>
    /// <returns></returns>
    public static int GetEnumMaxIndex<E>()
    {
        int maxIndex = int.MinValue;
        Array EnumArray = Enum.GetValues(typeof(E));
        foreach (int item in EnumArray)
        {
            if (item > maxIndex)
                maxIndex = item;
        }
        return maxIndex;
    }

    /// <summary>
    /// 获取枚举第几项
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <param name="position"></param>
    /// <returns></returns>
    public static E GetEnumValueByPosition<E>(int position)
    {
        int i = 0;
        foreach (E item in Enum.GetValues(typeof(E)))
        {
            if (i == position)
            {
                return item;
            }
            i++;
        }
        return default;
    }

    /// <summary>
    /// 获取所有枚举类型
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <returns></returns>
    public static List<E> GetEnumValue<E>()
    {
        List<E> listDat = new List<E>();
        int i = 0;
        foreach (E item in Enum.GetValues(typeof(E)))
        {
            listDat.Add(item);
            i++;
        }
        return listDat;
    }

    /// <summary>
    /// 获取所有名字
    /// </summary>
    /// <typeparam name="E"></typeparam>
    /// <returns></returns>
    public static string[] GetEnumNames<E>()
    {
        return Enum.GetNames(typeof(E));
    }
}