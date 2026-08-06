#ifndef COMMON_OUTLINE_INCLUDED
#define COMMON_OUTLINE_INCLUDED

// Alpha 扩张描边(通用)：在精灵/粒子贴图的透明区域，若周围存在不透明像素，
// 则沿轮廓外扩一圈描边色。适用于固定法线的 billboard/精灵(法线外扩不可用时的替代方案)。
// 与具体效果无关，任意使用 _BaseMap(带透明/裁剪)的 shader 均可 include 复用。
// 纹理以 TEXTURE2D_PARAM 参数传入(不引用具体贴图全局名)，保持跨 shader 复用能力。

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// 采样上下左右+四斜角共8邻域的最大 alpha，作为"本像素附近是否存在实体轮廓"的判据。
half OutlineNeighborAlpha(TEXTURE2D_PARAM(baseMap, baseSampler), float2 uv, float2 offset)
{
    half a = 0.0h;
    a = max(a, SAMPLE_TEXTURE2D(baseMap, baseSampler, uv + float2( offset.x, 0.0)).a);
    a = max(a, SAMPLE_TEXTURE2D(baseMap, baseSampler, uv + float2(-offset.x, 0.0)).a);
    a = max(a, SAMPLE_TEXTURE2D(baseMap, baseSampler, uv + float2(0.0,  offset.y)).a);
    a = max(a, SAMPLE_TEXTURE2D(baseMap, baseSampler, uv + float2(0.0, -offset.y)).a);
    a = max(a, SAMPLE_TEXTURE2D(baseMap, baseSampler, uv + float2( offset.x,  offset.y)).a);
    a = max(a, SAMPLE_TEXTURE2D(baseMap, baseSampler, uv + float2(-offset.x,  offset.y)).a);
    a = max(a, SAMPLE_TEXTURE2D(baseMap, baseSampler, uv + float2( offset.x, -offset.y)).a);
    a = max(a, SAMPLE_TEXTURE2D(baseMap, baseSampler, uv + float2(-offset.x, -offset.y)).a);
    return a;
}

// 在 baseCol(原始贴图采样) 上叠加 Alpha 扩张描边并返回新颜色。
// baseTexel   = _BaseMap_TexelSize.xy(单个纹素的 UV 尺寸)
// outlineSize = 描边纹素数(向外扩几个像素)
// outlineColor= 描边色(a 为描边不透明度)
// 仅在本像素为空白、且邻域存在实体处绘制描边；实体像素原样保留。
half4 ApplyAlphaOutline(TEXTURE2D_PARAM(baseMap, baseSampler), half4 baseCol,
                        float2 uv, float2 baseTexel, float outlineSize, half4 outlineColor)
{
    float2 offset = baseTexel * max(outlineSize, 0.0);
    half neighborA = OutlineNeighborAlpha(TEXTURE2D_ARGS(baseMap, baseSampler), uv, offset);

    // 描边强度 = 邻域实体程度 * (1-本像素alpha) * 描边不透明度，仅在空白区外扩
    half edge = saturate(neighborA) * saturate(1.0h - baseCol.a) * outlineColor.a;

    half4 outCol = baseCol;
    outCol.rgb = lerp(outCol.rgb, outlineColor.rgb, edge);
    outCol.a   = max(outCol.a, edge);
    return outCol;
}

#endif // COMMON_OUTLINE_INCLUDED
