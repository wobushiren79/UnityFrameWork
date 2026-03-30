using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public class RandomUtil
{

    public static RandomTools GetRandom(int seed, int offset1, int offset2, int offset3)
    {
        RandomTools randomTools = new RandomTools((ulong)(seed == 0 ? seed + 1 : seed) * (ulong)(offset1 == 0 ? offset1 + 1 : offset1) * (ulong)(offset2 == 0 ? offset2 + 1 : offset2) * (ulong)(offset3 == 0 ? offset3 + 1 : offset3));
        return randomTools;
    }

    public static RandomTools GetRandom(int seed, int offset1, int offset2)
    {
        RandomTools randomTools = new RandomTools((ulong)(seed == 0 ? seed + 1 : seed) * (ulong)(offset1 == 0 ? offset1 + 1 : offset1) * (ulong)(offset2 == 0 ? offset2 + 1 : offset2));
        return randomTools;
    }

    /// <summary>
    /// 随机获取枚举的一个值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetRandomEnum<T>()
    {
        T[] enumList = System.Enum.GetValues(typeof(T)) as T[];
        return enumList[Random.Range(0, enumList.Length)];
    }

    /// <summary>
    /// 获取List 中 随机一个数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T GetRandomDataByList<T>(List<T> list)
    {
        if (list.IsNull())
            return default;
        int position = Random.Range(0, list.Count);
        return list[position];
    }

    /// <summary>
    /// 获取list 中指定数量的集合 不重复
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="number"></param>
    /// <returns></returns>
    public static List<T> GetRandomDataByListForNumberNR<T>(List<T> list, int number)
    {
        List<T> listData = new List<T>();
        if (list.IsNull())
            return listData;
        if (list.Count < number)
        {
            number = list.Count;
        }
        List<int> tempList = new List<int>();
        for (int i = 0; i < number; i++)
        {
            int position = Random.Range(0, list.Count);
            if (!tempList.Contains(position))
            {
                listData.Add(list[position]);
                tempList.Add(position);
            }
            else
            {
                i--;
            }
        }
        return listData;
    }

    /// <summary>
    /// 获取IconBeanDictionary 中 随机一个数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static IconBean GetRandomDataByIconBeanDictionary(IconBeanDictionary list)
    {
        int position = Random.Range(0, list.Count);
        int i = 0;
        foreach (string key in list.Keys)
        {
            if (position == i)
            {
                IconBean data = new IconBean
                {
                    key = key,
                    value = list[key]
                };
                return data;
            }
            i++;
        }
        return null;
    }

    /// <summary>
    /// 获取IconBeanDictionary 中 随机一个数 排开
    /// </summary>
    /// <param name="list"></param>
    /// <param name="expelNameStr"></param>
    /// <returns></returns>
    public static IconBean GetRandomDataByIconBeanDictionaryExpel(IconBeanDictionary list, string expelNameStr)
    {
        IconBean iconData = GetRandomDataByIconBeanDictionary(list);
        if (iconData.key.Contains(expelNameStr))
        {
            return GetRandomDataByIconBeanDictionaryExpel(list, expelNameStr);
        }
        else
        {
            return iconData;
        }
    }

    /// <summary>
    /// 获取List随机数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static List<T> GetRandomListByList<T>(List<T> list)
    {
        if (list == null)
            return list;
        int counter = list.Count;
        T temp;
        int index;
        while (counter > 0)
        {
            counter--;
            index = Random.Range(0, counter);
            temp = list[counter];
            list[counter] = list[index];
            list[index] = temp;
        }
        return list;
    }

    /// <summary>
    /// 获取数组随机数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T[] GetRandomArrayByArray<T>(T[] list)
    {
        if (list == null)
            return list;
        int counter = list.Length;
        T temp;
        int index;
        while (counter > 0)
        {
            counter--;
            index = Random.Range(0, counter);
            temp = list[counter];
            list[counter] = list[index];
            list[index] = temp;
        }
        return list;
    }

    /// <summary>
    /// 获取数组随机数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T GetRandomDataByArray<T>(T[] list)
    {
        if (list == null)
            return default;
        int position = Random.Range(0, list.Length);
        return list[position];
    }

    /// <summary>
    /// 获取map随机数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mapData"></param>
    /// <returns></returns>
    public static T GetRandomDataByDictionary<T>(Dictionary<long, T> mapData)
    {
        int position = Random.Range(0, (int)mapData.Count);
        int tempPosition = 0;
        foreach (var itemData in mapData)
        {
            if (tempPosition == position)
            {
                return itemData.Value;
            }
            tempPosition++;
        }
        return default;
    }

    /// <summary>
    /// 随机产生常用汉字
    /// </summary>
    /// <param name="count">要产生汉字的个数</param>
    /// <returns>常用汉字</returns>
    public static string GetRandomGenerateChineseWord(int count)
    {
        if (count < 1)
            return "";

        string chineseWords = "";
        // Encoding gb = Encoding.GetEncoding("gb2312");
        //添加姓
        chineseWords += GetRandomDataByArray(GeneralDataUtil.ChinesNameWords);
        //添加名
        for (int i = 0; i < count - 1; i++)
        {
            int position = Random.Range(0, GeneralDataUtil.ChinesNameWordsForLastName.Length - 1);
            char name = GeneralDataUtil.ChinesNameWordsForLastName[position];
            //// 获取区码(常用汉字的区码范围为16-55)
            //int regionCode = Random.Range(16, 56);

            //// 获取位码(位码范围为1-94 由于55区的90,91,92,93,94为空,故将其排除)
            //int positionCode;
            //if (regionCode == 55)
            //{
            //    // 55区排除90,91,92,93,94
            //    positionCode = Random.Range(1, 90);
            //}
            //else
            //{
            //    positionCode = Random.Range(1, 95);
            //}

            //// 转换区位码为机内码
            //int regionCode_Machine = regionCode + 160;// 160即为十六进制的20H+80H=A0H
            //int positionCode_Machine = positionCode + 160;// 160即为十六进制的20H+80H=A0H

            //// 转换为汉字
            //byte[] bytes = new byte[] { (byte)regionCode_Machine, (byte)positionCode_Machine };\
            //chineseWords += gb.GetString(bytes);
            chineseWords += name;
        }
        return chineseWords;
    }

    /// <summary>
    /// 获取圆上任意一点
    /// </summary>
    /// <param name="centerPosition"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    public static Vector2 GetRandomPositionForCircle(Vector2 centerPosition, float radius)
    {
        return Random.insideUnitCircle.normalized * radius + centerPosition;
    }

    /// <summary>
    /// 获取球型上随机一点
    /// </summary>
    /// <param name="centerPosition"></param>
    /// <param name="radius"></param>
    /// <param name="type">0内部 1边界上</param>
    /// <returns></returns>
    public static Vector3 GetRandomPositionForSphere(Vector3 centerPosition, float radius,int type = 0)
    {
        if (type == 0)
        {
            return Random.insideUnitSphere.normalized * radius+centerPosition;
        }
        else
        {
            return Random.onUnitSphere.normalized * radius + centerPosition;
        }
    }
}