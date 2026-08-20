// UGUI Image 容器水波纹 Shader（多层视差水面）
// 效果：图片下半部分为"容器中的水"，由 1~4 个独立水层组成；
//       每层有自己完整的 sin 波形水面(独立 振幅/频率/速度/相位)，速度正负决定横移方向，
//       前层与后层反向横移 → 多层交错视差；每层水颜色独立手动调节(一般后层调得比前层深)；
//       后层水位可自动抬升，波峰从前层上方露出。
// 底图说明：默认「显示底图纹理」关闭 = 纯水体模式，水上区域透明(适合直接当动态水块用)；
//       开启后水上区域显示底图(适合给容器贴图加水)。
// 坐标说明：波纹与水位基于 0~1 归一化坐标计算。默认用主纹理 UV(UV0)；
//       若 Image 的 sprite 进了图集或为 9-slice(UV 被压缩成子矩形)，须挂载
//       UIGradientMeshUV 组件并开启「使用整矩形UV」开关，否则水面位置错乱。
Shader "FrameWork/UI/Shader_UI_ImageWaterWave"
{
    Properties
    {
        // 模板(Stencil)与颜色掩码参数为 UGUI Mask/RectMask2D 遮罩系统专用，
        // 运行时由 Mask 组件通过 MaterialPropertyBlock 自动写入，无需手动修改，
        // 保留它们只是为了让本特效 Image 能被父级 Mask 正确裁剪，删除会导致遮罩失效。
        [Header(Stencil for UGUI Mask  auto set)]
        _StencilComp("模板比较方式", Float) = 8
        _Stencil("模板ID", Float) = 0
        _StencilOp("模板操作", Float) = 0
        _StencilWriteMask("模板写入掩码", Float) = 255
        _StencilReadMask("模板读取掩码", Float) = 255
        _ColorMask("颜色通道掩码", Float) = 15

        [Header(Base)]
        [PerRendererData] _MainTex("主纹理", 2D) = "white" {}
        [Toggle(_BASETEX_ON)] _UseBaseTex("显示底图纹理(关闭=纯水体,水上区域透明)", Float) = 0
        [HDR] _Color("整体着色(乘法)", Color) = (1,1,1,1)
        _MainAlpha("整体透明度", Range(0, 2)) = 1
        [Toggle(_INTERNAL_TIME_ON)] _InternalTime("使用内置时间(关闭则用外部DeltaTime)", Float) = 1
        _DeltaTime("外部驱动时间(秒)", Float) = 0
        [Toggle(_RECTUV_ON)] _UseRectUV("使用整矩形UV(图集/9宫格时须挂UIGradientMeshUV)", Float) = 0
        [Enum(Off, 0, On, 1)] _UseUIAlphaClip("启用UI透明裁剪", Float) = 1
        [Enum(Off, 0, On, 1)] _UseUIClipRect("启用UI矩形裁剪", Float) = 1

        [Header(Water)]
        [HDR] _LayerColor1("第1层(前层)水颜色(半透明可见后层)", Color) = (0.35, 0.65, 1.0, 0.55)
        [HDR] _LayerColor2("第2层水颜色", Color) = (0.2, 0.45, 0.85, 0.7)
        [HDR] _LayerColor3("第3层水颜色", Color) = (0.12, 0.32, 0.72, 0.78)
        [HDR] _LayerColor4("第4层(最后层)水颜色", Color) = (0.08, 0.25, 0.6, 0.85)
        _WaterLevel("水位高度(0~1,指最前层)", Range(0, 1)) = 0.7
        _LayerLevelStep("层间水位抬升(后层比前层高出的量)", Range(-0.2, 0.2)) = 0.03
        _WaterEdgeSoftness("水面边缘柔和度(抗锯齿)", Range(0.001, 0.1)) = 0.008

        [Header(Layers)]
        [KeywordEnum(One, Two, Three, Four)] _WaveCount("水层数量(每层独立波形/横移)", Float) = 1
        _GlobalWaveAmp("全局振幅倍率(波形整体高低)", Range(0, 5)) = 1
        _GlobalWaveSpeed("全局波速倍率(负值所有层反向)", Range(-5, 5)) = 1
        _Wave1("第1层(前层)波形 x振幅,y频率,z速度(正左移/负右移),w相位", Vector) = (0.02, 3, 0.5, 0)
        _Wave2("第2层波形 x振幅,y频率,z速度(正左移/负右移),w相位", Vector) = (0.018, 2.5, -0.4, 1.57)
        _Wave3("第3层波形 x振幅,y频率,z速度(正左移/负右移),w相位", Vector) = (0.015, 2, 0.3, 3.14)
        _Wave4("第4层(最后层)波形 x振幅,y频率,z速度(正左移/负右移),w相位", Vector) = (0.012, 1.5, -0.2, 4.71)

        [Header(Surface Line)]
        [Toggle(_SURFACE_ON)] _SurfaceOn("启用水面亮线(每层浪沿高亮)", Float) = 1
        [HDR] _SurfaceColor("水面亮线颜色(各层自动向本层水色靠)", Color) = (1, 1, 1, 0.5)
        _SurfaceWidth("水面亮线宽度", Range(0.001, 0.1)) = 0.012
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "Queue" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma shader_feature_local _INTERNAL_TIME_ON
            #pragma shader_feature_local _BASETEX_ON
            #pragma shader_feature_local _RECTUV_ON
            #pragma shader_feature_local _SURFACE_ON
            #pragma shader_feature_local _WAVECOUNT_ONE _WAVECOUNT_TWO _WAVECOUNT_THREE _WAVECOUNT_FOUR

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // SRP Batcher 兼容：每材质常量统一放入 UnityPerMaterial
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _MainAlpha;
                half _UseUIAlphaClip;
                half _UseUIClipRect;
                float _DeltaTime;

                half4 _LayerColor1;
                half4 _LayerColor2;
                half4 _LayerColor3;
                half4 _LayerColor4;
                half _WaterLevel;
                half _LayerLevelStep;
                half _WaterEdgeSoftness;

                half _GlobalWaveAmp;
                half _GlobalWaveSpeed;
                float4 _Wave1;
                float4 _Wave2;
                float4 _Wave3;
                float4 _Wave4;

                half4 _SurfaceColor;
                half _SurfaceWidth;
            CBUFFER_END

            // UGUI 矩形裁剪由 Canvas 通过 MaterialPropertyBlock 设置，置于 CBUFFER 外
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // 当前水层总数(由波纹数量关键字决定)
        #if defined(_WAVECOUNT_FOUR)
            #define LAYER_COUNT 4
        #elif defined(_WAVECOUNT_THREE)
            #define LAYER_COUNT 3
        #elif defined(_WAVECOUNT_TWO)
            #define LAYER_COUNT 2
        #else
            #define LAYER_COUNT 1
        #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;   // 整矩形归一化坐标(由 UIGradientMeshUV 写入)
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 rectUV : TEXCOORD1;
                float4 mask : TEXCOORD2;
            };

            // 获取动画时间：内置时间 或 外部驱动时间
            float GetTime()
            {
            #ifdef _INTERNAL_TIME_ON
                return _Time.y;
            #else
                return _DeltaTime;
            #endif
            }

            // 单层波形高度：wave=(x振幅, y频率(横向周期数), z速度(周期/秒,正左移/负右移), w相位)
            half WaveOffset(float4 wave, half x, float t)
            {
                return wave.x * _GlobalWaveAmp
                       * sin(TWO_PI * (wave.y * x + wave.z * _GlobalWaveSpeed * t) + wave.w);
            }

            // 叠加单个水层到底色上：layerIndex 层序(0=最前层)，layerCol 本层手动水颜色
            void ApplyLayer(inout half4 col, float4 wave, half4 layerCol, half layerIndex, half2 uv, float t)
            {
                // 本层水面高度：基础水位 + 层间抬升 + 本层波形
                half layerY = _WaterLevel + layerIndex * _LayerLevelStep + WaveOffset(wave, uv.x, t);
                // 水体掩码：1=水下（水面处带柔和过渡）
                half waterMask = 1.0 - smoothstep(layerY - _WaterEdgeSoftness, layerY + _WaterEdgeSoftness, uv.y);
                col.rgb = lerp(col.rgb, layerCol.rgb, waterMask * layerCol.a);
                // 让透明底区域也显出水量
                col.a = lerp(col.a, max(col.a, layerCol.a), waterMask);

                // 本层水面亮线(颜色随层序向水色靠深)
            #ifdef _SURFACE_ON
                half surfLine = 1.0 - smoothstep(0.0, max(_SurfaceWidth, 1e-4), abs(uv.y - layerY));
                half3 surfCol = lerp(_SurfaceColor.rgb, layerCol.rgb, 0.5);
                col.rgb = lerp(col.rgb, surfCol, surfLine * _SurfaceColor.a);
                col.a = max(col.a, surfLine * _SurfaceColor.a);
            #endif
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.rectUV = IN.uv1.xy;
                OUT.color = IN.color * _Color;

                // UGUI 矩形裁剪所需的 mask 计算
                float2 pixelSize = OUT.positionHCS.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskSoft = float2(_UIMaskSoftnessX, _UIMaskSoftnessY);
                OUT.mask = float4(IN.positionOS.xy * 2 - clampedRect.xy - clampedRect.zw,
                                  0.25 / (0.25 * maskSoft + abs(pixelSize.xy)));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = GetTime();

                // 波纹/水位所用的 0~1 归一化坐标：默认 UV0，图集/9宫格时用 UV1
            #ifdef _RECTUV_ON
                half2 waveUV = IN.rectUV;
            #else
                half2 waveUV = IN.uv;
            #endif

                // === 基础采样 ===
            #ifdef _BASETEX_ON
                // 底图模式：水上区域显示底图(容器贴图)，水体染色叠在图上
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(IN.uv, _MainTex)) * IN.color;
            #else
                // 纯水模式：水上区域透明；以前层水色为基底保证单层时水下是正好的水颜色
                half4 col = half4(_LayerColor1.rgb, 0);
            #endif

                // === 水层叠加：从最后一层画到最前层(每层独立颜色,前层半透明盖住后层) ===
            #if LAYER_COUNT >= 4
                ApplyLayer(col, _Wave4, _LayerColor4, 3, waveUV, t);
            #endif
            #if LAYER_COUNT >= 3
                ApplyLayer(col, _Wave3, _LayerColor3, 2, waveUV, t);
            #endif
            #if LAYER_COUNT >= 2
                ApplyLayer(col, _Wave2, _LayerColor2, 1, waveUV, t);
            #endif
                ApplyLayer(col, _Wave1, _LayerColor1, 0, waveUV, t);

                // === 整体透明度 ===
                col.a *= _MainAlpha;

                // === UGUI 矩形裁剪 ===
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                col.a *= lerp(1.0, m.x * m.y, _UseUIClipRect);

                // === UI 透明裁剪 ===
                if (_UseUIAlphaClip > 0.5)
                {
                    clip(col.a - 0.001);
                }

                col.a = saturate(col.a);
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "UI/Default"
}
