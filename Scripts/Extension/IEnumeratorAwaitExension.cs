using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using UnityEngine;
public class WaitNextFrame
{
}

public static class Awaiters
{

    //readonly static WaitNextFrame _waitNextFrame = new WaitNextFrame();
    //readonly static WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();
    //readonly static WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();


    /// <summary>
    /// 这里改成不缓存，因为增加了取消逻辑，如果操作同一个对象取消，会导致同时使用的地方也返回异常
    /// </summary>
    public static WaitNextFrame NextFrame { get { return new WaitNextFrame(); } }
    public static WaitForFixedUpdate FixedUpdate { get { return new WaitForFixedUpdate(); } }
    public static WaitForEndOfFrame EndOfFrame { get { return new WaitForEndOfFrame(); } }

    public static WaitForSeconds Seconds(float seconds)
    {
        return new WaitForSeconds(seconds);
    }

    public static WaitForSecondsRealtime SecondsRealtime(float seconds)
    {
        return new WaitForSecondsRealtime(seconds);
    }

    public static WaitUntil Until(Func<bool> predicate)
    {
        return new WaitUntil(predicate);
    }

    public static WaitWhile While(Func<bool> predicate)
    {
        return new WaitWhile(predicate);
    }
}

public static class SyncContextUtil
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        UnitySynchronizationContext = SynchronizationContext.Current;
        UnityThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public static int UnityThreadId
    {
        get; private set;
    }

    public static SynchronizationContext UnitySynchronizationContext
    {
        get; private set;
    }
}

public class AsyncCoroutineRunner : MonoBehaviour
{
    static AsyncCoroutineRunner _instance;

    public static AsyncCoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject("AsyncCoroutineRunner")
                    .AddComponent<AsyncCoroutineRunner>();
            }

            return _instance;
        }
    }

    void Awake()
    {
        // Don't show in scene hierarchy
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        DontDestroyOnLoad(gameObject);
    }
}

public interface CancelAwaiter
{
    void Cancel();
}

public struct ClassAwaiterInfo
{
    public CancelAwaiter notifyCompletion;
    public IEnumerator enumerator;
    public float callTime;

    public void Cancel()
    {
        notifyCompletion.Cancel();
        AsyncCoroutineRunner.Instance.StopCoroutine(enumerator);
    }
}

// We could just add a generic GetAwaiter to YieldInstruction and CustomYieldInstruction
// but instead we add specific methods to each derived class to allow for return values
// that make the most sense for the specific instruction type
public static class IEnumeratorAwaitExtension
{
    public static Dictionary<string, List<ClassAwaiterInfo>> awaitDict = new Dictionary<string, List<ClassAwaiterInfo>>();

    public abstract class AwaiterBase : CancelAwaiter
    {
        protected bool _isDone;
        protected Exception _exception;
        protected Action _continuation;
        protected float _callTime;
        public string CalssName { get; set; }

        public void Cancel()
        {
            if (!string.IsNullOrEmpty(CalssName) && awaitDict.ContainsKey(CalssName))
            {
                var delItem = awaitDict[CalssName].Find(value => _callTime == value.callTime);
                awaitDict[CalssName].Remove(delItem);
                if (awaitDict[CalssName].Count == 0)
                {
                    awaitDict.Remove(CalssName);
                }
            }
            OnCancel();
        }

        public abstract void OnCancel();

        public void InitStack(string className, IEnumerator enumerator)
        {
            _callTime = Time.time;
            CalssName = className;
            if (string.IsNullOrEmpty(CalssName))
                return;

            if (!awaitDict.ContainsKey(CalssName))
            {
                var awaiterList = new List<ClassAwaiterInfo>();
                awaiterList.Add(new ClassAwaiterInfo() { enumerator = enumerator, notifyCompletion = this, callTime = _callTime });
                awaitDict.Add(CalssName, awaiterList);
            }
            else
            {
                awaitDict[className].Add(new ClassAwaiterInfo() { enumerator = enumerator, notifyCompletion = this, callTime = _callTime });
            }
        }
    }

