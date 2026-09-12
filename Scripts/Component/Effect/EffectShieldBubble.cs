using UnityEngine;

/// <summary>
/// 护罩穹顶视图驱动组件（框架层通用组件，任何游戏可直接作为预制根组件加载使用；不依赖 EffectBase/特效管线）。
/// <para>用 MaterialPropertyBlock 驱动罩体 shader（FrameWork/URP/ShieldBubble1）：
/// _CrackLevel=1-HP%（裂纹分档，可选能力）、_HitFlash 受击闪白、_Dissolve 溶解（存续期最后 1/5 经 SetLifetimePercent 渐进老化消融、PlayBreak 后溶解至消失）。</para>
/// <para>出现动画（取出/生成自动播放，OnEnable 触发）：罩体弹性缩放（0→目标半径，EaseOutBack 回弹）+ 能量聚合（_Dissolve 1→0），
/// 连线生长伸出（末端从起点伸向终点，EaseOutCubic）+ 渐入（_Fade 0→1）；时长由 SpawnAnimTime/LinkGrowTime 常量控制（要调表现改常量）。</para>
/// <para>子功能-能量连线（可选，enabledLink 开关）：驱动子节点 Line 的 LineRenderer（shader FrameWork/URP/ShieldLink1），
/// SetLinkEndpoints 每帧拉两端点，流动由 UpdateView 累加 _FlowTime 驱动，受击随罩体同闪，PlayBreak 时随罩体破碎同步淡出。</para>
/// <para>逐帧推进不走自身 Update，由外部调用方以游戏时间流速调 UpdateView（与战斗暂停/倍速同步）；
/// 池化安全：OnEnable（取出复用）时重置全部视觉状态。</para>
/// </summary>
public class EffectShieldBubble : BaseMonoBehaviour
{
    #region 常量
    /// <summary>shader 属性：碎裂程度 0~1</summary>
    protected static readonly int PropCrackLevel = Shader.PropertyToID("_CrackLevel");
    /// <summary>shader 属性：受击闪白 0~1（罩体与连线共用同名属性）</summary>
    protected static readonly int PropHitFlash = Shader.PropertyToID("_HitFlash");
    /// <summary>shader 属性：破碎溶解 0~1</summary>
    protected static readonly int PropDissolve = Shader.PropertyToID("_Dissolve");
    /// <summary>shader 属性：连线流动时间（驱动能量脉冲位置）</summary>
    protected static readonly int PropFlowTime = Shader.PropertyToID("_FlowTime");
    /// <summary>shader 属性：连线整体透明度乘数 0~1（出现渐入/破碎淡出）</summary>
    protected static readonly int PropLinkFade = Shader.PropertyToID("_Fade");
    /// <summary>受击闪白衰减速度（每秒）</summary>
    protected const float HitFlashDecaySpeed = 4f;
    /// <summary>破碎溶解时长（秒，与持有者回收倒计时对齐）</summary>
    public const float DissolveTime = 0.4f;
    /// <summary>连线破碎淡出时长（秒，快于罩体溶解，先收线再碎罩）</summary>
    public const float LinkFadeOutTime = 0.2f;
    /// <summary>存续期老化溶解上限（最后 1/5 存续期内渐进消融的最大值，不裁没罩体；到期后由 PlayBreak 推满）</summary>
    public const float AgingDissolveMax = 0.5f;
    /// <summary>存续期老化起始点（已流逝生命百分比；之前罩体保持完整不消融，之后线性消融至 AgingDissolveMax）</summary>
    public const float AgingStartPercent = 0.8f;
    /// <summary>出现动画时长（秒，罩体弹性缩放+能量聚合）</summary>
    public const float SpawnAnimTime = 0.3f;
    /// <summary>连线生长/渐入时长（秒，末端伸出+淡入）</summary>
    public const float LinkGrowTime = 0.25f;
    #endregion

