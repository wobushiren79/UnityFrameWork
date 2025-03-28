using UnityEngine;
using UnityEditor;
using Spine;
using Spine.Unity;
using System.Collections.Generic;
using System.IO;
using static Spine.Skin;
using Spine.Unity.AttachmentTools;
using Unity.VisualScripting;

public class SpineWindow : EditorWindow
{
    // 配置参数
    private SkeletonDataAsset skeletonDataAsset;
    private string targetSkinName = "default";
    private string outputPath = "Assets/LoadResources/Textures/Items";
    private string inputPath = "Assets/LoadResources/Spine/Creature";
    private bool isPutAllSkin = true;
    private string filterSkinName = "Clothes,Pants,Weapon,Shoes,Hat,Mask";//筛选名字

    [MenuItem("Custom/工具弹窗/Spine工具")]
    static void Init()
    {
        var window = GetWindow<SpineWindow>();
        window.titleContent = new GUIContent("Spine工具");
        window.minSize = new Vector2(350, 200);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("皮肤图片提取工具（只针对皮肤）", EditorStyles.boldLabel);

        // 路径选择
        EditorGUILayout.BeginHorizontal();
        inputPath = EditorGUILayout.TextField(
            new GUIContent("骨架数据文件夹", ""),
            inputPath
        );
        if (GUILayout.Button("浏览...", GUILayout.Width(80)))
        {
            string selectedPath = EditorUtility.SaveFolderPanel("选择骨架数据文件夹路径", inputPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                inputPath = selectedPath.Replace(Application.dataPath, "Assets");
            }
        }
        EditorGUILayout.EndHorizontal();

        // 数据源选择
        skeletonDataAsset = EditorGUILayout.ObjectField(
            new GUIContent("骨架数据源", "拖拽Spine的SkeletonDataAsset到这里"),
            skeletonDataAsset,
            typeof(SkeletonDataAsset),
            false
        ) as SkeletonDataAsset;


        // 皮肤名称输入
        targetSkinName = EditorGUILayout.TextField(
            new GUIContent("目标皮肤名称", "需要提取的皮肤名称（区分大小写）"),
            targetSkinName
        );

        EditorGUILayout.BeginHorizontal();
        EditorUI.GUIText("是否提取皮肤下所有图片", 150);
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

        EditorGUILayout.BeginHorizontal();
        EditorUI.GUIText("筛选名字 ,分割", 150);
        filterSkinName = EditorUI.GUIEditorText(filterSkinName, 500);
        EditorGUILayout.EndHorizontal();

        // 操作按钮
        EditorGUILayout.Space(15);
        if (GUILayout.Button("开始提取", GUILayout.Height(30)))
        {
            if (ValidateParameters())
            {
                ExtractSkinTextures(inputPath, outputPath, skeletonDataAsset, isPutAllSkin, targetSkinName, filterSkinName);
            }
        }
    }

