using System;
using System.Collections.Generic;
[Serializable]
public partial class BaseInfoBean : BaseBean
{
	/// <summary>
	///内容
	/// </summary>
	public string content;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class BaseInfoCfg : BaseCfg<long, BaseInfoBean>
{
	public static string fileName = "BaseInfo";
	protected static Dictionary<long, BaseInfoBean> dicData = null;
	public static Dictionary<long, BaseInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			BaseInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static BaseInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			BaseInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(BaseInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, BaseInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			BaseInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
