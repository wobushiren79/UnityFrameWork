using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class StyleBaseWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/样式预览弹窗（用于学习）")]
    static void CreateWindows()
    {
        EditorWindow.GetWindow(typeof(StyleBaseWindow));
    }

    Vector2 scrollPosition = Vector2.zero;
    string searchStr = "";

    private void OnGUI()
    {
        GUILayout.BeginHorizontal("helpbox");
        GUILayout.Label("查找内置样式：");
        searchStr = GUILayout.TextField(searchStr, "SearchTextField");
        if (GUILayout.Button("", "SearchCancelButton"))
        {
            searchStr = "";
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, "box");
        foreach (GUIStyle style in GUI.skin)
        {
            if (style.name.ToLower().Contains(searchStr.ToLower()))
            {
                DrawStyle(style);
            }
        }
        GUILayout.EndScrollView();
    }

    void DrawStyle(GUIStyle style)
    {
        GUILayout.BeginHorizontal("box");
        GUILayout.Button(style.name, style.name, GUILayout.Width(200));
        GUILayout.FlexibleSpace();
        EditorGUILayout.SelectableLabel(style.name);
        if (GUILayout.Button("复制样式名称"))
        {
            EditorGUIUtility.systemCopyBuffer = style.name;
        }
        GUILayout.EndHorizontal();
    }


}