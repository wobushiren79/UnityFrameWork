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
    }

    public override void OnDisable()
    {
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

    /// <summary>
    /// 刷新控件大小
    /// </summary>
    public void RefreshViewSize()
    {
        UGUIUtil.RefreshUISize(rectTransform);
    }
}