    #region 字段
    /// <summary>罩体根（球体，基准直径1的正球；SetRadius 换算缩放）</summary>
    public Transform bubbleRoot;
    /// <summary>罩体渲染器</summary>
    public MeshRenderer bubbleRenderer;
    /// <summary>连线渲染器（子节点 Line；可空=无连线子功能）</summary>
    public LineRenderer linkRenderer;
    /// <summary>是否开启连线子功能（prefab 上配置默认；运行时 SetLinkEnabled 切换）</summary>
    public bool enabledLink = true;
    /// <summary>罩体材质参数块（不写材质资产，池化/多实例安全）</summary>
    protected MaterialPropertyBlock mpb;
    /// <summary>连线材质参数块</summary>
    protected MaterialPropertyBlock mpbForLink;
    /// <summary>当前碎裂程度 0~1</summary>
    protected float crackLevel;
    /// <summary>当前受击闪白 0~1（PlayHitFlash 置1，UpdateView 衰减；罩体与连线同闪）</summary>
    protected float hitFlash;
    /// <summary>当前溶解度 0~1（-1=未启用；出现动画从1聚合到0、存续期经 SetLifetimePercent 老化、PlayBreak 后由 UpdateView 推满）</summary>
    protected float dissolve = -1f;
    /// <summary>是否处于破碎溶解中</summary>
    protected bool isBreakingDissolve;
    /// <summary>破碎起始溶解度（从当前老化值继续推满，无跳变）</summary>
    protected float breakDissolveStart;
    /// <summary>破碎溶解进度 0~1</summary>
    protected float breakDissolveProgress;
    /// <summary>罩体目标缩放（SetRadius 记录，出现动画按它弹性放大）</summary>
    protected Vector3 bubbleTargetScale = Vector3.one;
    /// <summary>出现动画进度 0~1（1=播完；播放期间 SetLifetimePercent 不响应防打架）</summary>
    protected float spawnProgress = 1f;
    /// <summary>连线流动时间（UpdateView 累加，喂 shader _FlowTime）</summary>
    protected float linkFlowTime;
    /// <summary>连线整体透明度乘数（出现 0→1 渐入 / 破碎 1→0 淡出）</summary>
    protected float linkFade = 1f;
    /// <summary>连线生长进度 0~1（末端从起点伸出到终点）</summary>
    protected float linkGrowProgress = 1f;
    /// <summary>连线当前起点（世界坐标，SetLinkEndpoints 记录）</summary>
    protected Vector3 linkStartPos;
    /// <summary>连线当前终点（世界坐标，SetLinkEndpoints 记录；生长动画按进度插值后写入渲染器）</summary>
    protected Vector3 linkEndPos;
    #endregion

    #region 生命周期
    /// <summary>
    /// 激活时（含池化复用取出）重置全部视觉状态并自动播放出现动画
    /// </summary>
    protected virtual void OnEnable()
    {
        ResetVisual();
    }

    /// <summary>重置视觉状态（池化复用防残留）：罩体缩到0/消融态起步，连线同点+全透明，等待出现动画展开</summary>
    protected void ResetVisual()
    {
        crackLevel = 0;
        hitFlash = 0;
        dissolve = 1f;//出现动画从完全消融态聚合到0
        isBreakingDissolve = false;
        breakDissolveStart = 0;
        breakDissolveProgress = 0;
        spawnProgress = 0;
        linkFlowTime = 0;
        linkFade = 0;
        linkGrowProgress = 0;
        //首帧防闪：罩体缩到0（UpdateView 出现动画逐帧放大到 bubbleTargetScale）、连线两端收成同点
        if (bubbleRoot != null)
            bubbleRoot.localScale = Vector3.zero;
        if (linkRenderer != null)
        {
            linkStartPos = linkEndPos = transform.position;
            linkRenderer.SetPosition(0, linkStartPos);
            linkRenderer.SetPosition(1, linkEndPos);
        }
        ApplyMpb();
        ApplyMpbForLink();
        RefreshLinkActive();
    }
    #endregion

