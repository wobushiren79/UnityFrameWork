using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class SpineAnimationStateBean : BaseBean
{
	/// <summary>
	///内容
	/// </summary>
	public string res;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class SpineAnimationStateCfg : BaseCfg<long, SpineAnimationStateBean>
{
	public static string fileName = "SpineAnimationState";
	protected static Dictionary<long, SpineAnimationStateBean> dicData = null;
	public static Dictionary<long, SpineAnimationStateBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static SpineAnimationStateBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static SpineAnimationStateBean GetItemData(long key)
	{
		if (dicData == null)
		{
			SpineAnimationStateBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(SpineAnimationStateBean[] arrayData)
	{
		dicData = new Dictionary<long, SpineAnimationStateBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			SpineAnimationStateBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
