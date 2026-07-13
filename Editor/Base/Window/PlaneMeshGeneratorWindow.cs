using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

/// <summary>
/// 细分平面网格生成器：一键生成 N×N 细分的平面 Mesh 资源，供高度图地形 shader
/// (FrameWork/URP/MeshTerrain) 做顶点位移用。网格约定与该 shader 完全对齐：
/// 躺在 XZ 平面、法线 +Y、UV(0-1) 对应 X/Z，故生成后直接采高度图即可起伏。
/// </summary>
public class PlaneMeshGeneratorWindow : EditorWindow
{
    #region 字段(参数)
    /// <summary>每边细分格数 N(顶点数 = (N+1)²)</summary>
    private int subdivisions = 100;
    /// <summary>平面世界宽度(X 方向)</summary>
    private float width = 10f;
    /// <summary>平面世界深度(Z 方向)</summary>
    private float depth = 10f;
    /// <summary>中心粗化偏向:0=均匀细分, 1=顶点最大限度往四周边缘聚集(中心平面区留最少顶点)</summary>
    private float centerCoarseBias = 0f;
    /// <summary>枢轴是否居中(否则枢轴在角落)</summary>
    private bool centerPivot = true;
    /// <summary>是否生成切线(供法线贴图/描边等需要切线的 shader)</summary>
    private bool generateTangents = true;
    /// <summary>生成后是否在场景中创建带该网格的物体</summary>
    private bool createInScene = false;
    /// <summary>网格资源保存路径</summary>
    private string savePath = "Assets/TerrainPlane.asset";

    private bool showBasic = true;
    private bool showOutput = true;
    private Vector2 scrollPos;
    #endregion

    #region 生命周期(窗口)
    /// <summary>打开细分平面网格生成器窗口</summary>
    [MenuItem("Custom/工具弹窗/细分平面网格生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<PlaneMeshGeneratorWindow>("细分平面网格生成器");
        window.minSize = new Vector2(380, 380);
    }

    /// <summary>绘制窗口 GUI</summary>
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // === 基本设置 ===
        showBasic = EditorGUILayout.BeginFoldoutHeaderGroup(showBasic, "基本设置");
        if (showBasic)
        {
            EditorGUI.indentLevel++;
            subdivisions = EditorGUILayout.IntSlider(
                new GUIContent("细分格数 N", "每边细分的格子数, 顶点越密起伏越平滑"), subdivisions, 1, 1000);
            width = EditorGUILayout.FloatField(new GUIContent("宽度 (X)", "平面世界宽度"), width);
            depth = EditorGUILayout.FloatField(new GUIContent("深度 (Z)", "平面世界深度"), depth);
            centerCoarseBias = EditorGUILayout.Slider(
                new GUIContent("中心粗化偏向", "0=均匀 / 越大顶点越往四周边缘聚集, 平坦中心区留最少顶点(省面)"),
                centerCoarseBias, 0f, 1f);
            centerPivot = EditorGUILayout.Toggle(
                new GUIContent("枢轴居中", "开=网格中心为原点 / 关=角落为原点"), centerPivot);
            generateTangents = EditorGUILayout.Toggle(
                new GUIContent("生成切线", "供法线贴图等需要切线的 shader, 不需要可关闭省体积"), generateTangents);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // === 输出设置 ===
        showOutput = EditorGUILayout.BeginFoldoutHeaderGroup(showOutput, "输出设置");
        if (showOutput)
        {
            EditorGUI.indentLevel++;
            createInScene = EditorGUILayout.Toggle(
                new GUIContent("场景中创建物体", "生成后额外在场景放一个挂好该网格的物体"), createInScene);
            EditorGUILayout.BeginHorizontal();
            savePath = EditorGUILayout.TextField("保存路径", savePath);
            if (GUILayout.Button("浏览", GUILayout.Width(50)))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "保存网格", "TerrainPlane", "asset", "选择保存位置");
                if (!string.IsNullOrEmpty(path))
                    savePath = path;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(6);

        // === 统计信息 ===
        long vertexCount = (long)(subdivisions + 1) * (subdivisions + 1);
        long triangleCount = (long)subdivisions * subdivisions * 2;
        EditorGUILayout.LabelField($"顶点数: {vertexCount:N0}    三角面: {triangleCount:N0}", EditorStyles.miniLabel);
        if (vertexCount > 65535)
            EditorGUILayout.HelpBox("顶点数超过 65535, 将自动使用 32 位索引 (UInt32)。", MessageType.Info);
        if (vertexCount > 4000000)
            EditorGUILayout.HelpBox("顶点数过大, 可能导致编辑器卡顿或内存占用过高。", MessageType.Warning);

        EditorGUILayout.Space(4);

        // === 生成按钮 ===
        Color oldBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("生成细分平面网格", GUILayout.Height(36)))
            GenerateMesh();
        GUI.backgroundColor = oldBg;

        EditorGUILayout.EndScrollView();
    }
    #endregion

