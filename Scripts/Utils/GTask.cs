using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 通用异步任务封装（UniTask 门面）：业务层的异步等待/发射/取消统一经本类调用，不直接触碰 UniTask 与 CancellationToken 的复杂用法。
/// <para>取消即抛 OperationCanceledException；UniTask 默认静默忽略该异常（UniTaskScheduler.PropagateOperationCanceledException=false）。</para>
/// <para>发射即忘：业务方法声明为 async UniTaskVoid，调用点用 `_ = Method()` 显式丢弃（消除「未观察异步调用」警告）——取消静默、真异常由 UniTaskScheduler 记录，无需 try/catch；禁止 async void（其取消抛的 OCE 会作为未处理异常进 Console，还得手写 try/catch 样板）。GTask.Run 仅用于「方法返回 UniTask 供他处 await，个别调用点又要发射即忘」的复用场景。</para>
/// </summary>
public static class GTask
{
    //预取消令牌：取消源已 Cancel/Dispose 后兜底返回（等待立即取消退出，而非 NRE）
    internal static readonly CancellationToken cancelledToken = new CancellationToken(true);

    #region 等待
    /// <summary>
    /// 等待指定秒数（受 timeScale 影响，同 WaitForSeconds 语义；负数按 0 处理，但秒数≤0 时仍跨一帧完成，并非同步返回）
    /// </summary>
    /// <param name="seconds">等待秒数</param>
    /// <param name="cancel">取消源（可空，空=不可取消）</param>
    public static UniTask Wait(float seconds, GTaskCancel cancel = null)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(Math.Max(0f, seconds)), cancellationToken: GetToken(cancel));
    }

    /// <summary>
    /// 等待指定秒数（实时计时，不受 timeScale 影响，同 WaitForSecondsRealtime 语义；负数按 0 处理，但秒数≤0 时仍跨一帧完成，并非同步返回）
    /// </summary>
    /// <param name="seconds">等待秒数</param>
    /// <param name="cancel">取消源（可空，空=不可取消）</param>
    public static UniTask WaitReal(float seconds, GTaskCancel cancel = null)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(Math.Max(0f, seconds)), ignoreTimeScale: true, cancellationToken: GetToken(cancel));
    }

    /// <summary>
    /// 等待下一帧
    /// </summary>
    /// <param name="cancel">取消源（可空，空=不可取消）</param>
    public static UniTask WaitFrame(GTaskCancel cancel = null)
    {
        return UniTask.NextFrame(GetToken(cancel));
    }

    /// <summary>
    /// 等待指定帧数（小于 1 按 1 帧处理）
    /// </summary>
    /// <param name="frameCount">等待帧数</param>
    /// <param name="cancel">取消源（可空，空=不可取消）</param>
    public static UniTask WaitFrames(int frameCount, GTaskCancel cancel = null)
    {
        return UniTask.DelayFrame(Math.Max(1, frameCount), cancellationToken: GetToken(cancel));
    }

    /// <summary>
    /// 等到本帧 LateUpdate 之后（布局/画布已刷新，适合"等 UI 重建完再读尺寸"的场景）
    /// </summary>
    /// <param name="cancel">取消源（可空，空=不可取消）</param>
    public static UniTask WaitFrameEnd(GTaskCancel cancel = null)
    {
        return UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, GetToken(cancel));
    }

    /// <summary>
    /// 等待条件成立（每帧检查）
    /// </summary>
    /// <param name="condition">条件函数</param>
    /// <param name="cancel">取消源（可空，空=不可取消）</param>
    public static UniTask WaitUntil(Func<bool> condition, GTaskCancel cancel = null)
    {
        if (condition == null)
            throw new ArgumentNullException(nameof(condition));
        return UniTask.WaitUntil(condition, cancellationToken: GetToken(cancel));
    }

    /// <summary>
    /// 等待条件不再成立（每帧检查）
    /// </summary>
    /// <param name="condition">条件函数</param>
    /// <param name="cancel">取消源（可空，空=不可取消）</param>
    public static UniTask WaitWhile(Func<bool> condition, GTaskCancel cancel = null)
    {
        if (condition == null)
            throw new ArgumentNullException(nameof(condition));
        return UniTask.WaitWhile(condition, cancellationToken: GetToken(cancel));
    }

    /// <summary>
    /// 等待条件成立（带超时看门狗，防止条件永不成立导致永久挂起；取消时抛 OperationCanceledException）
    /// </summary>
    /// <param name="condition">条件函数</param>
    /// <param name="timeoutSeconds">超时秒数（默认按 scaled 时间累计，暂停时不计时；≤0 时仅当条件未立即成立即视为超时）</param>
    /// <param name="cancel">取消源（可空，空=不可取消）</param>
    /// <param name="ignoreTimeScale">true=超时按实时计时（timeScale=0 的暂停界面等场景必须开，否则暂停时看门狗同步冻结=退化为永久挂起）</param>
    /// <returns>true=条件已达成，false=超时</returns>
    public static async UniTask<bool> WaitUntilTimeout(Func<bool> condition, float timeoutSeconds, GTaskCancel cancel = null, bool ignoreTimeScale = false)
    {
        if (condition == null)
            throw new ArgumentNullException(nameof(condition));
        var token = GetToken(cancel);
        var elapsed = 0f;
        //逐帧轮询+手动计时：不用 WhenAny(Delay)，避免超时后输掉的 WaitUntil 残留永久轮询
        while (!condition())
        {
            if (elapsed >= timeoutSeconds)
                return false;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
        }
        return true;
    }

    /// <summary>
    /// 等待 Tween 播完（每帧检查 IsActive；tween 被 Kill 或销毁同样视为结束，防止永久挂起）。
    /// <para>注意：DOTween 默认回收 tween 实例，Kill 后同一帧内若该实例被新 tween 复用，等待会挂到新 tween 上——Kill 后不要再持有/复用同一引用变量。不用 OnComplete/OnKill 回调是因其为赋值语义，会覆盖调用方已有回调。</para>
    /// </summary>
    /// <param name="tween">目标 Tween</param>
    /// <param name="cancel">取消源（可空，空=不可取消）</param>
    public static UniTask WaitTween(Tween tween, GTaskCancel cancel = null)
    {
        if (tween == null)
            return UniTask.CompletedTask;
        return UniTask.WaitUntil(() => tween == null || !tween.IsActive(), cancellationToken: GetToken(cancel));
    }

    /// <summary>
    /// 取取消源的令牌（空取消源返回 default=不可取消；已取消的源返回预取消令牌=立即取消）
    /// </summary>
    private static CancellationToken GetToken(GTaskCancel cancel)
    {
        return cancel != null ? cancel.Token : default;
    }
    #endregion

    #region 发射
    /// <summary>
    /// 发射并遗忘（等效 UniTask.Forget：任务仍在主线程 PlayerLoop 上推进，不涉及线程切换；取消抛的 OperationCanceledException 静默忽略，其余异常由 UniTaskScheduler 记录到 Console）。
    /// 仅用于「方法返回 UniTask 供他处 await，本调用点又要发射即忘」的场景；专用的发射即忘方法应声明为 async UniTaskVoid 直接调用，不要写 async void。
    /// </summary>
    /// <param name="task">异步任务</param>
    public static void Run(UniTask task)
    {
        task.Forget();
    }

    /// <summary>
    /// 发射并遗忘（带返回值的任务，返回值被丢弃）
    /// </summary>
    /// <param name="task">异步任务</param>
    public static void Run<T>(UniTask<T> task)
    {
        task.Forget();
    }
    #endregion

    #region 组合
    /// <summary>
    /// 等待全部任务完成（等效 UniTask.WhenAll 的直通封装，如"等一组 Tween 都播完"）。
    /// 组合本身不接管取消：各任务须各自携带取消源（如 GTask.Wait(..., cancel) 生成），任一任务取消/失败即整体以该异常结束。
    /// </summary>
    /// <param name="tasks">任务组</param>
    public static UniTask WhenAll(params UniTask[] tasks)
    {
        return UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// 等待全部任务完成并聚齐返回值（等效 UniTask.WhenAll 的直通封装；组合本身不接管取消，各任务须各自携带取消源）
    /// </summary>
    /// <param name="tasks">任务组</param>
    public static UniTask<T[]> WhenAll<T>(params UniTask<T>[] tasks)
    {
        return UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// 等待任一任务完成，返回先完成的任务索引（等效 UniTask.WhenAny 的直通封装）。
    /// 组合本身不接管取消：注意输掉竞速的任务仍在后台继续跑，不再需要时应确保各自带取消源以便收口。
    /// </summary>
    /// <param name="tasks">任务组</param>
    public static UniTask<int> WhenAny(params UniTask[] tasks)
    {
        return UniTask.WhenAny(tasks);
    }
    #endregion

    #region 取消源
    /// <summary>
    /// 新建取消源（同一处任务建议只建一次复用，每次重新开始调 Reset 重建令牌即可）
    /// </summary>
    /// <param name="linkObj">链接对象（可选）：该 GameObject 销毁时自动取消，无需手动收口；不传则调用方必须在收口点 Cancel/Dispose</param>
    /// <returns>取消源</returns>
    public static GTaskCancel NewCancel(GameObject linkObj = null)
    {
        return new GTaskCancel(linkObj);
    }

    /// <summary>
    /// 新建取消源（Component 便捷版，链接其 gameObject；组件销毁时自动取消）。
    /// 独立命名而非 NewCancel 重载：GameObject 与 Component 无继承关系，重载会导致 NewCancel(null) 字面量调用编译歧义（CS0121）。
    /// </summary>
    /// <param name="link">链接组件（可空，空=无链接）</param>
    /// <returns>取消源</returns>
    public static GTaskCancel NewCancelFor(Component link)
    {
        return NewCancel(link == null ? null : link.gameObject);
    }
    #endregion
}

/// <summary>
/// 异步取消源（CancellationTokenSource 简封装）：Cancel() 取消并销毁，Reset() 取消并重建令牌复用，均幂等可重复调用。
/// </summary>
public class GTaskCancel : IDisposable
{
    protected CancellationTokenSource cts;
    //链接对象：该 GameObject 销毁时取消源自动取消（Reset 重建时保持链接；对象已销毁则退化为无链接，IsCancel 仍为 true）
    protected GameObject linkObj;

    /// <summary>取消令牌（仅供框架层 GTask 内部取用，业务层勿直接使用；已取消/已销毁时返回预取消令牌）</summary>
    internal CancellationToken Token => cts?.Token ?? GTask.cancelledToken;

    /// <summary>是否已取消（Cancel/链接对象销毁后为 true，Reset 后复位为 false）</summary>
    public bool IsCancel => cts == null || cts.IsCancellationRequested;

    public GTaskCancel(GameObject linkObj = null)
    {
        this.linkObj = linkObj;
        Reset();
    }

    /// <summary>
    /// 取消在途任务并重建令牌（复用本取消源，不新建对象）；重新开始任务时调用。
    /// 在途任务已在其 await 点抛 OperationCanceledException 退出，不会误读到新令牌。
    /// </summary>
    public void Reset()
    {
        Cancel();
        cts = linkObj == null
            ? new CancellationTokenSource()
            : CancellationTokenSource.CreateLinkedTokenSource(linkObj.GetCancellationTokenOnDestroy());
    }

    /// <summary>
    /// 取消并销毁（幂等）；取消后在途 await 抛 OperationCanceledException 退出（UniTask 默认静默忽略）
    /// </summary>
    public void Cancel()
    {
        //先取出本地引用并置空字段：Cancel 会同步执行回调，回调重入本方法时内层调用直接短路，避免重复 Dispose/NRE
        var source = cts;
        if (source == null)
            return;
        cts = null;
        //回调抛异常会包成 AggregateException 继续上抛，finally 兜底保证销毁不残留脏状态
        try { source.Cancel(); }
        finally { source.Dispose(); }
    }

    /// <summary>
    /// Dispose 等同 Cancel
    /// </summary>
    public void Dispose()
    {
        Cancel();
    }
}
