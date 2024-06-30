using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static Codice.CM.WorkspaceServer.WorkspaceTreeDataStore;

public class SpineEditor : Editor
{

    [MenuItem("Custom/Spine/ÐÞ¸Ä°æºÅ")]
    public static void SpineChangeVersion()
    {
        Object obj = Selection.activeObject;
        if (obj == null)
            return;
        TextAsset textAsset = obj as TextAsset;
        if (textAsset == null)
            return;

        string path = EditorUtil.GetSelectionPathByObj(obj);
        string contentStr = textAsset.text;
        //LogUtil.Log($"contentStr:{contentStr}");
        string contentStrNew = contentStr.Replace("\"spine\": \"3.8.75\",", "\"spine\": \"3.8\"");
        //LogUtil.Log($"contentStrNew:{contentStrNew}");

        File.WriteAllText(path,contentStrNew);
        EditorUtil.RefreshAsset();
    }
}
