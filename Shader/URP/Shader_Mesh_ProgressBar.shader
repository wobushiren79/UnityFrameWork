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

        [Header(Background Time Gradient)]
        [Toggle] _BgTimeGradientEnable ("背景时间渐变开关 (开=背景色随时间在 背景颜色-渐变结束颜色 间循环 / 默认关闭)", Float) = 0
        _BgTimeGradientSpeed ("背景时间渐变速度 (每秒循环次数 / 越大变色越快)", Float) = 0.5

        [Header(Background Flow Light)]
        [Toggle] _BgFlowLightEnable ("背景流光开关 (开=背景上有一条循环扫过的亮带 / 默认关闭)", Float) = 0
        [HDR] _BgFlowLightColor ("背景流光颜色 (HDR>1 配合 Bloom 发光)", Color) = (1, 1, 1, 1)
        _BgFlowLightSpeed ("背景流光速度 (每秒扫过次数 / 负值反向)", Float) = 0.5
        _BgFlowLightWidth ("背景流光宽度 (亮带宽度占循环周期的比例)", Range(0.01, 0.5)) = 0.12
        _BgFlowLightAngle ("背景流光角度 (0=左到右 / 45=对角 / 90=下到上)", Range(0, 360)) = 45
        _BgFlowLightSoftness ("背景流光软边 (亮带边缘柔和度)", Range(0.001, 0.5)) = 0.08

        [Header(Fill)]
        _FillMap ("进度贴图 (随进度填充的图)", 2D) = "white" {}
        [HDR] _FillColor ("进度颜色", Color) = (0.2, 0.9, 0.3, 1)

        [Header(Fill Gradient)]
        [Toggle] _FillGradientEnable ("进度渐变开关 (开=进度用两色线性渐变 / 关=纯进度色 / 默认关闭)", Float) = 0
        [HDR] _FillColor2 ("进度渐变结束颜色 (渐变另一端 / 起点用进度颜色)", Color) = (0.1, 0.5, 0.15, 1)
        [Enum(LeftToRight,0,RightToLeft,1,BottomToTop,2,TopToBottom,3)]
        _FillGradientDirection ("进度渐变方向 (颜色过渡方向)", Float) = 0

        [Header(Fill Time Gradient)]
        [Toggle] _FillTimeGradientEnable ("进度时间渐变开关 (开=进度色随时间在 进度颜色-渐变结束颜色 间循环 / 默认关闭)", Float) = 0
        _FillTimeGradientSpeed ("进度时间渐变速度 (每秒循环次数 / 越大变色越快)", Float) = 0.5

        [Header(Fill Flow Light)]
        [Toggle] _FillFlowLightEnable ("进度流光开关 (开=进度条上有一条循环扫过的亮带 / 默认关闭)", Float) = 0
        [HDR] _FillFlowLightColor ("进度流光颜色 (HDR>1 配合 Bloom 发光)", Color) = (1, 1, 1, 1)
        _FillFlowLightSpeed ("进度流光速度 (每秒扫过次数 / 负值反向)", Float) = 0.5
        _FillFlowLightWidth ("进度流光宽度 (亮带宽度占循环周期的比例)", Range(0.01, 0.5)) = 0.12
        _FillFlowLightAngle ("进度流光角度 (0=左到右 / 45=对角 / 90=下到上)", Range(0, 360)) = 45
        _FillFlowLightSoftness ("进度流光软边 (亮带边缘柔和度)", Range(0.001, 0.5)) = 0.08

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

        // 表面类型/渲染模式/Alpha 裁剪/渲染面 由通用面板 SurfaceOptionsGUI 合并为"渲染设置"折叠组
        [Header(Surface Options)]
        [Enum(Opaque,0,Transparent,1)] _Surface ("表面类型 (0=不透明 / 1=透明 / 默认透明)", Float) = 1
        [Enum(AlphaBlend,0,Additive,1,PremultipliedAlpha,2)]
        _BlendMode ("渲染模式 (仅透明表面生效 / 0=标准透明 / 1=加法叠加发光 / 2=预乘透明)", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha 裁剪 (按阈值镂空丢弃像素 / 默认关闭)", Float) = 0
        _Cutoff ("裁剪阈值 (低于此 Alpha 的像素被丢弃 / 仅开启裁剪时生效)", Range(0, 1)) = 0.5
        [Enum(On,0,Off,2)] _Cull ("渲染面 (On=双面都显示 / Off=仅显示正面)", Float) = 2
        // 表面类型/渲染模式预设驱动的实际渲染状态(材质面板不直接暴露), 由 Blend/ZWrite 语句读取
        [HideInInspector] _SrcBlend ("__src", Float) = 5
        [HideInInspector] _DstBlend ("__dst", Float) = 10
        [HideInInspector] _ZWrite ("__zw", Float) = 0
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

        // 渲染状态由材质面板"表面类型/透明模式"预设驱动: 混合因子/深度写入/队列随之切换, 不投影
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma shader_feature_local _LIT_ON
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // 光照库(SurfaceData/InputData/BlinnPhong) → 通用受光助手
            #include "../Common/CommonLit.hlsl"
            // 通用渲染设置件(Alpha 裁剪 ApplyAlphaClip + _Cutoff 字段宏)
            #include "../Common/SurfaceOptions.hlsl"

            TEXTURE2D(_BgMap);   SAMPLER(sampler_BgMap);
            TEXTURE2D(_FillMap); SAMPLER(sampler_FillMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BgMap_ST;
                float4 _FillMap_ST;
                half4  _BgColor;
                half4  _BgColor2;
                half   _BgGradientEnable;
                half   _BgGradientDirection;
                half   _BgTimeGradientEnable;
                half   _BgTimeGradientSpeed;
                half   _BgFlowLightEnable;
                half4  _BgFlowLightColor;
                half   _BgFlowLightSpeed;
                half   _BgFlowLightWidth;
                half   _BgFlowLightAngle;
                half   _BgFlowLightSoftness;
                half4  _FillColor;
                half   _FillGradientEnable;
                half4  _FillColor2;
                half   _FillGradientDirection;
                half   _FillTimeGradientEnable;
                half   _FillTimeGradientSpeed;
                half   _FillFlowLightEnable;
                half4  _FillFlowLightColor;
                half   _FillFlowLightSpeed;
                half   _FillFlowLightWidth;
                half   _FillFlowLightAngle;
                half   _FillFlowLightSoftness;
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
                SURFACE_OPTIONS_CBUFFER   // Alpha 裁剪阈值 _Cutoff
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

            /// 流光带强度(0~1)：把 UV 投影到 angle 方向随时间滚动, 在 width 半宽内产生一条 softness 软边的循环亮带
            half GetFlowLightMask (float2 uv, half angle, half speed, half width, half softness)
            {
                float rad = radians(angle);
                float2 dir = float2(cos(rad), sin(rad));
                // 投影坐标(中心化后任意角度都能完整扫过整面), 随时间 frac 循环滚动; 用 float 防止时间累积后半精度抖动
                float coord = frac(dot(uv - 0.5, dir) + 0.5 - _Time.y * speed);
                // 亮带中心 0.5: 三角波 + smoothstep 软边(全程 float, 避免 half/float 混合提升)
                float d = abs(coord - 0.5);
                float w = width;
                float s = softness;
                return 1.0 - smoothstep(max(w - s, 0.0), w + s, d);
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

            /// 判定采样坐标是否落在 [0,1] UV 方框内: 旋转后越界的角落返回 0, 裁掉 Clamp 边缘拉伸拖影
            half UVInsideBox (float2 uv)
            {
                float2 inside = step(0.0, uv) * step(uv, float2(1.0, 1.0));
                return inside.x * inside.y;
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
                // 背景着色(用未旋转的 IN.uv 使渐变随网格稳定)：
                // 空间渐变=按方向在 背景颜色→渐变结束颜色 间线性插值; 时间渐变=让插值系数随时间循环
                half4 bgTint = _BgColor;
                if (_BgGradientEnable > 0.5)
                {
                    half g = GetGradientCoord(IN.uv, _BgGradientDirection);
                    // 叠加时间渐变时: 空间渐变沿方向随时间滚动流动(frac 循环)
                    if (_BgTimeGradientEnable > 0.5) g = frac(g + _Time.y * _BgTimeGradientSpeed);
                    bgTint = lerp(_BgColor, _BgColor2, g);
                }
                else if (_BgTimeGradientEnable > 0.5)
                {
                    // 纯时间渐变: 整片背景在两色间往复呼吸(sin 平滑无跳变)
                    half pulse = 0.5h + 0.5h * sin(_Time.y * _BgTimeGradientSpeed * TWO_PI);
                    bgTint = lerp(_BgColor, _BgColor2, pulse);
                }
                half4 bg   = SAMPLE_TEXTURE2D(_BgMap,   sampler_BgMap,   TRANSFORM_TEX(bgUV,   _BgMap))   * bgTint;
                // 进度着色(同背景用未旋转的 IN.uv 使渐变随网格稳定)：空间渐变=按方向在 进度颜色→渐变结束颜色 间插值; 时间渐变=系数随时间循环
                half4 fillTint = _FillColor;
                if (_FillGradientEnable > 0.5)
                {
                    half g = GetGradientCoord(IN.uv, _FillGradientDirection);
                    if (_FillTimeGradientEnable > 0.5) g = frac(g + _Time.y * _FillTimeGradientSpeed);
                    fillTint = lerp(_FillColor, _FillColor2, g);
                }
                else if (_FillTimeGradientEnable > 0.5)
                {
                    half pulse = 0.5h + 0.5h * sin(_Time.y * _FillTimeGradientSpeed * TWO_PI);
                    fillTint = lerp(_FillColor, _FillColor2, pulse);
                }
                half4 fill = SAMPLE_TEXTURE2D(_FillMap, sampler_FillMap, TRANSFORM_TEX(fillUV, _FillMap)) * fillTint;

                // 旋转后越界的角落置空: 防止 Clamp 环绕把贴边像素向角落拉伸成拖影(未旋转时 UV 在 [0,1] 内不受影响)
                bg   *= UVInsideBox(bgUV);
                fill *= UVInsideBox(fillUV);

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

                // 流光：背景/进度各一条随时间循环扫过的亮带(用未旋转 IN.uv 保持扫描方向稳定), 加法式叠在合成色上
                // 背景亮带乘 bg.a 只在背景可见处发光; 进度亮带乘 over(=进度遮罩*进度图alpha) 只在已填充区发光
                if (_BgFlowLightEnable > 0.5)
                {
                    half m = GetFlowLightMask(IN.uv, _BgFlowLightAngle, _BgFlowLightSpeed, _BgFlowLightWidth, _BgFlowLightSoftness);
                    rgb += _BgFlowLightColor.rgb * m * bg.a;
                }
                if (_FillFlowLightEnable > 0.5)
                {
                    half m = GetFlowLightMask(IN.uv, _FillFlowLightAngle, _FillFlowLightSpeed, _FillFlowLightWidth, _FillFlowLightSoftness);
                    rgb += _FillFlowLightColor.rgb * m * over;
                }

                // Alpha 裁剪(镂空)：开启 _ALPHATEST_ON 时丢弃低于 _Cutoff 的像素(不透明表面镂空常用)
                ApplyAlphaClip(a, _Cutoff);

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
