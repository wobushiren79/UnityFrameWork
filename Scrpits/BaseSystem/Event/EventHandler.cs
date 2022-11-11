using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventHandler : BaseSingleton<EventHandler>
{
    //事件集合
    private Dictionary<string, EventEntity> _DicEvent;

    public Dictionary<string, EventEntity> DicEvent
    {
        get
        {
            if (_DicEvent == null)
                _DicEvent = new Dictionary<string, EventEntity>();
            return _DicEvent;
        }
    }

    #region 注册事件
    public void RegisterEvent(string eventName, Action handler)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            eventEntity = new EventSignal();
            DicEvent.Add(eventName, eventEntity);
        }
               ((EventSignal)eventEntity).Subscribe(handler);
    }

    public void RegisterEvent<T>(string eventName, Action<T> handler)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            eventEntity = new EventSignal<T>();
            DicEvent.Add(eventName, eventEntity);
        }
        if (eventEntity is EventSignal<T> t)
        {
            t.Subscribe(handler);
        }
        else
        {
            Debug.LogError($"{eventName} 对应的类型错误 :{handler.GetType().FullName} ");
        }
    }

    public void RegisterEvent<T, V>(string eventName, Action<T, V> handler)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            eventEntity = new EventSignal<T, V>();
            DicEvent.Add(eventName, eventEntity);
        }
        if (eventEntity is EventSignal<T, V> t)
        {
            t.Subscribe(handler);
        }
        else
        {
            Debug.LogError($"{eventName} 对应的类型错误 :{handler.GetType().FullName} ");
        }
    }

    public void RegisterEvent<T, V, A>(string eventName, Action<T, V, A> handler)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            eventEntity = new EventSignal<T, V, A>();
            DicEvent.Add(eventName, eventEntity);
        }
        if (eventEntity is EventSignal<T, V, A> t)
        {
            t.Subscribe(handler);
        }
        else
        {
            Debug.LogError($"{eventName} 对应的类型错误 :{handler.GetType().FullName} ");
        }
    }

    public void RegisterEvent<T, V, A, B>(string eventName, Action<T, V, A, B> handler)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            eventEntity = new EventSignal<T, V, A, B>();
            DicEvent.Add(eventName, eventEntity);
        }
        if (eventEntity is EventSignal<T, V, A, B> t)
        {
            t.Subscribe(handler);
        }
        else
        {
            Debug.LogError($"{eventName} 对应的类型错误 :{handler.GetType().FullName} ");
        }
    }
    #endregion

    #region 注销事件
    public void UnRegisterEvent(string eventName, Action handler)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            return;
        }
      ((EventSignal)eventEntity).UnSubscribe(handler);
    }

    public void UnRegisterEvent<T>(string eventName, Action<T> handler)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            return;
        }
        if (eventEntity is EventSignal<T> t)
        {
            t.UnSubscribe(handler);
        }
        else
        {
            Debug.LogError($"{eventName} 对应的类型错误 :{handler.GetType().FullName} ");
        }
    }

    public void UnRegisterEvent<T, V>(string eventName, Action<T, V> handler)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            return;
        }
        if (eventEntity is EventSignal<T, V> t)
        {
            t.UnSubscribe(handler);
        }
        else
        {
            Debug.LogError($"{eventName} 对应的类型错误 :{handler.GetType().FullName} ");
        }
    }

    public void UnRegisterEvent<T, V, A>(string eventName, Action<T, V, A> handler)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            return;
        }
        if (eventEntity is EventSignal<T, V, A> t)
        {
            t.UnSubscribe(handler);
        }
        else
        {
            Debug.LogError($"{eventName} 对应的类型错误 :{handler.GetType().FullName} ");
        }
    }

    public void UnRegisterEvent<T, V, A, B>(string eventName, Action<T, V, A, B> handler)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            return;
        }
        if (eventEntity is EventSignal<T, V, A, B> t)
        {
            t.UnSubscribe(handler);
        }
        else
        {
            Debug.LogError($"{eventName} 对应的类型错误 :{handler.GetType().FullName} ");
        }
    }

    public void UnRegisterEvent(string eventName)
    {
        if (DicEvent.ContainsKey(eventName))
        {
            DicEvent[eventName].Dispose();
            DicEvent.Remove(eventName);
        }
    }
    #endregion

    #region 触发事件
    public void TriggerEvent(string eventName)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            //LogUtil.Log($"没有名字为{eventName}的事件");
            return;
        }
        if (eventEntity is EventSignal t)
        {
            t.Run();
        }
    }

    public void TriggerEvent<T>(string eventName, T arg1)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            //LogUtil.Log($"没有名字为{eventName}的事件");
            return;
        }
        if (eventEntity is EventSignal<T> t)
        {
            t.Run(arg1);
        }
    }

    public void TriggerEvent<T, U>(string eventName, T arg1, U arg2)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
           // LogUtil.Log($"没有名字为{eventName}的事件");
            return;
        }
        if (eventEntity is EventSignal<T, U> t)
        {
            t.Run(arg1, arg2);
        }
    }

    public void TriggerEvent<T, U, V>(string eventName, T arg1, U arg2, V arg3)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            //LogUtil.Log($"没有名字为{eventName}的事件");
            return;
        }
        if (eventEntity is EventSignal<T, U, V> t)
        {
            t.Run(arg1, arg2, arg3);
        }
    }

    public void TriggerEvent<T, U, V, W>(string eventName, T arg1, U arg2, V arg3, W arg4)
    {
        if (!DicEvent.TryGetValue(eventName, out EventEntity eventEntity))
        {
            //LogUtil.Log($"没有名字为{eventName}的事件");
            return;
        }
        if (eventEntity is EventSignal<T, U, V, W> t)
        {
            t.Run(arg1, arg2, arg3, arg4);
        }
    }
    #endregion

    #region 清理
    public void Clear()
    {
        foreach (var item in DicEvent)
        {
            item.Value.Dispose();
        }
        DicEvent.Clear();
    }
    #endregion
}

