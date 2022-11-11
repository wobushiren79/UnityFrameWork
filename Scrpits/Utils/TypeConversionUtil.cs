using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
public class TypeConversionUtil
{
    /// <summary>
    /// 自定义时间格式转换系统时间格式
    /// </summary>
    /// <param name="timeBean"></param>
    /// <returns></returns>
    public static DateTime TimeBeanToDateTime(TimeBean timeBean)
    {
        DateTime dateTime = new DateTime(timeBean.year, timeBean.month, timeBean.day, timeBean.hour, timeBean.minute, timeBean.second);
        return dateTime;
    }

    /// <summary>
    /// 自定义位置转为系统位置
    /// </summary>
    /// <param name="vector3Bean"></param>
    /// <returns></returns>
    public static Vector3 Vector3BeanToVector3(Vector3Bean vector3Bean)
    {
        Vector3 vector3 = new Vector3(vector3Bean.x, vector3Bean.y, vector3Bean.z);
        return vector3;
    }

    /// <summary>
    /// Vector3 转化为 Vector2
    /// </summary>
    /// <param name="listVector3"></param>
    /// <returns></returns>
    public static List<Vector2> ListV3ToListV2(List<Vector3> listVector3)
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
    public static List<Vector3Bean> ListV3ToListV3Bean(List<Vector3> listVector3)
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
    public static T[] ListToArray<T>(List<T> list)
    {
        if (list == null)
            return null;
        return list.ToArray();
    }

    /// <summary>
    /// 数组转List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <returns></returns>
    public static List<T> ArrayToList<T>(T[] array)
    {
        if (array == null)
            return null;
        return array.ToList<T>();
    }

    /// <summary>
    /// list转数组
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static T[] ListToArrayFromPosition<T>(List<T> list, int position)
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
    public static string ListToStringBySplit<T>(List<T> list, string split)
    {
        string data = "";
        if (data == null)
            return data;
        for (int i = 0; i < list.Count; i++)
        {
            if (i != 0)
            {
                data += split;
            }
            data += list[i].ToString();
        }
        return data;
    }

    /// <summary>
    /// 数组转string 通过split分割
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="split"></param>
    /// <returns></returns>
    public static string ArrayToStringBySplit<T>(T[] list, string split)
    {
        string data = "";
        if (data == null)
            return data;
        for (int i = 0; i < list.Length; i++)
        {
            if (i != 0)
            {
                data += split;
            }
            data += list[i].ToString();
        }
        return data;
    }

    /// <summary>
    /// Color转换ColorBean
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static ColorBean ColorToColorBean(Color color)
    {
        ColorBean colorBean = new ColorBean(color.r, color.g, color.b, color.a);
        return colorBean;
    }


    /// <summary>
    ///  图标字典转List
    /// </summary>
    /// <param name="map"></param>
    /// <returns></returns>
    public static List<IconBean> IconBeanDictionaryToList(IconBeanDictionary map)
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
    public static List<long> ListStrToListLong(List<string> listStr)
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
    ///  string[] 强转 long[]
    /// </summary>
    /// <param name="arrayStr"></param>
    /// <returns></returns>
    public static long[] ArrayStrToArrayLong(string[] arrayStr)
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
    public static int[] ArrayStrToArrayInt(string[] arrayStr)
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
    public static float[] ArrayStrToArrayFloat(string[] arrayStr)
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
    public static string NumberToChinese(int number)
    {
        if (number >= 10 || number < 0)
        {
            LogUtil.LogError("阿拉伯数字转中文数字失败");
            return "";
        }
        string[] chineseNumberList = new string[10] { "〇", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        return chineseNumberList[number];
    }

    /// <summary>
    /// string 转 INT[]
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static int[] StringToInt32(string data)
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
    public static string Int32ToString(int[] listInt)
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
    public static Texture2D SpriteToTex2D(Sprite sprite)
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
    public static Sprite Tex2DToSprite(Texture2D t2d)
    {
        return Sprite.Create(t2d, new Rect(0, 0, t2d.width, t2d.height), Vector2.zero);
    }

    /// <summary>
    /// list转map 需集成baseBean
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listData"></param>
    /// <returns></returns>
    public static Dictionary<long, T> ListToMap<T>(List<T> listData) where T : BaseBean
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
    public static List<T> MapToList<T>(Dictionary<long, T> mapData)
    {
        List<T> listData = new List<T>();
        foreach (var value in mapData.Values)
        {
            listData.Add(value);
        }
        return listData;
    }
}