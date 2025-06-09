using UnityEngine;
using UnityEditor;
using System.IO;

public class CubemapGeneratorWindow : EditorWindow
{
    private Texture2D[] faceTextures = new Texture2D[6];
    private string[] faceNames = new string[]
    {
        "右面 (X正方向)",
        "左面 (X负方向)",
        "下面 (Y负方向)",  // 注意这里交换了上下
        "上面 (Y正方向)",  // 注意这里交换了上下
        "前面 (Z正方向)",
        "后面 (Z负方向)"
    };

    private CubemapFace[] cubemapFaces = new CubemapFace[]
    {
        CubemapFace.PositiveX,
        CubemapFace.NegativeX,
        CubemapFace.NegativeY,  // 将PositiveY和NegativeY交换
        CubemapFace.PositiveY,  // 将PositiveY和NegativeY交换
        CubemapFace.PositiveZ,
        CubemapFace.NegativeZ
    };

    private int resolution = 512;
    private TextureFormat textureFormat = TextureFormat.RGBA32;
    private bool mipmap = false;
    private string savePath = "Assets/新Cubemap.cubemap";

    [MenuItem("Custom/工具弹窗/Cubemap生成器")]
    public static void ShowWindow()
    {
        GetWindow<CubemapGeneratorWindow>("Cubemap生成器");
    }

    void OnGUI()
    {
        GUILayout.Label("Cubemap生成器", EditorStyles.boldLabel);

        // 分辨率设置
        resolution = EditorGUILayout.IntField("分辨率", resolution);

        // 纹理格式
        textureFormat = (TextureFormat)EditorGUILayout.EnumPopup("纹理格式", textureFormat);

        // Mipmap选项
        mipmap = EditorGUILayout.Toggle("生成Mipmaps", mipmap);

        // 保存路径
        EditorGUILayout.BeginHorizontal();
        savePath = EditorGUILayout.TextField("保存路径", savePath);
        if (GUILayout.Button("浏览...", GUILayout.Width(80)))
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "保存Cubemap",
                "新Cubemap",
                "cubemap",
                "请输入要保存的Cubemap文件名");
            if (!string.IsNullOrEmpty(path))
            {
                savePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        // 六个面的纹理设置
        EditorGUILayout.Space();
        GUILayout.Label("面纹理设置", EditorStyles.boldLabel);

        for (int i = 0; i < 6; i++)
        {
            faceTextures[i] = (Texture2D)EditorGUILayout.ObjectField(
                faceNames[i],
                faceTextures[i],
                typeof(Texture2D),
                false);
        }

        // 生成按钮
        EditorGUILayout.Space();
        if (GUILayout.Button("生成Cubemap", GUILayout.Height(40)))
        {
            GenerateCubemap();
        }

        // 帮助文本
        EditorGUILayout.HelpBox(
            "使用说明：\n" +
            "1. 设置分辨率(必须是2的幂)\n" +
            "2. 为每个面分配纹理\n" +
            "3. 点击生成按钮",
            MessageType.Info);
    }

    private void GenerateCubemap()
    {
        // 检查分辨率是否为2的幂
        if ((resolution & (resolution - 1)) != 0)
        {
            EditorUtility.DisplayDialog("错误", "分辨率必须是2的幂(如64,128,256,512等)!", "确定");
            return;
        }

        // 检查是否有纹理缺失
        for (int i = 0; i < 6; i++)
        {
            if (faceTextures[i] == null)
            {
                EditorUtility.DisplayDialog("错误", $"请为所有面分配纹理！\n缺少: {faceNames[i]}", "确定");
                return;
            }
        }

        // 创建新的Cubemap
        Cubemap cubemap = new Cubemap(resolution, textureFormat, mipmap);

        try
        {
            // 设置每个面的纹理
            for (int i = 0; i < 6; i++)
            {
                Texture2D texture = faceTextures[i];

                // 检查纹理尺寸是否匹配
                if (texture.width != resolution || texture.height != resolution)
                {
                    if (EditorUtility.DisplayDialog("警告",
                        $"{faceNames[i]}纹理尺寸({texture.width}x{texture.height})与Cubemap分辨率({resolution}x{resolution})不匹配。\n是否要调整尺寸?",
                        "是", "否"))
                    {
                        Texture2D resizedTexture = ResizeTexture(texture, resolution, resolution);
                        cubemap.SetPixels(resizedTexture.GetPixels(), cubemapFaces[i]);
                    }
                    else
                    {
                        cubemap.SetPixels(texture.GetPixels(), cubemapFaces[i]);
                    }
                }
                else
                {
                    cubemap.SetPixels(texture.GetPixels(), cubemapFaces[i]);
                }
            }

            cubemap.Apply();

            // 保存到文件
            AssetDatabase.CreateAsset(cubemap, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", $"Cubemap生成成功！\n保存路径: {savePath}", "确定");

            // 高亮显示生成的Cubemap
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(savePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"生成Cubemap失败:\n{e.Message}", "确定");
        }
    }

    /// <summary>
    /// 调整纹理尺寸
    /// </summary>
    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        source.filterMode = FilterMode.Bilinear;
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}