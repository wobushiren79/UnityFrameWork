// 雨天地面积水 shader（FrameWork/URP/Puddle1，跨项目通用件）：程序化不规则水洼形状（椭圆SDF+值噪声扰动边缘），
// 采样 Cubemap 天空反射（云影可见=水面感关键），程序化雨滴涟漪（网格哈希扩散环）扰动反射方向，
// 菲涅尔+基础反射占比混深水色，形状种子取面片枢轴世界坐标（同材质不同位置自动不同轮廓）。
// AlphaTest 队列+不关深度测试但 ZWrite Off（保留 ZTest）：Transparent 队列的地面网格/角色后画可透过水面，水洼可放路中间。
// 场景倒影由 PlanarReflection 组件（FrameWork/Scripts/Component/Other/）运行时写全局 _PuddlePlanarTex/_PuddlePlanarActive 提供，
// 本 shader 屏幕空间采样（X 必须翻转，见 frag 内注释），组件关闭时回退纯天空反射。
Shader "FrameWork/URP/Puddle1"
{
    Properties
    {
        _DeepColor("深水颜色", Color) = (0.16, 0.24, 0.28, 1)
        _SkyTint("天空反射染色", Color) = (1, 1, 1, 1)
        _SkyBrightness("天空反射亮度", Float) = 1.0
        [NoScaleOffset]_SkyCube("天空反射贴图(Cube)", Cube) = "" {}
        _BaseReflect("基础反射占比(正俯视时的天空反射权重)", Range(0, 1)) = 0.55
        _SkyElevation("反射天空仰角(越低取越靠近地平线的天空)", Range(0.05, 1)) = 0.35
        _EdgeNoiseScale("边缘噪声密度", Float) = 2.5
        _EdgeWobble("边缘扰动幅度", Range(0, 0.35)) = 0.16
        _RippleDensity("涟漪密度", Float) = 7.0
        _RippleSpeed("涟漪速度", Float) = 0.9
        _RippleStrength("涟漪扰动强度", Range(0, 0.5)) = 0.18
        _RimDarken("边缘湿润压暗", Range(0, 1)) = 0.25
        _PlanarWeight("场景反射占比(0=仅天空反射)", Range(0, 1)) = 0.85
        _PlanarDistort("场景反射扰动强度", Range(0, 0.1)) = 0.04
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURECUBE(_SkyCube);
            SAMPLER(sampler_SkyCube);
            // 平面反射全局输入（PlanarReflection 组件运行时写入；_PuddlePlanarActive=0 时回退纯天空反射）
            TEXTURE2D(_PuddlePlanarTex);
            SAMPLER(sampler_PuddlePlanarTex);
            float _PuddlePlanarActive;

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _SkyTint;
                float _SkyBrightness;
                float _BaseReflect;
                float _SkyElevation;
                float _EdgeNoiseScale;
                float _EdgeWobble;
                float _RippleDensity;
                float _RippleSpeed;
                float _RippleStrength;
                float _RimDarken;
                float _PlanarWeight;
                float _PlanarDistort;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 seedPos : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
            };

            // 二维值噪声（整数格点 hash）
            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                return ValueNoise(p) * 0.65 + ValueNoise(p * 2.13) * 0.35;
            }

            // 雨滴涟漪：3x3 邻域格内各一个扩散环，返回对反射法线的二维扰动方向
            float2 RippleOffset(float2 p, float t)
            {
                float2 ripple = float2(0, 0);
                float2 pg = floor(p);
                [unroll] for (int j = -1; j <= 1; j++)
                [unroll] for (int i = -1; i <= 1; i++)
                {
                    float2 cell = pg + float2(i, j);
                    float life = frac(t + Hash21(cell));
                    float2 center = cell + 0.2 + float2(Hash21(cell + 7.13), Hash21(cell + 3.71)) * 0.6;
                    float2 delta = p - center;
                    float d = max(length(delta), 1e-4);
                    float radius = life * 0.45;
                    float width = 0.05 + life * 0.05;
                    float ring = smoothstep(width, 0.0, abs(d - radius)) * (1.0 - life) * (1.0 - life);
                    ripple += (delta / d) * ring;
                }
                return ripple;
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.uv = input.uv;
                // 取面片枢轴世界坐标做形状种子（对单次绘制恒定，不被顶点插值破坏）
                o.seedPos = float2(unity_ObjectToWorld._m03, unity_ObjectToWorld._m23);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 seed = float2(Hash21(floor(input.seedPos * 3.0)), Hash21(floor(input.seedPos.yx * 3.0))) * 37.0;

                // 椭圆 SDF + 噪声扰动边缘 → 不规则水洼轮廓，轮廓外裁掉
                float2 p = input.uv * 2.0 - 1.0;
                float n = Fbm(p * _EdgeNoiseScale + seed);
                float dist = length(p * float2(1.0, 1.12));
                float edge = 0.72 + (n - 0.5) * 2.0 * _EdgeWobble;
                clip(edge - dist - 0.001);

                // 雨滴涟漪 + 低频慢速摆动合成反射扰动
                float t = _Time.y * _RippleSpeed;
                float2 rip = RippleOffset(p * _RippleDensity + seed, t);
                rip += float2(Fbm(p * 3.0 + seed + t * 0.3) - 0.5, Fbm(p * 3.0 + seed + 31.7 + t * 0.27) - 0.5) * 0.6;

                // 反射方向：水平取视线镜像方位、仰角固定（低视角水洼镜面映的是近地平线天空，云影最丰富），涟漪扰动其水平分量
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 viewHoriz = normalize(float3(viewDirWS.x, 0.0, viewDirWS.z) + float3(1e-4, 0.0, 1e-4));
                float3 reflectDir = normalize(float3(-viewHoriz.x, _SkyElevation, -viewHoriz.z));
                reflectDir.xz += rip * _RippleStrength;
                reflectDir = normalize(reflectDir);
                half3 skyCol = SAMPLE_TEXTURECUBE(_SkyCube, sampler_SkyCube, reflectDir).rgb * _SkyTint.rgb * _SkyBrightness;

                // 平面反射：屏幕空间UV采样镜像场景（含树木/角色/天空），未启用时回退纯天空反射。
                // 注意 X 必须翻转：PlanarReflection 用 LookRotation(反射forward,反射up) 重建的是右手系相机，
                // 其 right 轴与真镜面相反，渲染出的 RT 相对真镜像左右颠倒，采样须用 1-x 翻回
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                screenUV.x = 1.0 - screenUV.x;
                screenUV += rip * _PlanarDistort;
                half3 planarCol = SAMPLE_TEXTURE2D(_PuddlePlanarTex, sampler_PuddlePlanarTex, screenUV).rgb;
                half3 reflectCol = lerp(skyCol, planarCol, _PlanarWeight * _PuddlePlanarActive);

                // 菲涅尔：掠射角反射强，正俯视透深水；基础反射占比保底防"黑洞感"
                float fresnel = saturate(_BaseReflect + (1.0 - _BaseReflect) * pow(1.0 - saturate(viewDirWS.y), 2.0));
                half3 col = lerp(_DeepColor.rgb, reflectCol, fresnel);

                // 边缘湿润压暗（水洼与湿土的过渡圈）
                float rim = smoothstep(0.08, 0.0, edge - dist);
                col *= 1.0 - rim * _RimDarken;

                col = MixFog(col, input.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
