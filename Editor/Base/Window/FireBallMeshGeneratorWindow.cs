using UnityEngine;
using UnityEditor;

/// <summary>
/// 火球网格生成器：一键生成"火星汤(+中心火球核心)"Mesh 资源，供火球 shader
/// (FrameWork/URP/MeshFireBallInstanced1) 在 vertex shader 内跑 GPU 粒子模拟用。
/// 网格约定与该 shader 完全对齐(改任一方必须同步改另一方)：
/// 每个火星/核心 = 1 个独立 quad(4 顶点/6 索引, 顶点不焊接)，顶点属性含义见 <see cref="GenerateMesh"/>。
/// </summary>
public class FireBallMeshGeneratorWindow : EditorWindow
{
    #region 字段(参数)
    /// <summary>火星数量(= quad 数, 顶点数 = 本值×4)</summary>
    private int sparkCount = 64;
    /// <summary>随机种子(固定则每次生成结果一致, 便于复现)</summary>
    private int randomSeed = 12345;
    /// <summary>发散方向的锥形角度(度)：360=全向球状喷射, 越小越收拢成一束</summary>
    private float spreadAngle = 360f;
    /// <summary>锥形喷射的中心轴(spreadAngle 小于360 时生效, 物体空间)</summary>
    private Vector3 spreadAxis = Vector3.forward;
    /// <summary>速度倍率的随机范围(逐火星, 乘到 shader 的 _SparkDistance 上)</summary>
    private Vector2 speedScaleRange = new Vector2(0.6f, 1.4f);
    /// <summary>大小倍率的随机范围(逐火星, 乘到 shader 的火星大小上)</summary>
    private Vector2 sizeScaleRange = new Vector2(0.5f, 1.5f);
    /// <summary>生命倍率的随机范围(逐火星, 乘到 shader 的 _SparkRate 上, 使各火星生灭快慢不一)</summary>
    private Vector2 lifeScaleRange = new Vector2(0.7f, 1.3f);
    /// <summary>是否额外烤一个"中心火球核心"quad(顶点色 a=1, 由 shader 用程序化噪声烧出火焰)</summary>
    private bool includeCore = true;
    /// <summary>
    /// 包围盒半径：必须 ≥ 起始半径+发散距离+下坠距离 + **火星世界化拖拽距离**, 否则火星飞出盒外时整团被视锥剔除(闪烁/消失)。
    /// <para>默认 13 的由来：拖拽距离 = 最大弹速 × 火星最大寿命 = (speed_move 上限 3 × attackerSpeedRate 上限 3) × 1/(_SparkRate 1 × 生命倍率下限 0.7) ≈ 12.9。
    /// ⚠️拖拽在世界空间做(不随缩放变)、bounds 却是物体空间且会被实例矩阵的 visualScale 缩小, 故 visualScale&lt;1 的弹道需按 13/visualScale 再放大。</para>
    /// </summary>
    private float boundsRadius = 13f;
    /// <summary>生成后是否在场景中创建带该网格的物体(便于立刻预览)</summary>
    private bool createInScene = true;
    /// <summary>网格资源保存路径</summary>
    private string savePath = "Assets/FireSparkMesh.asset";

    private bool showBasic = true;
    private bool showRandom = true;
    private bool showOutput = true;
    private Vector2 scrollPos;
    #endregion

