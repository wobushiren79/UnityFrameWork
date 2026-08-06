using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 网格快照残影：适用于「渲染走 MeshFilter + MeshRenderer、且当前显示几何已烘焙进 CPU 侧 Mesh」的对象，
/// 例如 Spine SkeletonAnimation（每帧 CPU 烘焙网格）、静态网格、程序化生成网格。
///
/// 做法：对当前帧 MeshFilter.sharedMesh 做快照（拷进各池对象持久复用的 Mesh，避开双缓冲覆盖），
/// 共享源材质数组（不克隆），淡出用 MaterialPropertyBlock 覆盖 _Color（PMA 预乘时整体乘，免克隆免泄漏、不影响本体）。
///
/// 不适用：SkinnedMeshRenderer（sharedMesh 是 bind pose，请用 <see cref="AfterimageGhostSkinnedMesh"/>）；
/// 形变靠顶点着色器/GPU 的对象（快照冻结不住 shader 动画，需渲染纹理捕获，见基类说明）。
/// </summary>
public class AfterimageGhostMesh : AfterimageGhostBase
{
    #region 绑定源

    /// <summary>本体 MeshRenderer（材质/排序来源）</summary>
    protected MeshRenderer srcRenderer;
    /// <summary>本体 MeshFilter（快照网格来源，每帧被双缓冲覆盖，须逐次拷贝）</summary>
    protected MeshFilter srcFilter;
    /// <summary>本体材质数组(缓存,残影共享,不克隆)</summary>
    protected Material[] srcMaterials;

    /// <summary>_Color 属性 id(缓存)</summary>
    protected static readonly int ColorPropId = Shader.PropertyToID("_Color");
    /// <summary>复用的材质属性块(逐残影覆盖 _Color)</summary>
    protected MaterialPropertyBlock mpb;

    /// <summary>网格残影运行时数据</summary>
    protected class MeshGhostItem : GhostItem
    {
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public Mesh mesh;   //持久复用的快照网格
    }

    #endregion

    #region 绑定接口

    /// <summary>
    /// 绑定源渲染器（MeshRenderer + MeshFilter）
    /// </summary>
    /// <param name="renderer">源 MeshRenderer</param>
    /// <param name="filter">源 MeshFilter</param>
    public void Init(MeshRenderer renderer, MeshFilter filter)
    {
        srcRenderer = renderer;
        srcFilter = filter;
        srcMaterials = renderer != null ? renderer.sharedMaterials : null;
        if (mpb == null)
        {
            mpb = new MaterialPropertyBlock();
        }
    }

    /// <summary>
    /// 便捷绑定：直接从物体上取 MeshRenderer + MeshFilter（如 Spine 的 skeletonAnimation.gameObject）
    /// </summary>
    /// <param name="source">带 MeshRenderer+MeshFilter 的物体</param>
    public void Init(GameObject source)
    {
        if (source == null)
        {
            return;
        }
        Init(source.GetComponent<MeshRenderer>(), source.GetComponent<MeshFilter>());
    }

    #endregion

    #region 基类实现

    /// <summary>源就绪判定：渲染器/过滤器/当前网格齐备</summary>
    protected override bool CanCapture()
    {
        return srcRenderer != null && srcFilter != null && srcFilter.sharedMesh != null;
    }

    /// <summary>新建网格残影对象(共享材质、关阴影、自带一份可复用空网格)</summary>
    protected override GhostItem CreateGhost()
    {
        var go = new GameObject("AfterimageGhost_Mesh");
        var filter = go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = srcMaterials;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        var mesh = new Mesh();
        mesh.MarkDynamic();
        filter.sharedMesh = mesh;
        return new MeshGhostItem { go = go, meshRenderer = meshRenderer, meshFilter = filter, mesh = mesh, age = 0 };
    }

    /// <summary>快照当前帧网格 + 位置/旋转/缩放 + 排序(压身后一层)</summary>
    protected override void CaptureInto(GhostItem item)
    {
        var g = (MeshGhostItem)item;
        CopyMesh(srcFilter.sharedMesh, g.mesh);
        var srcTF = srcRenderer.transform;
        //lossyScale 带上翻转后的负X,否则朝向会反
        g.go.transform.SetPositionAndRotation(srcTF.position, srcTF.rotation);
        g.go.transform.localScale = srcTF.lossyScale;
        g.meshRenderer.sortingLayerID = srcRenderer.sortingLayerID;
        g.meshRenderer.sortingOrder = srcRenderer.sortingOrder - 1;
    }

    /// <summary>MPB 覆盖 _Color 淡出(整体乘,不改共享材质、不影响本体)</summary>
    protected override void ApplyFade(GhostItem item, float t)
    {
        var g = (MeshGhostItem)item;
        mpb.SetColor(ColorPropId, ghostTint * t);
        g.meshRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>销毁网格残影(连带克隆的 Mesh)</summary>
    protected override void DestroyGhost(GhostItem item)
    {
        var g = (MeshGhostItem)item;
        if (g.mesh != null) Destroy(g.mesh);
        if (g.go != null) Destroy(g.go);
    }

    #endregion

    #region 工具

    /// <summary>
    /// 把源网格逐次拷贝到目标网格(复用目标 Mesh、保留子网格，避免重新分配 Mesh)
    /// </summary>
    /// <param name="src">源网格(本体当前帧)</param>
    /// <param name="dst">目标网格(残影持久复用)</param>
    protected static void CopyMesh(Mesh src, Mesh dst)
    {
        dst.Clear();
        dst.vertices = src.vertices;
        dst.uv = src.uv;
        dst.colors32 = src.colors32;
        dst.subMeshCount = src.subMeshCount;
        for (int i = 0; i < src.subMeshCount; i++)
        {
            dst.SetTriangles(src.GetTriangles(i), i);
        }
        dst.bounds = src.bounds;
    }

    #endregion
}
