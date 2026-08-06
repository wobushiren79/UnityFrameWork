#ifndef CUSTOM_DATA_INCLUDED
#define CUSTOM_DATA_INCLUDED

// 自定义数据相关属性定义
#define CUSTOM_DATA_PROPERTIES \
        [Toggle(_CUSTOMDATA_ON)] _EnableCustomData("启用自定义数据", Float) = 0 \
        _CustomData1X("自定义数据1 X (主贴图U偏移)", Float) = 0 \
        _CustomData1Y("自定义数据1 Y (主贴图V偏移)", Float) = 0 \
        _CustomData1Z("自定义数据1 Z (主贴图遮罩U偏移)", Float) = 0 \
        _CustomData1W("自定义数据1 W (主贴图遮罩V偏移)", Float) = 0 \
        _CustomData2X("自定义数据2 X (溶解度)", Float) = 0 \
        _CustomData2Y("自定义数据2 Y (溶解边缘宽度)", Float) = 0 \
        _CustomData2Z("自定义数据2 Z (扭曲X)", Float) = 0 \
        _CustomData2W("自定义数据2 W (扭曲Y)", Float) = 0

// 自定义数据结构体
struct CustomDataParams
{
    float4 customData1;
    float4 customData2;
};

// 初始化自定义数据
CustomDataParams InitCustomData()
{
    CustomDataParams params;
    params.customData1 = float4(0, 0, 0, 0);
    params.customData2 = float4(0, 0, 0, 0);
    return params;
}

// 从材质属性加载自定义数据
CustomDataParams LoadCustomDataFromProperties(float _CustomData1X, float _CustomData1Y, float _CustomData1Z, float _CustomData1W,
                                             float _CustomData2X, float _CustomData2Y, float _CustomData2Z, float _CustomData2W)
{
    CustomDataParams params;
    params.customData1 = float4(_CustomData1X, _CustomData1Y, _CustomData1Z, _CustomData1W);
    params.customData2 = float4(_CustomData2X, _CustomData2Y, _CustomData2Z, _CustomData2W);
    return params;
}

// 从顶点输入加载自定义数据
CustomDataParams LoadCustomDataFromVertex(float4 texcoord1, float4 texcoord2)
{
    CustomDataParams params;
    params.customData1 = texcoord1;
    params.customData2 = texcoord2;
    return params;
}

// 应用自定义数据到UV偏移
float2 ApplyCustomDataToUV(float2 uv, CustomDataParams customData)
{
    return uv + customData.customData1.xy;
}

// 应用自定义数据到MaskUV
float2 ApplyCustomDataToMaskUV(float2 maskuv, CustomDataParams customData)
{
    return maskuv + customData.customData1.zw;
}


// 应用自定义数据到溶解度
float ApplyCustomDataToDissolveFactor(float factor, CustomDataParams customData)
{
    return factor + customData.customData2.x;
}

// 应用自定义数据到溶解宽度
float ApplyCustomDataToDissolveEdgeWidth(float edgewidth, CustomDataParams customData)
{
    // 参考 ChameleonX：Custom2.y 以加法方式控制边缘宽度
    return edgewidth + customData.customData2.y;
}

// 应用自定义数据到扭曲速度
float2 ApplyCustomDataToDistortStrength(float2 distortstrength, CustomDataParams customData)
{
    // 参考 ChameleonX：Custom2.z/w 以加法方式控制 X/Y 扭曲强度
    distortstrength.x += customData.customData2.z;
    distortstrength.y += customData.customData2.w;
    return distortstrength;
}

#endif // CUSTOM_DATA_INCLUDED