    #region 私有方法(网格生成)
    /// <summary>按当前参数构建平面网格并保存为资源</summary>
    private void GenerateMesh()
    {
        if (string.IsNullOrEmpty(savePath))
        {
            EditorUtility.DisplayDialog("错误", "请设置保存路径!", "确定");
            return;
        }

        try
        {
            Mesh mesh = BuildPlaneMesh();
            mesh.name = System.IO.Path.GetFileNameWithoutExtension(savePath);

            // 覆盖已存在资源时复用其 GUID(避免断开引用), 否则新建
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                DestroyImmediate(mesh);
                mesh = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, savePath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (createInScene)
                CreateSceneObject(mesh);

            Selection.activeObject = mesh;
            EditorGUIUtility.PingObject(mesh);
            EditorUtility.DisplayDialog("成功", $"网格已保存至:\n{savePath}", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"生成失败:\n{e.Message}", "确定");
        }
    }

    /// <summary>
    /// 构建 (N+1)×(N+1) 顶点的平面网格。躺 XZ 平面、法线 +Y、UV(0-1) 对应 X/Z，
    /// 三角面绕序使正面朝上(从上方可见), 顶点超 65535 时自动切 32 位索引。
    /// </summary>
    private Mesh BuildPlaneMesh()
    {
        int vx = subdivisions + 1;          // 每行顶点数
        int vertexCount = vx * vx;

        Mesh mesh = new Mesh();
        if (vertexCount > 65535)
            mesh.indexFormat = IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector4[] tangents = generateTangents ? new Vector4[vertexCount] : null;

        float offsetX = centerPivot ? width * 0.5f : 0f;   // 居中时整体左移半宽
        float offsetZ = centerPivot ? depth * 0.5f : 0f;
        // 偏向指数:1=均匀, <1 时顶点向两端边缘聚集(中心变稀疏)
        float biasExp = Mathf.Lerp(1f, 0.25f, centerCoarseBias);

        for (int z = 0; z < vx; z++)
        {
            for (int x = 0; x < vx; x++)
            {
                int i = z * vx + x;
                // 归一化格子索引经偏向重映射为世界比例(UV 跟随世界位置, 高度图采样不拉伸)
                float fx = BiasToEdge((float)x / subdivisions, biasExp);
                float fz = BiasToEdge((float)z / subdivisions, biasExp);
                vertices[i] = new Vector3(fx * width - offsetX, 0f, fz * depth - offsetZ);
                uvs[i] = new Vector2(fx, fz);               // UV 铺满 0-1 供高度图采样
                normals[i] = Vector3.up;
                if (tangents != null) tangents[i] = new Vector4(1f, 0f, 0f, -1f);
            }
        }

        int[] triangles = new int[subdivisions * subdivisions * 6];
        int t = 0;
        for (int z = 0; z < subdivisions; z++)
        {
            for (int x = 0; x < subdivisions; x++)
            {
                int bl = z * vx + x;        // 左下
                int br = bl + 1;            // 右下
                int tl = bl + vx;          // 左上
                int tr = tl + 1;           // 右上
                // 绕序 (bl,tl,tr)/(bl,tr,br) → 法线 +Y, 从上方可见
                triangles[t++] = bl; triangles[t++] = tl; triangles[t++] = tr;
                triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = br;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        if (tangents != null) mesh.tangents = tangents;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// 把均匀的 0..1 参数按指数向两端(边缘)聚集。exponent=1 时恒等(均匀);
    /// exponent&lt;1 时中心变稀疏、边缘变密, 用于把顶点省给起伏的边缘区。
    /// </summary>
    private float BiasToEdge(float p, float exponent)
    {
        if (Mathf.Approximately(exponent, 1f)) return p;   // 均匀:零成本恒等
        float d = (p - 0.5f) * 2f;                          // 映射到 -1..1(0=中心)
        float signed = Mathf.Sign(d) * Mathf.Pow(Mathf.Abs(d), exponent);
        return 0.5f + 0.5f * signed;                        // 回到 0..1
    }

    /// <summary>在场景中创建挂好该网格的物体(带 MeshFilter/MeshRenderer, 可撤销)</summary>
    private void CreateSceneObject(Mesh mesh)
    {
        GameObject go = new GameObject(mesh.name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        Undo.RegisterCreatedObjectUndo(go, "创建细分平面");
        Selection.activeGameObject = go;
    }
    #endregion
}
