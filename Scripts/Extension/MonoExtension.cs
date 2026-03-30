using UnityEditor;
using UnityEngine;
using System.Collections;
using System;

public static class MonoExtension
{
    /// <summary>
    /// 延迟执行-秒
    /// </summary>
    /// <param name="mono"></param>
    /// <param name="seconds"></param>
    /// <param name="action"></param>
    public static void WaitExecuteSeconds(this MonoBehaviour mono, float seconds, Action action)
    {
        mono.StartCoroutine(CoroutineForDelayExecuteSeconds(seconds, action));
    }

    /// <summary>
    /// 延迟执行-真实时间
    /// </summary>
    /// <param name="mono"></param>
    /// <param name="seconds"></param>
    /// <param name="action"></param>
    public static void WaitExecuteSecondsRealtime(this MonoBehaviour mono, float seconds, Action action)
    {
        mono.StartCoroutine(CoroutineForDelayExecuteSecondsRealtime(seconds, action));
    }

    /// <summary>
    /// 延迟执行-当前帧结束
    /// </summary>
    /// <param name="mono"></param>
    /// <param name="numberFrame"></param>
    /// <param name="action"></param>
    public static void WaitExecuteEndOfFrame(this MonoBehaviour mono, int numberFrame, Action action)
    {
        mono.StartCoroutine(CoroutineForDelayExecuteEndOfFrame(numberFrame, action));
    }

    /// <summary>
    /// 延迟执行-fixedUpdate
    /// </summary>
    /// <param name="mono"></param>
    /// <param name="numberFix"></param>
    /// <param name="action"></param>
    public static void WaitExecuteFixedUpdate(this MonoBehaviour mono, int numberFix, Action action)
    {
        mono.StartCoroutine(CoroutineForDelayExecuteFixedUpdate(numberFix, action));
    }


    public static IEnumerator CoroutineForDelayExecuteSeconds(float timeWait, Action action)
    {
        yield return new WaitForSeconds(timeWait);
        action?.Invoke();
    }

    public static IEnumerator CoroutineForDelayExecuteSecondsRealtime(float timeWait, Action action)
    {
        yield return new WaitForSecondsRealtime(timeWait);
        action?.Invoke();
    }

    public static IEnumerator CoroutineForDelayExecuteEndOfFrame(int numberFrame, Action action)
    {
        for (int i = 0; i < numberFrame; i++)
        {
            yield return new WaitForEndOfFrame();
        }
        action?.Invoke();
    }
    public static IEnumerator CoroutineForDelayExecuteFixedUpdate(int numberFix, Action action)
    {
        for (int i = 0; i < numberFix; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        action?.Invoke();
    }
}