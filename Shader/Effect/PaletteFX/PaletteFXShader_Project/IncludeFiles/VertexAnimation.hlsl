#ifndef VERTEX_ANIMATION_INCLUDED
#define VERTEX_ANIMATION_INCLUDED

#include "ShaderUtils.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// ==================== 纹理声明 ====================

#ifndef VERTEX_ANIMATION_TEXTURE_DECLARED
#define VERTEX_ANIMATION_TEXTURE_DECLARED
TEXTURE2D(_VATex);
SAMPLER(sampler_VATex);
TEXTURE2D(_VATexMask);
SAMPLER(sampler_VATexMask);
#endif

// ==================== 参数定义 ====================

// 顶点动画基础参数
#ifndef VERTEX_ANIMATION_PARAMS_DEFINED
#define VERTEX_ANIMATION_PARAMS_DEFINED
float3 _VAPos; // 扰动强度
float _VACoordinateSpace; // 坐标空间：0=模型空间，1=世界空间
float _VANormalInfluence; // 法线影响程度 (0-1)
#endif

// 顶点动画贴图参数
#ifndef VERTEX_ANIMATION_TEX_PARAMS_DEFINED
#define VERTEX_ANIMATION_TEX_PARAMS_DEFINED
float4 _VATex_ST;
float _VATexSampleUV; // UV选择
float _VATexChannelR; // 贴图通道 R
float _VATexChannelG; // 贴图通道 G
float _VATexChannelB; // 贴图通道 B
float _VATexUVMode; // UV模式
float _VATexUVMode_Polar;
float _VATexUVMode_Swirl;
float _VATexSwirlFactor; // 漩涡强度
float _VATexRotateEnable; // 启用旋转
float _VATexRotateAngle; // 旋转角度
float _VATexUVClampU; // UV限制U
float _VATexUVClampV; // UV限制V
float _VATexUspeed; // U方向速度
float _VATexVspeed; // V方向速度
float _VATexInvert; // 反转程度
#endif

// 顶点动画贴图遮罩参数
#ifndef VERTEX_ANIMATION_TEX_MASK_PARAMS_DEFINED
#define VERTEX_ANIMATION_TEX_MASK_PARAMS_DEFINED
float4 _VATexMask_ST;
float _VATexMaskSampleUV; // UV选择
int _VATexMaskChannel; // 遮罩通道选择 (0=R, 1=G, 2=B, 3=A)
float _VATexMaskUVMode; // 遮罩UV模式
float _VATexMaskUVMode_Polar;
float _VATexMaskUVMode_Swirl;
float _VATexMaskSwirlFactor; // 漩涡强度
float _VATexMaskUVClampU; // 遮罩UV限制U
float _VATexMaskUVClampV; // 遮罩UV限制V
float _VATexMaskUspeed; // 遮罩U方向速度
float _VATexMaskVspeed; // 遮罩V方向速度
float _VATexMaskRotateEnable; // 遮罩启用旋转
float _VATexMaskRotateAngle; // 遮罩旋转角度
float _VATexMaskInvert; // 遮罩反转程度
#endif

// ==================== UV处理函数 ====================

// 1. UV处理：返回 float2
float2 ProcessUV_VATexUV(float2 uv, float4 st, float angle, float2 speed, float2 manualOffset, float swirlFactor, float2 clampValues, float2 swirlCenter, float uvAnimSpeedScale)
{
    int uvMode = 0;
#if defined(_VATEXUVMODE_POLAR)
    uvMode = UVMODE_POLAR;
#elif defined(_VATEXUVMODE_SWIRL)
    uvMode = UVMODE_SWIRL;
#endif
    return ProcessUV_Internal(uv, st, uvMode, angle, speed, manualOffset, swirlFactor, clampValues, swirlCenter, uvAnimSpeedScale, _VATexPolarCenter);
}

