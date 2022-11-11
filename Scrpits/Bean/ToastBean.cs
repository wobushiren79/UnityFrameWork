using UnityEditor;
using UnityEngine;

public class ToastBean
{
    public ToastEnum toastType;//类型
    public string content;//内容
    public Sprite toastIcon;//图标
    public float showTime;//显示时间

    public ToastBean(ToastEnum toastType, string content)
    {
        this.toastType = toastType;
        this.content = content;
        showTime = 3;
    }

    public ToastBean(ToastEnum toastType, string content, float showTime)
    {
        this.toastType = toastType;
        this.content = content;
        this.showTime = showTime;
    }

    public ToastBean(ToastEnum toastType, string content, Sprite toastIcon)
    {
        this.toastType = toastType;
        this.content = content;
        this.toastIcon = toastIcon;
        showTime = 3;
    }

    public ToastBean(ToastEnum toastType, string content, Sprite toastIcon, float showTime)
    {
        this.toastType = toastType;
        this.content = content;
        this.toastIcon = toastIcon;
        this.showTime = showTime;
    }
}