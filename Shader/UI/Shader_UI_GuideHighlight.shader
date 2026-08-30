// UGUI Image 引导高亮 Shader
// 适用于 URP + UGUI(Image)，自包含无外部 hlsl 依赖。
// 用途：新手引导/剧情引导遮罩——目标区域保持高亮(可透明穿透显示下方UI)，其余区域压暗。
// 支持形状：方形(可带圆角) / 圆形(椭圆)；高亮区中心、大小、边缘柔和度、压暗色、高亮叠加色均可自由设置。
// 坐标约定：_Center / _Size 使用 Image 自身 UV 空间(0~1)，_Size 为整幅宽高占比；
//           圆形取「以 _Size/2 为长短轴的椭圆」，要正圆请按 RectTransform 宽高比换算 _Size。
Shader "FrameWork/UI/Shader_UI_GuideHighlight"
{
    Properties
    {
        // 以下模板(Stencil)与颜色掩码参数为 UGUI Mask/RectMask2D 遮罩系统专用，
        // 运行时由 Mask 组件通过 MaterialPropertyBlock 自动写入，无需手动修改，
        // 保留它们只是为了让本 Image 能被父级 Mask 正确裁剪，删除会导致遮罩失效。
        [Header(Stencil for UGUI Mask  auto set)]
        _StencilComp("模板比较方式", Float) = 8
        _Stencil("模板ID", Float) = 0
        _StencilOp("模板操作", Float) = 0
        _StencilWriteMask("模板写入掩码", Float) = 255
        _StencilReadMask("模板读取掩码", Float) = 255
        _ColorMask("颜色通道掩码", Float) = 15

        [Header(Base)]
        [PerRendererData] _MainTex("主纹理", 2D) = "white" {}
        _Color("整体着色(乘法)", Color) = (1,1,1,1)
        [Enum(Off, 0, On, 1)] _UseUIAlphaClip("启用UI透明裁剪", Float) = 0
        [Enum(Off, 0, On, 1)] _UseUIClipRect("启用UI矩形裁剪", Float) = 1

        [Header(Highlight Area)]
        [Enum(Rect, 0, Circle, 1)] _ShapeType("高亮形状(方形/圆形)", Float) = 0
        _Center("高亮中心(UV 0~1)", Vector) = (0.5, 0.5, 0, 0)
        _Size("高亮大小(宽高占整幅比例)", Vector) = (0.3, 0.3, 0, 0)
        _CornerRadius("方形圆角半径(0~0.5, 仅方形生效)", Range(0, 0.5)) = 0
        _EdgeSoftness("边缘柔和过渡宽度", Range(0.001, 0.5)) = 0.02

        [Header(Colors)]
        _DimColor("压暗颜色(高亮区以外)", Color) = (0, 0, 0, 0.7)
        _HighlightColor("高亮叠加色(高亮区以内, alpha=0即纯透明)", Color) = (1, 1, 1, 0)
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // SRP Batcher 兼容：每材质常量统一放入 UnityPerMaterial
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _UseUIAlphaClip;
                half _UseUIClipRect;

                half _ShapeType;
                float4 _Center;
                float4 _Size;
                half _CornerRadius;
                half _EdgeSoftness;

                half4 _DimColor;
                half4 _HighlightColor;
            CBUFFER_END

            // UGUI 矩形裁剪由 Canvas 通过 MaterialPropertyBlock 设置，置于 CBUFFER 外
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 mask : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
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

            // 高亮区内外权重：返回 0=高亮区内，1=高亮区外(压暗区)
            half GetDimWeight(float2 uv)
            {
                float2 center = _Center.xy;
                float2 halfSize = max(_Size.xy * 0.5, 1e-5);
                half d;
                if (_ShapeType < 0.5)
                {
                    // 方形 SDF(带圆角)：d<0 在内部，d=0 恰在边界
                    half r = min(_CornerRadius, min(halfSize.x, halfSize.y));
                    float2 p = abs(uv - center) - halfSize + r;
                    d = length(max(p, 0.0)) + min(max(p.x, p.y), 0.0) - r;
                }
                else
                {
                    // 圆形(椭圆)：以 halfSize 为长短轴归一化，d<0 在内部
                    d = length((uv - center) / halfSize) - 1.0;
                }
                return smoothstep(0.0, max(_EdgeSoftness, 1e-4), d);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half dimWeight = GetDimWeight(IN.uv);

                // 高亮区内叠 _HighlightColor，区外叠 _DimColor，柔和过渡
                half4 areaCol = lerp(_HighlightColor, _DimColor, dimWeight);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * areaCol * IN.color;

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
