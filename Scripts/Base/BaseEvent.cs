using System;
using System.Collections.Generic;

public class BaseEvent
{
   // 定义内部类保存事件名称及注销委托
    private class EventRegistration
    {
        public string EventName { get; set; }
        public Action UnregisterAction { get; set; }
    }

    // 保存所有注册事件的注销委托
    private List<EventRegistration> eventRegistrations = new List<EventRegistration>();

    #region 事件相关

    /// <summary>
    /// 注销当前实例注册的所有事件
    /// </summary>
    public virtual void UnRegisterAllEvent()
    {
        // 遍历执行所有注销委托
        foreach (var registration in eventRegistrations)
        {
            registration.UnregisterAction();
        }
        eventRegistrations.Clear();
    }

    /// <summary>
    /// 注销指定事件名的所有当前实例注册的Action
    /// </summary>
    public virtual void UnRegisterEvent(string eventName)
    {
        // 逆序遍历避免索引错位
        for (int i = eventRegistrations.Count - 1; i >= 0; i--)
        {
            if (eventRegistrations[i].EventName == eventName)
            {
                eventRegistrations[i].UnregisterAction();
                eventRegistrations.RemoveAt(i);
            }
        }
    }

    // 注册事件并保存注销委托
    public void RegisterEvent(string eventName, Action action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        eventRegistrations.Add(new EventRegistration
        {
            EventName = eventName,
            UnregisterAction = () => EventHandler.Instance.UnRegisterEvent(eventName, action)
        });
    }

    public void RegisterEvent<A>(string eventName, Action<A> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        eventRegistrations.Add(new EventRegistration
        {
            EventName = eventName,
            UnregisterAction = () => EventHandler.Instance.UnRegisterEvent(eventName, action)
        });
    }

    public void RegisterEvent<A, B>(string eventName, Action<A, B> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        eventRegistrations.Add(new EventRegistration
        {
            EventName = eventName,
            UnregisterAction = () => EventHandler.Instance.UnRegisterEvent(eventName, action)
        });
    }

    public void RegisterEvent<A, B, C>(string eventName, Action<A, B, C> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        eventRegistrations.Add(new EventRegistration
        {
            EventName = eventName,
            UnregisterAction = () => EventHandler.Instance.UnRegisterEvent(eventName, action)
        });
    }

    public void RegisterEvent<A, B, C, D>(string eventName, Action<A, B, C, D> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        eventRegistrations.Add(new EventRegistration
        {
            EventName = eventName,
            UnregisterAction = () => EventHandler.Instance.UnRegisterEvent(eventName, action)
        });
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