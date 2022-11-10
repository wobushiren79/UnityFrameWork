

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class EditorUI
{

    /// <summary>
    /// 弹窗
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <param name="yesStr"></param>
    /// <param name="noStr"></param>
    /// <returns></returns>
    public static bool GUIDialog(string title, string content, string yesStr = "是", string noStr = "否")
    {
        return EditorUtility.DisplayDialog(title, content, yesStr, noStr);
    }

    /// <summary>
    /// 展示进度条
    /// </summary>
    /// <param name="title"></param>
    /// <param name="info"></param>
    /// <param name="progress"></param>
    public static void GUIShowProgressBar(string title, string info, float progress)
    {
         EditorUtility.DisplayProgressBar(title, info, progress);
    }
    
    /// <summary>
    /// 关闭进度条
    /// </summary>
    public static void GUIHideProgressBar()
    {
        EditorUtility.ClearProgressBar();
    }


    /// <summary>
    /// 打开文件夹
    /// </summary>
    /// <param name="path"></param>
    public static void OpenFolder(string path)
    {
        Process.Start(path);
    }

    /// <summary>
    /// 选择文件夹
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    public static string GetFolderPanel(string title, string folder = "", string defName = "")
    {
        return EditorUtility.OpenFolderPanel(title, folder, defName);
    }

    /// <summary>
    /// 按钮
    /// </summary>
    /// <param name="name"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static bool GUIButton(string name, int width, int height)
    {
        return GUILayout.Button(name, GUILayout.Width(width), GUILayout.Height(height));
    }
    public static bool GUIButton(string name, int width)
    {
        return GUIButton(name, width, 20);
    }
    public static bool GUIButton(string name)
    {
        return GUIButton(name, 100, 20);
    }

    /// <summary>
    /// 输入文本
    /// </summary>
    /// <param name="text"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static string GUIEditorText(string text, int width, int height)
    {
        return EditorGUILayout.TextArea(text, GUILayout.Width(width), GUILayout.Height(height));
    }
    public static string GUIEditorText(string text, int width)
    {
        return GUIEditorText(text, width, 20);
    }
    public static string GUIEditorText(string text)
    {
        return GUIEditorText(text, 100, 20);
    }
    public static long GUIEditorText(long text, int width, int height)
    {
        return long.Parse(GUIEditorText(text + "", width, height));
    }
    public static long GUIEditorText(long text, int width)
    {
        return GUIEditorText(text, width, 20);
    }
    public static long GUIEditorText(long text)
    {
        return GUIEditorText(text, 100, 20);
    }
    public static float GUIEditorText(float text, int width, int height)
    {
        return float.Parse(GUIEditorText(text + "", width, height));
    }
    public static float GUIEditorText(float text, int width)
    {
        return GUIEditorText(text, width, 20);
    }
    public static float GUIEditorText(float text)
    {
        return GUIEditorText(text, 100, 20);
    }
    public static int GUIEditorText(int text, int width, int height)
    {
        return int.Parse(GUIEditorText(text + "", width, height));
    }
    public static int GUIEditorText(int text, int width)
    {
        return GUIEditorText(text, width, 20);
    }
    public static int GUIEditorText(int text)
    {
        return GUIEditorText(text, 100, 20);
    }

    /// <summary>
    /// 文本
    /// </summary>
    /// <param name="text"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public static void GUIText(string text, int width, int height)
    {
        GUILayout.Label(text, GUILayout.Width(width), GUILayout.Height(height));
    }
    public static void GUIText(string text, int width)
    {
        GUIText(text, width, 20);
    }
    public static void GUIText(string text)
    {
        GUIText(text, 100, 20);
    }

    /// <summary>
    /// 枚举
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="title"></param>
    /// <param name="type"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static T GUIEnum<T>(string title, int type, int width, int height) where T : Enum
    {
        return (T)EditorGUILayout.EnumPopup(title, type.GetEnum<T>(), GUILayout.Width(width), GUILayout.Height(height));
    }
    public static T GUIEnum<T>(string title, int type, int width) where T : Enum
    {
        return (T)EditorGUILayout.EnumPopup(title, type.GetEnum<T>(), GUILayout.Width(width), GUILayout.Height(20));
    }
    public static T GUIEnum<T>(string title, int type) where T : Enum
    {
        return (T)EditorGUILayout.EnumPopup(title, type.GetEnum<T>(), GUILayout.Width(300), GUILayout.Height(20));
    }

    /// <summary>
    /// 选择Object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static T GUIObj<T>(string name, T data) where T : UnityEngine.Object
    {
        return EditorGUILayout.ObjectField(new GUIContent(name, ""), data, typeof(T), true) as T;
    }

    /// <summary>
    /// 展示图片
    /// </summary>
    /// <param name="picPath"></param>
    public static void GUIPic(string picPath)
    {
        GUIPic(picPath, 64, 64);
    }
    public static void GUIPic(string picPath, int width, int height)
    {
        Texture2D iconTex = EditorGUIUtility.FindTexture(picPath);
        if (iconTex)
            GUILayout.Label(iconTex, GUILayout.Width(width), GUILayout.Height(height));
    }

    /// <summary>
    /// 图片选择
    /// </summary>
    /// <param name="spIcon"></param>
    /// <param name="iconName"></param>
    public static void GUIPicSelect(string iconName, Sprite spIcon)
    {
        spIcon = EditorGUILayout.ObjectField(new GUIContent(iconName, ""), spIcon, typeof(Sprite), true) as Sprite;
    }


    /// <summary>
    /// 数据列表
    /// </summary>
    /// <param name="storeInfo"></param>
    public static string GUIListData<E>(string titleName, string content) where E : System.Enum
    {
        //前置相关
        EditorGUILayout.BeginVertical();
        GUILayout.Label(titleName + "：", GUILayout.Width(100), GUILayout.Height(20));
        if (GUILayout.Button("添加", GUILayout.Width(100), GUILayout.Height(20)))
        {
            content += ("|" + EnumExtension.GetEnumValueByPosition<E>(0).GetEnumName() + ":" + "1|");
        }
        List<string> listConditionData = content.SplitForListStr('|');
        content = "";
        for (int i = 0; i < listConditionData.Count; i++)
        {
            string itemConditionData = listConditionData[i];
            if (itemConditionData.IsNull())
            {
                continue;
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("删除", GUILayout.Width(100), GUILayout.Height(20)))
            {
                listConditionData.RemoveAt(i);
                i--;
                continue;
            }
            List<string> listItemConditionData = itemConditionData.SplitForListStr(':');
            listItemConditionData[0] = EditorGUILayout.EnumPopup(listItemConditionData[0].GetEnum<E>(), GUILayout.Width(300), GUILayout.Height(20)).GetEnumName();
            listItemConditionData[1] = EditorGUILayout.TextArea(listItemConditionData[1] + "", GUILayout.Width(100), GUILayout.Height(20));
            EditorGUILayout.EndHorizontal();
            content += (listItemConditionData[0] + ":" + listItemConditionData[1]) + "|";
        }
        EditorGUILayout.EndVertical();
        return content;
    }
}