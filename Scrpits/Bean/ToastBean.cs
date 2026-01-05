using UnityEditor;
using UnityEngine;

public class ToastBean
{
    public ToastEnum toastType;//类型
    public string content;//内容
    public Sprite toastIcon;//图标
    public Color toastIconColor;//图标颜色
    public float showTime;//显示时间

    public ToastBean(ToastEnum toastType, string content): this(toastType, content, null, Color.white, 3)
    {
        
    }

    public ToastBean(ToastEnum toastType, string content, float showTime): this(toastType, content, null, Color.white, showTime)
    {
        
    }

    public ToastBean(ToastEnum toastType, string content, Sprite toastIcon) : this(toastType, content, toastIcon, Color.white, 3)
    {

    }

    public ToastBean(ToastEnum toastType, string content, Sprite toastIcon, Color toastIconColor) : this(toastType, content, toastIcon, toastIconColor, 3)
    {

    }

    public ToastBean(ToastEnum toastType, string content, Sprite toastIcon, float showTime) : this(toastType, content, toastIcon, Color.white, showTime)
    {

    }

    public ToastBean(ToastEnum toastType, string content, Sprite toastIcon, Color toastIconColor, float showTime)
    {
        this.toastType = toastType;
        this.content = content;
        this.toastIcon = toastIcon;
        this.showTime = showTime;
        this.toastIconColor = toastIconColor;
    }
}