    #region 生命周期(窗口)
    /// <summary>打开火球网格生成器窗口</summary>
    [MenuItem("Custom/工具弹窗/火球网格生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<FireBallMeshGeneratorWindow>("火球网格生成器");
        window.minSize = new Vector2(380, 420);
    }

    /// <summary>绘制窗口 GUI</summary>
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.HelpBox(
            "生成配合 FrameWork/URP/MeshFireBallInstanced1 使用的火球网格。\n" +
            "火星数量 = quad 数，改数量 = 重新生成网格(shader 不用动)。",
            MessageType.Info);

        // === 基本设置 ===
        showBasic = EditorGUILayout.BeginFoldoutHeaderGroup(showBasic, "基本设置");
        if (showBasic)
        {
            EditorGUI.indentLevel++;
            sparkCount = EditorGUILayout.IntSlider(
                new GUIContent("火星数量", "= 火星 quad 数量"), sparkCount, 1, 2000);
            includeCore = EditorGUILayout.Toggle(
                new GUIContent("包含中心火球", "额外烤一个核心 quad(顶点色 a=1), 由 shader 用程序化噪声烧出火焰。关掉则只有火星"), includeCore);
            spreadAngle = EditorGUILayout.Slider(
                new GUIContent("喷射角度", "360=全向球状喷射 / 越小越收拢成一束"), spreadAngle, 1f, 360f);
            using (new EditorGUI.DisabledScope(spreadAngle >= 360f))
            {
                spreadAxis = EditorGUILayout.Vector3Field(
                    new GUIContent("喷射中心轴", "锥形喷射的朝向(物体空间), 喷射角度=360 时无意义"), spreadAxis);
            }
            boundsRadius = EditorGUILayout.FloatField(
                new GUIContent("包围盒半径",
                    "必须 ≥ 起始半径+发散距离+下坠距离 + 火星世界化拖拽距离(=弹速×火星寿命), 否则火星飞出盒外会被整团剔除。\n" +
                    "默认 13 = 最大弹速 9 × 火星最大寿命 1.43s；弹体 visualScale<1 时还需按 13/visualScale 放大(拖拽是世界空间的, bounds 会被缩放缩小)"),
                boundsRadius);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // === 逐火星随机 ===
        showRandom = EditorGUILayout.BeginFoldoutHeaderGroup(showRandom, "逐火星随机 (使火星彼此错开)");
        if (showRandom)
        {
            EditorGUI.indentLevel++;
            randomSeed = EditorGUILayout.IntField(
                new GUIContent("随机种子", "固定则每次生成结果一致, 便于复现"), randomSeed);
            speedScaleRange = DrawRangeField("速度倍率范围", "逐火星随机, 乘到 shader 的 发散距离 上", speedScaleRange);
            sizeScaleRange = DrawRangeField("大小倍率范围", "逐火星随机, 乘到 shader 的 火星大小 上", sizeScaleRange);
            lifeScaleRange = DrawRangeField("生命倍率范围", "逐火星随机, 乘到 shader 的 发散频率 上, 使生灭快慢不一", lifeScaleRange);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // === 输出 ===
        showOutput = EditorGUILayout.BeginFoldoutHeaderGroup(showOutput, "输出");
        if (showOutput)
        {
            EditorGUI.indentLevel++;
            savePath = EditorGUILayout.TextField(new GUIContent("保存路径", "网格资源(.asset)的保存路径"), savePath);
            createInScene = EditorGUILayout.Toggle(
                new GUIContent("在场景中创建物体", "生成后立刻放一个带该网格的物体到场景, 便于预览"), createInScene);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);
        int quadTotal = sparkCount + (includeCore ? 1 : 0);
        EditorGUILayout.LabelField(
            $"quad 数: {quadTotal} ({sparkCount} 火星{(includeCore ? " + 1 核心" : "")})   顶点数: {quadTotal * 4}   三角面: {quadTotal * 2}",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(4);

        if (GUILayout.Button("生成火星网格", GUILayout.Height(30)))
        {
            CreateAndSave();
        }

        EditorGUILayout.EndScrollView();
    }
    #endregion

    #region 私有方法(网格生成)
    /// <summary>
    /// 生成火球网格：N 个火星 quad(+可选 1 个中心核心 quad)，彼此互不相连，每 quad 的 4 顶点共享一份随机数据。
    /// 顶点属性含义(与 MeshFireBallInstanced1 严格对齐, 改任一方必须同步改另一方)：
    /// position=恒原点(真实位置由 shader 算) / normal=发散方向(核心用不到) / color.a=quad 类型(0=火星 1=核心) /
    /// uv0=角点UV(兼作贴图UV) / uv1=(种子,速度倍率,大小倍率,生命倍率)。
    /// </summary>
    private Mesh GenerateMesh()
    {
        // 用独立 Random.State 保证"同种子=同结果", 且不污染外部随机序列
        Random.State oldState = Random.state;
        Random.InitState(randomSeed);

        // 核心 quad 放在**最后**: 前 sparkCount 个 quad 恒为火星, 索引布局与"是否含核心"无关, 便于排查
        int quadCount = sparkCount + (includeCore ? 1 : 0);
        int vertCount = quadCount * 4;
        Vector3[] vertices = new Vector3[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        Color[] colors = new Color[vertCount];
        Vector2[] uv0 = new Vector2[vertCount];
        Vector4[] uv1 = new Vector4[vertCount];
        int[] triangles = new int[quadCount * 6];

        // quad 的 4 个角点 UV(0..1), shader 里 (uv-0.5) 即为 billboard 角点偏移
        Vector2[] cornerUV = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };

        for (int i = 0; i < quadCount; i++)
        {
            bool isCore = includeCore && i == sparkCount;

            // 逐火星随机：方向 + 一份随机数据(4 顶点共享, 保证同 quad 的顶点算出同一个 t/位置, 否则 quad 会被撕开)
            Vector3 dir = isCore ? Vector3.up : RandomDirection();
            Vector4 sparkData = isCore
                ? Vector4.zero                                                  // 核心不参与生命周期, 数据留空
                : new Vector4(
                    Random.value,                                               // x=种子(相位错开)
                    Random.Range(speedScaleRange.x, speedScaleRange.y),         // y=速度倍率
                    Random.Range(sizeScaleRange.x, sizeScaleRange.y),           // z=大小倍率
                    Random.Range(lifeScaleRange.x, lifeScaleRange.y));          // w=生命倍率
            // rgb 保留(恒为白, 留给"逐火星随机染色"等后续扩展); a=quad 类型标记
            Color vertColor = new Color(1f, 1f, 1f, isCore ? 1f : 0f);

            int vBase = i * 4;
            for (int c = 0; c < 4; c++)
            {
                vertices[vBase + c] = Vector3.zero;   // 恒原点: 真实位置完全由 shader 算
                normals[vBase + c] = dir;
                colors[vBase + c] = vertColor;
                uv0[vBase + c] = cornerUV[c];
                uv1[vBase + c] = sparkData;
            }

            int tBase = i * 6;
            triangles[tBase + 0] = vBase + 0;
            triangles[tBase + 1] = vBase + 2;
            triangles[tBase + 2] = vBase + 1;
            triangles[tBase + 3] = vBase + 0;
            triangles[tBase + 4] = vBase + 3;
            triangles[tBase + 5] = vBase + 2;
        }

        Random.state = oldState;

        Mesh mesh = new Mesh();
        mesh.name = "FireSparkMesh";
        // 顶点数超 65535 时需 32 位索引(火星数 > 16383 时)
        mesh.indexFormat = vertCount > 65535
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.uv = uv0;
        mesh.SetUVs(1, uv1);
        mesh.triangles = triangles;
        // ⚠️必须手工写 bounds, 不能 RecalculateBounds: 顶点全在原点 → 算出的盒子体积为0 → 火星一飞出就被视锥剔除(整团闪没)
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * boundsRadius * 2f);
        return mesh;
    }

    /// <summary>按喷射角度随机一个方向：360=全向球面均匀分布, 否则=绕中心轴的锥形均匀分布。</summary>
    private Vector3 RandomDirection()
    {
        if (spreadAngle >= 360f)
        {
            return Random.onUnitSphere;
        }

        Vector3 axis = spreadAxis.sqrMagnitude < 1e-6f ? Vector3.forward : spreadAxis.normalized;
        // 锥内均匀采样: cosθ 在 [cos(half), 1] 上均匀(直接对 θ 均匀会在轴心处过密)
        float halfAngle = spreadAngle * 0.5f * Mathf.Deg2Rad;
        float cosTheta = Random.Range(Mathf.Cos(halfAngle), 1f);
        float sinTheta = Mathf.Sqrt(Mathf.Max(0f, 1f - cosTheta * cosTheta));
        float phi = Random.Range(0f, Mathf.PI * 2f);

        // 构造以 axis 为 Z 的正交基, 把局部锥向量转到物体空间
        Vector3 helper = Mathf.Abs(axis.y) > 0.99f ? Vector3.right : Vector3.up;
        Vector3 tangent = Vector3.Normalize(Vector3.Cross(helper, axis));
        Vector3 bitangent = Vector3.Cross(axis, tangent);
        return tangent * (sinTheta * Mathf.Cos(phi)) + bitangent * (sinTheta * Mathf.Sin(phi)) + axis * cosTheta;
    }
    #endregion

    #region 私有方法(输出)
    /// <summary>生成网格并保存为资源, 按需在场景中创建预览物体。</summary>
    private void CreateAndSave()
    {
        if (string.IsNullOrEmpty(savePath) || !savePath.StartsWith("Assets/"))
        {
            EditorUtility.DisplayDialog("路径错误", "保存路径必须以 Assets/ 开头。", "确定");
            return;
        }
        if (!savePath.EndsWith(".asset"))
        {
            savePath += ".asset";
        }

        Mesh mesh = GenerateMesh();

        // 已存在则覆盖内容(保留 GUID, 避免已引用该网格的材质/预制体断链)
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);
        if (existing != null)
        {
            existing.Clear();
            EditorUtility.CopySerialized(mesh, existing);
            AssetDatabase.SaveAssets();
            mesh = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, savePath);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.Refresh();

        if (createInScene)
        {
            GameObject go = new GameObject("FireSparkPreview");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Undo.RegisterCreatedObjectUndo(go, "创建火星预览物体");
            Selection.activeGameObject = go;
        }
        else
        {
            Selection.activeObject = mesh;
        }

        Debug.Log($"[火球网格生成器] 已生成 {sparkCount} 个火星{(includeCore ? " + 1 个中心火球" : "")}({mesh.vertexCount} 顶点) → {savePath}");
    }

    /// <summary>绘制一个"最小~最大"范围输入行。</summary>
    private static Vector2 DrawRangeField(string label, string tooltip, Vector2 value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent(label, tooltip));
        value.x = EditorGUILayout.FloatField(value.x);
        value.y = EditorGUILayout.FloatField(value.y);
        EditorGUILayout.EndHorizontal();
        return value;
    }
    #endregion
}
