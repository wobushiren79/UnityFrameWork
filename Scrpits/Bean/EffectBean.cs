using UnityEditor;
using UnityEngine;

public class EffectBean
{
    //粒子名字
    public string effectName;
    //粒子类型
    public EffectTypeEnum effectType = EffectTypeEnum.Normal;
    //粒子展示时间
    public float timeForShow;
    //粒子的世界坐标
    public Vector3 effectPosition;
    //是否展示时播放
    public bool isPlayInShow = true;
}