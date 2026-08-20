using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 花海 GPU Instancing(Indirect) 渲染器：框架层通用组件，挂场景即用（不走 Handler/Manager 体系，公开 API 由调用方触发）。
/// <para>【核心】整片花海 = 纯数据（每朵花一条 32B 实例数据），全场一次 Graphics.DrawMeshInstancedIndirect 画完（1 个 draw call，零 GameObject/Renderer 开销），突破 DrawMeshInstanced 1023/批硬限。</para>
/// <para>【数据流】Generate() 一次性生成静态实例 buffer（位置/缩放/图集变体/风摆相位/yaw）+ 图集 Rect buffer；
/// 消散走独立的每实例 float 动态 buffer（dissolveStartTime，哨兵 -1），仅踩踏发生帧整份重传（10k 实例=40KB，微秒级），
/// shader 内 progress=saturate((_Time.y-start)/duration) 驱动噪声抖动裁剪（像素 dither 消散）。</para>
/// <para>【零物理】踩踏检测不走 Collider：TrampleAt(worldPos, radius) 走 XZ 空间哈希网格查花；消散花在 shader 里 clip 消失，不删实例、不改 instanceCount，零回读零 CPU 压缩。</para>
/// <para>【贴图】图集模式（自动均分 cols×rows 或手动 Rect 列表覆盖）/ 单图列表模式（运行时 PackTextures 打包，要求贴图开 Read/Write），归一到「图集 + Rect[] + 每实例变体下标」。</para>
/// <para>【形态】竖直立牌（yaw 广告牌，可叠风摆）/ 贴地平铺，由 shader keyword _FLATMODE 切换，两形态共用同一底部中心 pivot quad。</para>
/// <para>【地形】高度三模式：固定高度（平地默认）/ 射线采样（有碰撞体的网格地形）/ 高度图地形（FrameWork MeshTerrain 等 GPU 顶点位移地形——射线只能打到位移前的平面，必须改为 CPU 采样高度图复现 shader 位移公式；可自动识别地形材质的 _HeightMap/_HeightScale/_HeightInvert）。</para>
/// <para>【编辑模式预览】ExecuteAlways + SRP beginContextRendering 回调提交绘制，非 Play 状态直接可见；
/// 编辑模式时钟用 EditorApplication.timeSinceStartup（与编辑器下 shader _Time 同源），消散/风摆动画由 editModeLivePreview 强制 SceneView 重绘驱动。</para>
/// <para>【参数实时刷新】Inspector 改动自动生效：结构参数（范围/数量/种子/贴图列表/地形/朝向等，按签名对比判定）全量重建；表现参数（消散/风摆/染色/形态 keyword 等）仅刷材质，避免拖滑条触发重打包。
/// Play 模式改动同样生效（Update 内消费挂起标记）。配套 InspectorFlowerSeaInstanceRenderer 提供条件字段显示与手动刷新按钮。</para>
/// <para>【用法】挂到场景空物体 → 配贴图与范围 → 自动生成预览（generateOnEnable）或手动调 Generate()；
/// 生物走过时由调用方调 TrampleAt(位置, 半径)（或开 pollTargetsEnable 自动轮询目标列表）；ResetSea() 全部复原。</para>
/// <para>【构建注意】shader 开关用 multi_compile（全 8 变体始终进构建，杜绝运行时 EnableKeyword 命中裁剪变体）；
/// 预设材质 Resources/Materials/Mat_FlowerSeaInstancedIndirect_1 作为默认模板优先克隆，找不到时退化为 Shader.Find 新建。</para>
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class FlowerSeaInstanceRenderer : BaseMonoBehaviour
{
    #region 枚举

    /// <summary>花贴图来源模式</summary>
    public enum FlowerSeaTextureMode
    {
        /// <summary>图集：一张贴图按 cols×rows 自动均分（或手动 Rect 列表覆盖）</summary>
        Atlas,
        /// <summary>单图列表：运行时 PackTextures 打包成图集（要求贴图开 Read/Write）</summary>
        SingleList,
    }

    /// <summary>花朵形态</summary>
    public enum FlowerShapeEnum
    {
        /// <summary>竖直立牌：yaw 广告牌朝向镜头，可叠风摆</summary>
        UprightBillboard,
        /// <summary>贴地平铺：躺在地面上</summary>
        Flat,
    }

    /// <summary>高度来源模式</summary>
    public enum FlowerSeaHeightModeEnum
    {
        /// <summary>固定高度：transform.position.y + yOffset</summary>
        FixedY,
        /// <summary>射线采样：按 terrainLayer 向下射线逐朵采样（适合有碰撞体的网格地形）</summary>
        Raycast,
        /// <summary>高度图地形：CPU 采样高度图复现 MeshTerrain shader 的顶点位移（GPU 位移地形射线打不到，必须用此模式）</summary>
        HeightmapTerrain,
    }

    #endregion

    #region 可调参数：花海范围

    [Header("花海范围")]
    /// <summary>花海范围(世界单位 X/Z)，以组件位置为中心</summary>
    public Vector2 rangeSize = new Vector2(20f, 20f);
    /// <summary>花朵总数（严格生效；超过格子数时多轮复用格子，密度过大会重叠，请加大范围或调小缩放）</summary>
    [Range(1, 50000)] public int flowerCount = 3000;
    /// <summary>随机种子（同种子生成结果一致）</summary>
    public int randomSeed = 12345;
    /// <summary>抖动网格行列：按洗牌序分格布点（均匀防扎堆；花朵数超过格子数时多轮复用格子）</summary>
    public Vector2Int gridCells = new Vector2Int(60, 60);
    /// <summary>每朵花的世界缩放随机区间</summary>
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);
    /// <summary>贴地防 z-fight 的 Y 偏移（战斗道路面惯例 0.0001，花默认略高）</summary>
    public float yOffset = 0.001f;
    /// <summary>OnEnable 时自动生成花海；关闭则需调用方手动 Generate()</summary>
    public bool generateOnEnable = true;

    #endregion

    #region 可调参数：贴图

    [Header("花贴图（图集/单图二选一，非当前模式字段自动隐藏）")]
    /// <summary>贴图来源模式</summary>
    public FlowerSeaTextureMode textureMode = FlowerSeaTextureMode.Atlas;
    /// <summary>图集模式：花图集贴图</summary>
    public Texture2D atlasTexture;
    /// <summary>图集模式：自动均分列×行</summary>
    public Vector2Int atlasGrid = new Vector2Int(4, 4);
    /// <summary>图集模式：手动 UV Rect 列表（非空时覆盖自动均分）</summary>
    public List<Rect> manualRects;
    /// <summary>单图模式：独立贴图列表（要求每张开 Read/Write）</summary>
    public List<Texture2D> textureList;
    /// <summary>单图模式：打包图集边长上限</summary>
    public int packAtlasSize = 1024;
    /// <summary>单图模式：打包间距(像素)</summary>
    public int packPadding = 2;

    #endregion

    #region 可调参数：地形高度适配

    [Header("地形高度适配")]
    /// <summary>高度来源：固定高度 / 射线采样 / 高度图地形（GPU 顶点位移地形只能选高度图模式，射线打不到位移后的高度）</summary>
    public FlowerSeaHeightModeEnum heightMode = FlowerSeaHeightModeEnum.FixedY;
    // —— 射线采样模式 ——
    /// <summary>射线模式：地形射线命中的层</summary>
    public LayerMask terrainLayer;
    /// <summary>射线模式：射线起点相对组件位置的抬升高度</summary>
    public float rayStartHeight = 50f;
    /// <summary>射线模式：射线最大距离</summary>
    public float rayMaxDistance = 200f;
    // —— 高度图地形模式 ——
    /// <summary>高度图模式：地形 MeshRenderer（自动从其材质读取 _HeightMap/_HeightScale/_HeightInvert，FrameWork MeshTerrain 约定）</summary>
    public MeshRenderer terrainRenderer;
    /// <summary>高度图模式：手动指定高度图（留空则自动读地形材质；无需开 Read/Write，内部走 GPU 回读）</summary>
    public Texture2D terrainHeightMap;
    /// <summary>高度图模式：起伏高度（仅手动指定高度图时生效）</summary>
    public float terrainHeightScale = 5f;
    /// <summary>高度图模式：高度反转（仅手动指定高度图时生效；开=白为低黑为高）</summary>
    public bool terrainHeightInvert = false;

    #endregion

    #region 可调参数：花朵形态

    [Header("花朵形态")]
    /// <summary>竖直立牌(yaw 广告牌) / 贴地平铺</summary>
    public FlowerShapeEnum shape = FlowerShapeEnum.UprightBillboard;
    /// <summary>每朵花随机朝向（yaw）</summary>
    public bool randomYaw = true;

    #endregion

    #region 可调参数：消散

    [Header("踩踏消散")]
    /// <summary>消散时长(秒)</summary>
    public float dissolveDuration = 0.6f;
    /// <summary>消散噪声图（不赋则自动用内置 8×8 Bayer 抖动矩阵——像素颗粒侵蚀效果；赋 Perlin/云噪图则是团块状消散）</summary>
    public Texture2D dissolveNoise;
    /// <summary>噪声在单朵花 UV 上的平铺密度（越大颗粒越细；花较小时建议 2~6）</summary>
    public float dissolveNoiseScale = 4f;
    /// <summary>开启消散边缘 HDR 色带</summary>
    public bool dissolveEdgeBand = false;
    /// <summary>消散边缘色(HDR)</summary>
    [ColorUsage(true, true)] public Color dissolveEdgeColor = new Color(2f, 1.2f, 0.4f, 1f);
    /// <summary>消散边缘宽度</summary>
    [Range(0f, 0.3f)] public float dissolveEdgeWidth = 0.08f;

    #endregion

    #region 可调参数：风摆

    [Header("风摆（仅竖直模式生效）")]
    /// <summary>开启风摆</summary>
    public bool windEnable = true;
    /// <summary>风速(整体快慢)</summary>
    public float windSpeed = 2.5f;
    /// <summary>摆动幅度</summary>
    [Range(0f, 0.5f)] public float swayStrength = 0.08f;
    /// <summary>摆动频率</summary>
    [Range(0f, 10f)] public float swayFrequency = 2f;
    /// <summary>茎硬度(越大根部越不弯)</summary>
    [Range(1f, 4f)] public float stiffness = 2f;

    #endregion

    #region 可调参数：自动踩踏轮询

    [Header("自动踩踏轮询（可选，默认关；关闭时由调用方手动 TrampleAt）")]
    /// <summary>开启后按 pollInterval 轮询 pollTargets 位置自动踩踏</summary>
    public bool pollTargetsEnable = false;
    /// <summary>轮询目标列表</summary>
    public List<Transform> pollTargets;
    /// <summary>轮询间隔(秒)</summary>
    public float pollInterval = 0.2f;
    /// <summary>轮询踩踏半径</summary>
    public float pollTrampleRadius = 0.5f;

    #endregion

    #region 可调参数：编辑器预览

    [Header("编辑器预览")]
    /// <summary>编辑模式下强制 SceneView 重绘（风摆/消散动画在非 Play 状态也可见；关闭可省 GPU）</summary>
    public bool editModeLivePreview = true;
    /// <summary>编辑模式预览重绘帧率上限（越低越省 GPU，动画流畅度随之下降）</summary>
    [Range(5, 60)] public int editModePreviewFps = 30;

    #endregion

    #region 常量与 Shader 属性 ID

    //配套 Shader 名与预设材质 Resources 路径（预设材质作默认模板；keyword 为 multi_compile 全变体内置，无构建裁剪风险）
    private const string ShaderName = "FrameWork/URP/FlowerSeaInstancedIndirect1";
    private const string PresetMaterialPath = "Materials/Mat_FlowerSeaInstancedIndirect_1";
    //空间哈希网格边长（≥常见踩踏半径即可，过细增加遍历格数、过粗增加单格花数）
    private const float CellSize = 1f;
    //编辑模式拖动组件时的重建节流间隔(秒)
    private const float EditorRegenerateInterval = 0.15f;

    private static readonly int ID_BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ID_Cutoff = Shader.PropertyToID("_Cutoff");
    private static readonly int ID_DissolveNoise = Shader.PropertyToID("_DissolveNoise");
    private static readonly int ID_DissolveNoiseScale = Shader.PropertyToID("_DissolveNoiseScale");
    private static readonly int ID_DissolveDuration = Shader.PropertyToID("_DissolveDuration");
    private static readonly int ID_DissolveEdgeColor = Shader.PropertyToID("_DissolveEdgeColor");
    private static readonly int ID_DissolveEdgeWidth = Shader.PropertyToID("_DissolveEdgeWidth");
    private static readonly int ID_WindSpeed = Shader.PropertyToID("_WindSpeed");
    private static readonly int ID_SwayStrength = Shader.PropertyToID("_SwayStrength");
    private static readonly int ID_SwayFrequency = Shader.PropertyToID("_SwayFrequency");
    private static readonly int ID_Stiffness = Shader.PropertyToID("_Stiffness");
    private static readonly int ID_InstanceData = Shader.PropertyToID("_InstanceData");
    private static readonly int ID_VariantRects = Shader.PropertyToID("_VariantRects");
    private static readonly int ID_DissolveStart = Shader.PropertyToID("_DissolveStart");
    private static readonly int ID_FlowerSeaTime = Shader.PropertyToID("_FlowerSeaTime");
    //高度图地形材质属性（FrameWork MeshTerrain shader 约定名，见 Shader_Mesh_Terrain.shader / TerrainHeight.hlsl）
    private static readonly int ID_TerrainHeightMap = Shader.PropertyToID("_HeightMap");
    private static readonly int ID_TerrainHeightScale = Shader.PropertyToID("_HeightScale");
    private static readonly int ID_TerrainHeightInvert = Shader.PropertyToID("_HeightInvert");

    private const string KeywordFlat = "_FLATMODE";
    private const string KeywordWind = "_WIND_ON";
    private const string KeywordEdge = "_DISSOLVEEDGE_ON";

    #endregion

    #region 内部结构

    /// <summary>每朵花静态实例数据（stride 32B，与 shader 侧 FlowerInstanceData 布局一致）</summary>
    private struct InstanceData
    {
        /// <summary>xyz=世界位置(含地形高度) w=世界缩放</summary>
        public Vector4 posScale;
        /// <summary>x=图集变体下标 y=风摆随机相位 z=yaw 弧度 w=保留</summary>
        public Vector4 params0;
    }

    #endregion

    #region 内部数据

    //渲染资源：quad 静态共享（HideAndDontSave，域重载后懒重建）；材质/图集为组件实例独占
    private static Mesh sharedQuadMesh;
    private Material materialInstance;
    private Texture2D runtimeAtlas;
    private bool hasTriedLoadPresetMat;

    //GPU 缓冲（OnDestroy 幂等释放）
    private ComputeBuffer instanceBuffer;
    private ComputeBuffer variantRectBuffer;
    private ComputeBuffer dissolveBuffer;
    private ComputeBuffer argsBuffer;

    //CPU 侧数据镜像
    private InstanceData[] instanceDataArray;
    private float[] dissolveStartTimes;
    private Rect[] variantRects;
    private Texture2D finalTexture;

    //XZ 空间哈希（key = (long)cx<<32 | (uint)cz）
    private readonly Dictionary<long, List<int>> spatialGrid = new Dictionary<long, List<int>>();

    //高度图地形数据（Generate 时 TryPrepareTerrain 填充）
    private Mesh terrainMesh;
    private float[] heightmapHeights;
    private int heightmapW;
    private int heightmapH;
    private float heightmapScale;
    private bool heightmapInvert;
    private bool hasHeightmap;

    private bool dissolveDirty;
    private bool isReady;
    private bool hasLoggedError;
    private int currentCount;
    private int dissolvedCount;
    private Bounds cachedBounds;
    private float pollTimer;

    //参数实时刷新：结构签名对比 + 挂起标记（Play 走 Update 消费，编辑模式走 EditorApplication.delayCall 消费）
    private string lastStructuralSignature = "";
    private bool pendingRegenerate;
    private bool pendingMaterialRefresh;
