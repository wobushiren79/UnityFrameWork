using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public static class StringExtension 
{

    /// <summary>
    /// 计算字符串中指定字符出现次数
    /// </summary>
    /// <param name="selfData"></param>
    /// <param name="substring"></param>
    /// <returns></returns>
    public static int SubstringCount(this string selfData, string substring)
    {
        if (selfData.Contains(substring))
        {
            string strReplaced = selfData.Replace(substring, "");
            return (selfData.Length - strReplaced.Length);
        }
        return 0;
    }

    /// <summary>
    /// string 拆分成指定枚举
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfData"></param>
    /// <param name="substring"></param>
    /// <returns></returns>
    public static T[] SplitForArrayEnum<T>(this string selfData, char substring)
    {
        if (selfData.IsNull())
            return new T[0];
        string[] splitData = selfData.Split(new char[] { substring }, StringSplitOptions.RemoveEmptyEntries);
        if (splitData.IsNull())
        {
            return new T[0];
        }
        T[] listData = new T[splitData.Length];
        for (int i = 0; i < splitData.Length; i++)
        {
            if (splitData[i].IsNull())
            {

            }
            else
            {
                listData[i] = splitData[i].GetEnum<T>();
            }
        }
        return listData;
    }

    /// <summary>
    /// string通过指定字符拆分成数组
    /// </summary>
    /// <param name="selfData"></param>
    /// <param name="substring"></param>
    /// <returns></returns>
    public static List<string> SplitForListStr(this string selfData, char substring)
    {
        if (selfData == null)
            return new List<string>();
        string[] splitData = selfData.Split(new char[] { substring }, StringSplitOptions.RemoveEmptyEntries);
        List<string> listData = splitData.ToList();
        return listData;
    }

    /// <summary>
    /// string通过指定字符拆分成数组
    /// </summary>
    /// <param name="selfData"></param>
    /// <param name="substring"></param>
    /// <returns></returns>
    public static string[] SplitForArrayStr(this string selfData, char substring)
    {
        if (selfData == null)
            return new string[0];
        string[] splitData = selfData.Split(new char[] { substring }, StringSplitOptions.RemoveEmptyEntries);
        return splitData;
    }

    /// <summary>
    /// string通过指定字符拆分成数组
    /// </summary>
    /// <param name="selfData"></param>
    /// <param name="substring"></param>
    /// <returns></returns>
    public static long[] SplitForArrayLong(this string selfData, char substring)
    {
        if (selfData.IsNull())
            return new long[0];
        string[] splitData = selfData.Split(new char[] { substring }, StringSplitOptions.RemoveEmptyEntries);
        long[] listData = splitData.ToArrayLong();
        return listData;
    }

    /// <summary>
    /// string通过指定字符拆分成列表
    /// </summary>
    /// <param name="selfData"></param>
    /// <param name="substring"></param>
    /// <returns></returns>
    public static List<long> SplitForListLong(this string selfData, char substring)
    {
        if (selfData.IsNull())
            return new List<long>();
        string[] splitData = selfData.Split(new char[] { substring }, StringSplitOptions.RemoveEmptyEntries);
        List<long> listData = splitData.ToListLong();
        return listData;
    }

    /// <summary>
    /// string通过指定字符拆分成数组
    /// </summary>
    /// <param name="selfData"></param>
    /// <param name="substring"></param>
    /// <returns></returns>
    public static int[] SplitForArrayInt(this string selfData, char substring)
    {
        if (selfData == null)
            return new int[0];
        string[] splitData = selfData.Split(new char[] { substring }, StringSplitOptions.RemoveEmptyEntries);
        int[] listData = splitData.ToArrayInt();
        return listData;
    }

    /// <summary>
    /// string通过指定字符拆分成数组
    /// </summary>
    /// <param name="selfData"></param>
    /// <param name="substring"></param>
    /// <returns></returns>
    public static float[] SplitForArrayFloat(this string selfData, char substring)
    {
        if (selfData == null)
            return new float[0];
        string[] splitData = selfData.Split(new char[] { substring }, StringSplitOptions.RemoveEmptyEntries);
        float[] listData = splitData.ToArrayFloat();
        return listData;
    }

    /// <summary>
    /// 拆分并随机获取一个数值
    /// </summary>
    /// <param name="selfData"></param>
    /// <param name="substring"></param>
    /// <returns></returns>
    public static long SplitAndRandomForLong(this string selfData, char substring)
    {
        long[] arrayData = SplitForArrayLong(selfData, substring);
        if (arrayData.IsNull())
        {
            return 0;
        }
        return arrayData.GetRandomData();
    }
}