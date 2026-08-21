using UnityEngine;
using UnityEditor;
using System;
using DG.Tweening;
using UnityEngine.UI;

public class PopupShowView : BaseUIView
{
    //鼠标位置和弹窗偏移量
    public float offsetX = 0;
    public float offsetY = 0;
    public Vector2 offsetPivot = Vector2.zero;
    //是否实时更新位置
    public bool isUpdatePosition = true;

    #region 位置缓动参数
    //位置跟随刚度(越大越快贴近目标)
    public float positionFollowSpeed = 14f;
    //到达目标时的回弹过冲强度(0=无过冲, 越大OutBack回弹越明显)
    [Range(0f, 0.85f)]
    public float positionOvershoot = 0.45f;

    //位置缓动当前速度
    protected Vector3 positionVelocity;
    //缓动逼近的目标位置
    protected Vector3 targetPosition;
    //是否已初始化目标位置(首帧直接吸附, 避免从原点大幅滑入)
    protected bool hasTargetPosition;
    #endregion

    #region 出现/消失动画参数
    [Header("是否播放出现动画")]
    public bool isAnimForShow = true;
    [Header("是否播放消失动画")]
    public bool isAnimForHide = true;
    [Header("动画是否包含淡入淡出(代码自动补CanvasGroup, 无需prefab配置)")]
    public bool isAnimWithFade = true;
    [Header("出现动画时长")]
    public float animForShowDuration = 0.18f;
    [Header("消失动画时长")]
    public float animForHideDuration = 0.12f;

    //缩放动画Tween句柄
    protected Tween popupAnimTween;
    //淡入淡出用CanvasGroup(代码GetOrAdd)
    protected CanvasGroup canvasGroupForAnim;
    //消失动画播放中标记：抑制重复隐藏，并供ShowWithAnim判定中断恢复
    protected bool isHidingForAnim;
    #endregion

    protected Direction2DEnum mouseAreaLeftRight =  Direction2DEnum.Left;
    protected Direction2DEnum mouseAreaUpDown = Direction2DEnum.Down;

    //触发该弹窗的对象（通常为PopupButton所在的GameObject），用于检测触发对象意外失活时自动隐藏
    protected GameObject triggerObj;
    //触发对象失效时的回调（一般为触发按钮的CleanData）
    protected Action onTriggerInvalid;

    public override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 每帧计算目标位置、缓动逼近并检测触发对象有效性
    /// </summary>
    public virtual void Update()
    {
        if (rectTransform == null)
            return;
        //计算弹窗的目标位置(跟随鼠标)与轴心
        InitPosition();
        //缓动逼近目标位置(阻尼弹簧, 到达时带OutBack回弹)
        UpdatePositionTween();
        //检测触发对象是否还有效，若已失活则自动隐藏弹窗
        CheckTriggerValid();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        InitPosition();
        //重新激活时清除隐藏标记并播出现动画(首次创建与缓存复用走同一路径)
        isHidingForAnim = false;
        if (isAnimForShow)
            AnimForShow();
    }

