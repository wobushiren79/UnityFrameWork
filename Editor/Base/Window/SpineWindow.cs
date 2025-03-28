using UnityEngine;
using UnityEditor;
using Spine;
using Spine.Unity;
using System.Collections.Generic;
using System.IO;
using static Spine.Skin;
using Spine.Unity.AttachmentTools;
using Unity.VisualScripting;

public class SpineSkinExtractorWindow : EditorWindow
{
    // 配置参数
    private SkeletonDataAsset skeletonDataAsset;
    private string targetSkinName = "default";
    private string outputPath = "Assets/ExtractedSprites";
    private bool isPutAllSkin = true;
    [MenuItem("Custom/工具弹窗/Spine工具")]
    static void Init()
    {
        var window = GetWindow<SpineSkinExtractorWindow>();
        window.titleContent = new GUIContent("Spine工具");
        window.minSize = new Vector2(350, 200);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("皮肤图片提取工具", EditorStyles.boldLabel);
        
        // 数据源选择
        skeletonDataAsset = EditorGUILayout.ObjectField(
            new GUIContent("骨架数据源", "拖拽Spine的SkeletonDataAsset到这里"), 
            skeletonDataAsset, 
            typeof(SkeletonDataAsset), 
            false
        ) as SkeletonDataAsset;

        EditorGUILayout.BeginHorizontal();
        // 皮肤名称输入
        targetSkinName = EditorGUILayout.TextField(
            new GUIContent("目标皮肤名称", "需要提取的皮肤名称（区分大小写）"), 
            targetSkinName
        );
        EditorUI.GUIText("是否提取所有图片");
        isPutAllSkin = EditorGUILayout.Toggle(isPutAllSkin);
        EditorGUILayout.EndHorizontal();
        
        
        // 路径选择
        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField(
            new GUIContent("保存路径", "建议使用Assets下的路径"), 
            outputPath
        );
        if (GUILayout.Button("浏览...", GUILayout.Width(80)))
        {
            string selectedPath = EditorUtility.SaveFolderPanel("选择保存路径", outputPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                outputPath = selectedPath.Replace(Application.dataPath, "Assets");
            }
        }
        EditorGUILayout.EndHorizontal();

        // 操作按钮
        EditorGUILayout.Space(15);
        if (GUILayout.Button("开始提取", GUILayout.Height(30)))
        {
            if (ValidateParameters())
            {
                ExtractSkinTextures();
            }
        }
    }

