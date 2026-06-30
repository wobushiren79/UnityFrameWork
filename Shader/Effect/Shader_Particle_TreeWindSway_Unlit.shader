Shader "FrameWork/Particle/TreeWindSwayUnlit"
{
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("树木贴图", 2D) = "white" {}
        [MainColor]   _BaseColor ("染色颜色 (再乘以粒子颜色)", Color) = (1, 1, 1, 1)
        _Cutoff ("透明裁剪阈值 (0=不裁剪)", Range(0, 1)) = 0.0

        [Header(Particle Blend)]
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("源混合因子 (默认 SrcAlpha)", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("目标混合因子 (普通=OneMinusSrcAlpha / 叠加=One)", Float) = 10
        [ToggleUI] _ZWrite ("写入深度 (粒子通常关闭)", Float) = 0

        [Header(Render Face)]
        [Enum(On,0,Off,2)] _Cull ("是否开启双面渲染 (On=两面都显示 / Off=仅显示正面)", Float) = 0

        [Header(Soft Particles)]
        [Toggle(_SOFTPARTICLES_ON)] _SoftParticlesEnabled ("柔和粒子 (与场景相交处淡出 / 需开启相机深度纹理)", Float) = 0
        _SoftParticleNearFade ("柔和近端淡出 (开始淡出的相交距离)", Range(0, 10)) = 0.0
        _SoftParticleFarFade  ("柔和远端淡出 (完全显示的相交距离)", Range(0, 10)) = 1.0

        [Header(Camera Fade)]
        [Toggle(_CAMERAFADE_ON)] _CameraFadeEnabled ("相机距离淡出 (靠近相机时淡出)", Float) = 0
        _CameraNearFade ("相机近端淡出 (开始显示的相机距离)", Range(0, 20)) = 1.0
        _CameraFarFade  ("相机远端淡出 (完全显示的相机距离)", Range(0, 20)) = 2.0

        [Header(Position Offset)]
        _PositionOffset ("位置偏移 (XYZ 顶点整体偏移)", Vector) = (0, 0, 0, 0)

        [Header(Wind Sway)]
        _WindSpeed     ("风速 (整体快慢)", Range(0, 10)) = 2.0
        _SwayStrength  ("摆动幅度 (树冠左右摇摆大小)", Range(0, 1)) = 0.08
        _SwayFrequency ("摆动频率 (摇摆快慢)", Range(0, 10)) = 1.5
        _BendStrength  ("弯折幅度 (摆动时树冠下压)", Range(0, 1)) = 0.02

        [Header(Flutter)]
        _FlutterStrength ("抖动幅度 (枝叶细碎颤动大小)", Range(0, 0.5)) = 0.02
        _FlutterSpeed    ("抖动速度 (颤动快慢)", Range(0, 30)) = 12.0

        [Header(Anchor)]
        // 1 = 树干底部固定，越靠树冠摆动越大; 0 = 顶部固定(倒挂)
        _AnchorBottom ("底部锚点 (1=树干在下 / 0=树干在上)", Range(0, 1)) = 1.0
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
            Name "ParticleForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_BlendSrc] [_BlendDst]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _SOFTPARTICLES_ON
            #pragma shader_feature_local _CAMERAFADE_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // 公共贴图/淡出 + 树风摆字段与 ApplyWind(TreeWind 内部已 include 公共文件)
            #include "WindSway/TreeWind.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  color       : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
                half   fogFactor   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posOS = IN.positionOS.xyz;
                ApplyWind(posOS, IN.uv);

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.screenPos   = posInputs.positionNDC;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color       = IN.color;
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // 贴图 * 染色颜色 * 粒子顶点色(承载生命周期颜色/透明度)
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor * IN.color;
                clip(col.a - _Cutoff);

                col.a *= ParticleFade(IN.screenPos, _SoftParticleNearFade, _SoftParticleFarFade,
                                      _CameraNearFade, _CameraFarFade);
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
