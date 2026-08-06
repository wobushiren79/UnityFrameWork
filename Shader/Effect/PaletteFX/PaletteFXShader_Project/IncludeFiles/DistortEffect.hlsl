#ifndef DISTORT_EFFECT_INCLUDED
#define DISTORT_EFFECT_INCLUDED

#include "ShaderUtils.hlsl"
#include "MaterialProperties.hlsl"

// 1. UV处理：返回 float2
float2 ProcessUV_DistortTexUV(float2 uv, float4 st, float angle, float2 speed, float2 manualOffset, float swirlFactor, float2 clampValues, float2 swirlCenter, float uvAnimSpeedScale)
{
    int uvMode = 0;
#if defined(_DISTORTTEXUVMODE_POLAR)
    uvMode = UVMODE_POLAR;
#elif defined(_DISTORTTEXUVMODE_SWIRL)
    uvMode = UVMODE_SWIRL;
#endif
    return ProcessUV_Internal(uv, st, uvMode, angle, speed, manualOffset, swirlFactor, clampValues, swirlCenter, uvAnimSpeedScale, _DistortTexPolarCenter);
}

// 遮罩UV处理
float2 ProcessUV_DistortTexMaskUV(float2 uv, float4 st, float angle, float2 speed, float2 manualOffset, float swirlFactor, float2 clampValues, float2 swirlCenter, float uvAnimSpeedScale)
{
    int uvMode = 0;
#if defined(_DISTORTTEXMASKUVMODE_POLAR)
    uvMode = UVMODE_POLAR;
#elif defined(_DISTORTTEXMASKUVMODE_SWIRL)
    uvMode = UVMODE_SWIRL;
#endif
    return ProcessUV_Internal(uv, st, uvMode, angle, speed, manualOffset, swirlFactor, clampValues, swirlCenter, uvAnimSpeedScale, _DistortMaskTexPolarCenter);
}

// 2. 采样函数：移除Mask参数
half2 SampleDistortTex(float2 uv)
{
    uv = ProcessUV_DistortTexUV(uv, _DistortTex_ST, _DistortRotateAngle, float2(_DistortUspeed, _DistortVspeed), float2(0, 0), _DistortSwirlFactor, float2(_DistortClampU, _DistortClampV), _DistortTexSwirlCenter, 0.1);

    half4 col = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, uv);
    half tempDistortValue = SelectMaskChannel(col, _DistortTexChannel);
    return ReverseValue(half2(tempDistortValue, tempDistortValue), _DistortInvert);
}

half SampleDistortMask(float2 uv)
{
    uv = ProcessUV_DistortTexMaskUV(uv, _DistortMaskTex_ST, _DistortMaskRotateAngle, float2(_DistortMaskUspeed, _DistortMaskVspeed), float2(0, 0), _DistortMaskSwirlFactor, float2(_DistortMaskClampU, _DistortMaskClampV), _DistortMaskTexSwirlCenter, 0.1);

    half4 col = SAMPLE_TEXTURE2D(_DistortMaskTex, sampler_DistortMaskTex, uv);
    half maskValue = SelectMaskChannel(col, _DistortMaskChannel);
    return ReverseValue(maskValue, _DistortMaskInvert);
}

// 3. 应用函数：移除 Mask 裁剪逻辑
float2 ProcessDistort(float2 uv, float2 distortUV, float2 distortMaskUV, CustomDataParams customData)
{
    float2 resultUV = uv;

#if _DISTORT_ON
    half2 distortValue = SampleDistortTex(distortUV);
    float2 distortStrength = float2(_DistortStrengthX, _DistortStrengthY);

#if _CUSTOMDATA_ON
    distortStrength = ApplyCustomDataToDistortStrength(distortStrength, customData);
#endif

#if _DISTORTMASK_ON
    half mask = SampleDistortMask(distortMaskUV);
    distortValue *= mask;
#endif

    // 原有的 clampMask 乘法已删除
    resultUV += distortValue * distortStrength;
#endif

    return resultUV;
}

#endif // DISTORT_EFFECT_INCLUDED
