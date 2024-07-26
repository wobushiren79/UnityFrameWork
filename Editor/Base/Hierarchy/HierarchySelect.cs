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
    public static Dictionary<string, Component> dicSelectObj = new Dictionary<string, Component>();
    public static BaseUIComponent baseUIComponent = null;
    public static BaseUIView baseUIView = null;
    /// <summary>
    /// 视窗改变
    /// </summary>
    private static void OnHierarchyChanged()
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
                string componentName = itemKey.Replace("ui_", "");
                if (itemValue != null)
                {
                    Component[] listRootComponent = root.GetComponentsInChildren(itemValue,true);
                    foreach (Component itemRootComponent in listRootComponent)
                    {
                        if (itemRootComponent.name.Equals(componentName))
                        {
                            if (!dicSelectObj.ContainsKey(componentName))
                            {
                                dicSelectObj.Add(componentName, itemRootComponent);
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
    private static void OnHierarchyShowSelect(int instanceid, Rect selectionrect)
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
        Component selectComonent = null;
        if (dicSelectObj.TryGetValue(go.name, out selectComonent))
        {
            hasGo = true;
        }
        hasGo = GUI.Toggle(selectBox, hasGo, string.Empty);
        if (hasGo)
        {
            if (!dicSelectObj.ContainsKey(go.name))
            {
                dicSelectObj.Add(go.name, null);
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
            string[] listData = new string[componentList.Length];
            int selectComonentIndex = 0;
            //初始化所有可选component;
            for (int i = 0; i < componentList.Length; i++)
            {
                listData[i] = componentList[i].GetType().Name;
                if (selectComonent != null && selectComonent.GetType().Name.Equals(listData[i]))
                {
                    selectComonentIndex = i;
                }
            }
            //默认选择
            if (selectComonent == null)
            {
                //如果有设置控件
                if (listData.Length > 2)
                {
                    selectComonentIndex = componentList.Length - 1;
                }
                dicSelectObj[go.name] = componentList[selectComonentIndex];
            }
            //设置下拉数据 使用此方法需要连续点2次
            //int newSelectComonentIndex = EditorGUI.Popup(selectType,selectComonentIndex, listData);
            //int newSelectComonentIndex = GUI.Toolbar(selectType, selectComonentIndex, listData);
            //如果下拉数据改变
            //dicSelectObj[go.name] = componentList[selectComonentIndex];
            
            //自定义弹窗
            if (GUI.Button(selectType,listData[selectComonentIndex]))
            {
                Rect popupRect = GUILayoutUtility.GetLastRect();
                popupRect.x = selectType.x;
                popupRect.y = selectType.y + selectType.height;
                PopupWindow.Show(popupRect, new HierarchySelectPopupSelect((popupSelectIndex) => 
                {
                    dicSelectObj[go.name] = componentList[popupSelectIndex];
                },
                listData));
            }
        }
    }
}