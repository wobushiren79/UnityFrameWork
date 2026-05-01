using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MVCEditorWindow : EditorWindow
{
    public string mvcClassName = "";

    protected readonly string scriptsTemplatesPath = "/FrameWork/Editor/ScriptsTemplates/";

    public enum SaveTypeEnum
    {
        FileJson = 0,
        SQLite = 1,
        Excel = 2
    }
    protected SaveTypeEnum saveType = SaveTypeEnum.FileJson;

    protected string mvcBeanPath = "Assets/Scripts/Bean/MVC";
    protected string mvcServicePath = "Assets/Scripts/MVC/Service";
    protected bool showPathSettings = false;

    [MenuItem("Custom/工具弹窗/创建数据模块")]
    static void CreateWindows()
    {
        var window = GetWindow<MVCEditorWindow>("创建数据模块");
        window.minSize = new Vector2(420, 240);
    }

    private void OnGUI()
    {
        GUILayout.Space(12);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        GUILayout.Label("快速创建数据模块 (Bean + Service)", titleStyle);
        GUILayout.Space(16);

        // 模块名称
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("模块名称", GUILayout.Width(80));
        mvcClassName = EditorGUILayout.TextField(mvcClassName);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);

        // 存储类型
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("存储类型", GUILayout.Width(80));
        saveType = (SaveTypeEnum)EditorGUILayout.EnumPopup(saveType);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(12);

        // 路径设置（可折叠）
        showPathSettings = EditorGUILayout.Foldout(showPathSettings, "高级路径设置", true);
        if (showPathSettings)
        {
            EditorGUI.indentLevel++;
            mvcBeanPath = PathField("Bean路径", mvcBeanPath);
            mvcServicePath = PathField("Service路径", mvcServicePath);
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(16);

        // 创建按钮
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button("创建", GUILayout.Height(36)))
        {
            CreateDataModule(mvcClassName, saveType);
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);

        // 底部提示
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };
        GUILayout.Label("基于 BaseDataService<T> 生成，可直接用于 JSON/SQLite/Excel 读写", hintStyle);
    }

    private string PathField(string label, string path)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(80));
        path = EditorGUILayout.TextField(path);
        if (GUILayout.Button("浏览", GUILayout.Width(50)))
        {
            string absPath = EditorUtility.OpenFolderPanel($"选择{label}", Application.dataPath, "");
            if (!string.IsNullOrEmpty(absPath))
            {
                path = ConvertToAssetPath(absPath);
            }
        }
        EditorGUILayout.EndHorizontal();
        return path;
    }

    private string ConvertToAssetPath(string absolutePath)
    {
        absolutePath = absolutePath.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');
        if (absolutePath.StartsWith(dataPath))
        {
            return "Assets" + absolutePath.Substring(dataPath.Length);
        }
        return absolutePath;
    }

    /// <summary>
    /// 创建数据模块
    /// </summary>
    public void CreateDataModule(string fileName, SaveTypeEnum type)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            EditorUtility.DisplayDialog("错误", "请输入模块名称", "确定");
            return;
        }

        string beanPath = Application.dataPath + scriptsTemplatesPath + "MVC_Bean.txt";
        string servicePath = "";

        switch (type)
        {
            case SaveTypeEnum.FileJson:
                servicePath = Application.dataPath + scriptsTemplatesPath + "MVC_Service_FileJson.txt";
                break;
            case SaveTypeEnum.SQLite:
                servicePath = Application.dataPath + scriptsTemplatesPath + "MVC_Service_SQLite.txt";
                break;
            case SaveTypeEnum.Excel:
                servicePath = Application.dataPath + scriptsTemplatesPath + "MVC_Service_Excel.txt";
                break;
        }

        Dictionary<string, string> dicReplace = ReplaceRole(fileName);
        EditorUtil.CreateClass(dicReplace, beanPath, fileName + "Bean", mvcBeanPath);
        EditorUtil.CreateClass(dicReplace, servicePath, fileName + "Service", mvcServicePath);

        AssetDatabase.Refresh();
        ShowNotification(new GUIContent($"已创建 {fileName}Bean 和 {fileName}Service"), 3f);
    }

    /// <summary>
    /// 替换规则
    /// </summary>
    protected Dictionary<string, string> ReplaceRole(string fileName)
    {
        Dictionary<string, string> dicReplaceData = new Dictionary<string, string>();
        dicReplaceData.Add("#ScriptName#", fileName);
        dicReplaceData.Add("#Author#", "AppleCoffee");
        dicReplaceData.Add("#CreateTime#", DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"));
        return dicReplaceData;
    }
}
