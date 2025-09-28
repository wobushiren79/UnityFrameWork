using System;
using System.Collections.Generic;
[Serializable]
public partial class UITextBean : BaseBean
{
	/// <summary>
	///内容id
	/// </summary>
	public long content_id;
	/// <summary>
	///内容
	/// </summary>
	public string remark_content;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class UITextCfg : BaseCfg<long, UITextBean>
{
	public static string fileName = "UIText";
	protected static Dictionary<long, UITextBean> dicData = null;
	public static Dictionary<long, UITextBean> GetAllData()
	{
		if (dicData == null)
		{
			UITextBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static UITextBean GetItemData(long key)
	{
		if (dicData == null)
		{
			UITextBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(UITextBean[] arrayData)
	{
		dicData = new Dictionary<long, UITextBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			UITextBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
