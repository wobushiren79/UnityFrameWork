using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using TMPro;

public class RadioButtonView : BaseMonoBehaviour
{

    //选中时
    public Sprite spSelected;
    public Color colorIVSelected;
    public Color colorTVSelected;

    //未选中时
    public Sprite spUnselected;
    public Color colorIVUnselected;
    public Color colorTVUnselected;

    public Button rbButton;
    public Image rbImage;
    public Text rbText;
    public TextMeshProUGUI rbText2;

    protected IRadioButtonCallBack callBackForSelect;
    protected Action<RadioButtonView, bool> actionForSelect;

    //是否选中
    public bool isSelect = true;

    private void Awake()
    {
        rbButton.onClick.AddListener(RadioButtonSelected);
    }

    /// <summary>
    /// 按钮选择触发
    /// </summary>
    public void RadioButtonSelected()
    {
        SetStates(!isSelect);
    }

    /// <summary>
    /// 设置回调
    /// </summary>
    /// <param name="callback"></param>
    public void SetCallBack(IRadioButtonCallBack callback)
    {
        this.callBackForSelect = callback;
    }

    /// <summary>
    /// 设置文本
    /// </summary>
    public void SetText(string showText)
    {
        if(rbText != null)
            rbText.text = showText;
        if (rbText2 != null)
            rbText2.text = showText;
    }

    /// <summary>
    /// 设置状态 有回调
    /// </summary>
    /// <param name="isSelect"></param>
    public void SetStates(bool isSelect)
    {
        ChangeStates(isSelect);
        callBackForSelect?.RadioButtonSelected(this, isSelect);
        actionForSelect?.Invoke(this, isSelect);
    }

    /// <summary>
    /// 改变状态 无回调
    /// </summary>
    /// <param name="isSelect"></param>
    public void ChangeStates(bool isSelect)
    {
        if (rbButton.enabled == false)
        {
            return;
        }
        this.isSelect = isSelect;
        switch (isSelect)
        {
            case true:
                if (rbImage) {
                    rbImage.sprite = spSelected;
                    rbImage.color = colorIVSelected;
                }
                if (rbText)
                    rbText.color = colorTVSelected;
                if (rbText2)
                    rbText2.color = colorTVSelected;
                break;
            case false:
                if (rbImage) {
                    rbImage.sprite = spUnselected;
                    rbImage.color = colorIVUnselected;
                }
                if (rbText)
                    rbText.color = colorTVUnselected;
                if (rbText2)
                    rbText2.color = colorTVUnselected;
                break;
        }
    }

    /// <summary>
    /// 设置是否开启点击
    /// </summary>
    /// <param name="enabled"></param>
    public void SetEnabled(bool enabled)
    {
        rbButton.enabled = enabled;
    }

    public bool GetEnabled()
    {
        return rbButton.enabled;
    }
}