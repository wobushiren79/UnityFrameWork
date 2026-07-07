Shader "FrameWork/URP/MeshProgressBar"
{
    Properties
    {
        [Header(Background)]
        _BgMap ("背景贴图 (进度条底图)", 2D) = "white" {}
        [HDR] _BgColor ("背景颜色", Color) = (0.15, 0.15, 0.15, 1)

        [Header(Fill)]
        _FillMap ("进度贴图 (随进度填充的图)", 2D) = "white" {}
        [HDR] _FillColor ("进度颜色", Color) = (0.2, 0.9, 0.3, 1)

        [Header(Progress)]
        _Progress ("当前进度 (0=空 / 1=满)", Range(0, 1)) = 0.5
        [Enum(LeftToRight,0,RightToLeft,1,BottomToTop,2,TopToBottom,3)]
        _FillDirection ("填充方向 (进度增长方向)", Float) = 0
        _EdgeSoftness ("填充边缘软化 (0=硬边 / 越大越柔和)", Range(0, 0.2)) = 0.005

        [Header(Highlight)]
        [Toggle] _HighlightEnable ("开启高光版 (让颜色发光 / 需开 Bloom 后处理)", Float) = 0
        _BgHighlight ("背景高光强度", Range(1, 8)) = 2
        _FillHighlight ("进度高光强度", Range(1, 8)) = 2

        [Header(Lit)]
        [Toggle(_LIT_ON)] _LitEnable ("开启光照 (受主光/附加光/阴影/环境光 / 默认关闭)", Float) = 0

        [Header(Render Face)]
        [Enum(On,0,Off,2)] _Cull ("是否开启双面渲染 (On=两面都显示 / Off=仅显示正面)", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        // 半透明叠加：进度条通常悬浮显示，不写深度、不投影
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma shader_feature_local _LIT_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // 光照库(SurfaceData/InputData/BlinnPhong) → 通用受光助手
            #include "../Common/CommonLit.hlsl"

            TEXTURE2D(_BgMap);   SAMPLER(sampler_BgMap);
            TEXTURE2D(_FillMap); SAMPLER(sampler_FillMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BgMap_ST;
                float4 _FillMap_ST;
                half4  _BgColor;
                half4  _FillColor;
                half   _Progress;
                half   _FillDirection;
                half   _EdgeSoftness;
                half   _HighlightEnable;
                half   _BgHighlight;
                half   _FillHighlight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            /// 按填充方向取该像素的进度坐标(0~1)：值 <= 进度处视为已填充
            half GetFillCoord (float2 uv)
            {
                if (_FillDirection < 0.5)      return uv.x;         // 左 → 右
                else if (_FillDirection < 1.5) return 1.0 - uv.x;   // 右 → 左
                else if (_FillDirection < 2.5) return uv.y;         // 下 → 上
                else                           return 1.0 - uv.y;   // 上 → 下
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = IN.uv;
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // 背景与进度各自采样自己的贴图(独立 Tiling/Offset)并染色
                half4 bg   = SAMPLE_TEXTURE2D(_BgMap,   sampler_BgMap,   TRANSFORM_TEX(IN.uv, _BgMap))   * _BgColor;
                half4 fill = SAMPLE_TEXTURE2D(_FillMap, sampler_FillMap, TRANSFORM_TEX(IN.uv, _FillMap)) * _FillColor;

                // 高光版：分别放大背景/进度颜色的亮度(配合 Bloom 产生发光)
                if (_HighlightEnable > 0.5)
                {
                    bg.rgb   *= _BgHighlight;
                    fill.rgb *= _FillHighlight;
                }

                // 进度遮罩：填充坐标 <= 当前进度处为 1，边缘按软化宽度平滑过渡
                half coord = GetFillCoord(IN.uv);
                half fillMask = 1.0 - smoothstep(_Progress - _EdgeSoftness, _Progress + _EdgeSoftness, coord);

                // 在进度范围内用进度色覆盖背景色(按进度图自身 alpha 混合)，整体形状由背景 alpha 决定
                half over = fillMask * fill.a;
                half3 rgb = lerp(bg.rgb, fill.rgb, over);
                half  a   = bg.a;

                #if defined(_LIT_ON)
                    // 双面渲染时背面法线翻转，保证两面都能正确受光
                    half3 normalWS = normalize(IN.normalWS) * IS_FRONT_VFACE(cullFace, 1.0, -1.0);
                    return ApplyCommonLit(rgb, a, IN.positionWS, normalWS, IN.positionHCS, IN.fogFactor);
                #else
                    rgb = MixFog(rgb, IN.fogFactor);
                    return half4(rgb, a);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
