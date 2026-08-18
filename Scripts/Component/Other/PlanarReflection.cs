using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 平面反射（Planar Reflection）组件：在指定水平面下方维护一台镜像相机，
/// 把场景（含天空盒/树木/角色）渲染到 RenderTexture 并写入全局 Shader 纹理 _PuddlePlanarTex，
/// 配合 FrameWork/URP/Puddle1 水洼 shader（FrameWork/Shader/URP/Shader_Puddle_1.shader）的屏幕空间采样实现"水面倒映场景物体与角色"。
/// 挂在雨天 Details 的水洼组节点上，随 details 显隐自动开关；关闭时释放相机与 RT 并把全局开关 _PuddlePlanarActive 置 0。
/// </summary>
public class PlanarReflection : MonoBehaviour
{
    #region 配置参数
    [Header("反射平面")]
    [Tooltip("反射水平面相对本节点世界Y的偏移（水洼面片离地高度）")]
    public float waterLevelOffset = 0.02f;

    [Header("渲染设置")]
    [Tooltip("反射纹理高度（宽度按源相机纵横比推导），越低越省")]
    public int textureHeight = 288;
    [Tooltip("镜像相机的渲染遮罩（默认 Everything，可按需剔除粒子/UI）")]
    public LayerMask reflectMask = ~0;
    [Tooltip("是否每帧更新（false=隔帧更新，开销减半）")]
    public bool updateEveryFrame = true;
    [Tooltip("镜像相机远裁剪面（0=跟随主相机；反射只需近处物体时调小可减少裁剪与绘制量）")]
    public float farClipOverride = 0f;
    [Tooltip("水洼不在主相机视野内时跳过渲染（视野外零开销）")]
    public bool skipWhenNotVisible = true;
    #endregion

    #region 运行时状态
    /// <summary>镜像相机（禁用手动 Render）</summary>
    protected Camera reflectionCamera;
    /// <summary>反射渲染目标</summary>
    protected RenderTexture reflectionRT;
    /// <summary>帧计数（隔帧更新用）</summary>
    protected int frameCounter;
    /// <summary>主相机缓存（避免 Camera.main 每帧按标签查找）</summary>
    protected Camera cacheMainCamera;
    /// <summary>节点下水洼渲染器缓存（视野检测用）</summary>
    protected Renderer[] cacheChildRenderers;
    /// <summary>主相机视锥平面缓存（视野检测用，避免每帧分配）</summary>
    protected readonly Plane[] cacheFrustumPlanes = new Plane[6];
    #endregion

    #region 生命周期
    /// <summary>
    /// 启用时创建镜像相机与反射纹理，并开启全局 Shader 采样开关
    /// </summary>
    protected void OnEnable()
    {
        CreateReflectionCamera();
        cacheChildRenderers = GetComponentsInChildren<Renderer>(true);
        Shader.SetGlobalFloat("_PuddlePlanarActive", 1f);
        Shader.SetGlobalTexture("_PuddlePlanarTex", reflectionRT);
    }

    /// <summary>
    /// 禁用时关闭全局采样开关并释放镜像相机与反射纹理
    /// </summary>
    protected void OnDisable()
    {
        Shader.SetGlobalFloat("_PuddlePlanarActive", 0f);
        cacheMainCamera = null;
        cacheChildRenderers = null;
        if (reflectionCamera != null)
        {
            Destroy(reflectionCamera.gameObject);
            reflectionCamera = null;
        }
        if (reflectionRT != null)
        {
            reflectionRT.Release();
            Destroy(reflectionRT);
            reflectionRT = null;
        }
    }

    /// <summary>
    /// 每帧跟随主相机镜像渲染（隔帧更新时跳过奇数帧；水洼在主相机视野外时整帧跳过）
    /// </summary>
    protected void LateUpdate()
    {
        if (!updateEveryFrame && (frameCounter++ & 1) != 0)
            return;
        if (cacheMainCamera == null || !cacheMainCamera.enabled)
            cacheMainCamera = Camera.main;
        if (cacheMainCamera == null)
            return;
        if (skipWhenNotVisible && !IsAnyPuddleVisible(cacheMainCamera))
            return;
        RenderReflection(cacheMainCamera);
    }

    /// <summary>
    /// 检测是否有任一水洼面片进入主相机视锥（包围盒级别粗测，有任一面片可见即渲染）
    /// </summary>
    /// <param name="mainCamera">主相机</param>
    /// <returns>是否有水洼可见</returns>
    protected bool IsAnyPuddleVisible(Camera mainCamera)
    {
        if (cacheChildRenderers == null || cacheChildRenderers.Length == 0)
            return true;
        GeometryUtility.CalculateFrustumPlanes(mainCamera, cacheFrustumPlanes);
        foreach (var itemRenderer in cacheChildRenderers)
        {
            if (itemRenderer != null && itemRenderer.enabled && GeometryUtility.TestPlanesAABB(cacheFrustumPlanes, itemRenderer.bounds))
                return true;
        }
        return false;
    }
    #endregion