float2 ProcessUV_VATexMaskUV(float2 uv, float4 st, float angle, float2 speed, float2 manualOffset, float swirlFactor, float2 clampValues, float2 swirlCenter, float uvAnimSpeedScale)
{
    int uvMode = 0;
#if defined(_VATEXMASKUVMODE_POLAR)
    uvMode = UVMODE_POLAR;
#elif defined(_VATEXMASKUVMODE_SWIRL)
    uvMode = UVMODE_SWIRL;
#endif
    return ProcessUV_Internal(uv, st, uvMode, angle, speed, manualOffset, swirlFactor, clampValues, swirlCenter, uvAnimSpeedScale, _VATexMaskPolarCenter);
}

// ==================== 采样函数 ====================

// 2. 采样函数
half3 SampleVATex(float2 uv)
{
    uv = ProcessUV_VATexUV(uv, _VATex_ST, _VATexRotateAngle, float2(_VATexUspeed, _VATexVspeed), float2(0, 0), _VATexSwirlFactor, float2(_VATexUVClampU, _VATexUVClampV), _VATexSwirlCenter, 0.1);

    half4 col = SAMPLE_TEXTURE2D_LOD(_VATex, sampler_VATex, uv, 0);

    half3 vaTexChannels = half3(0.0h, 0.0h, 0.0h);
    vaTexChannels.r = _VATexChannelR > 0.5h ? col.r : 0.0h;
    vaTexChannels.g = _VATexChannelG > 0.5h ? col.g : 0.0h;
    vaTexChannels.b = _VATexChannelB > 0.5h ? col.b : 0.0h;

    vaTexChannels.r = ReverseValue(vaTexChannels.r, _VATexInvert);
    vaTexChannels.g = ReverseValue(vaTexChannels.g, _VATexInvert);
    vaTexChannels.b = ReverseValue(vaTexChannels.b, _VATexInvert);

    return vaTexChannels;
}

half SampleVATexMask(float2 uv)
{
    uv = ProcessUV_VATexMaskUV(uv, _VATexMask_ST, _VATexMaskRotateAngle, float2(_VATexMaskUspeed, _VATexMaskVspeed), float2(0, 0), _VATexMaskSwirlFactor, float2(_VATexMaskUVClampU, _VATexMaskUVClampV), _VATexMaskSwirlCenter, 0.1);

    half4 col = SAMPLE_TEXTURE2D_LOD(_VATexMask, sampler_VATexMask, uv, 0);
    half vaTexMask = SelectMaskChannel(col, _VATexMaskChannel);
    return ReverseValue(vaTexMask, _VATexMaskInvert);
}

// ==================== 顶点动画应用 ====================

// 3. 应用函数
float3 ApplyVertexAnimation(float3 positionOS, float3 normalOS, float2 uv, float2 uv1)
{
    float animateEnable = step(1e-5, dot(_VAPos, _VAPos));

    float2 selectedVATexUV = SelectUVChannel(uv, uv1, _VATexSampleUV);
    float2 vaTexUV = selectedVATexUV * _VATex_ST.xy + _VATex_ST.zw;

    // 直接采样
    half3 vaTexChannels = SampleVATex(vaTexUV);

    half vaTexMask = 1.0;
#if defined(_VATEXMASK_ON)
    float2 selectedVATexMaskUV = SelectUVChannel(uv, uv1, _VATexMaskSampleUV);
    float2 vaTexMaskUV = selectedVATexMaskUV * _VATexMask_ST.xy + _VATexMask_ST.zw;
    vaTexMask = SampleVATexMask(vaTexMaskUV);
    vaTexChannels *= vaTexMask;
#endif

    float3 directionalIntensity = float3(
        _VAPos.x * vaTexChannels.r,
        _VAPos.y * vaTexChannels.g,
        _VAPos.z * vaTexChannels.b
    ) * 10.0f;

#if defined(_VAWORLDSPACE_ON)
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
    float3 direction = lerp(float3(1, 1, 1), normalWS, _VANormalInfluence);
    float3 worldOffset = direction * directionalIntensity * 100.0;
    return animateEnable * TransformWorldToObjectDir(worldOffset, false);
#else
    float3 direction = lerp(float3(1, 1, 1), normalOS, _VANormalInfluence);
    return animateEnable * direction * directionalIntensity;
#endif
}

#endif // VERTEX_ANIMATION_INCLUDED
