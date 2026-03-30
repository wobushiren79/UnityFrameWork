using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class EffectBase : BaseMonoBehaviour
{
    public ParticleSystem mainPS;
    public List<ParticleSystem> listPS = new List<ParticleSystem>();
    public List<VisualEffect> listVE = new List<VisualEffect>();
    protected float[] originEffectSize;

    [HideInInspector]
    public EffectBean effectData;

    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear()
    {
        if (gameObject != null)
            Destroy(gameObject);
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="effectData"></param>
    public virtual void SetData(EffectBean effectData)
    {
        if (effectData == null)
            return;
        this.effectData = effectData;
        transform.position = effectData.effectPosition;
        //是否展示的时候 马上播放
        if (effectData.isPlayInShow)
        {
            PlayEffect();
        }
    }

    /// <summary>
    /// 获取特定的粒子特效
    /// </summary>
    public VisualEffect GetVisualEffect(int index = 0)
    {
        if(listVE.IsNull())
            return null;
        return listVE[index];
    }

    /// <summary>
    /// 获取特定的粒子特效
    /// </summary>
    public ParticleSystem GetParticleSystem(int index = 0)
    {        
        if(listPS.IsNull())
            return null;
        return listPS[index];
    }

    /// <summary>
    /// 播放粒子
    /// </summary>
    public virtual void PlayEffect(string sendEvent = "OnPlay")
    {
        if (!listPS.IsNull())
        {
            //如果有主粒子 播放主粒子就行
            if (mainPS != null)
            {
                mainPS.Play();
            }
            else
            {
                for (int i = 0; i < listPS.Count; i++)
                {
                    ParticleSystem itemPS = listPS[i];
                    itemPS.Play();
                }
            }
        }
        if (!listVE.IsNull())
        {
            for (int i = 0; i < listVE.Count; i++)
            {
                VisualEffect itemVE = listVE[i];
                itemVE.SendEvent(sendEvent);
            }
        }
    }

    /// <summary>
    /// 停止粒子
    /// </summary>
    public virtual void StopEffect(string sendEvent = "OnStop")
    {
        if (!listPS.IsNull())
        {
            for (int i = 0; i < listPS.Count; i++)
            {
                ParticleSystem itemPS = listPS[i];
                itemPS.Stop();
            }
        }
        if (!listVE.IsNull())
        {
            for (int i = 0; i < listVE.Count; i++)
            {
                VisualEffect itemVE = listVE[i];
                itemVE.SendEvent(sendEvent);
            }
        }

    }

    /// <summary>
    /// 设置PS系统的起始位置
    /// </summary>
    /// <param name="position"></param>
    public void SetParticleSystemStartPosition(Vector3 position)
    {
        if (listPS.IsNull())
            return;
        for (int i = 0; i < listPS.Count; i++)
        {
            ParticleSystem itemPS = listPS[i];
            var shapeModule = itemPS.shape;
            shapeModule.position = position;
        }
    }

    /// <summary>
    /// 设置PS系统的起始大小
    /// </summary>
    public void SetParticleSystemSize(float size)
    {
        if (listPS.IsNull())
            return;
        if (originEffectSize == null)
        {
            originEffectSize = new float[listPS.Count];
            for (int i = 0; i < listPS.Count; i++)
            {
                ParticleSystem itemPS = listPS[i];
                var mainModule = itemPS.main;
                if (mainModule.startSize.mode == ParticleSystemCurveMode.Constant)
                {
                    originEffectSize[i] = mainModule.startSize.constant;
                }
            } 
        }
        for (int i = 0; i < listPS.Count; i++)
        {
            ParticleSystem itemPS = listPS[i];
            var mainModule = itemPS.main;
            if (mainModule.startSize.mode == ParticleSystemCurveMode.Constant)
            {
                mainModule.startSize = size * originEffectSize[i];
            }
        }
    }
}