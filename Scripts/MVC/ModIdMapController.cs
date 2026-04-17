using UnityEngine;

public class ModIdMapController : BaseMVCController<ModIdMapModel, IModIdMapView>
{
    public ModIdMapController(BaseMonoBehaviour content, IModIdMapView view) : base(content, view)
    {
    }

    public override void InitData()
    {
    }

    /// <summary>
    /// 保存ModID映射数据
    /// </summary>
    public void SaveModIdMapData(ModIdMapBean bean)
    {
        if (bean == null)
        {
            GetView().SetModIdMapFail();
            return;
        }
        GetModel().SetModIdMapData(bean);
        GetView().SetModIdMapSuccess(bean);
    }

    /// <summary>
    /// 获取ModID映射数据
    /// </summary>
    public ModIdMapBean GetModIdMapData()
    {
        ModIdMapBean bean = GetModel().GetModIdMapData();
        if (bean == null)
        {
            GetView().GetModIdMapFail();
            return bean;
        }
        GetView().GetModIdMapSuccess(bean);
        return bean;
    }
}
