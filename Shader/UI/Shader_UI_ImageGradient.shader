// UGUI Image 双色渐变 Shader（手写 ShaderLab 版，替代 Shader_UI_ImageGradient_1.shadergraph）
// 支持：可选渐变方向（横/纵/双对角/自定义角度）、双色渐变、渐变中点偏移、锐利度、平滑过渡
// 渐变色属性名保持 _StartColor / _EndColor，与 GameUIUtil.SetGradientColor 直接兼容
// 重要：渐变坐标取自 UV1(TEXCOORD1) 的整矩形归一化坐标，而非 sprite 贴图 UV(TEXCOORD0)，
//       以规避图集子矩形 / 9-slice 导致的 UV 压缩。须配合 UIGradientMeshUV 组件写入 UV1，
//       且该组件会自动给 Canvas 开启 TexCoord1 附加通道，否则渐变退化为单色。
Shader "FrameWork/UI/Shader_UI_ImageGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("精灵贴图", 2D) = "white" {}

        [Header(Gradient)]
        _StartColor ("起始颜色(渐变A)", Color) = (1,1,1,1)
        _EndColor ("结束颜色(渐变B)", Color) = (0,0,0,1)
        [Enum(Horizontal,0,Vertical,1,Diagonal_TL_BR,2,Diagonal_BL_TR,3,Angle,4)]
        _DirectionMode ("渐变方向", Float) = 1
        _Angle ("自定义角度(方向选Angle时生效)", Range(0,360)) = 90
        _GradientOffset ("渐变中点偏移", Range(-1,1)) = 0
        _GradientScale ("渐变锐利度(越大过渡越急)", Range(0.01,4)) = 1
        [Toggle] _Smooth ("平滑过渡(smoothstep)", Float) = 1
        _Color ("整体叠加色(Tint)", Color) = (1,1,1,1)

        [Header(UI Stencil and Mask)]
        _StencilComp ("模板比较方式", Float) = 8
        _Stencil ("模板ID", Float) = 0
        _StencilOp ("模板操作", Float) = 0
        _StencilWriteMask ("模板写入掩码", Float) = 255
        _StencilReadMask ("模板读取掩码", Float) = 255
        _ColorMask ("颜色通道掩码", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("启用透明裁剪", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 gradUV   : TEXCOORD1;   // 整矩形归一化坐标(由 UIGradientMeshUV 写入)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 gradUV        : TEXCOORD2;   // 透传归一化渐变坐标
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _StartColor;
            fixed4 _EndColor;
            float _DirectionMode;
            float _Angle;
            float _GradientOffset;
            float _GradientScale;
            float _Smooth;

            // 根据方向模式计算 0~1 的渐变参数 t（含偏移/锐利度/平滑处理）
            float ComputeGradientT(float2 uv)
            {
                float t;
                if (_DirectionMode < 0.5)       t = uv.x;                          // 横向
                else if (_DirectionMode < 1.5)  t = uv.y;                          // 纵向
                else if (_DirectionMode < 2.5)  t = (uv.x + (1.0 - uv.y)) * 0.5;   // 左上->右下
                else if (_DirectionMode < 3.5)  t = (uv.x + uv.y) * 0.5;           // 左下->右上
                else                                                               // 自定义角度
                {
                    float rad = radians(_Angle);
                    float2 dir = float2(cos(rad), sin(rad));
                    t = dot(uv - 0.5, dir) + 0.5;
                }
                t = (t - 0.5) * _GradientScale + 0.5 + _GradientOffset;            // 以中点为基准缩放并偏移
                t = saturate(t);
                if (_Smooth > 0.5)
                    t = smoothstep(0.0, 1.0, t);
                return t;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.gradUV = v.gradUV;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float t = ComputeGradientT(IN.gradUV);
                half4 gradient = lerp(_StartColor, _EndColor, t);
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * gradient * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
