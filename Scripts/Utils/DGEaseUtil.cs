

using UnityEngine;

public class DGEaseUtil
{

    /// <summary>
    /// 重力影响的Ease
    /// </summary>
    public float EaseGravity(float time, float duration, float overshootOrAmplitude, float period)
    {
        float t = time / duration;
        float gravity = Physics.gravity.y;
        // 模拟重力影响，使用二次函数来定义动画曲线
        float y = -0.5f * gravity * t * t + t;
        return y;
    }

}
