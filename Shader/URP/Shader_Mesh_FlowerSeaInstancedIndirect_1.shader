// 花海 GPU Instancing(Indirect) shader：竖直立牌(yaw广告牌)/贴地平铺两形态 + 噪声抖动像素消散 + 风摆
// 数据全部来自组件(FlowerSeaInstanceRenderer)绑定的 StructuredBuffer，顶点内按 SV_InstanceID 索引
// keyword 用 multi_compile（非 shader_feature）：组件材质为运行时 new 的实例、无资产携带 keyword，
// multi_compile 保证全部 8 个变体始终进入构建，杜绝运行时 EnableKeyword 命中已裁剪变体的风险
Shader "FrameWork/URP/FlowerSeaInstancedIndirect1"
{
    Properties
    {
        [MainTexture] _BaseMap ("花图集 (建议 Point 过滤)", 2D) = "white" {}
        [MainColor]   _BaseColor ("整体染色", Color) = (1, 1, 1, 1)
        _Cutoff ("轮廓 Alpha 裁剪阈值", Range(0, 1)) = 0.1

        [Header(Dissolve)]
        _DissolveNoise ("消散噪声图 (建议 Point 过滤；组件未赋值时内置 Bayer 抖动兜底)", 2D) = "gray" {}
        _DissolveNoiseScale ("噪声平铺密度", Float) = 4.0
        _DissolveDuration ("消散时长(秒)", Float) = 0.6
        [HDR] _DissolveEdgeColor ("消散边缘色 (仅开 _DISSOLVEEDGE_ON 生效)", Color) = (2, 1.2, 0.4, 1)
        _DissolveEdgeWidth ("消散边缘宽度", Range(0, 0.3)) = 0.08

        [Header(Wind)]
        _WindSpeed ("风速 (整体快慢)", Range(0, 10)) = 2.5
        _SwayStrength ("摆动幅度", Range(0, 0.5)) = 0.08
        _SwayFrequency ("摆动频率", Range(0, 10)) = 2.0
        _Stiffness ("茎硬度 (越大越硬越直)", Range(1, 4)) = 2.0
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

        // 唯一 Pass：Unlit + 雾，无阴影/深度 Pass（花海不投影不受影，AlphaTest 队列绕开战斗场景 Z 轴透明排序问题）
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            // StructuredBuffer 需要 SM4.5+
            #pragma target 4.5
            #pragma vertex FlowerSeaVert
            #pragma fragment FlowerSeaFrag

            // 形态/风摆/消散边缘 keyword：运行时由组件材质开关
            // 用 multi_compile 而非 shader_feature/_local：①项目教训——Instanced 绘制下 _local 变体选择失效；
            // ②组件材质运行时创建、无资产携带 keyword，multi_compile 全变体内置进构建，无裁剪风险（仅 8 变体，开销可忽略）
            #pragma multi_compile _ _FLATMODE
            #pragma multi_compile _ _WIND_ON
            #pragma multi_compile _ _DISSOLVEEDGE_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            TEXTURE2D(_DissolveNoise);
            // 显式点采样兜底：不依赖贴图导入设置即保证像素颗粒感（命名命中 Unity 内建采样器约定）
            SAMPLER(samplerPointRepeat);

            // 每实例静态数据（Generate 时一次性上传，之后不变；与 C# 侧 InstanceData 布局一致，stride 32B）
            struct FlowerInstanceData
            {
                float4 posScale; // xyz=世界位置(含地形高度) w=世界缩放
                float4 params0;  // x=图集变体下标 y=风摆随机相位 z=yaw弧度 w=保留
            };
            StructuredBuffer<FlowerInstanceData> _InstanceData;
            StructuredBuffer<float4> _VariantRects;     // 图集变体 UV Rect：xy=offset zw=size
            // 每实例动态数据：消散开始时间（-1=未消散；仅踩踏发生帧整份重传）
            StructuredBuffer<float> _DissolveStart;

            CBUFFER_START(UnityPerMaterial)
                // 组件每渲染帧推送的统一时钟（编辑模式=编辑器时钟 Play=Time.time），消散/风摆都用它——
                // 不依赖内置 _Time.y，杜绝编辑模式下 shader 内置时钟与 CPU 盖章时钟不同源导致消散永不推进
                float _FlowerSeaTime;
                half4 _BaseColor;
                half  _Cutoff;
                half  _DissolveNoiseScale;
                half  _DissolveDuration;
                half4 _DissolveEdgeColor;
                half  _DissolveEdgeWidth;
                half  _WindSpeed;
                half  _SwayStrength;
                half  _SwayFrequency;
                half  _Stiffness;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION; // 底部中心 pivot 单位 quad：x∈[-0.5,0.5] y∈[0,1]
                float2 uv         : TEXCOORD0;
                uint  instanceID  : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS      : SV_POSITION;
                float2 uv               : TEXCOORD0; // 图集换算后的采样 UV
                float2 quadUV           : TEXCOORD1; // quad 原始 UV（噪声/风摆权重用）
                half   fogFactor        : TEXCOORD2;
                half   dissolveProgress : TEXCOORD3;
            };

            // 绕世界 Y 轴旋转向量（右手系，用于叠加每实例随机 yaw）
            float3 RotateAroundY(float3 v, float sinYaw, float cosYaw)
            {
                return v * cosYaw + cross(float3(0, 1, 0), v) * sinYaw;
            }

            Varyings FlowerSeaVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                FlowerInstanceData inst = _InstanceData[IN.instanceID];
                float4 posScale = inst.posScale;
                float4 params0  = inst.params0;
                float  dissolveStart = _DissolveStart[IN.instanceID];

                float3 anchorWS = posScale.xyz;
                float  size     = posScale.w;
                float  sinYaw, cosYaw;
                sincos(params0.z, sinYaw, cosYaw);

            #if _FLATMODE
                // 贴地平铺：quad 的 y∈[0,1] 重映射到 z∈[-0.5,0.5]（顺带把 pivot 语义从底部中心换成几何中心），绕 Y 转 yaw
                float2 localXZ = float2(IN.positionOS.x, IN.positionOS.y - 0.5) * size;
                float2 rotatedXZ = float2(localXZ.x * cosYaw - localXZ.y * sinYaw,
                                          localXZ.x * sinYaw + localXZ.y * cosYaw);
                float3 posWS = anchorWS + float3(rotatedXZ.x, 0, rotatedXZ.y);
            #else
                // 竖直 yaw 广告牌：相机前向投影到 XZ 得朝向，只绕世界 Y 面向镜头（相机近平视 XZ 时兜底 +Z）
                float3 camFwdXZ = float3(UNITY_MATRIX_I_V._m02, 0, UNITY_MATRIX_I_V._m22);
                camFwdXZ = dot(camFwdXZ, camFwdXZ) < 1e-6 ? float3(0, 0, 1) : normalize(camFwdXZ);
                float3 rightWS = normalize(cross(float3(0, 1, 0), camFwdXZ));
                rightWS = RotateAroundY(rightWS, sinYaw, cosYaw);

                float3 posWS = anchorWS + rightWS * (IN.positionOS.x * size) + float3(0, IN.positionOS.y * size, 0);

                #if _WIND_ON
                    // 风摆（对齐 GrassWindSway）：根部固定、越往花头摆越大，相位用每实例随机值错开
                    float heightWeight = pow(saturate(IN.uv.y), _Stiffness);
                    float sway = sin(_FlowerSeaTime * _WindSpeed * _SwayFrequency + params0.y) * _SwayStrength;
                    posWS += rightWS * sway * heightWeight;
                    posWS.y -= abs(sway) * 0.3 * heightWeight;
                #endif
            #endif

                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.quadUV = IN.uv;
                // 图集 UV：变体 Rect 内重映射
                float4 rect = _VariantRects[(uint)params0.x];
                OUT.uv = rect.xy + IN.uv * rect.zw;
                // 消散进度：-1 哨兵=未消散恒 0；max 防时长为 0 除零
                OUT.dissolveProgress = dissolveStart < 0 ? 0 : saturate((_FlowerSeaTime - dissolveStart) / max(_DissolveDuration, 0.001));
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 FlowerSeaFrag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, samplerPointRepeat, IN.uv) * _BaseColor;
                clip(col.a - _Cutoff); // 轮廓镂空

                half noise = SAMPLE_TEXTURE2D(_DissolveNoise, samplerPointRepeat, IN.quadUV * _DissolveNoiseScale).r;
            #if _DISSOLVEEDGE_ON
                // 抖动消散 + HDR 边缘色带：阈值乘 (1+宽度) 让出边缘带空间；×1.001 保证 progress=1 时整朵消失
                half threshold = IN.dissolveProgress * (1.0 + _DissolveEdgeWidth) * 1.001;
                half edge = noise - threshold;
                clip(edge);
                // 接近裁剪线的窄带染边缘色；step 门控避免未消散时误染色
                half band = (1 - saturate(edge / max(_DissolveEdgeWidth, 0.001))) * step(0.001, IN.dissolveProgress);
                col.rgb = lerp(col.rgb, _DissolveEdgeColor.rgb, band);
            #else
                // 抖动像素消散：噪声低于进度处被裁；×1.001 保证 progress=1 时整朵消失
                clip(noise - IN.dissolveProgress * 1.001);
            #endif

                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
