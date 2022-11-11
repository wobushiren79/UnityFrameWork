using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class ColorExtension
{
    public static Color SetColor(this Color self, float r = -1, float g = -1, float b = -1, float a = -1)
    {
        r = (r < 0 ? self.r : r);
        g = (g < 0 ? self.g : g);
        b = (b < 0 ? self.b : b);
        a = (a < 0 ? self.a : a);
        return new Color(r, g, b, a);
    }


    public static void SetColor(this Graphic self, float r = -1, float g = -1, float b = -1, float a = -1)
    {
        r = (r < 0 ? self.color.r : r);
        g = (g < 0 ? self.color.g : g);
        b = (b < 0 ? self.color.b : b);
        a = (a < 0 ? self.color.a : a);
        self.color = new Color(r, g, b, a);
    }

    public static void SetColorR(this Graphic self, float r)
    {
        self.color = new Color(r, -1, -1, -1);
    }

    public static void SetColorG(this Graphic self, float g)
    {
        self.color = new Color(-1, g, -1, -1);
    }

    public static void SetColorB(this Graphic self, float b)
    {
        self.color = new Color(-1, -1, b, -1);
    }

    public static void SetColorA(this Graphic self, float a)
    {
        self.color = new Color(-1, -1, -1, a);
    }

    
}