using UnityEditor;
using UnityEngine;

/// <summary>
/// 像素图快速设置编辑器：在 Project 窗口右键选中的图片资源上，一键应用像素图导入设置。
/// </summary>
public class TextureSettingEditor : Editor
{
    #region 菜单项

    /// <summary>
    /// 右键菜单：将选中图片设置为像素图，PixelsPerUnit 按图片宽高中较大的那个值自动设置。
    /// </summary>
    [MenuItem("Assets/像素图设置自动", false, 29)]
    static void SetPixelTextureAuto()
    {
        ApplyPixelTextureSetting(null);
    }

    /// <summary>
    /// 像素图设置自动 菜单项的可用性校验：开关启用且选中了纹理资源时可用（开关关闭则在右键菜单中隐藏）。
    /// </summary>
    [MenuItem("Assets/像素图设置自动", true)]
    static bool SetPixelTextureAutoValidate()
    {
        return EditorMenuSwitch.IsEnabled(EditorMenuSwitch.PixelTextureSetting) && HasTextureSelected();
    }

    // 暂时隐藏 64/32/16 固定档位，只保留「像素图设置自动」。如需恢复，取消下方注释即可。
    // /// <summary>
    // /// 右键菜单：将选中图片设置为像素图（PixelsPerUnit = 64）。
    // /// </summary>
    // [MenuItem("Assets/像素图设置64", false, 30)]
    // static void SetPixelTexture64()
    // {
    //     ApplyPixelTextureSetting(64);
    // }
    //
    // /// <summary>
    // /// 像素图设置64 菜单项的可用性校验：开关启用且选中了纹理资源时可用（开关关闭则在右键菜单中隐藏）。
    // /// </summary>
    // [MenuItem("Assets/像素图设置64", true)]
    // static bool SetPixelTexture64Validate()
    // {
    //     return EditorMenuSwitch.IsEnabled(EditorMenuSwitch.PixelTextureSetting) && HasTextureSelected();
    // }
    //
    // /// <summary>
    // /// 右键菜单：将选中图片设置为像素图（PixelsPerUnit = 32）。
    // /// </summary>
    // [MenuItem("Assets/像素图设置32", false, 31)]
    // static void SetPixelTexture32()
    // {
    //     ApplyPixelTextureSetting(32);
    // }
    //
    // /// <summary>
    // /// 像素图设置32 菜单项的可用性校验：开关启用且选中了纹理资源时可用（开关关闭则在右键菜单中隐藏）。
    // /// </summary>
    // [MenuItem("Assets/像素图设置32", true)]
    // static bool SetPixelTexture32Validate()
    // {
    //     return EditorMenuSwitch.IsEnabled(EditorMenuSwitch.PixelTextureSetting) && HasTextureSelected();
    // }
    //
    // /// <summary>
    // /// 右键菜单：将选中图片设置为像素图（PixelsPerUnit = 16）。
    // /// </summary>
    // [MenuItem("Assets/像素图设置16", false, 32)]
    // static void SetPixelTexture16()
    // {
    //     ApplyPixelTextureSetting(16);
    // }
    //
    // /// <summary>
    // /// 像素图设置16 菜单项的可用性校验：开关启用且选中了纹理资源时可用（开关关闭则在右键菜单中隐藏）。
    // /// </summary>
    // [MenuItem("Assets/像素图设置16", true)]
    // static bool SetPixelTexture16Validate()
    // {
    //     return EditorMenuSwitch.IsEnabled(EditorMenuSwitch.PixelTextureSetting) && HasTextureSelected();
    // }

    #endregion

    #region 私有逻辑

    /// <summary>
    /// 对选中的所有纹理资源批量应用像素图导入设置。
    /// 设置内容：TextureType=Sprite、SpriteMode=Single、FilterMode=Point、所有平台 Compression=None、指定 PixelsPerUnit。
    /// </summary>
    /// <param name="pixelsPerUnit">每单位像素数（64、32 或 16）；传 null 表示自动按图片宽高中较大值逐个设置。</param>
    static void ApplyPixelTextureSetting(int? pixelsPerUnit)
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

                // 自动模式：取图片宽高中较大的那个值作为 PixelsPerUnit；取不到尺寸则跳过该资源
                int targetPixelsPerUnit = pixelsPerUnit ?? GetMaxSizeForTexture(assetPath);
                if (targetPixelsPerUnit <= 0)
                    continue;

                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.filterMode = FilterMode.Point;
                textureImporter.spritePixelsPerUnit = targetPixelsPerUnit;
                // 默认平台 + 各具体平台标签页统一设为 None（不依赖继承，防止后续被勾 Override 时变回压缩）
                SetAllPlatformCompressionNone(textureImporter);

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

        string modeDesc = pixelsPerUnit.HasValue ? $"PixelsPerUnit={pixelsPerUnit.Value}" : "PixelsPerUnit=自动(宽高较大值)";
        Debug.Log($"像素图设置完成：{modeDesc}，成功处理 {successCount} 个资源");
    }

    /// <summary>
    /// 将纹理的默认平台及各具体平台（PC/安卓/iOS/WebGL）的 Compression 全部设为 None（Uncompressed）。
    /// 默认平台用属性设置；具体平台通过 GetPlatformTextureSettings/SetPlatformTextureSettings 显式写入，
    /// 不依赖"未 Override 时继承默认平台"的隐式行为，避免后续被勾 Override 时回退成压缩格式。
    /// </summary>
    /// <param name="textureImporter">目标纹理导入器。</param>
    static void SetAllPlatformCompressionNone(TextureImporter textureImporter)
    {
        // 默认平台
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;

        // 各具体平台标签页（即使当前未 Override 也显式写入，保证下拉框显示与打包结果一致）
        string[] platformNames = { "Standalone", "Android", "iPhone", "WebGL" };
        foreach (string platformName in platformNames)
        {
            TextureImporterPlatformSettings platformSettings = textureImporter.GetPlatformTextureSettings(platformName);
            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SetPlatformTextureSettings(platformSettings);
        }
    }

    /// <summary>
    /// 读取图片资源的宽高，返回较大的那个值（用于自动设置 PixelsPerUnit）。
    /// </summary>
    /// <param name="assetPath">图片资源路径。</param>
    /// <returns>宽高中的较大值；加载失败时返回 0。</returns>
    static int GetMaxSizeForTexture(string assetPath)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
            return 0;
        return Mathf.Max(texture.width, texture.height);
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
