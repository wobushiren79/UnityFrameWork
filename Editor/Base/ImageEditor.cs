using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 图片资源编辑器工具类
///
/// 功能说明:
/// 1. Texture2DArray生成 - 将多张选中的Texture2D合并为一个Texture2DArray资源
/// 2. Sprite图集切割 - 按指定的列x行网格自动切割Sprite图片,支持普通模式和Extrude(边距)模式
/// 3. 图片属性批量修改 - 提供FilterMode、PixelsPerUnit、TextureType等属性的快捷修改接口
///
/// 使用方式:
/// - 在Project窗口中选中一张或多张图片
/// - 通过菜单栏 Custom/图片工具/ 选择对应的切割方式
/// - 通过右键菜单 Assets/资源/创建Texture2DArray 生成纹理数组
/// - 也可在代码中直接调用 ChangeImageFilterMode / ChangeImagePixelsPerUnit 等静态方法
///
/// 切割参数说明:
/// - 列x行: 例如 5x4 表示横向5列、纵向4行,共切割出20个Sprite
/// - 锚点底部: Pivot设置在底部(Y=0.01),适用于需要脚底对齐的角色图
/// - 带边距: Extrude模式,每个Sprite四周有1像素边距,用于避免图集采样溢出
/// </summary>
public class ImageEditor : Editor
{
    [MenuItem("Assets/资源/创建Texture2DArray")]
    public static void CreateTextureArray()
    {
        UnityEngine.Object[] objs = GetSelectedTextures();
        if (objs == null || objs.Length == 0)
        {
            LogUtil.LogError("没有选中图片");
            return;
        }

        List<Texture2D> listTex = new List<Texture2D>();
        foreach (var obj in objs)
        {
            if (obj is Texture2D tex2D)
                listTex.Add(tex2D);
        }
        if (listTex.Count == 0)
            return;

        Texture2D first = listTex[0];
        Texture2DArray texture2DArray = new Texture2DArray(
            first.width,
            first.height,
            listTex.Count,
            TextureFormat.RGBA32,
            true,
            false);

        for (int i = 0; i < listTex.Count; i++)
        {
            Graphics.CopyTexture(listTex[i], 0, texture2DArray, i);
        }

        string dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(first));
        string savePath = $"{dir}/NewTexture2DArray.asset";
        AssetDatabase.CreateAsset(texture2DArray, savePath);
        AssetDatabase.SaveAssets();
        LogUtil.Log($"Texture2DArray 已保存到: {savePath}");
    }

    #region Sprite切割菜单

    [MenuItem("Custom/图片工具/单图(居中锚点)")]
    public static void Single()
    {
        BaseSpriteEditor(SpriteImportMode.Single, 0, 0);
    }

    [MenuItem("Custom/图片工具/单图(锚点底部)")]
    public static void SingleDown()
    {
        BaseSpriteEditor(SpriteImportMode.Single, 1, 1, 0.5f, 0.01f);
    }

    [MenuItem("Custom/图片工具/切割 2x1")]
    public static void Multiple2x1() => BaseSpriteEditor(SpriteImportMode.Multiple, 2, 1);

    [MenuItem("Custom/图片工具/切割 3x1")]
    public static void Multiple3x1() => BaseSpriteEditor(SpriteImportMode.Multiple, 3, 1);

    [MenuItem("Custom/图片工具/切割 4x1")]
    public static void Multiple4x1() => BaseSpriteEditor(SpriteImportMode.Multiple, 4, 1);

    [MenuItem("Custom/图片工具/切割 4x1(锚点底部)")]
    public static void Multiple4x1Down() => BaseSpriteEditor(SpriteImportMode.Multiple, 4, 1, 0.5f, 0f);

    [MenuItem("Custom/图片工具/切割 5x1")]
    public static void Multiple5x1() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 1);

    [MenuItem("Custom/图片工具/切割 5x1(带边距)")]
    public static void Multiple5x1ForExtrude() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 1, extrude: true);

    [MenuItem("Custom/图片工具/切割 6x1")]
    public static void Multiple6x1() => BaseSpriteEditor(SpriteImportMode.Multiple, 6, 1);

    [MenuItem("Custom/图片工具/切割 7x1")]
    public static void Multiple7x1() => BaseSpriteEditor(SpriteImportMode.Multiple, 7, 1);

    [MenuItem("Custom/图片工具/切割 12x1")]
    public static void Multiple12x1() => BaseSpriteEditor(SpriteImportMode.Multiple, 12, 1);

    [MenuItem("Custom/图片工具/切割 5x2")]
    public static void Multiple5x2() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 2);

    [MenuItem("Custom/图片工具/切割 5x2(带边距)")]
    public static void Multiple5x2ForExtrude() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 2, extrude: true);

    [MenuItem("Custom/图片工具/切割 5x3")]
    public static void Multiple5x3() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 3);

    [MenuItem("Custom/图片工具/切割 5x4")]
    public static void Multiple5x4() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 4);

    [MenuItem("Custom/图片工具/切割 5x4(带边距)")]
    public static void Multiple5x4ForExtrude() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 4, extrude: true);

    [MenuItem("Custom/图片工具/切割 5x5")]
    public static void Multiple5x5() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 5);

    [MenuItem("Custom/图片工具/切割 5x6")]
    public static void Multiple5x6() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 6);

    [MenuItem("Custom/图片工具/切割 5x7")]
    public static void Multiple5x7() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 7);

    [MenuItem("Custom/图片工具/切割 5x8")]
    public static void Multiple5x8() => BaseSpriteEditor(SpriteImportMode.Multiple, 5, 8);

    #endregion

    static void BaseSpriteEditor(SpriteImportMode spriteType, int cNumber, int rNumber,
        float pivotX = 0.5f, float pivotY = 0.5f, bool extrude = false)
    {
        UnityEngine.Object[] objs = GetSelectedTextures();
        if (objs.Length == 0)
        {
            LogUtil.LogError("没有选中图片");
            return;
        }

        for (int i = 0; i < objs.Length; i++)
        {
            Texture2D itemTexture = (Texture2D)objs[i];
            string path = AssetDatabase.GetAssetPath(itemTexture);
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter == null)
            {
                LogUtil.LogError($"无法获取TextureImporter: {path}");
                continue;
            }

            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = spriteType;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.maxTextureSize = 8192;
            textureImporter.spritePixelsPerUnit = 32;
            textureImporter.compressionQuality = 100;
            textureImporter.isReadable = true;

            if (cNumber == 0 && rNumber == 0)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                continue;
            }

            List<SpriteMetaData> newData = new List<SpriteMetaData>();

            float cItemSize, rItemSize;
            if (extrude)
            {
                cItemSize = (float)(itemTexture.width - 2 * cNumber) / cNumber;
                rItemSize = (float)(itemTexture.height - 2 * rNumber) / rNumber;
            }
            else
            {
                cItemSize = (float)itemTexture.width / cNumber;
                rItemSize = (float)itemTexture.height / rNumber;
            }

            int position = 0;
            for (int r = rNumber; r > 0; r--)
            {
                for (int c = 0; c < cNumber; c++)
                {
                    float x, y;
                    if (extrude)
                    {
                        x = c * cItemSize + 1 + c * 2;
                        y = (r - 1) * rItemSize + 1 + (r - 1) * 2;
                    }
                    else
                    {
                        x = c * cItemSize;
                        y = (r - 1) * rItemSize;
                    }

                    SpriteMetaData smd = new SpriteMetaData();
                    smd.alignment = 9;
                    smd.pivot = new Vector2(pivotX, pivotY);
                    smd.name = itemTexture.name + "_" + position;
                    smd.rect = new Rect(x, y, cItemSize, rItemSize);
                    newData.Add(smd);
                    position++;
                }
            }

            textureImporter.spritePivot = new Vector2(pivotX, pivotY);
            textureImporter.spritesheet = newData.ToArray();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    /// <summary>
    /// 获取当前在Project窗口中选中的所有Texture2D资源
    /// </summary>
    static UnityEngine.Object[] GetSelectedTextures()
    {
        return Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
    }

    #region 图片属性修改接口

    /// <summary>
    /// 修改图片的过滤模式 (Point/Bilinear/Trilinear)
    /// </summary>
    public static void ChangeImageFilterMode(FilterMode filterMode, string filePath = null, Texture2D targetText = null)
    {
        GetTextureImporter((textureImporter) =>
        {
            textureImporter.filterMode = filterMode;
        }, filePath, targetText);
    }

    /// <summary>
    /// 修改图片的每单位像素数
    /// </summary>
    public static void ChangeImagePixelsPerUnit(float spritePixelsPerUnit, string filePath = null, Texture2D targetText = null)
    {
        GetTextureImporter((textureImporter) =>
        {
            textureImporter.spritePixelsPerUnit = spritePixelsPerUnit;
        }, filePath, targetText);
    }

    /// <summary>
    /// 修改图片的导入类型 (Sprite/Default/NormalMap等)
    /// </summary>
    public static void ChangeImageTextureImporterType(TextureImporterType textureImporterType, string filePath = null, Texture2D targetText = null)
    {
        GetTextureImporter((textureImporter) =>
        {
            textureImporter.textureType = textureImporterType;
        }, filePath, targetText);
    }

    /// <summary>
    /// 获取指定图片的TextureImporter并执行回调修改,修改后自动重新导入
    /// 可通过filePath或targetText指定目标图片
    /// </summary>
    public static void GetTextureImporter(Action<TextureImporter> callBack, string filePath = null, Texture2D targetText = null)
    {
        if (targetText != null)
        {
            filePath = AssetDatabase.GetAssetPath(targetText);
        }
        if (filePath == null)
            return;

        TextureImporter textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (textureImporter == null)
        {
            LogUtil.LogError($"无法获取TextureImporter: {filePath}");
            return;
        }
        callBack?.Invoke(textureImporter);
        AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
    }

    #endregion
}
