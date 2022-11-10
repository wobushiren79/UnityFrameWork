using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ImageEditor : Editor
{
    [MenuItem("Assets/资源/TextureArray")]
    public static void CreateTextureArray()
    {
        GameObject[] objSelectArray = Selection.gameObjects;
        if (objSelectArray.IsNull())
            return;
        List<Texture2D> listTex = new List<Texture2D>();
        foreach (var itemData in objSelectArray)
        {
            Texture2D tex2D = itemData.GetComponent < Texture2D >();
            if (tex2D != null)
                listTex.Add(tex2D);
        }
        if (listTex.IsNull())
            return;
        Texture2DArray texture2DArray = new Texture2DArray(
            listTex[0].width, 
            listTex[0].height,
            listTex.Count,
            TextureFormat.RGBA32,
            true,
            false);
    }

    [MenuItem("Custom/Image/Single")]
    public static void Single()
    {
        BaseSpriteEditor(SpriteImportMode.Single, 0, 0);
    }

    [MenuItem("Custom/Image/SingleDown")]
    public static void SingleDown()
    {
        BaseSpriteEditor(SpriteImportMode.Single, 1, 1,0.5f,0.01f);
    }

    [MenuItem("Custom/Image/Multiple_2x1")]
    public static void Multiple2x1()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 2, 1);
    }

    [MenuItem("Custom/Image/Multiple_3x1")]
    public static void Multiple3x1()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 3, 1);
    }

    [MenuItem("Custom/Image/Multiple_4x1")]
    public static void Multiple4x1()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 4, 1);
    }
    [MenuItem("Custom/Image/Multiple_4x1 Down")]
    public static void Multiple4x1Down()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 4, 1, 0.5f, 0f);
    }

    [MenuItem("Custom/Image/Multiple_5x1")]
    public static void Multiple5x1()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 5, 1);
    }

    [MenuItem("Custom/Image/Multiple_5x1_Extrude")]
    public static void Multiple5x1ForExtrude()
    {
        BaseSpriteEditorForExtrude(SpriteImportMode.Multiple, 5, 1);
    }
    [MenuItem("Custom/Image/Multiple_6x1")]
    public static void Multiple6x1()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 6, 1);
    }
    [MenuItem("Custom/Image/Multiple_7x1")]
    public static void Multiple7x1()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 7, 1);
    }

    [MenuItem("Custom/Image/Multiple_12x1")]
    public static void Multiple12x1()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 12, 1);
    }

    [MenuItem("Custom/Image/Multiple_5x2")]
    public static void Multiple5x2()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 5, 2);
    }

    [MenuItem("Custom/Image/Multiple_5x2_Extrude")]
    public static void Multiple5x2ForExtrude()
    {
        BaseSpriteEditorForExtrude(SpriteImportMode.Multiple, 5, 2);
    }

    [MenuItem("Custom/Image/Multiple_5x3")]
    public static void Multiple5x3()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 5, 3);
    }

    [MenuItem("Custom/Image/Multiple_5x4")]
    public static void Multiple5x4()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 5, 4);
    }

    [MenuItem("Custom/Image/Multiple_5x4_Extrude")]
    public static void Multiple5x4ForExtrude()
    {
        BaseSpriteEditorForExtrude(SpriteImportMode.Multiple, 5, 4);
    }

    [MenuItem("Custom/Image/Multiple_5x5")]
    public static void Multiple5x5()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 5, 5);
    }

    [MenuItem("Custom/Image/Multiple_5x6")]
    public static void Multiple5x6()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 5, 6);
    }

    [MenuItem("Custom/Image/Multiple_5x7")]
    public static void Multiple5x7()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 5, 7);
    }

    [MenuItem("Custom/Image/Multiple_5x8")]
    public static void Multiple5x8()
    {
        BaseSpriteEditor(SpriteImportMode.Multiple, 5, 7);
    }

    static void BaseSpriteEditor(SpriteImportMode spriteType, int cNumber, int rNumber)
    {
         BaseSpriteEditor( spriteType,  cNumber,  rNumber, 0.5f, 0.5f);
    }

    static void BaseSpriteEditor(SpriteImportMode spriteType, int cNumber, int rNumber, float pivotX, float pivotY)
    {
        Object[] objs = GetSelectedTextures();

        // Selection.objects = new Object[0];

        if (objs.Length <= 0)
        {
            LogUtil.LogError("没有选中图片");
            return;
        }
        for (int i = 0; i < objs.Length; i++)
        {
            Texture2D itemTexture = (Texture2D)objs[i];
            string path = AssetDatabase.GetAssetPath(itemTexture);

            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = spriteType;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.maxTextureSize = 8192;
            textureImporter.spritePixelsPerUnit = 32;
            textureImporter.compressionQuality = 100;
            textureImporter.isReadable = true;

            if (cNumber == 0 && rNumber == 0)
                continue;
            List<SpriteMetaData> newData = new List<SpriteMetaData>();

            float cItemSize = itemTexture.width / cNumber;
            float rItemSize = itemTexture.height / rNumber;
            int position = 0;
            for (int r = rNumber; r > 0; r--)
            {
                for (int c = 0; c < cNumber; c++)
                {

                    SpriteMetaData smd = new SpriteMetaData();
                    smd.alignment = 9;
                    smd.name = itemTexture.name + "_" + position;
                    smd.rect = new Rect(c * cItemSize, (r - 1) * rItemSize, cItemSize, rItemSize);
                    smd.pivot = new Vector2(pivotX, pivotY);
                    newData.Add(smd);
                    position++;
                }
            }
            textureImporter.spritePivot = new Vector2(pivotX, pivotY);
            textureImporter.spritesheet = newData.ToArray();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    static void BaseSpriteEditorForExtrude(SpriteImportMode spriteType, int cNumber, int rNumber)
    {
        BaseSpriteEditorForExtrude(spriteType, cNumber, rNumber, 0.5f, 0.5f);
    }
    static void BaseSpriteEditorForExtrude(SpriteImportMode spriteType, int cNumber, int rNumber,float pivotX,float pivotY)
    {
        Object[] objs = GetSelectedTextures();

        // Selection.objects = new Object[0];

        if (objs.Length <= 0)
        {
            LogUtil.LogError("没有选中图片");
            return;
        }
        for (int i = 0; i < objs.Length; i++)
        {
            Texture2D itemTexture = (Texture2D)objs[i];
            string path = AssetDatabase.GetAssetPath(itemTexture);

            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = spriteType;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.maxTextureSize = 8192;
            textureImporter.spritePixelsPerUnit = 32;
            textureImporter.compressionQuality = 100;
            textureImporter.isReadable = true;

            if (cNumber == 0 && rNumber == 0)
                continue;
            List<SpriteMetaData> newData = new List<SpriteMetaData>();

            float cItemSize = (itemTexture.width - 2 * cNumber) / cNumber;
            float rItemSize = (itemTexture.height - 2 * rNumber) / rNumber;
            int position = 0;
            for (int r = rNumber; r > 0; r--)
            {
                for (int c = 0; c < cNumber; c++)
                {

                    SpriteMetaData smd = new SpriteMetaData();
                    smd.pivot = new Vector2(pivotX, pivotY);
                    smd.alignment = 9;
                    smd.name = itemTexture.name + "_" + position;
                    smd.rect = new Rect(c * cItemSize + 1 + c * 2, (r - 1) * rItemSize + 1 + (r - 1) * 2, cItemSize, rItemSize);
                    newData.Add(smd);
                    position++;
                }
            }

            textureImporter.spritesheet = newData.ToArray();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
    static Object[] GetSelectedTextures()
    {
        return Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
    }
}