    public override void OnDisable()
    {
        //先停动画并复位缩放/透明度，保证下次启用从干净状态开始
        KillPopupAnim();
        ResetPopupAnimState();
        isHidingForAnim = false;
        base.OnDisable();
        //清空触发器引用，避免下次启用时执行旧回调导致误关闭
        triggerObj = null;
        onTriggerInvalid = null;
        //重置缓动状态, 下次启用时重新吸附到鼠标位置
        hasTargetPosition = false;
        positionVelocity = Vector3.zero;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(0, 0);
        }
    }

    /// <summary>
    /// 设置触发该弹窗的对象与失效回调
    /// 弹窗会在Update中检测该对象是否还激活；若意外失活（被禁用或销毁），则触发回调以隐藏自身，
    /// 防止PopupButton意外关闭后弹窗一直存在的BUG
    /// </summary>
    /// <param name="obj">触发该弹窗的GameObject，通常为PopupButton所在的GameObject</param>
    /// <param name="onInvalid">触发对象失效时执行的回调（一般为触发按钮的CleanData）</param>
    public virtual void SetTrigger(GameObject obj, Action onInvalid)
    {
        triggerObj = obj;
        onTriggerInvalid = onInvalid;
    }

    /// <summary>
    /// 检测触发对象是否还有效，若已被禁用/销毁则触发回调以隐藏弹窗
    /// </summary>
    protected virtual void CheckTriggerValid()
    {
        //未注册回调时跳过检测，避免误触发
        if (onTriggerInvalid == null)
            return;
        //触发对象被销毁或被禁用时，调用一次回调并清理引用
        if (triggerObj == null || !triggerObj.activeInHierarchy)
        {
            Action callback = onTriggerInvalid;
            triggerObj = null;
            onTriggerInvalid = null;
            callback.Invoke();
        }
    }


    /// <summary>
    /// 计算弹窗目标位置(跟随鼠标)与轴心；首帧直接吸附, 之后交由缓动逼近
    /// </summary>
    public virtual void InitPosition()
    {
        if (isUpdatePosition && gameObject.activeSelf)
        {
            Transform tfContainer = UIHandler.Instance.manager.GetUITypeContainer(UITypeEnum.Popup);
            //屏幕坐标转换为UI坐标
            Vector2 outPosition = GameUtil.MousePointToUGUIPoint(null,(RectTransform)tfContainer);
            float moveX = outPosition.x;
            float moveY = outPosition.y;

            //记录目标位置, 实际位移由UpdatePositionTween缓动逼近(不再直接吸附)
            targetPosition = new Vector3(moveX + offsetX, moveY + offsetY, transform.localPosition.z);
            //首次出现直接吸附到目标, 避免从原点大幅滑入
            if (!hasTargetPosition)
            {
                hasTargetPosition = true;
                transform.localPosition = targetPosition;
                positionVelocity = Vector3.zero;
            }

            float offsetTotalX;
            float offsetTotalY;
            //判断鼠标在屏幕的左右
            if (Input.mousePosition.x <= (Screen.width / 2))
            {    
                //左
                offsetTotalX = 0 - offsetPivot.x;
                mouseAreaLeftRight = Direction2DEnum.Left;
            }
            else
            {  
                //右
                offsetTotalX = 1 + offsetPivot.x;
                mouseAreaLeftRight = Direction2DEnum.Right;
            }

            //屏幕上下修正
            if (Input.mousePosition.y <= (Screen.height / 2))
            {
                //下
                offsetTotalY = 0 + offsetPivot.y;
                mouseAreaUpDown = Direction2DEnum.Down;
            }
            else
            {
                //上
                offsetTotalY = 1 + offsetPivot.y;
                mouseAreaUpDown = Direction2DEnum.Up;
            }
            rectTransform.pivot = new Vector2(offsetTotalX, offsetTotalY);
        }
    }

    /// <summary>
    /// 阻尼弹簧缓动逼近目标位置：阻尼比&lt;1时到达目标会过冲并回弹, 形成OutBack效果
    /// </summary>
    protected virtual void UpdatePositionTween()
    {
        if (!hasTargetPosition)
            return;
        //用不受时间缩放影响的delta, 保证暂停时弹窗仍正常缓动; 限幅避免低帧时弹簧发散
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.04f);
        if (dt <= 0f)
            return;
        //omega=角频率(跟随刚度), zeta=阻尼比(<1过冲, =1临界无过冲)
        float omega = positionFollowSpeed;
        float zeta = Mathf.Clamp(1f - positionOvershoot, 0.1f, 1f);
        Vector3 pos = transform.localPosition;
        Vector3 disp = pos - targetPosition;
        //半隐式积分: a = -ω²·位移 - 2ζω·速度
        Vector3 accel = -(omega * omega) * disp - (2f * zeta * omega) * positionVelocity;
        positionVelocity += accel * dt;
        pos += positionVelocity * dt;
        pos.z = targetPosition.z;
        transform.localPosition = pos;
    }

    #region 出现/消失动画
    /// <summary>
    /// 出现动画：缩放0→1(OutBack绕pivot角弹出, pivot恒在靠鼠标一角=从指针弹出)+可选淡入；播前强制复位保证From终点正确
    /// </summary>
    public virtual void AnimForShow()
    {
        KillPopupAnim();
        ResetPopupAnimState();
        isHidingForAnim = false;
        popupAnimTween = transform.DOScale(Vector3.zero, animForShowDuration)
            .From().SetEase(Ease.OutBack).SetUpdate(UpdateType.Normal, true);
        if (isAnimWithFade)
        {
            GetOrAddCanvasGroupForAnim().DOFade(0f, animForShowDuration * 0.7f)
                .From().SetUpdate(UpdateType.Normal, true);
        }
    }

    /// <summary>
    /// 消失动画：缩放→0(InBack蓄力回缩)+可选淡出，播完回调真正隐藏
    /// </summary>
    /// <param name="onComplete">动画播完回调(真正执行隐藏)</param>
    public virtual void AnimForHide(Action onComplete)
    {
        KillPopupAnim();
        popupAnimTween = transform.DOScale(Vector3.zero, animForHideDuration)
            .SetEase(Ease.InBack).SetUpdate(UpdateType.Normal, true)
            .OnComplete(() => onComplete?.Invoke());
        if (isAnimWithFade)
        {
            GetOrAddCanvasGroupForAnim().DOFade(0f, animForHideDuration)
                .SetUpdate(UpdateType.Normal, true);
        }
    }

    /// <summary>
    /// 带动画展示：供UIHandler.ShowPopup调用，替代原ShowObj(true)；消失动画中复开走中断恢复
    /// </summary>
    public virtual void ShowWithAnim()
    {
        if (gameObject.activeInHierarchy)
        {
            //已激活且不在消失动画中=重复展示请求，直接忽略避免重播出现动画
            if (!isHidingForAnim)
                return;
            //消失动画被打断：Kill隐藏Tween(不触发其OnComplete，不会误隐藏)后重播出现动画
            AnimForShow();
            return;
        }
        //未激活：SetActive触发OnEnable，由OnEnable播出现动画
        this.ShowObj(true);
    }

    /// <summary>
    /// 带动画隐藏：供UIHandler.HidePopup调用，替代原ShowObj(false)；播完消失动画才真正失活
    /// </summary>
    public virtual void HideWithAnim()
    {
        //消失动画播放中重复调用直接忽略(防CheckTriggerValid→ClearData→HidePopup重入)
        if (isHidingForAnim)
            return;
        //开关关闭或对象已失活时保持原立即隐藏行为
        if (!isAnimForHide || !gameObject.activeInHierarchy)
        {
            this.ShowObj(false);
            return;
        }
        isHidingForAnim = true;
        AnimForHide(() =>
        {
            //播完真正隐藏；若期间被ShowWithAnim打断标记已清，不会误隐藏
            if (isHidingForAnim)
                this.ShowObj(false);
        });
    }

    /// <summary>
    /// 清理动画Tween(按句柄与CanvasGroup杀，不用transform.DOKill避免误杀其他动画)
    /// </summary>
    protected virtual void KillPopupAnim()
    {
        popupAnimTween?.Kill();
        popupAnimTween = null;
        if (canvasGroupForAnim != null)
            canvasGroupForAnim.DOKill();
    }

    /// <summary>
    /// 复位动画状态：缩放与透明度回满
    /// </summary>
    protected virtual void ResetPopupAnimState()
    {
        transform.localScale = Vector3.one;
        if (canvasGroupForAnim != null)
            canvasGroupForAnim.alpha = 1f;
    }

    /// <summary>
    /// 代码GetOrAdd CanvasGroup(仅alpha动画使用; 新添加时关闭blocksRaycasts防气泡边缘抢射线)
    /// </summary>
    protected virtual CanvasGroup GetOrAddCanvasGroupForAnim()
    {
        if (canvasGroupForAnim == null)
        {
            canvasGroupForAnim = GetComponent<CanvasGroup>();
            if (canvasGroupForAnim == null)
            {
                canvasGroupForAnim = gameObject.AddComponent<CanvasGroup>();
                //tooltip本不该拦截射线，关闭后根除动画期间气泡边缘扫过光标导致的闪烁
                canvasGroupForAnim.blocksRaycasts = false;
            }
        }
        return canvasGroupForAnim;
    }
    #endregion

    /// <summary>
    /// 刷新控件大小
    /// </summary>
    public void RefreshViewSize()
    {
        UGUIUtil.RefreshUISize(rectTransform);
    }
}