#ifndef MAIN_TEXTURE_INCLUDED
#define MAIN_TEXTURE_INCLUDED

#include "ShaderUtils.hlsl"
#include "MaterialProperties.hlsl"
#include "DistortEffect.hlsl"

// UV处理：返回 float2
float2 ProcessMainTextureUV(float2 uv, float2 manualOffset)
{
    int uvMode = 0;
#if defined(_MAINTEXUVMODE_POLAR)
    uvMode = UVMODE_POLAR;
#elif defined(_MAINTEXUVMODE_SWIRL)
    uvMode = UVMODE_SWIRL;
#endif

    return ProcessUV_Internal(
        uv, _MainTex_ST, uvMode, _RotateAngle, float2(_Uspeed, _Vspeed),
        manualOffset, _SwirlFactor, float2(_ClampU, _ClampV),
        _MainTexSwirlCenter, 0.1, _MainTexPolarCenter
    );
}

// 遮罩UV处理：返回 float2
float2 ProcessMainTextureUV(float2 uv)
{
    return ProcessMainTextureUV(uv, float2(0.0, 0.0));
}

// 遮罩UV处理：返回 float2
float2 ProcessMainTextureMaskUV(float2 maskUV, float2 manualOffset)
{
    int maskUVMode = 0;
#if defined(_MAINTEXMASKUVMODE_POLAR)
    maskUVMode = UVMODE_POLAR;
#elif defined(_MAINTEXMASKUVMODE_SWIRL)
    maskUVMode = UVMODE_SWIRL;
#endif

    return ProcessUV_Internal(
        maskUV, _DiffuseMaskTex_ST, maskUVMode, _DiffuseMaskAng,
        float2(_USpeed_diffusem, _VSpeed_diffusem), manualOffset,
        _MaskSwirlFactor, float2(_MaskClampU, _MaskClampV),
        _MainTexMaskSwirlCenter, 0.1, _MainTexMaskPolarCenter
    );
}

float2 ProcessMainTextureMaskUV(float2 maskUV)
{
    return ProcessMainTextureMaskUV(maskUV, float2(0.0, 0.0));
}

// 主贴图采样
half4 SampleMainTex(float2 uv, float2 manualOffset)
{
    // 应用 UV 变换与 Clamp
    uv = ProcessMainTextureUV(uv, manualOffset);

    half4 col;
    half4 oricol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

#if _FLOWMAP_ON && _DISTORT_ON
    half4 distortTexColor = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, uv);
    float3 distortTexRGB = distortTexColor.rgb;

#if _DISTORTMASK_ON
    // ⚠️ 修正：SampleDistortMask 现在只接受一个参数 (uv)
    half mask = SampleDistortMask(uv);
    float3 maskedDistortTexRGB = distortTexRGB * mask;
#else
    float3 maskedDistortTexRGB = distortTexRGB;
#endif

    float3 processedDistortTexRGB;
#ifdef _ENABLEFLOWMAPLTG_ON
    processedDistortTexRGB = FastLinearToSRGB(maskedDistortTexRGB);
#else
    processedDistortTexRGB = maskedDistortTexRGB;
#endif

    float3 flowDirection = -((processedDistortTexRGB) * 1.0 + -0.5) * _FlowMapIntensity;
    float timePhase1 = frac((_Time.y * _FlowMapSpeed) + 0.5);
    float timePhase2 = frac(_FlowMapSpeed * _Time.y);
    float flowLerpFactor = abs((0.5 - timePhase1) * 2.0);

    float3 distortTexUV1 = float3((timePhase1 * flowDirection.xy) + float2(uv), 0.0);
    float3 distortTexUV2 = float3(uv, 0.0) + (flowDirection * timePhase2);

    half4 tex1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortTexUV1.xy);
    half4 tex2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortTexUV2.xy);

    col.rgb = lerp(tex1.rgb, tex2.rgb, flowLerpFactor);
    col.a = lerp(tex1.a, tex2.a, flowLerpFactor);

    col = _MainAcceptDistort > 0.5 ? col : oricol;
#else
    col = oricol;
#endif

    col.rgb = AdjustBrightness(col.rgb, _Brightness);

    return col;
}

half4 SampleMainTex(float2 uv)
{
    return SampleMainTex(uv, float2(0.0, 0.0));
}

// 主贴图遮罩采样
half SampleMainTexMask(float2 maskUV, float2 manualOffset)
{
    // ⚠️ 修正：不再尝试获取 clampMask
    maskUV = ProcessMainTextureMaskUV(maskUV, manualOffset);
    // float maskClampMask = processedMaskUV.clampMask; // 已移除

    half4 maskCol = SAMPLE_TEXTURE2D(_DiffuseMaskTex, sampler_DiffuseMaskTex, maskUV);
    half mask = SelectMaskChannel(maskCol, _MaskChannel);
    mask = ReverseValue(mask, _isReverseMask);

    return mask;
}

half SampleMainTexMask(float2 maskUV)
{
    return SampleMainTexMask(maskUV, float2(0.0, 0.0));
}

// 处理主贴图入口
half4 ProcessMainTexture(float2 uv, float2 maskUV, bool enableMask, out half mainTexR, float2 mainCustomOffset, float2 maskCustomOffset)
{
    half4 col = SampleMainTex(uv, mainCustomOffset);
    mainTexR = col.r;  // 保存R通道值
    half selectedChannel = SelectMaskChannel(col, _MainTexChannel);
    col.rgb = _MainTexChannel < 4 ? selectedChannel.xxx : col.rgb;
    col.rgb *= _Color.rgb;
    col.a *= _Color.a;

#ifdef _MASK_ON
    half mask = SampleMainTexMask(maskUV, maskCustomOffset);
    col.a *= mask;
#endif

    return col;
}

half4 ProcessMainTexture(float2 uv, float2 maskUV, bool enableMask, out half mainTexR)
{
    return ProcessMainTexture(uv, maskUV, enableMask, mainTexR, float2(0.0, 0.0), float2(0.0, 0.0));
}

half4 ProcessMainTexture(float2 uv)
{
    half dummy = 0.0h;  // 添加这个变量
    return ProcessMainTexture(uv, uv, false, dummy);  // 现在是4个参数
}

#endif // MAIN_TEXTURE_INCLUDED
