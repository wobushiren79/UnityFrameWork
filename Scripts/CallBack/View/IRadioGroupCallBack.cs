using UnityEngine;
using UnityEditor;

public interface IRadioGroupCallBack 
{
    /// <summary>
    /// 按钮选择
    /// </summary>
    /// <param name="position"></param>
    /// <param name="view"></param>
    void RadioButtonSelected(RadioGroupView rgView, int position,RadioButtonView rbview);

    /// <summary>
    /// 按钮未选择
    /// </summary>
    /// <param name="position"></param>
    /// <param name="view"></param>
    void RadioButtonUnSelected(RadioGroupView rgView, int position, RadioButtonView rbview);
}