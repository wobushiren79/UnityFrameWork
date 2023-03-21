using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class EffectBase : BaseMonoBehaviour
{
    public List<ParticleSystem> listPS = new List<ParticleSystem>();
    public List<VisualEffect> listVE = new List<VisualEffect>();

    [HideInInspector]
    public EffectBean effectData;

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
    /// 播放粒子
    /// </summary>
    public virtual void PlayEffect()
    {
        if (!listPS.IsNull()) 
        {
            for (int i = 0; i < listPS.Count; i++)
            {
                ParticleSystem itemPS = listPS[i];
                itemPS.Play();
            }
        }
        if (!listVE.IsNull())
        {
            for (int i = 0; i < listVE.Count; i++)
            {
                VisualEffect itemVE = listVE[i];
                itemVE.SendEvent("OnPlay");
            }
        }
    }

    /// <summary>
    /// 停止粒子
    /// </summary>
    public virtual void StopEffect()
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
                itemVE.SendEvent("OnStop");
            }
        }

    }
}