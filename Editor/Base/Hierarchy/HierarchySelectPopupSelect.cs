using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class HierarchySelectPopupSelect : PopupWindowContent
{
    public Action<int> callBackForSelect;
    public string[] listData;
    public int selectIndex = 0;

    public Vector2 scrollViewPosition = Vector2.zero;

    public HierarchySelectPopupSelect(Action<int> callBackForSelect, string[] listData)
    {
        this.callBackForSelect = callBackForSelect;
        this.listData = listData;
    }

    public override void OnGUI(Rect rect)
    {
        scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);
        if (listData.IsNull())
        {
            return;
        }
        for (int i = 0; i < listData.Length; i++)
        {
            if (GUILayout.Button(listData[i]))
            {
                this.selectIndex = i;
                callBackForSelect?.Invoke(selectIndex);
                editorWindow.Close();
            }
        }
        GUILayout.EndScrollView();
    }
}