using UnityEngine;
using UnityEditor;
using DG.Tweening;
using UnityEngine.UI;
using System;
using TMPro;

public class DialogView : BaseUIView
{
    public Button ui_Submit;
    public Text ui_SubmitText;
    public TextMeshProUGUI ui_SubmitTextPro;

    public Button ui_Cancel;
    public Text ui_CancelText;
    public TextMeshProUGUI ui_CancelTextPro;

    public Button ui_Background;
    public Text ui_Title;
    public Text ui_Content;
    public TextMeshProUGUI ui_TitlePro;
    public TextMeshProUGUI ui_ContentPro;
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
        SetTitle(dialogData.title);
        SetContent(dialogData.content);
        SetSubmitStr(dialogData.submitStr);
        SetCancelStr(dialogData.cancelStr);
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    /// <param name="title"></param>
    public virtual void SetTitle(string title)
    {
        if (title.IsNull())
        {
            return;
        }
        if (ui_Title != null)
        {
            ui_Title.text = title;
        }
        if (ui_TitlePro != null)
        {
            ui_TitlePro.text = title;
        }
    }

    /// <summary>
    /// 设置内容
    /// </summary>
    /// <param name="content"></param>
    public virtual void SetContent(string content)
    {        
        if (content.IsNull())
        {
            return;
        }
        if (ui_Content != null)
        {
            ui_Content.text = content;
        }
        if (ui_ContentPro != null)
        {
            ui_ContentPro.text = content;
        }
    }

    /// <summary>
    /// 设置提交按钮问题
    /// </summary>
    /// <param name="str"></param>
    public virtual void SetSubmitStr(string str)
    {
        if (str.IsNull())
        {
           return;
        }
        if (ui_SubmitText != null)
        {
            ui_SubmitText.text = str;
        }
        if (ui_SubmitTextPro != null)
        {
            ui_SubmitTextPro.text = str;
        }
    }

    /// <summary>
    /// 设置取消按钮文字
    /// </summary>
    /// <param name="str"></param>
    public virtual void SetCancelStr(string str)
    {        
        if (str.IsNull())
        {
           return;
        }
        if (ui_CancelText != null)
        {
            ui_CancelText.text = str;
        }
        if (ui_CancelTextPro != null)
        {
            ui_CancelTextPro.text = str;
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