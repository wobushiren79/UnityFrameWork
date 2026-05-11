using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class BaseDataInfoBean : BaseBean
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
public partial class BaseDataInfoCfg : BaseCfg<long, BaseDataInfoBean>
{
	public static string fileName = "BaseDataInfo";
	protected static Dictionary<long, BaseDataInfoBean> dicData = null;
	public static Dictionary<long, BaseDataInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static BaseDataInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static BaseDataInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			BaseDataInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(BaseDataInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, BaseDataInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			BaseDataInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
