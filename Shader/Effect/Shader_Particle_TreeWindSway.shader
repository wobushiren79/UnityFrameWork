Shader "FrameWork/Particle/TreeWindSway"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("树木贴图", 2D) = "white" {}
        [MainColor]   _BaseColor ("染色颜色 (再乘以粒子颜色)", Color) = (1, 1, 1, 1)

        // 表面类型/渲染模式/Alpha 裁剪/渲染面 由通用面板 SurfaceOptionsGUI 合并为"渲染设置"折叠组，
        // 表面类型可设不透明(Opaque)/透明(Transparent)，混合因子/深度写入由预设驱动到 _SrcBlend/_DstBlend/_ZWrite
        [Header(Surface Options)]
        [Enum(Opaque,0,Transparent,1)] _Surface ("表面类型 (0=不透明 / 1=透明 / 默认不透明)", Float) = 0
        [Enum(AlphaBlend,0,Additive,1,PremultipliedAlpha,2)]
        _BlendMode ("渲染模式 (仅透明表面生效 / 0=标准透明 / 1=加法叠加发光 / 2=预乘透明)", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha 裁剪 (按阈值镂空丢弃像素 / 不透明时默认开启)", Float) = 1
        _Cutoff ("裁剪阈值 (低于此 Alpha 的像素被丢弃 / 仅开启裁剪时生效)", Range(0, 1)) = 0.5
        [Enum(On,0,Off,2)] _Cull ("渲染面 (On=双面都显示 / Off=仅显示正面)", Float) = 0
        [HideInInspector] _SrcBlend ("__src", Float) = 1
        [HideInInspector] _DstBlend ("__dst", Float) = 0
        [HideInInspector] _ZWrite ("__zw", Float) = 1

        [Header(Lit)]
        // 勾选=受光(BlinnPhong) / 取消=无光；默认受光(与合并前的 Lit 版一致)。
        // 用 ToggleOff 使"无关键字=受光"，老材质(无关键字)自动保持受光、无需改动。
        [ToggleOff(_UNLIT_ON)] _LitEnable ("开启光照 (勾选=受光 / 取消=无光)", Float) = 1

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _OutlineEnable ("开启描边 (沿轮廓 Alpha 外扩 / 默认关闭)", Float) = 0
        [HDR] _OutlineColor ("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineSize ("描边大小 (向外扩展的纹素数)", Range(0, 10)) = 1.0

        [Header(Soft Particles)]
        [Toggle(_SOFTPARTICLES_ON)] _SoftParticlesEnabled ("柔和粒子 (与场景相交处淡出 / 需开启相机深度纹理)", Float) = 0
        _SoftParticleNearFade ("柔和近端淡出 (开始淡出的相交距离)", Range(0, 10)) = 0.0
        _SoftParticleFarFade  ("柔和远端淡出 (完全显示的相交距离)", Range(0, 10)) = 1.0

        [Header(Camera Fade)]
        [Toggle(_CAMERAFADE_ON)] _CameraFadeEnabled ("相机距离淡出 (靠近相机时淡出)", Float) = 0
        _CameraNearFade ("相机近端淡出 (开始显示的相机距离)", Range(0, 20)) = 1.0
        _CameraFarFade  ("相机远端淡出 (完全显示的相机距离)", Range(0, 20)) = 2.0

        [Header(Position Offset)]
        _PositionOffset ("位置偏移 (XYZ 顶点整体偏移)", Vector) = (0, 0, 0, 0)

        [Header(Wind Sway)]
        _WindSpeed     ("风速 (整体快慢)", Range(0, 10)) = 2.0
        _SwayStrength  ("摆动幅度 (树冠左右摇摆大小)", Range(0, 1)) = 0.08
        _SwayFrequency ("摆动频率 (摇摆快慢)", Range(0, 10)) = 1.5
        _BendStrength  ("弯折幅度 (摆动时树冠下压)", Range(0, 1)) = 0.02

        [Header(Flutter)]
        _FlutterStrength ("抖动幅度 (枝叶细碎颤动大小)", Range(0, 0.5)) = 0.02
        _FlutterSpeed    ("抖动速度 (颤动快慢)", Range(0, 30)) = 12.0

        [Header(Anchor)]
        // 1 = 树干底部固定，越靠树冠摆动越大; 0 = 顶部固定(倒挂)
        _AnchorBottom ("底部锚点 (1=树干在下 / 0=树干在上)", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        // 正向 Pass：受光(默认)与无光(_UNLIT_ON)合并为一个 pass，用 keyword 分支切换
        Pass
        {
            Name "ParticleForward"
            Tags { "LightMode" = "UniversalForward" }

            // 混合因子/深度写入由材质面板"表面类型/渲染模式"预设驱动(不透明=One Zero+写深度, 透明=按模式混合+不写深度)
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _UNLIT_ON
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _SOFTPARTICLES_ON
            #pragma shader_feature_local _CAMERAFADE_ON
            #pragma shader_feature_local _OUTLINE_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // 光照库需在公共/风摆文件之前 include(其内部 Core 带 include guard 不重复)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // 公共贴图/淡出 + 树风摆字段与 ApplyWind(TreeWind 内部已 include 公共文件)
            #include "WindSway/TreeWind.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  color       : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                half3  normalWS    : TEXCOORD3;
                float4 screenPos   : TEXCOORD4;
                half   fogFactor   : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posOS = IN.positionOS.xyz;
                ApplyWind(posOS, IN.uv);

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.screenPos   = posInputs.positionNDC;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color       = IN.color;
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // 贴图 * 染色颜色 * 粒子顶点色(承载生命周期颜色/透明度)
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                #if defined(_OUTLINE_ON)
                    // 描边在采样阶段沿轮廓外扩(须在 clip 前，否则描边像素被裁掉)
                    baseSample = ApplyAlphaOutline(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), baseSample,
                                                   IN.uv, _BaseMap_TexelSize.xy, _OutlineSize, _OutlineColor);
                #endif
                half4 col = baseSample * _BaseColor * IN.color;
                // Alpha 裁剪(镂空)：仅开启 _ALPHATEST_ON 时丢弃低于 _Cutoff 的像素；不透明表面默认开启
                ApplyAlphaClip(col.a, _Cutoff);

                half fade = ParticleFade(IN.screenPos, _SoftParticleNearFade, _SoftParticleFarFade,
                                         _CameraNearFade, _CameraFarFade);

                #if defined(_UNLIT_ON)
                    // ===== 无光：直接输出染色 + 淡出 + 雾 =====
                    col.a *= fade;
                    col.rgb = MixFog(col.rgb, IN.fogFactor);
                    return col;
                #else
                    // ===== 受光：BlinnPhong =====
                    // 粒子公告板法线可能退化，长度过小时退回视线方向，避免光照异常
                    float nLen = length(IN.normalWS);
                    half3 normalWS = nLen > 1e-3 ? (half3)(IN.normalWS / nLen)
                                                 : (half3)GetWorldSpaceNormalizeViewDir(IN.positionWS);
                    // 双面渲染：背面法线翻转，保证两面都能正确受光
                    normalWS *= IS_FRONT_VFACE(cullFace, 1.0, -1.0);

                    SurfaceData surfaceData = (SurfaceData)0;
                    surfaceData.albedo    = col.rgb;
                    surfaceData.alpha     = col.a * fade;
                    surfaceData.occlusion = 1.0;

                    InputData inputData = (InputData)0;
                    inputData.positionWS             = IN.positionWS;
                    inputData.normalWS               = normalWS;
                    inputData.viewDirectionWS        = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                    inputData.shadowCoord            = TransformWorldToShadowCoord(IN.positionWS);
                    inputData.fogCoord               = IN.fogFactor;
                    inputData.bakedGI                = SampleSH(normalWS);
                    inputData.normalizedScreenSpaceUV= GetNormalizedScreenSpaceUV(IN.positionHCS);
                    inputData.shadowMask             = half4(1, 1, 1, 1);

                    half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
                    color.rgb = MixFog(color.rgb, IN.fogFactor);
                    return color;
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
    CustomEditor "WindSwayShaderGUI"
}
