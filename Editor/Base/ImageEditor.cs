using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 图片资源编辑器工具类（静态API）
/// 提供图片属性批量修改的编程接口，窗口功能已迁移至 ImageEditorWindow
/// </summary>
public static class ImageEditor
{
    /// <summary>
    /// 获取当前在Project窗口中选中的所有Texture2D资源
    /// </summary>
    public static UnityEngine.Object[] GetSelectedTextures()
    {
        return Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
    }

    #region 图片属性修改接口

    /// <summary>
    /// 修改图片的过滤模式。不指定目标时对当前选中图片生效。
    /// </summary>
    public static void ChangeImageFilterMode(FilterMode filterMode, string filePath = null, Texture2D targetText = null)
    {
        ApplyToTargets((ti) => ti.filterMode = filterMode, filePath, targetText);
    }

    /// <summary>
    /// 修改图片的每单位像素数。不指定目标时对当前选中图片生效。
    /// </summary>
    public static void ChangeImagePixelsPerUnit(float spritePixelsPerUnit, string filePath = null, Texture2D targetText = null)
    {
        ApplyToTargets((ti) => ti.spritePixelsPerUnit = spritePixelsPerUnit, filePath, targetText);
    }

    /// <summary>
    /// 修改图片的导入类型。不指定目标时对当前选中图片生效。
    /// </summary>
    public static void ChangeImageTextureImporterType(TextureImporterType textureImporterType, string filePath = null, Texture2D targetText = null)
    {
        ApplyToTargets((ti) => ti.textureType = textureImporterType, filePath, targetText);
    }

    /// <summary>
    /// 对指定图片的TextureImporter执行回调修改，不指定目标时对当前选中图片生效
    /// </summary>
    public static void GetTextureImporter(Action<TextureImporter> callBack, string filePath = null, Texture2D targetText = null)
    {
        ApplyToTargets(callBack, filePath, targetText);
    }

    #endregion

    private static void ApplyToTargets(Action<TextureImporter> callBack, string filePath, Texture2D targetText)
    {
        if (callBack == null) return;

        if (targetText != null)
        {
            filePath = AssetDatabase.GetAssetPath(targetText);
        }

        if (!string.IsNullOrEmpty(filePath))
        {
            ProcessOne(filePath, callBack);
            return;
        }

        var objs = GetSelectedTextures();
        if (objs.Length == 0)
        {
            Debug.LogError("没有选中图片");
            return;
        }
        foreach (var obj in objs)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            ProcessOne(path, callBack);
        }
    }

    private static void ProcessOne(string path, Action<TextureImporter> callBack)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null)
        {
            Debug.LogError($"无法获取TextureImporter: {path}");
            return;
        }
        callBack(ti);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}
