#ifndef COMMON_TRANSFORM_INCLUDED
#define COMMON_TRANSFORM_INCLUDED

// 顶点位移与旋转(通用)：在物体空间对顶点先绕原点按欧拉角(XYZ, 单位度)旋转, 再叠加位置偏移。
// 与具体效果无关, 任意需要整体平移/旋转的 shader(粒子/精灵/网格)均可 include 复用。
// 旋转顺序与 Unity Transform 欧拉角一致(ZXY); 法线用同一矩阵旋转以保证受光正确。

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// 由欧拉角(度)构造物体空间旋转矩阵(左乘列向量: mul(R, v))。
float3x3 BuildEulerRotationMatrix(float3 eulerDegrees)
{
    float3 r = radians(eulerDegrees);
    float3 s, c;
    sincos(r.x, s.x, c.x);
    sincos(r.y, s.y, c.y);
    sincos(r.z, s.z, c.z);

    float3x3 rotX = float3x3(1,  0,    0,
                             0, c.x, -s.x,
                             0, s.x,  c.x);
    float3x3 rotY = float3x3( c.y, 0, s.y,
                              0,   1, 0,
                             -s.y, 0, c.y);
    float3x3 rotZ = float3x3(c.z, -s.z, 0,
                             s.z,  c.z, 0,
                              0,    0,  1);

    // Unity 欧拉应用顺序 ZXY: R = Ry * Rx * Rz
    return mul(rotY, mul(rotX, rotZ));
}

// 对物体空间顶点位置应用旋转矩阵 + 偏移(先旋转后平移)。
float3 ApplyVertexTransform(float3 positionOS, float3x3 rotation, float3 offset)
{
    return mul(rotation, positionOS) + offset;
}

#endif // COMMON_TRANSFORM_INCLUDED
