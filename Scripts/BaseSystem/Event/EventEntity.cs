using System;
using UnityEditor;
using UnityEngine;

public interface IEventEntity : System.IDisposable
{

}

public class EventSignal : IEventEntity
{
    Action action;

    public void Run()
    {
        this.action?.Invoke();
    }

    public void UnSubscribe(Action action)
    {
        this.action -= action;
    }

    public void Subscribe(Action action)
    {
        this.action += action;
    }

    public void Dispose()
    {
        this.action = null;
    }
}

public class EventSignal<T> : IEventEntity
{
    Action<T> action;

    public void Run(T o)
    {
        action?.Invoke(o);
    }

    public void UnSubscribe(Action<T> action)
    {
        this.action -= action;
    }

    public void Subscribe(Action<T> action)
    {
        this.action += action;
    }

    public void Dispose()
    {
        this.action = null;
    }
}

public class EventSignal<T, U> : IEventEntity
{
    Action<T, U> action;

    public void Run(T t, U u)
    {
        action?.Invoke(t, u);
    }

    public void UnSubscribe(Action<T, U> action)
    {
        this.action -= action;
    }

    public void Subscribe(Action<T, U> action)
    {
        this.action += action;
    }

    public void Dispose()
    {
        this.action = null;
    }
}

public class EventSignal<T, U, V> : IEventEntity
{
    Action<T, U, V> action;

    public void Run(T t, U u, V v)
    {
        action?.Invoke(t, u, v);
    }

    public void UnSubscribe(Action<T, U, V> action)
    {
        this.action -= action;
    }

    public void Subscribe(Action<T, U, V> action)
    {
        this.action += action;
    }

    public void Dispose()
    {
        this.action = null;
    }
}

public class EventSignal<T, U, V, W> : IEventEntity
{
    Action<T, U, V, W> action;

    public void Run(T t, U u, V v, W w)
    {
        action?.Invoke(t, u, v, w);
    }

    public void UnSubscribe(Action<T, U, V, W> action)
    {
        this.action -= action;
    }

    public void Subscribe(Action<T, U, V, W> action)
    {
        this.action += action;
    }

    public void Dispose()
    {
        this.action = null;
    }
}