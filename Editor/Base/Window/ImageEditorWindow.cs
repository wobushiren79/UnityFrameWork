using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 图片处理工具窗口
/// 功能：Sprite切片、Texture2DArray创建、图片属性批量修改
/// </summary>
public class ImageEditorWindow : EditorWindow
{
    #region Sprite切片参数
    private int _sliceColumns = 5;
    private int _sliceRows = 4;
    private float _pivotX = 0.5f;
    private float _pivotY = 0.5f;
    private bool _extrude = false;
    #endregion

    #region 图片属性修改参数
    private FilterMode _targetFilterMode = FilterMode.Point;
    private float _targetPixelsPerUnit = 32f;
    private TextureImporterType _targetTextureType = TextureImporterType.Sprite;
    #endregion

    private Vector2 _scrollPosition;

    [MenuItem("Custom/工具弹窗/图片处理")]
    static void CreateWindows()
    {
        var window = GetWindow<ImageEditorWindow>();
        window.titleContent = new GUIContent("图片处理");
        window.minSize = new Vector2(420, 520);
    }

    private void OnGUI()
    {
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        GUILayout.BeginVertical();

        DrawSpriteSliceSection();
        GUILayout.Space(8);
        DrawTextureArraySection();
        GUILayout.Space(8);
        DrawImagePropertySection();

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    #region Sprite切片
    private void DrawSpriteSliceSection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Sprite 图集切割", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "在Project中选中一张或多张图片，设置切割参数后点击执行。\n" +
            "支持普通模式和Extrude（边距）模式。",
            MessageType.Info);

        // 切割参数
        EditorGUILayout.BeginHorizontal();
        _sliceColumns = EditorGUILayout.IntField("列数", _sliceColumns);
        GUILayout.Space(10);
        _sliceRows = EditorGUILayout.IntField("行数", _sliceRows);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _pivotX = EditorGUILayout.Slider("Pivot X", _pivotX, 0f, 1f);
        _pivotY = EditorGUILayout.Slider("Pivot Y", _pivotY, 0f, 1f);
        EditorGUILayout.EndHorizontal();

        _extrude = EditorGUILayout.Toggle("Extrude（带边距）", _extrude);
        GUILayout.Space(4);

