using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BaseObservable<T> : BaseMonoBehaviour
    where T : IBaseObserver
{
    private List<T> mObserverList;

    /// <summary>
    /// 获取所有观察者
    /// </summary>
    /// <returns></returns>
    public List<T> GetAllObserver()
    {
        if (mObserverList == null)
            mObserverList = new List<T>();
        return mObserverList;
    }

    /// <summary>
    /// 增加观察者
    /// </summary>
    /// <param name="observer"></param>
    public void AddObserver(T observer)
    {
        if (observer == null)
            return;
        if (mObserverList == null)
            mObserverList = new List<T>();
        mObserverList.Add(observer);
    }

    /// <summary>
    /// 增加观察者列表
    /// </summary>
    /// <param name="observerList"></param>
    public void AddObserver(List<T> observerList)
    {
        if (observerList.IsNull())
            return;
        if (mObserverList == null)
            mObserverList = new List<T>();
        mObserverList.AddRange(observerList);
    }

    /// <summary>
    /// 移除观察者
    /// </summary>
    /// <param name="observer"></param>
    public void RemoveObserver(T observer)
    {
        if (mObserverList.IsNull() || observer == null)
            return;
        if (mObserverList.Contains(observer))
            mObserverList.Remove(observer);
    }

    /// <summary>
    /// 移除观察者列表
    /// </summary>
    /// <param name="observerList"></param>
    public void RemoveObserverList(List<T> observerList)
    {
        if (mObserverList.IsNull() || observerList.IsNull())
            return;
        for (int i = 0; i < observerList.Count; i++)
        {
            RemoveObserver(observerList[i]);
        }
    }

    /// <summary>
    /// 移除所有观察者
    /// </summary>
    public void RemoveAllObserver()
    {
        if (mObserverList.IsNull())
            return;
        mObserverList.Clear();
    }

    /// <summary>
    /// 通知所有观察者
    /// </summary>
    /// <param name="type"></param>
    /// <param name="objs"></param>
    public void NotifyAllObserver(int type, params System.Object[] objs)
    {
        if (mObserverList.IsNull())
            return;
        foreach (T item in mObserverList)
        {
            try
            {
                item.ObserbableUpdate(this, type, objs);
            } catch
            {

            }
        }
    }

    /// <summary>
    /// 通知其中一个观察者
    /// </summary>
    /// <param name="observer"></param>
    /// <param name="type"></param>
    /// <param name="objs"></param>
    public void NotifyObserver(T observer, int type, params System.Object[] objs)
    {
        if (mObserverList.IsNull())
            return;
        foreach (T item in mObserverList)
        {
            if (item.Equals(observer))
            {
                item.ObserbableUpdate(this, type, objs);
            }
        }
    }
}