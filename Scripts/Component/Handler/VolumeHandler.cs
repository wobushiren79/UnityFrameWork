using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 后处理/环境渲染 Handler（框架层）：封装引擎原生能力——URP Volume 后处理(景深 DepthOfField)与 Unity 内置雾(RenderSettings.fog)。
/// 游戏层专属能力(第三方体积雾、按场景 InitData)写在 Assets/Scripts 下的同名 partial。
/// </summary>
public partial class VolumeHandler : BaseHandler<VolumeHandler, VolumeManager>
{
    //当前天空盒
    public AsyncOperationHandle<Material> currentSkyBox;

    #region 景深 DepthOfField（URP 后处理）

    /// <summary>
    /// 设置远景模糊
    /// </summary>
    /// <param name="mode">Off：选择此选项可禁用景深。Gaussian：选择此选项可使用更快但更有限的景深模式。Bokeh：选择此选项可使用基于散景的景深模式。</param>
    /// <param name="focusDistance">设置从摄像机到焦点的距离</param>
    /// <param name="focalLength">	设置摄像机传感器和摄像机镜头之间的距离（以毫米为单位）。值越大，景深越浅。</param>
    /// <param name="aperture">设置孔径比（也称为 f 值 (f-stop) 或 f 数 (f-number)）。值越小，景深越浅。</param>
    public void SetDepthOfField(DepthOfFieldMode mode, float focusDistance, float focalLength, float aperture, bool isActive = true)
    {
        var depthOfField = manager.depthOfField;
        depthOfField.mode.overrideState = true;
        depthOfField.mode.value = mode;
        depthOfField.focusDistance.overrideState = true;
        depthOfField.focusDistance.value = focusDistance;
        depthOfField.focalLength.overrideState = true;
        depthOfField.focalLength.value = focalLength;
        depthOfField.aperture.overrideState = true;
        depthOfField.aperture.value = aperture;
        SetDepthOfFieldActive(isActive);
    }

    /// <summary>
    /// 是否开启远景模糊
    /// </summary>
    /// <param name="isActive"></param>
    public void SetDepthOfFieldActive(bool isActive)
    {
        var depthOfField = manager.depthOfField;
        depthOfField.active = isActive;
    }

    #endregion

    #region 内置雾 RenderSettings.fog（Unity 原生）

    /// <summary>
    /// 设置内置雾（RenderSettings.fog）；一次设置所有字段，由 fogMode 决定生效项：
    /// Linear 用 startDistance/endDistance（多远开始糊/多远全糊），Exponential/ExponentialSquared 用 density（浓度，Exp2 最朦胧）
    /// </summary>
    /// <param name="fogColor">雾颜色（森林建议偏冷灰绿）</param>
    /// <param name="fogMode">雾模式：Linear / Exponential / ExponentialSquared</param>
    /// <param name="startDistance">线性雾起始距离（此距离内清晰，Linear 生效）</param>
    /// <param name="endDistance">线性雾终止距离（超过则全被雾遮住，Linear 生效）</param>
    /// <param name="density">指数雾浓度（Exp/Exp2 生效）</param>
    /// <param name="isActive">是否开启</param>
    public void SetFog(Color fogColor, FogMode fogMode = FogMode.Linear, float startDistance = 0f, float endDistance = 100f, float density = 0.02f, bool isActive = true)
    {
        RenderSettings.fog = isActive;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogStartDistance = startDistance;
        RenderSettings.fogEndDistance = endDistance;
        RenderSettings.fogDensity = density;
    }

    /// <summary>
    /// 开关内置雾（RenderSettings.fog）
    /// </summary>
    /// <param name="isActive">true 开启，false 关闭</param>
    public void SetFogActive(bool isActive)
    {
        RenderSettings.fog = isActive;
    }

    #endregion
}
