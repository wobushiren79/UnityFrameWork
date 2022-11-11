using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSHandler : BaseHandler<FPSHandler, BaseManager>
{
    private float m_LastUpdateShowTime = 0f;    //上一次更新帧率的时间;

    private float m_UpdateShowDeltaTime = 0.01f;//更新帧率的时间间隔;

    private int m_FrameUpdate = 0;//帧数;

    private float m_FPS = 0;

    protected void Start()
    {
        m_LastUpdateShowTime = Time.realtimeSinceStartup;
    }

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

    void Update()
    {
        m_FrameUpdate++;
        if (Time.realtimeSinceStartup - m_LastUpdateShowTime >= m_UpdateShowDeltaTime)
        {
            m_FPS = m_FrameUpdate / (Time.realtimeSinceStartup - m_LastUpdateShowTime);
            m_FrameUpdate = 0;
            m_LastUpdateShowTime = Time.realtimeSinceStartup;
        }
    }

    private int FPSShow;
    private float timeFPSShow;
    void OnGUI()
    {
        GameConfigBean gameConfig =  GameDataHandler.Instance.manager.GetGameConfig();
        if (gameConfig.framesShow)
        {
            timeFPSShow += Time.deltaTime;
            if (timeFPSShow >= 1)
            {
                FPSShow = Mathf.FloorToInt(m_FPS);
                timeFPSShow = 0;
            }
            GUI.Label(new Rect(Screen.width - 100, 0, 100, 100), $"FPS: {FPSShow}");
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
}
