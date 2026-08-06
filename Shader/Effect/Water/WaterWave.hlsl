#ifndef WATER_WAVE_INCLUDED
#define WATER_WAVE_INCLUDED

// 水面波形：三方向正弦波叠加。WaterSimple / WaterLowPoly 两个 shader 共享本文件，
// 改波形只需改这里。pos 为世界空间坐标(通常传 positionWS.xz * _EffectScale)，
// speed=_WaveSpeed，scale=_WaveScale。返回的高度为原始幅度(未乘 _WaveHeight)，
// 调用方自行乘振幅。

// 三个波的固定方向/频率倍率/相位速度/振幅（两 shader 一致，集中此处便于统一调整）
static const float2 WATER_WAVE_DIR1 = float2( 1.0,  0.4);
static const float2 WATER_WAVE_DIR2 = float2(-0.6,  1.0);
static const float2 WATER_WAVE_DIR3 = float2( 0.3, -0.8);

/// 波高(原始幅度, 约 -1..1)：仅需高度时用(顶点起伏)，比 WaterWave 省去斜率计算。
float WaterWaveHeight(float2 pos, float speed, float scale)
{
    float t = _Time.y * speed;
    float s = scale;
    float f1 = dot(pos, WATER_WAVE_DIR1) * s        + t;
    float f2 = dot(pos, WATER_WAVE_DIR2) * s * 1.3  + t * 1.2;
    float f3 = dot(pos, WATER_WAVE_DIR3) * s * 0.7  + t * 0.8;
    return sin(f1) * 0.5 + sin(f2) * 0.3 + sin(f3) * 0.2;
}

/// 波面斜率(原始幅度, 未乘 _WaveHeight)：仅需法线时用(逐像素波纹)，比 WaterWave 省去 sin 高度。
float2 WaterWaveSlope(float2 pos, float speed, float scale)
{
    float t = _Time.y * speed;
    float s = scale;
    float f1 = dot(pos, WATER_WAVE_DIR1) * s        + t;
    float f2 = dot(pos, WATER_WAVE_DIR2) * s * 1.3  + t * 1.2;
    float f3 = dot(pos, WATER_WAVE_DIR3) * s * 0.7  + t * 0.8;

    float2 slope = 0;
    slope += cos(f1) * 0.5 * s       * WATER_WAVE_DIR1;
    slope += cos(f2) * 0.3 * s * 1.3 * WATER_WAVE_DIR2;
    slope += cos(f3) * 0.2 * s * 0.7 * WATER_WAVE_DIR3;
    return slope;
}

#endif // WATER_WAVE_INCLUDED
