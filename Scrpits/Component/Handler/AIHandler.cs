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
    /// ����AI
    /// </summary>
    public T CreateAIEntity<T>(Action<T> actionBeforeStart = null) where T : AIBaseEntity
    {
        T targetAIEntity = manager.CreateAIEntity<T>();
        actionBeforeStart?.Invoke(targetAIEntity);
        //����AIʵ��
        targetAIEntity.StartAIEntity();
        return targetAIEntity;
    }

    /// <summary>
    /// �Ƴ�AIʵ��
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="targetAIEntity"></param>
    public void RemoveAIEntity<T>(T targetAIEntity) where T : AIBaseEntity
    {
        //�ر�AIʵ��
        targetAIEntity.CloseAIEntity();
        //�������
        targetAIEntity.ClearData();
        manager.RemoveAIEntity(targetAIEntity);
    }
}
