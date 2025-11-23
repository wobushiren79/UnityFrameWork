using UnityEngine;
using UnityEditor;
using DG.Tweening;
using UnityEngine.UI;
using System;
using TMPro;

public class DialogView : BaseUIView
{
    public DialogBean dialogData;

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

    public virtual void Start()
    {
        InitData();
    }

    public override void OnDestroy()
    {
        dialogData.actionDestoryBefore?.Invoke(this, dialogData);
        base.OnDestroy();
        UIHandler.Instance.manager.RemoveDialog(this);
        dialogData.actionDestoryAfter?.Invoke(dialogData);
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
            ui_Background.onClick.AddListener(BGOnClick);
        }
    }

    public virtual void SubmitOnClick()
    {
        dialogData.actionSubmit?.Invoke(this, dialogData);
        if (dialogData.isDestroySubmit)
            DestroyDialog();
    }
    public virtual void CancelOnClick()
    {
        dialogData.actionCancel?.Invoke(this, dialogData);
        if (dialogData.isDestroyCancel)
            DestroyDialog();
    }

    public virtual void BGOnClick()
    {
        dialogData.actionBG?.Invoke(this, dialogData);
        if (dialogData.isDestroyBG)
            DestroyDialog();
    }

    public virtual void DestroyDialog()
    {
        if (dialogData.timeDestroyDelay != 0)
        {
            transform.DOScale(new Vector3(1, 1, 1), dialogData.timeDestroyDelay).OnComplete(()=> 
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
}