    /// <summary>
    /// 参数验证
    /// </summary>
    bool ValidateParameters()
    {
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
    public static void ExtractSkinTextures(
        string inputPath,//输入路径
        string outputPath,//输出路径
        SkeletonDataAsset selectSkeletonDataAsset,//传入的指定骨架资源
        bool isPutAllSkin,//是否输出所有的皮肤
        string targetSkinName,//指定皮肤输出
        string filterSkinName//筛选的皮肤名字 填入之后只输出带有这些字符的图片（用,分割）
        )
    {
        try
        {
            List<SkeletonDataAsset> listSkeletonDataAsset = new List<SkeletonDataAsset>();
            if (inputPath.IsNull())
            {
                // 获取骨架数据
                SkeletonData skeletonData = selectSkeletonDataAsset.GetSkeletonData(true);
                if (skeletonData == null)
                {
                    Debug.LogError("无法获取骨架数据，请检查SkeletonDataAsset设置");
                    return;
                }
                listSkeletonDataAsset.Add(selectSkeletonDataAsset);
            }
            else
            {
                listSkeletonDataAsset = FindAllSkeletonDataAssetsInDirectory(inputPath);
            }
            if (listSkeletonDataAsset.IsNull())
            {
                Debug.LogError("没有骨架数据");
                return;
            }
            foreach (var skeletonDataAsset in listSkeletonDataAsset)
            {
                var skeletonData = skeletonDataAsset.GetSkeletonData(true);
                // 查找目标皮肤
                List<Skin> listTargetSkin = new List<Skin>();
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
                }

                foreach (var itemSkin in listTargetSkin)
                {
                    // 提取并保存纹理
                    ExtractAndSaveTextures(skeletonDataAsset, itemSkin, outputPath, filterSkinName);
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"提取完成！，已保存至：{outputPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"提取过程中发生错误：{ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 收集皮肤使用的所有区域
    /// </summary>
    public static HashSet<string> CollectUsedRegions(Skin skin, SkeletonData skeletonData)
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
    public static void ProcessAttachment(Attachment attachment, HashSet<string> regions)
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
    public static void ExtractAndSaveTextures(SkeletonDataAsset skeletonDataAsset, Skin skin, string outputPath, string filterSkinName)
    {
        // 收集所有使用的图集区域
        var targetRegions = CollectUsedRegions(skin, skeletonDataAsset.GetSkeletonData(true));
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
            // 如果不止1张图片 需要进行合并
            if (targetRegions.Count > 1)
            {
                // 暂时不生成 在
                // var attachments = skin.Attachments;
                // List<RegionAttachment> listData = new List<RegionAttachment>();
                // foreach (var itemAttachment in attachments)
                // {
                //     Attachment attachment = itemAttachment.Value;
                //     // 处理区域附件
                //     if (attachment is RegionAttachment regionAttachment)
                //     {
                //         if (regionAttachment.GetRegion() != null)
                //         {
                //             listData.Add(regionAttachment);
                //         }
                //     
                //     // 处理网格附件
                //     else if (attachment is MeshAttachment meshAttachment)
                //     {
                //         if (meshAttachment.GetRegion() != null)
                //         {

                //         }
                //     }
                // }

            }
            else
            {
                foreach (AtlasRegion region in atlas.Regions)
                {
                    if (targetRegions.Contains(region.name))
                    {
                        if (filterSkinName.IsNull())
                        {
                            SaveRegionTexture(spineAtlas, skin, sourceTexture, region, outputPath);
                        }
                        else
                        {
                            List<string> listFilter = filterSkinName.SplitForListStr(',');
                            foreach (var itemFilter in listFilter)
                            {
                                if (region.name.Contains(itemFilter))
                                {
                                    SaveRegionTexture(spineAtlas, skin, sourceTexture, region, outputPath);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // 还原纹理设置
            RestoreTextureSettings(texImporter);
        }
    }

    /// <summary>
    /// 准备纹理读取（启用Read/Write）
    /// </summary>
    public static TextureImporter PrepareTextureForRead(Texture2D texture)
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
    public static void RestoreTextureSettings(TextureImporter importer)
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
    public static void SaveRegionTexture(SpineAtlasAsset spineAtlasAsset, Skin skin, Texture2D source, AtlasRegion region, string outputPath)
    {
        bool isRotated = region.rotate;

        // 计算实际提取尺寸（旋转时需要交换宽高）
        int extractWidth = isRotated ? region.height : region.width;
        int extractHeight = isRotated ? region.width : region.height;

        // 坐标转换（y坐标计算要使用实际提取高度）
        int x = region.x;
        int y = source.height - region.y - extractHeight; // 关键修改点

        // 边界检查（使用实际提取尺寸）
        if (x < 0 || y < 0 || x + extractWidth > source.width || y + extractHeight > source.height)
        {
            Debug.LogError($"Region {region.name} exceeds source texture bounds.");
            return;
        }

        // 提取像素（使用调整后的尺寸）
        Color[] pixels = source.GetPixels(x, y, extractWidth, extractHeight);

        // 创建临时纹理（尺寸为实际提取的尺寸）
        Texture2D tempTex = new Texture2D(extractWidth, extractHeight, TextureFormat.RGBA32, false);
        tempTex.SetPixels(pixels);
        tempTex.Apply();

        Texture2D newTex = tempTex;

        // 执行旋转（自动处理宽高交换）
        if (isRotated)
        {
            newTex = RotateTextureCW(tempTex);
        }

        // 确保输出目录存在
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        // 保存文件
        string fileName = $"{spineAtlasAsset.name}_{skin.Name.Replace("/", "_")}.png";
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
            settings.filterMode = FilterMode.Point;
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

    public static Texture2D RotateTextureCW(Texture2D original)
    {
        int originalWidth = original.width;
        int originalHeight = original.height;
        Color[] originalPixels = original.GetPixels();
        Color[] rotatedPixels = new Color[originalWidth * originalHeight];

        for (int x = 0; x < originalWidth; x++)
        {
            for (int y = 0; y < originalHeight; y++)
            {
                int newX = y;
                int newY = originalWidth - x - 1;
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
    public static string GetAvailableSkins(SkeletonData data)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (Skin skin in data.Skins)
        {
            sb.AppendLine($"- {skin.Name}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 查询指定目录下所有的资源
    /// </summary>
    /// <param name="directoryPath"></param>
    /// <returns></returns>
    public static List<SkeletonDataAsset> FindAllSkeletonDataAssetsInDirectory(string searchPath)
    {
        List<SkeletonDataAsset> assets = new List<SkeletonDataAsset>();
        AssetDatabase.Refresh();
        string[] guids = AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { searchPath });
        Debug.Log($"找到 {guids.Length} 个GUID");

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            SkeletonDataAsset asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(assetPath);
            if (asset != null) assets.Add(asset);
        }
        return assets;
    }
}