    public static void StopAwait(this object value, string Name)
    {
        List<ClassAwaiterInfo> classAwaiterInfo;
        var className = Name;
        if (awaitDict.TryGetValue(className, out classAwaiterInfo))
        {
            for (int i = 0; i < classAwaiterInfo.Count; i++)
            {
                classAwaiterInfo[i].Cancel();
            }
            awaitDict.Remove(className);
        }
    }

    public static Dictionary<object, string> objectName = new Dictionary<object, string>();

    public static T SetAwaiterName<T>(this T nameObj, string Value)
    {
        if (objectName == null)
            objectName = new Dictionary<object, string>();

        if (!objectName.ContainsKey(nameObj))
        {
            objectName.Add(nameObj, Value);
        }
        return nameObj;
    }

    public static string GetAwaiterName(this object nameObj)
    {
        if (objectName == null || nameObj == null)
            return null;

        if (objectName.ContainsKey(nameObj))
        {
            return objectName[nameObj];
        }
        return null;
    }


    public static SimpleCoroutineAwaiter GetAwaiter(this WaitForSeconds instruction)
    {
        return GetAwaiterReturnVoid(instruction);
    }

    public static SimpleCoroutineAwaiter GetAwaiter(this WaitForEndOfFrame instruction)
    {
        return GetAwaiterReturnVoid(instruction);
    }

    public static SimpleCoroutineAwaiter GetAwaiter(this WaitNextFrame instruction)
    {
        return GetAwaiterReturnVoid(null);
    }

    public static SimpleCoroutineAwaiter GetAwaiter(this WaitForFixedUpdate instruction)
    {
        return GetAwaiterReturnVoid(instruction);
    }

    public static SimpleCoroutineAwaiter GetAwaiter(this WaitForSecondsRealtime instruction)
    {
        return GetAwaiterReturnVoid(instruction);
    }

    public static SimpleCoroutineAwaiter GetAwaiter(this WaitUntil instruction)
    {
        return GetAwaiterReturnVoid(instruction);
    }

    public static SimpleCoroutineAwaiter GetAwaiter(this WaitWhile instruction)
    {
        return GetAwaiterReturnVoid(instruction);
    }

    public static SimpleCoroutineAwaiter<AsyncOperation> GetAwaiter(this AsyncOperation instruction)
    {
        return GetAwaiterReturnSelf(instruction);
    }

    public static SimpleCoroutineAwaiter<UnityEngine.Object> GetAwaiter(this ResourceRequest instruction)
    {
        var awaiter = new SimpleCoroutineAwaiter<UnityEngine.Object>();
        var action = InstructionWrappers.ResourceRequest(awaiter, instruction);
        awaiter.InitStack(instruction.GetAwaiterName(), action);
        RunOnUnityScheduler(() =>
        {
            try
            {
                AsyncCoroutineRunner.Instance.StartCoroutine(
                    action);
            }
            catch (System.Exception exception)
            {
                var stackTrace = exception.Data["StackTrace"];
                LogUtil.LogError($"{exception.Message}{System.Environment.NewLine}{stackTrace}");
            }
        }
        );

        return awaiter;
    }

    // Return itself so you can do things like (await new WWW(url)).bytes
    public static SimpleCoroutineAwaiter<WWW> GetAwaiter(this WWW instruction)
    {
        return GetAwaiterReturnSelf(instruction);
    }

    public static SimpleCoroutineAwaiter<AssetBundle> GetAwaiter(this AssetBundleCreateRequest instruction)
    {
        var awaiter = new SimpleCoroutineAwaiter<AssetBundle>();
        var action = InstructionWrappers.AssetBundleCreateRequest(awaiter, instruction);
        awaiter.InitStack(instruction.GetAwaiterName(), action);
        RunOnUnityScheduler(() => AsyncCoroutineRunner.Instance.StartCoroutine(action
           ));
        return awaiter;
    }

    public static SimpleCoroutineAwaiter<UnityEngine.Object> GetAwaiter(this AssetBundleRequest instruction)
    {
        var awaiter = new SimpleCoroutineAwaiter<UnityEngine.Object>();
        var action = InstructionWrappers.AssetBundleRequest(awaiter, instruction);
        awaiter.InitStack(instruction.GetAwaiterName(), action);
        RunOnUnityScheduler(() => AsyncCoroutineRunner.Instance.StartCoroutine(
            action));
        return awaiter;
    }

