using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

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
    /// string通过指定字符拆分成列表
    /// </summary>
    /// <param name="selfData"></param>
    /// <param name="substring"></param>
    /// <param name="rangestring">范围 一般用"-"</param>
    /// <returns></returns>
    public static List<long> SplitForListLong(this string selfData, char substring, char rangestring)
    {
        if (selfData == null)
            return new List<long>();

        string[] splitData = selfData.Split(new[] { substring }, StringSplitOptions.RemoveEmptyEntries);

        // 使用 IndexOf 替代 Contains 以提高效率
        if (selfData.IndexOf(rangestring) >= 0)
        {
            // 预计算元素信息及总容量
            var elementInfo = new (bool isRange, long start, long end, long value)[splitData.Length];
            int totalCount = 0;

            for (int i = 0; i < splitData.Length; i++)
            {
                string item = splitData[i];
                string[] parts = item.Split(new[] { rangestring }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 2)
                {
                    long start = long.Parse(parts[0]);
                    long end = long.Parse(parts[1]);
                    elementInfo[i] = (true, start, end, 0);
                    totalCount += (int)(end - start + 1);
                }
                else
                {
                    long val = long.Parse(item);
                    elementInfo[i] = (false, 0, 0, val);
                    totalCount++;
                }
            }

            // 按预计算容量初始化列表
            List<long> result = new List<long>(totalCount);
            foreach (var info in elementInfo)
            {
                if (info.isRange)
                {
                    for (long num = info.start; num <= info.end; num++)
                        result.Add(num);
                }
                else
                {
                    result.Add(info.value);
                }
            }
            return result;
        }
        else
        {
            // 无范围符时直接转换
            return splitData.ToListLong();
        }
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