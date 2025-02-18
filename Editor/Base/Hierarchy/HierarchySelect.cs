using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using System;

[InitializeOnLoad]
public class HierarchySelect
{
    static HierarchySelect()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyShowSelect;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;

    }

    //选择列表
    public static Dictionary<string, List<Component>> dicSelectObj = new Dictionary<string, List<Component>>();
    public static BaseUIComponent baseUIComponent = null;
    public static BaseUIView baseUIView = null;
    /// <summary>
    /// 视窗改变
    /// </summary>
    public static void OnHierarchyChanged()
    {
        if (!EditorUtil.CheckIsPrefabMode(out var prefabStage))
        {
            return;
        }
        dicSelectObj.Clear();
        baseUIComponent = null;
        baseUIView = null;

        GameObject root = prefabStage.prefabContentsRoot;
        baseUIComponent = root.GetComponent<BaseUIComponent>();
        baseUIView = root.GetComponent<BaseUIView>();

        if (baseUIComponent == null && baseUIView == null) return;
        //设置初始化数据
        Dictionary<string, Type> dicData = null;
        if (baseUIComponent != null)
            dicData = ReflexUtil.GetAllNameAndType(baseUIComponent);
        if (baseUIView != null)
            dicData = ReflexUtil.GetAllNameAndType(baseUIView);
        foreach (var itemData in dicData)
        {
            string itemKey = itemData.Key;
            Type itemValue = itemData.Value;
            if (itemKey.Contains("ui_"))
            {
                string componentName = itemKey.Replace("ui_", "").Replace($"_{itemValue.Name}","");
                if (itemValue != null)
                {
                    Component[] listRootComponent = root.GetComponentsInChildren(itemValue,true);
                    foreach (Component itemRootComponent in listRootComponent)
                    {
                        if (itemRootComponent.name.Equals(componentName))
                        {
                            if (!dicSelectObj.ContainsKey(componentName))
                            {
                                dicSelectObj.Add(componentName, new List<Component>() { itemRootComponent });
                            }
                            else
                            {
                                dicSelectObj[componentName].Add(itemRootComponent);
                            }
                        }
                    }
                }
            }
        }
        return;
    }

    /// <summary>
    /// 视窗元素
    /// </summary>
    /// <param name="instanceid"></param>
    /// <param name="selectionrect"></param>
    public static void OnHierarchyShowSelect(int instanceid, Rect selectionrect)
    {
        //如果不是编辑模式则不进行操作
        if (!EditorUtil.CheckIsPrefabMode(out var prefabStage))
        {
            return;
        }
        //如果不是UI也不进行操作
        if (baseUIComponent == null && baseUIView == null)
        {
            return;
        }

        //获取当前obj
        var go = EditorUtility.InstanceIDToObject(instanceid) as GameObject;
        if (go == null)
            return;
        if (baseUIComponent == null)
        {
            baseUIComponent = go.GetComponent<BaseUIComponent>();
        }
        if (baseUIView == null)
        {
            baseUIView = go.GetComponent<BaseUIView>();
        }
        //控制开关
        var selectBox = new Rect(selectionrect);
        selectBox.x = selectBox.xMax - 30;
        selectBox.width = 10;
        //检测是否选中
        bool hasGo = false;
        List<Component> selectComponentList = null;
        if (dicSelectObj.TryGetValue(go.name, out selectComponentList))
        {
            hasGo = true;
        }
        hasGo = GUI.Toggle(selectBox, hasGo, string.Empty);
        //容错处理
        if (hasGo)
        {
            if (!dicSelectObj.ContainsKey(go.name))
            {
                selectComponentList = new List<Component>();
                dicSelectObj.Add(go.name, selectComponentList);
            }
        }
        else
        {
            if (dicSelectObj.ContainsKey(go.name))
            {
                dicSelectObj.Remove(go.name);
            }
        }
        //如果选中了
        if (hasGo)
        {
            //下拉选择
            var selectType = new Rect(selectionrect);
            selectType.x = selectBox.xMax - 160;
            selectType.width = 150;
            //获取该obj下所拥有的所有comnponent
            Component[] componentList = go.GetComponents<Component>();
            //所有选择的控件下表
            HashSet<int> selectComponentIndex = new HashSet<int>();
            //初始化所有可选component;
            for (int i = 0; i < componentList.Length; i++)
            {
                var itemComponentName = componentList[i].GetType().Name;
                if (selectComponentList != null && selectComponentList.Count > 0)
                {
                    for (int f = 0; f < selectComponentList.Count; f++)
                    {
                        if (selectComponentList[f].GetType().Name.Equals(itemComponentName))
                        {
                            if(!selectComponentIndex.Contains(i))
                                selectComponentIndex.Add(i);
                            break;
                        }
                    }
                }
            }
            //默认选择
            if (selectComponentIndex == null || selectComponentIndex.Count == 0)
            {
                //如果有设置控件
                if (componentList.Length >= 1)
                {
                    selectComponentIndex.Add(componentList.Length - 1);
                    if (dicSelectObj.ContainsKey(go.name))
                    {
                        dicSelectObj[go.name].Clear();
                        dicSelectObj[go.name].Add(componentList[componentList.Length - 1]);
                    }
                    else
                    {
                        dicSelectObj.Add(go.name, new List<Component>() { componentList[componentList.Length - 1] });
                    }
                }
            }
            string buttonText = "";
            foreach (var item in selectComponentIndex)
            {
                var itemComponent = componentList[item];
                buttonText += $"{itemComponent.GetType().Name} ";
            }

            //自定义弹窗
            if (GUI.Button(selectType, buttonText))
            {
                Rect popupRect = GUILayoutUtility.GetLastRect();
                popupRect.x = selectType.x;
                popupRect.y = selectType.y + selectType.height;
                PopupWindow.Show(popupRect, new HierarchySelectPopupSelect((selectComonentIndexCB) =>
                {
                    dicSelectObj[go.name].Clear();
                    //没有选择任意一个
                    if (selectComonentIndexCB == null || selectComonentIndexCB.Count == 0)
                    {
                     
                    }
                    else
                    {
                        foreach (var item in  selectComonentIndexCB)
                        {
                            var itemComponent = componentList[item];
                            dicSelectObj[go.name].Add(itemComponent);
                        }
                    }
                    //UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                },
                componentList, selectComponentIndex));
            }
        }
    }
}