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
    /// 等待指定秒数（受 timeScale 影响，同 WaitForSeconds 语义；负数按 0 处理）
    /// </summary>
    /// <param name="seconds">等待秒数</param>
    /// <param name="cancel">取消源（可空，空=不可取消）</param>
    public static UniTask Wait(float seconds, GTaskCancel cancel = null)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(Math.Max(0f, seconds)), cancellationToken: GetToken(cancel));
    }

    /// <summary>
    /// 等待指定秒数（实时计时，不受 timeScale 影响，同 WaitForSecondsRealtime 语义；负数按 0 处理）
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
    /// 等待 Tween 播完（每帧检查；tween 被 Kill 或销毁同样视为结束，防止永久挂起）
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
    /// 发射并遗忘（取消抛的 OperationCanceledException 静默忽略；其余异常由 UniTaskScheduler 记录到 Console）。
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
        if (cts == null)
            return;
        cts.Cancel();
        cts.Dispose();
        cts = null;
    }

    /// <summary>
    /// Dispose 等同 Cancel
    /// </summary>
    public void Dispose()
    {
        Cancel();
    }
}
