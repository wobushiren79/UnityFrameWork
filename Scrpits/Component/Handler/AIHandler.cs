using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHandler : BaseHandler<AIHandler, AIManager>
{

    public void Update()
    {
        if (manager.listAIEntity.Count > 0)
        {
            for (int i = 0; i < manager.listAIEntity.Count; i++)
            {
                var itemAIEntity = manager.listAIEntity[i];
                itemAIEntity.Update();
            }
        }
    }

    public void FixedUpdate()
    {
        if (manager.listAIEntity.Count > 0)
        {
            for (int i = 0; i < manager.listAIEntity.Count; i++)
            {
                var itemAIEntity = manager.listAIEntity[i];
                itemAIEntity.FixedUpdate();
            }
        }
    }

    /// <summary>
    /// 创建AI
    /// </summary>
    public T CreateAIEntity<T>(Action<T> actionBeforeStart = null) where T : AIBaseEntity
    {
        T targetAIEntity = manager.CreateAIEntity<T>();
        actionBeforeStart?.Invoke(targetAIEntity);
        //开启AI实例
        targetAIEntity.StartAIEntity();
        return targetAIEntity;
    }

    /// <summary>
    /// 移除AI实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="targetAIEntity"></param>
    public void RemoveAIEntity<T>(T targetAIEntity) where T : AIBaseEntity
    {
        //关闭AI实例
        targetAIEntity.CloseAIEntity();
        //清空数据
        targetAIEntity.ClearData();
        manager.RemoveAIEntity(targetAIEntity);
    }
}
