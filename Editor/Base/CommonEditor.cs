using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CommonEditor : Editor
{
    #region GameObject 路径

    /// <summary>
    /// 复制 Hierarchy 中选中 GameObject 的层级路径
    /// </summary>
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

    /// <summary>
    /// 逐级向上拼接 GameObject 的层级路径
    /// </summary>
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

    #endregion

    #region 资源路径（子菜单：复制资源路径 / 复制全路径）

    /// <summary>
    /// 复制选中资源（含文件夹）的工程相对路径，如 Assets/xxx
    /// </summary>
    [MenuItem("Assets/复制路径/复制资源路径", false, 0)]
    static void CopyAssetPathToClipboard()
    {
        CopySelectedAssetPaths(false);
    }

    /// <summary>
    /// 复制选中资源（含文件夹）的磁盘绝对路径，如 E:/Project/Assets/xxx
    /// </summary>
    [MenuItem("Assets/复制路径/复制全路径", false, 1)]
    static void CopyAssetFullPathToClipboard()
    {
        CopySelectedAssetPaths(true);
    }

    /// <summary>
    /// 收集 Project 窗口选中项（文件与文件夹均支持、支持多选）的路径并写入剪贴板
    /// </summary>
    /// <param name="isFullPath">true 输出磁盘绝对路径，false 输出工程相对路径</param>
    static void CopySelectedAssetPaths(bool isFullPath)
    {
        List<string> pathList = new List<string>();
        // 用 assetGUIDs 遍历，文件夹(DefaultAsset)与普通资源一并覆盖，避免 activeObject 判空漏掉文件夹
        foreach (string guid in Selection.assetGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) continue;
            pathList.Add(isFullPath ? GetFullPath(assetPath) : assetPath);
        }

        if (pathList.Count == 0)
        {
            Debug.Log("复制路径失败：请在 Project 窗口选择文件或文件夹");
            return;
        }

        string result = string.Join("\n", pathList);
        EditorGUIUtility.systemCopyBuffer = result;
        Debug.Log($"复制路径成功：\n{result}");
    }

    /// <summary>
    /// 把工程相对路径(Assets/…)转为磁盘绝对路径，统一为正斜杠
    /// </summary>
    static string GetFullPath(string assetPath)
    {
        return Path.GetFullPath(assetPath).Replace('\\', '/');
    }

    #endregion
}
