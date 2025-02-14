using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BaseEvent
{
    //所有注册事件
    private List<string> listEvents = new List<string>();

    #region 事件相关
    /// <summary>
    /// 游戏结束注销所有事件
    /// </summary>
    public virtual void UnRegisterAllEvent()
    {
        for (int i = 0; i < listEvents.Count; i++)
        {
            string itemEventName = listEvents[i];
            EventHandler.Instance.UnRegisterEvent(itemEventName);
        }
        listEvents.Clear();
    }

    public virtual void UnRegisterEvent(string eventName)
    {
        EventHandler.Instance.UnRegisterEvent(eventName);
        listEvents.Remove(eventName);
    }

    public void RegisterEvent(string eventName, Action action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }

    public void RegisterEvent<A>(string eventName, Action<A> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }
    public void RegisterEvent<A, B>(string eventName, Action<A, B> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }
    public void RegisterEvent<A, B, C>(string eventName, Action<A, B, C> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }
    public void RegisterEvent<A, B, C, D>(string eventName, Action<A, B, C, D> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }

    public virtual void TriggerEvent(string eventName)
    {
        EventHandler.Instance.TriggerEvent(eventName);
    }
    public virtual void TriggerEvent<A>(string eventName, A data)
    {
        EventHandler.Instance.TriggerEvent(eventName, data);
    }
    public virtual void TriggerEvent<A, B>(string eventName, A dataA, B dataB)
    {
        EventHandler.Instance.TriggerEvent(eventName, dataA, dataB);
    }
    public virtual void TriggerEvent<A, B, C>(string eventName, A dataA, B dataB, C dataC)
    {
        EventHandler.Instance.TriggerEvent(eventName, dataA, dataB, dataC);
    }
    public virtual void TriggerEvent<A, B, C, D>(string eventName, A dataA, B dataB, C dataC, D dataD)
    {
        EventHandler.Instance.TriggerEvent(eventName, dataA, dataB, dataC, dataD);
    }
    #endregion
}