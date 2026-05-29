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
// =============================================================================
Shader "URP/Shader_Water_Simple"
{
    Properties
    {
        [Header(Colors)]
        _ShallowColor   ("浅水颜色 (靠岸)", Color)        = (0.3, 0.7, 0.8, 0.7)
        _DeepColor      ("深水颜色 (远处)", Color)        = (0.0, 0.2, 0.35, 0.95)
        _DepthDistance  ("水深渐变距离 (越大越缓)", Range(0.1, 30)) = 5.0

        [Header(Waves)]
        _WaveHeight     ("波浪高度", Range(0, 2))         = 0.2
        _WaveSpeed      ("波浪速度", Range(0, 5))         = 1.0
        _WaveScale      ("波浪密度 (越大波越细)", Range(0.05, 5)) = 0.5

        [Header(Reflection and Light)]
        _ReflectionAmount ("反射强度", Range(0, 1))       = 0.6
        _FresnelPower     ("菲涅尔锐度", Range(0.5, 8))   = 4.0
        _Smoothness       ("水面光滑度", Range(0, 1))     = 0.9
        _SpecularStrength ("太阳高光强度", Range(0, 8))   = 2.0
        _SpecularColor    ("高光颜色", Color)             = (1, 1, 1, 1)

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
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
                float  _EdgeFade;
            CBUFFER_END

            // 多个正弦波叠加, 返回高度; 同时输出 XZ 方向的斜率用于法线
            float WaveHeight(float2 pos, out float2 slope)
            {
                float t = _Time.y * _WaveSpeed;
                float s = _WaveScale;

                // 三组方向不同的正弦波
                float h = 0;
                slope = 0;

                float2 d1 = float2( 1.0, 0.4); float f1 = (dot(pos, d1) * s + t);
                float2 d2 = float2(-0.6, 1.0); float f2 = (dot(pos, d2) * s * 1.3 + t * 1.2);
                float2 d3 = float2( 0.3, -0.8);float f3 = (dot(pos, d3) * s * 0.7 + t * 0.8);

                h += sin(f1) * 0.5;
                h += sin(f2) * 0.3;
                h += sin(f3) * 0.2;

                slope += cos(f1) * 0.5 * s * d1;
                slope += cos(f2) * 0.3 * s * 1.3 * d2;
                slope += cos(f3) * 0.2 * s * 0.7 * d3;

                return h * _WaveHeight;
            }

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                float2 slope;
                float h = WaveHeight(positionWS.xz, slope);
                positionWS.y += h;

                // 由斜率重建法线
                float3 normalWS = normalize(float3(-slope.x, 1.0, -slope.y));

                o.positionWS = positionWS;
                o.normalWS   = normalWS;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                o.fogFactor  = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS  = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                // 场景深度 -> 水深
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float  sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float  surfEye  = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float  depthDiff = max(sceneEye - surfEye, 0);

                // 靠岸颜色渐变
                float depth01 = saturate(depthDiff / _DepthDistance);
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depth01);

                // 反射 + 菲涅尔
                float3 reflectDir = reflect(-viewDirWS, normalWS);
                half3 reflection = GlossyEnvironmentReflection(reflectDir, input.positionWS, 1.0 - _Smoothness, 1.0).rgb;
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _ReflectionAmount;

                float3 color = lerp(waterColor.rgb, reflection, saturate(fresnel));

                // 太阳高光
                Light mainLight = GetMainLight();
                float3 halfVec = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfVec));
                float spec = pow(NdotH, exp2(_Smoothness * 11.0 + 1.0)) * _SpecularStrength;
                color += _SpecularColor.rgb * spec * mainLight.color * mainLight.shadowAttenuation;

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
