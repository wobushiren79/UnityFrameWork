using System;

[Serializable]
public class ProgressBean
{
    public long id;

    public int proNumCurrent;
    public int proNumMax;

    public float progress;

    /// <summary>
    /// 增加进度
    /// </summary>
    public float AddProgress(float addPro)
    {
        progress += addPro;
        if (progress > 1)
            progress = 1;
        if (progress < 0)
            progress = 0;
        return progress;
    }

    /// <summary>
    /// 增加进度数值
    /// </summary>
    public float AddProgressNum(int addProNum)
    {
        proNumCurrent += addProNum;
        if(proNumCurrent > proNumMax)
        {
            proNumCurrent = proNumMax;
        }
        if (proNumCurrent < 0)
        {
            proNumCurrent = 0;
        }
        progress = proNumCurrent / (float)proNumMax;
        return progress;
    }
}