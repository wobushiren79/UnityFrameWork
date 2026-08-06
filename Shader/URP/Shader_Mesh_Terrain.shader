Shader "FrameWork/URP/MeshTerrain"
{
    Properties
    {
        [Header(Height Displacement)]
        [NoScaleOffset] _HeightMap ("高度图 (灰度决定地形起伏)", 2D) = "black" {}
        [ToggleUI] _HeightInvert ("高度反转 (关=白为高黑为低 / 开=白为低黑为高)", Float) = 0
        _HeightScale ("起伏高度 (顶点向上位移的世界高度)", Float) = 5.0
        _NormalStrength ("坡度光照强度 (坡面明暗对比, 越大越陡峭感)", Range(0, 8)) = 1.0
        _NormalSampleDistance ("法线采样间距 (纹素数, 越大坡面越平滑)", Range(0.5, 8)) = 1.0

        [Header(Surface)]
        [MainTexture] _BaseMap ("地表贴图 (草地/岩石, 可平铺)", 2D) = "white" {}
        [MainColor]   _BaseColor ("地表染色", Color) = (1, 1, 1, 1)

        [Header(Height Color)]
        [Toggle(_HEIGHT_COLOR_ON)] _HeightColorEnable ("开启高度染色 (按海拔在低处色-高处色间混合)", Float) = 0
        _LowColor ("低处颜色 (山谷)", Color) = (0.6, 0.75, 0.45, 1)
        _HighColor ("高处颜色 (山顶)", Color) = (0.9, 0.95, 0.85, 1)
        _HeightColorRange ("高度染色区间 (归一化 x=起 y=止)", Vector) = (0, 1, 0, 0)

        [Header(Lit)]
        [Toggle(_LIT_ON)] _LitEnable ("开启光照 (受主光/附加光/阴影/环境光, 坡面明暗靠它 / 默认开启)", Float) = 1

        [Header(Render Face)]
        [Enum(On,0,Off,2)] _Cull ("是否开启双面渲染 (On=两面都显示 / Off=仅显示正面)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        // 所有 Pass 共用：材质参数 + 贴图声明 + 高度位移助手
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "../Effect/Terrain/TerrainHeight.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_HeightMap);
        SAMPLER(sampler_HeightMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _HeightMap_TexelSize;   // Unity 按高度图自动填充, 供有限差分取纹素尺寸
            half4  _BaseColor;
            float  _HeightInvert;          // 0=白为高 / 1=白为低
            float  _HeightScale;
            float  _NormalStrength;
            float  _NormalSampleDistance;
            half4  _LowColor;
            half4  _HighColor;
            float4 _HeightColorRange;
        CBUFFER_END
        ENDHLSL

        // 正向 Pass：高度图位移 + 重算法线受光(坡面明暗) + 可选高度染色
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma shader_feature_local _LIT_ON
            #pragma shader_feature_local _HEIGHT_COLOR_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            // 光照库(SurfaceData/InputData/BlinnPhong) → 通用受光助手
            #include "../Common/CommonLit.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                half3  normalWS    : TEXCOORD2;
                half   fogFactor   : TEXCOORD3;
                half   height01    : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // 按高度图位移顶点并重算物体空间法线, uv 用网格原始 UV(0-1)采高度图
                float3 normalOS;
                float  height01;
                float3 positionOS = ApplyTerrainHeight(TEXTURE2D_ARGS(_HeightMap, sampler_HeightMap),
                                                       IN.positionOS.xyz, IN.uv, _HeightMap_TexelSize.xy,
                                                       _HeightScale, _NormalSampleDistance,
                                                       _NormalStrength, _HeightInvert, normalOS, height01);

                VertexPositionInputs posInputs = GetVertexPositionInputs(positionOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);   // 地表贴图独立平铺
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                OUT.height01    = height01;
                return OUT;
            }

            half4 frag (Varyings IN, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                #if defined(_HEIGHT_COLOR_ON)
                    // 按海拔在低处色-高处色间混合, 叠加到地表色上
                    half t = smoothstep(_HeightColorRange.x, _HeightColorRange.y, IN.height01);
                    col.rgb *= lerp(_LowColor.rgb, _HighColor.rgb, t);
                #endif

                #if defined(_LIT_ON)
                    // 双面渲染时背面法线翻转, 保证两面都正确受光
                    half3 normalWS = normalize(IN.normalWS) * IS_FRONT_VFACE(cullFace, 1.0, -1.0);
                    return ApplyCommonLit(col.rgb, 1.0, IN.positionWS, normalWS, IN.positionHCS, IN.fogFactor);
                #else
                    col.rgb = MixFog(col.rgb, IN.fogFactor);
                    return col;
                #endif
            }
            ENDHLSL
        }

        // 阴影投射 Pass：同步高度位移, 让山脊在山谷上投出自阴影
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull [_Cull]
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 复刻 URP ShadowCasterPass 的偏移 + 近裁剪夹紧逻辑
            float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
            {
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            Varyings ShadowPassVertex (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);

                // 与 Forward 一致地位移+重算法线, 保证阴影几何跟随起伏
                float3 normalOS;
                float  height01;
                float3 positionOS = ApplyTerrainHeight(TEXTURE2D_ARGS(_HeightMap, sampler_HeightMap),
                                                       IN.positionOS.xyz, IN.uv, _HeightMap_TexelSize.xy,
                                                       _HeightScale, _NormalSampleDistance,
                                                       _NormalStrength, _HeightInvert, normalOS, height01);

                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS   = TransformObjectToWorldNormal(normalOS);
                OUT.positionHCS = GetShadowPositionHClip(positionWS, normalWS);
                return OUT;
            }

            half4 ShadowPassFragment (Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // 深度 Pass：同步高度位移, 供深度预通道/依赖深度的后处理使用
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthOnlyVertex (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);

                // 与 Forward 一致地位移, 保证深度几何跟随起伏(深度无需法线)
                float3 normalOS;
                float  height01;
                float3 positionOS = ApplyTerrainHeight(TEXTURE2D_ARGS(_HeightMap, sampler_HeightMap),
                                                       IN.positionOS.xyz, IN.uv, _HeightMap_TexelSize.xy,
                                                       _HeightScale, _NormalSampleDistance,
                                                       _NormalStrength, _HeightInvert, normalOS, height01);

                OUT.positionHCS = TransformObjectToHClip(positionOS);
                return OUT;
            }

            half4 DepthOnlyFragment (Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
