using System;
using System.Collections.Generic;
public partial class SpineAnimationStateBean
{
}
public partial class SpineAnimationStateCfg
{
    public static Dictionary<long, string[]> dicSpineAnimData = null;

    /// <summary>
    /// 获取对应枚举的的动画
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
                string[] arrrayAnimTemp = itemData.res.Split(',');
                dicSpineAnimData.Add(itemData.id, arrrayAnimTemp);
            }
        }
        if (dicSpineAnimData.TryGetValue((int)spineAnimationState,out string[] anims))
        {
            return anims;
        }
        return null;
    }


    /// <summary>
    /// 检测
    /// </summary>
    public static string CheckSpineAnim(SpineAnimationStateEnum spineAnimationState, HashSet<string> allSpineAnimName)
    {
        string[] arrayAnim = GetSpineAnimNames(spineAnimationState);
        for (int i = 0; i < arrayAnim.Length; i++)
        {
            var itemAnimName = arrayAnim[i];
            if (allSpineAnimName.Contains(itemAnimName))
            {
                return itemAnimName;
            }
        }
        return null;
    }
}
