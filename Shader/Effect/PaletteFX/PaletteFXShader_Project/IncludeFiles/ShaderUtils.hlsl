#ifndef SHADER_UTILS_INCLUDED
#define SHADER_UTILS_INCLUDED

#ifndef UNITY_COMMON_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#endif

// ==================== UV旋转函数 ====================
float2 RotateUV(float2 uv, float angle, float2 center = float2(0.5, 0.5))
{
    float rad = radians(angle);
    uv -= center;
    float s = sin(rad);
    float c = cos(rad);
    float2x2 rotMat = float2x2(c, -s, s, c);
    uv = mul(rotMat, uv);
    uv += center;
    return uv;
}

// ==================== 亮度/饱和度调整 ====================
half3 AdjustBrightness(half3 color, half brightness)
{
    return color * brightness;
}

half3 AdjustSaturation(half3 color, half saturation)
{
    half grey = dot(color, half3(0.299, 0.587, 0.114));
    return lerp(half3(grey, grey, grey), color, saturation);
}
half AdjustLevelsContrast(half value, half contrast)
{
    value = saturate(value);
    contrast = max(0.0, contrast);

    float gammaValue = pow(max(value, 0.0001), 1.0 / 2.2);
    gammaValue = saturate((gammaValue - 0.5) * contrast + 0.5);
    return pow(max(gammaValue, 0.0001), 2.2);
}
// ==================== 反转函数 ====================
half ReverseValue(half value, half reverseAmount = 0)
{
    value = max(0, value);
    float gammaValue = pow(max(value, 0.0001), 1.0 / 2.2);
    float invertedGamma = lerp(gammaValue, 1.0 - gammaValue, reverseAmount);
    invertedGamma = max(0.0001, invertedGamma);
    return pow(invertedGamma, 2.2);
}

half2 ReverseValue(half2 value, half reverseAmount = 0)
{
    return half2(
        ReverseValue(value.x, reverseAmount),
        ReverseValue(value.y, reverseAmount)
    );
}

// ==================== 通道选择 ====================
half SelectMaskChannel(half4 mask, int channel)
{
    return
        (channel == 0) * mask.r +
        (channel == 1) * mask.g +
        (channel == 2) * mask.b +
        (channel == 3) * mask.a;
}

float2 SelectUVChannel(float2 uv0, float2 uv1, float uvSelector)
{
    return (uvSelector > 0.5) ? uv1 : uv0;
}

// ==================== UV模式定义 ====================
#define UVMODE_NORMAL 0
#define UVMODE_POLAR 1
#define UVMODE_SWIRL 2

// ==================== 极坐标/漩涡变换 ====================
float2 PolarUV(float2 uv, float4 st, float2 center)
{
    uv = uv - center;
    return float2(length(uv) * 2.0, atan2(uv.x, uv.y) / 6.2831852) + st.zw;
}

float2 SwirlUV(float2 uv, float2 center, float strength, float2 offset)
{
    float2 delta = uv - center;
    float angle = strength * length(delta);
    float x = cos(angle) * delta.x - sin(angle) * delta.y;
    float y = sin(angle) * delta.x + cos(angle) * delta.y;
    return float2(x + center.x + offset.x, y + center.y + offset.y);
}

// ==================== 🔧 核心修改：UV Clamp ====================

// 🆕 新增：计算UV Clamp遮罩（不执行clip）
float CalculateUVClampMask(float2 uv, float2 clampValues)
{
    const float epsilon = 0.0015;

    // 创建Clamp掩码：如果clampValues > 0.5，则启用对应轴的裁剪
    float2 clampMask = step(0.5, clampValues);

    // 定义有效范围（带边界收缩）
    float2 minBounds = float2(epsilon, epsilon);
    float2 maxBounds = float2(1.0 - epsilon, 1.0 - epsilon);

    // 检查UV是否在有效范围内
    float2 inBoundsMin = step(minBounds, uv);      // uv >= epsilon
    float2 inBoundsMax = step(uv, maxBounds);      // uv <= 1-epsilon
    float2 inBounds = inBoundsMin * inBoundsMax;

    // 应用Clamp掩码：只检查启用了Clamp的轴
    float2 validMask = lerp(float2(1, 1), inBounds, clampMask);

    // 返回最终遮罩值（0=裁剪，1=保留）
    return validMask.x * validMask.y;
}

