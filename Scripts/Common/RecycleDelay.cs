/// <summary>
/// 回收延迟描述，用于 <see cref="BaseManager.ScheduleRecycle"/> 这类延迟回收 API。
/// 区分"立即 / 下一帧 / N 秒"三种语义，避免用 0/-1 这种魔法值表达延迟。
/// </summary>
public readonly struct RecycleDelay
{
    /// <summary>
    /// 延迟方式
    /// </summary>
    public enum DelayMode
    {
        /// <summary>本帧立即回收</summary>
        Immediate,
        /// <summary>下一帧再回收 (避免当前帧动作闪现/数据被复用等问题)</summary>
        NextFrame,
        /// <summary>等待 N 秒后再回收</summary>
        Seconds,
    }

    public readonly DelayMode mode;
    public readonly float seconds;

    private RecycleDelay(DelayMode mode, float seconds)
    {
        this.mode = mode;
        this.seconds = seconds;
    }

    /// <summary>本帧同步回收 (不入待回收队列)</summary>
    public static readonly RecycleDelay Immediate = new RecycleDelay(DelayMode.Immediate, 0f);

    /// <summary>下一帧再回收 (兼容原 WaitNextFrame 的语义)</summary>
    public static readonly RecycleDelay NextFrame = new RecycleDelay(DelayMode.NextFrame, 0f);

    /// <summary>
    /// 等待 N 秒后再回收；如果 seconds &lt;= 0 则自动降级为 <see cref="NextFrame"/>
    /// (避免悄悄退化成 Immediate 引发隐 bug)
    /// </summary>
    public static RecycleDelay Wait(float seconds)
    {
        return seconds > 0f ? new RecycleDelay(DelayMode.Seconds, seconds) : NextFrame;
    }
}
