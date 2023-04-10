using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class EffectManager : BaseManager
{
    //粒子模型列表
    public Dictionary<string, GameObject> dicEffectModel = new Dictionary<string, GameObject>();
    //闲置粒子列表
    public Dictionary<string, Queue<EffectBase>> dicIdleEffect = new Dictionary<string, Queue<EffectBase>>();

    /// <summary>
    /// 创建粒子
    /// </summary>
    /// <param name="objContainer"></param>
    /// <param name="effectData"></param>
    /// <param name="completeAction"></param>
    public void GetEffect(GameObject objContainer, EffectBean effectData, Action<EffectBase> completeAction)
    {
        if (dicIdleEffect.TryGetValue(effectData.effectName,out Queue<EffectBase> listIdleEffect))
        {
            if (listIdleEffect.Count > 0)
            {
                EffectBase effect = listIdleEffect.Dequeue();
                effect.SetData(effectData);
                effect.ShowObj(true);
                completeAction?.Invoke(effect);
                return;
            }
        }

        GetModelForAddressables(dicEffectModel, $"Assets/Prefabs/Effects/Common/{effectData.effectName}.prefab", (obj) =>
        {
            GameObject objEffects = Instantiate(objContainer, obj);
            objEffects.ShowObj(true);
            EffectBase effect = objEffects.GetComponent<EffectBase>();
            if (effect != null)
            {
                effect.SetData(effectData);       
            }
            completeAction?.Invoke(effect);
        });
    }

    /// <summary>
    /// 删除粒子
    /// </summary>
    /// <param name="effect"></param>
    public void DestoryEffect(EffectBase effect)
    {
        EffectBean effectData = effect.effectData;
        if (dicIdleEffect.TryGetValue(effectData.effectName, out Queue<EffectBase> listIdleEffect))
        {
            listIdleEffect.Enqueue(effect);
        }
        else
        {
            Queue<EffectBase> listEffect = new Queue<EffectBase>();
            listEffect.Enqueue(effect);
            dicIdleEffect.Add(effectData.effectName, listEffect);
        }
        effect.ShowObj(false);
    }
}