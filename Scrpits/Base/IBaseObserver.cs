using UnityEngine;
using UnityEditor;

public interface IBaseObserver 
{
    /// <summary>
    /// 被观察者数据更新
    /// </summary>
    /// <param name="type"></param>
    /// <param name="obj"></param>
    void ObserbableUpdate<T>(T observable, int type,params System.Object[] obj) where T : Object;
}