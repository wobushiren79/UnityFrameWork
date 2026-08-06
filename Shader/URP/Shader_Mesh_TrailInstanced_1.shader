Shader "FrameWork/URP/MeshTrailInstanced1"
{
    // 弹道拖尾(方案1 Instanced)专用 shader = Shader_Mesh_Common_1 的**完整参数集** + 一个逐实例的年龄档透明度 _TrailAlpha。
    //
    // 【为什么要独立一个 shader，而不是给 MeshCommon1 加 _TrailAlpha】
    // 逐实例属性(UNITY_INSTANCING_BUFFER)不在 UnityPerMaterial CBUFFER 内，会危及 shader 的 SRP Batcher 兼容性——
    // 而 MeshCommon1 被 27 个场景静态网格材质(Building 石/墙/柱/宝座/地毯/BG)以普通 MeshRenderer 使用、正吃 SRP Batcher 合批。
    // 本 shader 只由 AttackModeInstanceRenderer 走 DrawMeshInstanced 使用(与 SRP Batcher 本就互斥)，故逐实例属性在这里零代价。
    //
    // 【_TrailAlpha 的作用】年龄档透明度做成逐实例，使「所有年龄档 × 所有弹道」能填进同一个矩阵缓冲、
    // 一次 DrawMeshInstanced 画完(旧实现档 alpha 是 MPB 上的整批 uniform，故每档必须单独一次 draw)。
    //
    // 【参数与 MeshCommon1 保持一致(勿删减)】轨迹材质 = 克隆弹体桶材质后把 shader 换成本 shader(见 SetupTrailMaterial)。
    // Unity 换 shader 时按**属性名**保留同名属性值，故属性集与 MeshCommon1 对齐 = 弹体材质的全部设定(贴图/图集 UV/
    // 宽高比/缩放/旋转/描边/光照/表面)零丢失地继承下来，本 shader 是 MeshCommon1 的 drop-in 替身。
    // 改 MeshCommon1 的参数时**必须同步本文件**(ShaderLab 的 Properties 块无法 #include，只能各留一份；
    // 但顶点变换/描边/受光的算法本体是共享的 Common/*.hlsl，不会分叉)。
    //
    // 【⚠️哪些开关由 C# 强制关掉，及原因】属性都在、值都保留，但 SetupTrailMaterial 会显式关这几个——改那里前先读这段：
    //   _ROTATE_TIME_ON : 必须关。轨迹是过去某刻的静态快照，每个采样点当时的自转角已由 C# 烤进实例矩阵；
    //                     若这里再按 _Time 转一遍 = 转两遍(骨头 200001 踩过：两套自旋互相抵消看似不转)。
    //   _ALPHATEST_ON   : 必须关。轨迹靠 alpha 渐隐(最老档 ~0.05)，硬裁剪阈值(默认 0.5)会把整条尾巴裁没。
    //                     (即便开了也已做防护：clip 在乘 _TrailAlpha **之前**按弹体原 alpha 判定，不会因渐隐而误裁)
    //   _LIT_ON         : 关(轨迹走无光，与旧实现表现一致)。属性保留，想要受光轨迹改 SetupTrailMaterial 一行即可；
    //                     但注意轨迹的 MPB 只灌 _TrailAlpha、不灌 _InstancedFlatGI，开 Lit 会缺一份环境光而偏暗。
    //   表面/混合/深度   : 强制透明 + SrcAlpha/OneMinusSrcAlpha + ZWrite Off + Transparent 队列。
    // _OUTLINE_ON 不强制关——描边随弹体材质继承(弹体有描边则轨迹也有，符合"轨迹是弹体贴图的快照")。
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
        // ⚠️轨迹下由 SetupTrailMaterial 强制关闭(自转角已烤进实例矩阵，再开即转两遍)；保留参数是为与 MeshCommon1 对齐、克隆时不丢值
        [Toggle(_ROTATE_TIME_ON)] _AutoRotateEnable ("开启随时间旋转 (绕物体原点自动旋转 / 默认关闭)", Float) = 0
        [Enum(Forward, 0, Reverse, 1)] _RotateDirection ("旋转方向 (0=正向 / 1=反向)", Float) = 0
        _RotateSpeed ("旋转速度 (每轴 度每秒 XYZ)", Vector) = (0, 0, 0, 0)

        [Header(Trail)]
        // 逐实例年龄档透明度：由 C# 经 MaterialPropertyBlock.SetFloatArray 逐实例灌入(见 DrawTrailBatch)。
        // 默认1 使材质预览/非实例化渲染时不至于全透明看不见。
        [HideInInspector] _TrailAlpha ("轨迹透明度 (逐实例年龄档 / 由代码灌入)", Float) = 1

        // 表面类型/渲染模式/Alpha 裁剪/渲染面 由通用面板 SurfaceOptionsGUI 合并为"渲染设置"折叠组(与 MeshCommon1 一致)
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

        // 外部灌入的平坦环境光补偿(仅 Lit 生效)：供 GPU Instancing 批量绘制通过 MaterialPropertyBlock 补齐 SampleSH 读不到的环境光；默认0
        // ⚠️轨迹的 MPB 只灌 _TrailAlpha 不灌本属性(轨迹默认无光用不上)；若把 _LIT_ON 打开需自行补灌，否则会缺一份环境光偏暗
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

        // 所有 Pass 共用：材质参数 + 贴图声明 + 顶点位移旋转助手 + 通用 Alpha 裁剪(与 MeshCommon1 同构)
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

        // 正向 Pass：轨迹唯一的 pass。
        // ⚠️不做 ShadowCaster/DepthOnly(与 MeshCommon1 的差别)：轨迹恒以 ShadowCastingMode.Off 绘制、且不写深度，
        // 那两个 pass 永远跑不到，加了只是白编译一堆死变体。参数集仍与 MeshCommon1 完全一致。
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            // 混合因子/深度写入由材质"表面类型/渲染模式"预设驱动(轨迹被 SetupTrailMaterial 强制为透明+不写深度)
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

            // 逐实例年龄档透明度：档0(最靠近弹体)=startAlpha → 档 count-1(最老)=endAlpha，
            // 由 C# 每批一次 SetFloatArray 灌入，使一次 DrawMeshInstanced 内可并存所有档
            UNITY_INSTANCING_BUFFER_START(TrailProps)
                UNITY_DEFINE_INSTANCED_PROP(float, _TrailAlpha)
            UNITY_INSTANCING_BUFFER_END(TrailProps)

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

                // 与 MeshCommon1 一致：物体空间先按欧拉角旋转再加偏移，法线用同一矩阵旋转
                // ⚠️_ROTATE_TIME_ON 在轨迹下被 C# 强制关闭(自转角已烤进实例矩阵)，此分支对轨迹实际不会走到，
                // 保留是为与 MeshCommon1 参数/行为一致(本 shader 是它的 drop-in 替身)
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
                    // 描边在采样阶段沿轮廓外扩(须在 clip 前，否则描边像素被裁掉)；轨迹不强制关描边，随弹体材质继承
                    baseSample = ApplyAlphaOutline(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), baseSample,
                                                   IN.uv, _BaseMap_TexelSize.xy, _OutlineSize, _OutlineColor);
                #endif
                half4 col = baseSample * _BaseColor;
                // ⚠️Alpha 裁剪必须在乘 _TrailAlpha **之前**：按弹体原始 alpha 判定轮廓镂空。
                // 若先乘再裁，越老的档(alpha→0.05)会整档低于阈值被全部裁掉、尾巴凭空消失。
                // (轨迹默认已由 SetupTrailMaterial 关掉裁剪，这里的顺序是开着也不出错的防护)
                ApplyAlphaClip(col.a, _Cutoff);
                // 本实例所属年龄档的透明度：越老的档越透明
                col.a *= UNITY_ACCESS_INSTANCED_PROP(TrailProps, _TrailAlpha);

                #if defined(_LIT_ON)
                    // 双面渲染：背面法线翻转，保证两面都能正确受光；透明表面用 col.a 参与混合
                    half3 normalWS = normalize(IN.normalWS) * IS_FRONT_VFACE(cullFace, 1.0, -1.0);
                    half4 litColor = ApplyCommonLit(col.rgb, col.a, IN.positionWS, normalWS, IN.positionHCS, IN.fogFactor);
                    // 补 DrawMeshInstanced 缺失的环境光(⚠️轨迹的 MPB 不灌此属性，开 Lit 需自行补灌，否则=0 会偏暗)
                    litColor.rgb += col.rgb * _InstancedFlatGI.rgb;
                    return litColor;
                #else
                    col.rgb = MixFog(col.rgb, IN.fogFactor);
                    return col;
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
    CustomEditor "MeshCommonShaderGUI"
}
