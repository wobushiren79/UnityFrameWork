using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIEditorWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/字体处理")]
    public static void Open()
    {
        EditorWindow.GetWindow(typeof(UIEditorWindow));
    }

    public Font SelectOldFont;
    static Font OldFont;

    public Font SelectNewFont;
    static Font NewFont;

    private void OnGUI()
    {
        SelectOldFont = (Font)EditorGUILayout.ObjectField("请选择想更换的字体", SelectOldFont, typeof(Font), true, GUILayout.MinWidth(100));
        OldFont = SelectOldFont;
        SelectNewFont = (Font)EditorGUILayout.ObjectField("请选择新的字体", SelectNewFont, typeof(Font), true, GUILayout.MinWidth(100));
        NewFont = SelectNewFont;

        if (EditorUI.GUIButton("更换选中的预制体",300))
        {
            if (SelectOldFont == null || SelectNewFont == null)
            {
                Debug.LogError("请选择字体！");
            }
            else
            {
                ChangeForText();
            }
        }
        if (EditorUI.GUIButton("更换文件夹下所有的预制体", 300))
        {
            if (SelectOldFont == null || SelectNewFont == null)
            {
                Debug.LogError("请选择字体！");
            }
            else
            {
                ChangeSelectFolderForText();
            }
        }
        GUILayout.Space(5);
        if (EditorUI.GUIButton("修正选中Bestfit的默认字体大小", 300))
        {
            FixTextBestfit();
        }
        if (EditorUI.GUIButton("修正文件夹下所有Bestfit的默认字体大小", 300))
        {
            FixSelectFolderTextBestfit();
        }
    }

    /// <summary>
    /// 替换选中的文本字体
    /// </summary>
    public static void ChangeForText()
    {
        List<Text> listText = GetTextForSelect();
        Debug.Log("找到" + listText.Count + "个Text，即将处理");
        int count = 0;
        foreach (Text text in listText)
        {
            Undo.RecordObject(text, text.gameObject.name);
            if (text.font == OldFont)
            {
                text.font = NewFont;
                //Debug.Log(AimText.name + ":" + AimText.text);
                EditorUtility.SetDirty(text);
                count++;
            }
        }
        if (count > 0)
        {
            EditorUtil.RefreshAsset();
        }
        Debug.Log("字体更换完毕！更换了" + count + "个");
    }

    /// <summary>
    /// 替换文件夹下所有的字体
    /// </summary>
    public static void ChangeSelectFolderForText()
    {
        List<Text> listText = GetTextForSelectFolder();
        int count = 0;
        foreach (Text text in listText)
        {
            Undo.RecordObject(text, text.gameObject.name);
            if (text.font == OldFont)
            {
                text.font = NewFont;
                EditorUtility.SetDirty(text);
                count++;
            }
        }
        if (count > 0)
        {
            EditorUtil.RefreshAsset();
        }
    }

    /// <summary>
    /// 修正选中Bestfit的默认字体大小
    /// </summary>
    public static void FixTextBestfit()
    {
        List<Text> listText = GetTextForSelect();
        foreach (var itemText in listText)
        {
            Undo.RecordObject(itemText, itemText.gameObject.name);
            if (itemText.resizeTextForBestFit == true && itemText.fontSize != itemText.resizeTextMaxSize)
            {
                itemText.fontSize = itemText.resizeTextMaxSize;
                EditorUtility.SetDirty(itemText);
            }
        }
        EditorUtil.RefreshAsset();
    }
    public static void FixSelectFolderTextBestfit()
    {
        List<Text> listText = GetTextForSelectFolder();
        foreach (var itemText in listText)
        {
            Undo.RecordObject(itemText, itemText.gameObject.name);
            if (itemText.resizeTextForBestFit == true && itemText.fontSize != itemText.resizeTextMaxSize)
            {
                itemText.fontSize = itemText.resizeTextMaxSize;
                EditorUtility.SetDirty(itemText);
            }
        }
        EditorUtil.RefreshAsset();
    }

    public static List<Text> GetTextForSelect()
    {
        List<Text> listData = new List<Text>();
        Object[] Texts = Selection.GetFiltered(typeof(Text), SelectionMode.Deep);
        foreach (Object text in Texts)
        {
            if (text)
            {
                Text itemText = (Text)text;
                listData.Add(itemText);
            }
        }
        return listData;
    }

    public static List<Text> GetTextForSelectFolder()
    {
        List<Text> listData = new List<Text>();
        object[] objs = Selection.GetFiltered(typeof(object), SelectionMode.DeepAssets);
        for (int i = 0; i < objs.Length; i++)
        {
            string ext = System.IO.Path.GetExtension(objs[i].ToString());
            if (!ext.Contains(".GameObject"))
            {
                continue;
            }
            GameObject go = (GameObject)objs[i];
            var Texts = go.GetComponentsInChildren<Text>(true);
            foreach (Text text in Texts)
            {
                listData.Add(text);
            }
        }
        return listData;
    }
}
