using UnityEngine;
using UnityEditor;

public abstract class BaseMVC 
{
    //上下文对象
    protected BaseMonoBehaviour mContent;

    /// <summary>
    /// 初始化数据
    /// </summary>
    public abstract void InitData();

    /// <summary>
    /// 设置上下文对象
    /// </summary>
    /// <param name="content"></param>
    public void SetContent(BaseMonoBehaviour content)
    {
        this.mContent = content;
        InitData();
    }

    /// <summary>
    /// 获取上下文对象
    /// </summary>
    /// <returns></returns>
    public BaseMonoBehaviour GetContent()
    {
        return mContent;
    }
}