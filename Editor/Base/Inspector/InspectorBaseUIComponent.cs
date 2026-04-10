using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Text;
using System;
using Unity.VisualScripting;

[InitializeOnLoad]
[CustomEditor(typeof(BaseUIComponent), true)]
public class InspectorBaseUIComponent : Editor
{
    protected readonly static string scriptsTemplatesPath = "/FrameWork/Editor/ScriptsTemplates/UI_BaseUIComponent.txt";
    protected readonly static string classSuffix = "Component";
    protected readonly static string keyEditorPrefs = "InspectorBaseUIComponent";
    
    // UI样式相关变量
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private GUIStyle infoBoxStyle;
    private GUIStyle sectionStyle;
    private GUIStyle warningStyle;
    
    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 10),
                padding = new RectOffset(5, 5, 5, 5),
                normal = { textColor = new Color(0.2f, 0.4f, 0.8f) }
            };
            
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30,
                margin = new RectOffset(5, 5, 5, 5),
                padding = new RectOffset(10, 10, 5, 5)
            };
            
            infoBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(5, 5, 10, 10),
                padding = new RectOffset(10, 10, 10, 10),
                fontSize = 11
            };
            
            sectionStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(0, 0, 10, 10),
                padding = new RectOffset(5, 5, 10, 10)
            };
            
            warningStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.8f, 0.4f, 0.2f) }
            };
        }
    }
    
    [InitializeOnLoadMethod]
    public static void Init()
    {
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }
    
    private static void OnAfterAssemblyReload()
    {
        bool isAutoAdd = EditorPrefs.GetBool(keyEditorPrefs, false);
        if (isAutoAdd)
        {
            HierarchySelect.OnHierarchyChanged();
            HandleForSetUICompontData();
            EditorPrefs.SetBool(keyEditorPrefs, false);
        }
    }

    public override void OnInspectorGUI()
    {
        InitializeStyles();
        
        // 绘制默认的Inspector
        base.OnInspectorGUI();
        
        // 检查是否是预制体模式
        if (!EditorUtil.CheckIsPrefabMode())
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("⚠️ 请进入预制体编辑模式以使用UI组件功能", warningStyle);
            return;
        }
        
        // 添加分隔线
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);
        
        // UI组件工具区域
        using (new GUILayout.VerticalScope(sectionStyle))
        {
            // 标题
            EditorGUILayout.LabelField("UI组件工具", headerStyle);
            EditorGUILayout.Space(5);
            
            // 说明信息
            using (new GUILayout.VerticalScope(infoBoxStyle))
            {
                EditorGUILayout.LabelField("功能说明：", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• 生成UI组件脚本：创建与当前UI相关的C#脚本");
                EditorGUILayout.LabelField("• 设置UI组件数据：自动绑定Hierarchy选中的控件到脚本变量");
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("注意：操作前请确保已在Hierarchy视图中选中需要绑定的UI控件", warningStyle);
            }
            
            EditorGUILayout.Space(15);
            
            // 按钮区域
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                
                // 生成脚本按钮
                GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
                if (GUILayout.Button("🔄 生成UI组件脚本", buttonStyle, GUILayout.Width(180), GUILayout.Height(35)))
                {
                    BaseUIInit component = (BaseUIInit)target;
                    HandleForCreateUIComponent(component);
                    EditorPrefs.SetBool(keyEditorPrefs, true);
                }
                GUI.backgroundColor = Color.white;
                
                GUILayout.Space(15);
                
                // 设置数据按钮
                GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                if (GUILayout.Button("⚙️  设置UI组件数据", buttonStyle, GUILayout.Width(180), GUILayout.Height(35)))
                {
                    BaseUIInit component = (BaseUIInit)target;
                    HandleForSetUICompontData(component);
                }
                GUI.backgroundColor = Color.white;
                
                GUILayout.FlexibleSpace();
            }
            
            // 状态提示
            EditorGUILayout.Space(10);
            bool hasPendingChanges = EditorPrefs.GetBool(keyEditorPrefs, false);
            if (hasPendingChanges)
            {
                EditorGUILayout.HelpBox("脚本生成完成！将在重新编译后自动设置数据。", MessageType.Info);
            }
        }
        
        // 底部空间
        EditorGUILayout.Space(20);
    }

    ////Hierarchy视图
    //[MenuItem("GameObject/创建/UIComponent", false, 10)]
    ////Projects视图
    //[MenuItem("Assets/创建/UIComponent", false, 10)]
    public void HandleForCreateUIComponent(BaseUIInit uiComponent)
    {
        GameObject objSelect = Selection.activeGameObject;
        string createfileName = GetCreateScriptFileName(objSelect);
        string currentFileName = GetCurrentScriptFileName(objSelect);
        string templatesPath = Application.dataPath + scriptsTemplatesPath;

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
    public static void HandleForSetUICompontData(BaseUIInit uiComponent = null)
    {
        GameObject objSelect = null;
        
        // 优先从 HierarchySelect 获取（用于代码生成后的自动绑定）
        if (uiComponent == null)
        {
            uiComponent = HierarchySelect.baseUIComponent;
            if (uiComponent != null)
            {
                objSelect = uiComponent.gameObject;
            }
        }
        
        // 如果 HierarchySelect 没有，尝试从 Selection 获取（用于手动点击设置）
        if (uiComponent == null)
        {
            objSelect = Selection.activeGameObject;
            if (objSelect == null)
                return;
            uiComponent = objSelect.GetComponent<BaseUIComponent>();
        }
        if (uiComponent == null)
            return;
        if (objSelect == null)
        {
            objSelect = uiComponent.gameObject;
        }
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
    public static Dictionary<string, string> ReplaceRole(string className)
    {
        //这里实现自定义的一些规则  
        Dictionary<string, string> dicReplaceData = new Dictionary<string, string>();

        Dictionary<string, List<Component>> dicSelect = HierarchySelect.dicSelectObj;
        StringBuilder content = new StringBuilder();
        //获取基类
        GameObject objSelect = Selection.activeGameObject;
        Dictionary<string, Type> dicBaseTypes = new Dictionary<string, Type>();

        BaseMonoBehaviour uiComponent = objSelect.GetComponent<BaseMonoBehaviour>();
        if (uiComponent != null)
        {
            dicBaseTypes = ReflexUtil.GetAllNameAndTypeFromBase(uiComponent);
        }

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