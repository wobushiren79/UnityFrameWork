#ifndef COMMON_NOISE_INCLUDED
#define COMMON_NOISE_INCLUDED

// 程序化噪声(通用)：哈希 → 值噪声 → fbm 分形叠加。
// 与具体效果无关, 任意需要湍流/扰动/溶解/云雾的 shader 均可 include 复用(火焰/烟雾/水面/溶解/贴图抖动…)。
// 全部为纯函数、不引用任何全局 uniform, 不依赖贴图(省一张噪声图的采样与内存)。
//
// 【选值噪声而非 Perlin/Simplex 的原因】火焰/烟雾这类湍流效果只需"连续且无明显重复感"，
// 值噪声(双线性插值 + 平滑曲线)已足够, 且指令数明显少于 Simplex；需要更高质量梯度时再另加函数。

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// 2D → 1D 哈希: 把平面坐标散列成 [0,1) 伪随机数。
// 经典 sin-fract 手法: 不同 GPU 的 sin 精度略有差异(结果可能有极小平台差), 但对视觉噪声完全够用。
float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// 2D 值噪声: 对整数格点哈希取值, 用平滑曲线(smoothstep 的 3t²-2t³)做双线性插值。
// 返回 [0,1]，输入坐标每 +1 换一个格子(故用 scale 控制"颗粒大小")。
float ValueNoise2D(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    // 平滑插值权重: 直接用 f 会因导数不连续产生格子状棱线
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = Hash21(i + float2(0.0, 0.0));
    float b = Hash21(i + float2(1.0, 0.0));
    float c = Hash21(i + float2(0.0, 1.0));
    float d = Hash21(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// fbm(分形布朗运动): 叠加多个倍频噪声, 每层频率翻倍、振幅减半, 得到自然的多尺度湍流。
// octaves 须为编译期常量(循环会被展开)；层数越多细节越丰富、开销线性增长, 火焰用 2~3 层足够。
// 返回归一化到 [0,1] 的结果(除以总振幅, 否则叠加后值域会溢出)。
float Fbm2D(float2 p, int octaves)
{
    float sum = 0.0;
    float amp = 0.5;
    float totalAmp = 0.0;

    [unroll(4)]
    for (int i = 0; i < octaves; i++)
    {
        sum += ValueNoise2D(p) * amp;
        totalAmp += amp;
        p *= 2.0;      // 频率翻倍
        amp *= 0.5;    // 振幅减半
    }

    return sum / max(totalAmp, 1e-4);
}

#endif // COMMON_NOISE_INCLUDED
