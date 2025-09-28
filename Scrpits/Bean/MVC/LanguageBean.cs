using System;
using System.Collections.Generic;
[Serializable]
public partial class LanguageBean : BaseBean
{
	/// <summary>
	///内容-中文
	/// </summary>
	public string content;
}
public partial class LanguageCfg : BaseCfg<long, LanguageBean>
{
	public static string currentLanguage = "";
	public static string fileName = "Language";
	protected static Dictionary<long, LanguageBean> dicData = null;
	public static Dictionary<long, LanguageBean> GetAllData()
	{
		if (dicData == null)
		{
			LanguageBean[] arrayData = GetInitData(fileName + "_" + currentLanguage);
			InitData(arrayData);
		}
		return dicData;
	}
	public static LanguageBean GetItemData(long key)
	{
		if (dicData == null)
		{
			LanguageBean[] arrayData = GetInitData(fileName + "_" + currentLanguage);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(LanguageBean[] arrayData)
	{
		dicData = new Dictionary<long, LanguageBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			LanguageBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