    #region 外部驱动
    /// <summary>
    /// 逐帧驱动（由持有方以游戏时间流速调用）：出现动画（罩体弹性缩放+能量聚合、连线生长+渐入）；闪白衰减；破碎中溶解推满+连线淡出
    /// </summary>
    /// <param name="deltaTime">时间增量（战斗场景传 GameFightLogic.GetFightDeltaTime()，暂停冻结/倍速同步）</param>
    public void UpdateView(float deltaTime)
    {
        bool dirty = false;
        //出现动画：罩体 0→目标缩放（回弹）+ 消融 1→0 聚合
        if (spawnProgress < 1)
        {
            spawnProgress = Mathf.Min(1, spawnProgress + deltaTime / SpawnAnimTime);
            float ease = EaseOutBack(spawnProgress);
            if (bubbleRoot != null)
                bubbleRoot.localScale = bubbleTargetScale * ease;
            dissolve = 1f - spawnProgress;
            dirty = true;
        }
        if (hitFlash > 0)
        {
            hitFlash = Mathf.Max(0, hitFlash - deltaTime * HitFlashDecaySpeed);
            dirty = true;
        }
        if (isBreakingDissolve && breakDissolveProgress < 1)
        {
            breakDissolveProgress = Mathf.Min(1, breakDissolveProgress + deltaTime / DissolveTime);
            dissolve = Mathf.Lerp(breakDissolveStart, 1f, breakDissolveProgress);
            dirty = true;
        }
        if (dirty)
            ApplyMpb();
        //连线：流动时间每帧都变常写；生长伸出+渐入/破碎淡出
        if (linkRenderer != null && enabledLink)
        {
            linkFlowTime += deltaTime;
            bool linkDirty = true;
            if (linkGrowProgress < 1)
            {
                linkGrowProgress = Mathf.Min(1, linkGrowProgress + deltaTime / LinkGrowTime);
                ApplyLinkPositions();
            }
            if (isBreakingDissolve)
            {
                if (linkFade > 0)
                    linkFade = Mathf.Max(0, linkFade - deltaTime / LinkFadeOutTime);
            }
            else if (linkFade < 1)
            {
                linkFade = Mathf.Min(1, linkFade + deltaTime / LinkGrowTime);
            }
            if (linkDirty)
                ApplyMpbForLink();
        }
    }

    /// <summary>
    /// 设置护罩半径（世界单位）：罩体按基准球放大（正球）；出现动画中的缩放以此为目标
    /// </summary>
    public void SetRadius(float radius)
    {
        //罩体：基准球直径1 → 直径=半径×2（正球不压扁；圆心抬升由持有方按半径处理）
        bubbleTargetScale = new Vector3(radius * 2f, radius * 2f, radius * 2f);
        //出现动画未播完时不直接写缩放（动画每帧覆盖），播完则立即生效
        if (bubbleRoot != null && spawnProgress >= 1)
            bubbleRoot.localScale = bubbleTargetScale;
    }

    /// <summary>
    /// 按护罩剩余生命百分比刷新碎裂表现（裂纹能力，可选；分档由 shader 内部按 _CrackLevel 阈值完成）
    /// </summary>
    public void SetHealthPercent(float healthPercent)
    {
        crackLevel = Mathf.Clamp01(1f - healthPercent);
        ApplyMpb();
    }

    /// <summary>
    /// 存续期老化表现：前 4/5 存续期罩体保持完整不消融，进入最后 1/5 后按已流逝生命百分比渐进消融（0→ AgingDissolveMax 上限，不裁没；出现动画/破碎中不响应）
    /// </summary>
    public void SetLifetimePercent(float lifetimePercent)
    {
        if (isBreakingDissolve || spawnProgress < 1)
            return;
        //最后 1/5 区间内从 0 线性推进到 1，区间外 Clamp 住（前段恒为 0 不消融）
        float agingProgress = Mathf.InverseLerp(AgingStartPercent, 1f, Mathf.Clamp01(lifetimePercent));
        dissolve = agingProgress * AgingDissolveMax;
        ApplyMpb();
    }

