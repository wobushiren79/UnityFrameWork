Shader "Effect/PaletteFX_Project"
{
    Properties
    {
        // 主贴图设置
        [MainTexture] _MainTex ("主贴图", 2D) = "white" {}
        [Enum(R,0,G,1,B,2,A,3,RGBA,4)] _MainTexChannel("通道选择", Float) = 4
        [Enum(UV0,0,UV1,1)] _MainTexSampleUV("采样UV", Float) = 0
        [Enum(Normal,0,Polar,1,Swirl,2)] _UVMode("UV模式", Float) = 0
        _MainTexPolarCenter("主贴图极坐标中心", Vector) = (0.5, 0.5, 0, 0)
        _MainTexSwirlCenter("主贴图漩涡中心", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector]_MainTexUVMode_Polar("极坐标", Float) = 0
        [HideInInspector]_MainTexUVMode_Swirl("漩涡", Float) = 0
        _SwirlFactor ("漩涡强度", Float) = 1.0
        _SwirlRadius ("漩涡半径", Float) = 0.5
        _RotateAngle ("旋转角度", Float) = 0
        [Toggle]_ClampU ("UClamp", Float) = 0
        [Toggle]_ClampV ("VClamp", Float) = 0
        [HDR][MainColor] _Color ("颜色", Color) = (1,1,1,1)
        _Brightness ("亮度", Range(0, 10)) = 1
        _Uspeed ("USpeed", Float) = 0
        _Vspeed ("VSpeed", Float) = 0
        [Enum(Off,0,On,1)]_MainAcceptDistort("接受扭曲", Float) = 1


        // 主贴图遮罩设置
        [Toggle(_MASK_ON)] _EnableMask("启用遮罩", Float) = 0
        _DiffuseMaskTex("主遮罩贴图", 2D) = "white" {}
        [Enum(UV0,0,UV1,1)] _MainTexMaskSampleUV("采样UV", Float) = 0
        [Enum(R,0,G,1,B,2,A,3)] _MaskChannel ("通道选择", Float) = 0
        [Enum(Normal,0,Polar,1,Swirl,2)] _MaskUVMode ("UV模式", Float) = 0
        _MainTexMaskPolarCenter("主贴图遮罩极坐标中心", Vector) = (0.5, 0.5, 0, 0)
        _MainTexMaskSwirlCenter("主贴图遮罩漩涡中心", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector]_MainTexMaskUVMode_Polar("极坐标", Float) = 0
        [HideInInspector]_MainTexMaskUVMode_Swirl("漩涡", Float) = 0
        _MaskSwirlFactor ("漩涡强度", Float) = 1.0
        _DiffuseMaskAng("旋转角度", Float) = 0
        [Toggle]_MaskClampU ("UClamp", Float) = 0
        [Toggle]_MaskClampV ("VClamp", Float) = 0
        [Range(0, 1)]_isReverseMask("反转程度", Float) = 0
        [Enum(Off,0,On,1)]_MaskAcceptDistort("接受扭曲", Float) = 0
        _USpeed_diffusem("USpeed", Float) = 0
        _VSpeed_diffusem("VSpeed", Float) = 0


        // 附加贴图设置
        [Toggle(_ADDTEX_ON)] _EnableAddTex("启用附加图", Float) = 0
        [Enum(Alpha,0,Add,1,Multiply,2)] _AddBlendMode ("混合模式", Float) = 0
        [HideInInspector]_AddBlendMode_Additive("叠加模式", Float) = 0
        [HideInInspector]_AddBlendMode_Multiply("乘法模式", Float) = 0
        _AddTex ("附加图", 2D) = "white" {}
        [Enum(R,0,G,1,B,2,A,3,RGBA,4)] _AddTexChannel("通道选择", Float) = 4
        [Enum(UV0,0,UV1,1)] _AddTexSampleUV("采样UV", Float) = 0
        [Toggle] _AddMulMainAlpha("跟随主透明度", Float) = 0
        [Toggle] _AddMaskUseMainTexR("使用主图R通道作为Mask", Float) = 0
        _AddMaskContrast("Mask对比度", Range(0, 10)) = 1
        [Enum(Normal,0,Polar,1,Swirl,2)] _AddUVMode ("UV模式", Float) = 0
        _AddTexPolarCenter("附加图极坐标中心", Vector) = (0.5, 0.5, 0, 0)
        _AddTexSwirlCenter("附加图漩涡中心", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector]_AddTexUVMode_Polar("极坐标", Float) = 0
        [HideInInspector]_AddTexUVMode_Swirl("漩涡", Float) = 0
        _AddSwirlFactor ("漩涡强度", Float) = 1.0
        _AddRotateAngle ("旋转角度", Float) = 0
        [Toggle]_AddClampU ("UClamp", Float) = 0
        [Toggle]_AddClampV ("VClamp", Float) = 0
        [HDR] _AddColor ("颜色", Color) = (1,1,1,1)
        _AddBrightness ("亮度", Range(0, 10)) = 1
        _AddUspeed ("USpeed", Float) = 0
        _AddVspeed ("VSpeed", Float) = 0
        [Enum(Off,0,On,1)]_AddAcceptDistort("接受扭曲", Float) = 0

        // 附加贴图遮罩设置
        [Toggle(_ADDMASK_ON)] _EnableAddMask("附加图遮罩", Float) = 0
        _AddTexMask("附加图遮罩", 2D) = "white" {}
        [Enum(UV0,0,UV1,1)] _AddTexMaskSampleUV("采样UV", Float) = 0
        [Enum(R,0,G,1,B,2,A,3)] _AddMaskChannel ("通道选择", Float) = 0
        [Enum(Normal,0,Polar,1,Swirl,2)] _AddMaskUVMode ("UV模式", Float) = 0
        _AddTexMaskPolarCenter("附加图遮罩极坐标中心", Vector) = (0.5, 0.5, 0, 0)
        _AddTexMaskSwirlCenter("附加图遮罩漩涡中心", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector]_AddTexMaskUVMode_Polar("极坐标", Float) = 0
        [HideInInspector]_AddTexMaskUVMode_Swirl("漩涡", Float) = 0
        _AddMaskSwirlFactor ("漩涡强度", Float) = 1.0
        _AddMaskRotateAngle("旋转角度", Float) = 0
        [Toggle]_AddMaskClampU ("UClamp", Float) = 0
        [Toggle]_AddMaskClampV ("VClamp", Float) = 0
        [Range(0, 1)]_AddIsReverseMask("反转程度", Float) = 0
        _AddUSpeed_mask("USpeed", Float) = 0
        _AddVSpeed_mask("VSpeed", Float) = 0
        [Enum(Off,0,On,1)]_AddMaskAcceptDistort("接受扭曲", Float) = 0

        // 溶解设置
        [Toggle(_DISSOLVE_ON)] _EnableDissolve("启用溶解", Float) = 0
        _DissolveTex ("溶解贴图", 2D) = "white" {}
        [Enum(UV0,0,UV1,1)] _DissolveTexSampleUV("采样UV", Float) = 0
        [Enum(Normal,0,Polar,1,Swirl,2)] _DissolveUVMode ("UV模式", Float) = 0
        _DissolveTexPolarCenter("溶解贴图极坐标中心", Vector) = (0.5, 0.5, 0, 0)
        _DissolveTexSwirlCenter("溶解贴图漩涡中心", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector]_DissolveTexUVMode_Polar("极坐标", Float) = 0
        [HideInInspector]_DissolveTexUVMode_Swirl("漩涡", Float) = 0
        _DissolveSwirlFactor ("漩涡强度", Float) = 1.0
        _DissolveRotateAngle ("旋转角度", Float) = 0
        [Toggle]_DissolveClampU ("UClamp", Float) = 0
        [Toggle]_DissolveClampV ("VClamp", Float) = 0
        [HDR] _DissolveColor ("边缘颜色", Color) = (1,1,1,1)
        _DissolveEdgeIntensity ("边缘亮度", Range(0, 10)) = 1
        [Enum(R,0,G,1,B,2,A,3)] _DissolveTexChannel ("通道选择", Float) = 0
        _DissolveUspeed ("USpeed", Float) = 0
        _DissolveVspeed ("VSpeed", Float) = 0
        [Range(0, 1)]_DissolveInvert("反转程度", Float) = 0
        _DissolveFactor ("溶解度", Float) = 0
        _DissolveEdgeWidth ("边缘宽度", Range(0, 1)) = 0.1
        _DissolveEdgeSmoothness ("边缘平滑度", Range(0, 1)) = 0.5

        // 溶解附加贴图设置
        [Toggle(_DISSOLVEADD_ON)] _EnableDissolveAdd("启用溶解附加图", Float) = 0
        _DissolveAddTex ("溶解附加图", 2D) = "white" {}
        [Enum(UV0,0,UV1,1)] _DissolveAddTexSampleUV("采样UV", Float) = 0
        [Enum(Normal,0,Polar,1,Swirl,2)] _DissolveAddUVMode ("UV模式", Float) = 0
        _DissolveAddTexPolarCenter("溶解附加图极坐标中心", Vector) = (0.5, 0.5, 0, 0)
        _DissolveAddTexSwirlCenter("溶解附加图漩涡中心", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector]_DissolveTexAddUVMode_Polar("极坐标", Float) = 0
        [HideInInspector]_DissolveTexAddUVMode_Swirl("漩涡", Float) = 0
        _DissolveAddSwirlFactor ("漩涡强度", Float) = 1.0
        _DissolveAddRotateAngle ("旋转角度", Float) = 0
        [Toggle]_DissolveAddClampU ("UClamp", Float) = 0
        [Toggle]_DissolveAddClampV ("VClamp", Float) = 0
        [Enum(R,0,G,1,B,2,A,3)] _DissolveAddTexChannel ("通道选择", Float) = 0
        _DissolveAddUspeed ("USpeed", Float) = 0
        _DissolveAddVspeed ("VSpeed", Float) = 0
        [Range(0, 1)]_DissolveAddInvert("反转程度", Float) = 0

        // 扭曲设置
        [Toggle(_DISTORT_ON)] _EnableDistort("启用扭曲", Float) = 0
        _DistortTex ("扭曲贴图", 2D) = "white" {}
        [Enum(UV0,0,UV1,1)] _DistortTexSampleUV("采样UV", Float) = 0
        [Enum(Normal,0,Polar,1,Swirl,2)] _DistortUVMode ("UV模式", Float) = 0
        _DistortTexPolarCenter("扭曲贴图极坐标中心", Vector) = (0.5, 0.5, 0, 0)
        _DistortTexSwirlCenter("扭曲贴图漩涡中心", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector]_DistortTexUVMode_Polar("极坐标", Float) = 0
        [HideInInspector]_DistortTexUVMode_Swirl("漩涡", Float) = 0
        _DistortSwirlFactor ("漩涡强度", Float) = 1.0
        _DistortRotateAngle ("旋转角度", Float) = 0
        [Toggle]_DistortClampU ("UClamp", Float) = 0
        [Toggle]_DistortClampV ("VClamp", Float) = 0
        [Enum(R,0,G,1,B,2,A,3)] _DistortTexChannel ("贴图通道", Float) = 0
        _DistortUspeed ("USpeed", Float) = 0
        _DistortVspeed ("VSpeed", Float) = 0
        [Range(0, 1)]_DistortInvert("反转程度", Float) = 0
        _DistortStrengthX ("X方向强度", Float) = 0
        _DistortStrengthY ("Y方向强度", Float) = 0

        // 扭曲遮罩设置
        [Toggle(_DISTORTMASK_ON)] _EnableDistortMask("启用扭曲遮罩", Float) = 0
        _DistortMaskTex("扭曲遮罩贴图", 2D) = "white" {}
        [Enum(UV0,0,UV1,1)] _DistortMaskTexSampleUV("采样UV", Float) = 0
        [Enum(R,0,G,1,B,2,A,3)] _DistortMaskChannel ("通道选择", Float) = 0
        [Enum(Normal,0,Polar,1,Swirl,2)] _DistortMaskUVMode ("UV模式", Float) = 0
        _DistortMaskTexPolarCenter("扭曲遮罩极坐标中心", Vector) = (0.5, 0.5, 0, 0)
        _DistortMaskTexSwirlCenter("扭曲遮罩漩涡中心", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector]_DistortTexMaskUVMode_Polar("极坐标", Float) = 0
        [HideInInspector]_DistortTexMaskUVMode_Swirl("漩涡", Float) = 0
        _DistortMaskSwirlFactor ("漩涡强度", Float) = 1.0
        _DistortMaskRotateAngle("旋转角度", Float) = 0
        [Toggle]_DistortMaskClampU ("UClamp", Float) = 0
        [Toggle]_DistortMaskClampV ("VClamp", Float) = 0
        [Range(0, 1)]_DistortMaskInvert("反转程度", Float) = 0
        _DistortMaskUspeed("USpeed", Float) = 0
        _DistortMaskVspeed("VSpeed", Float) = 0

        // FlowMap设置
        [Toggle(_FLOWMAP_ON)] _EnableFlowMap("FlowMap模式", Float) = 0
        _FlowMapIntensity ("强度", Range(0, 1)) = 0.5
        _FlowMapSpeed ("流动速度", Range(-1, 1)) = 0.2
        [Toggle(_ENABLEFLOWMAPLTG_ON)]_EnableFlowMapLTG("启用伽马校正", Float) = 1

        // 菲涅尔设置
        [Toggle(_FRESNEL_ON)] _EnableFresnel("启用菲涅尔", Float) = 0
        [HDR] _FresnelColor("颜色", Color) = (1,1,1,1)
        _FresnelBrightness("亮度", Range(0, 10)) = 1
        _FresnelRange("范围", Float) = 5
        _FresnelIntensity("强度", Float) = 1
        _FresnelEdgeHardness("边缘硬度", Float) = 1
        _FresnelInvert("反转程度", Range(0, 1)) = 0
        [Toggle] _FresnelAsAlpha("菲涅尔透明", Float) = 0

        _FresnelViewOffsetX("视角偏移X", Range(-1, 1)) = 0
        _FresnelViewOffsetY("视角偏移Y", Range(-1, 1)) = 0
        _FresnelViewOffsetZ("视角偏移Z", Range(-1, 1)) = 0

        // 顶点动画设置
        [Toggle(_VERTEXANIM_ON)] _EnableVertexAnim("启用顶点动画", Float) = 0
        _VAPos("方向强度", Vector) = (0,0,0,0)
        [Enum(ModelSpace,0,WorldSpace,1)] _VACoordinateSpace("坐标空间", Float) = 0
        _VANormalInfluence("法线影响", Range(0, 1)) = 1

        // 顶点动画贴图设置
        _VATex("顶点动画贴图", 2D) = "white" {}
        [Enum(UV0,0,UV1,1)] _VATexSampleUV("采样UV", Float) = 0
        [Enum(Normal,0,Polar,1,Swirl,2)] _VATexUVMode("UV模式", Float) = 0
        _VATexPolarCenter("顶点动画贴图极坐标中心", Vector) = (0.5, 0.5, 0, 0)
        _VATexMaskPolarCenter("顶点动画遮罩极坐标中心", Vector) = (0.5, 0.5, 0, 0)
        _VATexSwirlCenter("顶点动画贴图漩涡中心", Vector) = (0.5, 0.5, 0, 0)
        _VATexMaskSwirlCenter("顶点动画遮罩漩涡中心", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector]_VATexUVMode_Polar("极坐标", Float) = 0
        [HideInInspector]_VATexUVMode_Swirl("漩涡", Float) = 0
        _VATexSwirlFactor("漩涡强度", Float) = 1.0
        _VATexRotateAngle("旋转角度", Float) = 0
        [Toggle]_VATexUVClampU("UClamp", Float) = 0
        [Toggle]_VATexUVClampV("VClamp", Float) = 0
        _VATexUspeed("USpeed", Float) = 0
        _VATexVspeed("VSpeed", Float) = 0
        _VATexChannelR("R", Float) = 1
        _VATexChannelG("G", Float) = 1
        _VATexChannelB("B", Float) = 1
        [Range(0, 1)]_VATexInvert("反转程度", Float) = 0

        // 顶点动画贴图遮罩设置
        [Toggle(_VATEXMASK_ON)] _EnableVATexMask("启用顶点动画遮罩", Float) = 0
        _VATexMask("顶点动画遮罩", 2D) = "white" {}
        [Enum(UV0,0,UV1,1)] _VATexMaskSampleUV("采样UV", Float) = 0
        [Enum(R,0,G,1,B,2,A,3)] _VATexMaskChannel("遮罩通道", Float) = 0
        [Enum(Normal,0,Polar,1,Swirl,2)] _VATexMaskUVMode("UV模式", Float) = 0
        [HideInInspector]_VATexMaskUVMode_Polar("极坐标", Float) = 0
        [HideInInspector]_VATexMaskUVMode_Swirl("漩涡", Float) = 0
        _VATexMaskSwirlFactor("漩涡强度", Float) = 1.0
        [Toggle]_VATexMaskUVClampU("UClamp", Float) = 0
        [Toggle]_VATexMaskUVClampV("VClamp", Float) = 0
        _VATexMaskUspeed("USpeed", Float) = 0
        _VATexMaskVspeed("VSpeed", Float) = 0
        _VATexMaskRotateAngle("旋转角度", Range(0, 360)) = 0
        [Range(0, 1)]_VATexMaskInvert("反转程度", Float) = 0

        // 自定义数据设置 (使用CustomData.hlsl中定义的属性)
        [Toggle(_CUSTOMDATA_ON)] _EnableCustomData("启用自定义数据", Float) = 0
        _CustomData1X("自定义数据1 X (主贴图U偏移)", Float) = 0 
        _CustomData1Y("自定义数据1 Y (主贴图V偏移)", Float) = 0 
        _CustomData1Z("自定义数据1 Z (主贴图遮罩U偏移)", Float) = 0 
        _CustomData1W("自定义数据1 W (主贴图遮罩V偏移)", Float) = 0 
        _CustomData2X("自定义数据2 X (溶解度)", Float) = 0 
        _CustomData2Y("自定义数据2 Y (溶解边缘宽度)", Float) = 0 
        _CustomData2Z("自定义数据2 Z (扭曲X)", Float) = 0 
        _CustomData2W("自定义数据2 W (扭曲Y)", Float) = 0

        // 全局控制
        _GlobalAlpha ("全局透明度", Range(0, 1)) = 1
        _GlobalSaturation ("全局饱和度", Range(0, 2)) = 1
        [Toggle]_GlobalUIClipRect("应用UI裁剪", Float) = 1
        // 渲染模式设置
        [Enum(Opaque,0,Transparent,1,Additive,2,Cutout,3)] _BlendMode ("混合模式", Float) = 1
        [HideInInspector]_BlendMode_Additive("叠加模式", Float) = 0
        [HideInInspector]_BlendMode_Cutout("透明裁剪模式", Float) = 0
        _Cutoff ("透明度裁剪", Range(0, 1)) = 0.5
        [Enum(Off,0,On,1)] _ZWrite ("深度写入", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("深度测试", Float) = 4
        _DepthOffsetFactor("深度偏移因子", Float) = 0
        _DepthOffsetUnits("深度偏移单位", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("剔除模式", Float) = 2

        // 混合模式属性 (由脚本控制，不在Inspector中显示)
        [HideInInspector] _SrcBlend ("SrcBlend", Float) = 1
        [HideInInspector] _DstBlend ("DstBlend", Float) = 0

        // 模板测试属性
        _EnableStencilTest ("Enable Stencil Test", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
    }

    SubShader
    {
        Tags {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest[_ZTest]
            Offset[_DepthOffsetFactor],[_DepthOffsetUnits]
            Cull [_Cull]


            // 模板测试配置
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

           
            //互斥变体带_ON
            #pragma shader_feature_local _MASK_ON
            #pragma shader_feature_local _ADDTEX_ON
            #pragma shader_feature_local _ADDMASK_ON
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma shader_feature_local _DISSOLVEADD_ON
            #pragma shader_feature_local _DISTORT_ON
            #pragma shader_feature_local _DISTORTMASK_ON
            #pragma shader_feature_local _FLOWMAP_ON
            #pragma shader_feature_local _ENABLEFLOWMAPLTG_ON
            #pragma shader_feature_local _FRESNEL_ON
            #pragma shader_feature_local _VERTEXANIM_ON
            #pragma shader_feature_local _VATEXMASK_ON
            #pragma shader_feature_local _CUSTOMDATA_ON
            #pragma shader_feature_local _VAWORLDSPACE_ON
            //多选一不带_ON
            #pragma shader_feature_local _ _MAINTEXUVMODE_POLAR _MAINTEXUVMODE_SWIRL
            #pragma shader_feature_local _ _MAINTEXMASKUVMODE_POLAR _MAINTEXMASKUVMODE_SWIRL
            
            #pragma shader_feature_local _ _ADDTEXUVMODE_POLAR _ADDTEXUVMODE_SWIRL
            #pragma shader_feature_local _ _ADDTEXMASKUVMODE_POLAR _ADDTEXMASKUVMODE_SWIRL

            #pragma shader_feature_local _ _DISSOLVETEXUVMODE_POLAR _DISSOLVETEXUVMODE_SWIRL
            #pragma shader_feature_local _ _DISSOLVETEXADDUVMODE_POLAR _DISSOLVETEXADDUVMODE_SWIRL

            #pragma shader_feature_local _ _DISTORTTEXUVMODE_POLAR _DISTORTTEXUVMODE_SWIRL
            #pragma shader_feature_local _ _DISTORTTEXMASKUVMODE_POLAR _DISTORTTEXMASKUVMODE_SWIRL

            #pragma shader_feature_local _ _VATEXUVMODE_POLAR _VATEXUVMODE_SWIRL
            #pragma shader_feature_local _ _VATEXMASKUVMODE_POLAR _VATEXMASKUVMODE_SWIRL

            #pragma shader_feature_local _ _BLENDMODE_ADDITIVE _BLENDMODE_CUTOUT
            #pragma shader_feature_local _ _ADDBLENDMODE_ADDITIVE _ADDBLENDMODE_MULTIPLY

            

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Assets/FrameWork/Shader/Effect/PaletteFX/PaletteFXShader_Project/IncludeFiles/MaterialProperties.hlsl"
            #include "Assets/FrameWork/Shader/Effect/PaletteFX/PaletteFXShader_Project/IncludeFiles/CustomData.hlsl"
            #include "Assets/FrameWork/Shader/Effect/PaletteFX/PaletteFXShader_Project/IncludeFiles/ShaderUtils.hlsl"
            #include "Assets/FrameWork/Shader/Effect/PaletteFX/PaletteFXShader_Project/IncludeFiles/MainTexture.hlsl"
            #include "Assets/FrameWork/Shader/Effect/PaletteFX/PaletteFXShader_Project/IncludeFiles/AdditionalTexture.hlsl"
            #include "Assets/FrameWork/Shader/Effect/PaletteFX/PaletteFXShader_Project/IncludeFiles/DistortEffect.hlsl"
            #include "Assets/FrameWork/Shader/Effect/PaletteFX/PaletteFXShader_Project/IncludeFiles/DissolveEffect.hlsl"
            #include "Assets/FrameWork/Shader/Effect/PaletteFX/PaletteFXShader_Project/IncludeFiles/Fresnel.hlsl"
            #include "Assets/FrameWork/Shader/Effect/PaletteFX/PaletteFXShader_Project/IncludeFiles/VertexAnimation.hlsl"
            #include "Assets/FrameWork/Shader/Effect/PaletteFX/PaletteFXShader_Project/IncludeFiles/StencilTest.hlsl"
            
            

            // 所有材质属性已在 MaterialProperties.hlsl 中定义


            // 所有纹理声明已在 MaterialProperties.hlsl 中定义

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                // 参考 ChameleonX：粒子 UV2 会打包到 TEXCOORD0.zw（Custom Vertex Streams 里的 UV2）
                float4 uv : TEXCOORD0;
                float4 color : COLOR;
            #if _CUSTOMDATA_ON
                float4 customData1 : TEXCOORD1;
                float4 customData2 : TEXCOORD2;
            #else
                float2 uv1 : TEXCOORD1;
            #endif
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float2 maskUV : TEXCOORD1; // 主贴图遮罩UV坐标
                float2 addUV : TEXCOORD2; // 附加贴图UV坐标
                float2 addMaskUV : TEXCOORD3; // 附加贴图遮罩UV坐标
                float2 dissolveUV : TEXCOORD4; // 溶解贴图UV坐标
                float2 dissolveAddUV : TEXCOORD5; // 溶解附加贴图UV坐标
                float2 distortUV : TEXCOORD6; // 扭曲贴图UV坐标
                float2 distortMaskUV : TEXCOORD7; // 扭曲遮罩贴图UV坐标
                float3 normalWS : TEXCOORD8; // 世界空间法线 - 用于菲涅尔计算
                float3 viewDirWS : TEXCOORD9; // 世界空间视角方向 - 用于菲涅尔计算
                #if _CUSTOMDATA_ON
                    float4 customData1 : TEXCOORD10;
                    float4 customData2 : TEXCOORD11;
                #endif
                float4 clipmask : TEXCOORD12;
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };




            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                #if _CUSTOMDATA_ON
                    // 传递自定义数据到片元着色器
                    output.customData1 = input.customData1;
                    output.customData2 = input.customData2;
                #endif

                // 应用顶点动画
                float3 positionOS = input.positionOS.xyz;
                float2 uv0 = input.uv.xy;
            #if _CUSTOMDATA_ON
                float2 uv1 = input.uv.zw;
            #else
                float2 uv1 = input.uv1.xy;
            #endif

                #if defined(_VERTEXANIM_ON)
                    // 计算顶点动画偏移
                    float3 vertexOffset = ApplyVertexAnimation(positionOS, input.normalOS, uv0, uv1);

                    // 应用顶点偏移
                    positionOS += vertexOffset;
                #endif

                // 计算世界空间位置
                float3 positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);

                // 计算世界空间法线 - 用于菲涅尔计算
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                // 计算世界空间视角方向 - 用于菲涅尔计算
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);

                // 主贴图UV,使用通用UV选择函数
                float2 selectedMainUV = SelectUVChannel(uv0, uv1, _MainTexSampleUV);
                output.uv = selectedMainUV * _MainTex_ST.xy + _MainTex_ST.zw;

                // 主贴图遮罩UV - 使用通用UV选择函数和独立的缩放和偏移
                float2 selectedMaskUV = SelectUVChannel(uv0, uv1, _MainTexMaskSampleUV);
                output.maskUV = selectedMaskUV * _DiffuseMaskTex_ST.xy + _DiffuseMaskTex_ST.zw;

                // 附加贴图UV - 使用通用UV选择函数
                float2 selectedAddUV = SelectUVChannel(uv0, uv1, _AddTexSampleUV);
                output.addUV = selectedAddUV * _AddTex_ST.xy + _AddTex_ST.zw;

                // 附加贴图遮罩UV - 使用通用UV选择函数
                float2 selectedAddMaskUV = SelectUVChannel(uv0, uv1, _AddTexMaskSampleUV);
                output.addMaskUV = selectedAddMaskUV * _AddTexMask_ST.xy + _AddTexMask_ST.zw;

                // 溶解贴图UV - 使用通用UV选择函数
                float2 selectedDissolveUV = SelectUVChannel(uv0, uv1, _DissolveTexSampleUV);
                output.dissolveUV = selectedDissolveUV * _DissolveTex_ST.xy + _DissolveTex_ST.zw;

                // 溶解附加贴图UV - 使用通用UV选择函数
                float2 selectedDissolveAddUV = SelectUVChannel(uv0, uv1, _DissolveAddTexSampleUV);
                output.dissolveAddUV = selectedDissolveAddUV * _DissolveAddTex_ST.xy + _DissolveAddTex_ST.zw;

                // 扭曲贴图UV - 使用通用UV选择函数
                float2 selectedDistortUV = SelectUVChannel(uv0, uv1, _DistortTexSampleUV);
                output.distortUV = selectedDistortUV * _DistortTex_ST.xy + _DistortTex_ST.zw;

                // 扭曲遮罩贴图UV - 使用通用UV选择函数
                float2 selectedDistortMaskUV = SelectUVChannel(uv0, uv1, _DistortMaskTexSampleUV);
                output.distortMaskUV = selectedDistortMaskUV * _DistortMaskTex_ST.xy + _DistortMaskTex_ST.zw;

                output.color = input.color;

                float4 vPosition = TransformWorldToHClip(positionWS);
                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (input.positionOS.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                output.clipmask = float4(input.positionOS.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 自定义数据（参考 ChameleonX：Custom1/2 直接来自粒子 Custom Vertex Streams）
                float4 customData1;
                float4 customData2;
            #if _CUSTOMDATA_ON
                customData1 = input.customData1;
                customData2 = input.customData2;
            #else
                customData1 = float4(_CustomData1X, _CustomData1Y, _CustomData1Z, _CustomData1W);
                customData2 = float4(_CustomData2X, _CustomData2Y, _CustomData2Z, _CustomData2W);
            #endif

                CustomDataParams dataParams;
                dataParams.customData1 = customData1;
                dataParams.customData2 = customData2;

                // 应用扭曲效果
                float2 distortedUV = ProcessDistort(input.uv, input.distortUV, input.distortMaskUV, dataParams);

                float2 distortedMaskUV;
            #if _MASK_ON
                distortedMaskUV = _MaskAcceptDistort > 0.5 ? ProcessDistort(input.maskUV, input.distortUV, input.distortMaskUV, dataParams) : input.maskUV;
            #else
                distortedMaskUV = input.maskUV;
            #endif

                float2 distortedAddUV = _AddAcceptDistort > 0.5 ? ProcessDistort(input.addUV, input.distortUV, input.distortMaskUV, dataParams) : input.addUV;
                float2 distortedAddMaskUV = _AddMaskAcceptDistort > 0.5 ? ProcessDistort(input.addMaskUV, input.distortUV, input.distortMaskUV, dataParams) : input.addMaskUV;
                float2 distortedDissolveUV = ProcessDistort(input.dissolveUV, input.distortUV, input.distortMaskUV, dataParams);
                float2 distortedDissolveAddUV = ProcessDistort(input.dissolveAddUV, input.distortUV, input.distortMaskUV, dataParams);

                // 为主贴图的UV创建一个独立变量
                float2 mainTexFinalUV = _MainAcceptDistort > 0.5 ? distortedUV : input.uv;

                float2 mainCustomOffset = float2(0.0, 0.0);
                float2 mainMaskCustomOffset = float2(0.0, 0.0);
            #if _CUSTOMDATA_ON
                // 纯粹 Custom1：
                // - Custom1.xy：仅用于主贴图 UV 偏移
                // - Custom1.zw：仅用于主贴图遮罩 UV 偏移（与“接受扭曲”开关无关）
                mainCustomOffset = customData1.xy;
                mainMaskCustomOffset = customData1.zw;
            #endif

                half mainTexR = 0.0h;  // 声明变量存储主贴图R通道

                // 修改主贴图处理
#if _MASK_ON
                half4 mainCol = ProcessMainTexture(mainTexFinalUV, distortedMaskUV, true, mainTexR, mainCustomOffset, mainMaskCustomOffset);
#else
                half4 mainCol = ProcessMainTexture(mainTexFinalUV, mainTexFinalUV, false, mainTexR, mainCustomOffset, float2(0.0, 0.0));
#endif

                // 修改附加贴图处理，传入mainTexR
                half4 finalCol = ProcessAdditionalTexture(mainCol, distortedAddUV, distortedAddMaskUV, mainTexR);

                // 应用溶解效果
            #if _DISSOLVE_ON
                finalCol = ApplyDissolve(finalCol, distortedDissolveUV, distortedDissolveAddUV, dataParams);
            #endif

                // 应用菲涅尔效果
            #if _FRESNEL_ON
                float3 viewOffset = float3(_FresnelViewOffsetX, _FresnelViewOffsetY, _FresnelViewOffsetZ);
                bool useFresnelAsAlpha = _FresnelAsAlpha > 0.5;
                ProcessFresnel(finalCol, input.normalWS, input.viewDirWS, input.color, customData1,
                              viewOffset, _FresnelRange, _FresnelIntensity, _FresnelEdgeHardness,
                              _FresnelInvert, 0, _FresnelColor, _FresnelBrightness, useFresnelAsAlpha);
            #endif

                finalCol *= input.color;

                // ✅ 删除：不再统一应用UV Clamp遮罩
                // float combinedClampMask = mainUVClampMask * mainMaskUVClampMask * addUVClampMask * addMaskUVClampMask;
                // finalCol.a *= combinedClampMask;

                // 应用全局饱和度
                finalCol.rgb = AdjustSaturation(finalCol.rgb, _GlobalSaturation);

                // 应用全局透明度
                finalCol.a *= _GlobalAlpha;

                // UI裁剪
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.clipmask.xy)) * input.clipmask.zw);
                finalCol.a = finalCol.a * (m.x * m.y * _GlobalUIClipRect + (1 - _GlobalUIClipRect));

                // 最终统一执行clip（只在Cutout模式下）
            #if defined(_BLENDMODE_CUTOUT)
                clip(finalCol.a - _Cutoff);
                return finalCol;
            #elif defined(_BLENDMODE_ADDITIVE)
                finalCol.rgb *= finalCol.a;
                return finalCol;
            #else
                return finalCol;
            #endif
            }
            ENDHLSL
        }
    }

    //FallBack "Standard"
    CustomEditor "PaletteFXShaderGUI_Project"
}
