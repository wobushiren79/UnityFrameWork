Shader "FrameWork/URP/MeshOrbit"
{
    Properties
    {
        _MainTex ("图标图集(运行时赋图集纹理)", 2D) = "white" {}
        [HideInInspector] _OrbitCount ("当前图标数量(代码重建Mesh时写入)", Float) = 0
        _OrbitRadius ("公转半径", Float) = 0.8
        _OrbitSpeed ("公转速度(度/秒)", Float) = 40
        _IconSize ("图标世界尺寸", Float) = 0.22
        _BaseHeight ("基础高度(相对挂点根节点)", Float) = 0.6
        _FloatAmplitude ("上下浮动幅度", Float) = 0.06
        _FloatFreq ("上下浮动频率", Float) = 2
        _SpawnDuration ("入场缩放时长(秒)", Float) = 0.4
        _Cutoff ("Alpha裁剪阈值(像素剪影硬边 裁剪换取ZWrite无排序烦恼)", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "TransparentCutout"
            "Queue"           = "AlphaTest"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            // ZWrite开+Alpha裁剪: 环绕到挂点身后被身体正确遮挡, 图标间相互遮挡也自洽, 无透明排序问题
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _OrbitCount;
                float _OrbitRadius;
                float _OrbitSpeed;
                float _IconSize;
                float _BaseHeight;
                float _FloatAmplitude;
                float _FloatFreq;
                float _SpawnDuration;
                float _Cutoff;
            CBUFFER_END

            // 通用环绕Mesh着色器: 一个Mesh装N个图标quad绕挂点公转+上下浮动+入场缩放, 全部顶点动画在GPU按 _Time.y 匀速计算, 每帧CPU零开销
            // 顶点数据约定(由C#侧构建): POSITION=单位quad角点(±0.5,±0.5,0); UV=图集UV; TEXCOORD1=x图标序号(均分圆周)/y浮动相位; TEXCOORD2=入场时间(_Time.y同轴)

            struct Attributes
            {
                float3 positionOS : POSITION;   // 单位quad角点(±0.5,±0.5,0), shader内乘图标尺寸
                float2 uv         : TEXCOORD0;  // 图集UV(textureRect换算)
                float2 orbitData  : TEXCOORD1;  // x=图标序号(均分圆周) y=浮动相位(起伏错开)
                float  spawnTime  : TEXCOORD2;  // 入场时间(_Time.y同轴 驱动入场缩放)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  fogFactor  : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                //公转角度: 序号均分圆周 + 内置时间*角速度(_Time.y匀速 不随游戏倍速)
                float stepAngle = 360.0 / max(_OrbitCount, 1.0);
                float angle = radians(IN.orbitData.x * stepAngle + _Time.y * _OrbitSpeed);
                float s, c;
                sincos(angle, s, c);
                //环绕中心(物体局部空间): XZ平面公转 + Y方向正弦浮动; mesh挂目标物体下, 跟随移动/销毁全免费
                float3 centerOS = float3(c * _OrbitRadius,
                                         _BaseHeight + sin(_Time.y * _FloatFreq + IN.orbitData.y) * _FloatAmplitude,
                                         s * _OrbitRadius);
                //入场缩放(smoothstep缓动, 从中心弹出到轨道)
                float spawnT = saturate((_Time.y - IN.spawnTime) / max(_SpawnDuration, 0.01));
                float scale = _IconSize * spawnT * spawnT * (3.0 - 2.0 * spawnT);
                //球形广告牌展开(视图矩阵逆的右/上向量), 透视/正交通吃, 图标始终正面可读
                float3 centerWS = TransformObjectToWorld(centerOS);
                float3 camRightWS = UNITY_MATRIX_I_V._m00_m10_m20;
                float3 camUpWS    = UNITY_MATRIX_I_V._m01_m11_m21;
                float3 positionWS = centerWS + (camRightWS * IN.positionOS.x + camUpWS * IN.positionOS.y) * scale;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                clip(col.a - _Cutoff);
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
