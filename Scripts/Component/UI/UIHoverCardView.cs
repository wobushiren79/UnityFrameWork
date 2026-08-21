using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 通用悬停卡牌组件(小丑牌/Balatro风格)：鼠标进入时弹起放大并上抬，
/// 光标在卡面内移动时卡牌朝光标方向3D倾斜(视差跟随)，倾斜由欠阻尼弹簧驱动(快速划过会甩动、松手回摆)，
/// 悬停静止期间叠加双轴错相持续摆动(悬空摇晃感, 渐入渐出)，移出后弹回原位。
/// 自身实现IPointerEnter/Exit/Move(事件沿射线接收层向上冒泡，可挂在item根节点或独立子节点)；
/// 其他动画需要独占目标变换时用SetHoverSuppressed抑制；maxTiltAngle=0且hoverLiftOffset=zero时退化为纯缩放悬停。
/// </summary>
public class UIHoverCardView : BaseUIView, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    #region 序列化参数
    [Header("动画目标(为空时取自身transform)")]
    public Transform targetTransform;
    [Header("悬停放大倍率(1=不缩放)")]
    public float hoverScale = 1.5f;
    [Header("缩放/上抬动画时长")]
    public float scaleDuration = 0.2f;
    [Header("悬停上抬偏移(anchoredPosition, zero=不上抬)")]
    public Vector2 hoverLiftOffset = new Vector2(0, 10);
    [Header("最大倾斜角(光标到卡面边缘打满, 0=不倾斜)")]
    public float maxTiltAngle = 30f;
    [Header("倾斜弹簧刚度(越大越跟手)")]
    public float tiltFollowSpeed = 30f;
    [Header("倾斜回弹强度(0=无过冲, 越大甩动越明显)")]
    [Range(0f, 0.85f)]
    public float tiltOvershoot = 0.35f;
    [Header("悬停持续摆动幅度(度, 0=静止时无摆动)")]
    public float idleSwayAngle = 10f;
    [Header("悬停持续摆动频率(周期/秒)")]
    public float idleSwayFrequency = 2f;
    [Header("是否响应指针事件(关闭后仅接受外部驱动)")]
    public bool isListenPointer = true;
    #endregion

    #region 内部状态
    //缩放/上抬Tween，复用时先Kill避免叠加
    protected Tween scaleTween;
    protected Tween liftTween;
    //倾斜当前角(x=绕X轴俯仰, y=绕Y轴偏转)/目标角/弹簧速度
    protected Vector2 tiltCurrent;
    protected Vector2 tiltTarget;
    protected Vector2 tiltVelocity;
    //悬停中标记
    protected bool isHovering;
    //是否已抑制悬停(其他动画独占目标变换时置位)
    protected bool isHoverSuppressed;
    //悬停持续摆动的当前振幅权重(0~1渐入渐出)
    protected float swayWeight;
    //目标初始变换(Awake时缓存，支持目标本身带变换)
    protected Vector3 scaleOriginal;
    protected Vector2 positionOriginal;
    protected Vector3 rotationOriginal;
    //目标的RectTransform(上抬与倾斜结算用，target非UI时为空)
    protected RectTransform targetRectTransform;
    #endregion

    #region 生命周期
    /// <summary>
    /// 初始化动画目标并缓存初始变换
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        if (targetTransform == null)
            targetTransform = transform;
        targetRectTransform = targetTransform as RectTransform;
        RefreshOriginalTransform();
    }

    /// <summary>
    /// 首帧重新缓存初始变换(SetData/布局通常在Awake后才设置, Start时才是真实静止态, 避免还原到prefab默认值)
    /// </summary>
    protected virtual void Start()
    {
        RefreshOriginalTransform();
    }

    /// <summary>
    /// 每帧驱动倾斜弹簧(欠阻尼：快速划过甩动、停下回摆)
    /// </summary>
    public virtual void Update()
    {
        UpdateTiltSpring();
    }

    /// <summary>
    /// 失活时清理动画并复位变换，避免鼠标停留未触发Exit导致动画残留
    /// </summary>
    public override void OnDisable()
    {
        base.OnDisable();
        isHovering = false;
        KillHoverAnim();
    }

    /// <summary>
    /// 销毁时清理动画句柄
    /// </summary>
    public override void OnDestroy()
    {
        KillHoverAnim();
        base.OnDestroy();
    }
    #endregion

    #region 指针事件
    /// <summary>
    /// 鼠标进入：弹起放大+上抬，并立即朝进入点倾斜
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isListenPointer || isHoverSuppressed)
            return;
        isHovering = true;
        PlayEnterAnim();
        //进入瞬间即朝进入点倾倒，不用等首次Move
        UpdateTiltTarget(eventData);
    }

    /// <summary>
    /// 鼠标在卡面内移动：刷新倾斜目标(弹簧在Update中逼近，形成视差跟随+惯性甩动)
    /// </summary>
    public void OnPointerMove(PointerEventData eventData)
    {
        if (!isListenPointer || isHoverSuppressed || !isHovering)
            return;
        UpdateTiltTarget(eventData);
    }

    /// <summary>
    /// 鼠标移出：缩放/上抬还原，倾斜目标归零由弹簧带回弹归位
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isListenPointer || isHoverSuppressed)
            return;
        isHovering = false;
        PlayExitAnim();
        //倾斜不Kill，目标归零让弹簧自然回摆收尾
        tiltTarget = Vector2.zero;
    }
    #endregion

    #region 倾斜弹簧
    /// <summary>
    /// 按光标在卡面内的归一化位置结算倾斜目标角(光标在下→向下俯, 在右→右缘倒向远处)
    /// </summary>
    /// <param name="eventData">指针事件数据(取屏幕坐标)</param>
    protected virtual void UpdateTiltTarget(PointerEventData eventData)
    {
        if (maxTiltAngle <= 0f || targetRectTransform == null)
            return;
        Camera cam = GetPointerCamera();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRectTransform, eventData.position, cam, out Vector2 localPoint))
            return;
        //按卡面半宽半高归一化到[-1,1]
        Vector2 halfSize = targetRectTransform.rect.size * 0.5f;
        float offsetX = halfSize.x > 0 ? Mathf.Clamp(localPoint.x / halfSize.x, -1f, 1f) : 0f;
        float offsetY = halfSize.y > 0 ? Mathf.Clamp(localPoint.y / halfSize.y, -1f, 1f) : 0f;
        tiltTarget = new Vector2(-offsetY * maxTiltAngle, offsetX * maxTiltAngle);
    }

    /// <summary>
    /// 欠阻尼弹簧积分驱动倾斜(同PopupShowView位置弹簧写法)：快速划过甩动、停下回摆；悬停中叠加持续摆动
    /// </summary>
    protected virtual void UpdateTiltSpring()
    {
        if (targetTransform == null)
            return;
        //用不受时间缩放影响的delta, 限幅避免低帧时弹簧发散
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.04f);
        if (dt <= 0f)
            return;
        //持续摆动振幅渐入渐出(约0.4秒)
        float swayTargetWeight = (isHovering && idleSwayAngle > 0f) ? 1f : 0f;
        swayWeight = Mathf.MoveTowards(swayWeight, swayTargetWeight, dt * 2.5f);
        //倾斜目标=光标视差目标+持续摆动偏移
        Vector2 effectiveTarget = tiltTarget + ComputeIdleSway();
        Vector2 disp = tiltCurrent - effectiveTarget;
        //静止判定：无摆动时位移与速度都足够小才吸附停转，避免指数衰减每帧空转
        if (swayWeight <= 0f && disp.sqrMagnitude < 0.0001f && tiltVelocity.sqrMagnitude < 0.01f)
        {
            if (tiltCurrent != effectiveTarget || tiltVelocity != Vector2.zero)
            {
                tiltCurrent = effectiveTarget;
                tiltVelocity = Vector2.zero;
                ApplyTiltRotation();
            }
            return;
        }
        //omega=角频率(跟随刚度), zeta=阻尼比(<1过冲回摆)
        float omega = tiltFollowSpeed;
        float zeta = Mathf.Clamp(1f - tiltOvershoot, 0.1f, 1f);
        //半隐式积分: a = -ω²·位移 - 2ζω·速度
        Vector2 accel = -(omega * omega) * disp - (2f * zeta * omega) * tiltVelocity;
        tiltVelocity += accel * dt;
        tiltCurrent += tiltVelocity * dt;
        ApplyTiltRotation();
    }

    /// <summary>
    /// 计算悬停持续摆动偏移(小丑牌悬空摇晃感)：两轴错相90°形成环绕摆动
    /// </summary>
    protected virtual Vector2 ComputeIdleSway()
    {
        if (swayWeight <= 0f)
            return Vector2.zero;
        float phase = Time.unscaledTime * Mathf.PI * 2f * idleSwayFrequency;
        return new Vector2(Mathf.Sin(phase), Mathf.Sin(phase + Mathf.PI * 0.5f)) * (idleSwayAngle * swayWeight);
    }

    /// <summary>
    /// 把当前倾斜角叠加到初始旋转上
    /// </summary>
    protected virtual void ApplyTiltRotation()
    {
        targetTransform.localRotation = Quaternion.Euler(rotationOriginal.x + tiltCurrent.x, rotationOriginal.y + tiltCurrent.y, rotationOriginal.z);
    }

    /// <summary>
    /// 取指针事件对应的相机(Overlay画布为null，否则取父Canvas的worldCamera)
    /// </summary>
    protected virtual Camera GetPointerCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return canvas.worldCamera;
        return null;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 播放进入动画(弹起放大+上抬)；被抑制时忽略
    /// </summary>
    public virtual void PlayEnterAnim()
    {
        if (isHoverSuppressed || targetTransform == null)
            return;
        scaleTween?.Kill();
        liftTween?.Kill();
        scaleTween = targetTransform
            .DOScale(scaleOriginal * hoverScale, scaleDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(UpdateType.Normal, true);
        if (hoverLiftOffset != Vector2.zero && targetRectTransform != null)
        {
            liftTween = targetRectTransform
                .DOAnchorPos(positionOriginal + hoverLiftOffset, scaleDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(UpdateType.Normal, true);
        }
    }

    /// <summary>
    /// 播放移出动画(缩放/上抬还原)；被抑制时忽略
    /// </summary>
    public virtual void PlayExitAnim()
    {
        if (isHoverSuppressed || targetTransform == null)
            return;
        scaleTween?.Kill();
        liftTween?.Kill();
        scaleTween = targetTransform
            .DOScale(scaleOriginal, scaleDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(UpdateType.Normal, true);
        if (hoverLiftOffset != Vector2.zero && targetRectTransform != null)
        {
            liftTween = targetRectTransform
                .DOAnchorPos(positionOriginal, scaleDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(UpdateType.Normal, true);
        }
    }

    /// <summary>
    /// 清理悬停动画并复位变换；供UI关闭/外部动画接管时主动调用
    /// </summary>
    public virtual void KillHoverAnim()
    {
        scaleTween?.Kill();
        scaleTween = null;
        liftTween?.Kill();
        liftTween = null;
        tiltCurrent = Vector2.zero;
        tiltTarget = Vector2.zero;
        tiltVelocity = Vector2.zero;
        swayWeight = 0f;
        if (targetTransform != null)
        {
            targetTransform.localScale = scaleOriginal;
            targetTransform.localRotation = Quaternion.Euler(rotationOriginal);
            if (targetRectTransform != null)
                targetRectTransform.anchoredPosition = positionOriginal;
        }
    }

    /// <summary>
    /// 设置悬停抑制：其他动画(如解锁/选中)独占目标变换时置true并停掉悬停动画，结束后恢复false
    /// </summary>
    /// <param name="isSuppressed">是否抑制</param>
    public virtual void SetHoverSuppressed(bool isSuppressed)
    {
        isHoverSuppressed = isSuppressed;
        if (isSuppressed)
        {
            //只停手不复位，变换交给外部动画接管(复位由KillHoverAnim兜底)
            scaleTween?.Kill();
            scaleTween = null;
            liftTween?.Kill();
            liftTween = null;
            tiltTarget = Vector2.zero;
            isHovering = false;
        }
    }

    /// <summary>
    /// 重新缓存初始变换；目标变换被外部永久修改后调用，避免还原到过期值
    /// </summary>
    public virtual void RefreshOriginalTransform()
    {
        if (targetTransform == null)
            return;
        scaleOriginal = targetTransform.localScale;
        rotationOriginal = targetTransform.localEulerAngles;
        if (targetRectTransform != null)
            positionOriginal = targetRectTransform.anchoredPosition;
    }
    #endregion
}
