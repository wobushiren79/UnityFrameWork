Shader "FrameWork/SpriteRenderer/TreeWindSway"
{
    Properties
    {
        [MainTexture] _BaseMap ("树木贴图", 2D) = "white" {}
        [MainColor]   _BaseColor ("染色颜色", Color) = (1, 1, 1, 1)
        _Cutoff ("透明裁剪阈值 (0=不裁剪)", Range(0, 1)) = 0.0

        [Header(Lit)]
        // 勾选=受光(BlinnPhong) / 取消=无光；默认无光(与合并前的精灵版一致)。
        // 用 ToggleOff 使"无关键字=受光"，与网格/粒子版统一同一 keyword 约定。
        [ToggleOff(_UNLIT_ON)] _LitEnable ("开启光照 (勾选=受光 / 取消=无光)", Float) = 0

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _OutlineEnable ("开启描边 (沿轮廓 Alpha 外扩 / 默认关闭)", Float) = 0
        [HDR] _OutlineColor ("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineSize ("描边大小 (向外扩展的纹素数)", Range(0, 10)) = 1.0

        [Header(Render Face)]
        [Enum(On,0,Off,2)] _Cull ("是否开启双面渲染 (On=两面都显示 / Off=仅显示正面)", Float) = 0

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
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Pass
        {
            Name "SpriteForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _UNLIT_ON
            #pragma shader_feature_local _OUTLINE_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            // 光照库(SurfaceData/InputData/BlinnPhong) → 通用受光助手；描边助手
            #include "../Common/CommonLit.hlsl"
            #include "../Common/Outline.hlsl"

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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseMap_TexelSize;   // Unity 按所赋贴图自动填充，供描边取纹素尺寸
                half4  _BaseColor;
                half   _Cutoff;
                float4 _PositionOffset;
                half   _WindSpeed;
                half   _SwayStrength;
                half   _SwayFrequency;
                half   _BendStrength;
                half   _FlutterStrength;
                half   _FlutterSpeed;
                half   _AnchorBottom;
                half4  _OutlineColor;
                half   _OutlineSize;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // 沿 UV.y 计算摆动权重：以树干底部为锚点时，越靠近树冠摆动越大
                float heightWeight = lerp(IN.uv.y, 1.0 - IN.uv.y, 1.0 - _AnchorBottom);

                // 让不同树在世界空间下的相位错开，避免所有树同步摆动
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float phase = positionWS.x * 0.5 + positionWS.z * 0.5;

                float t = _Time.y * _WindSpeed;

                // 主摆动：水平方向正弦摇摆，强度随高度权重二次增长（更自然）
                float swayWave = sin(t * _SwayFrequency + phase);
                float sway = swayWave * _SwayStrength * heightWeight * heightWeight;

                // 高频抖动：模拟枝叶在风中的细碎颤动
                float flutter = sin(t * _FlutterSpeed + phase * 3.0 + IN.uv.x * 6.2831)
                                * _FlutterStrength * heightWeight;

                // 摆动时树冠轻微下压，模拟枝干弯折
                float bend = -abs(swayWave) * _BendStrength * heightWeight;

                float3 posOS = IN.positionOS.xyz;
                posOS.x += sway + flutter;
                posOS.y += bend;

                // 整体位置偏移：所有顶点统一平移(不随风摆动)
                posOS += _PositionOffset.xyz;

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                #if defined(_OUTLINE_ON)
                    // 描边在采样阶段沿轮廓外扩(须在 clip 前，否则描边像素被裁掉)
                    baseSample = ApplyAlphaOutline(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), baseSample,
                                                   IN.uv, _BaseMap_TexelSize.xy, _OutlineSize, _OutlineColor);
                #endif
                half4 col = baseSample * _BaseColor;

                // 可选 Alpha 裁剪（_Cutoff > 0 时生效）
                clip(col.a - _Cutoff);

                #if defined(_UNLIT_ON)
                    // 无光：直接输出染色(与合并前一致，精灵默认无雾)
                    return col;
                #else
                    // 精灵法线固定(quad 朝相机)，双面渲染时背面翻转保证正确受光
                    half3 normalWS = normalize(IN.normalWS) * IS_FRONT_VFACE(cullFace, 1.0, -1.0);
                    return ApplyCommonLit(col.rgb, col.a, IN.positionWS, normalWS, IN.positionHCS, IN.fogFactor);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
    CustomEditor "WindSwayShaderGUI"
}
