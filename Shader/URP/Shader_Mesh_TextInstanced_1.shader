Shader "FrameWork/URP/MeshTextInstanced1"
{
    // 飘字(伤害数字)专用 shader：字符图集 + GPU Instancing 批量渲染，上浮/淡出/弹跳全部在 vertex shader 时间驱动。
    //
    // 【为什么独立 shader】逐实例属性(UNITY_INSTANCING_BUFFER)不在 UnityPerMaterial CBUFFER 内、危及 SRP Batcher 兼容性；
    // 本 shader 只由 FightTextInstanceRenderer 走 DrawMeshInstanced 使用(与 SRP Batcher 本就互斥)，故逐实例属性在这里零代价。
    // (与 TrailInstanced1 / FireBallInstanced1 的独立理由相同)
    //
    // 【实例粒度 = 字符】一条飘字("12345")拆成 5 个字符实例，每个实例一个 quad：
    //   C# 诞生时一次算好「字符世界锚点矩阵(含排版偏移+字符缩放) + 图集 UV + 颜色 + 出生时刻」填槽，
    //   此后 CPU 只按寿剔除，动画全部由本 shader 用 _Time.y - _TextTime 推进——无 TMP、无 DOTween、热路径零 GC。
    //
    // 【网格约定】单位 Quad(顶点 ±0.5、UV 满幅 0..1，Unity 内置 Quad 即满足)；billboard 展开用 (uv-0.5) 作角点，
    // 对网格顶点位置不敏感(任何满幅 UV 的 quad 都正确)。实例矩阵：平移=字符锚点，X/Y 列模长=字符宽/高
    // (X 含图集格子宽高比修正，可能非均匀——非正方形格子时 C# 按格子像素比横向补偿，使字形不被拉伸)，无旋转。
    //
    // 【图集约定(_BaseMap)】等分 _AtlasCols×_AtlasRows 格(材质面板可调, 默认 4×4)，第 0 格在图集【左上】、从左到右从上到下，
    // 格序 = FightTextInstanceRenderer.atlasChars 的字符序(默认 "0123456789" 纯数字)。字形格内居中、建议占格 ~80%。
    // ⚠️格子总数(列×行)须 ≥ atlasChars 长度，否则表尾字符会采到错误格子(CPU 只灌格序索引, 不预检)。
    // UV 解算全在本 shader：C# 逐实例只灌「格序索引 _TextIndex」，行列数改材质即生效，无需重新 Setup。
    //
    // 【动画曲线(对齐旧 TMP+DOTween 方案)】
    //   上浮: y += _RiseHeight × (1-(1-t)^_RiseEase)                  —— OutQuad，旧 DOMoveY(+0.5, 1s, OutQuad)
    //   淡出: a *= 1 - ((t-_FadeStart)/(_FadeEnd-_FadeStart))^_FadeEase —— 旧 DOFade(0, 0.8s, InQuad)
    //   弹跳: s *= 1 + _PopScale × sin(saturate(age/_PopDuration)×π)   —— 旧 DOScale(1.2, 0.15s, 2×Yoyo)
    //
    // 【⚠️寿终保险丝】age ≥ _Lifetime 的实例塌缩成零尺寸——C# 槽剔除是主路径，这里是兜底，
    // 防止 C# 停更(如战斗结束未清场)时在屏数字永久残留。
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("字符图集 (等分格, 第0格在左上, 格序见 FightTextInstanceRenderer.atlasChars)", 2D) = "white" {}
        [MainColor]   _BaseColor ("整体染色 (与逐实例颜色相乘)", Color) = (1, 1, 1, 1)

        [Header(Atlas)]
        _AtlasCols ("图集列数 (等分格, 与字符表格序一致)", Float) = 4
        _AtlasRows ("图集行数 (列×行 须 ≥ 字符表长度)", Float) = 4

        [Header(Lifetime)]
        _Lifetime ("生命时长 (秒 / C# 剔除与 shader 动画共用此值)", Float) = 1.0
        _RiseHeight ("上浮高度 (世界单位)", Float) = 0.5
        _RiseEase ("上浮缓动指数 (2=OutQuad 先快后慢)", Range(0.5, 5)) = 2.0

        [Header(Fade)]
        _FadeStart ("淡出起点 (生命占比 0~1)", Range(0, 1)) = 0.0
        _FadeEnd ("淡完终点 (生命占比 0~1)", Range(0, 1)) = 0.8
        _FadeEase ("淡出缓动指数 (2=InQuad 越来越快地淡没)", Range(0.5, 5)) = 2.0

        [Header(Pop)]
        _PopScale ("弹跳幅度 (0.2 = 出生瞬间放大到 1.2 倍)", Range(0, 1)) = 0.2
        _PopDuration ("弹跳时长 (秒 / 一次 1→峰值→1 的正弦峰)", Float) = 0.3

        [Header(Instanced)]
        // ——以下均为**逐实例属性**，由 FightTextInstanceRenderer 经 MaterialPropertyBlock 按实例灌入，
        //   材质面板值对 DrawMeshInstanced 路径无效(仅单 quad 挂 MeshRenderer 预览时生效)——
        [HideInInspector] _TextIndex ("字符图集格序索引 (逐实例 / 由代码灌入)", Float) = 0
        [HideInInspector] _TextColor ("字符颜色 (逐实例 / 由代码灌入)", Color) = (1, 1, 1, 1)
        [HideInInspector] _TextTime ("出生时刻 (逐实例 / Time.timeSinceLevelLoad 基准)", Float) = 0

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("深度测试 (LEqual=被场景遮挡 / Always=恒在最前)", Float) = 4
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            half4  _BaseColor;
            float  _AtlasCols;
            float  _AtlasRows;
            float  _Lifetime;
            float  _RiseHeight;
            float  _RiseEase;
            float  _FadeStart;
            float  _FadeEnd;
            float  _FadeEase;
            float  _PopScale;
            float  _PopDuration;
        CBUFFER_END
        ENDHLSL

        // 正向 Pass：飘字唯一的 pass。
        // 不做 ShadowCaster/DepthOnly：飘字恒不投影、不写深度(顶点位置是 shader 算的, 深度 pass 也对不上)，加了只是白编译死变体。
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            // 写死透明混合：伤害数字要可读性(非加法发光)，双面显示(billboard 展开后任何角度都是正面)
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            // 逐实例属性：图集格序索引 + 颜色 + 出生时刻，由 C# 每帧一次 SetVectorArray/SetFloatArray 整批灌入
            UNITY_INSTANCING_BUFFER_START(TextProps)
                UNITY_DEFINE_INSTANCED_PROP(float,  _TextIndex)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TextColor)
                UNITY_DEFINE_INSTANCED_PROP(float,  _TextTime)
            UNITY_INSTANCING_BUFFER_END(TextProps)

            struct Attributes
            {
                float4 positionOS : POSITION;    // 单位 quad 顶点(实际用 uv-0.5 作角点, 见文件头网格约定)
                float2 uv         : TEXCOORD0;   // quad 角点 UV(0..1)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;   // 图集格子 UV(已应用逐实例 uvRect)
                half4  tint        : TEXCOORD1;   // 逐实例颜色 × 淡出系数(vert 算好, 插值无失真——颜色/透明度全实例恒定)
                half   fogFactor   : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float  spawnTime  = UNITY_ACCESS_INSTANCED_PROP(TextProps, _TextTime);
                float  charIndex  = UNITY_ACCESS_INSTANCED_PROP(TextProps, _TextIndex);
                half4  textColor  = UNITY_ACCESS_INSTANCED_PROP(TextProps, _TextColor);

                float age = _Time.y - spawnTime;
                float t = saturate(age / max(_Lifetime, 1e-4));

                // 弹跳缩放：前 _PopDuration 秒一个正弦峰(1→1+_PopScale→1)，对齐旧 DOTween Yoyo
                float popScale = 1.0 + _PopScale * sin(saturate(age / max(_PopDuration, 1e-4)) * 3.14159265);

                // 实例矩阵：平移=字符世界锚点，X/Y 列模长=字符宽/高(格子宽高比修正后可能非均匀，故两轴分别取模长)
                float3 anchorWS = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                float2 quadScale = float2(
                    length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20)),
                    length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21)));

                // 上浮：OutQuad 缓动(世界空间 Y，与相机/弹道朝向无关)
                anchorWS.y += _RiseHeight * (1.0 - pow(1.0 - t, _RiseEase));

                // billboard：取相机右/上轴展开 quad 角点，使字符恒正对相机
                float3 camRightWS = UNITY_MATRIX_I_V._m00_m10_m20;
                float3 camUpWS    = UNITY_MATRIX_I_V._m01_m11_m21;
                float2 corner = (IN.uv - 0.5) * quadScale * popScale;
                // ⚠️寿终保险丝：age ≥ _Lifetime 塌缩成零尺寸(C# 槽剔除是主路径，此处兜底防残留)
                corner *= (age < _Lifetime) ? 1.0 : 0.0;
                float3 posWS = anchorWS + camRightWS * corner.x + camUpWS * corner.y;

                OUT.positionHCS = TransformWorldToHClip(posWS);
                // 图集格子 UV：格序索引 → 行列 → UV(约定第 0 格在图集左上, UV 原点在左下 → 行序翻转)；
                // 行列数取材质参数, 面板改动即时生效(CPU 只灌格序索引, 不参与 UV 解算)
                float2 uvGrid = float2(1.0 / _AtlasCols, 1.0 / _AtlasRows);
                float  col = fmod(charIndex, _AtlasCols);
                float  row = floor(charIndex / _AtlasCols);
                float2 uvOffset = float2(col * uvGrid.x, 1.0 - (row + 1.0) * uvGrid.y);
                OUT.uv = IN.uv * uvGrid + uvOffset;
                // 淡出：占比区间内按 _FadeEase 加速淡没(在 vert 算好, 整条飘字各顶点同值, 插值无失真)
                float fadeT = saturate((t - _FadeStart) / max(_FadeEnd - _FadeStart, 1e-4));
                textColor.a *= 1.0 - pow(fadeT, _FadeEase);
                OUT.tint = textColor;
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor * IN.tint;
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
