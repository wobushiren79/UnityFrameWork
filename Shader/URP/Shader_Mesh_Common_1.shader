Shader "FrameWork/URP/MeshCommon1"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("主贴图", 2D) = "white" {}
        [MainColor]   _BaseColor ("染色颜色", Color) = (1, 1, 1, 1)

        [Header(Lit)]
        [Toggle(_LIT_ON)] _LitEnable ("开启光照 (受主光/附加光/阴影/环境光 / 默认关闭)", Float) = 0

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _OutlineEnable ("开启描边 (沿轮廓 Alpha 外扩 / 默认关闭)", Float) = 0
        [HDR] _OutlineColor ("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineSize ("描边大小 (向外扩展的纹素数)", Range(0, 10)) = 1.0

        [Header(Transform)]
        _VertexScale ("大小 (整体缩放倍数 / 物体空间)", Float) = 1
        // 物体空间 XY 各轴缩放：作用于顶点最内层(自旋/偏移之前)，用于修正非方形贴图的宽高比(默认1不改)；一般由代码写入(如 DSP 换图弹道)，故隐藏
        [HideInInspector] _VertexScaleXY ("大小XY (物体空间 XY 各轴缩放)", Vector) = (1, 1, 1, 1)
        _VertexOffset ("位置偏移 (物体空间 XYZ)", Vector) = (0, 0, 0, 0)
        _VertexRotation ("角度旋转 (欧拉角 XYZ / 度)", Vector) = (0, 0, 0, 0)

        [Header(Auto Rotate)]
        [Toggle(_ROTATE_TIME_ON)] _AutoRotateEnable ("开启随时间旋转 (绕物体原点自动旋转 / 默认关闭)", Float) = 0
        [Enum(Forward, 0, Reverse, 1)] _RotateDirection ("旋转方向 (0=正向 / 1=反向)", Float) = 0
        _RotateSpeed ("旋转速度 (每轴 度每秒 XYZ)", Vector) = (0, 0, 0, 0)

        // 表面类型/渲染模式/Alpha 裁剪/渲染面 由通用面板 SurfaceOptionsGUI 合并为"渲染设置"折叠组，
        // 表面类型可设不透明(Opaque)或透明(Transparent)，混合因子/深度写入由预设驱动到 _SrcBlend/_DstBlend/_ZWrite
        [Header(Surface Options)]
        [Enum(Opaque,0,Transparent,1)] _Surface ("表面类型 (0=不透明 / 1=透明 / 默认不透明)", Float) = 0
        [Enum(AlphaBlend,0,Additive,1,PremultipliedAlpha,2)]
        _BlendMode ("渲染模式 (仅透明表面生效 / 0=标准透明 / 1=加法叠加发光 / 2=预乘透明)", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha 裁剪 (按阈值镂空丢弃像素 / 不透明网格默认开启)", Float) = 1
        _Cutoff ("裁剪阈值 (低于此 Alpha 的像素被丢弃 / 仅开启裁剪时生效)", Range(0, 1)) = 0.5
        [Enum(On,0,Off,2)] _Cull ("渲染面 (On=双面都显示 / Off=仅显示正面)", Float) = 0
        // 表面类型/渲染模式预设驱动的实际渲染状态(材质面板不直接暴露), 由 Blend/ZWrite 语句读取
        [HideInInspector] _SrcBlend ("__src", Float) = 1
        [HideInInspector] _DstBlend ("__dst", Float) = 0
        [HideInInspector] _ZWrite ("__zw", Float) = 1

        // 渲染优先级偏移(相对基础队列 AlphaTest 2450)，由 MeshCommonShaderGUI 以"优先级"滑条绘制并写入 material.renderQueue
        [HideInInspector] _QueueOffset ("Queue Offset", Float) = 0.0

        // 外部灌入的平坦环境光补偿(仅 Lit 生效)：供 GPU Instancing 批量绘制(DrawMeshInstanced)通过 MaterialPropertyBlock 补齐 SampleSH 读不到的环境光；默认0，普通渲染(预制/材质直用)不受影响
        [HideInInspector] _InstancedFlatGI ("Instanced Flat GI", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        // 所有 Pass 共用：材质参数 + 贴图声明 + 顶点位移旋转助手 + 通用 Alpha 裁剪
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "../Common/Transform.hlsl"
        #include "../Common/SurfaceOptions.hlsl"   // 通用渲染设置件：ApplyAlphaClip(按 _ALPHATEST_ON 镂空)

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseMap_TexelSize;   // Unity 按所赋贴图自动填充，供描边取纹素尺寸
            half4  _BaseColor;
            half   _Cutoff;
            half4  _OutlineColor;
            half   _OutlineSize;
            float  _VertexScale;         // 物体空间整体缩放倍数(默认1)
            float4 _VertexScaleXY;       // 物体空间 XY 各轴缩放(最内层, 修正非方形贴图宽高比; 默认(1,1))
            float4 _VertexOffset;        // 物体空间位置偏移(xyz)
            float4 _VertexRotation;      // 欧拉角旋转(xyz, 度)
            float4 _RotateSpeed;         // 随时间旋转的每轴角速度(xyz, 度/秒)
            half   _RotateDirection;     // 旋转方向(0 正向 / 1 反向, 顶点着色器换算为 +1/-1)
            half4  _InstancedFlatGI;     // 外部灌入的平坦环境光补偿(rgb, 仅 Lit 生效; 默认0)
        CBUFFER_END
        ENDHLSL

        // 正向 Pass：Lit 开启时受光，关闭时输出无光颜色(均支持描边+裁剪+雾)
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            // 混合因子/深度写入由材质面板"表面类型/渲染模式"预设驱动(不透明=One Zero+写深度, 透明=按模式混合+不写深度)
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma shader_feature_local _LIT_ON
            #pragma shader_feature_local _OUTLINE_ON
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _ROTATE_TIME_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            // 光照库(SurfaceData/InputData/BlinnPhong) → 通用受光助手；描边助手
            #include "../Common/CommonLit.hlsl"
            #include "../Common/Outline.hlsl"

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

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // 物体空间先按欧拉角旋转再加偏移，法线用同一矩阵旋转
                // 随时间旋转开启时叠加"角速度×方向×时间(_Time.y)"，否则用静态欧拉角
                float3 vertexEuler = _VertexRotation.xyz;
                #if defined(_ROTATE_TIME_ON)
                    vertexEuler = ApplyTimeRotationEuler(vertexEuler, _RotateSpeed.xyz, 1.0 - 2.0 * _RotateDirection, _Time.y);
                #endif
                float3x3 rotMat = BuildEulerRotationMatrix(vertexEuler);
                // 先按 _VertexScale 整体缩放, 再旋转+偏移(法线不随均匀缩放改变方向, 无需缩放)
                float3 positionOS = ApplyVertexTransform(IN.positionOS.xyz * _VertexScale * float3(_VertexScaleXY.x, _VertexScaleXY.y, 1.0), rotMat, _VertexOffset.xyz);
                float3 normalOS   = mul(rotMat, IN.normalOS);

                VertexPositionInputs posInputs = GetVertexPositionInputs(positionOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                #if defined(_OUTLINE_ON)
                    // 描边在采样阶段沿轮廓外扩(须在 clip 前，否则描边像素被裁掉)
                    baseSample = ApplyAlphaOutline(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), baseSample,
                                                   IN.uv, _BaseMap_TexelSize.xy, _OutlineSize, _OutlineColor);
                #endif
                half4 col = baseSample * _BaseColor;
                // Alpha 裁剪(镂空)：仅开启 _ALPHATEST_ON 时丢弃低于 _Cutoff 的像素；不透明表面默认不裁剪
                ApplyAlphaClip(col.a, _Cutoff);

                #if defined(_LIT_ON)
                    // 双面渲染：背面法线翻转，保证两面都能正确受光；透明表面用 col.a 参与混合
                    half3 normalWS = normalize(IN.normalWS) * IS_FRONT_VFACE(cullFace, 1.0, -1.0);
                    half4 litColor = ApplyCommonLit(col.rgb, col.a, IN.positionWS, normalWS, IN.positionHCS, IN.fogFactor);
                    // 补 DrawMeshInstanced 缺失的环境光：SampleSH 对实例化绘制读不到环境探针→偏暗，用外部灌入的平坦 GI 补齐(rgb 加到反照率上)；普通渲染 _InstancedFlatGI=0 无影响
                    litColor.rgb += col.rgb * _InstancedFlatGI.rgb;
                    return litColor;
                #else
                    col.rgb = MixFog(col.rgb, IN.fogFactor);
                    return col;
                #endif
            }
            ENDHLSL
        }

        // 阴影投射 Pass：按贴图 alpha 裁剪出实际轮廓阴影(Lit 开启时投形状阴影)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull [_Cull]
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _ROTATE_TIME_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 复刻 URP ShadowCasterPass 的偏移 + 近裁剪夹紧逻辑
            float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
            {
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            Varyings ShadowPassVertex (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // 与 Forward 一致地先旋转+偏移，保证阴影轮廓跟随变换
                // 随时间旋转开启时叠加"角速度×方向×时间(_Time.y)"，否则用静态欧拉角
                float3 vertexEuler = _VertexRotation.xyz;
                #if defined(_ROTATE_TIME_ON)
                    vertexEuler = ApplyTimeRotationEuler(vertexEuler, _RotateSpeed.xyz, 1.0 - 2.0 * _RotateDirection, _Time.y);
                #endif
                float3x3 rotMat = BuildEulerRotationMatrix(vertexEuler);
                // 先按 _VertexScale 整体缩放, 再旋转+偏移(法线不随均匀缩放改变方向, 无需缩放)
                float3 positionOS = ApplyVertexTransform(IN.positionOS.xyz * _VertexScale * float3(_VertexScaleXY.x, _VertexScaleXY.y, 1.0), rotMat, _VertexOffset.xyz);
                float3 normalOS   = mul(rotMat, IN.normalOS);

                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS   = TransformObjectToWorldNormal(normalOS);
                OUT.positionHCS = GetShadowPositionHClip(positionWS, normalWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 ShadowPassFragment (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                ApplyAlphaClip(alpha, _Cutoff);   // 仅开启裁剪时按轮廓镂空阴影
                return 0;
            }
            ENDHLSL
        }

        // 深度 Pass：供深度预通道/依赖深度的后处理使用，同步 Alpha 裁剪
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _ROTATE_TIME_ON

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthOnlyVertex (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // 与 Forward 一致地先旋转+偏移，保证深度轮廓跟随变换
                // 随时间旋转开启时叠加"角速度×方向×时间(_Time.y)"，否则用静态欧拉角
                float3 vertexEuler = _VertexRotation.xyz;
                #if defined(_ROTATE_TIME_ON)
                    vertexEuler = ApplyTimeRotationEuler(vertexEuler, _RotateSpeed.xyz, 1.0 - 2.0 * _RotateDirection, _Time.y);
                #endif
                float3x3 rotMat = BuildEulerRotationMatrix(vertexEuler);
                // 先按 _VertexScale 整体缩放, 再旋转+偏移(法线不随均匀缩放改变方向, 无需缩放)
                float3 positionOS = ApplyVertexTransform(IN.positionOS.xyz * _VertexScale * float3(_VertexScaleXY.x, _VertexScaleXY.y, 1.0), rotMat, _VertexOffset.xyz);

                OUT.positionHCS = TransformObjectToHClip(positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 DepthOnlyFragment (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                ApplyAlphaClip(alpha, _Cutoff);   // 仅开启裁剪时按轮廓镂空深度
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
    CustomEditor "MeshCommonShaderGUI"
}
