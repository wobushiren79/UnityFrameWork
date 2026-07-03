#ifndef PARTICLE_COMMON_INCLUDED
#define PARTICLE_COMMON_INCLUDED

// URP 透明粒子 shader 的通用部分：贴图采样器 + 公共材质字段宏 + 粒子淡出函数。
// 与具体效果无关，任意粒子 shader 均可 include 复用。
// 修改"柔和粒子/相机淡出"等通用逻辑时只需改本文件，所有引用方同步生效。

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
// Alpha 扩张描边通用函数(ApplyAlphaOutline)，供各粒子 shader 复用
#include "Outline.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// 公共材质字段(贴图ST+纹素尺寸/染色/裁剪/柔和粒子+相机淡出参数/整体偏移/描边)。
// 由引用方的 CBUFFER 在块首展开，其后紧跟该 shader 特有字段，
// 保证全部 material uniform 仍在同一 UnityPerMaterial 块内(SRP Batcher 兼容)。
// _BaseMap_TexelSize 由 Unity 按所赋贴图自动填充，放在 UnityPerMaterial 内保持 SRP Batcher 兼容。
#define PARTICLE_COMMON_CBUFFER \
    float4 _BaseMap_ST; \
    float4 _BaseMap_TexelSize; \
    half4  _BaseColor; \
    half   _Cutoff; \
    float  _SoftParticleNearFade; \
    float  _SoftParticleFarFade; \
    float  _CameraNearFade; \
    float  _CameraFarFade; \
    float4 _PositionOffset; \
    half4  _OutlineColor; \
    half   _OutlineSize;

// 粒子淡出系数：柔和粒子(与场景深度相交淡出) * 相机近距离淡出，未开启对应开关时返回 1。
// 参数化设计(不直接引用全局 uniform)，便于被各 shader 复用。
half ParticleFade(float4 screenPos, float softNear, float softFar, float camNear, float camFar)
{
    half fade = 1.0h;
#if defined(_SOFTPARTICLES_ON) || defined(_CAMERAFADE_ON)
    float thisEyeDepth = screenPos.w;
#endif
#if defined(_SOFTPARTICLES_ON)
    float2 screenUV = screenPos.xy / screenPos.w;
    float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
    fade *= saturate((sceneEyeDepth - softNear - thisEyeDepth) / max(softFar, 1e-4));
#endif
#if defined(_CAMERAFADE_ON)
    fade *= saturate((thisEyeDepth - camNear) / max(camFar - camNear, 1e-4));
#endif
    return fade;
}

#endif // PARTICLE_COMMON_INCLUDED
