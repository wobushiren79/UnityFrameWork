// 沙漠热浪 / 空气扭曲：透明网格采样相机不透明贴图(_CameraOpaqueTexture)，
// 按滚动的程序化噪声偏移屏幕 UV，制造像水波一样飘动的热浪扭曲。无需任何噪声贴图。
// 用法：铺一张 Quad(横铺地面上方或竖立面向相机)，材质用本 Shader；URP 须开启 Opaque Texture。
Shader "FrameWork/Effect/HeatHaze"
{
    Properties
    {
        [Header(Distortion)]
        _NoiseScale ("噪声缩放 (越大波纹越密)", Float) = 8.0
        _DistortStrength ("扭曲强度 (屏幕 UV 偏移量)", Range(0, 0.1)) = 0.02
        _RiseSpeed ("上升速度 (热浪向上滚动)", Float) = 0.6
        _WaveSpeed ("波动速度 (横向摆动)", Float) = 1.0

        [Header(Appearance)]
        _Alpha ("整体强度 (0 隐藏 ~ 1 全扭曲)", Range(0, 1)) = 1.0
        _TintColor ("热浪染色 (白=不染色 / 可微调偏黄)", Color) = (1, 1, 1, 1)

        [Header(Mask)]
        _EdgeFade ("边缘羽化 (0~0.5 / 越大边缘越柔)", Range(0, 0.5)) = 0.15
        _VerticalFade ("沿 UV.Y 衰减 (底强顶弱 / 0=均匀)", Range(0, 1)) = 0.3

        // 渲染优先级偏移(相对透明基础队列 3000)，由 HeatHazeShaderGUI 以"优先级"滑条绘制并写入 material.renderQueue
        [HideInInspector] _QueueOffset ("Queue Offset", Float) = 0.0
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

        Pass
        {
            Name "HeatHaze"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // 声明并提供 SampleSceneColor(uv) → 采样相机不透明贴图 _CameraOpaqueTexture
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _NoiseScale;
                float  _DistortStrength;
                float  _RiseSpeed;
                float  _WaveSpeed;
                float  _Alpha;
                half4  _TintColor;
                float  _EdgeFade;
                float  _VerticalFade;
            CBUFFER_END

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
                float4 screenPos   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 2D 哈希 → 值噪声(value noise)：无需贴图的平滑随机场，用于制造有机热浪抖动
            float Hash2 (float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise (float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);                 // 平滑插值(smoothstep)
                float a = Hash2(i);
                float b = Hash2(i + float2(1.0, 0.0));
                float c = Hash2(i + float2(0.0, 1.0));
                float d = Hash2(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.screenPos   = ComputeScreenPos(posInputs.positionCS);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float t = _Time.y;
                // 两层解相关的值噪声随时间上升滚动，得到 X/Y 两个方向的扭曲量
                float2 nUV    = IN.uv * _NoiseScale;
                float2 scroll = float2(_WaveSpeed * 0.3, -_RiseSpeed) * t;
                float  nx     = ValueNoise(nUV + scroll) - 0.5;
                float  ny     = ValueNoise(nUV + scroll + float2(37.2, 11.7)) - 0.5;
                float2 distort = float2(nx, ny) * _DistortStrength;

                // 遮罩：四边羽化 + 沿 UV.Y 的衰减，让扭曲在边缘平滑过渡不露硬边
                float edge  = smoothstep(0.0, _EdgeFade, IN.uv.x) * smoothstep(0.0, _EdgeFade, 1.0 - IN.uv.x)
                            * smoothstep(0.0, _EdgeFade, IN.uv.y) * smoothstep(0.0, _EdgeFade, 1.0 - IN.uv.y);
                float vFade = lerp(1.0, 1.0 - IN.uv.y, _VerticalFade);
                float mask  = saturate(edge * vFade);

                // 偏移量随遮罩衰减，避免边缘处采样跳变
                float2 screenUV  = IN.screenPos.xy / IN.screenPos.w;
                float2 sampleUV  = screenUV + distort * mask;

                half3 scene = SampleSceneColor(sampleUV) * _TintColor.rgb;
                return half4(scene, mask * _Alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
    CustomEditor "HeatHazeShaderGUI"
}
