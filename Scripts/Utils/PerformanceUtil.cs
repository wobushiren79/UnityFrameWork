using System;
using UnityEngine;
using UnityEngine.Profiling;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
using SDProcess = System.Diagnostics.Process;
#endif

/// <summary>
/// 运行时性能消耗采样工具：FPSHandler 的性能浮层从本类读取快照数据，业务层不要直接触碰采样细节。
/// <para>CPU/GPU 帧耗时(ms) 走 FrameTimingManager；CPU 占用率走进程处理器时间差分；GPU 占用率走 Windows PDH 性能计数器（GPU Engine 英文路径，无视中文系统的计数器名本地化）；内存走 Profiler 总预留。</para>
/// <para>占用率(%) 仅 Windows（含编辑器）有效；采样未就绪/平台不支持时对应 Has 标志为 false，界面显示 -。</para>
/// </summary>
public static class PerformanceUtil
{
    #region 数据快照
    /// <summary>CPU 主线程帧耗时（毫秒，最近若干帧平均）</summary>
    public static double CpuFrameMs;
    /// <summary>GPU 帧耗时（毫秒，最近若干帧平均）</summary>
    public static double GpuFrameMs;
    /// <summary>GPU 帧耗时是否有效（编辑器与不支持的图形接口下为 false）</summary>
    public static bool HasGpuFrameMs;
    /// <summary>CPU 占用率（0-100，已按核心数归一化）</summary>
    public static double CpuPercent;
    /// <summary>CPU 占用率是否有效（首次采样仅建基线，第二次起为 true）</summary>
    public static bool HasCpuPercent;
    /// <summary>GPU 占用率（本进程全部 GPU 引擎实例求和，可能超 100，与任务管理器同口径）</summary>
    public static double GpuPercent;
    /// <summary>GPU 占用率是否有效（PDH 初始化失败或采样未就绪为 false）</summary>
    public static bool HasGpuPercent;
    /// <summary>总预留内存（MB）</summary>
    public static long MemMB;
    #endregion

    #region 刷新入口
    private static float lastRefreshTime = -1f; //上次刷新时间（-1 保证首次必刷新）
    private const float RefreshInterval = 0.5f; //刷新间隔（秒）

    /// <summary>
    /// 到刷新点（0.5 秒节流，实时计时不受 timeScale 影响）时采样所有指标并返回 true
    /// </summary>
    /// <returns>本次是否真正执行了采样</returns>
    public static bool Refresh()
    {
        float now = Time.realtimeSinceStartup;
        if (now - lastRefreshTime < RefreshInterval)
            return false;
        lastRefreshTime = now;
        RefreshFrameTiming();
        RefreshCpuPercent(now);
        RefreshGpuPercent();
        MemMB = Profiler.GetTotalReservedMemoryLong() >> 20;
        return true;
    }
    #endregion

    #region 帧耗时采样
    private static readonly FrameTiming[] frameTimings = new FrameTiming[16]; //预分配缓冲，采样零分配

    /// <summary>
    /// 每帧调用一次捕获帧时序（仅在浮层显示时调用，隐藏时无开销）
    /// </summary>
    public static void CaptureFrame()
    {
        FrameTimingManager.CaptureFrameTimings();
    }

    /// <summary>
    /// 刷新帧耗时快照：平均缓冲内最近若干帧的 CPU 主线程/GPU 耗时
    /// </summary>
    private static void RefreshFrameTiming()
    {
        uint count = FrameTimingManager.GetLatestTimings((uint)frameTimings.Length, frameTimings);
        if (count == 0)
            return; //无新数据，保持上次值
        double cpuSum = 0, gpuSum = 0;
        for (uint i = 0; i < count; i++)
        {
            cpuSum += frameTimings[i].cpuMainThreadFrameTime;
            gpuSum += frameTimings[i].gpuFrameTime;
        }
        CpuFrameMs = cpuSum / count * 1000.0;
        GpuFrameMs = gpuSum / count * 1000.0;
        HasGpuFrameMs = gpuSum > 0; //编辑器下 gpuFrameTime 恒 0
    }
    #endregion

    #region 生命周期
    /// <summary>
    /// 初始化采样（幂等）：缓存进程标识、建立 CPU 基线、初始化 GPU 占用率计数器
    /// </summary>
    public static void Init()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (pdhQuery != IntPtr.Zero)
            return; //已初始化（句柄即幂等标志，兼容禁用域重载的 Play 模式）
        pidPrefix = $"pid_{SDProcess.GetCurrentProcess().Id}_";
        //CPU 首次采样仅建立基线，下个刷新点才有差分数据
        lastProcessorTime = SDProcess.GetCurrentProcess().TotalProcessorTime;
        lastCpuWallTime = Time.realtimeSinceStartup;
        cpuBaseReady = true;
        //GPU Engine 计数器：英文路径无视中文系统本地化；通配符挂全部实例，进程生灭免管理
        if (PdhOpenQuery(null, IntPtr.Zero, out pdhQuery) == 0
            && PdhAddEnglishCounter(pdhQuery, "\\GPU Engine(*)\\Utilization Percentage", IntPtr.Zero, out pdhCounter) == 0)
        {
            pdhReady = true;
            pdhPrimed = false;
        }
        else
        {
            pdhQuery = IntPtr.Zero; //初始化失败永久不重试，GPU% 恒显示 -
        }
#endif
    }

    /// <summary>
    /// 释放采样资源（关闭 PDH 查询句柄）
    /// </summary>
    public static void Dispose()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (pdhQuery != IntPtr.Zero)
        {
            PdhCloseQuery(pdhQuery);
            pdhQuery = IntPtr.Zero;
            pdhReady = false;
            pdhPrimed = false;
            HasGpuPercent = false;
        }
