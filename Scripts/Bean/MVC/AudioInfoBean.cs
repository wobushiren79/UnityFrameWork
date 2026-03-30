using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class AudioInfoBean : BaseBean
{
	/// <summary>
	///内容
	/// </summary>
	public string name_res;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
	/// <summary>
	///类型0音效 1音乐 2环境音
	/// </summary>
	public int audio_type;
}
public partial class AudioInfoCfg : BaseCfg<long, AudioInfoBean>
{
	public static string fileName = "AudioInfo";
	protected static Dictionary<long, AudioInfoBean> dicData = null;
	public static Dictionary<long, AudioInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static AudioInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static AudioInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			AudioInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(AudioInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, AudioInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			AudioInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
