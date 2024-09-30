using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Rendering.InspectorCurveEditor;

public class HierarchySelectPopupSelect : PopupWindowContent
{
    public Action<HashSet<int>> callBackForSelect;
    public Component[] listComponent;
    public HashSet<int> selectData = new HashSet<int>();

    public Vector2 scrollViewPosition = Vector2.zero;

    public HierarchySelectPopupSelect(Action<HashSet<int>> callBackForSelect, Component[] listComponent, HashSet<int> selectData)
    {
        this.callBackForSelect = callBackForSelect;
        this.listComponent = listComponent;
        this.selectData = selectData;
    }

    public override void OnGUI(Rect rect)
    {
        scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);
        if (listComponent.IsNull())
        {
            editorWindow.Close();
            return;
        }
        for (int i = 0; i < listComponent.Length; i++)
        {
            var itemComponentName = listComponent[i].GetType().Name;
            int targetIndex = i;
            bool isSelect = selectData.Contains(targetIndex);
            GUILayout.BeginHorizontal();
            bool newSelect = GUILayout.Toggle(isSelect, string.Empty);
            if (GUILayout.Button(itemComponentName))
            {
                if (isSelect)
                {
                    selectData.Remove(targetIndex);
                }
                else
                {
                    selectData.Add(targetIndex);
                }
                callBackForSelect?.Invoke(selectData);
                //editorWindow.Close();
            }
            if (isSelect != newSelect)
            {
                if (isSelect)
                {
                    selectData.Remove(targetIndex);
                }
                else
                {
                    selectData.Add(targetIndex);
                }
                callBackForSelect?.Invoke(selectData);
            }

            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }
}