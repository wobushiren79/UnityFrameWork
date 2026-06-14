using UnityEditor;
using UnityEngine;

/// <summary>
/// 像素图快速设置编辑器：在 Project 窗口右键选中的图片资源上，一键应用像素图导入设置。
/// </summary>
public class TextureSettingEditor : Editor
{
    #region 菜单项

    /// <summary>
    /// 右键菜单：将选中图片设置为像素图（PixelsPerUnit = 32）。
    /// </summary>
    [MenuItem("Assets/像素图设置32", false, 31)]
    static void SetPixelTexture32()
    {
        ApplyPixelTextureSetting(32);
    }

    /// <summary>
    /// 像素图设置32 菜单项的可用性校验：仅当选中了纹理资源时可用。
    /// </summary>
    [MenuItem("Assets/像素图设置32", true)]
    static bool SetPixelTexture32Validate()
    {
        return HasTextureSelected();
    }

    /// <summary>
    /// 右键菜单：将选中图片设置为像素图（PixelsPerUnit = 16）。
    /// </summary>
    [MenuItem("Assets/像素图设置16", false, 32)]
    static void SetPixelTexture16()
    {
        ApplyPixelTextureSetting(16);
    }

    /// <summary>
    /// 像素图设置16 菜单项的可用性校验：仅当选中了纹理资源时可用。
    /// </summary>
    [MenuItem("Assets/像素图设置16", true)]
    static bool SetPixelTexture16Validate()
    {
        return HasTextureSelected();
    }

    #endregion

    #region 私有逻辑

    /// <summary>
    /// 对选中的所有纹理资源批量应用像素图导入设置。
    /// 设置内容：TextureType=Sprite、SpriteMode=Single、FilterMode=Point、指定 PixelsPerUnit。
    /// </summary>
    /// <param name="pixelsPerUnit">每单位像素数（32 或 16）。</param>
    static void ApplyPixelTextureSetting(int pixelsPerUnit)
    {
        Object[] selectedObjects = Selection.objects;
        int successCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (Object selectedObject in selectedObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (textureImporter == null)
                    continue;

                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.filterMode = FilterMode.Point;
                textureImporter.spritePixelsPerUnit = pixelsPerUnit;

                EditorUtility.SetDirty(textureImporter);
                textureImporter.SaveAndReimport();
                successCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log($"像素图设置完成：PixelsPerUnit={pixelsPerUnit}，成功处理 {successCount} 个资源");
    }

    /// <summary>
    /// 判断当前选中资源中是否存在纹理资源（用于菜单项可用性校验）。
    /// </summary>
    /// <returns>存在至少一个纹理资源时返回 true。</returns>
    static bool HasTextureSelected()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrEmpty(assetPath))
                continue;

            if (AssetImporter.GetAtPath(assetPath) is TextureImporter)
                return true;
        }
        return false;
    }

    #endregion
}
