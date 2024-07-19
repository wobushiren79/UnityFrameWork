

using UnityEngine;

public class DGEaseUtil
{

    /// <summary>
    /// ����Ӱ���Ease
    /// </summary>
    public float EaseGravity(float time, float duration, float overshootOrAmplitude, float period)
    {
        float t = time / duration;
        float gravity = Physics.gravity.y;
        // ģ������Ӱ�죬ʹ�ö��κ��������嶯������
        float y = -0.5f * gravity * t * t + t;
        return y;
    }

}
