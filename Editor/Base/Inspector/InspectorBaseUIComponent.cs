using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Text;
using System;

[InitializeOnLoad]
[CustomEditor(typeof(BaseUIComponent), true)]
public class InspectorBaseUIComponent : Editor
{
    protected readonly static string scrpitsTemplatesPath = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUIComponent.txt";
    protected readonly static string classSuffix = "Component";
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (!EditorUtil.CheckIsPrefabMode())
        {
            return;
        }
        GUILayout.Space(50);
        GUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("生成UICompont脚本", 200))
        {
            HandleForCreateUIComponent();
        }
        if (EditorUI.GUIButton("设置UICompont数据", 200))
        {
            HandleForSetUICompontData();
        }
        GUILayout.EndHorizontal();
    }

    ////Hierarchy视图
    //[MenuItem("GameObject/创建/UIComponent", false, 10)]
    ////Projects视图
    //[MenuItem("Assets/创建/UIComponent", false, 10)]
    public void HandleForCreateUIComponent()
    {
        GameObject objSelect = Selection.activeGameObject;
        string createfileName = GetCreateScriptFileName(objSelect);
        string currentFileName = GetCurrentScriptFileName(objSelect);
        string templatesPath = Application.dataPath + scrpitsTemplatesPath;

        if (!EditorUtil.CheckIsPrefabMode(out var prefabStage))
        {
            LogUtil.Log("没有进入编辑模式");
            return;
        }
        string[] path = EditorUtil.GetScriptPath(currentFileName);
        //string path = prefabStage.assetPath;
        //获取最后一个/的索引
        if (path.Length == 0)
        {
            LogUtil.Log("没有名字为" + currentFileName + "的类,请先创建");
            return;
        }
        //规则替换
        Dictionary<string, string> dicReplace = ReplaceRole(currentFileName);
        //创建文件
        EditorUtil.CreateClass(dicReplace, templatesPath, createfileName, path[0]);
        Undo.RecordObject(objSelect, objSelect.gameObject.name);
        EditorUtility.SetDirty(objSelect);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 处理 设置UI的值
    /// </summary>
    public void HandleForSetUICompontData()
    {
        GameObject objSelect = Selection.activeGameObject;
        if (objSelect == null)
            return;
        BaseMonoBehaviour uiComponent = objSelect.GetComponent<BaseMonoBehaviour>();
        Dictionary<string, Type> dicData = ReflexUtil.GetAllNameAndType(uiComponent);
        foreach (var itemData in dicData)
        {
            string itemKey = itemData.Key;
            Type itemValue = itemData.Value;
            if (itemKey.Contains("ui_"))
            {
                //获取选中的控件
                Dictionary<string, List<Component>> dicSelect = HierarchySelect.dicSelectObj;
                //对比选中的控件和属性名字是否一样
                if (dicSelect.TryGetValue(itemKey.Replace("ui_", "").Replace($"_{itemValue.Name}",""), out List<Component> listSelectComponent))
                {
                    if (listSelectComponent != null && listSelectComponent.Count > 0)
                    {
                        foreach (var itemComponent in listSelectComponent)
                        {
                            if(itemComponent.GetType().Name == itemValue.Name)
                            {
                                ReflexUtil.SetValueByName(uiComponent, itemKey, itemComponent);
                            }
                        }
                    }
                }
            }
        }
        Undo.RecordObject(objSelect, objSelect.gameObject.name);
        EditorUtility.SetDirty(objSelect);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 替换规则
    /// </summary>
    /// <param name="scripteContent"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    protected static Dictionary<string, string> ReplaceRole(string className)
    {
        //这里实现自定义的一些规则  
        Dictionary<string, string> dicReplaceData = new Dictionary<string, string>();

        Dictionary<string, List<Component>> dicSelect = HierarchySelect.dicSelectObj;
        StringBuilder content = new StringBuilder();
        //获取基类
        GameObject objSelect = Selection.activeGameObject;
        BaseMonoBehaviour uiComponent = objSelect.GetComponent<BaseMonoBehaviour>();
        Dictionary<string, Type> dicBaseTypes = ReflexUtil.GetAllNameAndTypeFromBase(uiComponent);

        //添加类名---------------------------
        dicReplaceData.Add("#ClassName#", className);
        //------------------------------------

        //添加属性---------------------------
        foreach (var itemSelect in dicSelect)
        {
            List<Component> listSelectItem = itemSelect.Value;
            if (listSelectItem == null || listSelectItem.Count == 0)
                continue;
            foreach (var itemComponent in listSelectItem)
            {
                Type type = itemComponent.GetType();

                //如果基类里面已经有了这个属性，则不再添加
                string uiViewName = "";
                if (listSelectItem.Count == 1)
                {
                    //如果只有一个 不需要加类型后缀
                    uiViewName = $"ui_{itemSelect.Key}";
                }
                else
                {
                    uiViewName = $"ui_{itemSelect.Key}_{type.Name}";
                }
                if (dicBaseTypes.ContainsKey(uiViewName))
                    continue;
                content.Append($"    public {type.Name} {uiViewName};\r\n\r\n");
            }
        }
        dicReplaceData.Add("#PropertyList#", content.ToString());
        //------------------------------------

        //添加引用---------------------------
        string usingStr = "";
        List<string> listUsing = new List<string>();
        foreach (var itemSelect in dicSelect)
        {
            List<Component> listSelectItem = itemSelect.Value;
            if (listSelectItem == null || listSelectItem.Count == 0)
                continue;
            foreach (var itemComponent in listSelectItem)
            {
                Type type = itemComponent.GetType();
                if (type.Namespace.IsNull())
                    continue;
                if (!listUsing.Contains(type.Namespace))
                {
                    listUsing.Add(type.Namespace);
                }
            }
        }
        foreach (var itemUsing in listUsing)
        {
            usingStr += $"using {itemUsing};\r\n";
        }
        dicReplaceData.Add("#Using#", usingStr);
        //------------------------------------
        return dicReplaceData;
    }

    /// <summary>
    /// 获取创建脚本名字
    /// </summary>
    /// <param name="objSelect"></param>
    /// <returns></returns>
    public virtual string GetCreateScriptFileName(GameObject objSelect)
    {
        string fileName = objSelect.name + classSuffix;
        if (!fileName.Substring(0, 2).Equals("UI"))
        {
            fileName = fileName.Insert(0, "UI");
        }
        return fileName;
    }

    /// <summary>
    /// 获取当前脚本名字
    /// </summary>
    /// <param name="objSelect"></param>
    /// <returns></returns>
    public virtual string GetCurrentScriptFileName(GameObject objSelect)
    {
        string fileName = objSelect.name;
        if (!fileName.Substring(0, 2).Equals("UI"))
        {
            fileName = fileName.Insert(0, "UI");
        }
        return fileName;
    }
}