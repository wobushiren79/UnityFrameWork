using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ToastView : BaseMonoBehaviour
{
    public Image ivIcon;
    public Text tvContent;
    public CanvasGroup cgToast;

    public void Awake()
    {
        AutoLinkUI();
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
        SetIcon(toastData.toastIcon);
        //设置内容
        SetContent(toastData.content);
        //定时销毁
        DestroyToast(toastData.showTime);

        UGUIUtil.RefreshUISize(tvContent.rectTransform);
        UGUIUtil.RefreshUISize((RectTransform)cgToast.transform);

        AnimForShow();
    }

    /// <summary>
    /// 设置图标
    /// </summary>
    /// <param name="spIcon"></param>
    public void SetIcon(Sprite spIcon)
    {
        if (ivIcon != null && spIcon != null)
        {
            ivIcon.sprite = spIcon;
        }
    }

    /// <summary>
    /// 设置内容
    /// </summary>
    /// <param name="content"></param>
    public void SetContent(string content)
    {
        if (tvContent != null)
        {
            tvContent.text = content;
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
