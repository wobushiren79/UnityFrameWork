using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class TypeExtension
{

    /// <summary>
    /// 自定义时间格式转换系统时间格式
    /// </summary>
    /// <param name="timeBean"></param>
    /// <returns></returns>
    public static DateTime ToDateTime(this TimeBean timeBean)
    {
        DateTime dateTime = new DateTime(timeBean.year, timeBean.month, timeBean.day, timeBean.hour, timeBean.minute, (int)timeBean.second);
        return dateTime;
    }

    /// <summary>
    /// Vector3Bean 转化为 Vector3
    /// </summary>
    /// <param name="vector3Bean"></param>
    /// <returns></returns>
    public static Vector3 ToVector3(this Vector3Bean vector3Bean)
    {
        Vector3 vector3 = new Vector3(vector3Bean.x, vector3Bean.y, vector3Bean.z);
        return vector3;
    }

    /// <summary>
    /// Vector3 转化为 Vector3Bean
    /// </summary>
    /// <param name="vector3"></param>
    /// <returns></returns>
    public static Vector3Bean ToVector3Bean(this Vector3 vector3)
    {
        Vector3Bean vector3Bean = new Vector3Bean(vector3);
        return vector3Bean;
    }

    /// <summary>
    /// Vector3 转化为 Vector2
    /// </summary>
    /// <param name="listVector3"></param>
    /// <returns></returns>
    public static List<Vector2> ToListV2(this List<Vector3> listVector3)
    {
        List<Vector2> listVector2 = new List<Vector2>();
        foreach (Vector3 item in listVector3)
        {
            listVector2.Add(new Vector2(item.x, item.y));
        }
        return listVector2;
    }

    /// <summary>
    /// Vector3 转化为 Vector2
    /// </summary>
    /// <param name="listVector3"></param>
    /// <returns></returns>
    public static List<Vector3Bean> ToListV3Bean(this List<Vector3> listVector3)
    {
        List<Vector3Bean> listVector3Bean = new List<Vector3Bean>();
        foreach (Vector3 item in listVector3)
        {
            listVector3Bean.Add(new Vector3Bean(item));
        }
        return listVector3Bean;
    }

    /// <summary>
    /// list转数组
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T[] ToArrayFromPosition<T>(this List<T> list, int position)
    {
        if (list == null)
            return null;
        int listCount = list.Count;
        T[] tempArray = new T[listCount];
        int f = 0;
        for (int i = 0; i < listCount; i++)
        {
            int startPosition = i + position;
            if (startPosition < listCount)
            {
                tempArray[i] = list[startPosition];
            }
            else
            {
                tempArray[i] = list[f];
                f++;
            }

        }
        return tempArray;
    }

    /// <summary>
    /// list转string 通过split分割
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="split"></param>
    /// <returns></returns>
    public static string ToStringBySplit<T>(this List<T> list, string split)
    {
        StringBuilder data = new StringBuilder();
        if (data == null)
            return data.ToString();
        for (int i = 0; i < list.Count; i++)
        {
            if (i != 0)
            {
                data.Append(split);
            }
            data.Append(list[i].ToString());
        }
        return data.ToString();
    }

    /// <summary>
    /// 数组转string 通过split分割
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="split"></param>
    /// <returns></returns>
    public static string ToStringBySplit<T>(this T[] list, string split)
    {
        StringBuilder data = new StringBuilder();
        if (data == null)
            return data.ToString();
        for (int i = 0; i < list.Length; i++)
        {
            if (i != 0)
            {
                data.Append(split);
            }
            data.Append(list[i].ToString());
        }
        return data.ToString();
    }

    /// <summary>
    /// Color转换ColorBean
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static ColorBean ToColorBean(this Color color)
    {
        ColorBean colorBean = new ColorBean(color.r, color.g, color.b, color.a);
        return colorBean;
    }


    /// <summary>
    ///  图标字典转List
    /// </summary>
    /// <param name="map"></param>
    /// <returns></returns>
    public static List<IconBean> ToList(this IconBeanDictionary map)
    {
        List<IconBean> listData = new List<IconBean>();
        foreach (string key in map.Keys)
        {
            IconBean iconBean = new IconBean
            {
                key = key,
                value = map[key]
            };
            listData.Add(iconBean);
        }
        return listData;
    }

    /// <summary>
    /// List<string> 强转 List<long>
    /// </summary>
    /// <param name="listStr"></param>
    /// <returns></returns>
    public static List<long> ToListLong(this List<string> listStr)
    {
        if (listStr == null)
            return null;
        List<long> listData = new List<long>();
        foreach (string itemStr in listStr)
        {
            if (long.TryParse(itemStr, out long itemLong))
            {
                listData.Add(itemLong);
            }
        }
        return listData;
    }

    /// <summary>
    /// string[] 强转 List<long>
    /// </summary>
    public static List<long> ToListLong(this string[] arrayStr)
    {
        if (arrayStr == null)
            return null;
        List<long> listData = new List<long>(arrayStr.Length);
        for (int i = 0; i < arrayStr.Length; i++)
        {
            string itemStr = arrayStr[i];
            if (long.TryParse(itemStr, out long itemLong))
            {
                listData.Add(itemLong);
            }
        }
        return listData;
    }

    /// <summary>
    ///  string[] 强转 long[]
    /// </summary>
    /// <param name="arrayStr"></param>
    /// <returns></returns>
    public static long[] ToArrayLong(this string[] arrayStr)
    {
        if (arrayStr == null)
            return null;
        long[] listData = new long[arrayStr.Length];
        for (int i = 0; i < arrayStr.Length; i++)
        {
            string itemStr = arrayStr[i];
            if (long.TryParse(itemStr, out long itemLong))
            {
                listData[i] = itemLong;
            }
        }
        return listData;
    }


    /// <summary>
    ///  string[] 强转 long[]
    /// </summary>
    /// <param name="arrayStr"></param>
    /// <returns></returns>
    public static int[] ToArrayInt(this string[] arrayStr)
    {
        if (arrayStr == null)
            return null;
        int[] listData = new int[arrayStr.Length];
        for (int i = 0; i < arrayStr.Length; i++)
        {
            string itemStr = arrayStr[i];
            if (int.TryParse(itemStr, out int itemInt))
            {
                listData[i] = itemInt;
            }
        }
        return listData;
    }

    /// <summary>
    ///  string[] 强转 float[]
    /// </summary>
    /// <param name="arrayStr"></param>
    /// <returns></returns>
    public static float[] ToArrayFloat(this string[] arrayStr)
    {
        if (arrayStr == null)
            return null;
        float[] listData = new float[arrayStr.Length];
        for (int i = 0; i < arrayStr.Length; i++)
        {
            string itemStr = arrayStr[i];
            if (float.TryParse(itemStr, out float itemFloat))
            {
                listData[i] = itemFloat;
            }
        }
        return listData;
    }

    /// <summary>
    /// 数字转中文
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static string ToChinese(this int number)
    {
        if (number >= 10 || number < 0)
        {
            LogUtil.LogError("阿拉伯数字转中文数字失败");
            return "";
        }
        string[] chineseNumberList = new string[10] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        return chineseNumberList[number];
    }

    /// <summary>
    /// string 转 INT[]
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static int[] ToInt32(this string data)
    {
        char[] charList = data.ToCharArray();
        int[] intList = new int[charList.Length];
        for (int i = 0; i < charList.Length; i++)
        {
            char itemChar = charList[i];
            intList[i] = Convert.ToInt32(itemChar);
        }
        return intList;
    }

    /// <summary>
    /// INT[]  转 string
    /// </summary>
    /// <param name="listInt"></param>
    /// <returns></returns>
    public static string ToString(this int[] listInt)
    {
        char[] charList = new char[listInt.Length];
        for (int i = 0; i < listInt.Length; i++)
        {
            int itemInt = listInt[i];
            try
            {
                charList[i] = Convert.ToChar(itemInt);
            }
            catch
            {

            }
        }
        return new string(charList);
    }

    /// <summary>
    /// Sprite转Tex2D
    /// </summary>
    /// <param name="sprite"></param>
    /// <returns></returns>
    public static Texture2D ToTex2D(this Sprite sprite)
    {
        var targetTex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
        var pixels = sprite.texture.GetPixels(
            (int)sprite.textureRect.x,
            (int)sprite.textureRect.y,
            (int)sprite.textureRect.width,
            (int)sprite.textureRect.height);
        targetTex.SetPixels(pixels);
        targetTex.Apply();
        return targetTex;
    }

    /// <summary>
    /// Tex2D转Sprite
    /// </summary>
    /// <param name="t2d"></param>
    /// <returns></returns>
    public static Sprite ToSprite(this Texture2D t2d)
    {
        return Sprite.Create(t2d, new Rect(0, 0, t2d.width, t2d.height), Vector2.zero);
    }

    /// <summary>
    /// list转map 需集成baseBean
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listData"></param>
    /// <returns></returns>
    public static Dictionary<long, T> ToMap<T>(this List<T> listData) where T : BaseBean
    {
        Dictionary<long, T> map = new Dictionary<long, T>();
        if (listData == null)
            return map;
        for (int i = 0; i < listData.Count; i++)
        {
            T itemData = listData[i];
            map.Add(itemData.id, itemData);
        }
        return map;
    }

    /// <summary>
    /// map转list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="mapData"></param>
    /// <returns></returns>
    public static List<T> ToList<T>(this Dictionary<long, T> mapData)
    {
        List<T> listData = new List<T>();
        foreach (var value in mapData.Values)
        {
            listData.Add(value);
        }
        return listData;
    }

    /// <summary>
    /// 转换成V3
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static Vector3[] ToVector3(this Vector2[] self)
    {
        Vector3[] listData = new Vector3[self.Length];
        for (int i = 0; i < self.Length;i++)
        {
            Vector2 itemData = self[i];
            listData[i] = new Vector3(itemData.x, itemData.y,0);
        }
        return listData;
    }


    /// <summary>
    /// list转int(仅限枚举)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <returns></returns>
    public static List<int> ToListInt<T>(this List<T> self)
    {
        List<int> listData = new List<int>();
        for (int i = 0; i < self.Count; i++)
        {
            listData.Add((int)(object)self[i]);
        }
        return listData;
    }


    /// <summary>
    /// 转换成List 只添加Value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <param name="self"></param>
    /// <returns></returns>
    public static List<V> ToListForValue<T,V>(this Dictionary<T,V> self)
    {
        List<V> listData = new List<V>();
        foreach (var itemData in self)
        {
            listData.Add(itemData.Value);
        }
        return listData;
    }

    /// <summary>
    /// 转换成List 只添加Key
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <param name="self"></param>
    /// <returns></returns>
    public static List<T> ToListForKey<T, V>(this Dictionary<T, V> self)
    {
        List<T> listData = new List<T>();
        foreach (var itemData in self)
        {
            listData.Add(itemData.Key);
        }
        return listData;
    }

    /// <summary>
    /// 转换成List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <param name="self"></param>
    /// <param name="listDataKey"></param>
    /// <param name="listDataValue"></param>
    public static void ToListForKeyAndValue<T, V>(this Dictionary<T, V> self, out List<T> listDataKey, out List<V> listDataValue)
    {
        listDataKey = new List<T>();
        listDataValue = new List<V>();
        foreach (var itemData in self)
        {
            listDataKey.Add(itemData.Key);
            listDataValue.Add(itemData.Value);
        }
    }
}