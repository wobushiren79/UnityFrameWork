using DG.DOTweenEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(BaseUICreateWindow))]
public class BaseUICreateWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/UI脚本创建")]
    static void CreateWindows()
    {
        // Get existing open window or if none, make a new one:
        BaseUICreateWindow window = (BaseUICreateWindow)EditorWindow.GetWindow(typeof(BaseUICreateWindow));
        window.Show();
    }

    protected readonly static string scrpitsTemplatesPath_UI = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUI.txt";
    protected readonly static string scrpitsTemplatesPath_UIView = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUIView.txt";
    protected readonly static string scrpitsTemplatesPath_UIDialog = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUIDialog.txt";
    protected readonly static string scrpitsTemplatesPath_UIPopup = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUIPopup.txt";
    protected readonly static string scrpitsTemplatesPath_UIToast = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUIToast.txt";

    public static string pathCreateBase = "Assets/Scrpits/Component/UI";
    public static string pathCreateGame = "Assets/Scrpits/Component/UI/Game";
    public static string modelName = "";

    public static string keyEditorPrefs = "InspectorBaseUICreate";

    [InitializeOnLoadMethod]
    public static void Init()
    {
        ToolbarExtension.ToolbarZoneLeftAlign += OnToolbarGUI;
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }

    static void OnToolbarGUI(VisualElement rootVisualElement)
    {
        var refresh = new EditorToolbarDropdown();
        refresh.text = "UI脚本创建";
        refresh.clicked += () =>
        {
            CreateWindows();
        };

        rootVisualElement.Add(refresh);
    }

    private static void OnAfterAssemblyReload()
    {
        bool isAutoAdd = EditorPrefs.GetBool(keyEditorPrefs, false);
        if (isAutoAdd)
        {
            GameObject objSelect = Selection.activeGameObject;
            string createfileName = GetCreateScriptFileName(objSelect);
            AddComponentByFileName(objSelect, createfileName);
            EditorPrefs.SetBool(keyEditorPrefs, false);
        }
    }

    public void OnGUI()
    {
        if (!EditorUtil.CheckIsPrefabMode())
        {
            return;
        }
        if (EditorUI.GUIButton("刷新", 200))
        {
            HandleForRefresh();
        }

        GUILayout.BeginHorizontal();
        EditorUI.GUIText("模块名字");
        modelName = EditorUI.GUIEditorText(modelName, 500);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorUI.GUIText("生成路径");
        pathCreateGame = EditorUI.GUIEditorText(pathCreateGame, 500);
        GUILayout.EndHorizontal();

        if (EditorUI.GUIButton("生成UI脚本", 200))
        {
            HandleForCreate(1);
        }
        if (EditorUI.GUIButton("生成View脚本", 200))
        {
            HandleForCreate(2);
        }
        if (EditorUI.GUIButton("生成Dialog脚本", 200))
        {
            HandleForCreate(3);
        }
        if (EditorUI.GUIButton("生成Popup脚本", 200))
        {
            HandleForCreate(4);
        }
        if (EditorUI.GUIButton("生成Toast脚本", 200))
        {
            HandleForCreate(5);
        }
        if (EditorUI.GUIButton("生成Common脚本", 200))
        {
            HandleForCreate(6);
        }
    }

    public string GetOriginTargetName(string targetName)
    {
        if (targetName.StartsWith("UI"))
        {
            targetName = targetName.Remove(0, 2);
        }
        if (targetName.StartsWith("View"))
        {
            targetName = targetName.Remove(0, 4);
        }
        if (targetName.StartsWith("Dialog"))
        {
            targetName = targetName.Remove(0, 6);
        }
        if (targetName.StartsWith("Popup"))
        {
            targetName = targetName.Remove(0, 5);
        }
        if (targetName.StartsWith("Toast"))
        {
            targetName = targetName.Remove(0, 5);
        }
        if (targetName.EndsWith("Item"))
        {
            targetName = targetName.Remove(targetName.Length - 4, 4);
        }
        return targetName;
    }

    public void HandleForRefresh()
    {
        GameObject objSelect = Selection.activeGameObject;
        modelName = GetOriginTargetName(objSelect.name);
        var directoryInfos = FileUtil.GetDirectoriesByPath(pathCreateGame);
        foreach (var itemDirectory in directoryInfos)
        {
            //LogUtil.Log($"HandleForRefresh itemDirectory_{itemDirectory.Name}");
            if (modelName.Contains(itemDirectory.Name))
            {
                modelName = itemDirectory.Name;
                return;
            }
        }
    }

    public void HandleForCreate(int typeCreate)
    {
        string scrpitsTemplatesPath =  "";

        GameObject objSelect = Selection.activeGameObject;
        string createfileName = GetCreateScriptFileName(objSelect);
        switch (typeCreate)
        {
            case 1:
                scrpitsTemplatesPath = scrpitsTemplatesPath_UI;
                break;
            case 2:
            case 6:
                scrpitsTemplatesPath = scrpitsTemplatesPath_UIView;
                break;
            case 3:
                scrpitsTemplatesPath = scrpitsTemplatesPath_UIDialog;
                break;
            case 4:
                scrpitsTemplatesPath = scrpitsTemplatesPath_UIPopup;
                break;
            case 5:
                scrpitsTemplatesPath = scrpitsTemplatesPath_UIToast;
                break;
        }
        string templatesPath = Application.dataPath + scrpitsTemplatesPath;

        if (!EditorUtil.CheckIsPrefabMode(out var prefabStage))
        {
            LogUtil.Log("没有进入编辑模式");
            return;
        }
        string[] path = EditorUtil.GetScriptPath(createfileName);
        if (path.Length > 0)
        {
            LogUtil.LogError("创建失败 已经创建" + createfileName + "的类");
            AddComponentByFileName(objSelect, createfileName);
            return;
        }
        
        ////规则替换
        Dictionary<string, string> dicReplace = InspectorBaseUIComponent.ReplaceRole(createfileName);
        ////创建文件
        string pathCreateFinal;
        if (modelName.IsNull())
        {
            LogUtil.LogError("还未输入模块名字");
            return;
        }
        //Dialog
        else if (typeCreate == 3)
        {
            pathCreateFinal = $"{pathCreateBase}/Dialog";
        }
        //Popup
        else if (typeCreate == 4)
        {
            pathCreateFinal = $"{pathCreateBase}/Popup";
        }
        //Toast
        else if (typeCreate == 5)
        {
            pathCreateFinal = $"{pathCreateBase}/Toast";
        }
        //Toast
        else if (typeCreate == 6)
        {
            pathCreateFinal = $"{pathCreateBase}/Common";
        }
        else
        {
            pathCreateFinal = $"{pathCreateGame}/{modelName}";
        }


        EditorUtil.CreateClass(dicReplace, templatesPath, createfileName, pathCreateFinal);
        //使用EditorPrefs保存数据 因为脚本重新编译之后参数会还原
        EditorPrefs.SetBool(keyEditorPrefs, true);

        Undo.RecordObject(objSelect, objSelect.gameObject.name);
        EditorUtility.SetDirty(objSelect);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 添加component
    /// </summary>
    /// <param name="objSelect"></param>
    /// <param name="createfileName"></param>
    public static void AddComponentByFileName(GameObject objSelect,string createfileName)
    {
        System.Type componentType = System.Type.GetType($"{createfileName}, Assembly-CSharp");
        if (componentType != null && objSelect.GetComponent(componentType) == null)
        {
            objSelect.AddComponent(componentType);
        }
    }

    /// <summary>
    /// 获取创建脚本名字
    /// </summary>
    /// <param name="objSelect"></param>
    /// <returns></returns>
    public static string GetCreateScriptFileName(GameObject objSelect)
    {
        string fileName = objSelect.name;
        if (!fileName.Substring(0, 2).Equals("UI"))
        {
            fileName = fileName.Insert(0, "UI");
        }
        return fileName;
    }
}