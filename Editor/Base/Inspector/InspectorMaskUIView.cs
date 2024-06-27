using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
[CustomEditor(typeof(MaskUIView), true)]
public class InspectorMaskUIView : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (!EditorUtil.CheckIsPrefabMode())
        {
            return;
        }
        GUILayout.Space(50);
        if (EditorUI.GUIButton("收集所有控件", 200))
        {
            HandleForCollectUI();
        }
    }

    /// <summary>
    /// 手机所有控件
    /// </summary>
    public void HandleForCollectUI()
    {
        MaskUIView targetMask = target.GetComponent<MaskUIView>();
        targetMask.CollectAllGraphic();
        serializedObject.ApplyModifiedProperties();
    }
}