using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

[InitializeOnLoad]
[CustomEditor(typeof(EffectBase), true)]
public class InspectorEffectBase : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (!EditorUtil.CheckIsPrefabMode())
        {
            return;
        }
        GUILayout.Space(50);
        if (EditorUI.GUIButton("初始化设置", 200))
        {
            InitData();
        }
        GUILayout.Space(10);
        if (EditorUI.GUIButton("初始化设置(优先主粒子)", 200))
        {
            InitDataWithMainPS();
        }
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        EffectBase targetEffect = target.GetComponent<EffectBase>();
        VisualEffect[] visualEffects = targetEffect.GetComponentsInChildren<VisualEffect>();
        ParticleSystem[] particlesSystems = targetEffect.GetComponentsInChildren<ParticleSystem>();

        targetEffect.mainPS = null;
        targetEffect.listVE = new List<VisualEffect>(visualEffects);
        targetEffect.listPS = new List<ParticleSystem>(particlesSystems);

        EditorUtility.SetDirty(target);
        EditorUtil.RefreshAsset();
    }

    /// <summary>
    /// 初始化数据，优先将根节点自身的ParticleSystem设为mainPS，有mainPS则不再填充listPS
    /// </summary>
    public void InitDataWithMainPS()
    {
        EffectBase targetEffect = target.GetComponent<EffectBase>();
        VisualEffect[] visualEffects = targetEffect.GetComponentsInChildren<VisualEffect>();

        if (targetEffect.TryGetComponent(out ParticleSystem rootPS))
        {
            targetEffect.mainPS = rootPS;
            targetEffect.listPS = new List<ParticleSystem>();
        }
        else
        {
            targetEffect.mainPS = null;
            ParticleSystem[] particlesSystems = targetEffect.GetComponentsInChildren<ParticleSystem>();
            targetEffect.listPS = new List<ParticleSystem>(particlesSystems);
        }

        targetEffect.listVE = new List<VisualEffect>(visualEffects);

        EditorUtility.SetDirty(target);
        EditorUtil.RefreshAsset();
    }

}