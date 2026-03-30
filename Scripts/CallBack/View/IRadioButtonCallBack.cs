using UnityEngine;
using UnityEditor;

public interface IRadioButtonCallBack
{

    /// <summary>
    /// 单选按钮点击
    /// </summary>
    /// <param name="view"></param>
    /// <param name="isSelect"></param>
    void RadioButtonSelected(RadioButtonView view, bool isSelect);
}