#if UNITY_EDITOR
    private double lastEditorRegenerateTime;
    private double lastPreviewRepaintTime;
#endif

    #endregion

    #region 生命周期

    /// <summary>OnEnable：建材质、注册渲染回调（编辑/Play 双模式），按 generateOnEnable 决定是否自动生成</summary>
    private void OnEnable()
    {
        EnsureMaterial();
        RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
#if UNITY_EDITOR
        EditorApplication.update += EditorUpdate;
#endif
        if (generateOnEnable)
            Generate();
    }

    /// <summary>OnDisable：注销渲染/编辑器回调（缓冲保留，重新 Enable 可继续绘制）</summary>
    private void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
    }

    /// <summary>Update（仅 Play）：消费 Inspector 改动挂起标记 + 自动踩踏轮询；绘制在渲染回调里</summary>
    private void Update()
    {
        if (!Application.isPlaying) return;
        ConsumePendingRefresh();
        UpdatePollTargets();
    }

    /// <summary>OnDestroy：幂等释放 GPU 缓冲 + 销毁独占材质与运行时图集（共享 quad 不销毁）</summary>
    private void OnDestroy()
    {
        ReleaseBuffers();
        DestroySmart(materialInstance); materialInstance = null;
        DestroySmart(runtimeAtlas); runtimeAtlas = null;
    }

    /// <summary>Inspector 改动：参数夹取 + 触发自动实时刷新</summary>
    private void OnValidate()
    {
        gridCells.x = Mathf.Max(1, gridCells.x);
        gridCells.y = Mathf.Max(1, gridCells.y);
        atlasGrid.x = Mathf.Max(1, atlasGrid.x);
        atlasGrid.y = Mathf.Max(1, atlasGrid.y);
        packAtlasSize = Mathf.Max(64, packAtlasSize);
        if (scaleRange.x > scaleRange.y) scaleRange = new Vector2(scaleRange.y, scaleRange.x);
        pollInterval = Mathf.Max(0.02f, pollInterval);
        dissolveDuration = Mathf.Max(0.01f, dissolveDuration);
        RequestAutoRefresh();
    }

    #endregion

    #region 对外接口

    /// <summary>当前花朵总数（未 Generate 返回 0）</summary>
    public int FlowerCount => isReady ? currentCount : 0;

    /// <summary>当前已消散（含消散中）的花朵数，TrampleAt 命中即累计，ResetSea/Generate 归零</summary>
    public int DissolvedCount => isReady ? dissolvedCount : 0;

    /// <summary>
    /// 重新生成花海：准备贴图 → 采样布点 → 重建缓冲 → 重置全部消散状态。全量重建，结构参数改动后调用生效（编辑/Play 双模式可用）。
    /// </summary>
    public void Generate()
    {
        hasLoggedError = false;
        isReady = false;
        pendingRegenerate = false;
        pendingMaterialRefresh = false;
        EnsureMaterial();
        if (materialInstance == null) { LogErrorOnce("Shader 缺失：" + ShaderName); return; }
        if (flowerCount <= 0) { LogErrorOnce("flowerCount 需 > 0"); return; }
        if (!TryPrepareTexture()) return;
        if (!TryPrepareTerrain()) return;

        SamplePositions();
        if (currentCount <= 0) { LogErrorOnce("有效花朵数为 0（检查 gridCells 与 flowerCount）"); return; }
        dissolvedCount = 0;
        BuildSpatialGrid();
        CreateBuffers();
        PushMaterialProperties();
        lastStructuralSignature = BuildStructuralSignature();
        isReady = true;
    }

    /// <summary>重置全部消散状态（不重新布点）</summary>
    public void ResetSea()
    {
        if (!isReady) return;
        for (int i = 0; i < dissolveStartTimes.Length; i++)
            dissolveStartTimes[i] = -1f;
        dissolvedCount = 0;
        dissolveDirty = true;
    }

    /// <summary>
    /// 踩踏消散入口：worldPos 半径 radius（XZ 平面）内未消散的花写入消散开始时间（编辑/Play 双模式可用）。
    /// </summary>
    /// <param name="worldPos">踩踏中心世界坐标（Y 忽略）</param>
    /// <param name="radius">踩踏半径(世界单位)</param>
    public void TrampleAt(Vector3 worldPos, float radius)
    {
        if (!isReady) return;
        float sqrRadius = radius * radius;
        float now = GetShaderTime();
        int minCX = (int)Mathf.Floor((worldPos.x - radius) / CellSize);
        int maxCX = (int)Mathf.Floor((worldPos.x + radius) / CellSize);
        int minCZ = (int)Mathf.Floor((worldPos.z - radius) / CellSize);
        int maxCZ = (int)Mathf.Floor((worldPos.z + radius) / CellSize);
        for (int cx = minCX; cx <= maxCX; cx++)
        {
            for (int cz = minCZ; cz <= maxCZ; cz++)
            {
                if (!spatialGrid.TryGetValue(PackCellKey(cx, cz), out List<int> list)) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    int idx = list[i];
                    if (dissolveStartTimes[idx] >= 0) continue; // 已消散/消散中跳过
                    Vector4 posScale = instanceDataArray[idx].posScale;
                    float dx = posScale.x - worldPos.x;
                    float dz = posScale.z - worldPos.z;
                    if (dx * dx + dz * dz <= sqrRadius)
                    {
                        dissolveStartTimes[idx] = now;
                        dissolvedCount++;
                        dissolveDirty = true;
                    }
                }
            }
        }
    }

    /// <summary>指定下标的花是否已开始消散</summary>
    public bool IsDissolved(int index)
    {
        if (!isReady || index < 0 || index >= currentCount) return false;
        return dissolveStartTimes[index] >= 0;
    }

    #endregion

    #region 内部实现：渲染

    /// <summary>SRP 渲染回调（编辑/Play 双模式，每个渲染上下文一次）：脏则上传消散 buffer → 全场一次 Indirect 绘制</summary>
    private void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        if (!isReady) return;
        bool hasValidCamera = false;
        for (int i = 0; i < cameras.Count; i++)
        {
            if (cameras[i] != null && cameras[i].cameraType != CameraType.Preview)
            {
                hasValidCamera = true;
                break;
            }
        }
        if (!hasValidCamera) return; // 跳过材质预览等预览相机
        // 每渲染帧推送统一时钟：与 TrampleAt 盖章同源（编辑模式=编辑器时钟，Play=Time.time），shader 不依赖内置 _Time
        materialInstance.SetFloat(ID_FlowerSeaTime, GetShaderTime());
        if (dissolveDirty)
        {
            dissolveBuffer.SetData(dissolveStartTimes);
            dissolveDirty = false;
        }
        Graphics.DrawMeshInstancedIndirect(sharedQuadMesh, 0, materialInstance, cachedBounds, argsBuffer);
    }

    /// <summary>确保材质存在：优先克隆 Resources 预设材质模板，失败退化 Shader.Find 新建</summary>
    private void EnsureMaterial()
    {
        if (materialInstance != null) return;
        if (!hasTriedLoadPresetMat)
        {
            hasTriedLoadPresetMat = true;
            Material preset = Resources.Load<Material>(PresetMaterialPath);
            if (preset != null)
                materialInstance = new Material(preset);
        }
        if (materialInstance == null)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader != null) materialInstance = new Material(shader);
        }
        if (materialInstance != null)
            materialInstance.enableInstancing = true;
    }

    /// <summary>创建/重建全部 GPU 缓冲并上传数据（含 args 与消散重置）</summary>
    private void CreateBuffers()
    {
        ReleaseBuffers();
        EnsureQuadMesh();

        instanceBuffer = new ComputeBuffer(currentCount, 8 * sizeof(float));
        instanceBuffer.SetData(instanceDataArray);
        variantRectBuffer = new ComputeBuffer(variantRects.Length, 4 * sizeof(float));
        variantRectBuffer.SetData(variantRects);
        dissolveBuffer = new ComputeBuffer(currentCount, sizeof(float));
        dissolveBuffer.SetData(dissolveStartTimes);
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5]
        {
            sharedQuadMesh.GetIndexCount(0), (uint)currentCount,
            sharedQuadMesh.GetIndexStart(0), sharedQuadMesh.GetBaseVertex(0), 0,
        };
        argsBuffer.SetData(args);
        dissolveDirty = false;

        materialInstance.SetBuffer(ID_InstanceData, instanceBuffer);
        materialInstance.SetBuffer(ID_VariantRects, variantRectBuffer);
        materialInstance.SetBuffer(ID_DissolveStart, dissolveBuffer);
    }

    /// <summary>幂等释放全部 GPU 缓冲</summary>
    private void ReleaseBuffers()
    {
        if (instanceBuffer != null) { instanceBuffer.Release(); instanceBuffer = null; }
        if (variantRectBuffer != null) { variantRectBuffer.Release(); variantRectBuffer = null; }
        if (dissolveBuffer != null) { dissolveBuffer.Release(); dissolveBuffer = null; }
        if (argsBuffer != null) { argsBuffer.Release(); argsBuffer = null; }
    }

    /// <summary>推送材质属性与 keyword（_FLATMODE/_WIND_ON/_DISSOLVEEDGE_ON）——表现参数实时刷新的唯一出口</summary>
    private void PushMaterialProperties()
    {
        materialInstance.SetTexture(ID_BaseMap, finalTexture);
        materialInstance.SetColor(ID_BaseColor, Color.white);
        materialInstance.SetFloat(ID_Cutoff, 0.1f);
        // 消散噪声：未赋值时用内置 Bayer 抖动矩阵兜底（引擎默认灰图会导致整朵齐消失，无渐变动画）
        materialInstance.SetTexture(ID_DissolveNoise, dissolveNoise != null ? dissolveNoise : GetDefaultDissolveNoise());
        materialInstance.SetFloat(ID_DissolveNoiseScale, dissolveNoiseScale);
        materialInstance.SetFloat(ID_DissolveDuration, dissolveDuration);
        materialInstance.SetColor(ID_DissolveEdgeColor, dissolveEdgeColor);
        materialInstance.SetFloat(ID_DissolveEdgeWidth, dissolveEdgeWidth);
        materialInstance.SetFloat(ID_WindSpeed, windSpeed);
        materialInstance.SetFloat(ID_SwayStrength, swayStrength);
        materialInstance.SetFloat(ID_SwayFrequency, swayFrequency);
        materialInstance.SetFloat(ID_Stiffness, stiffness);
        SetKeyword(KeywordFlat, shape == FlowerShapeEnum.Flat);
        SetKeyword(KeywordWind, windEnable && shape == FlowerShapeEnum.UprightBillboard);
        SetKeyword(KeywordEdge, dissolveEdgeBand);
    }

    /// <summary>材质 keyword 开关</summary>
    private void SetKeyword(string keyword, bool enable)
    {
        if (enable) materialInstance.EnableKeyword(keyword);
        else materialInstance.DisableKeyword(keyword);
    }

    /// <summary>懒加载静态共享 quad（底部中心 pivot：x∈[-0.5,0.5] y∈[0,1]；法线不参与光照故不写）</summary>
    private void EnsureQuadMesh()
    {
        if (sharedQuadMesh != null) return;
        sharedQuadMesh = new Mesh { name = "FlowerSea_Quad", hideFlags = HideFlags.HideAndDontSave };
        sharedQuadMesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f),
            new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f),
        };
        sharedQuadMesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
        };
        sharedQuadMesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
        sharedQuadMesh.UploadMeshData(false);
    }

    /// <summary>编辑/Play 双模式安全销毁</summary>
    private static void DestroySmart(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    /// <summary>当前时钟（与 shader _Time 同源）：编辑模式用编辑器启动时间，Play 模式用游戏时间</summary>
    private float GetShaderTime()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return (float)EditorApplication.timeSinceStartup;
#endif
        return Time.time;
    }

    #endregion

    #region 内部实现：生成

    /// <summary>准备贴图：两种模式归一到「finalTexture + variantRects」</summary>
    private bool TryPrepareTexture()
    {
        if (textureMode == FlowerSeaTextureMode.Atlas)
        {
            if (atlasTexture == null) { LogErrorOnce("图集模式未赋 atlasTexture"); return false; }
            finalTexture = atlasTexture;
            variantRects = (manualRects != null && manualRects.Count > 0)
                ? manualRects.ToArray()
                : BuildAutoSliceRects(atlasGrid.x, atlasGrid.y);
            return variantRects.Length > 0;
        }
        // 单图列表模式：预检可读性后运行时打包
        if (textureList == null || textureList.Count == 0) { LogErrorOnce("单图模式 textureList 为空"); return false; }
        string badNames = "";
        for (int i = 0; i < textureList.Count; i++)
        {
            if (textureList[i] == null) { badNames += $"[空:{i}] "; continue; }
            if (!textureList[i].isReadable) badNames += textureList[i].name + " ";
        }
        if (badNames.Length > 0) { LogErrorOnce("以下贴图未开 Read/Write 或为 null：" + badNames); return false; }
        DestroySmart(runtimeAtlas); runtimeAtlas = null;
        try
        {
            runtimeAtlas = new Texture2D(packAtlasSize, packAtlasSize, TextureFormat.RGBA32, false);
            variantRects = runtimeAtlas.PackTextures(textureList.ToArray(), packPadding, packAtlasSize);
            runtimeAtlas.filterMode = FilterMode.Point;
            runtimeAtlas.Apply(false, false);
        }
        catch (System.Exception e)
        {
            LogErrorOnce("PackTextures 失败：" + e.Message);
            return false;
        }
        if (variantRects == null || variantRects.Length == 0) { LogErrorOnce("PackTextures 失败：图集边长不足，调大 packAtlasSize"); return false; }
        finalTexture = runtimeAtlas;
        return true;
    }

    /// <summary>图集自动均分切片（UV 空间，行优先从左下角起）</summary>
    private Rect[] BuildAutoSliceRects(int cols, int rows)
    {
        Rect[] rects = new Rect[cols * rows];
        float w = 1f / cols;
        float h = 1f / rows;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                rects[r * cols + c] = new Rect(c * w, r * h, w, h);
        return rects;
    }

    /// <summary>采样布点：抖动网格（格子下标洗牌后按序取格，花朵数超过格子数时多轮复用、每朵仍独立随机抖动）+ 地形射线高度 + 随机缩放/相位/yaw/变体</summary>
    private void SamplePositions()
    {
        System.Random rng = new System.Random(randomSeed);
        int cellTotal = gridCells.x * gridCells.y;
        currentCount = flowerCount; // 数量严格等于 flowerCount（格子不够时复用格子，不再截断）

        // 格子下标洗牌：花朵按洗牌序取格，保证优先均匀铺满全场不扎堆；超过格子数则取模多轮复用
        List<int> cellIndices = new List<int>(cellTotal);
        for (int i = 0; i < cellTotal; i++) cellIndices.Add(i);
        for (int i = cellTotal - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cellIndices[i], cellIndices[j]) = (cellIndices[j], cellIndices[i]);
        }

        Vector3 center = transform.position;
        float originX = center.x - rangeSize.x * 0.5f;
        float originZ = center.z - rangeSize.y * 0.5f;
        float cellW = rangeSize.x / gridCells.x;
        float cellH = rangeSize.y / gridCells.y;
        int variantCount = variantRects.Length;

        instanceDataArray = new InstanceData[currentCount];
        dissolveStartTimes = new float[currentCount];
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int i = 0; i < currentCount; i++)
        {
            int cell = cellIndices[i % cellTotal]; // 超过格子数时多轮复用格子（每朵仍独立随机抖动，均匀度优于纯随机）
            int cx = cell % gridCells.x;
            int cz = cell / gridCells.x;
            float x = originX + (cx + (float)rng.NextDouble()) * cellW;
            float z = originZ + (cz + (float)rng.NextDouble()) * cellH;
            float y = SampleGroundY(x, z);

            float scale = scaleRange.x + (scaleRange.y - scaleRange.x) * (float)rng.NextDouble();
            float phase = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float yaw = randomYaw ? (float)(rng.NextDouble() * Mathf.PI * 2.0) : 0f;
            float variant = rng.Next(variantCount);

            instanceDataArray[i] = new InstanceData
            {
                posScale = new Vector4(x, y, z, scale),
                params0 = new Vector4(variant, phase, yaw, 0f),
            };
            dissolveStartTimes[i] = -1f;
            min = Vector3.Min(min, new Vector3(x, y, z));
            max = Vector3.Max(max, new Vector3(x, y, z));
        }

        // 视锥剔除包围盒：实例数据 min/max 外扩（最大缩放 + 风摆/高度余量）
        float margin = scaleRange.y + 1.5f;
        cachedBounds = new Bounds((min + max) * 0.5f, (max - min) + Vector3.one * margin * 2f);
    }

    /// <summary>采样单点地面高度：按 heightMode 走射线/高度图，未命中或关闭时恒为 transform.position.y + yOffset</summary>
    private float SampleGroundY(float x, float z)
    {
        if (heightMode == FlowerSeaHeightModeEnum.Raycast && terrainLayer.value != 0)
        {
            // 走组件所在 scene 的物理场景（预制体模式也能命中自身场景网格）
            Vector3 origin = new Vector3(x, transform.position.y + rayStartHeight, z);
            if (gameObject.scene.GetPhysicsScene().Raycast(origin, Vector3.down, out RaycastHit hit, rayMaxDistance, terrainLayer))
                return hit.point.y + yOffset;
        }
        else if (heightMode == FlowerSeaHeightModeEnum.HeightmapTerrain && hasHeightmap)
        {
            // 世界XZ → 地形物体空间 → 网格UV(0-1)，采样高度图复现 TerrainHeight.hlsl 的 positionOS.y += h * heightScale 位移公式
            Transform terrainTF = terrainRenderer.transform;
            Vector3 local = terrainTF.InverseTransformPoint(new Vector3(x, terrainTF.position.y, z));
            Bounds bounds = terrainMesh.bounds;
            float u = (local.x - bounds.min.x) / bounds.size.x;
            float v = (local.z - bounds.min.z) / bounds.size.z;
            if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
            {
                float h = SampleHeightmap01(u, v);
                if (heightmapInvert) h = 1f - h;
                local.y += h * heightmapScale;
                return terrainTF.TransformPoint(local).y + yOffset;
            }
        }
        return transform.position.y + yOffset;
    }

    /// <summary>高度图模式准备：解析地形网格 + 高度图（自动识别 MeshTerrain 材质属性）并 GPU 回读到 CPU 数组</summary>
    private bool TryPrepareTerrain()
    {
        hasHeightmap = false;
        if (heightMode != FlowerSeaHeightModeEnum.HeightmapTerrain) return true;
        if (terrainRenderer == null) { LogErrorOnce("高度图模式未赋 terrainRenderer（地形的 MeshRenderer）"); return false; }
        MeshFilter meshFilter = terrainRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) { LogErrorOnce("terrainRenderer 上没有 MeshFilter/网格"); return false; }
        terrainMesh = meshFilter.sharedMesh;

        // 高度图与参数：手动指定优先，留空则自动读地形材质（FrameWork MeshTerrain 约定的 _HeightMap/_HeightScale/_HeightInvert）
        Texture2D map = terrainHeightMap;
        heightmapScale = terrainHeightScale;
        heightmapInvert = terrainHeightInvert;
        if (map == null)
        {
            Material terrainMat = terrainRenderer.sharedMaterial;
            if (terrainMat == null || !terrainMat.HasProperty(ID_TerrainHeightMap) || !terrainMat.HasProperty(ID_TerrainHeightScale))
            {
                LogErrorOnce("高度图模式：未手动指定高度图，且地形材质不含 _HeightMap/_HeightScale（非 MeshTerrain 约定，请手动指定）");
                return false;
            }
            map = terrainMat.GetTexture(ID_TerrainHeightMap) as Texture2D;
            heightmapScale = terrainMat.GetFloat(ID_TerrainHeightScale);
            heightmapInvert = terrainMat.HasProperty(ID_TerrainHeightInvert) && terrainMat.GetFloat(ID_TerrainHeightInvert) > 0.5f;
        }
        if (map == null) { LogErrorOnce("高度图模式：高度图为空（地形材质 _HeightMap 未赋值）"); return false; }

        CacheHeightmapPixels(map);
        hasHeightmap = true;
        return true;
    }

    /// <summary>GPU 回读高度图到 CPU float 数组：Blit 到线性临时 RT 再 ReadPixels，规避贴图未开 Read/Write 的限制</summary>
    private void CacheHeightmapPixels(Texture2D map)
    {
        heightmapW = map.width;
        heightmapH = map.height;
        RenderTexture rt = RenderTexture.GetTemporary(heightmapW, heightmapH, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(map, rt);
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tmp = new Texture2D(heightmapW, heightmapH, TextureFormat.RGBA32, false);
        tmp.ReadPixels(new Rect(0, 0, heightmapW, heightmapH), 0, 0);
        tmp.Apply();
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);
        Color32[] pixels = tmp.GetPixels32();
        DestroySmart(tmp);
        heightmapHeights = new float[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            heightmapHeights[i] = pixels[i].r / 255f; // 高度图只看 R 通道（与 shader 一致）
    }

    /// <summary>高度图双线性采样（UV 0-1，与 shader 的 bilinear 采样对齐）</summary>
    private float SampleHeightmap01(float u, float v)
    {
        float x = Mathf.Clamp01(u) * (heightmapW - 1);
        float y = Mathf.Clamp01(v) * (heightmapH - 1);
        int x0 = (int)x, y0 = (int)y;
        int x1 = Mathf.Min(x0 + 1, heightmapW - 1), y1 = Mathf.Min(y0 + 1, heightmapH - 1);
        float fx = x - x0, fy = y - y0;
        float h00 = heightmapHeights[y0 * heightmapW + x0];
        float h10 = heightmapHeights[y0 * heightmapW + x1];
        float h01 = heightmapHeights[y1 * heightmapW + x0];
        float h11 = heightmapHeights[y1 * heightmapW + x1];
        return Mathf.Lerp(Mathf.Lerp(h00, h10, fx), Mathf.Lerp(h01, h11, fx), fy);
    }

    /// <summary>按花位置建 XZ 空间哈希（TrampleAt 查询用）</summary>
    private void BuildSpatialGrid()
    {
        spatialGrid.Clear();
        for (int i = 0; i < currentCount; i++)
        {
            Vector4 posScale = instanceDataArray[i].posScale;
            int cx = (int)Mathf.Floor(posScale.x / CellSize);
            int cz = (int)Mathf.Floor(posScale.z / CellSize);
            long key = PackCellKey(cx, cz);
            if (!spatialGrid.TryGetValue(key, out List<int> list))
            {
                list = new List<int>(4);
                spatialGrid.Add(key, list);
            }
            list.Add(i);
        }
    }

    /// <summary>格子坐标打包 key（支持负坐标）</summary>
    private static long PackCellKey(int cx, int cz)
    {
        return ((long)cx << 32) | (uint)cz;
    }

    /// <summary>错误只报一次（Generate 入口会重置标记，允许重试时再报）</summary>
    private void LogErrorOnce(string msg)
    {
        if (hasLoggedError) return;
        hasLoggedError = true;
        Debug.LogError("[FlowerSeaInstanceRenderer] " + msg, this);
    }

    //默认消散噪声：8×8 Bayer 有序抖动矩阵（行优先），消散时按阈值顺序逐格侵蚀，经典像素颗粒感
    private static readonly byte[] Bayer8x8 =
    {
         0, 32,  8, 40,  2, 34, 10, 42,
        48, 16, 56, 24, 50, 18, 58, 26,
        12, 44,  4, 36, 14, 46,  6, 38,
        60, 28, 52, 20, 62, 30, 54, 22,
         3, 35, 11, 43,  1, 33,  9, 41,
        51, 19, 59, 27, 49, 17, 57, 25,
        15, 47,  7, 39, 13, 45,  5, 37,
        63, 31, 55, 23, 61, 29, 53, 21,
    };
    private static Texture2D sharedDefaultNoise;

    /// <summary>懒加载内置默认消散噪声图（8×8 Bayer，Point/Repeat；静态共享，域重载后重建）</summary>
    private static Texture2D GetDefaultDissolveNoise()
    {
        if (sharedDefaultNoise != null) return sharedDefaultNoise;
        sharedDefaultNoise = new Texture2D(8, 8, TextureFormat.RGBA32, false)
        {
            name = "FlowerSea_DefaultBayerNoise",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
        };
        // 格值归一化到 (0,1) 区间中点，避免 0/1 边界值导致消散首末时刻表现异常
        Color32[] pixels = new Color32[64];
        for (int i = 0; i < 64; i++)
        {
            byte v = (byte)Mathf.RoundToInt((Bayer8x8[i] + 0.5f) / 64f * 255f);
            pixels[i] = new Color32(v, v, v, 255);
        }
        sharedDefaultNoise.SetPixels32(pixels);
        sharedDefaultNoise.Apply(false, false);
        return sharedDefaultNoise;
    }

    #endregion

    #region 内部实现：参数实时刷新

    /// <summary>
    /// 结构参数签名：范围/数量/种子/网格/缩放/偏移/贴图来源与列表/地形/朝向等影响实例数据烘焙的参数；
    /// 消散/风摆/形态 keyword 等表现参数不入签名（只刷材质即可）
    /// </summary>
    private string BuildStructuralSignature()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
        sb.Append(textureMode).Append(atlasTexture != null ? atlasTexture.GetInstanceID() : 0).Append(atlasGrid);
        if (manualRects != null)
            for (int i = 0; i < manualRects.Count; i++) sb.Append(manualRects[i]);
        if (textureList != null)
            for (int i = 0; i < textureList.Count; i++) sb.Append(textureList[i] != null ? textureList[i].GetInstanceID() : 0).Append(';');
        sb.Append(packAtlasSize).Append(packPadding);
        sb.Append(rangeSize).Append(flowerCount).Append(randomSeed).Append(gridCells).Append(scaleRange).Append(yOffset);
        sb.Append(heightMode).Append(terrainLayer.value).Append(rayStartHeight).Append(rayMaxDistance)
          .Append(terrainRenderer != null ? terrainRenderer.GetInstanceID() : 0)
          .Append(terrainHeightMap != null ? terrainHeightMap.GetInstanceID() : 0)
          .Append(terrainHeightScale).Append(terrainHeightInvert)
          .Append(randomYaw);
        return sb.ToString();
    }

    /// <summary>Inspector 改动后的自动刷新：结构参数全量重建、表现参数仅刷材质；未生成过不自动刷</summary>
    private void RequestAutoRefresh()
    {
        if (!isReady) return;
        bool structural = BuildStructuralSignature() != lastStructuralSignature;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 编辑模式：delayCall 延迟到 OnValidate 外执行（OnValidate 内禁止建 GPU 资源），标记位合并同一帧多次改动
            if (structural)
            {
                if (pendingRegenerate) return;
                pendingRegenerate = true;
                EditorApplication.delayCall += ConsumePendingRegenerateInEditor;
            }
            else if (!pendingMaterialRefresh)
            {
                pendingMaterialRefresh = true;
                EditorApplication.delayCall += ConsumePendingMaterialRefreshInEditor;
            }
            return;
        }
