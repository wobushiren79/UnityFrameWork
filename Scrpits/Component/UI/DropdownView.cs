using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

public class DropdownView : BaseMonoBehaviour
{
    public Dropdown dropdown;
    protected ICallBack callBack;
    public List<Dropdown.OptionData> listData;

    private void Start()
    {
        dropdown.onValueChanged.AddListener(OnDropDownChange);
    }


    public void SetData(List<Dropdown.OptionData> listData)
    {
        this.listData = listData;
        dropdown.ClearOptions();
        for (int i = 0; i < listData.Count; i++)
        {
            Dropdown.OptionData data = listData[i];
            dropdown.options.Add(data);
        }
    }

    public void SetPosition(string name)
    {
        for (int i = 0; i < listData.Count; i++)
        {
            Dropdown.OptionData data = listData[i];
            if (data.text.Equals(name))
            {
                dropdown.value = i;
                return;
            }
        }
    }

    public void SetPosition(int position)
    {
        dropdown.value = position;
    }

    public void SetCallBack(ICallBack callBack)
    {
        this.callBack = callBack;
    }

    /// <summary>
    /// 监听
    /// </summary>
    /// <param name="position"></param>
    public void OnDropDownChange(int position)
    {
        if (callBack != null)
        {
            callBack.OnDropDownValueChange(this, position ,listData[position]);
        }
    }

    /// <summary>
    /// 回调
    /// </summary>
    public interface ICallBack
    {
        void OnDropDownValueChange(DropdownView view,int position, Dropdown.OptionData optionData);
    }
}