#endif
    }
    #endregion

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    #region CPU占用率
    private static TimeSpan lastProcessorTime;  //上次采样的进程处理器总时间
    private static float lastCpuWallTime;       //上次采样的墙钟时间
    private static bool cpuBaseReady;           //CPU 基线是否已建立

    /// <summary>
    /// 刷新 CPU 占用率：进程处理器时间差分 ÷（墙钟差 × 核心数）× 100
    /// </summary>
    /// <param name="now">当前墙钟时间（Time.realtimeSinceStartup）</param>
    private static void RefreshCpuPercent(float now)
    {
        TimeSpan processorTime = SDProcess.GetCurrentProcess().TotalProcessorTime;
        if (cpuBaseReady)
        {
            double wallDelta = now - lastCpuWallTime;
            if (wallDelta > 0)
            {
                CpuPercent = Math.Max(0, (processorTime - lastProcessorTime).TotalSeconds / (wallDelta * Environment.ProcessorCount) * 100.0);
                HasCpuPercent = true;
            }
        }
        lastProcessorTime = processorTime;
        lastCpuWallTime = now;
        cpuBaseReady = true;
    }
    #endregion

    #region GPU占用率（PDH）
    private const uint PDH_FMT_DOUBLE = 0x00000200; //格式化输出 double
    private const uint PDH_MORE_DATA = 0x800007D2;  //缓冲不足，需按返回大小扩容

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhOpenQuery(string szDataSource, IntPtr dwUserData, out IntPtr phQuery);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhAddEnglishCounter(IntPtr hQuery, string szFullCounterPath, IntPtr dwUserData, out IntPtr phCounter);
    [DllImport("pdh.dll")]
    private static extern uint PdhCollectQueryData(IntPtr hQuery);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhGetFormattedCounterArray(IntPtr hCounter, uint dwFormat, ref int lpcbBufferSize, out int lpdwItemCount, IntPtr ItemBuffer);
    [DllImport("pdh.dll")]
    private static extern uint PdhCloseQuery(IntPtr phQuery);

    [StructLayout(LayoutKind.Sequential)]
    private struct PDH_FMT_COUNTERVALUE_ITEM
    {
        public IntPtr szName;   //实例名（如 pid_1234_luid_..._engtype_3D）
        public uint CStatus;    //该实例本次数据状态（0=有效）
        public double doubleValue;
    }

    private static IntPtr pdhQuery = IntPtr.Zero;   //PDH 查询句柄
    private static IntPtr pdhCounter = IntPtr.Zero; //GPU Engine 计数器句柄
    private static bool pdhReady;                   //PDH 是否初始化成功（失败永久不重试）
    private static bool pdhPrimed;                  //是否已完成首次 Collect（利用率是差分数据，首次无效）
    private static byte[] pdhBuffer;                //实例数据缓冲（实例变多遇 PDH_MORE_DATA 时扩容）
    private static string pidPrefix;                //当前进程实例名前缀 "pid_{pid}_"

    /// <summary>
    /// 刷新 GPU 占用率：读取 GPU Engine 全部实例，筛选当前进程实例求和
    /// </summary>
    private static void RefreshGpuPercent()
    {
        if (!pdhReady)
            return;
        if (PdhCollectQueryData(pdhQuery) != 0)
            return;
        if (!pdhPrimed)
        {
            pdhPrimed = true; //首次 Collect 仅建立差分基线，数据下个刷新点才有效
            return;
        }
        //两遍调用：先取所需缓冲大小，再读数据（实例数量随进程生灭动态变化）
        int bufferSize = pdhBuffer?.Length ?? 0;
        int itemCount;
        uint result = PdhGetFormattedCounterArray(pdhCounter, PDH_FMT_DOUBLE, ref bufferSize, out itemCount, IntPtr.Zero);
        if (result == 0 && itemCount == 0)
        {
            GpuPercent = 0; //本进程当前没有任何 GPU 引擎实例
            HasGpuPercent = true;
            return;
        }
        if (result != PDH_MORE_DATA)
            return;
        if (pdhBuffer == null || pdhBuffer.Length < bufferSize)
            pdhBuffer = new byte[bufferSize];
        GCHandle handle = GCHandle.Alloc(pdhBuffer, GCHandleType.Pinned);
        try
        {
            result = PdhGetFormattedCounterArray(pdhCounter, PDH_FMT_DOUBLE, ref bufferSize, out itemCount, handle.AddrOfPinnedObject());
            if (result != 0)
                return;
            double sum = 0;
            int itemSize = Marshal.SizeOf<PDH_FMT_COUNTERVALUE_ITEM>();
            IntPtr baseAddr = handle.AddrOfPinnedObject();
            for (int i = 0; i < itemCount; i++)
            {
                PDH_FMT_COUNTERVALUE_ITEM item = Marshal.PtrToStructure<PDH_FMT_COUNTERVALUE_ITEM>(baseAddr + i * itemSize);
                if (item.CStatus != 0)
                    continue; //该实例本次数据无效，跳过
                string name = Marshal.PtrToStringUni(item.szName);
                if (name != null && name.StartsWith(pidPrefix, StringComparison.Ordinal))
                    sum += item.doubleValue;
            }
            GpuPercent = sum;
            HasGpuPercent = true;
        }
        finally
        {
            handle.Free();
        }
    }
    #endregion
#else
    /// <summary>
    /// 非 Windows 平台：CPU 占用率不可用（空实现，HasCpuPercent 恒 false）
    /// </summary>
    /// <param name="now">当前墙钟时间</param>
    private static void RefreshCpuPercent(float now) { }

    /// <summary>
    /// 非 Windows 平台：GPU 占用率不可用（空实现，HasGpuPercent 恒 false）
    /// </summary>
    private static void RefreshGpuPercent() { }
#endif
}
