#ifndef DISSOLVE_EFFECT_INCLUDED
#define DISSOLVE_EFFECT_INCLUDED

#include "ShaderUtils.hlsl"
#include "MaterialProperties.hlsl"

// ==================== UV处理函数 ====================

// 1. UV处理函数：直接返回 float2
float2 ProcessUV_DissolveTexUV(float2 uv, float4 st, float angle, float2 speed, float2 manualOffset, float swirlFactor, float2 clampValues, float2 swirlCenter, float uvAnimSpeedScale)
{
    int uvMode = 0;
#if defined(_DISSOLVETEXUVMODE_POLAR)
    uvMode = UVMODE_POLAR;
#elif defined(_DISSOLVETEXUVMODE_SWIRL)
    uvMode = UVMODE_SWIRL;
#endif
    return ProcessUV_Internal(uv, st, uvMode, angle, speed, manualOffset, swirlFactor, clampValues, swirlCenter, uvAnimSpeedScale, _DissolveTexPolarCenter);
}

float2 ProcessUV_DissolveTexAddUV(float2 uv, float4 st, float angle, float2 speed, float2 manualOffset, float swirlFactor, float2 clampValues, float2 swirlCenter, float uvAnimSpeedScale)
{
    int uvMode = 0;
#if defined(_DISSOLVETEXADDUVMODE_POLAR)
    uvMode = UVMODE_POLAR;
#elif defined(_DISSOLVETEXADDUVMODE_SWIRL)
    uvMode = UVMODE_SWIRL;
#endif
    return ProcessUV_Internal(uv, st, uvMode, angle, speed, manualOffset, swirlFactor, clampValues, swirlCenter, uvAnimSpeedScale, _DissolveAddTexPolarCenter);
}
// ==================== 采样函数 ====================

// 2. 采样函数：移除 Mask 输出参数
half SampleDissolveTex(float2 uv)
{
    // 应用 Clamp 后的 UV
    uv = ProcessUV_DissolveTexUV(uv, _DissolveTex_ST, _DissolveRotateAngle, float2(_DissolveUspeed, _DissolveVspeed), float2(0, 0), _DissolveSwirlFactor, float2(_DissolveClampU, _DissolveClampV), _DissolveTexSwirlCenter, 0.1);

    half4 col = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, uv);
    half dissolve = SelectMaskChannel(col, _DissolveTexChannel);
    return ReverseValue(dissolve, _DissolveInvert);
}

half SampleDissolveAddTex(float2 uv)
{
    uv = ProcessUV_DissolveTexAddUV(uv, _DissolveAddTex_ST, _DissolveAddRotateAngle, float2(_DissolveAddUspeed, _DissolveAddVspeed), float2(0, 0), _DissolveAddSwirlFactor, float2(_DissolveAddClampU, _DissolveAddClampV), _DissolveAddTexSwirlCenter, 0.1);

    half4 col = SAMPLE_TEXTURE2D(_DissolveAddTex, sampler_DissolveAddTex, uv);
    half dissolveAdd = SelectMaskChannel(col, _DissolveAddTexChannel);
    return ReverseValue(dissolveAdd, _DissolveAddInvert);
}
// ==================== 溶解效果应用 ====================

// 3. 应用函数：移除 Mask 混合逻辑
half4 ApplyDissolve(half4 originalColor, float2 dissolveUV, float2 dissolveAddUV, CustomDataParams customData)
{
    // 直接采样（此时 UV 已经被拉伸 Clamp 到边缘，不会出现透明缝隙）
    half dissolveA = SampleDissolveTex(dissolveUV);

    half dissolveAddA = 0;
#if _DISSOLVEADD_ON
    dissolveAddA = SampleDissolveAddTex(dissolveAddUV);
#endif

    // 后续逻辑保持不变...
    half finalDissolveA;
#if _DISSOLVEADD_ON
    finalDissolveA = saturate((dissolveA + dissolveAddA) / 2);
#else
    finalDissolveA = dissolveA;
#endif

    float dissolveFactor = _DissolveFactor;
    float edgeIntensity = _DissolveEdgeIntensity;
#if _CUSTOMDATA_ON
    dissolveFactor = ApplyCustomDataToDissolveFactor(dissolveFactor, customData);
#endif

    // 早期退出优化
    if (dissolveFactor <= 0.001) return originalColor;

    // 计算边缘和透明度
    half edgeWidth = max(0.001, _DissolveEdgeWidth);
#if _CUSTOMDATA_ON
    edgeWidth = ApplyCustomDataToDissolveEdgeWidth(edgeWidth, customData);
    edgeWidth = max(0.001, edgeWidth);
#endif

    half smoothness = saturate(_DissolveEdgeSmoothness);
    half smoothRange = lerp(0.001, 1, smoothness);

    half dissolveStart = dissolveFactor - edgeWidth;
    half dissolveEnd = dissolveFactor + smoothRange;
    half dissolveMask = smoothstep(dissolveStart, dissolveEnd, finalDissolveA);

    half edgeStart = dissolveFactor - edgeWidth;
    half edgeEnd = dissolveFactor;
    half edgeRange = edgeEnd - edgeStart;

    half edgeX = saturate((finalDissolveA - edgeStart) / max(0.001, edgeRange));
    half edgeSmooth = edgeX * edgeX * (3.0 - 2.0 * edgeX);

    half normalizationFactor = 1.0 / max(0.001, edgeWidth + smoothRange);
    half edgeMask = edgeSmooth * (1.0 - dissolveMask) * normalizationFactor * edgeWidth;

    half4 result = originalColor;
    half3 edgeColor = _DissolveColor.rgb * _DissolveColor.a * edgeIntensity;
    result.rgb = result.rgb + edgeColor * edgeMask;
    result.a *= dissolveMask;

    return result;
}

#endif // DISSOLVE_EFFECT_INCLUDED