        // 快捷预设
        GUILayout.Label("快捷预设:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("单图(居中)", GUILayout.Width(80))) ApplyPreset(0, 0, 0.5f, 0.5f, false);
        if (GUILayout.Button("单图(底部)", GUILayout.Width(80))) ApplyPreset(1, 1, 0.5f, 0.01f, false);
        if (GUILayout.Button("4x1(底部)", GUILayout.Width(80))) ApplyPreset(4, 1, 0.5f, 0f, false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("5x1", GUILayout.Width(50))) ApplyPreset(5, 1, 0.5f, 0.5f, false);
        if (GUILayout.Button("5x1+E", GUILayout.Width(50))) ApplyPreset(5, 1, 0.5f, 0.5f, true);
        if (GUILayout.Button("5x2", GUILayout.Width(50))) ApplyPreset(5, 2, 0.5f, 0.5f, false);
        if (GUILayout.Button("5x2+E", GUILayout.Width(50))) ApplyPreset(5, 2, 0.5f, 0.5f, true);
        if (GUILayout.Button("5x3", GUILayout.Width(50))) ApplyPreset(5, 3, 0.5f, 0.5f, false);
        if (GUILayout.Button("5x4", GUILayout.Width(50))) ApplyPreset(5, 4, 0.5f, 0.5f, false);
        if (GUILayout.Button("5x4+E", GUILayout.Width(50))) ApplyPreset(5, 4, 0.5f, 0.5f, true);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("5x5", GUILayout.Width(50))) ApplyPreset(5, 5, 0.5f, 0.5f, false);
        if (GUILayout.Button("5x6", GUILayout.Width(50))) ApplyPreset(5, 6, 0.5f, 0.5f, false);
        if (GUILayout.Button("5x7", GUILayout.Width(50))) ApplyPreset(5, 7, 0.5f, 0.5f, false);
        if (GUILayout.Button("5x8", GUILayout.Width(50))) ApplyPreset(5, 8, 0.5f, 0.5f, false);
        if (GUILayout.Button("6x1", GUILayout.Width(50))) ApplyPreset(6, 1, 0.5f, 0.5f, false);
        if (GUILayout.Button("7x1", GUILayout.Width(50))) ApplyPreset(7, 1, 0.5f, 0.5f, false);
        if (GUILayout.Button("12x1", GUILayout.Width(50))) ApplyPreset(12, 1, 0.5f, 0.5f, false);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);
        if (GUILayout.Button("执行切割", GUILayout.Height(28)))
        {
            SpriteSlice.Execute(_sliceColumns, _sliceRows, _pivotX, _pivotY, _extrude);
        }

        EditorGUILayout.EndVertical();
    }

    private void ApplyPreset(int cols, int rows, float px, float py, bool extrude)
    {
        _sliceColumns = cols;
        _sliceRows = rows;
        _pivotX = px;
        _pivotY = py;
        _extrude = extrude;
    }
    #endregion

    #region Texture2DArray
    private void DrawTextureArraySection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Texture2DArray 创建", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "在Project中选中多张Texture2D，合并为一个Texture2DArray资源。",
            MessageType.Info);

        if (GUILayout.Button("创建 Texture2DArray", GUILayout.Height(28)))
        {
            TextureArrayCreator.Create();
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region 图片属性修改
    private void DrawImagePropertySection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("图片属性批量修改", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "在Project中选中一张或多张图片，修改导入属性。",
            MessageType.Info);

        _targetFilterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", _targetFilterMode);
        _targetPixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", _targetPixelsPerUnit);
        _targetTextureType = (TextureImporterType)EditorGUILayout.EnumPopup("Texture Type", _targetTextureType);

        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("应用 FilterMode", GUILayout.Height(24)))
        {
            ImageEditor.ChangeImageFilterMode(_targetFilterMode);
        }
        if (GUILayout.Button("应用 PixelsPerUnit", GUILayout.Height(24)))
        {
            ImageEditor.ChangeImagePixelsPerUnit(_targetPixelsPerUnit);
        }
        if (GUILayout.Button("应用 TextureType", GUILayout.Height(24)))
        {
            ImageEditor.ChangeImageTextureImporterType(_targetTextureType);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
    #endregion
}

/// <summary>
/// Sprite切片执行逻辑（从ImageEditor提取）
/// </summary>
internal static class SpriteSlice
{
    public static void Execute(int cNumber, int rNumber, float pivotX, float pivotY, bool extrude)
    {
        var objs = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
        if (objs.Length == 0)
        {
            Debug.LogError("没有选中图片");
            return;
        }

        for (int i = 0; i < objs.Length; i++)
        {
            Texture2D itemTexture = (Texture2D)objs[i];
            string path = AssetDatabase.GetAssetPath(itemTexture);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
            {
                Debug.LogError($"无法获取TextureImporter: {path}");
                continue;
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = cNumber == 0 && rNumber == 0 ? SpriteImportMode.Single : SpriteImportMode.Multiple;
            ti.filterMode = FilterMode.Point;
            ti.maxTextureSize = 8192;
            ti.spritePixelsPerUnit = 32;
            ti.compressionQuality = 100;
            ti.isReadable = true;

            if (cNumber == 0 && rNumber == 0)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                continue;
            }

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

            List<SpriteMetaData> newData = new List<SpriteMetaData>();
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

            ti.spritePivot = new Vector2(pivotX, pivotY);
            ti.spritesheet = newData.ToArray();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}

/// <summary>
/// Texture2DArray创建逻辑（从ImageEditor提取）
/// </summary>
internal static class TextureArrayCreator
{
    public static void Create()
    {
        var objs = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
        if (objs == null || objs.Length == 0)
        {
            Debug.LogError("没有选中图片");
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
        Debug.Log($"Texture2DArray 已保存到: {savePath}");
    }
}