    #region 反射渲染
    /// <summary>
    /// 创建镜像相机子物体与反射 RT（关阴影/后处理省开销，ClearFlags=Skybox 让反射自带天空）
    /// </summary>
    protected void CreateReflectionCamera()
    {
        reflectionRT = new RenderTexture(textureHeight * 16 / 9, textureHeight, 24, RenderTextureFormat.Default);
        reflectionRT.name = "PlanarReflectionRT";
        reflectionRT.Create();

        GameObject objCamera = new GameObject("ReflectionCamera");
        objCamera.transform.SetParent(transform, false);
        reflectionCamera = objCamera.AddComponent<Camera>();
        reflectionCamera.enabled = false;//手动 Render，不参与常规渲染
        reflectionCamera.clearFlags = CameraClearFlags.Skybox;
        reflectionCamera.cullingMask = reflectMask;
        //关阴影与后处理（URP 附加数据），反射渲染开销减半
        var cameraData = objCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null)
        {
            cameraData.renderShadows = false;
            cameraData.renderPostProcessing = false;
        }
    }

    /// <summary>
    /// 把源相机镜像到水平面下方并渲染一帧到反射 RT
    /// </summary>
    /// <param name="srcCamera">被镜像的源相机（主相机）</param>
    protected void RenderReflection(Camera srcCamera)
    {
        float planeY = transform.position.y + waterLevelOffset;
        //几何镜像：位置关于水平面对称，前向/上向反射后重建朝向（镜像视差自然成立，无需翻转裁剪面）
        Vector3 srcPos = srcCamera.transform.position;
        Vector3 mirrorPos = new Vector3(srcPos.x, 2f * planeY - srcPos.y, srcPos.z);
        Vector3 forward = srcCamera.transform.forward;
        Vector3 up = srcCamera.transform.up;
        Vector3 mirrorForward = new Vector3(forward.x, -forward.y, forward.z);
        Vector3 mirrorUp = new Vector3(up.x, -up.y, up.z);
        reflectionCamera.transform.SetPositionAndRotation(mirrorPos, Quaternion.LookRotation(mirrorForward, mirrorUp));
        reflectionCamera.fieldOfView = srcCamera.fieldOfView;
        reflectionCamera.nearClipPlane = srcCamera.nearClipPlane;
        reflectionCamera.farClipPlane = farClipOverride > 0f ? farClipOverride : srcCamera.farClipPlane;
        reflectionCamera.cullingMask = reflectMask;
        //RT 宽度按源相机纵横比自适应（窗口比例变化时重建）
        int targetWidth = Mathf.Max(16, Mathf.RoundToInt(textureHeight * srcCamera.aspect));
        if (reflectionRT.width != targetWidth)
        {
            reflectionRT.Release();
            reflectionRT.width = targetWidth;
            reflectionRT.Create();
        }
        reflectionCamera.targetTexture = reflectionRT;
        //斜投影近裁剪面=水平面，裁掉水面以下的几何（如地面背面）
        reflectionCamera.projectionMatrix = CalculateObliqueMatrix(srcCamera.projectionMatrix, CameraSpacePlane(reflectionCamera, new Vector3(0f, planeY, 0f), Vector3.up));
        reflectionCamera.Render();
        reflectionCamera.targetTexture = null;
    }

    /// <summary>
    /// 计算相机空间下的水平面方程（斜投影裁剪用）
    /// </summary>
    /// <param name="cam">镜像相机</param>
    /// <param name="pos">平面上一点（世界坐标）</param>
    /// <param name="normal">平面法线（世界坐标）</param>
    /// <returns>相机空间平面方程 xyz=法线 w=-点距</returns>
    protected static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal)
    {
        Matrix4x4 worldToCamera = cam.worldToCameraMatrix;
        Vector3 cPos = worldToCamera.MultiplyPoint(pos);
        Vector3 cNormal = worldToCamera.MultiplyVector(normal).normalized;
        return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
    }

    /// <summary>
    /// 计算斜投影矩阵（把近裁剪面改为指定平面，标准 Water.cs 做法）
    /// </summary>
    /// <param name="projection">源投影矩阵</param>
    /// <param name="clipPlane">相机空间裁剪平面</param>
    /// <returns>斜投影矩阵</returns>
    protected static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projection, Vector4 clipPlane)
    {
        Vector4 q = projection.inverse * new Vector4(Mathf.Sign(clipPlane.x), Mathf.Sign(clipPlane.y), 1.0f, 1.0f);
        Vector4 c = clipPlane * (2.0f / Vector4.Dot(clipPlane, q));
        projection[2] = c.x - projection[3];
        projection[6] = c.y - projection[7];
        projection[10] = c.z - projection[11];
        projection[14] = c.w - projection[15];
        return projection;
    }
    #endregion
}
