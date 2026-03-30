using UnityEngine;
using UnityEditor;

public interface ILoadCallBack <T>
{
    /// <summary>
    /// 加载成功
    /// </summary>
    /// <param name="data"></param>
    void LoadSuccess(T data);

    /// <summary>
    /// 加载失败
    /// </summary>
    /// <param name="msg"></param>
    void LoadFail(string msg);
}