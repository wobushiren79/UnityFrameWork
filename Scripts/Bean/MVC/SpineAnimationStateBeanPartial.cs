using System;
using System.Collections.Generic;
public partial class SpineAnimationStateBean
{
}
public partial class SpineAnimationStateCfg
{
    /// <summary>
    /// 各状态候选动画名缓存（id=SpineAnimationStateEnum 值；候选名解析时已统一转小写，数组顺序=匹配优先级）
    /// </summary>
    public static Dictionary<long, string[]> dicSpineAnimData = null;

    #region 动画名候选与匹配
    /// <summary>
    /// 获取对应状态的候选动画名数组（返回的是小写候选名，仅供 CheckSpineAnim 做大小写不敏感匹配）
    /// </summary>
    public static string[] GetSpineAnimNames(SpineAnimationStateEnum spineAnimationState)
    {
        if (dicSpineAnimData == null)
        {
            dicSpineAnimData = new Dictionary<long, string[]>();
            var allData = GetAllData();
            foreach (var item in allData)
            {
                var itemData = item.Value;
                string[] arrayAnimTemp = itemData.res.Split(',');
                //候选名统一转小写缓存：匹配时与骨架动画名的小写形式比较，配置表无需再配大小写变体
                for (int i = 0; i < arrayAnimTemp.Length; i++)
                {
                    arrayAnimTemp[i] = arrayAnimTemp[i].ToLowerInvariant();
                }
                dicSpineAnimData.Add(itemData.id, arrayAnimTemp);
            }
        }
        if (dicSpineAnimData.TryGetValue((int)spineAnimationState, out string[] anims))
        {
            return anims;
        }
        return null;
    }

    /// <summary>
    /// 按状态在骨架实际动画名集合中找第一个命中的动画名（大小写不敏感；命中后返回骨架里的原始大小写名——Spine SetAnimation 需精确名）
    /// </summary>
    /// <param name="spineAnimationState">动画状态</param>
    /// <param name="allSpineAnimName">目标骨架实际包含的动画名集合（原始大小写）</param>
    /// <returns>命中的骨架实际动画名；无命中返回 null</returns>
    public static string CheckSpineAnim(SpineAnimationStateEnum spineAnimationState, HashSet<string> allSpineAnimName)
    {
        string[] arrayAnim = GetSpineAnimNames(spineAnimationState);
        if (arrayAnim == null || allSpineAnimName == null)
        {
            return null;
        }
        //骨架实际动画名按小写建索引但保留原名，同名不同大小写时取先出现者
        Dictionary<string, string> dicActualAnimName = new Dictionary<string, string>();
        foreach (var actualAnimName in allSpineAnimName)
        {
            string lowerAnimName = actualAnimName.ToLowerInvariant();
            if (!dicActualAnimName.ContainsKey(lowerAnimName))
            {
                dicActualAnimName.Add(lowerAnimName, actualAnimName);
            }
        }
        //按候选优先级顺序取第一个命中，返回骨架原始大小写名
        for (int i = 0; i < arrayAnim.Length; i++)
        {
            if (dicActualAnimName.TryGetValue(arrayAnim[i], out string targetAnimName))
            {
                return targetAnimName;
            }
        }
        return null;
    }
    #endregion
}
