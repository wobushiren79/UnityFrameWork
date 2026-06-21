// UGUI Image 帧动画(精灵表) Shader
// 适用于 URP + UGUI(Image / RawImage)，自包含无外部 hlsl 依赖。
// 把一张按"列x行"排布的精灵表(Sprite Sheet)按设定帧率逐帧切片播放，
// 通过设置 列数(横向帧数)/行数(纵向帧数)/总帧数/帧率 即可播放一张图的帧动画。
// 精灵表帧序约定：从左到右、从上到下(第 0 帧在左上角)。
Shader "FrameWork/UI/Shader_UI_FrameAnimation"
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
        [PerRendererData] _MainTex("精灵表(主纹理)", 2D) = "white" {}
        [HDR] _Color("整体着色(乘法)", Color) = (1,1,1,1)
        _MainAlpha("整体透明度", Range(0, 2)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("源混合因子", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("目标混合因子", Float) = 10
        [Enum(Off, 0, On, 1)] _UseUIAlphaClip("启用UI透明裁剪", Float) = 1
        [Enum(Off, 0, On, 1)] _UseUIClipRect("启用UI矩形裁剪", Float) = 1

        [Header(Frame Animation)]
        _Cols("列数(横向帧数)", Float) = 4
        _Rows("行数(纵向帧数)", Float) = 4
        // 总帧数：留 0 表示用 列数x行数(整张精灵表铺满)；若末行有空帧，填实际帧数以跳过空帧。
        _FrameCount("总帧数(0=列x行)", Float) = 0
        _FPS("播放帧率(帧/秒)", Float) = 12
        _StartFrame("起始帧索引", Float) = 0
        [Enum(Off, 0, On, 1)] _Loop("循环播放(关则停在末帧)", Float) = 1
        [Toggle(_INTERNAL_TIME_ON)] _InternalTime("使用内置时间(关闭则用外部DeltaTime)", Float) = 1
        _DeltaTime("外部驱动时间(秒)", Float) = 0
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
        Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma shader_feature_local _INTERNAL_TIME_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // SRP Batcher 兼容：每材质常量统一放入 UnityPerMaterial
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _MainAlpha;
                half _UseUIAlphaClip;
                half _UseUIClipRect;
                float _DeltaTime;

                float _Cols;
                float _Rows;
                float _FrameCount;
                float _FPS;
                float _StartFrame;
                half _Loop;
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
                float4 worldPosition : TEXCOORD1;
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

            // 根据时间计算当前帧索引，并把原始 UV 映射到精灵表中该帧的子区块
            // 精灵表帧序：从左到右、从上到下(第 0 帧在左上角)
            float2 GetFrameUV(float2 uv, float time)
            {
                float cols = max(_Cols, 1.0);
                float rows = max(_Rows, 1.0);
                // 总帧数为 0 时取整张精灵表(列x行)
                float total = _FrameCount > 0.5 ? _FrameCount : cols * rows;
                total = max(total, 1.0);

                // 按帧率推进，再叠加起始帧
                float frame = floor(time * _FPS) + _StartFrame;
                if (_Loop > 0.5)
                {
                    // 循环：真模运算(frame - floor(frame/total)*total)，结果恒落在 [0,total)
                    frame = frame - floor(frame / total) * total;
                }
                else
                {
                    // 不循环：钳制在末帧
                    frame = clamp(frame, 0.0, total - 1.0);
                }

                float col = fmod(frame, cols);
                float row = floor(frame / cols);
                // 纹理 UV 的 v=0 在底部，精灵表第 0 行在顶部，需翻转行号
                float vRow = (rows - 1.0) - row;

                float2 tile = float2(1.0 / cols, 1.0 / rows);
                return uv * tile + float2(col * tile.x, vRow * tile.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.worldPosition = IN.positionOS;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // 先套用主纹理 Tiling/Offset，帧切片在片元里基于该 UV 处理
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
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

                // === 帧动画：把 UV 切到当前帧子区块 ===
                float2 frameUV = GetFrameUV(IN.uv, t);

                // === 基础采样 ===
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, frameUV) * IN.color;

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
