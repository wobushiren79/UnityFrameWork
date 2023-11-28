using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

public class OpenEditor : Editor
{
    [MenuItem("Custom/Open/使用Notepad++打开文件", false, 1)]
    static void OpenByNotepadPP()
    {
        if (Selection.objects.Length != 1)//由于文本文件不属于GameObject类型，因此这里使用Selection.gameObjects是无法获取选择的文本文件的
        {
            UnityEngine.Debug.LogError("you can only open one file per time");
            return;
        }
        UnityEngine.Object obj = Selection.objects[0];
        string path = Application.dataPath + AssetDatabase.GetAssetPath(obj).Substring(6);
        if (!(path.EndsWith(".cs") || path.EndsWith(".txt")
            || path.EndsWith(".bin") || path.EndsWith(".xml")
            || path.EndsWith(".shader")
            || path.EndsWith(".bin")))
        {
            UnityEngine.Debug.LogError("you can only open .cs .txt .bin .xml .shader .bin file");
            return;
        }
        UnityEngine.Debug.Log(path + ",opened");
        //Process.Start(path);//使用默认打开方式打开文本文件
        Process.Start("notepad++.exe", path);//使用notepad++打开，需要机器安装了该软件
    }

    [MenuItem("Custom/Open/打开存档路径")]
    static void OpenPersistantPath()
    {
        string path = Application.persistentDataPath;
        EditorUI.OpenFolder(path);
    }


    [InitializeOnLoadMethod]
    public static void Init()
    {
        ToolbarExtension.ToolbarZoneLeftAlign += OnToolbarGUI;
    }

    static void OnToolbarGUI(VisualElement rootVisualElement)
    {
        var refresh = new EditorToolbarDropdown();
        refresh.text = "打开存档路径";
        refresh.clicked += () =>
        {
            OpenPersistantPath();
        };

        var m_TextElement = refresh.Q<TextElement>(className: "unity-editor-toolbar-element__label");
        var ArrowElement = refresh.Q(className: "unity-icon-arrow");

        m_TextElement.style.width = 100;
        m_TextElement.style.textOverflow = TextOverflow.Clip;
        m_TextElement.style.unityTextAlign = TextAnchor.MiddleCenter;
        ArrowElement.style.display = DisplayStyle.None;

        rootVisualElement.Add(refresh);
    }
}
