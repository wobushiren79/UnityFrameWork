using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIEditorWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/更换字体")]
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

        if (GUILayout.Button("更换选中的预制体"))
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
        if (GUILayout.Button("更换文件夹下所有的预制体"))
        {
            if (SelectOldFont == null || SelectNewFont == null)
            {
                Debug.LogError("请选择字体！");
            }
            else
            {
                ChangeSelectFloudForText();
            }
        }
    }

    public static void ChangeForText()
    {
        Object[] Texts = Selection.GetFiltered(typeof(Text), SelectionMode.Deep);
        Debug.Log("找到" + Texts.Length + "个Text，即将处理");
        int count = 0;
        foreach (Object text in Texts)
        {
            if (text)
            {
                Text AimText = (Text)text;
                Undo.RecordObject(AimText, AimText.gameObject.name);
                if (AimText.font == OldFont)
                {
                    AimText.font = NewFont;
                    //Debug.Log(AimText.name + ":" + AimText.text);
                    EditorUtility.SetDirty(AimText);
                    count++;
                }
            }
        }
        Debug.Log("字体更换完毕！更换了" + count + "个");
    }

    public static void ChangeSelectFloudForText()
    {

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
            int count = 0;
            foreach (Text text in Texts)
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
                AssetDatabase.SaveAssets();
                Debug.Log(go.name + "界面有:" + count + "个Arial字体");
            }

        }
    }
}
