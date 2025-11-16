using UnityEngine;
using UnityEditor;
using System;

[Serializable]
public class DialogBean
{
    //弹窗类型
    public DialogEnum dialogType;
    //弹窗按钮事件
    public Action<DialogView, DialogBean> actionSubmit;
    //弹窗按钮事件
    public Action<DialogView, DialogBean> actionCancel;
    //弹窗关闭事件-之前
    public Action<DialogView, DialogBean> actionDestoryBefore;
    //弹窗关闭事件-之后
    public Action<DialogBean> actionDestoryAfter;
    //弹窗回调
    public DialogView.IDialogCallBack callBack;

    public string title;
    public string content;
    public string submitStr;
    public string cancelStr;
    //弹窗编号
    public int dialogPosition;
    //备注
    public string remark;
    //延迟删除
    public float delayDelete;
}