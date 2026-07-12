Shader "FrameWork/URP/MeshProgressBar"
{
    Properties
    {
        [Header(Shape)]
        [Enum(Bar,0,Radial,1)] _ShapeType ("进度条形状 (0=条形 / 1=圆形)", Float) = 0

        [Header(Background)]
        _BgMap ("背景贴图 (进度条底图)", 2D) = "white" {}
        [HDR] _BgColor ("背景颜色", Color) = (0.15, 0.15, 0.15, 1)

        [Header(Background Gradient)]
        [Toggle] _BgGradientEnable ("背景渐变开关 (开=背景用两色线性渐变 / 关=纯背景色 / 默认关闭)", Float) = 0
        [HDR] _BgColor2 ("背景渐变结束颜色 (渐变另一端 / 起点用背景颜色)", Color) = (0.05, 0.05, 0.05, 1)
        [Enum(LeftToRight,0,RightToLeft,1,BottomToTop,2,TopToBottom,3)]
        _BgGradientDirection ("背景渐变方向 (颜色过渡方向)", Float) = 2

        [Header(Fill)]
        _FillMap ("进度贴图 (随进度填充的图)", 2D) = "white" {}
        [HDR] _FillColor ("进度颜色", Color) = (0.2, 0.9, 0.3, 1)

        [Header(Composite)]
        [Toggle] _FillShowThrough ("进度独立显示 (开=进度不被背景透明区裁剪 / 关=背景当轮廓蒙版 / 默认开)", Float) = 1

        [Header(Progress)]
        _Progress ("当前进度 (0=空 / 1=满)", Range(0, 1)) = 0.5
        [Enum(LeftToRight,0,RightToLeft,1,BottomToTop,2,TopToBottom,3)]
        _FillDirection ("条形填充方向 (进度增长方向 / 仅条形)", Float) = 0
        _EdgeSoftness ("填充边缘软化 (0=硬边 / 越大越柔和)", Range(0, 0.2)) = 0.005

        [Header(Radial)]
        [Enum(Clockwise,0,CounterClockwise,1)]
        _RadialDirection ("圆形进度方向 (0=顺时针 / 1=逆时针 / 仅圆形)", Float) = 0
        _RadialStartAngle ("圆形起始角度 (从12点顺时针 单位度 / 仅圆形)", Range(0, 360)) = 0

        [Header(Radial Background Rotate)]
        [Toggle] _BgRotateEnable ("背景旋转开关 (仅圆形有效 / 默认关闭)", Float) = 0
        _BgRotateSpeed ("背景旋转速度 (度每秒)", Float) = 60
        [Enum(Clockwise,0,CounterClockwise,1)]
        _BgRotateDirection ("背景旋转方向 (0=顺时针 / 1=逆时针)", Float) = 0

        [Header(Radial Fill Rotate)]
        [Toggle] _FillRotateEnable ("进度旋转开关 (仅圆形有效 / 默认关闭)", Float) = 0
        _FillRotateSpeed ("进度旋转速度 (度每秒)", Float) = 60
        [Enum(Clockwise,0,CounterClockwise,1)]
        _FillRotateDirection ("进度旋转方向 (0=顺时针 / 1=逆时针)", Float) = 0

        [Header(Highlight)]
        [Toggle] _HighlightEnable ("开启高光版 (让颜色发光 / 需开 Bloom 后处理)", Float) = 0
        _BgHighlight ("背景高光强度", Range(1, 8)) = 2
        _FillHighlight ("进度高光强度", Range(1, 8)) = 2

        [Header(Lit)]
        [Toggle(_LIT_ON)] _LitEnable ("开启光照 (受主光/附加光/阴影/环境光 / 默认关闭)", Float) = 0

        [Header(Render Face)]
        [Enum(On,0,Off,2)] _Cull ("是否开启双面渲染 (On=两面都显示 / Off=仅显示正面)", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        // 半透明叠加：进度条通常悬浮显示，不写深度、不投影
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma shader_feature_local _LIT_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // 光照库(SurfaceData/InputData/BlinnPhong) → 通用受光助手
            #include "../Common/CommonLit.hlsl"

            TEXTURE2D(_BgMap);   SAMPLER(sampler_BgMap);
            TEXTURE2D(_FillMap); SAMPLER(sampler_FillMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BgMap_ST;
                float4 _FillMap_ST;
                half4  _BgColor;
                half4  _BgColor2;
                half   _BgGradientEnable;
                half   _BgGradientDirection;
                half4  _FillColor;
                half   _Progress;
                half   _FillDirection;
                half   _EdgeSoftness;
                half   _HighlightEnable;
                half   _BgHighlight;
                half   _FillHighlight;
                half   _ShapeType;
                half   _RadialDirection;
                half   _RadialStartAngle;
                half   _BgRotateEnable;
                half   _BgRotateSpeed;
                half   _BgRotateDirection;
                half   _FillRotateEnable;
                half   _FillRotateSpeed;
                half   _FillRotateDirection;
                half   _FillShowThrough;
            CBUFFER_END

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

            /// 按填充方向取该像素的进度坐标(0~1)：值 <= 进度处视为已填充
            half GetFillCoord (float2 uv)
            {
                if (_FillDirection < 0.5)      return uv.x;         // 左 → 右
                else if (_FillDirection < 1.5) return 1.0 - uv.x;   // 右 → 左
                else if (_FillDirection < 2.5) return uv.y;         // 下 → 上
                else                           return 1.0 - uv.y;   // 上 → 下
            }

            /// 按方向取该像素沿 UV 的线性渐变系数(0~1), 供背景渐变在两色间插值
            half GetGradientCoord (float2 uv, half dir)
            {
                if (dir < 0.5)      return uv.x;         // 左 → 右
                else if (dir < 1.5) return 1.0 - uv.x;   // 右 → 左
                else if (dir < 2.5) return uv.y;         // 下 → 上
                else                return 1.0 - uv.y;   // 上 → 下
            }

            /// 圆形进度坐标(0~1)：以 UV 中心为圆心, 从12点起按方向绕一圈, 值 <= 进度处视为已填充
            half GetRadialCoord (float2 uv)
            {
                float2 d = uv - 0.5;
                // atan2(x,y): 0 在正上方(12点), 顺时针增大 → 归一化到 0~1
                half t = frac(atan2(d.x, d.y) / TWO_PI + 1.0);
                t = frac(t - _RadialStartAngle / 360.0 + 1.0);     // 起始角度偏移
                if (_RadialDirection > 0.5) t = frac(1.0 - t);     // 逆时针则反向
                return t;
            }

            /// 按时间绕 UV 中心旋转采样坐标(仅圆形且开启时生效), 供背景/进度复用以让贴图转动
            float2 RotateUVAroundCenter (float2 uv, half enable, half speed, half dir)
            {
                if (_ShapeType < 0.5 || enable < 0.5) return uv;
                // 顺时针为正: 旋转采样坐标(逆时针)使贴图视觉上顺时针转; 用 float 防止时间累积后半精度抖动
                float dirSign = (dir < 0.5) ? 1.0 : -1.0;
                float ang = _Time.y * radians(speed) * dirSign;
                float s, c;
                sincos(ang, s, c);
                float2 p = uv - 0.5;
                return float2(p.x * c - p.y * s, p.x * s + p.y * c) + 0.5;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = IN.uv;
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // 圆形下可绕中心旋转的采样坐标: 背景仅贴图转; 进度的 fillUV 同时驱动贴图与进度弧角度(整条弧一起转)
                float2 bgUV   = RotateUVAroundCenter(IN.uv, _BgRotateEnable,   _BgRotateSpeed,   _BgRotateDirection);
                float2 fillUV = RotateUVAroundCenter(IN.uv, _FillRotateEnable, _FillRotateSpeed, _FillRotateDirection);
                // 背景着色：开渐变时按方向在 背景颜色 → 渐变结束颜色 间线性插值(用未旋转的 IN.uv 使渐变随网格稳定)
                half4 bgTint = _BgColor;
                if (_BgGradientEnable > 0.5)
                {
                    bgTint = lerp(_BgColor, _BgColor2, GetGradientCoord(IN.uv, _BgGradientDirection));
                }
                half4 bg   = SAMPLE_TEXTURE2D(_BgMap,   sampler_BgMap,   TRANSFORM_TEX(bgUV,   _BgMap))   * bgTint;
                half4 fill = SAMPLE_TEXTURE2D(_FillMap, sampler_FillMap, TRANSFORM_TEX(fillUV, _FillMap)) * _FillColor;

                // 高光版：分别放大背景/进度颜色的亮度(配合 Bloom 产生发光)
                if (_HighlightEnable > 0.5)
                {
                    bg.rgb   *= _BgHighlight;
                    fill.rgb *= _FillHighlight;
                }

                // 进度遮罩：条形按方向取坐标; 圆形按角度取坐标, 用旋转后的 fillUV 让整条进度弧(含贴图)整体旋转
                half coord = (_ShapeType < 0.5) ? GetFillCoord(IN.uv) : GetRadialCoord(fillUV);
                half fillMask = 1.0 - smoothstep(_Progress - _EdgeSoftness, _Progress + _EdgeSoftness, coord);

                // 进度覆盖量：进度范围内 * 进度图自身 alpha
                half over = fillMask * fill.a;

                half3 rgb;
                half  a;
                if (_FillShowThrough > 0.5)
                {
                    // 独立显示：背景/进度各带 alpha 做标准 source-over, 背景透明处也能看到纯净进度色
                    a   = over + bg.a * (1.0 - over);
                    rgb = (fill.rgb * over + bg.rgb * bg.a * (1.0 - over)) / max(a, 1e-4h);
                }
                else
                {
                    // 蒙版模式：进度色叠在背景上, 整体形状由背景 alpha 裁剪
                    rgb = lerp(bg.rgb, fill.rgb, over);
                    a   = bg.a;
                }

                #if defined(_LIT_ON)
                    // 双面渲染时背面法线翻转，保证两面都能正确受光
                    half3 normalWS = normalize(IN.normalWS) * IS_FRONT_VFACE(cullFace, 1.0, -1.0);
                    return ApplyCommonLit(rgb, a, IN.positionWS, normalWS, IN.positionHCS, IN.fogFactor);
                #else
                    rgb = MixFog(rgb, IN.fogFactor);
                    return half4(rgb, a);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
    CustomEditor "MeshProgressBarShaderGUI"
}
