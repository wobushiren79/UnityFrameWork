Shader "Frame Work/URP/GrassWindSway"
{
    Properties
    {
        [MainTexture] _BaseMap ("草贴图", 2D) = "white" {}
        [MainColor]   _BaseColor ("染色颜色", Color) = (1, 1, 1, 1)
        _Cutoff ("透明裁剪阈值 (0=不裁剪)", Range(0, 1)) = 0.0

        [Header(Position Offset)]
        _PositionOffset ("位置偏移 (XYZ 顶点整体偏移)", Vector) = (0, 0, 0, 0)

        [Header(Wind Sway)]
        _WindSpeed     ("风速 (整体快慢)", Range(0, 10)) = 2.5
        _SwayStrength  ("摆动幅度 (草尖左右摇摆大小)", Range(0, 1)) = 0.12
        _SwayFrequency ("摆动频率 (摇摆快慢)", Range(0, 10)) = 2.0
        _WindDir       ("风向 (-1左 / +1右 顺风倾倒)", Range(-1, 1)) = 0.3
        _BendStrength  ("顺风弯倒幅度 (草整体被风压弯)", Range(0, 1)) = 0.15

        [Header(Flutter)]
        _FlutterStrength ("抖动幅度 (草叶细碎颤动大小)", Range(0, 0.5)) = 0.03
        _FlutterSpeed    ("抖动速度 (颤动快慢)", Range(0, 30)) = 14.0

        [Header(Stiffness)]
        // 越大草越硬(根部以上更晚开始弯)，越小越软(贴近根部就开始弯)
        _Stiffness ("草茎硬度 (越大越硬越直)", Range(1, 4)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Cutoff;
                float4 _PositionOffset;
                half   _WindSpeed;
                half   _SwayStrength;
                half   _SwayFrequency;
                half   _WindDir;
                half   _BendStrength;
                half   _FlutterStrength;
                half   _FlutterSpeed;
                half   _Stiffness;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // 根部在贴图最底部(UV.y=0)固定，越往草尖(UV.y=1)摆动越大
                // 用 pow(uv.y, _Stiffness) 控制草茎硬度：值越大，根部附近越不弯、越像硬草茎
                float heightWeight = pow(saturate(IN.uv.y), _Stiffness);

                // 让每株草在世界空间下相位错开，避免整片草同步摆动
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float phase = positionWS.x * 0.7 + positionWS.z * 0.7;

                float t = _Time.y * _WindSpeed;

                // 主摆动：草尖左右正弦摇摆
                float swayWave = sin(t * _SwayFrequency + phase);
                float sway = swayWave * _SwayStrength;

                // 顺风弯倒：草整体往风向方向被持续压弯(带轻微脉动)
                float bend = _WindDir * _BendStrength * (0.75 + 0.25 * sin(t * 0.5 + phase));

                // 高频抖动：草叶细碎颤动
                float flutter = sin(t * _FlutterSpeed + phase * 3.0 + IN.uv.x * 6.2831)
                                * _FlutterStrength;

                // 水平位移随高度权重增长，根部几乎不动
                float offsetX = (sway + bend + flutter) * heightWeight;

                // 草尖被压弯时轻微下沉，保持草茎长度观感
                float offsetY = -abs(offsetX) * 0.3;

                float3 posOS = IN.positionOS.xyz;
                posOS.x += offsetX;
                posOS.y += offsetY;

                // 整体位置偏移：所有顶点统一平移(不随风摆动)
                posOS += _PositionOffset.xyz;

                OUT.positionHCS = TransformObjectToHClip(posOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // 可选 Alpha 裁剪（_Cutoff > 0 时生效）
                clip(col.a - _Cutoff);

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