    /// <summary>
    /// 参数验证
    /// </summary>
    bool ValidateParameters()
    {
        if (skeletonDataAsset == null)
        {
            Debug.LogError("必须指定骨架数据源！");
            return false;
        }

        if (string.IsNullOrEmpty(targetSkinName))
        {
            Debug.LogError("皮肤名称不能为空！");
            return false;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            Debug.LogError("必须指定保存路径！");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 核心提取逻辑
    /// </summary>
    void ExtractSkinTextures()
    {
        try
        {
            // 获取骨架数据
            SkeletonData skeletonData = skeletonDataAsset.GetSkeletonData(true);
            if (skeletonData == null)
            {
                Debug.LogError("无法获取骨架数据，请检查SkeletonDataAsset设置");
                return;
            }

            // 查找目标皮肤
            List<Skin> listTargetSkin = new List<Skin>();
            HashSet<string> usedRegions = new HashSet<string>();
            //导出所有皮肤
            if (isPutAllSkin)
            {
                foreach (Skin skin in skeletonData.Skins)
                {
                    listTargetSkin.Add(skin);
                }
            }
            //导出单个皮肤
            else
            {
                Skin targetSkin = skeletonData.FindSkin(targetSkinName);
                if (targetSkin == null)
                {
                    Debug.LogError($"找不到皮肤：{targetSkinName}，可用皮肤列表：\n{GetAvailableSkins(skeletonData)}");
                    return;
                }
                listTargetSkin.Add(targetSkin);
                // 收集所有使用的图集区域
                usedRegions = CollectUsedRegions(targetSkin, skeletonData);
            }

            foreach (var itemSkin in listTargetSkin)
            {
                // 收集所有使用的图集区域
                var itemUsedRegions = CollectUsedRegions(itemSkin, skeletonData);
                usedRegions.AddRange(itemUsedRegions);
            }

            // 提取并保存纹理
            ExtractAndSaveTextures(usedRegions);

            AssetDatabase.Refresh();
            Debug.Log($"提取完成！共找到{usedRegions.Count}个纹理区域，已保存至：{outputPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"提取过程中发生错误：{ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 收集皮肤使用的所有区域
    /// </summary>
    HashSet<string> CollectUsedRegions(Skin skin, SkeletonData skeletonData)
    {
        HashSet<string> regions = new HashSet<string>();
        
        // 新版Spine遍历方式（4.0+）
        List<SkinEntry> skinEntries = new List<SkinEntry>();
        foreach (SlotData slot in skeletonData.Slots)
        {
            skin.GetAttachments(slot.Index, skinEntries);
            foreach (SkinEntry entry in skinEntries)
            {
                ProcessAttachment(skin.GetAttachment(slot.Index, entry.Name), regions);
            }
        }

        // 旧版兼容方式（3.8及以下）
        #if !SPINE_TK2D && !SPINE_4_0
        foreach (var pair in skin.Attachments)
        {
            ProcessAttachment(pair.Value, regions);
        }
        #endif

        return regions;
    }

    /// <summary>
    /// 处理不同类型的附件
    /// </summary>
    void ProcessAttachment(Attachment attachment, HashSet<string> regions)
    {
        if (attachment == null) return;

        // 处理区域附件
        if (attachment is RegionAttachment regionAttachment)
        {
            if (regionAttachment.GetRegion() != null)
            {
                regions.Add(regionAttachment.GetRegion().name);
            }
        }
        // 处理网格附件
        else if (attachment is MeshAttachment meshAttachment)
        {
            if (meshAttachment.GetRegion() != null)
            {
                regions.Add(meshAttachment.GetRegion().name);
            }
        }
        // 处理其他类型附件...
    }

    /// <summary>
    /// 从图集提取并保存纹理
    /// </summary>
    void ExtractAndSaveTextures(HashSet<string> targetRegions)
    {
        // 确保目录存在
        Directory.CreateDirectory(outputPath);

        // 遍历所有图集
        foreach (AtlasAssetBase atlasAsset in skeletonDataAsset.atlasAssets)
        {
            if (!(atlasAsset is SpineAtlasAsset spineAtlas)) continue;

            // 获取图集数据
            Atlas atlas = spineAtlas.GetAtlas();
            if (atlas == null) continue;

            // 获取源纹理
            Texture2D sourceTexture = spineAtlas.materials[0].mainTexture as Texture2D;
            if (sourceTexture == null) continue;

            // 准备纹理读取
            TextureImporter texImporter = PrepareTextureForRead(sourceTexture);

            // 遍历图集区域
            foreach (AtlasRegion region in atlas.Regions)
            {
                if (targetRegions.Contains(region.name))
                {
                    SaveRegionTexture(spineAtlas, sourceTexture, region);
                }
            }

            // 还原纹理设置
            RestoreTextureSettings(texImporter);
        }
    }

    /// <summary>
    /// 准备纹理读取（启用Read/Write）
    /// </summary>
    TextureImporter PrepareTextureForRead(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (!importer.isReadable)
        {
            importer.isReadable = true;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Debug.Log($"临时启用Read/Write：{texture.name}");
        }

        return importer;
    }

    /// <summary>
    /// 还原纹理设置
    /// </summary>
    void RestoreTextureSettings(TextureImporter importer)
    {
        if (importer != null && importer.isReadable)
        {
            importer.isReadable = false;
            AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"恢复纹理设置：{importer.assetPath}");
        }
    }

    /// <summary>
    /// 保存区域纹理
    /// </summary>
    void SaveRegionTexture(SpineAtlasAsset spineAtlasAsset, Texture2D source, AtlasRegion region)
    {
        // 坐标转换
        int x = region.x;
        int y = source.height - region.y - region.height;

        // 边界检查
        if (x < 0 || y < 0 || x + region.width > source.width || y + region.height > source.height)
        {
            Debug.LogError($"Region {region.name} exceeds source texture bounds.");
            return;
        }

        // 处理旋转
        bool isRotated = region.rotate;
        int width = isRotated ? region.height : region.width;
        int height = isRotated ? region.width : region.height;

        // 提取像素
        Color[] pixels = source.GetPixels(x, y, region.width, region.height);
        Texture2D newTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        newTex.SetPixels(pixels);
        newTex.Apply();

        // 执行旋转
        if (isRotated)
        {
            newTex = RotateTextureCW(newTex);
        }

        // 确保输出目录存在
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        // 保存文件
        string fileName = $"{spineAtlasAsset.name}_{region.name}.png";
        string fullPath = Path.Combine(outputPath, fileName);
        File.WriteAllBytes(fullPath, newTex.EncodeToPNG());

        // 刷新资源数据库
        AssetDatabase.Refresh();

        // 设置导入参数
        string assetPath = fullPath.Replace(Application.dataPath, "Assets");
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spritePixelsPerUnit = 100;
            settings.mipmapEnabled = false;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.alphaIsTransparency = true; // 关键参数
            settings.filterMode= FilterMode.Point;
            importer.SetTextureSettings(settings);
            
            // 设置平台参数（示例）
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            
            importer.SaveAndReimport();
        }
        else
        {
            Debug.LogError($"Failed to load importer for: {assetPath}");
        }
    }

    public Texture2D RotateTextureCW(Texture2D original)
    {
        int originalWidth = original.width;
        int originalHeight = original.height;
        Color[] originalPixels = original.GetPixels();
        Color[] rotatedPixels = new Color[originalWidth * originalHeight];

        for (int x = 0; x < originalWidth; x++)
        {
            for (int y = 0; y < originalHeight; y++)
            {
                int newX = originalHeight - y - 1;
                int newY = x;
                rotatedPixels[newX + newY * originalHeight] = originalPixels[x + y * originalWidth];
            }
        }

        Texture2D rotatedTex = new Texture2D(originalHeight, originalWidth);
        rotatedTex.SetPixels(rotatedPixels);
        rotatedTex.Apply();
        return rotatedTex;
    }

    /// <summary>
    /// 获取可用皮肤列表
    /// </summary>
    string GetAvailableSkins(SkeletonData data)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (Skin skin in data.Skins)
        {
            sb.AppendLine($"- {skin.Name}");
        }
        return sb.ToString();
    }
}