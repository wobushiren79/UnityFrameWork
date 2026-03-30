using UnityEngine;
using UnityEditor;
using System;
using System.Diagnostics;

public class TimeUtil
{
    /// <summary>
    /// 获取时间戳
    /// </summary>
    /// <returns></returns>
    public static long GetTimeStampForS64()
    {
        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds);
    }

    /// <summary>
    /// 获取时间戳
    /// </summary>
    /// <returns></returns>
    public static int GetTimeStampForS32()
    {
        TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt32(ts.TotalSeconds);
    }

    /// <summary>
    /// 获取时间戳
    /// </summary>
    /// <returns></returns>
    public static long GetTimeStampForMS64()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalMilliseconds);
    }


    //获取当前时间------------------------------------------------------------------------------------------------------------
    public static int GetNowTimeForYear()
    {
        return DateTime.Now.Year;
    }
    public static int GetNowTimeForMonth()
    {
        return DateTime.Now.Month;
    }
    public static int GetNowTimeForDay()
    {
        return DateTime.Now.Day;
    }
    public static int GetNowTimeForHour()
    {
        return DateTime.Now.Hour;
    }
    public static int GetNowTimeForMinute()
    {
        return DateTime.Now.Minute;
    }
    public static int GetNowTimeForSecond()
    {
        return DateTime.Now.Second;
    }
    public static TimeBean GetNowTime()
    {
        DateTime dateTime = DateTime.Now;
        TimeBean timeData = new TimeBean(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
        return timeData;
    }

    //比较时间------------------------------------------------------------------------------------------------------------
    public static TimeSpan SubtractTime(TimeBean timeOne, TimeBean timeTwo)
    {
        DateTime dateOne = timeOne.ToDateTime();
        DateTime dateTwo = timeTwo.ToDateTime();
        TimeSpan timeSpan = dateOne.Subtract(dateTwo);
        return timeSpan;
    }
    public static double SubtractTimeForDays(TimeBean timeOne, TimeBean timeTwo)
    {
        TimeSpan timeSpan = SubtractTime(timeOne, timeTwo);
        return timeSpan.Days;
    }
    public static double SubtractTimeForTotalDays(TimeBean timeOne, TimeBean timeTwo)
    {
        TimeSpan timeSpan = SubtractTime(timeOne, timeTwo);
        return timeSpan.TotalDays;
    }

    public static double SubtractTimeForHours(TimeBean timeOne, TimeBean timeTwo)
    {
        TimeSpan timeSpan = SubtractTime(timeOne, timeTwo);
        return timeSpan.Hours;
    }
    public static double SubtractTimeForTotalHours(TimeBean timeOne, TimeBean timeTwo)
    {
        TimeSpan timeSpan = SubtractTime(timeOne, timeTwo);
        return timeSpan.TotalHours;
    }

    public static double SubtractTimeForMinutes(TimeBean timeOne, TimeBean timeTwo)
    {
        TimeSpan timeSpan = SubtractTime(timeOne, timeTwo);
        return timeSpan.Minutes;
    }
    public static double SubtractTimeForTotalMinutes(TimeBean timeOne, TimeBean timeTwo)
    {
        TimeSpan timeSpan = SubtractTime(timeOne, timeTwo);
        return timeSpan.TotalMinutes;
    }

    public static double SubtractTimeForSeconds(TimeBean timeOne, TimeBean timeTwo)
    {
        TimeSpan timeSpan = SubtractTime(timeOne, timeTwo);
        return timeSpan.Seconds;
    }
    public static double SubtractTimeForTotalSeconds(TimeBean timeOne, TimeBean timeTwo)
    {
        TimeSpan timeSpan = SubtractTime(timeOne, timeTwo);
        return timeSpan.TotalSeconds;
    }

    /// <summary>
    /// 计算方法耗时
    /// </summary>
    /// <param name="mark"></param>
    /// <param name="findAct"></param>
    public static Stopwatch GetMethodTimeStart()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        return sw;
    }

    public static void GetMethodTimeEnd(string mark, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        LogUtil.Log("方法耗时" + mark + "：" + stopwatch.Elapsed.Ticks.ToString());
    }
}