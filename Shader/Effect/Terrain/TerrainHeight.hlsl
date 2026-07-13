#ifndef TERRAIN_HEIGHT_INCLUDED
#define TERRAIN_HEIGHT_INCLUDED

// 高度图地形位移件：把一块细分平面(默认躺在物体空间 XZ 平面、up=+Y)按灰度高度图
// 顶点位移出山坡起伏，并用有限差分从高度梯度重算法线(坡面明暗全靠这个法线)。
// 与具体地表无关，任意"平面细分网格 + 高度图"的地形/雪堆/沙丘均可 include 复用。
// 纹理用 TEXTURE2D_PARAM 参数化传入(不写死贴图名)，故不绑定某张具体高度图。

// 采样高度图灰度[0,1]。顶点阶段无导数，须用 LOD0 采样。
// invert: 0=白为高(默认)/1=白为低, 反转须作用于每次采样以保证法线梯度一致。
float SampleTerrainHeight(TEXTURE2D_PARAM(heightMap, samplerHeightMap), float2 uv, float invert)
{
    float h = SAMPLE_TEXTURE2D_LOD(heightMap, samplerHeightMap, uv, 0).r;
    return lerp(h, 1.0 - h, invert);
}

// 按高度图位移顶点并重算物体空间法线。
// heightMap/samplerHeightMap: 高度图与采样器; positionOS: 原始物体坐标(平面);
// uv: 高度图 UV(网格 0-1); texel: _HeightMap_TexelSize.xy(单纹素 UV 尺寸);
// heightScale: 向上位移的世界高度; sampleDist: 求法线的相邻采样间距(纹素数, 越大越平滑);
// normalStrength: 坡度光照强度(放大梯度→坡面明暗对比更强); invert: 0=白高/1=白低;
// outNormalOS: 输出重算后的法线; outHeight01: 输出中心点归一化高度[0,1](供按海拔染色)。
// 返回位移后的物体坐标。
float3 ApplyTerrainHeight(TEXTURE2D_PARAM(heightMap, samplerHeightMap), float3 positionOS,
                          float2 uv, float2 texel, float heightScale, float sampleDist,
                          float normalStrength, float invert, out float3 outNormalOS, out float outHeight01)
{
    // 中心高度(按 invert 反转) → 沿物体 +Y 位移
    float h = SampleTerrainHeight(heightMap, samplerHeightMap, uv, invert);
    positionOS.y += h * heightScale;
    outHeight01 = h;

    // 有限差分:取左右/上下相邻高度(同样反转),由梯度构造法线(right 更高→法线朝左倾)
    float2 off = texel * sampleDist;
    float hL = SampleTerrainHeight(heightMap, samplerHeightMap, uv - float2(off.x, 0), invert);
    float hR = SampleTerrainHeight(heightMap, samplerHeightMap, uv + float2(off.x, 0), invert);
    float hD = SampleTerrainHeight(heightMap, samplerHeightMap, uv - float2(0, off.y), invert);
    float hU = SampleTerrainHeight(heightMap, samplerHeightMap, uv + float2(0, off.y), invert);

    float3 n;
    n.x = (hL - hR) * heightScale * normalStrength;
    n.z = (hD - hU) * heightScale * normalStrength;
    n.y = 1.0;
    outNormalOS = normalize(n);
    return positionOS;
}

#endif // TERRAIN_HEIGHT_INCLUDED
