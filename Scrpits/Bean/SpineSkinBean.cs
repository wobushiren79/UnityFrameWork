using System;
using UnityEngine;

[Serializable]
public class SpineSkinBean
{
    public long skinId;
    public bool hasColor;
    public Color skinColor;

    public SpineSkinBean(long skinId)
    {
        this.skinId = skinId;
        this.hasColor = false;
        this.skinColor = Color.white;
    }
}