#endif
        if (structural) pendingRegenerate = true;
        else pendingMaterialRefresh = true;
    }

    /// <summary>Play 模式 Update 内消费挂起标记</summary>
    private void ConsumePendingRefresh()
    {
        if (pendingRegenerate)
        {
            pendingRegenerate = false;
            Generate();
        }
        else if (pendingMaterialRefresh)
        {
            pendingMaterialRefresh = false;
            if (isReady) PushMaterialProperties();
        }
    }

#if UNITY_EDITOR
    /// <summary>编辑模式延迟消费：全量重建（this 判空防延迟回调时组件已销毁）</summary>
    private void ConsumePendingRegenerateInEditor()
    {
        if (this == null) { pendingRegenerate = false; return; }
        pendingRegenerate = false;
        Generate();
    }

    /// <summary>编辑模式延迟消费：仅刷材质</summary>
    private void ConsumePendingMaterialRefreshInEditor()
    {
        if (this == null) { pendingMaterialRefresh = false; return; }
        pendingMaterialRefresh = false;
        if (isReady) PushMaterialProperties();
    }

    /// <summary>编辑器帧回调（仅编辑模式）：组件被拖动时节流重建 + 实时预览强制 SceneView 重绘</summary>
    private void EditorUpdate()
    {
        if (Application.isPlaying || this == null || !isReady) return;
        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            if (EditorApplication.timeSinceStartup - lastEditorRegenerateTime > EditorRegenerateInterval)
            {
                lastEditorRegenerateTime = EditorApplication.timeSinceStartup;
                Generate();
            }
        }
        // 实时预览按帧率上限节流重绘（编辑模式满帧率重绘会让显卡空转）
        if (editModeLivePreview && EditorApplication.timeSinceStartup - lastPreviewRepaintTime >= 1.0 / editModePreviewFps)
        {
            lastPreviewRepaintTime = EditorApplication.timeSinceStartup;
            SceneView.RepaintAll();
        }
    }
#endif

    #endregion

    #region 内部实现：自动踩踏轮询

    /// <summary>按 pollInterval 轮询目标位置自动 TrampleAt（默认关闭，手动触发时无开销）</summary>
    private void UpdatePollTargets()
    {
        if (!isReady || !pollTargetsEnable || pollTargets == null || pollTargets.Count == 0) return;
        pollTimer += Time.deltaTime;
        if (pollTimer < pollInterval) return;
        pollTimer = 0f;
        for (int i = 0; i < pollTargets.Count; i++)
        {
            if (pollTargets[i] != null)
                TrampleAt(pollTargets[i].position, pollTrampleRadius);
        }
    }

    #endregion

    #region 编辑器便利

    /// <summary>Scene 视图画出花海范围盒（选中时）</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(rangeSize.x, 1f, rangeSize.y));
    }

    [ContextMenu("重新生成花海")]
    private void ContextRegenerate() => Generate();

    [ContextMenu("重置全部消散")]
    private void ContextResetSea() => ResetSea();

    [ContextMenu("测试踩踏中心(r=2)")]
    private void ContextTrampleCenter() => TrampleAt(transform.position, 2f);

    #endregion
}
