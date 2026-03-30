using UnityEngine;
using UnityEditor;

public abstract class BaseSingleton<T> where T : new()
{
    protected static  T instance;
    protected static object syncRoot = new Object();
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new T();
                    }
                }
            }
            return instance;
        }
    }
}