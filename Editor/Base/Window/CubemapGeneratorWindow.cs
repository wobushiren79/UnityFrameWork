using UnityEngine;
using UnityEditor;

public class CubemapGeneratorWindow : EditorWindow
{
    private Texture2D[] faceTextures = new Texture2D[6];
    private string[] faceNames = new string[]
    {
        "Right (+X)",
        "Left (-X)",
        "Down (-Y)",
        "Up (+Y)",
        "Front (+Z)",
        "Back (-Z)"
    };

    private CubemapFace[] cubemapFaces = new CubemapFace[]
    {
        CubemapFace.PositiveX,
        CubemapFace.NegativeX,
        CubemapFace.NegativeY,
        CubemapFace.PositiveY,
        CubemapFace.PositiveZ,
        CubemapFace.NegativeZ
    };

    // Cross layout mapping: [row, col] -> face index, -1 = empty
    //        [Up]
    // [Left] [Front] [Right] [Back]
    //        [Down]
    private static readonly int[,] crossLayout = new int[3, 4]
    {
        { -1, 3, -1, -1 },  // row 0:        Up
        {  1, 4,  0,  5 },  // row 1: Left  Front  Right  Back
        { -1, 2, -1, -1 },  // row 2:        Down
    };

    private static readonly int[] resolutionPresets = { 64, 128, 256, 512, 1024, 2048, 4096 };
    private int resolutionIndex = 3; // default 512

    private static readonly TextureFormat[] supportedFormats =
    {
        TextureFormat.RGBA32,
        TextureFormat.RGB24,
        TextureFormat.ARGB32,
        TextureFormat.RGBAHalf,
        TextureFormat.RGBAFloat,
    };
    private static readonly string[] formatNames =
    {
        "RGBA32",
        "RGB24",
        "ARGB32",
        "RGBAHalf (HDR)",
        "RGBAFloat (HDR)",
    };
    private int formatIndex = 0;

    private bool mipmap = false;
    private bool autoResize = true;
    private string savePath = "Assets/NewCubemap.cubemap";

    private bool showSettings = true;
    private bool showFaces = true;
    private Vector2 scrollPos;