    /// <summary>受击闪白（置1后 UpdateView 自动衰减回0；罩体与连线同闪）</summary>
    public void PlayHitFlash()
    {
        hitFlash = 1f;
        ApplyMpbForLink();
    }

    /// <summary>破碎表现：罩体从当前溶解度继续溶解至1（DissolveTime 秒，由 UpdateView 推进）+ 连线同步淡出（LinkFadeOutTime 秒）；回收由持有者在溶解结束后执行</summary>
    public void PlayBreak()
    {
        if (isBreakingDissolve)
            return;
        isBreakingDissolve = true;
        breakDissolveStart = Mathf.Max(0, dissolve);
        breakDissolveProgress = 0;
    }
    #endregion

    #region 连线子功能
    /// <summary>
    /// 开关连线子功能（写 enabledLink 并按开关显隐 Line 对象）
    /// </summary>
    public void SetLinkEnabled(bool isEnabled)
    {
        enabledLink = isEnabled;
        RefreshLinkActive();
    }

    /// <summary>
    /// 设置连线两端点（世界坐标；调用方每帧调用以跟随两端生物；生长动画期间末端按进度插值伸出；连线关闭/无 Line 节点时空操作）
    /// </summary>
    public void SetLinkEndpoints(Vector3 startPos, Vector3 endPos)
    {
        if (linkRenderer == null || !enabledLink)
            return;
        linkStartPos = startPos;
        linkEndPos = endPos;
        ApplyLinkPositions();
    }

    /// <summary>
    /// 设置连线宽度（世界单位，起末同宽）
    /// </summary>
    public void SetLinkWidth(float width)
    {
        if (linkRenderer == null)
            return;
        linkRenderer.startWidth = width;
        linkRenderer.endWidth = width;
    }

    /// <summary>按 enabledLink 开关刷新 Line 对象显隐</summary>
    protected void RefreshLinkActive()
    {
        if (linkRenderer != null)
            linkRenderer.gameObject.SetActive(enabledLink);
    }

    /// <summary>把记录的端点按生长进度写入渲染器（生长中末端从起点伸出到终点）</summary>
    protected void ApplyLinkPositions()
    {
        if (linkRenderer == null)
            return;
        linkRenderer.SetPosition(0, linkStartPos);
        linkRenderer.SetPosition(1, Vector3.Lerp(linkStartPos, linkEndPos, EaseOutCubic(linkGrowProgress)));
    }
    #endregion

    #region 内部
    /// <summary>把当前罩体视觉状态写入罩体材质参数块</summary>
    protected void ApplyMpb()
    {
        if (bubbleRenderer == null)
            return;
        if (mpb == null)
            mpb = new MaterialPropertyBlock();
        bubbleRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(PropCrackLevel, crackLevel);
        mpb.SetFloat(PropHitFlash, hitFlash);
        mpb.SetFloat(PropDissolve, Mathf.Max(0, dissolve));
        bubbleRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>把当前连线视觉状态写入连线材质参数块</summary>
    protected void ApplyMpbForLink()
    {
        if (linkRenderer == null)
            return;
        if (mpbForLink == null)
            mpbForLink = new MaterialPropertyBlock();
        linkRenderer.GetPropertyBlock(mpbForLink);
        mpbForLink.SetFloat(PropFlowTime, linkFlowTime);
        mpbForLink.SetFloat(PropHitFlash, hitFlash);
        mpbForLink.SetFloat(PropLinkFade, linkFade);
        linkRenderer.SetPropertyBlock(mpbForLink);
    }

    /// <summary>缓出回弹（出现动画缩放用，末尾轻微 overshoot）</summary>
    protected static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    /// <summary>缓出三次方（连线生长用）</summary>
    protected static float EaseOutCubic(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }
    #endregion
}
