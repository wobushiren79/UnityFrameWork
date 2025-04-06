using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class EffectManager : BaseManager
{
    //粒子模型列表
    public Dictionary<string, GameObject> dicEffectModel = new Dictionary<string, GameObject>();
    //闲置粒子列表
    public Dictionary<string, Queue<EffectBase>> dicPoorEffect = new Dictionary<string, Queue<EffectBase>>();

    //当前展示的粒子
    public List<EffectBase> listEffect = new List<EffectBase>();
    //当前展示的粒子-持久
    public Dictionary<string, EffectBase> dicEffectForEnduring = new Dictionary<string, EffectBase>();

    public string pathEffect = "Assets/LoadResources/Effects";

    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < listEffect.Count; i++)
        {
            var itemData = listEffect[i];
            itemData.Clear();
        }
        listEffect.Clear();
        foreach (var item in dicPoorEffect)
        {
            var listPoor = item.Value;
            while (listPoor.Count > 0)
            {
                var itemView = listPoor.Dequeue();
                itemView.Clear();
            }
        }
        dicPoorEffect.Clear();
        dicEffectForEnduring.Clear();
    }

    /// <summary>
    /// 获取持续性粒子
    /// </summary>
    /// <param name="effectName"></param>
    /// <param name="actionForGet"></param>
    public void GetEffectForEnduring(string effectName, Action<EffectBase> actionForGet)
    {
        if (dicEffectForEnduring.TryGetValue(effectName, out EffectBase targetEffect))
        {
            actionForGet?.Invoke(targetEffect);
            return;
        }
        else
        {
            EffectBean effectData = new EffectBean();
            effectData.effectName = effectName;
            effectData.effectType = EffectTypeEnum.Visual;
            effectData.effectShowType = EffectShowTypeEnum.Enduring;
            effectData.isPlayInShow = false;
            effectData.timeForShow = -1;
            GetEffect(gameObject, effectData, (effectBase) =>
            {
                actionForGet?.Invoke(effectBase);
            });
        }
    }

    /// <summary>
    /// 创建粒子-一次性
    /// </summary>
    public void GetEffect(GameObject objContainer, EffectBean effectData, Action<EffectBase> completeAction)
    {
        if (dicPoorEffect.TryGetValue(effectData.effectName, out Queue<EffectBase> listPoorEffect))
        {
            if (listPoorEffect.Count > 0)
            {
                EffectBase effect = listPoorEffect.Dequeue();
                effect.SetData(effectData);
                effect.ShowObj(true);
                listEffect.Add(effect);
                //持续性粒子列表添加
                if (effectData.effectShowType == EffectShowTypeEnum.Enduring)
                {
                    if (!dicEffectForEnduring.ContainsKey(effectData.effectName))
                        dicEffectForEnduring.Add(effectData.effectName, effect);
                }
                completeAction?.Invoke(effect);
                return;
            }
        }

        //同步
        GameObject objEffectsModel = GetModelForAddressablesSync(dicEffectModel, $"{pathEffect}/{effectData.effectName}.prefab");
        GameObject objEffects = Instantiate(objContainer, objEffectsModel);
        objEffects.ShowObj(true);
        EffectBase effectTarget = objEffects.GetComponent<EffectBase>();
        if (effectTarget != null)
        {
            effectTarget.SetData(effectData);
            listEffect.Add(effectTarget);
            //持续性粒子列表添加
            if (effectData.effectShowType == EffectShowTypeEnum.Enduring)
            {
                if (!dicEffectForEnduring.ContainsKey(effectData.effectName))
                    dicEffectForEnduring.Add(effectData.effectName, effectTarget);
            }
        }
        else
        {
            LogUtil.LogError($"粒子 {effectData.effectName} 没有 EffectBase");
        }
        completeAction?.Invoke(effectTarget);
        //异步
        //GetModelForAddressables(dicEffectModel, $"{pathEffect}/{effectData.effectName}.prefab", (obj) =>
        //{
        //    GameObject objEffects = Instantiate(objContainer, obj);
        //    objEffects.ShowObj(true);
        //    EffectBase effect = objEffects.GetComponent<EffectBase>();
        //    if (effect != null)
        //    {
        //        effect.SetData(effectData);
        //    }
        //    completeAction?.Invoke(effect);
        //});
    }

    /// <summary>
    /// 删除粒子
    /// </summary>
    /// <param name="effect"></param>
    public void DestoryEffect(EffectBase effect)
    {
        EffectBean effectData = effect.effectData;
        listEffect.Remove(effect);
        if (dicPoorEffect.TryGetValue(effectData.effectName, out Queue<EffectBase> listPoorEffect))
        {
            listPoorEffect.Enqueue(effect);
        }
        else
        {
            Queue<EffectBase> listEffect = new Queue<EffectBase>();
            listEffect.Enqueue(effect);
            dicPoorEffect.Add(effectData.effectName, listEffect);
        }
        //持续性粒子列表删除
        if (effectData.effectShowType == EffectShowTypeEnum.Enduring)
        {
            if(dicEffectForEnduring.ContainsKey(effectData.effectName))
                dicEffectForEnduring.Remove(effectData.effectName);
        }
        effect.ShowObj(false);
    }
}