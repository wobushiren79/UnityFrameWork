// =============================================================================
//  Water_LowPoly.shader
//  URP 低多边形(Low Poly)风格水面 (无贴图, 纯参数控制)
//  -----------------------------------------------------------------------------
//  特点:
//    - 不需要任何贴图, 参数简单易懂
//    - 顶点起伏 + 平面着色(Flat Shading): 每个三角面一个法线, 呈现棱面感
//    - 双色渐变 (波峰亮色 / 波谷暗色)
//    - 简单菲涅尔反射 + 太阳高光
//    - 岸边泡沫线 (基于场景深度, 无需贴图)
//  提示: 网格面数越少棱面感越强; 建议用较稀疏的 Plane 网格。
//  前置: URP Asset 开启 Depth Texture (用于岸边泡沫与软融)
//  波纹尺寸: 波纹在世界空间采样, 尺寸恒为固定的世界尺寸, 与 mesh 的 Transform/父级缩放
//        完全无关 —— 无论把水面放大多少倍, 波纹都不会被拉大。_EffectScale 仅作波纹密度
//        微调(越大波越密/越小, 默认 1)。泡沫/水深等岸边效果同样为绝对世界尺寸。
//        注意: 波纹是逐顶点位移, 低模网格放大后顶点稀疏会使波纹采样不足而变淡/消失,
//        大面积水面请改用细分较密的平面网格(而非放大内置低模网格)。
// =============================================================================
Shader "FrameWork/URP/WaterLowPoly"
{
    Properties
    {
        [Header(Density)]
        _EffectScale    ("波纹密度 (固定世界尺寸,与缩放无关,默认1)", Float)  = 1.0

        [Header(Colors)]
        _CrestColor     ("波峰颜色 (亮)", Color)          = (0.4, 0.8, 0.9, 0.85)
        _TroughColor    ("波谷颜色 (暗)", Color)          = (0.05, 0.35, 0.55, 0.95)
        _ColorBlend     ("峰谷过渡 (越大越软)", Range(0.1, 4)) = 1.0

        [Header(Waves)]
        _WaveHeight     ("波浪高度", Range(0, 3))         = 0.4
        _WaveSpeed      ("波浪速度", Range(0, 5))         = 1.0
        _WaveScale      ("波浪密度 (越大波越细)", Range(0.05, 5)) = 0.4

        [Header(Light)]
        _ReflectionAmount ("反射强度", Range(0, 1))       = 0.4
        _FresnelPower     ("菲涅尔锐度", Range(0.5, 8))   = 3.0
        _Smoothness       ("水面光滑度", Range(0, 1))     = 0.7
        _SpecularStrength ("太阳高光强度", Range(0, 8))   = 1.5
        _SpecularColor    ("高光颜色", Color)             = (1, 1, 1, 1)
        _AmbientBoost     ("环境亮度补偿", Range(0, 2))   = 0.3
        _ShadowStrength   ("阴影强度 (0=不受阴影)", Range(0, 1)) = 1.0

        [Header(Shore Foam)]
        _FoamColor      ("泡沫颜色", Color)               = (1, 1, 1, 1)
        _FoamDistance   ("泡沫宽度 (靠岸)", Range(0, 3))  = 0.5
        _EdgeFade       ("岸边软融距离", Range(0, 3))     = 0.3
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
            #include "../Effect/Water/WaterWave.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float  waveHeight : TEXCOORD1; // 当前点的波高 (用于峰谷上色)
                float4 screenPos  : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float  _EffectScale;
                float4 _CrestColor;
                float4 _TroughColor;
                float  _ColorBlend;
                float  _WaveHeight;
                float  _WaveSpeed;
                float  _WaveScale;
                float  _ReflectionAmount;
                float  _FresnelPower;
                float  _Smoothness;
                float  _SpecularStrength;
                float4 _SpecularColor;
                float  _AmbientBoost;
                float  _ShadowStrength;
                float4 _FoamColor;
                float  _FoamDistance;
                float  _EdgeFade;
            CBUFFER_END

            // 波形函数抽到 Effect/Water/WaterWave.hlsl 共用 (WaterWaveHeight 返回原始幅度高度)

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // 波纹在世界空间采样 => 波长恒为固定世界尺寸, 与 Transform/父级缩放完全无关,
                // 缩放 mesh 波纹绝不被拉大 (positionWS 已含全部缩放)。_EffectScale 仅作密度微调。
                float h = WaterWaveHeight(positionWS.xz * _EffectScale, _WaveSpeed, _WaveScale);
                positionWS.y += h * _WaveHeight;

                o.positionWS = positionWS;
                o.waveHeight = h; // 归一化前的相对高度
                o.positionCS = TransformWorldToHClip(positionWS);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                o.fogFactor  = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // === 平面着色: 用世界坐标的屏幕导数算出每个三角面的法线 ===
                float3 dpdx = ddx(input.positionWS);
                float3 dpdy = ddy(input.positionWS);
                float3 normalWS = normalize(cross(dpdy, dpdx));
                // 保证朝上
                if (normalWS.y < 0) normalWS = -normalWS;

                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                // === 峰谷双色 ===
                float crest = saturate(input.waveHeight * _ColorBlend * 0.5 + 0.5);
                half4 waterColor = lerp(_TroughColor, _CrestColor, crest);

                // === 主光漫反射 (棱面感来源) + 阴影 ===
                // 透明物走逐片元阴影坐标; shadowFactor 按 _ShadowStrength 插值受阴影程度
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float shadowFactor = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = waterColor.rgb * (NdotL * mainLight.color * shadowFactor + _AmbientBoost);

                // === 反射 + 菲涅尔 ===
                float3 reflectDir = reflect(-viewDirWS, normalWS);
                half3 reflection = GlossyEnvironmentReflection(reflectDir, input.positionWS, 1.0 - _Smoothness, 1.0).rgb;
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _ReflectionAmount;
                float3 color = lerp(diffuse, reflection, saturate(fresnel));

                // === 太阳高光 ===
                float3 halfVec = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfVec));
                float spec = pow(NdotH, exp2(_Smoothness * 11.0 + 1.0)) * _SpecularStrength;
                color += _SpecularColor.rgb * spec * mainLight.color * shadowFactor;

                // === 岸边泡沫 (基于场景深度) ===
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float  sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float  surfEye  = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float  depthDiff = max(sceneEye - surfEye, 0);

                // 泡沫/软融是岸边物理效果, 维持固定世界尺寸 (不随 mesh 缩放)
                float foam = 1.0 - saturate(depthDiff / max(_FoamDistance, 0.001));
                foam = smoothstep(0.6, 1.0, foam);
                color = lerp(color, _FoamColor.rgb, foam);

                // === Alpha 与岸边软融 ===
                float edgeFade = saturate(depthDiff / max(_EdgeFade, 0.001));
                float alpha = saturate(max(waterColor.a, foam)) * edgeFade;

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
