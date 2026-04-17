public class ModIdMapService : BaseDataStorage
{
    protected readonly string saveFileName;

    public ModIdMapService()
    {
        saveFileName = "ModIdMap";
    }

    /// <summary>
    /// 查询ModID映射数据
    /// </summary>
    public ModIdMapBean QueryData()
    {
        return BaseLoadData<ModIdMapBean>(saveFileName, jsonType: JsonTypeEnum.Net);
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    public void UpdateData(ModIdMapBean data)
    {
        BaseSaveData(saveFileName, data, jsonType: JsonTypeEnum.Net);
    }
}
