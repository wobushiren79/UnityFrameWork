using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

public class SelectView : BaseUIView
{
    public Button selectLeft;
    public Button selectRight;

    public Text tvTitle;
    public Text tvContent;

    private int currentIndex = 0;
    private ICallBack callBack;

    private List<string> listData = new List<string>();

    private void Start()
    {
        if (selectLeft != null)
            selectLeft.onClick.AddListener(LeftSelect);
        if (selectRight != null)
            selectRight.onClick.AddListener(RightSelect);
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    /// <param name="title"></param>
    public void SetTitle(string title)
    {
        tvTitle.text = title;
    }

    /// <summary>
    /// 设置内容
    /// </summary>
    /// <param name="content"></param>
    public void SetContent(string content)
    {
        tvContent.text = content;
    }

    /// <summary>
    /// 设置回掉
    /// </summary>
    /// <param name="callBack"></param>
    public void SetCallBack(ICallBack callBack)
    {
        this.callBack = callBack;
    }

    /// <summary>
    /// 左选
    /// </summary>
    public void LeftSelect()
    {
        SetPosition(currentIndex - 1);
    }

    /// <summary>
    /// 右选
    /// </summary>
    public void RightSelect()
    {
        SetPosition(currentIndex + 1);
    }

    /// <summary>
    /// 设置列表数据
    /// </summary>
    /// <param name="listData"></param>
    public void SetListData(List<string> listData)
    {
        this.listData = listData;
    }

    /// <summary>
    /// 随机数据
    /// </summary>
    public void RandomSelect()
    {
        int randomPosition = Random.Range(0, listData.Count);
        SetPosition(randomPosition);
    }

    /// <summary>
    /// 设置序号
    /// </summary>
    /// <param name="position"></param>
    public void SetPosition(int position)
    {
        if (listData.IsNull())
            return;
        this.currentIndex = position;
        if (currentIndex < 0)
        {
            currentIndex = listData.Count - 1;
        }
        else if (currentIndex > listData.Count - 1)
        {
            currentIndex = 0;
        }
        SetContent(listData[currentIndex]);
        callBack?.ChangeSelectPosition(this, currentIndex);
    }

    /// <summary>
    /// 获取选取的数据
    /// </summary>
    /// <returns></returns>
    public int GetSelectPosition()
    {
        return currentIndex;
    }

    public interface ICallBack
    {
        void ChangeSelectPosition(SelectView selectView, int position);
    }
}