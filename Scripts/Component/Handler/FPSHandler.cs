using UnityEngine;

public partial class FPSHandler : BaseHandler<FPSHandler, BaseManager>
{
    #region 帧率计算
    private float m_LastUpdateShowTime = 0f;    //上一次更新帧率的时间;
    private float m_UpdateShowDeltaTime = 0.01f;//更新帧率的时间间隔;
    private int m_FrameUpdate = 0;//帧数;
    private float m_FPS = 0;
    #endregion

    #region 性能浮层
    private int FPSShow;                        //当前显示的FPS
    private float timeFPSShow;                  //FPS显示刷新计时（1秒）
    private bool m_IsShowDebug;                 //是否开发包/编辑器（Debug.isDebugBuild，常量性质只算一次）
    private GameConfigBean m_GameConfig;        //游戏配置缓存（同一引用，设置改动即时生效）
    private string m_ShowText = "";             //显示文本（0.5秒重建一次，绘制零字符串分配）
    private GUIContent m_Content;               //复用的绘制内容
    private GUIStyle m_Style;                   //文本样式（仅 OnGUI 内懒创建，避免非 OnGUI 上下文触碰 GUI 系统）
    private float m_TextTimer;                  //性能文本刷新计时（0.5秒）
    #endregion

    #region 生命周期
    protected void Start()
    {
        m_LastUpdateShowTime = Time.realtimeSinceStartup;
        m_IsShowDebug = Debug.isDebugBuild;
        m_GameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        PerformanceUtil.Init();
        m_ShowText = BuildShowText(); //先构建一次，避免开局 0.5 秒空黑条
    }

    void Update()
    {
        m_FrameUpdate++;
        if (Time.realtimeSinceStartup - m_LastUpdateShowTime >= m_UpdateShowDeltaTime)
        {
            m_FPS = m_FrameUpdate / (Time.realtimeSinceStartup - m_LastUpdateShowTime);
            m_FrameUpdate = 0;
            m_LastUpdateShowTime = Time.realtimeSinceStartup;
        }
        //浮层隐藏时不做性能采样与文本刷新
        if (!IsShow())
            return;
        PerformanceUtil.CaptureFrame();
        //FPS 计时从 OnGUI 移入 Update（修正原双倍累加：OnGUI 每帧被调 2 次）
        timeFPSShow += Time.unscaledDeltaTime;
        if (timeFPSShow >= 1f)
        {
            FPSShow = Mathf.FloorToInt(m_FPS);
            timeFPSShow = 0;
        }
        m_TextTimer += Time.unscaledDeltaTime;
        if (m_TextTimer >= 0.5f)
        {
            m_TextTimer = 0;
            if (PerformanceUtil.Refresh())
                m_ShowText = BuildShowText();
        }
    }

    void OnDestroy()
    {
        PerformanceUtil.Dispose();
    }

    void OnGUI()
    {
        if (!IsShow())
            return;
        if (m_Style == null)
        {
            m_Style = new GUIStyle { fontSize = 14, alignment = TextAnchor.MiddleRight };
            m_Style.normal.textColor = Color.white;
            m_Content = new GUIContent();
        }
        m_Content.text = m_ShowText;
        float width = m_Style.CalcSize(m_Content).x + 12; //宽度按文本自适应，分辨率变化即时右对齐
        Rect rect = new Rect(Screen.width - width - 8, 4, width, 22);
        Color oldColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f); //半透明黑底提升可读性
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = oldColor;
        GUI.Label(rect, m_Content, m_Style);
    }
    #endregion

    #region 帧率设置
    /// <summary>
    /// 设置锁帧
    /// </summary>
    /// <param name="isLock">是否锁帧</param>
    /// <param name="fps">目标帧率</param>
    public void SetData(bool isLock, int fps)
    {
        if (isLock)
        {
            Application.targetFrameRate = fps;
        }
        else
        {
            Application.targetFrameRate = -1;
        }
    }

    /// <summary>
    /// 设置锁帧（配置表整型入口）
    /// </summary>
    /// <param name="isLock">是否锁帧（1=锁）</param>
    /// <param name="fps">目标帧率</param>
    public void SetData(int isLock, int fps)
    {
        if (isLock == 1)
        {
            Application.targetFrameRate = fps;
        }
        else
        {
            Application.targetFrameRate = -1;
        }
    }

    /// <summary>
    /// 设置垂直同步
    /// 使用“不同步”(0) 不等待 VSync。值必须为 0、1、2、3 或 4。
    /// 如果此设置设置为“不同步”(0) 以外的值，则Application.targetFrameRate的值将被忽略。
    /// </summary>
    /// <param name="SyncCount"></param>
    public void SetSyncCount(int SyncCount)
    {
        QualitySettings.vSyncCount = SyncCount;
    }
    #endregion

    #region 性能浮层逻辑
    /// <summary>
    /// 是否显示性能浮层（Development Build/编辑器默认显示；正式包看 framesShow 配置）
    /// </summary>
    private bool IsShow()
    {
        if (m_IsShowDebug)
            return true;
        if (m_GameConfig == null)
            m_GameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        return m_GameConfig != null && m_GameConfig.framesShow;
    }

    /// <summary>
    /// 构建显示文本（0.5秒一次；无效指标显示 -，如编辑器下 GPU 帧耗时）
    /// </summary>
    private string BuildShowText()
    {
        string cpuPercent = PerformanceUtil.HasCpuPercent ? $"{PerformanceUtil.CpuPercent:F0}%" : "-";
        string gpuFrame = PerformanceUtil.HasGpuFrameMs ? $"{PerformanceUtil.GpuFrameMs:F1}ms" : "-";
        string gpuPercent = PerformanceUtil.HasGpuPercent ? $"{PerformanceUtil.GpuPercent:F0}%" : "-";
        return $"FPS: {FPSShow} | CPU: {PerformanceUtil.CpuFrameMs:F1}ms {cpuPercent} | GPU: {gpuFrame} {gpuPercent} | MEM: {PerformanceUtil.MemMB}MB";
    }
    #endregion
}
