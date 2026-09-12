Shader "FrameWork/URP/ShieldLink1"
{
    Properties
    {
        _BaseColor    ("线体颜色(低透明度防糊白)", Color) = (0.15, 0.45, 0.95, 0.35)
        _FlowColor    ("流动能量色(HDR)", Color)          = (0.45, 0.90, 1.40, 1)
        _FlowFreq     ("流动脉冲数(沿全长)", Float)        = 3.0
        _FlowSpeed    ("流动速度(由组件_FlowTime驱动)", Float) = 1.6
        _FlowTime     ("流动时间(代码MPB驱动)", Float)     = 0
        _HitFlash     ("受击闪白(代码MPB驱动)", Range(0, 1)) = 0
        _Fade         ("整体透明度乘数(代码MPB驱动,破碎淡出)", Range(0, 1)) = 1
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
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            // 加法发光能量线: 不写深度不测深度(罩体/生物之上的纯表现层); 双面: LineRenderer 已朝相机, 兜底
            Blend SrcAlpha One
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _FlowColor;
                float _FlowFreq;
                float _FlowSpeed;
                float _FlowTime;
                float _HitFlash;
                float _Fade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;  // LineRenderer(TextureMode=Stretch): x=0→1 沿全长(起点→终点), y=0→1 横向
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // 横向边缘衰减(中线最亮, 边缘到0)
                float edge = 1.0 - abs(IN.uv.y - 0.5) * 2.0;
                edge = edge * edge;

                // 能量脉冲: 沿 uv.x 从起点(被保护目标)流向终点(代受者), 节奏对齐"伤害转移"语义
                float p = frac(IN.uv.x * _FlowFreq - _FlowTime * _FlowSpeed);
                float pulse = smoothstep(0.0, 0.15, p) * smoothstep(0.45, 0.2, p);
                // 底流: 全程常驻的微光流动(双线叠加避免断流感)
                float baseFlow = 0.6 + 0.4 * sin((IN.uv.x * _FlowFreq * 2.0 - _FlowTime * _FlowSpeed * 0.7) * 6.2831853);

                // 合成: 弱底线体 + 脉冲能量 + 受击闪白
                half3 color = _BaseColor.rgb * baseFlow + pulse * _FlowColor.rgb;
                color = lerp(color, half3(1.4, 1.4, 1.4), _HitFlash);
                float alpha = saturate(_BaseColor.a * baseFlow + pulse * _FlowColor.a + _HitFlash * 0.6) * edge * _Fade;
                return half4(color * edge, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
