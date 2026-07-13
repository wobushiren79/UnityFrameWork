using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 地形高度图导出器：把 Unity Terrain 的高度数据导出成游戏可直接采样的灰度贴图
/// (EXR 浮点/PNG 8-bit)，并自动把导入设置改为 Linear + 无压缩 + Clamp，
/// 直接可喂高度图地形 shader (FrameWork/URP/MeshTerrain) 做顶点位移。
/// 坐标约定与该 shader/网格生成器一致:贴图 X 对应地形 X、贴图 Y 对应地形 Z。
/// </summary>
public class TerrainHeightmapExporterWindow : EditorWindow
{
    #region 字段(参数)
    /// <summary>导出格式:0=EXR(浮点/无台阶/推荐) / 1=PNG(8-bit/可能台阶)</summary>
    private enum ExportFormat { EXR, PNG }
    private ExportFormat format = ExportFormat.EXR;
    /// <summary>是否把实际 min-max 高度拉伸到 0-1(让偏暗的高度图变可见/好用, 但破坏 1:1 高度对应)</summary>
    private bool normalizeToRange = false;
    /// <summary>目标地形</summary>
    private Terrain terrain;
    /// <summary>网格保存路径</summary>
    private string savePath = "Assets/TerrainHeightmap.exr";

    private Vector2 scrollPos;
    #endregion

    #region 生命周期(窗口)
    /// <summary>打开地形高度图导出器窗口</summary>
    [MenuItem("Custom/工具弹窗/地形高度图导出")]
    public static void ShowWindow()
    {
        var window = GetWindow<TerrainHeightmapExporterWindow>("地形高度图导出");
        window.minSize = new Vector2(400, 320);
        window.TryPickSelectedTerrain();
    }

