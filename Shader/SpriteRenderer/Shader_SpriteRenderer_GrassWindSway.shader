Shader "FrameWork/SpriteRenderer/GrassWindSway"
{
    Properties
    {
        [MainTexture] _BaseMap ("草贴图", 2D) = "white" {}
        [MainColor]   _BaseColor ("染色颜色", Color) = (1, 1, 1, 1)

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
        // 勾选=受光(BlinnPhong) / 取消=无光；默认无光(与合并前的精灵版一致)。
        // 用 ToggleOff 使"无关键字=受光"，与网格/粒子版统一同一 keyword 约定。
        [ToggleOff(_UNLIT_ON)] _LitEnable ("开启光照 (勾选=受光 / 取消=无光)", Float) = 0

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _OutlineEnable ("开启描边 (沿轮廓 Alpha 外扩 / 默认关闭)", Float) = 0
        [HDR] _OutlineColor ("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineSize ("描边大小 (向外扩展的纹素数)", Range(0, 10)) = 1.0

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
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Pass
        {
            Name "SpriteForward"
            Tags { "LightMode" = "UniversalForward" }

            // 混合因子/深度写入由材质面板"表面类型/渲染模式"预设驱动(不透明=One Zero+写深度, 透明=按模式混合+不写深度)
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _UNLIT_ON
            #pragma shader_feature_local _OUTLINE_ON
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            // 光照库(SurfaceData/InputData/BlinnPhong) → 通用受光助手；描边助手；通用渲染设置(ApplyAlphaClip)
            #include "../Common/CommonLit.hlsl"
            #include "../Common/Outline.hlsl"
            #include "../Common/SurfaceOptions.hlsl"

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
                half   _WindDir;
                half   _BendStrength;
                half   _FlutterStrength;
                half   _FlutterSpeed;
                half   _Stiffness;
                half4  _OutlineColor;
                half   _OutlineSize;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
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

                // Alpha 裁剪(镂空)：仅开启 _ALPHATEST_ON 时丢弃低于 _Cutoff 的像素；不透明表面默认开启
                ApplyAlphaClip(col.a, _Cutoff);

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
