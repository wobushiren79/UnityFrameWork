// =============================================================================
//  Water_Simple.shader
//  URP 简易水面 (无贴图, 纯参数控制)
//  -----------------------------------------------------------------------------
//  特点:
//    - 不需要任何贴图, 全部参数化, 简单易懂
//    - 正弦波顶点起伏 + 解析法线 (平滑波纹)
//    - 靠岸浅色 -> 远岸深色 (基于场景深度)
//    - 反射探针/天空盒反射 + 菲涅尔
//    - 太阳高光
//  前置: URP Asset 开启 Depth Texture (用于靠岸渐变与边缘软融)
//  性能/缩放: 波纹法线在【片元逐像素】程序化计算(世界空间), 因此:
//    - 波纹清晰度与网格密度无关 => 内置低模 Plane 即可, 无需细分网格, 开销几乎为零;
//    - 世界空间采样 => 放大多少倍波纹都不被拉大。
//    _EffectScale=波纹密度, _WaveHeight=涟漪强度, _WaveScale=波纹粗细。
//    可选 _VertexWave 开启真实顶点几何起伏(有轮廓凸起, 但需较密网格才平滑; 默认关=纯平面)。
// =============================================================================
Shader "FrameWork/URP/WaterSimple"
{
    Properties
    {
        [Header(Density)]
        _EffectScale    ("波纹密度 (固定世界尺寸,与缩放无关,默认1)", Float)  = 1.0

        [Header(Colors)]
        _ShallowColor   ("浅水颜色 (靠岸)", Color)        = (0.3, 0.7, 0.8, 0.7)
        _DeepColor      ("深水颜色 (远处)", Color)        = (0.0, 0.2, 0.35, 0.95)
        _DepthDistance  ("水深渐变距离 (越大越缓)", Range(0.1, 30)) = 5.0

        [Header(Waves)]
        [Toggle(_VERTEX_WAVE)] _VertexWave ("顶点几何起伏 (需较密网格; 关=平面+逐像素涟漪最省)", Float) = 0
        _WaveHeight     ("波浪高度 / 涟漪强度", Range(0, 2))  = 0.2
        _WaveSpeed      ("波浪速度", Range(0, 5))         = 1.0
        _WaveScale      ("波浪密度 (越大波越细)", Range(0.05, 5)) = 0.5

        [Header(Reflection and Light)]
        _ReflectionAmount ("反射强度", Range(0, 1))       = 0.6
        _FresnelPower     ("菲涅尔锐度", Range(0.5, 8))   = 4.0
        _Smoothness       ("水面光滑度", Range(0, 1))     = 0.9
        _SpecularStrength ("太阳高光强度", Range(0, 8))   = 2.0
        _SpecularColor    ("高光颜色", Color)             = (1, 1, 1, 1)
        _ShadowStrength   ("阴影强度 (0=不受阴影)", Range(0, 1)) = 0.5

        [Header(Edge)]
        _EdgeFade       ("岸边软融距离", Range(0, 3))     = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma shader_feature_local _VERTEX_WAVE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "../Effect/Water/WaterWave.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;   // 逐像素法线所需的世界坐标
                float4 screenPos  : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float  _EffectScale;
                float  _VertexWave;
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _DepthDistance;
                float  _WaveHeight;
                float  _WaveSpeed;
                float  _WaveScale;
                float  _ReflectionAmount;
                float  _FresnelPower;
                float  _Smoothness;
                float  _SpecularStrength;
                float4 _SpecularColor;
                float  _ShadowStrength;
                float  _EdgeFade;
            CBUFFER_END

            // 波形函数抽到 Effect/Water/WaterWave.hlsl 共用 (WaterWaveHeight / WaterWaveSlope)

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                #ifdef _VERTEX_WAVE
                    // 可选真实顶点几何起伏(世界空间采样=缩放不变形; 需较密网格才平滑)
                    positionWS.y += WaterWaveHeight(positionWS.xz * _EffectScale, _WaveSpeed, _WaveScale) * _WaveHeight;
                #endif

                o.positionWS = positionWS;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                o.fogFactor  = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 逐像素程序化波纹法线: 用像素世界坐标算斜率 => 波纹清晰度与网格密度无关(低模也满分辨率),
                // 世界空间采样 => 缩放不变形。世界斜率 = slope × _WaveHeight × _EffectScale。
                float2 slope = WaterWaveSlope(input.positionWS.xz * _EffectScale, _WaveSpeed, _WaveScale);
                float2 wslope = slope * (_WaveHeight * _EffectScale);
                float3 normalWS  = normalize(float3(-wslope.x, 1.0, -wslope.y));
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                // 场景深度 -> 水深
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float  sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float  surfEye  = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float  depthDiff = max(sceneEye - surfEye, 0);

                // 水深渐变/软融是岸边物理效果, 维持固定世界尺寸 (不随 mesh 缩放)
                // 靠岸颜色渐变
                float depth01 = saturate(depthDiff / _DepthDistance);
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depth01);

                // 主光 + 阴影: 用世界坐标取阴影(透明物走逐片元阴影坐标)。shadowFactor 按 _ShadowStrength 插值受阴影程度。
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float shadowFactor = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);

                // 反射 + 菲涅尔 (仅水体本色受阴影压暗, 环境反射不受阴影)
                float3 reflectDir = reflect(-viewDirWS, normalWS);
                half3 reflection = GlossyEnvironmentReflection(reflectDir, input.positionWS, 1.0 - _Smoothness, 1.0).rgb;
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _ReflectionAmount;

                float3 color = lerp(waterColor.rgb * shadowFactor, reflection, saturate(fresnel));

                // 太阳高光 (阴影内不出现)
                float3 halfVec = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfVec));
                float spec = pow(NdotH, exp2(_Smoothness * 11.0 + 1.0)) * _SpecularStrength;
                color += _SpecularColor.rgb * spec * mainLight.color * shadowFactor;

                // Alpha 与岸边软融
                float edgeFade = saturate(depthDiff / max(_EdgeFade, 0.001));
                float alpha = saturate(lerp(_ShallowColor.a, _DeepColor.a, depth01) + fresnel * 0.4) * edgeFade;

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
