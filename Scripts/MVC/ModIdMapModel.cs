public class ModIdMapModel : BaseMVCModel
{
    protected ModIdMapService serviceModIdMap;

    public override void InitData()
    {
        serviceModIdMap = new ModIdMapService();
    }

    /// <summary>
    /// 获取ModID映射数据
    /// </summary>
    public ModIdMapBean GetModIdMapData()
    {
        ModIdMapBean bean = serviceModIdMap.QueryData();
        if (bean == null)
            bean = new ModIdMapBean();
        return bean;
    }

    /// <summary>
    /// 保存ModID映射数据
    /// </summary>
    public void SetModIdMapData(ModIdMapBean data)
    {
        serviceModIdMap.UpdateData(data);
    }
}
