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
        EditorUI.GUIText("筛选名字 ,分割(只提取包含)", 150);
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
            if (selectSkeletonDataAsset != null)
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
        var skeletonData = skeletonDataAsset.GetSkeletonData(true);
        var targetRegions = CollectUsedRegions(skin, skeletonData);
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
                Texture2D originTex = null;
                foreach (var itemTemp in targetRegions)
                {
                    foreach (AtlasRegion region in atlas.Regions)
                    {
                        if (itemTemp.Equals(region.name))
                        {
                            System.Action actionStartMerge = () =>
                            {
                                var attachments = skin.Attachments;
                                RegionAttachment targetAttachment = null;
                                foreach (var itemAttachment in attachments)
                                {
                                    if (itemAttachment.Value.Name.Equals(region.name))
                                    {
                                        targetAttachment = (RegionAttachment)itemAttachment.Value;
                                    }
                                }
                                // 2. 创建临时的 Skeleton 实例
                                Skeleton skeleton = new Skeleton(skeletonData);
                                // 3. 应用默认姿势（初始变换）
                                skeleton.SetToSetupPose(); // 应用默认骨骼和插槽的初始位置
                                skeleton.UpdateWorldTransform(); // 计算世界变换
                                                                 // 4. 查找插槽
                                Slot slot = skeleton.FindSlot(region.name);
                                // 5. 获取插槽的父骨骼
                                Bone bone = slot.Bone;
                                // 6. 计算世界坐标
                                // 将附件的本地偏移转换为世界坐标
                                bone.LocalToWorld(targetAttachment.X, targetAttachment.Y, out float worldX, out float worldY);

                                Texture2D newTex = CreateRegionTexture(region, sourceTexture);
                                originTex = MergeTexturesForOverlay(originTex, newTex, worldX, worldY);
                            };


                            if (filterSkinName.IsNull())
                            {
                                actionStartMerge?.Invoke();
                            }
                            else
                            {
                                List<string> listFilter = filterSkinName.SplitForListStr(',');
                                foreach (var itemFilter in listFilter)
                                {
                                    string regionNameFirst = region.name.Split('_')[0];
                                    if (regionNameFirst.Equals(itemFilter))
                                    {
                                        actionStartMerge?.Invoke();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (originTex != null)
                {
                    originTex = CropTransparentEdges(originTex);
                    SaveRegionTexture(spineAtlas, skin, originTex, outputPath);
                }

            }
            else
            {
                foreach (AtlasRegion region in atlas.Regions)
                {
                    if (targetRegions.Contains(region.name))
                    {
                        if (filterSkinName.IsNull())
                        {
                            Texture2D newTex = CreateRegionTexture(region, sourceTexture);
                            SaveRegionTexture(spineAtlas, skin, newTex, outputPath);
                        }
                        else
                        {
                            List<string> listFilter = filterSkinName.SplitForListStr(',');
                            foreach (var itemFilter in listFilter)
                            {
                                string regionNameFirst = region.name.Split('_')[0];
                                if (regionNameFirst.Equals(itemFilter))
                                {
                                    Texture2D newTex = CreateRegionTexture(region, sourceTexture);
                                    SaveRegionTexture(spineAtlas, skin, newTex, outputPath);
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
    public static void SaveRegionTexture(SpineAtlasAsset spineAtlasAsset, Skin skin, Texture2D newTex, string outputPath)
    {
        if (newTex == null)
        {
            LogUtil.LogError($"创建贴图失败 skin_{skin.Name} spineAtlasAsset_{spineAtlasAsset.name}");
            return;
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
            Debug.LogError($"Failed to load importer for: {fullPath}");
        }
    }

    /// <summary>
    /// 创建一个贴图
    /// </summary>
    public static Texture2D CreateRegionTexture(AtlasRegion region, Texture2D source)
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
            return null;
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
        return newTex;
    }

    /// <summary>
    /// 合并2个贴图
    /// </summary>
    public static Texture2D MergeTexturesForOverlay(Texture2D textureOld, Texture2D textureNew, float posX, float posY)
    {
        //如果原始贴图为null 说明是第一次 需要先创建一个透明的底子------------
        int startSize = 1;
        Color colorNull = new Color(0, 0, 0, 0);
        if (textureOld == null)
        {
            textureOld = new Texture2D(startSize, startSize, TextureFormat.RGBA32, false);
            Color[] colors = new Color[startSize * startSize];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = colorNull;
            }
            textureOld.SetPixels(colors);
        }
        //----------------------------------------------------------------
        if (textureOld == null || textureNew == null)
        {
            Debug.LogError("没有贴图");
            return null;
        }

        int offsetX = Mathf.FloorToInt(posX * 100);
        int offsetY = Mathf.FloorToInt(posY * 100);

        int newTextSizeX = (Mathf.CeilToInt(textureNew.width / 2f) + Mathf.Abs(offsetX)) * 2;
        int newTextSizeY = (Mathf.CeilToInt(textureNew.height / 2f) + Mathf.Abs(offsetY)) * 2;

        int mergeTexSizeX = textureOld.width;
        int mergeTexSizeY = textureOld.height;

        //使用最大的
        if (mergeTexSizeX < newTextSizeX)
        {
            mergeTexSizeX = newTextSizeX;
        }
        if (mergeTexSizeY < newTextSizeY)
        {
            mergeTexSizeY = newTextSizeY;
        }

        // 创建新贴图
        Texture2D mergedResult = new Texture2D(mergeTexSizeX, mergeTexSizeY, TextureFormat.RGBA32, false);
        Color[] colorsMergedResult = new Color[mergeTexSizeX * mergeTexSizeY];
        for (int i = 0; i < colorsMergedResult.Length; i++)
        {
            colorsMergedResult[i] = colorNull;
        }
        mergedResult.SetPixels(colorsMergedResult);
        //设置老贴图
        for (int x = 0; x < textureOld.width; x++)
        {
            for (int y = 0; y < textureOld.height; y++)
            {
                Color oldColor = textureOld.GetPixel(x, y);
                mergedResult.SetPixel(x + (mergeTexSizeX / 2) - (textureOld.width / 2), y + (mergeTexSizeY / 2) - (textureOld.height / 2), oldColor);
            }
        }
        //设置新贴图
        for (int x = 0; x < textureNew.width; x++)
        {
            for (int y = 0; y < textureNew.height; y++)
            {
                Color newColor = textureNew.GetPixel(x, y);
                if (newColor.a > 0.01f)
                {
                    mergedResult.SetPixel(x - (textureNew.width / 2) + (mergeTexSizeX / 2) + offsetX, y - (textureNew.height / 2) + (mergeTexSizeY / 2) + offsetY, newColor);
                }
            }
        }

        mergedResult.Apply(); // 应用更改
        return mergedResult;
    }

    /// <summary>
    /// 裁切边缘透明部分
    /// </summary>
    public static Texture2D CropTransparentEdges(Texture2D targetTex)
    {
        // 获取像素数据
        Color32[] pixels = targetTex.GetPixels32();
        int width = targetTex.width;
        int height = targetTex.height;

        // 查找有效区域边界
        int xMin = width, xMax = 0;
        int yMin = height, yMax = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[x + y * width].a > 0)
                {
                    xMin = Mathf.Min(xMin, x);
                    xMax = Mathf.Max(xMax, x);
                    yMin = Mathf.Min(yMin, y);
                    yMax = Mathf.Max(yMax, y);
                }
            }
        }

        // 创建新纹理
        int newWidth = xMax - xMin + 1;
        int newHeight = yMax - yMin + 1;
        Texture2D croppedTexture = new Texture2D(newWidth, newHeight);

        // 复制有效像素
        Color32[] newPixels = new Color32[newWidth * newHeight];
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                newPixels[x + y * newWidth] = pixels[(x + xMin) + (y + yMin) * width];
            }
        }

        croppedTexture.SetPixels32(newPixels);
        croppedTexture.Apply();
        return croppedTexture;
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