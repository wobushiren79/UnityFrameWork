using UnityEngine;
using UnityEditor;
using DG.Tweening;
using UnityEngine.UI;
using System;

public class DialogView : BaseUIView
{
    public Button ui_Submit;
    public Text ui_SubmitText;

    public Button ui_Cancel;
    public Text ui_CancelText;

    public Button ui_Background;
    public Text ui_Title;
    public Text ui_Content;

    protected IDialogCallBack callBack;

    protected Action<DialogView, DialogBean> actionSubmit;
    protected Action<DialogView, DialogBean> actionCancel;

    public DialogBean dialogData;

    protected float timeDelayDelete;

    protected bool isSubmitDestroy = true;

    public virtual void Start()
    {
        InitData();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        UIHandler.Instance.manager.RemoveDialog(this);
    }

    public virtual void InitData()
    {
        if (ui_Submit != null)
        {
            ui_Submit.onClick.RemoveAllListeners();
            ui_Submit.onClick.AddListener(SubmitOnClick);
        }
        if (ui_Cancel != null)
        {
            ui_Cancel.onClick.RemoveAllListeners();
            ui_Cancel.onClick.AddListener(CancelOnClick);
        }
        if (ui_Background != null)
        {
            ui_Background.onClick.RemoveAllListeners();
            ui_Background.onClick.AddListener(CancelOnClick);
        }
    }


    public void SetSubmitDestroy(bool isSubmitDestroy)
    {
        this.isSubmitDestroy = isSubmitDestroy;
    }

    public virtual void SubmitOnClick()
    {
        callBack?.Submit(this, dialogData);
        actionSubmit?.Invoke(this, dialogData);
        if (isSubmitDestroy)
        {
            DestroyDialog();
        }
    }
    public virtual void CancelOnClick()
    {
        callBack?.Cancel(this, dialogData);
        actionCancel?.Invoke(this, dialogData);
        DestroyDialog();
    }

    public virtual void DestroyDialog()
    {
        if (timeDelayDelete != 0)
        {
            transform.DOScale(new Vector3(1, 1, 1), timeDelayDelete).OnComplete(()=> 
            { 
                if(gameObject != null)
                    Destroy(gameObject); 
            });
        }
        else
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    public void SetCallBack(IDialogCallBack callBack)
    {
        this.callBack = callBack;
    }

    public virtual void SetAction(Action<DialogView, DialogBean> actionSubmit, Action<DialogView, DialogBean> actionCancel)
    {
        this.actionSubmit += actionSubmit;
        this.actionCancel += actionCancel;
    }

    public virtual void SetData(DialogBean dialogData)
    {
        if (dialogData == null)
            return;
        this.dialogData = dialogData;

        if (dialogData.title != null)
        {
            SetTitle(dialogData.title);
        }
        if (dialogData.content != null)
        {
            SetContent(dialogData.content);
        }
        if (dialogData.submitStr != null)
        {
            SetSubmitStr(dialogData.submitStr);
        }
        if (dialogData.cancelStr != null)
        {
            SetCancelStr(dialogData.cancelStr);
        }
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    /// <param name="title"></param>
    public virtual void SetTitle(string title)
    {
        if (ui_Title != null)
        {
            ui_Title.text = title;
        }
    }

    /// <summary>
    /// 设置内容
    /// </summary>
    /// <param name="content"></param>
    public virtual void SetContent(string content)
    {
        if (ui_Content != null)
        {
            ui_Content.text = content;
        }
    }

    /// <summary>
    /// 设置提交按钮问题
    /// </summary>
    /// <param name="str"></param>
    public virtual void SetSubmitStr(string str)
    {
        if (ui_SubmitText != null)
        {
            ui_SubmitText.text = str;
        }
    }

    /// <summary>
    /// 设置取消按钮文字
    /// </summary>
    /// <param name="str"></param>
    public virtual void SetCancelStr(string str)
    {
        if (ui_CancelText != null)
        {
            ui_CancelText.text = str;
        }
    }

    /// <summary>
    /// 设置延迟删除
    /// </summary>
    /// <param name="delayTime"></param>
    public virtual void SetDelayDelete(float delayTime)
    {
        this.timeDelayDelete = delayTime;
    }

    public interface IDialogCallBack
    {
        void Submit(DialogView dialogView, DialogBean dialogBean);
        void Cancel(DialogView dialogView, DialogBean dialogBean);
    }
}