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
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        EffectBase targetEffect = target.GetComponent<EffectBase>();
        VisualEffect[] visualEffects = targetEffect.GetComponentsInChildren<VisualEffect>();
        ParticleSystem[] particlesSystems = targetEffect.GetComponentsInChildren<ParticleSystem>();

        targetEffect.listVE = new List<VisualEffect>(visualEffects);
        targetEffect.listPS = new List<ParticleSystem>(particlesSystems);

        EditorUtility.SetDirty(target);
        EditorUtil.RefreshAsset();
    }

}