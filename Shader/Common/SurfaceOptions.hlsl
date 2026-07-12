#ifndef COMMON_SURFACE_OPTIONS_INCLUDED
#define COMMON_SURFACE_OPTIONS_INCLUDED

// 通用"渲染设置"HLSL 件：Alpha 裁剪(镂空)。
// 表面类型/渲染模式(混合)/渲染面 属于固定功能状态(Blend/ZWrite/Cull), 由材质属性 + 面板助手
// SurfaceOptionsGUI 驱动，无需在 HLSL 声明；本文件只封装需要在 frag 参与运算的 Alpha 裁剪。
// 与具体效果无关，任意 shader 均可 include 复用(配合 SurfaceOptionsGUI 材质面板)。

// 公共材质字段宏：仅 Alpha 裁剪阈值需进 HLSL。
// 由引用方 CBUFFER_START(UnityPerMaterial) 内展开，保证 material uniform 同处一个块(SRP Batcher 兼容)。
#define SURFACE_OPTIONS_CBUFFER \
    half _Cutoff;

// Alpha 裁剪：开启 _ALPHATEST_ON 时丢弃 alpha 低于 cutoff 的像素(镂空)。未开启则零成本。
// cutoff 以参数传入(不直接引用全局 uniform)，避免 include 早于 CBUFFER 声明造成前向引用，并保持跨 shader 复用。
// 需在 frag 输出/受光前调用，并在 Properties 用 [Toggle(_ALPHATEST_ON)] + pass 加
// #pragma shader_feature_local _ALPHATEST_ON 接线。
void ApplyAlphaClip(half alpha, half cutoff)
{
#if defined(_ALPHATEST_ON)
    clip(alpha - cutoff);
#endif
}

#endif // COMMON_SURFACE_OPTIONS_INCLUDED
