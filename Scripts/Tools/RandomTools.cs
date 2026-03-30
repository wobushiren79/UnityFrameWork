using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public class RandomTools
{
    public UInt64 seed;

    public RandomTools(UInt64 seed)
    {
        this.seed = (seed ^ 0x5DEECE66DUL) & ((1UL << 48) - 1);
    }
    protected UInt32 Next(int bits)
    {
        seed = (seed * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);

        return (UInt32)(seed >> (48 - bits));
    }

    public int NextInt(int n)
    {
        if (n <= 0) {
            LogUtil.Log("n must be positive");
            return 0;
        } 

        if ((n & -n) == n)  // i.e., n is a power of 2
            return (int)((n * (long)Next(31)) >> 31);

        long bits, val;
        do
        {
            bits = Next(31);
            val = bits % (UInt32)n;
        }
        while (bits - val + (n - 1) < 0);

        return (int)val;
    }

    public int NextInt(int start,int end)
    {
        int randomData = NextInt(end - start);
        return randomData + start;
    }

    public long NextLong()
    {
        return ((long)Next(32) << 32) + Next(32);
    }
    public bool NextBoolean()
    {
        return Next(1) != 0;
    }

    public float NextFloat()
    {
        return Next(24) / ((float)(1 << 24));
    }
    public double NextDouble()
    {
        return (((long)Next(26) << 27) + Next(27))
          / (double)(1L << 53);
    }

}