// 花海 GPU Instancing(Indirect) shader：竖直立牌(yaw广告牌)/贴地平铺两形态 + 噪声抖动像素消散 + 风摆 + 可选阴影投射
// 数据全部来自组件(FlowerSeaInstanceRenderer)绑定的 StructuredBuffer，顶点内按 SV_InstanceID 索引
// keyword 用 multi_compile（非 shader_feature）：组件材质为运行时 new 的实例、无资产携带 keyword，
// multi_compile 保证全部变体始终进入构建，杜绝运行时 EnableKeyword 命中裁剪变体的风险
// 阴影：组件 castShadows 开启后走 ShadowCaster Pass（Alpha 镂空/消散同步裁剪）；立牌在阴影 Pass 中 UNITY_MATRIX_I_V 为光源视角，自动面向光源投影；
// shadowRadius>0 时超半径花朵顶点退化为零面积三角形（光栅零开销剔除），只投相机半径内的阴影
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

        // 两 Pass 共享的声明与实例坐标/消散计算放 HLSLINCLUDE（阴影 Pass 复用同一套 Billboard/风摆/图集逻辑，杜绝两边写法漂移）
        HLSLINCLUDE
        // StructuredBuffer 需要 SM4.5+
        #pragma target 4.5

        // 形态/风摆 keyword：运行时由组件材质开关（阴影 Pass 也要保持同形态同摆动，投影才与画面一致）
        // 用 multi_compile 而非 shader_feature/_local：①项目教训——Instanced 绘制下 _local 变体选择失效；
        // ②组件材质运行时创建、无资产携带 keyword，multi_compile 全变体内置进构建，无裁剪风险
        #pragma multi_compile _ _FLATMODE
        #pragma multi_compile _ _WIND_ON

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // Shadows.hlsl 提供阴影 Pass 用的 ApplyShadowBias（正渲染 Pass 包含亦无副作用）
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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
            // 阴影半径裁剪参数（组件逐帧推送）：>0 时以 _ShadowCenter(渲染相机位置) 为圆心按 XZ 距离裁剪投影花朵
            float3 _ShadowCenter;
            float _ShadowRadius;
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

        // 绕世界 Y 轴旋转向量（右手系，用于叠加每实例随机 yaw）
        float3 RotateAroundY(float3 v, float sinYaw, float cosYaw)
        {
            return v * cosYaw + cross(float3(0, 1, 0), v) * sinYaw;
        }

        // 实例世界坐标 + 图集采样 UV：竖直 yaw 广告牌（可叠风摆）/ 贴地平铺两形态；
        // 阴影 Pass 复用本函数——阴影渲染时 UNITY_MATRIX_I_V 是光源视角矩阵，立牌自动面向光源，投影比屏幕朝向更厚
        void ComputeInstancePositionWS(uint instanceID, float3 positionOS, float2 uv, out float3 posWS, out float2 atlasUV)
        {
            FlowerInstanceData inst = _InstanceData[instanceID];
            float4 posScale = inst.posScale;
            float4 params0  = inst.params0;

            float3 anchorWS = posScale.xyz;
            float  size     = posScale.w;
            float  sinYaw, cosYaw;
            sincos(params0.z, sinYaw, cosYaw);

        #if _FLATMODE
            // 贴地平铺：quad 的 y∈[0,1] 重映射到 z∈[-0.5,0.5]（顺带把 pivot 语义从底部中心换成几何中心），绕 Y 转 yaw
            float2 localXZ = float2(positionOS.x, positionOS.y - 0.5) * size;
            float2 rotatedXZ = float2(localXZ.x * cosYaw - localXZ.y * sinYaw,
                                      localXZ.x * sinYaw + localXZ.y * cosYaw);
            posWS = anchorWS + float3(rotatedXZ.x, 0, rotatedXZ.y);
        #else
            // 竖直 yaw 广告牌：相机前向投影到 XZ 得朝向，只绕世界 Y 面向镜头（相机近平视 XZ 时兜底 +Z）
            float3 camFwdXZ = float3(UNITY_MATRIX_I_V._m02, 0, UNITY_MATRIX_I_V._m22);
            camFwdXZ = dot(camFwdXZ, camFwdXZ) < 1e-6 ? float3(0, 0, 1) : normalize(camFwdXZ);
            float3 rightWS = normalize(cross(float3(0, 1, 0), camFwdXZ));
            rightWS = RotateAroundY(rightWS, sinYaw, cosYaw);

            posWS = anchorWS + rightWS * (positionOS.x * size) + float3(0, positionOS.y * size, 0);

            #if _WIND_ON
                // 风摆（对齐 GrassWindSway）：根部固定、越往花头摆越大，相位用每实例随机值错开
                float heightWeight = pow(saturate(uv.y), _Stiffness);
                float sway = sin(_FlowerSeaTime * _WindSpeed * _SwayFrequency + params0.y) * _SwayStrength;
                posWS += rightWS * sway * heightWeight;
                posWS.y -= abs(sway) * 0.3 * heightWeight;
            #endif
        #endif

            // 图集 UV：变体 Rect 内重映射
            float4 rect = _VariantRects[(uint)params0.x];
            atlasUV = rect.xy + uv * rect.zw;
        }

        // 消散进度：-1 哨兵=未消散恒 0；max 防时长为 0 除零（两 Pass 共用，阴影随消散同步消失）
        half ComputeDissolveProgress(uint instanceID)
        {
            float dissolveStart = _DissolveStart[instanceID];
            return dissolveStart < 0 ? 0 : saturate((_FlowerSeaTime - dissolveStart) / max(_DissolveDuration, 0.001));
        }
        ENDHLSL

        // 主 Pass：Unlit + 雾，AlphaTest 队列绕开战斗场景 Z 轴透明排序问题
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex FlowerSeaVert
            #pragma fragment FlowerSeaFrag

            // 消散边缘 keyword 只影响颜色，阴影 Pass 不需要（放 Pass 内声明，避免阴影变体无谓翻倍）
            #pragma multi_compile _ _DISSOLVEEDGE_ON
            #pragma multi_compile_fog

            struct Varyings
            {
                float4 positionHCS      : SV_POSITION;
                float2 uv               : TEXCOORD0; // 图集换算后的采样 UV
                float2 quadUV           : TEXCOORD1; // quad 原始 UV（噪声/风摆权重用）
                half   fogFactor        : TEXCOORD2;
                half   dissolveProgress : TEXCOORD3;
            };

            Varyings FlowerSeaVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                float3 posWS;
                ComputeInstancePositionWS(IN.instanceID, IN.positionOS, IN.uv, posWS, OUT.uv);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.quadUV = IN.uv;
                OUT.dissolveProgress = ComputeDissolveProgress(IN.instanceID);
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

        // 阴影投射 Pass：组件 castShadows 开启时参与 ShadowMap 渲染；复用共享顶点逻辑，Alpha 轮廓与消散同步裁剪
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex FlowerSeaShadowVert
            #pragma fragment FlowerSeaShadowFrag
            // 对齐 GrassWindSway 阴影 Pass 写法：点光源阴影变体关键字
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // URP 阴影 Pass 全局光向（Shadows.hlsl 不声明，与 GrassWindSway 一样手动声明）
            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowVaryings
            {
                float4 positionHCS      : SV_POSITION;
                float2 uv               : TEXCOORD0; // 图集换算后的采样 UV（Alpha 裁剪用）
                float2 quadUV           : TEXCOORD1; // quad 原始 UV（消散噪声用）
                half   dissolveProgress : TEXCOORD2;
            };

            // 复刻 URP ShadowCasterPass 的偏移 + 近裁剪夹紧逻辑（对齐 GrassWindSway）
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

            ShadowVaryings FlowerSeaShadowVert(Attributes IN)
            {
                ShadowVaryings OUT = (ShadowVaryings)0;
                // 阴影半径裁剪：超半径的花四顶点压成同一退化点（零面积三角形被光栅免费剔除，不产出任何阴影像素）
                if (_ShadowRadius > 0)
                {
                    float2 deltaXZ = _InstanceData[IN.instanceID].posScale.xz - _ShadowCenter.xz;
                    if (dot(deltaXZ, deltaXZ) > _ShadowRadius * _ShadowRadius)
                    {
                        OUT.positionHCS = float4(0, 0, 0, 1);
                        return OUT;
                    }
                }
                float3 posWS;
                ComputeInstancePositionWS(IN.instanceID, IN.positionOS, IN.uv, posWS, OUT.uv);
                OUT.quadUV = IN.uv;
                OUT.dissolveProgress = ComputeDissolveProgress(IN.instanceID);
                // quad 无法线：取背光向作假法线吃 normal bias，规避阴影 acne（与 GetShadowPositionHClip 内同源计算保证一致）
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - posWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                OUT.positionHCS = GetShadowPositionHClip(posWS, -lightDirectionWS);
                return OUT;
            }

            half4 FlowerSeaShadowFrag(ShadowVaryings IN) : SV_TARGET
            {
                // 与主 Pass 一致的镂空裁剪；消散用基础版阈值（边缘色带只影响颜色，阴影无需 _DISSOLVEEDGE_ON 变体）
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, samplerPointRepeat, IN.uv).a;
                clip(alpha - _Cutoff);
                half noise = SAMPLE_TEXTURE2D(_DissolveNoise, samplerPointRepeat, IN.quadUV * _DissolveNoiseScale).r;
                clip(noise - IN.dissolveProgress * 1.001);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
