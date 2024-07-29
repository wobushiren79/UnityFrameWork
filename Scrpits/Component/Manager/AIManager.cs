using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIManager : BaseManager
{
    //当前的所有AI实例
    public List<AIBaseEntity> listAIEntity = new List<AIBaseEntity>();

    //AI缓存池
    public Dictionary<string, Queue<AIBaseEntity>> poolAIEntity = new Dictionary<string, Queue<AIBaseEntity>>();

    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear()
    {
        listAIEntity.Clear();
        poolAIEntity.Clear();
    }

    /// <summary>
    /// 创建AI实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T CreateAIEntity<T>() where T : AIBaseEntity
    {
        string nameAIEntity = typeof(T).Name;
        T targetAIEntity = null; 
        if (poolAIEntity.TryGetValue(nameAIEntity, out Queue<AIBaseEntity> itemPool))
        {
            if (itemPool.Count > 0)
            {
                targetAIEntity = itemPool.Dequeue() as T;
            }
        }
        if (targetAIEntity == null)
        {
            targetAIEntity = ReflexUtil.CreateInstance<T>();
            targetAIEntity.InitData();
        }
        listAIEntity.Add(targetAIEntity);
        return targetAIEntity;
    }

    /// <summary>
    /// 移除AI实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="targetAIEntity"></param>
    public void RemoveAIEntity<T>(T targetAIEntity) where T : AIBaseEntity
    {
        listAIEntity.Remove(targetAIEntity);
        Type targetType = targetAIEntity.GetType();
        string nameAIEntity = targetType.Name;
        if (poolAIEntity.TryGetValue(nameAIEntity, out Queue<AIBaseEntity> itemPool))
        {
            itemPool.Enqueue(targetAIEntity);
        }
        else
        {
            Queue<AIBaseEntity> newItemPool = new Queue<AIBaseEntity>();
            newItemPool.Enqueue(targetAIEntity);
            poolAIEntity.Add(nameAIEntity, newItemPool);
        }
    }
}
