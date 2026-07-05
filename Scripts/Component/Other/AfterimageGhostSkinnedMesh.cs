using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 蒙皮网格快照残影：适用于 SkinnedMeshRenderer（3D 骨骼角色，含 BlendShape）。
///
/// 关键区别：SkinnedMeshRenderer.sharedMesh 是绑定姿势(bind pose)，真正的蒙皮形变在 GPU 上完成，
/// 直接拷 sharedMesh 只会得到 T-pose。必须用 <c>SkinnedMeshRenderer.BakeMesh</c> 把当前 GPU 蒙皮结果读回 CPU，
/// 才是当前这一帧的动作姿态（BlendShape 亦一并烘焙）。其余（对象池 / 淡出 / 共享材质 / MPB 染色）与网格残影一致。
///
/// 说明：单参 BakeMesh 不含 transform 缩放，故残影 GameObject 用 lossyScale 补上；若某项目的 SMR 摆放很特殊
/// （如缩放来自非均匀父级），可按需改用 BakeMesh(mesh, useScale:true) + localScale=one。本游戏为 2D Spine，
/// 此类作为框架通用能力提供，接入真实 3D 角色时建议在 Unity 内核对一次朝向/缩放。
/// </summary>
public class AfterimageGhostSkinnedMesh : AfterimageGhostBase
{
    #region 绑定源

    /// <summary>本体 SkinnedMeshRenderer</summary>
    protected SkinnedMeshRenderer srcRenderer;
    /// <summary>本体材质数组(缓存,残影共享,不克隆)</summary>
    protected Material[] srcMaterials;

    /// <summary>_Color 属性 id(缓存)</summary>
    protected static readonly int ColorPropId = Shader.PropertyToID("_Color");
    /// <summary>复用的材质属性块(逐残影覆盖 _Color)</summary>
    protected MaterialPropertyBlock mpb;

    /// <summary>蒙皮残影运行时数据</summary>
    protected class SkinnedGhostItem : GhostItem
    {
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public Mesh mesh;   //持久复用的烘焙网格
    }

    #endregion

    #region 绑定接口

    /// <summary>
    /// 绑定源 SkinnedMeshRenderer
    /// </summary>
    /// <param name="renderer">源蒙皮渲染器</param>
    public void Init(SkinnedMeshRenderer renderer)
    {
        srcRenderer = renderer;
        srcMaterials = renderer != null ? renderer.sharedMaterials : null;
        if (mpb == null)
        {
            mpb = new MaterialPropertyBlock();
        }
    }

    #endregion

    #region 基类实现

    /// <summary>源就绪判定</summary>
    protected override bool CanCapture()
    {
        return srcRenderer != null;
    }

    /// <summary>新建蒙皮残影对象(用普通 MeshRenderer 渲染烘焙出的静态网格)</summary>
    protected override GhostItem CreateGhost()
    {
        var go = new GameObject("AfterimageGhost_Skinned");
        var filter = go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = srcMaterials;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        var mesh = new Mesh();
        mesh.MarkDynamic();
        filter.sharedMesh = mesh;
        return new SkinnedGhostItem { go = go, meshRenderer = meshRenderer, meshFilter = filter, mesh = mesh, age = 0 };
    }

    /// <summary>烘焙当前蒙皮姿态到 CPU 网格 + 位置/旋转/缩放 + 排序(压身后一层)</summary>
    protected override void CaptureInto(GhostItem item)
    {
        var g = (SkinnedGhostItem)item;
        //关键:BakeMesh 把当前 GPU 蒙皮结果读回 CPU(区别于直接拷 bind pose 的 sharedMesh)
        srcRenderer.BakeMesh(g.mesh);
        var srcTF = srcRenderer.transform;
        g.go.transform.SetPositionAndRotation(srcTF.position, srcTF.rotation);
        //单参 BakeMesh 不含缩放,用 lossyScale 补上
        g.go.transform.localScale = srcTF.lossyScale;
        g.meshRenderer.sortingLayerID = srcRenderer.sortingLayerID;
        g.meshRenderer.sortingOrder = srcRenderer.sortingOrder - 1;
    }

    /// <summary>MPB 覆盖 _Color 淡出</summary>
    protected override void ApplyFade(GhostItem item, float t)
    {
        var g = (SkinnedGhostItem)item;
        mpb.SetColor(ColorPropId, ghostTint * t);
        g.meshRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>销毁蒙皮残影(连带烘焙的 Mesh)</summary>
    protected override void DestroyGhost(GhostItem item)
    {
        var g = (SkinnedGhostItem)item;
        if (g.mesh != null) Destroy(g.mesh);
        if (g.go != null) Destroy(g.go);
    }

    #endregion
}
