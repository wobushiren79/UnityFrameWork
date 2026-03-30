using UnityEngine;
using UnityEditor;

public abstract class BaseMVCController<M,V> : BaseMVC
where M : BaseMVCModel, new()
{
    //模型
    private M mModel;
    //视图
    private V mView;

    public BaseMVCController(BaseMonoBehaviour content,V view)
    {
        SetContent(content);
        //添加相应模型
        mModel = new M();
        mModel.SetContent(mContent);
        //添加相应视图
        mView = view;
    }

    /// <summary>
    /// 设置模型
    /// </summary>
    /// <param name="model"></param>
    public void SetModel(M model)
    {
        this.mModel= model;
    }

    /// <summary>
    /// 获取模型
    /// </summary>
    /// <returns></returns>
    public M GetModel()
    {
        return mModel;
    }

    /// <summary>
    /// 设置视图
    /// </summary>
    /// <param name="mView"></param>
    public void SetView(V view)
    {
        this.mView = view;
    }

    /// <summary>
    /// 获取视图
    /// </summary>
    /// <returns></returns>
    public V GetView()
    {
        return mView;
    }

}