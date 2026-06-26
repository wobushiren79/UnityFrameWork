#ifndef MATERIAL_PROPERTIES_INCLUDED
#define MATERIAL_PROPERTIES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// 主贴图纹理声明
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

// 主贴图遮罩纹理声明
TEXTURE2D(_DiffuseMaskTex);
SAMPLER(sampler_DiffuseMaskTex);

// 附加贴图纹理声明
TEXTURE2D(_AddTex);
SAMPLER(sampler_AddTex);
TEXTURE2D(_AddTexMask);
SAMPLER(sampler_AddTexMask);

// 溶解贴图纹理声明
TEXTURE2D(_DissolveTex);
SAMPLER(sampler_DissolveTex);
TEXTURE2D(_DissolveAddTex);
SAMPLER(sampler_DissolveAddTex);

// 扭曲贴图纹理声明
TEXTURE2D(_DistortTex);
SAMPLER(sampler_DistortTex);
TEXTURE2D(_DistortMaskTex);
SAMPLER(sampler_DistortMaskTex);

// 所有材质属性放在CBUFFER中
CBUFFER_START(UnityPerMaterial)
    // 全局属性
    float _Cutoff;
    float _GlobalUIClipRect;
    int _BlendMode;
    float _BlendMode_Additive;
    float _BlendMode_Cutout;
    float _GlobalAlpha;
    float _GlobalSaturation;

    // 主贴图属性
    float4 _MainTex_ST;
    half4 _Color;
    float _MainTexSampleUV;
    float _Uspeed;
    float _Vspeed;
    float _RotateAngle;
    float _Brightness;
    int   _UVMode;
    int _MainTexChannel;
    float _MainTexUVMode_Polar;
    float _MainTexUVMode_Swirl;
    float _SwirlFactor;
    float _ClampU;
    float _ClampV;
    float _MainAcceptDistort;

    // 主贴图遮罩属性
    float _EnableMask;
    float4 _DiffuseMaskTex_ST;
    float _MainTexMaskSampleUV;
    float _USpeed_diffusem;
    float _VSpeed_diffusem;
    float _DiffuseMaskAng;
    float _DiffuseMaskRo;
    float _isReverseMask;
    int _MaskUVMode;
    float _MainTexMaskUVMode_Polar;
    float _MainTexMaskUVMode_Swirl;
    float _MaskSwirlFactor;
    float _MaskClampU;
    float _MaskClampV;
    int _MaskChannel;
    float _MaskAcceptDistort;

    // 主贴图极坐标中心点
    float2 _MainTexPolarCenter;
    float2 _MainTexSwirlCenter;
    float2 _MainTexMaskPolarCenter;
    float2 _MainTexMaskSwirlCenter;

    // 附加贴图属性
    float _EnableAddTex;
    float4 _AddTex_ST;
    half4 _AddColor;
    float _AddTexSampleUV;
    float _AddUspeed;
    float _AddVspeed;
    float _AddRotateAngle;
    float _AddBrightness;
    float _AddAcceptDistort;
    float _AddMulMainAlpha;
    float _AddBlendChannel;
    int _AddUVMode;
    int _AddTexChannel;
    float _AddTexUVMode_Polar;
    float _AddTexUVMode_Swirl;
    float _AddSwirlFactor;
    float _AddClampU;
    float _AddClampV;
    int _AddBlendMode;
    float _AddBlendMode_Additive;
    float _AddBlendMode_Multiply;
    float _AddMaskUseMainTexR;
    float _AddMaskContrast;
    // 附加贴图遮罩属性
    float _EnableAddMask;
    float4 _AddTexMask_ST;
    float _AddTexMaskSampleUV;
    float _AddUSpeed_mask;
    float _AddVSpeed_mask;
    float _AddMaskRotateAngle;
    float _AddIsReverseMask;
    int _AddMaskUVMode;
    float _AddTexMaskUVMode_Polar;
    float _AddTexMaskUVMode_Swirl;
    float _AddMaskSwirlFactor;
    float _AddMaskClampU;
    float _AddMaskClampV;
    int _AddMaskChannel;
    float _AddMaskAcceptDistort;

    // 附加贴图极坐标中心点  
    float2 _AddTexPolarCenter;
    float2 _AddTexSwirlCenter;
    float2 _AddTexMaskPolarCenter;
    float2 _AddTexMaskSwirlCenter;

    // 溶解属性
    float _EnableDissolve;
    float4 _DissolveTex_ST;
    half4 _DissolveColor;
    float _DissolveTexSampleUV;
    float _DissolveEdgeIntensity;
    float _DissolveUspeed;
    float _DissolveVspeed;
    float _DissolveRotateAngle;
    float _DissolveInvert;
    float _DissolveFactor;
    float _DissolveEdgeWidth;
    float _DissolveEdgeSmoothness;
    int _DissolveTexChannel;
    int _DissolveUVMode;
    float _DissolveTexUVMode_Polar;
    float _DissolveTexUVMode_Swirl;
    float _DissolveSwirlFactor;
    float _DissolveClampU;
    float _DissolveClampV;

    // 溶解附加贴图属性
    float _EnableDissolveAdd;
    float4 _DissolveAddTex_ST;
    float _DissolveAddTexSampleUV;
    float _DissolveAddRotateAngle;
    int _DissolveAddTexChannel;
    int _DissolveAddUVMode;
    float _DissolveTexAddUVMode_Polar;
    float _DissolveTexAddUVMode_Swirl;
    float _DissolveAddSwirlFactor;
    float _DissolveAddClampU;
    float _DissolveAddClampV;
    float _DissolveAddUspeed;
    float _DissolveAddVspeed;
    float _DissolveAddInvert;
    // 溶解贴图极坐标中心点
    float2 _DissolveTexPolarCenter;
    float2 _DissolveTexSwirlCenter;
    float2 _DissolveAddTexPolarCenter;
    float2 _DissolveAddTexSwirlCenter;

    // 扭曲属性
    float _EnableDistort;
    float4 _DistortTex_ST;
    float _DistortTexSampleUV;
    int _DistortTexChannel;
    int _DistortUVMode;
    float _DistortTexUVMode_Polar;
    float _DistortTexUVMode_Swirl;
    float _DistortSwirlFactor;
    float _DistortRotateAngle;
    float _DistortClampU;
    float _DistortClampV;
    float _DistortUspeed;
    float _DistortVspeed;
    float _DistortInvert;
    float _DistortStrengthX;
    float _DistortStrengthY;

    // 扭曲遮罩属性
    float _EnableDistortMask;
    float4 _DistortMaskTex_ST;
    float _DistortMaskTexSampleUV;
    int _DistortMaskChannel;
    int _DistortMaskUVMode;
    float _DistortTexMaskUVMode_Polar;
    float _DistortTexMaskUVMode_Swirl;
    float _DistortMaskSwirlFactor;
    float _DistortMaskRotateAngle;
    float _DistortMaskClampU;
    float _DistortMaskClampV;
    float _DistortMaskInvert;
    float _DistortMaskUspeed;
    float _DistortMaskVspeed;
    // 扭曲贴图极坐标中心点
    float2 _DistortTexPolarCenter;
    float2 _DistortTexSwirlCenter;
    float2 _DistortMaskTexPolarCenter;
    float2 _DistortMaskTexSwirlCenter;

    // FlowMap属性
    float _EnableFlowMap;
    float _FlowMapIntensity;
    float _FlowMapSpeed;
    float _EnableFlowMapLTG;

    // 菲涅尔属性
    float _EnableFresnel;
    float4 _FresnelColor;
    float _FresnelRange;
    float _FresnelIntensity;
    float _FresnelBrightness;
    float _FresnelEdgeHardness;
    float _FresnelInvert;
    float _FresnelAsAlpha;
    float _FresnelViewOffsetX;
    float _FresnelViewOffsetY;
    float _FresnelViewOffsetZ;

    // 顶点动画开关属性
    float _EnableVertexAnim;
    float _EnableVATexMask;
    // 顶点动画贴图极坐标中心点
    float2 _VATexPolarCenter;
    float2 _VATexMaskPolarCenter;
    float2 _VATexSwirlCenter;
    float2 _VATexMaskSwirlCenter;

    // 自定义数据属性
    float _EnableCustomData;
    float _CustomData1X;
    float _CustomData1Y;
    float _CustomData1Z;
    float _CustomData1W;
    float _CustomData2X;
    float _CustomData2Y;
    float _CustomData2Z;
    float _CustomData2W;
CBUFFER_END

float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float4 _ClipRect;

#endif // MATERIAL_PROPERTIES_INCLUDED
