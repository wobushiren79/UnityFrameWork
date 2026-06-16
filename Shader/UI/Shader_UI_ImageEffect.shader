// UGUI Image 通用特效 Shader
// 适用于 URP + UGUI(Image / RawImage)，自包含无外部 hlsl 依赖。
// 通过 Toggle 关键字开关各项特效，未开启的特效不参与编译，零开销。
// 参考自 Gala/HonorCardBackVFXV2，提炼并通用化其 UV变换/流光/渐变/扭曲/溶解/极坐标 等思路。
Shader "FrameWork/UI/Shader_UI_ImageEffect"
{
    Properties
    {
        // 以下模板(Stencil)与颜色掩码参数为 UGUI Mask/RectMask2D 遮罩系统专用，
        // 运行时由 Mask 组件通过 MaterialPropertyBlock 自动写入，无需手动修改，
        // 保留它们只是为了让本特效 Image 能被父级 Mask 正确裁剪，删除会导致遮罩失效。
        // (自定义面板 ShaderGUIImageEffect 已把这组参数收进折叠的“UGUI 遮罩(模板)”分组)
        [Header(Stencil for UGUI Mask  auto set)]
        _StencilComp("模板比较方式", Float) = 8
        _Stencil("模板ID", Float) = 0
        _StencilOp("模板操作", Float) = 0
        _StencilWriteMask("模板写入掩码", Float) = 255
        _StencilReadMask("模板读取掩码", Float) = 255
        _ColorMask("颜色通道掩码", Float) = 15

        [Header(Base)]
        [PerRendererData] _MainTex("主纹理", 2D) = "white" {}
        [HDR] _Color("整体着色(乘法)", Color) = (1,1,1,1)
        _MainAlpha("整体透明度", Range(0, 2)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("源混合因子", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("目标混合因子", Float) = 10
        [Toggle(_INTERNAL_TIME_ON)] _InternalTime("使用内置时间(关闭则用外部DeltaTime)", Float) = 1
        _DeltaTime("外部驱动时间(秒)", Float) = 0
        [Enum(Off, 0, On, 1)] _UseUIAlphaClip("启用UI透明裁剪", Float) = 1
        [Enum(Off, 0, On, 1)] _UseUIClipRect("启用UI矩形裁剪", Float) = 1

        [Header(Main UV (Rotation and Flow))]
        [Toggle(_MAINUV_ON)] _MainUVOn("启用主纹理UV变换", Float) = 0
        _MainTexRotation("主纹理旋转(度)", Range(0, 360)) = 0
        _ScrollSpeedX("主纹理横向流动速度", Float) = 0
        _ScrollSpeedY("主纹理纵向流动速度", Float) = 0

        [Header(Polar Coordinates (Radial))]
        [Toggle(_POLAR_ON)] _PolarOn("启用极坐标(径向/漩涡)", Float) = 0
        _PolarRotateSpeed("极坐标旋转速度", Float) = 0
        _PolarScale("极坐标缩放(半径,角度)", Vector) = (1, 1, 0, 0)
        _PolarOffset("极坐标偏移(半径,角度)", Vector) = (0, 0, 0, 0)

        [Header(Distortion (UV Warp))]
        [Toggle(_DISTORT_ON)] _DistortOn("启用扭曲", Float) = 0
        _DistortTex("扭曲纹理(噪声图)", 2D) = "gray" {}
        [Enum(R, 0, A, 1)] _DistortChannel("扭曲取样通道", Float) = 0
        _DistortStrength("扭曲强度", Range(0, 1)) = 0.1
        _DistortRotation("扭曲纹理旋转(度)", Range(0, 360)) = 0
        _DistortScrollX("扭曲横向流动速度", Float) = 0
        _DistortScrollY("扭曲纵向流动速度", Float) = 0

        [Header(Mask Texture)]
        [Toggle(_MASKTEX_ON)] _MaskOn("启用遮罩纹理", Float) = 0
        _MaskTex("遮罩纹理(R/A通道)", 2D) = "white" {}
        [Enum(R, 0, A, 1)] _MaskChannel("遮罩取样通道", Float) = 0
        _MaskRotation("遮罩旋转(度)", Range(0, 360)) = 0
        _MaskScrollX("遮罩横向流动速度", Float) = 0
        _MaskScrollY("遮罩纵向流动速度", Float) = 0

        [Header(Shine (Sweep Light))]
        [Toggle(_SHINE_ON)] _ShineOn("启用流光扫光", Float) = 0
        [HDR] _ShineColor("流光颜色", Color) = (1,1,1,1)
        _ShineAngle("流光方向角度(度)", Range(0, 360)) = 45
        _ShineWidth("流光宽度", Range(0.01, 1)) = 0.15
        _ShineSoftness("流光边缘柔和度", Range(0, 1)) = 0.5
        _ShineSpeed("流光速度", Float) = 1
        _ShineInterval("流光间隔(扫过后的停顿)", Range(0, 10)) = 1
        _ShineIntensity("流光强度", Range(0, 5)) = 1
        [Enum(Off, 0, On, 1)] _ShineMaskByAlpha("仅在不透明区域显示流光", Float) = 1

        [Header(Gradient (Color Ramp))]
        [Toggle(_GRADIENT_ON)] _GradientOn("启用渐变叠色", Float) = 0
        [HDR] _GradientColorA("渐变起始色", Color) = (1,1,1,1)
        [HDR] _GradientColorB("渐变结束色", Color) = (0.5,0.5,1,1)
        _GradientAngle("渐变方向角度(度)", Range(0, 360)) = 90
        _GradientScale("渐变缩放(对比度)", Range(0.01, 5)) = 1
        _GradientOffset("渐变偏移", Range(-1, 1)) = 0
        _GradientScrollSpeed("渐变流动速度", Float) = 0
        [Enum(Multiply, 0, Add, 1, Replace, 2)] _GradientBlend("渐变混合模式", Float) = 0
        _GradientIntensity("渐变混合强度", Range(0, 1)) = 1

        [Header(Hue Shift (Rainbow))]
        [Toggle(_HUE_ON)] _HueOn("启用色相流动(彩虹)", Float) = 0
        _HueShiftSpeed("色相流动速度", Float) = 0.2
        _HueShiftOffset("色相起始偏移", Range(0, 1)) = 0
        _HueRangeScale("色相空间渐变(按UV方向铺开)", Range(0, 4)) = 0
        _HueAngle("色相铺开方向角度(度)", Range(0, 360)) = 0
        _HueSaturation("饱和度倍率", Range(0, 3)) = 1

        [Header(Dissolve)]
        [Toggle(_DISSOLVE_ON)] _DissolveOn("启用溶解", Float) = 0
        _DissolveTex("溶解噪声图(R/A通道)", 2D) = "white" {}
        [Enum(R, 0, A, 1)] _DissolveChannel("溶解取样通道", Float) = 0
        _DissolveAmount("溶解程度(0完整,1消失)", Range(0, 1)) = 0
        _DissolveEdgeWidth("溶解边缘宽度", Range(0, 0.5)) = 0.05
        _DissolveSoftness("溶解边缘柔和度", Range(0.001, 0.5)) = 0.02
        [HDR] _DissolveEdgeColor("溶解边缘发光色", Color) = (1,0.6,0.1,1)
        _DissolveRotation("溶解纹理旋转(度)", Range(0, 360)) = 0
        _DissolveScrollX("溶解横向流动速度", Float) = 0
        _DissolveScrollY("溶解纵向流动速度", Float) = 0

        [Header(Pulse (Breathing))]
        [Toggle(_PULSE_ON)] _PulseOn("启用呼吸闪烁", Float) = 0
        _PulseSpeed("呼吸频率", Float) = 1
        _PulseMin("呼吸最小亮度", Range(0, 1)) = 0.5
        _PulseMax("呼吸最大亮度", Range(0, 2)) = 1
        [Enum(Brightness, 0, Alpha, 1)] _PulseTarget("呼吸作用对象", Float) = 0
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
            #pragma shader_feature_local _MAINUV_ON
            #pragma shader_feature_local _POLAR_ON
            #pragma shader_feature_local _DISTORT_ON
            #pragma shader_feature_local _MASKTEX_ON
            #pragma shader_feature_local _SHINE_ON
            #pragma shader_feature_local _GRADIENT_ON
            #pragma shader_feature_local _HUE_ON
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma shader_feature_local _PULSE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 注：PI / TWO_PI 已由 URP 的 Macros.hlsl(经 Core.hlsl 引入)定义，直接复用，不再重复 #define。

            // SRP Batcher 兼容：每材质常量统一放入 UnityPerMaterial
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _MainAlpha;
                half _UseUIAlphaClip;
                half _UseUIClipRect;
                float _DeltaTime;

                half _MainTexRotation;
                float _ScrollSpeedX;
                float _ScrollSpeedY;

                half _PolarRotateSpeed;
                float4 _PolarScale;
                float4 _PolarOffset;

                float4 _DistortTex_ST;
                half _DistortChannel;
                half _DistortStrength;
                half _DistortRotation;
                float _DistortScrollX;
                float _DistortScrollY;

                float4 _MaskTex_ST;
                half _MaskChannel;
                half _MaskRotation;
                float _MaskScrollX;
                float _MaskScrollY;

                half4 _ShineColor;
                half _ShineAngle;
                half _ShineWidth;
                half _ShineSoftness;
                half _ShineSpeed;
                half _ShineInterval;
                half _ShineIntensity;
                half _ShineMaskByAlpha;

                half4 _GradientColorA;
                half4 _GradientColorB;
                half _GradientAngle;
                half _GradientScale;
                half _GradientOffset;
                half _GradientScrollSpeed;
                half _GradientBlend;
                half _GradientIntensity;

                half _HueShiftSpeed;
                half _HueShiftOffset;
                half _HueRangeScale;
                half _HueAngle;
                half _HueSaturation;

                float4 _DissolveTex_ST;
                half4 _DissolveEdgeColor;
                half _DissolveChannel;
                half _DissolveAmount;
                half _DissolveEdgeWidth;
                half _DissolveSoftness;
                half _DissolveRotation;
                float _DissolveScrollX;
                float _DissolveScrollY;

                half _PulseSpeed;
                half _PulseMin;
                half _PulseMax;
                half _PulseTarget;
            CBUFFER_END

            // UGUI 矩形裁剪由 Canvas 通过 MaterialPropertyBlock 设置，置于 CBUFFER 外
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DistortTex);
            SAMPLER(sampler_DistortTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            TEXTURE2D(_DissolveTex);
            SAMPLER(sampler_DissolveTex);

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

            // 绕中心旋转 + Tiling/Offset：先把 uv 平移到中心旋转，再套用 ST 缩放偏移
            float2 TransformUV(float2 uv, float4 st, half rotationDeg)
            {
                half a = radians(rotationDeg);
                half s = sin(a);
                half c = cos(a);
                uv -= 0.5;
                uv = float2(c * uv.x - s * uv.y, s * uv.x + c * uv.y);
                uv += 0.5;
                return uv * st.xy + st.zw;
            }

            // 直角坐标 → 极坐标(x=半径, y=角度)，可随时间旋转，并叠加缩放/偏移
            float2 PolarUV(float2 uv, half rotateSpeed, float2 scale, float2 offset, float time)
            {
                float2 d = uv * 2.0 - 1.0;
                float r = length(d);
                float theta = atan2(d.y, d.x) / TWO_PI + 0.5;
                theta += time * rotateSpeed;
                return float2(r, theta) * scale + offset;
            }

            // RGB 转 HSV
            half3 RGBtoHSV(half3 c)
            {
                half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
                half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));
                half d = q.x - min(q.w, q.y);
                half e = 1.0e-10;
                return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            // HSV 转 RGB
            half3 HSVtoRGB(half3 c)
            {
                half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                half3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            // 按角度把 UV 投影成 0~1 的方向标量（用于流光/渐变方向）
            half ProjectUV(float2 uv, half angleDeg)
            {
                half a = radians(angleDeg);
                half2 dir = half2(cos(a), sin(a));
                return dot(uv - 0.5, dir) + 0.5;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.worldPosition = IN.positionOS;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // 主纹理 Tiling/Offset/旋转 统一放到片元里处理，这里只传原始 UV
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

            half4 frag(Varyings IN) : SV_Target
            {
                float t = GetTime();

                // === 主纹理 UV：Tiling/Offset + 旋转 ===
                float2 mainUV = TransformUV(IN.uv, _MainTex_ST, _MainTexRotation);

                // === 极坐标(径向/漩涡) ===
            #ifdef _POLAR_ON
                mainUV = PolarUV(IN.uv, _PolarRotateSpeed, _PolarScale.xy, _PolarOffset.xy, t);
            #endif

                // === 主纹理流动 ===
            #ifdef _MAINUV_ON
                mainUV += frac(t * float2(_ScrollSpeedX, _ScrollSpeedY));
            #endif

                // === 扭曲(用噪声图偏移主UV) ===
            #ifdef _DISTORT_ON
                float2 distUV = TransformUV(IN.uv, _DistortTex_ST, _DistortRotation)
                                + frac(t * float2(_DistortScrollX, _DistortScrollY));
                half4 distTex = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distUV);
                half distVal = lerp(distTex.r, distTex.a, _DistortChannel);
                mainUV += (distVal - 0.5) * _DistortStrength;
            #endif

                // === 基础采样 ===
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV) * IN.color;

                // === 遮罩纹理(独立 Tiling/旋转/流动) ===
            #ifdef _MASKTEX_ON
                float2 maskUV = TransformUV(IN.uv, _MaskTex_ST, _MaskRotation)
                                + frac(t * float2(_MaskScrollX, _MaskScrollY));
                half4 maskTex = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, maskUV);
                half maskValue = lerp(maskTex.r, maskTex.a, _MaskChannel);
                col.a *= maskValue;
            #endif

                // === 渐变叠色 ===
            #ifdef _GRADIENT_ON
                half gradFactor = ProjectUV(IN.uv, _GradientAngle);
                gradFactor = (gradFactor - 0.5) * _GradientScale + 0.5 + _GradientOffset
                             + t * _GradientScrollSpeed;
                // frac 形成可循环渐变（配合流动）；静态时 _GradientScale=1 即铺满 0~1
                gradFactor = frac(gradFactor);
                half4 gradCol = lerp(_GradientColorA, _GradientColorB, gradFactor);
                half3 gradBlended;
                if (_GradientBlend < 0.5)       gradBlended = col.rgb * gradCol.rgb;   // 正片叠底
                else if (_GradientBlend < 1.5)  gradBlended = col.rgb + gradCol.rgb;   // 叠加
                else                            gradBlended = gradCol.rgb;             // 替换
                col.rgb = lerp(col.rgb, gradBlended, _GradientIntensity * gradCol.a);
            #endif

                // === 色相流动(彩虹) ===
            #ifdef _HUE_ON
                half3 hsv = RGBtoHSV(col.rgb);
                half hueOffset = ProjectUV(IN.uv, _HueAngle) * _HueRangeScale;
                hsv.x = frac(hsv.x + _HueShiftOffset + t * _HueShiftSpeed + hueOffset);
                hsv.y = saturate(hsv.y * _HueSaturation);
                col.rgb = HSVtoRGB(hsv);
            #endif

                // === 流光扫光 ===
            #ifdef _SHINE_ON
                half proj = ProjectUV(IN.uv, _ShineAngle);
                // 扫光头在 [-width, 1+width] 间往返，间隔段停留在画面外形成停顿
                half travelRange = 1.0 + 2.0 * _ShineWidth + _ShineInterval;
                half sweepPos = -_ShineWidth + frac(t * _ShineSpeed) * travelRange;
                half shineEdge = _ShineWidth * (1.0 - _ShineSoftness);
                half shine = 1.0 - smoothstep(shineEdge, _ShineWidth, abs(proj - sweepPos));
                half shineMask = lerp(1.0, col.a, _ShineMaskByAlpha);
                // 扫光带强度(含流光色自身透明度)
                half shineBand = shine * _ShineColor.a;
                // 在带内把底色推向流光色(lerp)，颜色因此真正生效；_ShineIntensity 控制亮度(可>1 形成HDR辉光)
                col.rgb = lerp(col.rgb, _ShineColor.rgb * _ShineIntensity, saturate(shineBand * shineMask));
                // 仅当关闭“仅不透明区显示”时，允许流光在透明区域点亮 alpha
                col.a = saturate(col.a + shineBand * (1.0 - _ShineMaskByAlpha));
            #endif

                // === 溶解 ===
            #ifdef _DISSOLVE_ON
                float2 disUV = TransformUV(IN.uv, _DissolveTex_ST, _DissolveRotation)
                               + frac(t * float2(_DissolveScrollX, _DissolveScrollY));
                half4 disTex = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, disUV);
                half disNoise = lerp(disTex.r, disTex.a, _DissolveChannel);
                half disSoft = max(_DissolveSoftness, 1e-4);
                // 主体 alpha：噪声高于阈值的部分保留
                half disBody = smoothstep(_DissolveAmount, _DissolveAmount + disSoft, disNoise);
                // 边缘带：阈值与(阈值+边缘宽度)之间的环形区域
                half disEdge = disBody - smoothstep(_DissolveAmount + _DissolveEdgeWidth,
                                                    _DissolveAmount + _DissolveEdgeWidth + disSoft, disNoise);
                col.rgb = lerp(col.rgb, _DissolveEdgeColor.rgb * _DissolveEdgeColor.a, saturate(disEdge));
                col.a *= disBody;
            #endif

                // === 呼吸闪烁 ===
            #ifdef _PULSE_ON
                half pulse = lerp(_PulseMin, _PulseMax, sin(t * _PulseSpeed * TWO_PI) * 0.5 + 0.5);
                if (_PulseTarget < 0.5) col.rgb *= pulse;   // 作用于亮度
                else                    col.a *= pulse;     // 作用于透明度
            #endif

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

    CustomEditor "ShaderGUIImageEffect"
    Fallback "UI/Default"
}
