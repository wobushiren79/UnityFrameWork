Shader "FrameWork/URP/MeshFireBallInstanced1"
{
    // 火球(中心火焰核心 + 四散火星)专用 shader：核心与火星在**同一个 shader、同一次 draw** 内画完。
    // 两者都在 vertex shader 里跑完整模拟，无 CPU 开销、无 GC、不挂 ParticleSystem。
    //
    // 【一个网格里混着两种 quad，靠顶点色 alpha 区分】
    //   COLOR.a = 0 → 火星(spark)：一堆互不相连的 quad，各自沿预烤方向飞散、循环生灭
    //   COLOR.a = 1 → 核心(core) ：单个 quad 钉在物体原点，程序化噪声烧出翻滚火焰
    // vert/frag 里按类型**运行时分支**(不能用 keyword 静态分支——两种 quad 在同一次 draw 里, 变体只能有一个)。
    // 合到一个 shader 的收益 = 核心与火星共用一次 DrawMeshInstanced；代价 = 两条分支的指令都在同一个变体内。
    //
    // 【本质】把 ParticleSystem 的模拟搬进顶点着色器：
    // 每个火星 quad 的 4 个顶点共享一份预烤随机数据(方向/种子/速度/大小)，
    // vertex shader 按 _Time 算出该 quad 当前的生命进度 t(0→1 循环)，据此把它推出去 + 展成朝向相机的 billboard。
    // 火星数量 = 网格 quad 数，改数量 = 换网格，不改代码。
    //
    // 【配套网格必须由「火球网格生成器」生成】(Custom/工具弹窗/火球网格生成器 → FireBallMeshGeneratorWindow)
    // 顶点属性约定(生成器与本 shader 必须同步改)：
    //   POSITION  : 恒为原点(0,0,0)。真实位置完全由本 shader 算，故网格 bounds 由生成器手工写入(不能靠 RecalculateBounds，否则=0 会被视锥剔除掉)
    //   NORMAL    : 该火星的发散方向(物体空间单位向量)；核心 quad 用不到(填任意值)。不作光照用(本 shader 恒无光)，纯粹借这个 float3 通道存方向
    //   COLOR     : a=quad 类型(0=火星 / 1=核心)；rgb 保留(恒为白, 留给"逐火星随机染色"等后续扩展)
    //   TEXCOORD0 : quad 角点 UV(0..1)，同时兼作贴图采样 UV。billboard 展开时用 (uv-0.5) 作角点偏移
    //   TEXCOORD1 : float4(种子, 速度倍率, 大小倍率, 生命倍率)——每 quad 一份随机值，使火星彼此错开、不整齐划一
    //
    // 【为什么走独立 shader 而不是加到 MeshCommon1/TrailInstanced1】
    // 与 TrailInstanced1 同理：逐实例属性(_SeedOffset)不在 UnityPerMaterial CBUFFER 内、危及 SRP Batcher 兼容性；
    // 本 shader 只由 DrawMeshInstanced 使用(与 SRP Batcher 本就互斥)，故逐实例属性在这里零代价。
    //
    // 【⚠️_SeedOffset 必须逐实例灌，否则同屏所有火球会整齐跳动】
    // _Time.y 是全局的：不灌种子偏移时，每发弹道的 frac(seed + _Time*rate) 完全相同 = 所有火球同一帧同时爆同时灭，一眼假。
    // 灌法与 TrailInstanced1 的 _TrailAlpha 完全一致：MaterialPropertyBlock.SetFloatArray 逐实例给不同随机值。
    // (单发预览/挂 MeshRenderer 直接看效果时不灌也行，默认 0 即可)
    //
    // 【坐标空间的分工(别搞反)】
    //   发散方向 : 物体空间——随实例矩阵一起转，火星喷出的朝向跟着弹道走(符合"火星从弹体喷出")
    //   出生点   : 世界空间——靠逐实例 _VelocityWS 反推(见下"火星世界化")，使火星脱离火球被甩在身后
    //   重力/上浮 : 世界空间——在 TransformObjectToWorld **之后**加。若在物体空间加，弹道一转向"下"就变成了侧向，火焰会横着飘
    //   billboard: 世界空间——取相机右/上轴展开，故核心与火星恒正对相机(无论弹道怎么转)
    //
    // 【⚠️火星世界化 _VelocityWS——不灌则火星"挂"在火球上跟着走】
    // shader 只知道火球**现在**在哪，没有"每颗火星出生时火球在哪"的记忆，故默认整团火星是刚性绑在实例矩阵上的：
    // 火球一移动，所有火星跟着平移(像挂在车上的装饰)，而不是像真火星那样被甩在身后。
    // 解法：喂**逐实例世界速度矢量**(方向×速率, 单位/秒)，shader 用 `出生点 = 当前位置 - 速度 × 已存活秒数` 把每颗火星退回出生地。
    // 匀速直线时精确；弹道拐弯/追踪时是近似(按当前速度外推)，视觉上足够。
    // 灌法同 _SeedOffset：AttackModeInstanceRenderer 按实例 MaterialPropertyBlock.SetVectorArray 灌(速度由它帧差分算)。
    // **默认 0 → 退化为旧的"绑在火球上"行为，不灌不会坏**(挂 MeshRenderer 单发预览即走此降级：逐实例属性无 MPB 数组时回退读材质值)。
    //
    // 【⚠️火星世界化要求网格 bounds 覆盖拖拽距离】火星退回出生点后，整团火星实际占据的世界范围 = 火球身后 `弹速 × 火星寿命`。
    // 而 DrawMeshInstanced 用 `实例矩阵 × mesh.bounds` 逐实例剔除、又没有外部传 bounds 的口子，故 bounds 不够大时
    // 火球自身一出画面，还留在画面内的那条火星尾巴会**整条突然消失**。bounds 由火球网格生成器手工写入，见其"包围盒半径"。
    //
    // 【与 TrailInstanced1 的取舍差异】不复用 ParticleCommon.hlsl(软粒子/相机淡出)：那套要采场景深度图，
    // 而火星/核心是加法混合的自发光体、与场景相交时本就不穿帮，为省一张深度采样不引入。需要软粒子时再 include 它。
    Properties
    {
        [Header(Surface)]
        [MainTexture] _BaseMap ("火星贴图 (建议软边圆点 / 留白则为方块)", 2D) = "white" {}
        [MainColor]   _BaseColor ("染色颜色 (与生命周期渐变色相乘)", Color) = (1, 1, 1, 1)

        // ——中心火球(核心)在前, 四散火星在后: 与材质面板 MeshCommonShaderGUI 的分组顺序保持一致——
        [Header(Fire Core)]
        // 关掉后核心 quad 会被塌缩成零尺寸(不出画面)，退化为纯火星发散
        [Toggle(_CORE_ON)] _CoreEnable ("开启中心火球 (关闭则只剩火星)", Float) = 1
        // 核心可选贴图：默认全白=纯程序化噪声烧形状；给图则与噪声结果相乘(可用来定制火球轮廓/纹理)
        _CoreMap ("火球贴图 (留白=纯程序化噪声生成)", 2D) = "white" {}
        _CoreSize ("火球大小", Float) = 0.9
        [HDR] _CoreColorHot ("核心颜色 (最热/中心)", Color) = (5.0, 3.0, 1.0, 1)
        [HDR] _CoreColorCold ("边缘颜色 (最冷/外沿)", Color) = (1.5, 0.2, 0.02, 1)
        // 噪声"颗粒"密度：越大火焰细节越碎(格子越小)
        _CoreNoiseScale ("火焰噪声密度 (越大细节越碎)", Range(1, 20)) = 5.0
        // 噪声向上滚动的速度 = 火焰往上舔的速度
        _CoreNoiseSpeed ("火焰翻滚速度 (噪声上滚)", Float) = 1.2
        // 噪声对边缘的扰动强度：0=标准圆球 / 越大边缘被撕得越碎、越像火舌
        _CoreNoiseStrength ("边缘撕裂强度 (0=圆球 / 越大越像火舌)", Range(0, 1)) = 0.45
        // 边缘软硬：小=硬边(卡通火球) / 大=软边(弥散辉光)
        _CoreEdgeSoft ("边缘柔和度 (小=硬边卡通 / 大=软边弥散)", Range(0.01, 1)) = 0.35
        // 核心整体呼吸缩放的幅度与速度(0=不呼吸)
        _CorePulseAmount ("呼吸幅度 (0=不缩放)", Range(0, 0.5)) = 0.08
        _CorePulseSpeed ("呼吸速度 (每秒次数)", Float) = 2.0

        [Header(Fire Spark Motion)]
        // 每秒跑完多少个生命周期。越大 = 火星喷得越急促(单个火星存活时间 = 1/本值)
        _SparkRate ("发散频率 (每秒生命周期数 / 越大越急促)", Range(0.1, 20)) = 2.0
        // 火星在一个生命周期内沿自身方向飞出的最大距离(物体空间)
        _SparkDistance ("发散距离 (一生飞出的距离 / 物体空间)", Float) = 1.0
        // 距离随生命的缓动指数：<1=先快后慢(火星被空气拖住, 像真火星) / =1=匀速 / >1=先慢后快
        _SparkEase ("发散缓动 (小于1先快后慢=有拖拽感 / 1=匀速)", Range(0.2, 3)) = 0.5
        // 生命末尾的世界空间竖直位移：正=下坠(火花/岩浆) 负=上浮(火焰/余烬)
        _SparkGravity ("下坠距离 (世界空间 / 负值=上浮飘火)", Float) = -0.3
        // 火星发射起点距弹体中心的距离：0=全从一点喷出 / 大于0=从球壳喷出(火球体积感)
        _SparkOriginRadius ("起始半径 (0=一点喷出 / 大于0=从球壳喷出)", Float) = 0.15

        [Header(Fire Spark Look)]
        _SparkSizeStart ("起始大小", Float) = 0.2
        _SparkSizeEnd ("结束大小 (通常收到0=烧尽)", Float) = 0.0
        // HDR 颜色：配合加法混合出过曝辉光。默认走 白热黄 → 暗红 的火焰色温衰减
        [HDR] _SparkColorStart ("起始颜色 (刚喷出/最热)", Color) = (4.0, 2.2, 0.6, 1)
        [HDR] _SparkColorEnd ("结束颜色 (将熄灭/最冷)", Color) = (1.2, 0.15, 0.02, 1)
        // 生命周期首尾的渐显/渐隐占比：防止火星凭空出现/凭空消失(硬切会很假)
        _SparkFadeIn ("渐显占比 (生命周期开头)", Range(0.001, 1)) = 0.12
        _SparkFadeOut ("渐隐占比 (生命周期结尾)", Range(0.001, 1)) = 0.6

        [Header(Transform)]
        // 整体缩放：同时作用于发散距离/起始半径/火星大小/火球大小，故调它=整团火球等比放大缩小
        _VertexScale ("大小 (整体缩放倍数 / 物体空间)", Float) = 1

        [Header(Instanced)]
        // ——以下两项均为**逐实例属性**, 由 AttackModeInstanceRenderer 经 MaterialPropertyBlock 按实例灌入,
        //   材质面板上填的值对 DrawMeshInstanced 路径无效(会被 MPB 数组覆盖), 故一律 HideInInspector;
        //   仅"挂 MeshRenderer 单发预览"时材质值才生效(逐实例属性无 MPB 数组时回退读材质值)——
        //   预览想看甩尾就临时给 _VelocityWS 填个速度, 想看错峰就给 _SeedOffset 填个 0~1 随机值——
        //   本 shader 的其余属性都是逐材质的, 别把这两个跟它们混为一谈。
        // 逐实例种子偏移：不灌则同屏所有火球的火星同一帧同时爆同时灭(详见文件头⚠️)
        [HideInInspector] _SeedOffset ("种子偏移 (逐实例 / 由代码灌入)", Float) = 0
        // 逐实例世界速度矢量(xyz=方向×速率, 单位/秒; w 未用)：本物体当前的世界飞行速度, 火星靠它反推出生点脱离火球(详见文件头⚠️与 vert 内⚠️)。
        // 通用属性(非火星专用)：任何需要"物体世界速度"的 shader(拉伸粒子/运动模糊/速度着色)都可复用此名与渲染器的这条灌入链路
        [HideInInspector] _VelocityWS ("世界速度矢量 (逐实例 / 由代码灌入)", Vector) = (0, 0, 0, 0)

        [Header(Surface Options)]
        // 默认即"透明 + 加法叠加"：火焰是自发光体，加法混合天然免排序(叠得越多越亮)，这正是火球要的
        [Enum(Opaque,0,Transparent,1)] _Surface ("表面类型 (0=不透明 / 1=透明 / 火球默认透明)", Float) = 1
        [Enum(AlphaBlend,0,Additive,1,PremultipliedAlpha,2)]
        _BlendMode ("渲染模式 (火球默认加法叠加发光)", Float) = 1
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha 裁剪 (火球默认关闭 / 靠渐隐消失)", Float) = 0
        _Cutoff ("裁剪阈值 (仅开启裁剪时生效)", Range(0, 1)) = 0.5
        [Enum(On,0,Off,2)] _Cull ("渲染面 (On=双面都显示 / Off=仅显示正面)", Float) = 0
        // 由 SurfaceOptionsGUI 依表面类型/渲染模式预设写入(材质面板不直接暴露)
        [HideInInspector] _SrcBlend ("__src", Float) = 5   // SrcAlpha
        [HideInInspector] _DstBlend ("__dst", Float) = 1   // One (加法叠加)
        [HideInInspector] _ZWrite ("__zw", Float) = 0      // 透明自发光不写深度

        [HideInInspector] _QueueOffset ("Queue Offset", Float) = 0.0
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
        #include "../Common/SurfaceOptions.hlsl"   // 通用渲染设置件：ApplyAlphaClip(按 _ALPHATEST_ON 镂空)
        #include "../Common/Noise.hlsl"            // 通用程序化噪声：Fbm2D(火焰湍流)

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_CoreMap);
        SAMPLER(sampler_CoreMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseMap_TexelSize;
            float4 _CoreMap_ST;
            half4  _BaseColor;
            half   _Cutoff;
            float  _SparkRate;
            float  _SparkDistance;
            float  _SparkEase;
            float  _SparkGravity;
            float  _SparkOriginRadius;
            float  _SparkSizeStart;
            float  _SparkSizeEnd;
            half4  _SparkColorStart;
            half4  _SparkColorEnd;
            half   _SparkFadeIn;
            half   _SparkFadeOut;
            float  _CoreSize;
            half4  _CoreColorHot;
            half4  _CoreColorCold;
            float  _CoreNoiseScale;
            float  _CoreNoiseSpeed;
            half   _CoreNoiseStrength;
            half   _CoreEdgeSoft;
            half   _CorePulseAmount;
            float  _CorePulseSpeed;
            float  _VertexScale;
        CBUFFER_END
        ENDHLSL

        // 正向 Pass：火球唯一的 pass。
        // 不做 ShadowCaster/DepthOnly：火球恒不投影、不写深度(顶点位置是 shader 算的, 深度 pass 也对不上)，加了只是白编译死变体。
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _CORE_ON
            #pragma multi_compile_fog

            // 逐实例属性：种子偏移(相位错开, 详见文件头⚠️) + 世界速度矢量(火星世界化, 详见 vert 内⚠️)
            UNITY_INSTANCING_BUFFER_START(SparkProps)
                UNITY_DEFINE_INSTANCED_PROP(float, _SeedOffset)
                UNITY_DEFINE_INSTANCED_PROP(float4, _VelocityWS)
            UNITY_INSTANCING_BUFFER_END(SparkProps)

            struct Attributes
            {
                float4 positionOS : POSITION;    // 恒为原点，仅占位(真实位置由本 shader 算)
                float3 dirOS      : NORMAL;      // 火星发散方向(物体空间单位向量)；核心 quad 用不到
                half4  color      : COLOR;       // a=quad 类型(0=火星 / 1=核心)；rgb 保留
                float2 uv         : TEXCOORD0;   // quad 角点 UV(0..1)，兼作贴图 UV
                float4 sparkData  : TEXCOORD1;   // x=种子 y=速度倍率 z=大小倍率 w=生命倍率
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  tint        : TEXCOORD1;  // 火星: 生命周期渐变色(rgb)+生灭渐隐(a)；核心: 恒为白(着色在 frag 算)
                half   isCore      : TEXCOORD2;  // 0=火星 / 1=核心(供 frag 运行时分支)
                half   fogFactor   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                half isCore = IN.color.a;
                float seedOffset = UNITY_ACCESS_INSTANCED_PROP(SparkProps, _SeedOffset);

                float3 posOS;
                float  size;
                half4  tint;
                float  t = 0.0;     // 火星的生命进度(核心恒0, 下面重力/世界化项据此自动归零)
                float  age = 0.0;   // 火星已存活秒数(核心恒0)

                if (isCore > 0.5h)
                {
                    // ——核心：钉在物体原点，只做呼吸缩放。着色全部交给 frag 的程序化噪声——
                    posOS = float3(0.0, 0.0, 0.0);
                    // 呼吸相位也叠 seedOffset，使每发弹道的火球胀缩不同步
                    float pulse = 1.0 + _CorePulseAmount * sin((_Time.y * _CorePulseSpeed + seedOffset) * 6.2831853);
                    size = _CoreSize * pulse * _VertexScale;
                    #if !defined(_CORE_ON)
                        size = 0.0;   // 关闭核心：塌缩成零尺寸, 该 quad 不产生任何像素
                    #endif
                    tint = half4(1.0h, 1.0h, 1.0h, 1.0h);
                }
                else
                {
                    // ——火星：沿自身方向从球壳飞出, 循环生灭——
                    // 生命进度 t：0(刚喷出) → 1(熄灭) 循环。种子使每个火星错开相位，逐实例偏移使每发弹道也错开
                    t = frac(IN.sparkData.x + seedOffset + _Time.y * _SparkRate * IN.sparkData.w);
                    // 已存活秒数 = 生命进度 × 单条生命的时长(时长=1/每秒生命周期数)。供下面的"世界化"回退出生点用
                    age = t / max(_SparkRate * IN.sparkData.w, 1e-4);
                    // pow 缓动使火星先快后慢(_SparkEase<1)，像被空气拖住
                    float travel = _SparkDistance * IN.sparkData.y * pow(t, _SparkEase);
                    posOS = IN.dirOS * (_SparkOriginRadius + travel) * _VertexScale;
                    size = lerp(_SparkSizeStart, _SparkSizeEnd, t) * IN.sparkData.z * _VertexScale;
                    // 生灭渐隐：开头 _SparkFadeIn 段渐显、结尾 _SparkFadeOut 段渐隐(否则火星凭空生灭很假)
                    half fade = saturate(t / max(_SparkFadeIn, 1e-4)) * saturate((1.0 - t) / max(_SparkFadeOut, 1e-4));
                    tint = half4(lerp(_SparkColorStart.rgb, _SparkColorEnd.rgb, t), fade);
                }

                // —— 转世界：实例矩阵在此生效(整团火球跟随弹体位置/朝向) ——
                float3 posWS = TransformObjectToWorld(posOS);

                // ⚠️【火星世界化】把火星退回"它出生那一刻火球所在的位置"，使其脱离火球、留在世界空间。
                // 不做这步的话火星是**刚性绑在火球 transform 上**的：火球一移动整团火星跟着平移(像挂在车上的装饰)，
                // 而不是像真火星那样被甩在身后。根因=shader 只知道火球**现在**在哪, 没有每颗火星出生时的位置记忆。
                // 解法=用速度反推出生点: 出生点 = 当前位置 - 速度 × 已存活秒数(匀速直线时精确; 弹道拐弯/追踪时是近似, 视觉上足够)。
                // _VelocityWS 为逐实例世界速度矢量(方向×速率, 单位/秒), 由 C# 灌入(灌法同 _SeedOffset)。
                // **默认 0 → 该项归零 → 行为与"绑在火球上"完全一致**, 故不灌也不会坏, 是平滑降级。
                // 核心 age 恒为 0 → 不受影响(核心本就该钉在火球中心)。
                float3 velocityWS = UNITY_ACCESS_INSTANCED_PROP(SparkProps, _VelocityWS).xyz;
                posWS -= velocityWS * age;

                // ⚠️重力/上浮必须在世界空间加：物体空间加的话，弹道一转向"下"火焰就横着飘了。
                // 核心 t 恒为0 → 该项自动归零(核心钉在原点不受重力)
                posWS.y -= _SparkGravity * t * t * _VertexScale;

                // —— billboard：取相机右/上轴，把 quad 角点在世界空间展开，使核心与火星恒正对相机 ——
                float3 camRightWS = UNITY_MATRIX_I_V._m00_m10_m20;
                float3 camUpWS    = UNITY_MATRIX_I_V._m01_m11_m21;
                float2 corner = (IN.uv - 0.5) * size;
                posWS += camRightWS * corner.x + camUpWS * corner.y;

                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv = IN.uv;          // 原始 0..1 角点 UV, 贴图 ST 在 frag 按类型各自应用
                OUT.tint = tint;
                OUT.isCore = isCore;
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            // 程序化火焰核心：噪声扰动的径向衰减 → 撕裂的火舌边缘 + 由内到外的色温渐变。
            // 返回 rgb=火焰色(HDR), a=覆盖度。
            half4 ShadeFireCore(float2 uv, float seedOffset)
            {
                // r: 0=中心 1=边缘
                float r = length(uv - 0.5) * 2.0;

                // 噪声随时间向上滚 = 火焰往上舔；seedOffset 让每发弹道的火焰纹路不同
                float2 nuv = uv * _CoreNoiseScale + float2(seedOffset * 13.7, -_Time.y * _CoreNoiseSpeed);
                float n = Fbm2D(nuv, 3);

                // 用噪声扰动"到边缘的距离"：噪声高处半径被推大 → 该处提前消失(撕出缺口)；低处被拉小 → 火舌探出
                float d = r + (n - 0.5) * _CoreNoiseStrength * 2.0;

                // 覆盖度: d 从 (1-柔和度) 到 1 之间衰减到 0
                half alpha = 1.0h - smoothstep(1.0 - _CoreEdgeSoft, 1.0, d);

                // 色温: 中心最热 → 外沿最冷(用扰动后的 d, 使色温也跟着火舌起伏)
                half3 col = lerp(_CoreColorHot.rgb, _CoreColorCold.rgb, saturate(d));

                return half4(col, alpha);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 col;
                if (IN.isCore > 0.5h)
                {
                    float seedOffset = UNITY_ACCESS_INSTANCED_PROP(SparkProps, _SeedOffset);
                    col = ShadeFireCore(IN.uv, seedOffset);
                    // 可选火球贴图: 默认全白 = 纯程序化噪声定形; 给图则与噪声结果相乘
                    col *= SAMPLE_TEXTURE2D(_CoreMap, sampler_CoreMap, TRANSFORM_TEX(IN.uv, _CoreMap));
                    col *= _BaseColor;
                }
                else
                {
                    col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(IN.uv, _BaseMap)) * _BaseColor;
                    col.rgb *= IN.tint.rgb;   // 生命周期色温衰减(白热 → 暗红)
                    col.a   *= IN.tint.a;     // 生灭渐隐
                }

                // 火球默认关裁剪(靠渐隐消失)；开着时按当前 alpha 判定
                ApplyAlphaClip(col.a, _Cutoff);

                // ⚠️加法混合下必须把颜色拉向**黑**而非雾色(故用 MixFogColor 传0，不能用 MixFog)：
                // 加法是"往屏幕上加光"，混进雾色等于远处火球越雾越亮、糊成灰块；拉向0 才是正确的"被雾吃掉"
                col.rgb = MixFogColor(col.rgb, half3(0.0, 0.0, 0.0), IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
    CustomEditor "MeshCommonShaderGUI"
}
