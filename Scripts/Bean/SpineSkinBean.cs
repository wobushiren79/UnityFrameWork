using System;
using UnityEngine;

[Serializable]
public class SpineSkinBean
{
    public long skinId;
    public bool hasColor;
    public ColorBean skinColor;

    public SpineSkinBean()
    {

    }


    public SpineSkinBean(long skinId) : this(skinId, false, Color.white)
    {

    }


    public SpineSkinBean(long skinId, bool hasColor, Color color)
    {
        this.skinId = skinId;
        this.hasColor = hasColor;
        this.skinColor = new ColorBean(color);
    }
}