    public static SimpleCoroutineAwaiter<T> GetAwaiter<T>(this IEnumerator<T> coroutine)
    {
        var awaiter = new SimpleCoroutineAwaiter<T>();
        var action = new CoroutineWrapper<T>(coroutine, awaiter).Run();
        awaiter.InitStack(coroutine.GetAwaiterName(), action);
        RunOnUnityScheduler(() => AsyncCoroutineRunner.Instance.StartCoroutine(
           action));
        return awaiter;
    }

    public static SimpleCoroutineAwaiter<object> GetAwaiter(this IEnumerator coroutine)
    {
        var awaiter = new SimpleCoroutineAwaiter<object>();
        var action = new CoroutineWrapper<object>(coroutine, awaiter).Run();
        awaiter.InitStack(coroutine.GetAwaiterName(), action);
        RunOnUnityScheduler(() => AsyncCoroutineRunner.Instance.StartCoroutine(
            action));
        return awaiter;
    }

    static SimpleCoroutineAwaiter GetAwaiterReturnVoid(object instruction)
    {
        var awaiter = new SimpleCoroutineAwaiter();
        var action = InstructionWrappers.ReturnVoid(awaiter, instruction);
        awaiter.InitStack(instruction.GetAwaiterName(), action);
        RunOnUnityScheduler(() => AsyncCoroutineRunner.Instance.StartCoroutine(
            action));
        return awaiter;
    }

    static SimpleCoroutineAwaiter<T> GetAwaiterReturnSelf<T>(T instruction)
    {
        var awaiter = new SimpleCoroutineAwaiter<T>();
        var action = InstructionWrappers.ReturnSelf(awaiter, instruction);
        awaiter.InitStack(instruction.GetAwaiterName(), action);
        RunOnUnityScheduler(() => AsyncCoroutineRunner.Instance.StartCoroutine(
            action));
        return awaiter;
    }

    static void RunOnUnityScheduler(Action action)
    {
        if (SynchronizationContext.Current == SyncContextUtil.UnitySynchronizationContext)
        {
            action();
        }
        else
        {
            SyncContextUtil.UnitySynchronizationContext.Post(_ => action(), null);
        }
    }

