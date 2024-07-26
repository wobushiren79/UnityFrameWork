using UnityEditor;
using UnityEngine;

public class CommonEditor : Editor
{
    [MenuItem("GameObject/复制路径")]
    static void CopyPath()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject != null)
        {
            string gameObjectPath = GetGameObjectPath(selectedObject);
            EditorGUIUtility.systemCopyBuffer = gameObjectPath;
            Debug.Log($"复制路径成功: {gameObjectPath}");
        }
        else
        {
            Debug.LogError("复制路径失败");
        }
    }

    [MenuItem("Assets/复制路径", false, 25)]
    static void CopyFilePathToClipboard()
    {
        string selectedFilePath = "";
        if (Selection.activeObject != null)
        {
            selectedFilePath = AssetDatabase.GetAssetPath(Selection.activeObject);
        }
        else
        {
            Object[] selectedObjects = Selection.GetFiltered(typeof(DefaultAsset), SelectionMode.Assets);
            if (selectedObjects.Length == 0)
            {
                Debug.Log("请选择一个目录");
                return;
            }
            selectedFilePath = AssetDatabase.GetAssetPath(selectedObjects[0]);
        }
        if (!string.IsNullOrEmpty(selectedFilePath))
        {
            EditorGUIUtility.systemCopyBuffer = selectedFilePath;
            Debug.Log($"复制路径成功{selectedFilePath} ");
        }
        else
        {
            Debug.Log("复制路径失败");
        }
    }

    static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}