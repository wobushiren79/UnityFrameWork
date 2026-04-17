public interface IModIdMapView
{
    /// <summary>
    /// 获取ModID映射成功
    /// </summary>
    void GetModIdMapSuccess(ModIdMapBean bean);

    /// <summary>
    /// 获取ModID映射失败
    /// </summary>
    void GetModIdMapFail();

    /// <summary>
    /// 设置ModID映射成功
    /// </summary>
    void SetModIdMapSuccess(ModIdMapBean bean);

    /// <summary>
    /// 设置ModID映射失败
    /// </summary>
    void SetModIdMapFail();
}
