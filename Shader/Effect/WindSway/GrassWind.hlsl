#ifndef WINDSWAY_GRASS_INCLUDED
#define WINDSWAY_GRASS_INCLUDED

// 草风摆：材质字段(公共 + 草特有) + 风摆位移算法。
// 草 Unlit / 草 Lit 两个 shader 共享本文件，改草风摆只需改这里。
#include "../../Common/ParticleCommon.hlsl"

CBUFFER_START(UnityPerMaterial)
    PARTICLE_COMMON_CBUFFER
    half _WindSpeed;
    half _SwayStrength;
    half _SwayFrequency;
    half _WindDir;
    half _BendStrength;
    half _FlutterStrength;
    half _FlutterSpeed;
    half _Stiffness;
CBUFFER_END

// 风摆顶点位移：根部(UV.y=0)固定，越往草尖摆动越大(每个粒子按世界坐标错相位)
void ApplyWind(inout float3 positionOS, float2 uv)
{
    // pow(uv.y, _Stiffness) 控制草茎硬度：值越大根部附近越不弯、越像硬草茎
    float heightWeight = pow(saturate(uv.y), _Stiffness);

    float3 positionWS = TransformObjectToWorld(positionOS);
    float phase = positionWS.x * 0.7 + positionWS.z * 0.7;

    float t = _Time.y * _WindSpeed;

    float swayWave = sin(t * _SwayFrequency + phase);
    float sway = swayWave * _SwayStrength;

    float bend = _WindDir * _BendStrength * (0.75 + 0.25 * sin(t * 0.5 + phase));

    float flutter = sin(t * _FlutterSpeed + phase * 3.0 + uv.x * 6.2831) * _FlutterStrength;

    float offsetX = (sway + bend + flutter) * heightWeight;
    float offsetY = -abs(offsetX) * 0.3;

    positionOS.x += offsetX;
    positionOS.y += offsetY;
    positionOS += _PositionOffset.xyz;
}

#endif // WINDSWAY_GRASS_INCLUDED
