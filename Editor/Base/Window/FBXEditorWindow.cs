using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using UnityEditor;
using UnityEngine;

public class FBXEditorWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/FBX处理")]
    static void CreateWindows()
    {
        EditorWindow.GetWindowWithRect(typeof(FBXEditorWindow), new Rect(0, 0, 600, 800));
    }

    public UnityEngine.Object targetSelectMat;

    private void OnGUI()
    {
        //设置材质UI
        UIForSetMat();
    }

    /// <summary>
    /// 设置默认材质
    /// </summary>
    public void UIForSetMat()
    {
        EditorGUILayout.BeginHorizontal();
        EditorUI.GUIText("设置默认材质:");
        targetSelectMat = EditorGUILayout.ObjectField("材质", targetSelectMat, typeof(Material), true);
        if (EditorUI.GUIButton("设置材质"))
        {
            OnClickForSetFbxMat();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 设置设置材质
    /// </summary>
    public void OnClickForSetFbxMat()
    {
        var gameobjects =  EditorUtil.GetSelectionAll<GameObject>();
        if (gameobjects.Count == 0)
        {
            LogUtil.LogError("没有选中物体");
            return;
        }
        for (int i = 0; i < gameobjects.Count; i++)
        {
            var itemObj = gameobjects[i];
            LogUtil.Log($"SetFbxMat itemObj_{itemObj.name} type_{itemObj.GetType().Name}");
            FBXEditor.ChangeMaterial(itemObj, targetSelectMat as Material);
        }
    }
}