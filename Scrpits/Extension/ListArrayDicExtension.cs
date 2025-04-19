using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public static class ListArrayDicExtension
{
    /// <summary>
    /// 循环
    /// </summary>
    public static void ForEach<T>(this T[] self, Action<int, T> actionItem)
    {
        for (int i = 0; i < self.Length; i++)
        {
            actionItem?.Invoke(i, self[i]);
        }
    }
    
    /// <summary>
    /// 循环
    /// </summary>
    public static void ForEach<T>(this List<T> self, Action<int, T> actionItem)
    {
        for (int i = 0; i < self.Count; i++)
        {
            actionItem?.Invoke(i, self[i]);
        }
    }

    /// <summary>
    /// 循环
    /// </summary>
    public static void ForEach<T, D>(this Dictionary<T, D> self, Action<T, D> actionItem)
    {
        foreach (var item in self)
        {
            actionItem?.Invoke(item.Key, item.Value);
        }
    }


    /// <summary>
    /// List去重
    /// </summary>
    public static List<T> DistinctEx<T>(this List<T> self)
    {
        return self.Distinct().ToList();
    }

    public static int[] Add(this int[] self, int add)
    {
        int[] newData = new int[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newData[i] = add + self[i];
        }
        return newData;
    }

    public static void AddForSelf(this int[] self, int add)
    {
        for (int i = 0; i < self.Length; i++)
        {
            self[i] += add;
        }
    }
}
