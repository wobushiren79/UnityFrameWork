using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class ToastView : BaseUIView
{
    public Image ui_Icon;
    public Text ui_Content;
    public TextMeshProUGUI ui_ContentPro;
    public CanvasGroup cgToast;

    public override void Awake()
    {
        base.Awake();
        cgToast = GetComponent<CanvasGroup>();
    }

    public virtual void AnimForShow()
    {
        if (cgToast != null)
            cgToast.DOFade(0, 0.2f).From();
        gameObject.transform.DOScale(Vector3.zero, 0.2f).From().SetEase(Ease.OutBack);
    }

    public void SetData(ToastBean toastData)
    {
        //设置Icon
        SetIcon(toastData.toastIcon, toastData.toastIconColor);
        //设置内容
        SetContent(toastData.content);
        //定时销毁
        DestroyToast(toastData.showTime);
        if (ui_Content != null)
        {
            UGUIUtil.RefreshUISize(ui_Content.rectTransform);
        }
        if (ui_ContentPro != null)
        {
            UGUIUtil.RefreshUISize(ui_ContentPro.rectTransform);
        }
        UGUIUtil.RefreshUISize((RectTransform)cgToast.transform);

        AnimForShow();
    }

    /// <summary>
    /// 设置图标
    /// </summary>
    /// <param name="spIcon"></param>
    public void SetIcon(Sprite spIcon, Color spIconColor)
    {
        if (ui_Icon != null && spIcon != null)
        {
            ui_Icon.sprite = spIcon;
            ui_Icon.color = spIconColor;
        }
    }

    /// <summary>
    /// 设置内容
    /// </summary>
    /// <param name="content"></param>
    public void SetContent(string content)
    {
        if (ui_Content != null)
        {
            ui_Content.text = content;
        }
        if (ui_ContentPro!=null)
        {
            ui_ContentPro.text = content;
        }
    }

    /// <summary>
    /// 摧毁Toast
    /// </summary>
    /// <param name="timeDelay"></param>
    private void DestroyToast(float timeDelay)
    {
        if (cgToast != null)
            cgToast.DOFade(0, 0.2f).SetDelay(timeDelay);
        this.WaitExecuteSeconds(timeDelay + 0.2f, () =>
        {
             //延迟删除
             Destroy(gameObject);
        });
    }

}
