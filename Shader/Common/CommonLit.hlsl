#ifndef COMMON_LIT_INCLUDED
#define COMMON_LIT_INCLUDED

// 通用简易光照(BlinnPhong)：给定 albedo/alpha/世界坐标/世界法线/裁剪坐标/雾因子，
// 返回经主光+附加光+阴影+环境光(SH)照亮并混合雾后的最终颜色。
// 与具体效果无关，任意带"是否开启 Lit"开关的 shader(粒子/精灵/网格)均可 include 复用。
// 约束：须在 include 本文件前先 include URP 的 Lighting.hlsl(提供 SurfaceData/InputData/UniversalFragmentBlinnPhong)。

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// 计算受光颜色：albedo=已算好的反照率, alpha=最终不透明度, positionWS=世界坐标,
// normalWS=已归一化世界法线, positionHCS=裁剪空间坐标(供屏幕空间遮蔽), fogFactor=雾因子。
half4 ApplyCommonLit(half3 albedo, half alpha, float3 positionWS, half3 normalWS,
                     float4 positionHCS, half fogFactor)
{
    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo    = albedo;
    surfaceData.alpha     = alpha;
    surfaceData.occlusion = 1.0;

    InputData inputData = (InputData)0;
    inputData.positionWS              = positionWS;
    inputData.normalWS                = normalWS;
    inputData.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(positionWS);
    inputData.shadowCoord             = TransformWorldToShadowCoord(positionWS);
    inputData.fogCoord                = fogFactor;
    inputData.bakedGI                 = SampleSH(normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionHCS);
    inputData.shadowMask              = half4(1, 1, 1, 1);

    half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, fogFactor);
    return color;
}

#endif // COMMON_LIT_INCLUDED