// 🔧 修改后的ApplyUVClamp：返回UV和遮罩
float2 ApplyUVClamp(float2 uv, float2 clampValues)
{
    // 定义安全边界（根据纹理分辨率调整）
    // 对于1024x1024贴图，0.001约等于1像素
    // 对于2048x2048贴图，0.0005约等于1像素
    const float epsilon = 0.005;
    
    float2 clampMask = step(0.5, clampValues);
    
    // 将UV限制在 [epsilon, 1-epsilon] 范围内
    // 这样可以避免采样到wrap的边缘
    float2 clampedUV = clamp(uv, epsilon, 1.0 - epsilon);
    
    return lerp(uv, clampedUV, clampMask);
}

// Wrap an offset so that after rotation + ST scaling it stays within [-0.5, 0.5) per axis in sampling space.
// This keeps UV values small and avoids precision jitter at high scroll speeds.
float2 PaletteFX_WrapOffsetInSamplingSpace(float2 offset, float2x2 uvRotMat, float2 stDivisor, float2 clampMask)
{
    float2 wrapMask = 1.0 - clampMask;
    float2 deltaSampling = mul(uvRotMat, offset) * stDivisor;
    float2 wrapCount = floor(deltaSampling + 0.5) * wrapMask;
    float2 basisU = mul(transpose(uvRotMat), float2(1.0 / stDivisor.x, 0.0));
    float2 basisV = mul(transpose(uvRotMat), float2(0.0, 1.0 / stDivisor.y));
    return offset - basisU * wrapCount.x - basisV * wrapCount.y;
}

// 🆕 新增：兼容旧代码的简化版本（直接返回UV，遮罩通过参数输出）
float2 ApplyUVClampWithMask(float2 uv, float2 clampValues, out float mask)
{
    mask = CalculateUVClampMask(uv, clampValues);
    return uv;
}

// ==================== 内部UV处理逻辑 ====================

float2 ProcessUV_Internal(float2 uv, float4 st, int mode, float angle, float2 speed, float2 manualOffset, float swirlFactor, float2 clampValues, float2 swirlCenter, float uvAnimSpeedScale, float2 polarCenter)
{
    // NOTE: This is the legacy PaletteFX UV scroll behavior.
    // - Scroll speed is applied in unscaled UV space, then affected by ST tiling when re-applying st.xy.
    // - Scroll direction stays stable relative to the final rotated UV (speed is pre-rotated, then UV is rotated).

    // 1. 转换角度为弧度
    float rad = radians(angle);
    float s = sin(rad);
    float c = cos(rad);

    // 2. 创建流动方向的旋转矩阵（与后续 RotateUV 抵消，从而保持速度方向不受 angle 影响）
    float2x2 flowRotMat = float2x2(c, s, -s, c);

    // 3. 处理UV坐标（移除ST偏移与缩放，得到原始UV空间）
    float2 originalUV = uv;
    float epsilon = 1e-6;
    float2 safeScale = max(abs(st.xy), epsilon);
    float2 scaleSign = sign(st.xy);
    float2 divisor = safeScale * scaleSign;
    originalUV = (uv - st.zw) / divisor;

    // 4. 旋转速度向量，并计算时间偏移
    float2 rotatedSpeed = mul(flowRotMat, speed);
    float2 timeOffset = _Time.yy * rotatedSpeed * uvAnimSpeedScale;

    // Wrap scroll offset on unclamped axes to keep values small (precision stability) without changing sampling.
    float2 clampMask = step(0.5, clampValues);
    float2 stSignForWrap = lerp(-1.0, 1.0, step(0.0, st.xy));
    float2 stDivisorForWrap = max(abs(st.xy), epsilon) * stSignForWrap;
    float2x2 uvRotMat = float2x2(c, -s, s, c);
    timeOffset = PaletteFX_WrapOffsetInSamplingSpace(timeOffset, uvRotMat, stDivisorForWrap, clampMask);
    float2 totalOffset = timeOffset + manualOffset;

    // 5. 根据UV模式应用处理（在原始UV空间内应用 offset）
    originalUV = (mode == UVMODE_POLAR) ? PolarUV(originalUV, float4(1.0, 1.0, totalOffset.x, totalOffset.y), polarCenter) :
        (mode == UVMODE_SWIRL) ? SwirlUV(originalUV, swirlCenter, swirlFactor, totalOffset) :
        originalUV + totalOffset;

    // 6. 对UV坐标应用旋转
    originalUV = RotateUV(originalUV, angle, float2(0.5, 0.5));

    // 7. 重新应用平铺和偏移
    uv = originalUV * st.xy + st.zw;

    // 8. 应用UV截断
    uv = ApplyUVClamp(uv, clampValues);

    return uv;
}

#endif // SHADER_UTILS_INCLUDED