    [MenuItem("Custom/工具弹窗/Cubemap生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<CubemapGeneratorWindow>("Cubemap生成器");
        window.minSize = new Vector2(420, 500);
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // === Settings Section ===
        showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSettings, "基本设置");
        if (showSettings)
        {
            EditorGUI.indentLevel++;

            resolutionIndex = EditorGUILayout.Popup("分辨率", resolutionIndex,
                System.Array.ConvertAll(resolutionPresets, r => r + "x" + r));

            formatIndex = EditorGUILayout.Popup("纹理格式", formatIndex, formatNames);

            mipmap = EditorGUILayout.Toggle("生成 Mipmaps", mipmap);
            autoResize = EditorGUILayout.Toggle("自动缩放不匹配纹理", autoResize);

            EditorGUILayout.BeginHorizontal();
            savePath = EditorGUILayout.TextField("保存路径", savePath);
            if (GUILayout.Button("浏览", GUILayout.Width(50)))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "保存Cubemap", "NewCubemap", "cubemap", "选择保存位置");
                if (!string.IsNullOrEmpty(path))
                    savePath = path;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // === Face Textures Section ===
        showFaces = EditorGUILayout.BeginFoldoutHeaderGroup(showFaces, "面纹理设置");
        if (showFaces)
        {
            DrawCrossLayout();

            EditorGUILayout.Space(2);
            if (GUILayout.Button("清空所有面纹理", GUILayout.Height(20)))
            {
                for (int i = 0; i < 6; i++)
                    faceTextures[i] = null;
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);

        // === Generate Button ===
        GUI.enabled = IsAnyFaceAssigned();
        Color oldBg = GUI.backgroundColor;
        GUI.backgroundColor = AllFacesAssigned() ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.8f, 0.4f);
        if (GUILayout.Button("生成 Cubemap", GUILayout.Height(36)))
            GenerateCubemap();
        GUI.backgroundColor = oldBg;
        GUI.enabled = true;

        // === Status ===
        EditorGUILayout.Space(4);
        if (!AllFacesAssigned())
        {
            string missing = "";
            for (int i = 0; i < 6; i++)
            {
                if (faceTextures[i] == null)
                    missing += (missing.Length > 0 ? ", " : "") + faceNames[i];
            }
            EditorGUILayout.HelpBox("缺少面纹理: " + missing, MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("所有面已就绪，可以生成 Cubemap。", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCrossLayout()
    {
        float previewSize = Mathf.Min(80, (position.width - 60) / 4f);

        for (int row = 0; row < 3; row++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (int col = 0; col < 4; col++)
            {
                int faceIdx = crossLayout[row, col];
                if (faceIdx < 0)
                {
                    GUILayout.Space(previewSize + 6);
                }
                else
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(previewSize));
                    EditorGUILayout.LabelField(faceNames[faceIdx], EditorStyles.miniLabel,
                        GUILayout.Width(previewSize));
                    faceTextures[faceIdx] = (Texture2D)EditorGUILayout.ObjectField(
                        faceTextures[faceIdx], typeof(Texture2D), false,
                        GUILayout.Width(previewSize), GUILayout.Height(previewSize));
                    EditorGUILayout.EndVertical();
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    private bool AllFacesAssigned()
    {
        for (int i = 0; i < 6; i++)
            if (faceTextures[i] == null) return false;
        return true;
    }

    private bool IsAnyFaceAssigned()
    {
        for (int i = 0; i < 6; i++)
            if (faceTextures[i] != null) return true;
        return false;
    }

    private void GenerateCubemap()
    {
        int resolution = resolutionPresets[resolutionIndex];
        TextureFormat textureFormat = supportedFormats[formatIndex];

        if (string.IsNullOrEmpty(savePath))
        {
            EditorUtility.DisplayDialog("错误", "请设置保存路径!", "确定");
            return;
        }

        if (!AllFacesAssigned())
        {
            EditorUtility.DisplayDialog("错误", "请为所有面分配纹理!", "确定");
            return;
        }

        // Check texture readability upfront
        for (int i = 0; i < 6; i++)
        {
            if (!faceTextures[i].isReadable)
            {
                EditorUtility.DisplayDialog("错误",
                    $"纹理 [{faceNames[i]}] 不可读。\n请在纹理导入设置中勾选 Read/Write Enabled。\n纹理: {faceTextures[i].name}",
                    "确定");
                return;
            }
        }

        // Check size mismatches
        if (!autoResize)
        {
            for (int i = 0; i < 6; i++)
            {
                var tex = faceTextures[i];
                if (tex.width != resolution || tex.height != resolution)
                {
                    if (!EditorUtility.DisplayDialog("尺寸不匹配",
                        $"{faceNames[i]} 尺寸为 {tex.width}x{tex.height}，目标为 {resolution}x{resolution}。\n强制使用原始尺寸可能导致错误。\n\n是否继续?",
                        "继续", "取消"))
                        return;
                    break; // Only ask once
                }
            }
        }

        Cubemap cubemap = new Cubemap(resolution, textureFormat, mipmap);

        try
        {
            for (int i = 0; i < 6; i++)
            {
                EditorUtility.DisplayProgressBar("生成Cubemap", $"处理 {faceNames[i]}...", (float)i / 6);

                Texture2D texture = faceTextures[i];
                bool needResize = texture.width != resolution || texture.height != resolution;

                if (needResize && autoResize)
                {
                    Texture2D resized = ResizeTexture(texture, resolution, resolution);
                    cubemap.SetPixels(resized.GetPixels(), cubemapFaces[i]);
                    DestroyImmediate(resized);
                }
                else
                {
                    cubemap.SetPixels(texture.GetPixels(), cubemapFaces[i]);
                }
            }

            EditorUtility.DisplayProgressBar("生成Cubemap", "保存资源...", 0.95f);

            cubemap.Apply();
            AssetDatabase.CreateAsset(cubemap, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("成功", $"Cubemap 已保存至:\n{savePath}", "确定");

            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(savePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"生成失败:\n{e.Message}", "确定");
        }
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight, source.format, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}
