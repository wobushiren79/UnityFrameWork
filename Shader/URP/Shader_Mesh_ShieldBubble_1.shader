Shader "FrameWork/URP/ShieldBubble1"
{
    Properties
    {
        _BaseColor     ("罩体颜色(低透明度防糊白)", Color) = (0.12, 0.35, 0.85, 0.10)
        _FresnelColor  ("边缘发光色(HDR)", Color)    = (0.35, 0.80, 1.30, 1)
        _FresnelPower  ("菲涅尔指数(越大边缘越窄)", Range(0.5, 8)) = 4.5
        _CrackColor    ("裂纹颜色(HDR)", Color)      = (0.90, 0.95, 1.10, 1.5)
        _CrackLevel    ("碎裂程度(代码MPB驱动)", Range(0, 1)) = 0
        _HitFlash      ("受击闪白(代码MPB驱动)", Range(0, 1)) = 0
        _Dissolve      ("破碎溶解(代码MPB驱动)", Range(0, 1)) = 0
        _CrackScale    ("裂纹密度", Float)           = 2.2
        _FlowSpeed     ("能量流动速度", Float)       = 0.6
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

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            // 加法发光罩体; 不写深度但做深度测试: 全球mesh圆心贴地, 地面以下半球被路面遮住→读出穹顶感; 双面: 相机穿入罩内仍可见
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _FresnelColor;
                half4 _CrackColor;
                float _FresnelPower;
                float _CrackLevel;
                float _HitFlash;
                float _Dissolve;
                float _CrackScale;
                float _FlowSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;  // 物体空间位置(程序化噪声坐标, 缩放随物体不随世界)
            };

            // ---- 程序化噪声(零贴图依赖) ----
            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // voronoi 边距(F2-F1, 胞界处趋近0) + 胞hash(供分档选取子集)
            void voronoi(float2 p, out float edgeDist, out float cellHash)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float f1 = 8.0;
                float f2 = 8.0;
                float2 closestCell = 0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 cellPos = float2(hash21(i + neighbor), hash21(i + neighbor + 19.19));
                        float2 diff = neighbor + cellPos - f;
                        float dist = dot(diff, diff);
                        if (dist < f1)
                        {
                            f2 = f1;
                            f1 = dist;
                            closestCell = i + neighbor;
                        }
                        else if (dist < f2)
                        {
                            f2 = dist;
                        }
                    }
                }
                edgeDist = sqrt(f2) - sqrt(f1);
                cellHash = hash21(closestCell);
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                // 压扁球的法线经逆转置矩阵变换, 非均匀缩放下仍正确
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // 球面UV(经度/纬度): 罩体曲面上的程序化图案坐标(极点轻微收缩, 穹顶视角不可见)
                float3 dirOS = normalize(IN.positionOS);
                float2 sphereUV = float2(atan2(dirOS.z, dirOS.x) / 6.2831853 + 0.5, dirOS.y * 0.5 + 0.5);

                // 菲涅尔边缘光
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);

                // 能量底纹: 缓慢流动的明暗
                float flow = valueNoise(sphereUV * float2(6.0, 3.0) + float2(_Time.y * _FlowSpeed, 0));
                float flowGlow = 0.75 + 0.5 * flow;

                // 程序化裂纹: voronoi 边线, 按胞hash分3档子集随 _CrackLevel 依次点亮, 档位越高线越粗
                float edgeDist, cellHash;
                voronoi(sphereUV * float2(_CrackScale * 2.0, _CrackScale), edgeDist, cellHash);
                float s1 = smoothstep(0.33, 0.40, _CrackLevel);   // 细裂纹(1/3胞)
                float s2 = smoothstep(0.66, 0.73, _CrackLevel);   // 密集裂纹(2/3胞)
                float s3 = smoothstep(0.90, 0.95, _CrackLevel);   // 全裂纹濒碎
                float lineWidth = 0.02 + 0.06 * _CrackLevel;
                float crackLine = 1.0 - smoothstep(0.0, lineWidth, edgeDist);
                float crack = crackLine * (step(cellHash, 0.34) * s1 + step(cellHash, 0.67) * s2 + s3);
                crack = saturate(crack);

                // 濒碎呼吸脉冲: 碎裂>0.9 时整体明暗闪烁
                float pulse = lerp(1.0, 0.75 + 0.25 * sin(_Time.y * 20.0), smoothstep(0.9, 0.95, _CrackLevel));

                // 破碎溶解: 噪声 mask 裁切 + 溶解边缘爆发辉光(_Dissolve=0 时完全不裁)
                float dNoise = valueNoise(sphereUV * float2(8.0, 4.0));
                float dRemain = dNoise - _Dissolve * 1.05;
                float dissolveMask = smoothstep(0.0, 0.02, dRemain);
                float dissolveEdge = (1.0 - smoothstep(0.0, 0.08, dRemain)) * step(0.001, _Dissolve);

                // 合成: 罩体底色压得很弱(加法混合下防"一团白")+菲涅尔窄边缘光(底纹流动) → 叠加裂纹 → 溶解边缘光 → 受击闪白(只闪边缘与裂纹带,罩体中部不糊白)
                half3 color = _BaseColor.rgb * flowGlow * 0.5 + fresnel * _FresnelColor.rgb * flowGlow;
                color += crack * _CrackColor.rgb * _CrackColor.a;
                color += dissolveEdge * _CrackColor.rgb * 2.0;
                float flashMask = saturate(fresnel * 1.5 + crack + dissolveEdge);
                color = lerp(color, half3(1.5, 1.5, 1.5), _HitFlash * flashMask);

                float alpha = saturate(_BaseColor.a * flowGlow + fresnel * 0.45 + crack * 0.75 + dissolveEdge * 0.7 + _HitFlash * flashMask * 0.2);
                alpha *= dissolveMask * pulse;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
