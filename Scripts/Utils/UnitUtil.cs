using UnityEngine;
using UnityEditor;
using System;

public class UnitUtil
{
    public enum UnitEnum
    {
        None = 0,
        Million = 1,//百万
        Billion = 2,//十亿
        Trillion = 3,//万亿
        Quadrillion = 4,
        Quintillion = 5,
        Sextillion = 6,
        Septillion = 7,
        Octillion = 8,
        Nonillion = 9,
        Decillion = 10,
        Undecillion = 11,
        Duodecillion = 12,
        Tredecillion = 13,
        Quattuordecillion = 14,
        Quindecillion = 15,
        Exdecillion = 16,
        Septendecillion = 17,
        Octodecillion = 18,
        Novemdecillion = 19,
        Vigintillion = 20,
        Centillion = 21,
        Max = -1//更大
    }

    /// <summary>
    /// Double转换成带单位的字符
    /// </summary>
    /// <param name="number"></param>
    /// <param name="outNumberStr"></param>
    /// <param name="outUnit"></param>
    public static void DoubleToStrUnit(double number,out string outNumberStr,out UnitEnum outUnit)
    {
        DoubleToStrUnitKeepNumber(number,0,out outNumberStr,out outUnit);
    }

    public static void DoubleToStrUnitKeepNumber(double number,int keepNumber, out string outNumberStr, out UnitEnum outUnit)
    {
        string numberStr = number.ToString("f0");
        outUnit = GetUnitByLength(numberStr.Length);
        string tempNumberStr = number.ToString("f"+ keepNumber + ""); 
        switch (outUnit)
        {
            case UnitEnum.None:
                outNumberStr = string.Format("{0:N"+ keepNumber + "}", double.Parse(tempNumberStr));
                break;
            case UnitEnum.Max:
                outNumberStr = "∞";
                break;
            default:
                double tempNumber = (number / Math.Pow(10, 6 + (int)(outUnit - 1) * 3));
                outNumberStr = Math.Truncate(tempNumber * 1000) / 1000 + "";
                break;
        }
    }

    /// <summary>
    /// 根据数字长度获取单位
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public static UnitEnum GetUnitByLength(int length)
    {
        if (length > 0 && length < 7)
        {
            return UnitEnum.None;
        }
        else
        {
            int tempLength = length - 7;
            int unit = tempLength / 3 + 1;
            try
            {
                return (UnitEnum)unit;
            }
            catch (Exception)
            {
                return UnitEnum.Max;
            }
        }
    }
}