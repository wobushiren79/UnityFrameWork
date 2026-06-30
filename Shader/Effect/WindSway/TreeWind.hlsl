#ifndef WINDSWAY_TREE_INCLUDED
#define WINDSWAY_TREE_INCLUDED

// 树风摆：材质字段(公共 + 树特有) + 风摆位移算法。
// 树 Unlit / 树 Lit 两个 shader 共享本文件，改树风摆只需改这里。
#include "../../Common/ParticleCommon.hlsl"

CBUFFER_START(UnityPerMaterial)
    PARTICLE_COMMON_CBUFFER
    half _WindSpeed;
    half _SwayStrength;
    half _SwayFrequency;
    half _BendStrength;
    half _FlutterStrength;
    half _FlutterSpeed;
    half _AnchorBottom;
CBUFFER_END

// 风摆顶点位移：以树干底部为锚点，越靠树冠摆动越大(每个粒子按世界坐标错相位)
void ApplyWind(inout float3 positionOS, float2 uv)
{
    float heightWeight = lerp(uv.y, 1.0 - uv.y, 1.0 - _AnchorBottom);

    float3 positionWS = TransformObjectToWorld(positionOS);
    float phase = positionWS.x * 0.5 + positionWS.z * 0.5;

    float t = _Time.y * _WindSpeed;

    float swayWave = sin(t * _SwayFrequency + phase);
    float sway = swayWave * _SwayStrength * heightWeight * heightWeight;

    float flutter = sin(t * _FlutterSpeed + phase * 3.0 + uv.x * 6.2831) * _FlutterStrength * heightWeight;

    float bend = -abs(swayWave) * _BendStrength * heightWeight;

    positionOS.x += sway + flutter;
    positionOS.y += bend;
    positionOS += _PositionOffset.xyz;
}

#endif // WINDSWAY_TREE_INCLUDED
