using System.Collections.Generic;
using System.Linq;

public static class ListArrayDicExtension
{
    /// <summary>
    /// List去重
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static List<T> DistinctEx<T>(this List<T> self)
    {
        return self.Distinct().ToList();
    }
}