    /// <summary>绘制窗口 GUI</summary>
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("从 Unity Terrain 导出高度图", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        // 目标地形(为空时给出从选中拾取的按钮)
        EditorGUI.BeginChangeCheck();
        terrain = (Terrain)EditorGUILayout.ObjectField(
            new GUIContent("目标地形", "要导出高度图的 Terrain 物体"), terrain, typeof(Terrain), true);
        if (EditorGUI.EndChangeCheck()) SyncExtension();

        if (terrain == null)
        {
            if (GUILayout.Button("拾取当前选中的地形"))
                TryPickSelectedTerrain();
            EditorGUILayout.HelpBox("请指定一个 Terrain。", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space(4);

        // 格式
        EditorGUI.BeginChangeCheck();
        format = (ExportFormat)EditorGUILayout.EnumPopup(
            new GUIContent("导出格式", "EXR=浮点无台阶(推荐) / PNG=8-bit 平缓坡可能出台阶"), format);
        if (EditorGUI.EndChangeCheck()) SyncExtension();

        // 保存路径
        EditorGUILayout.BeginHorizontal();
        savePath = EditorGUILayout.TextField("保存路径", savePath);
        if (GUILayout.Button("浏览", GUILayout.Width(50)))
        {
            string ext = format == ExportFormat.EXR ? "exr" : "png";
            string path = EditorUtility.SaveFilePanelInProject("保存高度图", "TerrainHeightmap", ext, "选择保存位置");
            if (!string.IsNullOrEmpty(path)) savePath = path;
        }
        EditorGUILayout.EndHorizontal();

        // 地形信息
        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"高度图分辨率: {res} × {res}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"地形实际高度 (size.y): {td.size.y:0.##}", EditorStyles.miniLabel);
        EditorGUILayout.HelpBox(
            $"导出为归一化灰度(0-1)。若想 1:1 还原起伏, MeshTerrain 材质的\"起伏高度\"设为 {td.size.y:0.##} 即可。",
            MessageType.Info);
        if (format == ExportFormat.PNG)
            EditorGUILayout.HelpBox("PNG 为 8-bit(256 级), 平缓长坡可能出现台阶。追求平滑请选 EXR。", MessageType.Warning);

        normalizeToRange = EditorGUILayout.Toggle(
            new GUIContent("归一化到实际范围", "把地形实际最低-最高高度拉伸到 0-1。开=图更亮/对比强(但不再 1:1 对应 size.y)"),
            normalizeToRange);
        EditorGUILayout.HelpBox("高度图偏暗多为正常:地形只用了 0-1 的一小段。导出后会打印实际 min-max 范围, 想看清可开上面开关。", MessageType.None);

        EditorGUILayout.Space(6);

        // 导出按钮
        Color oldBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("导出高度图", GUILayout.Height(36)))
            Export();
        GUI.backgroundColor = oldBg;

        EditorGUILayout.EndScrollView();
    }
    #endregion

    #region 私有方法(拾取/路径)
    /// <summary>尝试把当前场景选中的地形设为目标</summary>
    private void TryPickSelectedTerrain()
    {
        if (Selection.activeGameObject != null)
        {
            var t = Selection.activeGameObject.GetComponent<Terrain>();
            if (t != null) { terrain = t; SyncExtension(); }
        }
    }

    /// <summary>保持保存路径扩展名与所选格式一致</summary>
    private void SyncExtension()
    {
        if (string.IsNullOrEmpty(savePath)) return;
        string dir = Path.GetDirectoryName(savePath);
        string name = Path.GetFileNameWithoutExtension(savePath);
        string ext = format == ExportFormat.EXR ? ".exr" : ".png";
        savePath = string.IsNullOrEmpty(dir) ? name + ext : (dir.Replace('\\', '/') + "/" + name + ext);
    }
    #endregion

    #region 私有方法(导出)
    /// <summary>读取地形高度并编码为贴图写盘, 再设为线性导入</summary>
    private void Export()
    {
        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog("错误", "请指定有效的 Terrain!", "确定");
            return;
        }
        if (string.IsNullOrEmpty(savePath))
        {
            EditorUtility.DisplayDialog("错误", "请设置保存路径!", "确定");
            return;
        }

        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;
        Texture2D tex = null;
        try
        {
            EditorUtility.DisplayProgressBar("导出高度图", "读取地形高度...", 0.2f);
            // GetHeights 返回 [y, x] 归一化高度; y↔地形Z, x↔地形X(与 shader UV 约定一致)
            float[,] heights = td.GetHeights(0, 0, res, res);

            // 统计实际高度范围, 便于判断是否只是"偏暗而非无数据"
            float min = 1f, max = 0f;
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float h = heights[y, x];
                    if (h < min) min = h;
                    if (h > max) max = h;
                }
            float range = max - min;
            bool doNormalize = normalizeToRange && range > 1e-6f;

            bool isExr = format == ExportFormat.EXR;
            tex = new Texture2D(res, res, isExr ? TextureFormat.RGBAFloat : TextureFormat.RGBA32, false, true);
            Color[] pixels = new Color[res * res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float h = heights[y, x];
                    if (doNormalize) h = (h - min) / range;     // 拉伸 min-max 到 0-1
                    pixels[y * res + x] = new Color(h, h, h, 1f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false);

            // 打印实际范围(全黑排查依据):max 很小说明地形本就矮, 数据非空
            Debug.Log($"[高度图导出] 归一化高度范围 min={min:0.####} max={max:0.####} " +
                      $"(对应世界高度 {min * td.size.y:0.##}~{max * td.size.y:0.##}); " +
                      (doNormalize ? "已拉伸到 0-1。" : (max < 0.1f ? "范围很小→图偏暗属正常, 可勾\"归一化到实际范围\"看清或直接调大 shader 起伏高度。" : "范围正常。")));

            EditorUtility.DisplayProgressBar("导出高度图", "编码写盘...", 0.7f);
            byte[] bytes = isExr ? tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat) : tex.EncodeToPNG();
            File.WriteAllBytes(savePath, bytes);

            AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
            ApplyLinearImportSettings(savePath, res);

            EditorUtility.ClearProgressBar();
            var asset = AssetDatabase.LoadMainAssetAtPath(savePath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            EditorUtility.DisplayDialog("成功", $"高度图已导出:\n{savePath}", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"导出失败:\n{e.Message}", "确定");
        }
        finally
        {
            if (tex != null) DestroyImmediate(tex);
        }
    }

    /// <summary>把导出的贴图设为线性 + 无压缩 + Clamp + 无 Mip, 保证高度采样精确</summary>
    private void ApplyLinearImportSettings(string path, int resolution)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;                    // 关键:线性, 灰度线性对应高度
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;  // 非2次幂不缩放, 保原分辨率
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        // maxTextureSize 取 ≥分辨率的2次幂, 避免被下采样丢精度
        int maxSize = 32;
        while (maxSize < resolution && maxSize < 8192) maxSize <<= 1;
        importer.maxTextureSize = Mathf.Min(8192, maxSize);
        importer.SaveAndReimport();
    }
    #endregion
}
