using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 单选按钮组
/// 管理一组 RadioButtonView，保证同一时刻只有一个按钮处于选中状态，并向外派发选中/取消回调
/// </summary>
public class RadioGroupView : BaseMonoBehaviour, IRadioButtonCallBack
{
    #region 字段

    /// <summary>
    /// 是否允许取消选择(为 true 时点击已选中按钮可取消选中，组内可同时无选中项)
    /// </summary>
    public bool isCancelSelect = false;

    /// <summary>
    /// 组内的单选按钮列表
    /// </summary>
    public List<RadioButtonView> listButton;

    /// <summary>
    /// 按钮组选中状态变化的外部回调
    /// </summary>
    private IRadioGroupCallBack mRGCallBack;

    #endregion

    #region 生命周期

    /// <summary>
    /// 初始化时为列表内每个按钮注册本组为回调对象
    /// </summary>
    private void Start()
    {
        if (listButton.IsNull())
        {
            return;
        }
        for (int i = 0; i < listButton.Count; i++)
        {
            RadioButtonView itemRB = listButton[i];
            if (itemRB == null)
            {
                LogUtil.LogError($"RadioGroupView[{gameObject.name}] Start: listButton 第 {i} 个元素为空，请检查预制体上 RadioGroupView 组件的 List Button 是否存在未赋值的空槽位");
                continue;
            }
            itemRB.SetCallBack(this);
        }
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 向按钮组动态添加一个单选按钮，并为其注册本组为回调对象
    /// </summary>
    /// <param name="targetRB">要加入的单选按钮</param>
    public void AddRadioButton(RadioButtonView targetRB)
    {
        if (listButton == null)
        {
            listButton = new List<RadioButtonView>();
        }
        listButton.Add(targetRB);
        targetRB.SetCallBack(this);
    }

    /// <summary>
    /// 设置选中指定下标的按钮，其余按钮置为未选中
    /// </summary>
    /// <param name="position">要选中的按钮下标</param>
    /// <param name="isCallBack">选中后是否触发外部选中回调</param>
    public void SetPosition(int position, bool isCallBack)
    {
        if (listButton == null)
            return;
        if (position > listButton.Count)
            return;
        for (int i = 0; i < listButton.Count; i++)
        {
            RadioButtonView itemRB = listButton[i];
            if (itemRB == null)
            {
                LogUtil.LogError($"RadioGroupView[{gameObject.name}] SetPosition: listButton 第 {i} 个元素为空，请检查预制体上的 List Button 空槽位");
                continue;
            }
            if (i == position)
            {
                itemRB.ChangeStates(true);
                if (isCallBack)
                {
                    if (mRGCallBack != null)
                        mRGCallBack.RadioButtonSelected(this, i, itemRB);
                }
            }
            else
            {
                itemRB.ChangeStates(false);
            }
        }
    }

    /// <summary>
    /// 自动从子物体收集所有 RadioButtonView 填充按钮列表，并为其注册本组为回调对象
    /// </summary>
    public void InitRadioButton()
    {
        if (listButton == null)
            listButton = new List<RadioButtonView>();
        listButton.Clear();
        RadioButtonView[] rbList = GetComponentsInChildren<RadioButtonView>();
        if (rbList != null)
            listButton = TypeConversionUtil.ArrayToList(rbList);
        if (listButton != null)
        {
            for (int i = 0; i < listButton.Count; i++)
            {
                RadioButtonView itemRB = listButton[i];
                if (itemRB == null)
                {
                    LogUtil.LogError($"RadioGroupView[{gameObject.name}] InitRadioButton: 收集到的第 {i} 个 RadioButtonView 为空");
                    continue;
                }
                itemRB.SetCallBack(this);
            }
        }
    }

    /// <summary>
    /// 设置按钮组选中状态变化的外部回调
    /// </summary>
    /// <param name="callback">外部回调对象</param>
    public void SetCallBack(IRadioGroupCallBack callback)
    {
        this.mRGCallBack = callback;
    }

    #endregion

    #region IRadioButtonCallBack 回调

    /// <summary>
    /// 单个按钮被点击时的回调：将被点击按钮置为选中、其余置为未选中，并向外派发选中/取消回调
    /// </summary>
    /// <param name="view">被点击的按钮</param>
    /// <param name="isSelect">被点击按钮的目标选中状态</param>
    public void RadioButtonSelected(RadioButtonView view, bool isSelect)
    {
        if (listButton.IsNull())
        {
            return;
        }
        for (int i = 0; i < listButton.Count; i++)
        {
            RadioButtonView itemRB = listButton[i];
            if (itemRB == null)
            {
                LogUtil.LogError($"RadioGroupView[{gameObject.name}] RadioButtonSelected: listButton 第 {i} 个元素为空，请检查预制体上的 List Button 空槽位");
                continue;
            }
            if (itemRB.Equals(view))
            {
                if (!isCancelSelect)
                {
                    itemRB.ChangeStates(true);
                }
                if (mRGCallBack != null)
                    mRGCallBack.RadioButtonSelected(this, i, itemRB);
            }
            else
            {
                itemRB.ChangeStates(false);
                if (mRGCallBack != null)
                    mRGCallBack.RadioButtonUnSelected(this, i, itemRB);
            }
        }
    }

    #endregion
}
