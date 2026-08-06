#ifndef FEATURES_FRESNEL_INCLUDED
#define FEATURES_FRESNEL_INCLUDED

#include "ShaderUtils.hlsl"

// 菲涅尔效果相关属性
// 这些属性在TabExampleShader.shader的CBUFFER_START中定义
// 这里不再声明变量，而是在函数中直接使用传入的参数

// 菲涅尔使用加法混合模式

// 计算基础菲涅尔效果
float CalculateBaseFresnel(float3 normalWS, float3 viewDirWS, float3 viewOffset, float range, float edgeHardness, float invert)
{

    // 首先，确保视角方向是归一化的
    float3 V = normalize(viewDirWS);

    // 找一个与视角方向不平行的向量，用于构建正交基
    float3 upVector = abs(V.y) < 0.999 ? float3(0, 1, 0) : float3(1, 0, 0);

    // 构建正交基 - 右向量和上向量
    float3 rightVector = normalize(cross(upVector, V));
    float3 upVectorOrtho = normalize(cross(V, rightVector));

    // 在这个正交基中应用偏移
    // X偏移影响右向量方向，Y偏移影响上向量方向，Z偏移影响前向量（视角）方向
    float3 offsetViewDir = normalize(V + rightVector * viewOffset.x + upVectorOrtho * viewOffset.y + V * viewOffset.z);

    // 不修改法线方向
    float3 offsetNormalWS = normalize(normalWS);

    // 计算基础菲涅尔效果 - 视角与法线夹角的余弦值
    float NdotV = saturate(dot(normalize(offsetNormalWS), normalize(offsetViewDir)));

    // 根据反转程度决定是使用 1-NdotV（标准菲涅尔）还是直接使用 NdotV（反转菲涅尔）
    // 当 invert = 0 时，使用 1-NdotV（标准菲涅尔，边缘亮）
    // 当 invert = 1 时，使用 NdotV（反转菲涅尔，中心亮）
    float fresnelBase = lerp(1.0 - NdotV, NdotV, invert);

    // 应用范围参数 - 使用 abs 确保不会有负值
    float fresnel = pow(abs(fresnelBase), range);

    // 应用边缘硬度
    fresnel = pow(saturate(fresnel), edgeHardness);

    return saturate(fresnel);
}

// 应用菲涅尔加法混合
float3 ApplyFresnelBlend(float3 baseColor, float3 fresnelColor, float fresnelFactor, float brightness = 1.0)
{
    // 使用加法混合应用菲涅尔效果，并应用亮度调整
    return baseColor + fresnelColor * fresnelFactor * brightness;
}

// 菲涅尔效果入口函数
void ProcessFresnel(inout float4 color, float3 normalWS, float3 viewDirWS, float4 vertexColor, float4 customData1,
                   float3 viewOffset, float range, float intensity, float edgeHardness, float invert, float mask, float4 fresnelColor, float brightness = 1.0, bool useAsAlpha = false)
{
    // 计算菲涅尔强度
    float finalIntensity = intensity * vertexColor.b;

    // 计算基础菲涅尔效果
    float fresnelFactor = CalculateBaseFresnel(normalWS, viewDirWS, viewOffset, range, edgeHardness, invert) * finalIntensity;

    // 标准菲涅尔模式
    // 应用菲涅尔加法混合，并传递亮度参数
    color.rgb = ApplyFresnelBlend(color.rgb, fresnelColor.rgb, fresnelFactor, brightness);
    
    // 如果启用了菲涅尔因子作为透明度的功能
    // 将菲涅尔因子作为整个材质的透明度
    color.a = useAsAlpha > 0.5 ? color.a * fresnelFactor : color.a;
}

#endif // FEATURES_FRESNEL_INCLUDED
