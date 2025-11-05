using System;
using System.Collections.Generic;
[Serializable]
public partial class LanguageBean : BaseBean
{
	/// <summary>
	///内容
	/// </summary>
	public string content;
}
public partial class LanguageCfg : BaseCfg<long, LanguageBean>
{
	public static string currentLanguage = "";
	public static string fileName = "Language";
	protected static Dictionary<string, Dictionary<long, LanguageBean>> dicData = null;

	public static LanguageBean GetItemData(string cfgName, long key)
	{
		if (dicData == null || !dicData.ContainsKey(cfgName))
		{
			LanguageBean[] arrayData = GetInitData($"{fileName}_{cfgName}_{currentLanguage}");
			InitData(cfgName, arrayData);
		}
		if (dicData.TryGetValue(cfgName, out Dictionary<long, LanguageBean> cfgData))
		{
			if (cfgData.TryGetValue(key, out LanguageBean value))
			{
				return value;
			}
		}
		return null;
	}
	
	public static void InitData(string cfgName, LanguageBean[] arrayData)
	{
		Dictionary<long, LanguageBean> cfgDicData = new Dictionary<long, LanguageBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			LanguageBean itemData = arrayData[i];
			cfgDicData.Add(itemData.id, itemData);
		}
		if (dicData == null)
        {
			dicData = new Dictionary<string, Dictionary<long, LanguageBean>>();
        }
		dicData.Add(cfgName, cfgDicData);
	}
}
