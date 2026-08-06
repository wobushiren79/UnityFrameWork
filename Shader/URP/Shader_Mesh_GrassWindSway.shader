Shader "FrameWork/URP/GrassWindSway"
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
        // 勾选=受光(BlinnPhong) / 取消=无光；默认受光(与原始 Lit 版一致)。
        // 用 ToggleOff 使"无关键字=受光"，老材质(无关键字)自动保持受光、无需改动。
        [ToggleOff(_UNLIT_ON)] _LitEnable ("开启光照 (勾选=受光 / 取消=无光)", Float) = 1

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

        // 所有 Pass 共用：材质参数 + 贴图 + 风摆位移函数(保证阴影/深度与正向渲染同步摆动)
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // Alpha 扩张描边通用函数(ApplyAlphaOutline)
        #include "../Common/Outline.hlsl"
        // 通用渲染设置件：ApplyAlphaClip(按 _ALPHATEST_ON 镂空)
        #include "../Common/SurfaceOptions.hlsl"

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

        // 风摆顶点位移：根部(UV.y=0)固定，越往草尖摆动越大(实例化下每株草相位错开)
        void ApplyWind(inout float3 positionOS, float2 uv)
        {
            // pow(uv.y, _Stiffness) 控制草茎硬度：值越大根部附近越不弯、越像硬草茎
            float heightWeight = pow(saturate(uv.y), _Stiffness);

            float3 positionWS = TransformObjectToWorld(positionOS);
            float phase = positionWS.x * 0.7 + positionWS.z * 0.7;

            float t = _Time.y * _WindSpeed;

            float swayWave = sin(t * _SwayFrequency + phase);
            float sway = swayWave * _SwayStrength;

            float bend = _WindDir * _BendStrength * (0.75 + 0.25 * sin(t * 0.5 + phase));

            float flutter = sin(t * _FlutterSpeed + phase * 3.0 + uv.x * 6.2831)
                            * _FlutterStrength;

            float offsetX = (sway + bend + flutter) * heightWeight;
            float offsetY = -abs(offsetX) * 0.3;

            positionOS.x += offsetX;
            positionOS.y += offsetY;
            positionOS += _PositionOffset.xyz;
        }
        ENDHLSL

        // 正向光照 Pass：接收主光/附加光/阴影 + 环境光(SH) + 雾
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            // 混合因子/深度写入由材质面板"表面类型/渲染模式"预设驱动(不透明=One Zero+写深度, 透明=按模式混合+不写深度)
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #pragma multi_compile_instancing
            #pragma shader_feature_local _UNLIT_ON
            #pragma shader_feature_local _OUTLINE_ON
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

            Varyings LitPassVertex (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posOS = IN.positionOS.xyz;
                ApplyWind(posOS, IN.uv);

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 LitPassFragment (Varyings IN, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
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
                    // 无光：直接输出染色 + 雾
                    col.rgb = MixFog(col.rgb, IN.fogFactor);
                    return col;
                #else
                // 双面渲染：背面法线翻转，保证两面都能正确受光
                half3 normalWS = normalize(IN.normalWS) * IS_FRONT_VFACE(cullFace, 1.0, -1.0);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = col.rgb;
                surfaceData.alpha      = col.a;   // 透明表面用 col.a 参与混合(不透明表面 One/Zero 忽略)
                surfaceData.occlusion  = 1.0;

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

        // 阴影投射 Pass：同步风摆位移 + Alpha 裁剪，保证投影随草摆动且镂空正确
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
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

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
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posOS = IN.positionOS.xyz;
                ApplyWind(posOS, IN.uv);

                float3 positionWS = TransformObjectToWorld(posOS);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS = GetShadowPositionHClip(positionWS, normalWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 ShadowPassFragment (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                ApplyAlphaClip(alpha, _Cutoff);   // 仅开启裁剪时按轮廓镂空
                return 0;
            }
            ENDHLSL
        }

        // 深度 Pass：供深度预通道/相关后处理使用，同步风摆 + Alpha 裁剪
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
            #pragma shader_feature_local _ALPHATEST_ON

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

            Varyings DepthOnlyVertex (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posOS = IN.positionOS.xyz;
                ApplyWind(posOS, IN.uv);

                OUT.positionHCS = TransformObjectToHClip(posOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 DepthOnlyFragment (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                ApplyAlphaClip(alpha, _Cutoff);   // 仅开启裁剪时按轮廓镂空
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
    CustomEditor "WindSwayShaderGUI"
}
