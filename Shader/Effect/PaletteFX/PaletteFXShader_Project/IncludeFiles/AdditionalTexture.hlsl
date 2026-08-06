#ifndef ADDITIONAL_TEXTURE_INCLUDED
#define ADDITIONAL_TEXTURE_INCLUDED

#include "ShaderUtils.hlsl"
#include "MaterialProperties.hlsl"

float2 ProcessUV_AddTexUV(float2 uv, float4 st, float angle, float2 speed, float2 manualOffset,
    float swirlFactor, float2 clampValues, float2 swirlCenter, float uvAnimSpeedScale)
{
    int uvMode = 0;
#if defined(_ADDTEXUVMODE_POLAR)
    uvMode = UVMODE_POLAR;
#elif defined(_ADDTEXUVMODE_SWIRL)
    uvMode = UVMODE_SWIRL;
#endif

    return ProcessUV_Internal(
        uv, st, uvMode, angle, speed, manualOffset, swirlFactor,
        clampValues, swirlCenter, uvAnimSpeedScale, _AddTexPolarCenter
    );
}

float2 ProcessUV_AddTexMaskUV(float2 uv, float4 st, float angle, float2 speed, float2 manualOffset,
    float swirlFactor, float2 clampValues, float2 swirlCenter, float uvAnimSpeedScale)
{
    int uvMode = 0;
#if defined(_ADDTEXMASKUVMODE_POLAR)
    uvMode = UVMODE_POLAR;
#elif defined(_ADDTEXMASKUVMODE_SWIRL)
    uvMode = UVMODE_SWIRL;
#endif

    return ProcessUV_Internal(
        uv, st, uvMode, angle, speed, manualOffset, swirlFactor,
        clampValues, swirlCenter, uvAnimSpeedScale, _AddTexMaskPolarCenter
    );
}

// ✅ 修改：直接在采样时应用UV Clamp遮罩
half4 SampleAddTex(float2 uv, float2 maskUV)
{
    uv = ProcessUV_AddTexUV(
        uv, _AddTex_ST, _AddRotateAngle, float2(_AddUspeed, _AddVspeed),
        float2(0.0, 0.0), _AddSwirlFactor, float2(_AddClampU, _AddClampV),
        _AddTexSwirlCenter, 0.1
    );
    
    half4 col = SAMPLE_TEXTURE2D(_AddTex, sampler_AddTex, uv);
    half selectedChannel = SelectMaskChannel(col, _AddTexChannel);
    col.rgb = _AddTexChannel < 4 ? selectedChannel.xxx : col.rgb;
    col.rgb = AdjustBrightness(col.rgb, _AddBrightness);
#if _ADDMASK_ON
    maskUV = ProcessUV_AddTexMaskUV(
        maskUV, _AddTexMask_ST, _AddMaskRotateAngle, float2(_AddUSpeed_mask, _AddVSpeed_mask),
        float2(0.0, 0.0), _AddMaskSwirlFactor, float2(_AddMaskClampU, _AddMaskClampV),
        _AddTexMaskSwirlCenter, 0.1
    );
    half4 maskSample = SAMPLE_TEXTURE2D(_AddTexMask, sampler_AddTexMask, maskUV);
    half mask = SelectMaskChannel(maskSample, _AddMaskChannel);
    mask = ReverseValue(mask, _AddIsReverseMask);
    col.a *= mask;
#endif

    return col;
}

half4 BlendTextures(half4 mainTex, half4 addTex)
{
    half4 result = mainTex;
    half addAlpha = addTex.a;
    // 跟随主透明度（参考 ChameleonX：仅在开关开启时，才用主贴图 alpha 衰减附加图权重）
    addAlpha = _AddMulMainAlpha > 0.5 ? addAlpha * mainTex.a : addAlpha;

#if defined(_ADDBLENDMODE_MULTIPLY)
    result = mainTex * addTex;
    return result;
#elif defined(_ADDBLENDMODE_ADDITIVE)
    result = lerp(mainTex, mainTex + addTex, addAlpha);
    result = saturate(result);
    return result;
#else
    result = addTex * addAlpha + mainTex * (1 - addAlpha);
    return result;
#endif
}

// ✅ 简化：不再输出UV Clamp遮罩
// 修改函数签名，接收mainTexR参数
half4 ProcessAdditionalTexture(half4 mainCol, float2 addUV, float2 addMaskUV, half mainTexR)
{
    half4 finalCol = mainCol;

#ifdef _ADDTEX_ON
    half4 addCol = SampleAddTex(addUV, addMaskUV);
    addCol *= _AddColor;

    // 使用传入的mainTexR
    half extraMask = lerp(1.0, AdjustLevelsContrast(mainTexR, _AddMaskContrast), step(0.5, _AddMaskUseMainTexR));
    addCol.a *= extraMask;
    addCol.a *= extraMask;
    finalCol = BlendTextures(mainCol, addCol);
#endif

    return finalCol;
}

#endif // ADDITIONAL_TEXTURE_INCLUDED
