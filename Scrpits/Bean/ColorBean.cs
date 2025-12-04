using UnityEngine;
using UnityEditor;
using System;

[Serializable]
public class ColorBean
{
    public float r = 1;
    public float g = 1;
    public float b = 1;
    public float a = 1;

    public ColorBean()
    {

    }

    public ColorBean(float r, float g, float b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = 1;
    }

    public ColorBean(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public ColorBean(string colorStr)
    {
        float[] colors = colorStr.SplitForArrayFloat(',');
        if (colors == null)
            return;
        else if (colors.Length == 1)
        {
            this.r = colors[0];
        }
        else if (colors.Length == 2)
        {
            this.r = colors[0];
            this.g = colors[1];
        }
        else if (colors.Length == 3)
        {
            this.r = colors[0];
            this.g = colors[1];
            this.b = colors[2];
        }
        else if (colors.Length == 4)
        {
            this.r = colors[0];
            this.g = colors[1];
            this.b = colors[2];
            this.a = colors[3];
        }
    }

    public ColorBean(Color color) : this(color.r, color.g, color.b, color.a)
    {
    }
    public void SetColor(Color targetColor)
    {
        this.r = targetColor.r;
        this.g = targetColor.g;
        this.b = targetColor.b;
        this.a = targetColor.a;
    }

    public Color GetColor()
    {
        return new Color(r, g, b, a);
    }
    public static ColorBean White()
    {
        return new ColorBean(1, 1, 1, 1);
    }
    public static ColorBean Black()
    {
        return new ColorBean(0, 0, 0, 1);
    }
    public static ColorBean Random()
    {
        return new ColorBean(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1);
    }
}