    static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new Exception("Assert hit in UnityAsyncUtil package!");
        }
    }

    public class SimpleCoroutineAwaiter<T> : AwaiterBase, INotifyCompletion
    {
        T _result;

        public bool IsCompleted
        {
            get { return _isDone; }
        }

        public T GetResult()
        {
            Assert(_isDone);

            if (_exception != null)
            {
                ExceptionDispatchInfo.Capture(_exception).Throw();
            }

            return _result;
        }

        public void Complete(T result, Exception e)
        {
            Assert(!_isDone);

            _isDone = true;
            _exception = e;
            _result = result;

            if (_continuation != null)
            {
                RunOnUnityScheduler(_continuation);
            }
            Cancel();
        }

        void INotifyCompletion.OnCompleted(Action continuation)
        {
            Assert(_continuation == null);
            Assert(!_isDone);

            _continuation = continuation;
        }

        public override void OnCancel()
        {
            if (!_isDone)
            {
                Complete(default(T), new Exception("Task to cancel"));
            }
        }
    }

    public class SimpleCoroutineAwaiter : AwaiterBase, INotifyCompletion
    {
        public bool IsCompleted
        {
            get { return _isDone; }
        }

        public void GetResult()
        {
            Assert(_isDone);

            if (_exception != null)
            {
                ExceptionDispatchInfo.Capture(_exception).Throw();
            }
        }

        public void Complete(Exception e)
        {
            Assert(!_isDone);

            _isDone = true;
            _exception = e;

            if (_continuation != null)
            {
                RunOnUnityScheduler(_continuation);
            }

            Cancel();
        }

        void INotifyCompletion.OnCompleted(Action continuation)
        {
            Assert(_continuation == null);
            Assert(!_isDone);

            _continuation = continuation;
        }

        public override void OnCancel()
        {
            if (!_isDone)
            {
                Complete(new Exception("Task to Cancel"));
            }
        }
    }

    class CoroutineWrapper<T>
    {
        readonly SimpleCoroutineAwaiter<T> _awaiter;
        readonly Stack<IEnumerator> _processStack;

        public CoroutineWrapper(
            IEnumerator coroutine, SimpleCoroutineAwaiter<T> awaiter)
        {
            _processStack = new Stack<IEnumerator>();
            _processStack.Push(coroutine);
            _awaiter = awaiter;
        }

        public IEnumerator Run()
        {
            while (true)
            {
                var topWorker = _processStack.Peek();

                bool isDone;

                try
                {
                    isDone = !topWorker.MoveNext();
                }
                catch (Exception e)
                {
                    // The IEnumerators we have in the process stack do not tell us the
                    // actual names of the coroutine methods but it does tell us the objects
                    // that the IEnumerators are associated with, so we can at least try
                    // adding that to the exception output
                    var objectTrace = GenerateObjectTrace(_processStack);

                    if (objectTrace.Any())
                    {
                        _awaiter.Complete(
                            default(T), new Exception(
                                GenerateObjectTraceMessage(objectTrace), e));
                    }
                    else
                    {
                        _awaiter.Complete(default(T), e);
                    }

                    yield break;
                }

                if (isDone)
                {
                    _processStack.Pop();

                    if (_processStack.Count == 0)
                    {
                        _awaiter.Complete((T)topWorker.Current, null);
                        yield break;
                    }
                }

                // We could just yield return nested IEnumerator's here but we choose to do
                // our own handling here so that we can catch exceptions in nested coroutines
                // instead of just top level coroutine
                if (topWorker.Current is IEnumerator)
                {
                    _processStack.Push((IEnumerator)topWorker.Current);
                }
                else
                {
                    // Return the current value to the unity engine so it can handle things like
                    // WaitForSeconds, WaitToEndOfFrame, etc.
                    yield return topWorker.Current;
                }
            }
        }

        string GenerateObjectTraceMessage(List<Type> objTrace)
        {
            var result = new StringBuilder();

            foreach (var objType in objTrace)
            {
                if (result.Length != 0)
                {
                    result.Append(" -> ");
                }

                result.Append(objType.ToString());
            }

            result.AppendLine();
            return "Unity Coroutine Object Trace: " + result.ToString();
        }

        static List<Type> GenerateObjectTrace(IEnumerable<IEnumerator> enumerators)
        {
            var objTrace = new List<Type>();

            foreach (var enumerator in enumerators)
            {
                // NOTE: This only works with scripting engine 4.6
                // And could easily stop working with unity updates
                var field = enumerator.GetType().GetField("$this", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                if (field == null)
                {
                    continue;
                }

                var obj = field.GetValue(enumerator);

                if (obj == null)
                {
                    continue;
                }

                var objType = obj.GetType();

                if (!objTrace.Any() || objType != objTrace.Last())
                {
                    objTrace.Add(objType);
                }
            }

            objTrace.Reverse();
            return objTrace;
        }
    }

    static class InstructionWrappers
    {
        public static IEnumerator ReturnVoid(
            SimpleCoroutineAwaiter awaiter, object instruction)
        {
            // For simple instructions we assume that they don't throw exceptions
            yield return instruction;
            awaiter.Complete(null);
        }

        public static IEnumerator AssetBundleCreateRequest(
            SimpleCoroutineAwaiter<AssetBundle> awaiter, AssetBundleCreateRequest instruction)
        {
            yield return instruction;
            awaiter.Complete(instruction.assetBundle, null);
        }

        public static IEnumerator ReturnSelf<T>(
            SimpleCoroutineAwaiter<T> awaiter, T instruction)
        {
            yield return instruction;
            awaiter.Complete(instruction, null);
        }

        public static IEnumerator AssetBundleRequest(
            SimpleCoroutineAwaiter<UnityEngine.Object> awaiter, AssetBundleRequest instruction)
        {
            yield return instruction;
            awaiter.Complete(instruction.asset, null);
        }

        public static IEnumerator ResourceRequest(
            SimpleCoroutineAwaiter<UnityEngine.Object> awaiter, ResourceRequest instruction)
        {
            yield return instruction;
            awaiter.Complete(instruction.asset, null);
        }
    }
}
