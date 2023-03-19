using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MVCEditorWindow : EditorWindow
{
    public string mvcClassName = "";

    protected readonly string scrpitsTemplatesPath = "/Editor/ScrpitsTemplates/";

    protected int saveType = 1;
    protected string mvcBeanPath = "Assets/Scrpits/Bean/MVC";
    protected string mvcViewPath = "Assets/Scrpits/MVC/View";
    protected string mvcModelPath = "Assets/Scrpits/MVC/Model";
    protected string mvcControllerPath = "Assets/Scrpits/MVC/Controller";
    protected string mvcServicePath = "Assets/Scrpits/MVC/Service";

    [MenuItem("Custom/工具弹窗/创建MVC")]
    static void CreateWindows()
    {
        EditorWindow.GetWindow(typeof(MVCEditorWindow));
    }


    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorUI.GUIText("创建MVC,输入MVC名称:", 200);
        mvcClassName = EditorUI.GUIEditorText(mvcClassName, 100);
        EditorUI.GUIText("保存类型（1.SQLite 2.FileJson 3.Excel）:", 300);
        saveType = EditorUI.GUIEditorText(saveType, 50);
        EditorGUILayout.EndHorizontal();

        EditorUI.GUIText("Bean路径:");
        mvcBeanPath = EditorUI.GUIEditorText(mvcBeanPath, 500);
        EditorUI.GUIText("View路径:");
        mvcViewPath = EditorUI.GUIEditorText(mvcViewPath, 500);
        EditorUI.GUIText("Model路径:");
        mvcModelPath = EditorUI.GUIEditorText(mvcModelPath, 500);
        EditorUI.GUIText("Controller路径:");
        mvcControllerPath = EditorUI.GUIEditorText(mvcControllerPath, 500);
        EditorUI.GUIText("Service路径:");
        mvcServicePath = EditorUI.GUIEditorText(mvcServicePath, 500);

        if (EditorUI.GUIButton("创建"))
        {
            CreateMVCClass(mvcClassName, saveType);
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 创建MVC
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="saveType">1.sqlite 2.filejson</param>
    public void CreateMVCClass(string fileName, int saveType)
    {
        //注意，Application.datapath会根据使用平台不同而不同  
        string beanPath = Application.dataPath + scrpitsTemplatesPath + "MVC_Bean.txt";
        string viewPath = Application.dataPath + scrpitsTemplatesPath + "MVC_IView.txt";
        string modelPath = Application.dataPath + scrpitsTemplatesPath + "MVC_Model.txt";
        string controllerPath = Application.dataPath + scrpitsTemplatesPath + "MVC_Controller.txt";
        string servicePath = "";

        switch (saveType)
        {
            case 1:
                servicePath = Application.dataPath + scrpitsTemplatesPath + "MVC_Service_SQLite.txt";
                break;
            case 2:
                servicePath = Application.dataPath + scrpitsTemplatesPath + "MVC_Service_FileJson.txt";
                break;
            case 3:
                servicePath = Application.dataPath + scrpitsTemplatesPath + "MVC_Service_Excel.txt";
                break;
        }

        //创建.CS文件
        if (saveType != 3)
        {
            Dictionary<string, string> dicReplace= ReplaceRole(fileName);
            EditorUtil.CreateClass(dicReplace, beanPath, fileName + "Bean", mvcBeanPath);
        }
        Dictionary<string, string> dicReplaceView = ReplaceRole(fileName);
        EditorUtil.CreateClass(dicReplaceView, viewPath, "I" + fileName + "View", mvcViewPath);

        Dictionary<string, string> dicReplaceModel = ReplaceRole(fileName);
        EditorUtil.CreateClass(dicReplaceModel, modelPath, fileName + "Model", mvcModelPath);

        Dictionary<string, string> dicReplaceController = ReplaceRole(fileName);
        EditorUtil.CreateClass(dicReplaceController, controllerPath, fileName + "Controller", mvcControllerPath);

        Dictionary<string, string> dicReplaceService = ReplaceRole(fileName);
        EditorUtil.CreateClass(dicReplaceService, servicePath, fileName + "Service", mvcServicePath);
        //刷新资源
        AssetDatabase.Refresh();
    }


    /// <summary>
    /// 替换规则
    /// </summary>
    /// <param name="scripteContent"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    protected Dictionary<string,string> ReplaceRole(string fileName)
    {        
        //这里实现自定义的一些规则  
        Dictionary<string, string> dicReplaceData = new Dictionary<string, string>();
        dicReplaceData.Add("#ScriptName#", fileName);
        dicReplaceData.Add("#Author#", "AppleCoffee");
        dicReplaceData.Add("#CreateTime#", System.DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"));
        return dicReplaceData;
    }
}
