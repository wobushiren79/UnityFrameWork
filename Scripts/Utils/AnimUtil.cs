using UnityEngine;

/// <summary>
/// 动画工具类(框架层)：与具体游戏逻辑无关的通用 Animator/动画工具方法
/// </summary>
public static partial class AnimUtil
{
    #region 动画片段

    /// <summary>
    /// 读取 Animator 中名字包含指定关键字的动画片段时长
    /// </summary>
    /// <param name="animator">目标 Animator</param>
    /// <param name="clipNameKeyword">动画片段名关键字(片段名通常由"控制器名_状态名"组成 故一般传状态名即可命中)</param>
    /// <param name="defaultLength">未找到对应片段时返回的兜底时长</param>
    /// <returns>匹配到的动画片段时长 找不到则返回 defaultLength</returns>
    public static float GetAnimClipLength(Animator animator, string clipNameKeyword, float defaultLength = 0)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].name.Contains(clipNameKeyword))
                {
                    return clips[i].length;
                }
            }
        }
        return defaultLength;
    }

    #endregion
}
