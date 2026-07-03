Shader "FrameWork/SpriteRenderer/Common1"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("主贴图", 2D) = "white" {}
        [MainColor]   _BaseColor ("染色颜色", Color) = (1, 1, 1, 1)
        _Cutoff ("透明裁剪阈值 (0=不裁剪)", Range(0, 1)) = 0.0

        [Header(Lit)]
        [Toggle(_LIT_ON)] _LitEnable ("开启光照 (受主光/附加光/阴影/环境光 / 默认关闭)", Float) = 0

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _OutlineEnable ("开启描边 (沿轮廓 Alpha 外扩 / 默认关闭)", Float) = 0
        [HDR] _OutlineColor ("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineSize ("描边大小 (向外扩展的纹素数)", Range(0, 10)) = 1.0

        [Header(Render Face)]
        [Enum(On,0,Off,2)] _Cull ("是否开启双面渲染 (On=两面都显示 / Off=仅显示正面)", Float) = 0

        [Header(Transform)]
        _VertexOffset ("位置偏移 (物体空间 XYZ)", Vector) = (0, 0, 0, 0)
        _VertexRotation ("角度旋转 (欧拉角 XYZ / 度)", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Pass
        {
            Name "SpriteForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _LIT_ON
            #pragma shader_feature_local _OUTLINE_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // 光照库(SurfaceData/InputData/BlinnPhong) → 通用受光助手；描边助手；顶点位移旋转助手
            #include "../Common/CommonLit.hlsl"
            #include "../Common/Outline.hlsl"
            #include "../Common/Transform.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseMap_TexelSize;   // Unity 按所赋贴图自动填充，供描边取纹素尺寸
                half4  _BaseColor;
                half   _Cutoff;
                half4  _OutlineColor;
                half   _OutlineSize;
                float4 _VertexOffset;        // 物体空间位置偏移(xyz)
                float4 _VertexRotation;      // 欧拉角旋转(xyz, 度)
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  color       : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                half3  normalWS    : TEXCOORD3;
                half   fogFactor   : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // 物体空间先按欧拉角旋转再加偏移，法线用同一矩阵旋转
                float3x3 rotMat = BuildEulerRotationMatrix(_VertexRotation.xyz);
                float3 positionOS = ApplyVertexTransform(IN.positionOS.xyz, rotMat, _VertexOffset.xyz);
                float3 normalOS   = mul(rotMat, IN.normalOS);

                VertexPositionInputs posInputs = GetVertexPositionInputs(positionOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color       = IN.color;
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // 贴图 * 染色 * SpriteRenderer 顶点色(承载 color/透明度)
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                #if defined(_OUTLINE_ON)
                    // 描边在采样阶段沿轮廓外扩(须在 clip 前，否则描边像素被裁掉)
                    baseSample = ApplyAlphaOutline(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), baseSample,
                                                   IN.uv, _BaseMap_TexelSize.xy, _OutlineSize, _OutlineColor);
                #endif
                half4 col = baseSample * _BaseColor * IN.color;
                clip(col.a - _Cutoff);

                #if defined(_LIT_ON)
                    // 精灵法线固定(quad 朝相机)，双面渲染时背面翻转保证正确受光
                    half3 normalWS = normalize(IN.normalWS) * IS_FRONT_VFACE(cullFace, 1.0, -1.0);
                    return ApplyCommonLit(col.rgb, col.a, IN.positionWS, normalWS, IN.positionHCS, IN.fogFactor);
                #else
                    col.rgb = MixFog(col.rgb, IN.fogFactor);
                    return col;
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
