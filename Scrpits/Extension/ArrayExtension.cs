using UnityEditor;
using UnityEngine;

public static class ArrayExtension
{

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