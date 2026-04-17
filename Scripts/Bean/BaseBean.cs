using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class BaseBean
{
    public long id;
}

public class BaseCfg<E, T> where T : BaseBean
{
    protected static T[] arrayData;//数组数据
    
    protected static T GetItemData(E key, Dictionary<E, T> dicData)
    {
        if (dicData.TryGetValue(key, out T value))
        {
            return value;
        }
        return null;
    }

    protected static T[] GetInitData(string fileName)
    {
        if (fileName == null)
        {
            LogUtil.Log($"读取文件失败-没有文件名称{fileName}");
            return null;
        }
        TextAsset textAsset = LoadResourcesUtil.SyncLoadData<TextAsset>($"JsonText/{fileName}");
        if (textAsset == null || textAsset.text == null)
            return null;

        T[] baseData = JsonUtil.FromJsonByNet<T[]>(textAsset.text);
        var combinedList = baseData != null ? new List<T>(baseData) : new List<T>();

        //添加MOD数据
        var modeListData =  GetInitDataForMods(fileName);
        combinedList.AddRange(modeListData);
        arrayData = combinedList.ToArray();
        return arrayData;
    }

    /// <summary>
    /// 初始化Mod数据
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    protected static List<T> GetInitDataForMods(string fileName)
    {
        // 加载Mod扩展数据
        List<T> listModsData = new List<T>();
        if (ModHandler.Instance != null && ModHandler.Instance.manager != null)
        {
            var modInfos = ModHandler.Instance.manager.GetModJsonTextFileInfos(fileName);
            foreach (var info in modInfos)
            {
                if (!File.Exists(info.filePath))
                    continue;

                string jsonText = File.ReadAllText(info.filePath);
                T[] modArray = JsonUtil.FromJsonByNet<T[]>(jsonText);
                if (modArray == null)
                    continue;

                for (int i = 0; i < modArray.Length; i++)
                {
                    T bean = modArray[i];
                    bean.id = CombineModId(info.modId, bean.id);
                    listModsData.Add(bean);
                }
            }
        }
        return listModsData;
    }

    private static long CombineModId(int modId, long selfId)
    {
        string idStr = $"{modId:D5}{selfId:D14}";
        if (long.TryParse(idStr, out long result))
            return result;

        LogUtil.LogWarning($"[Mod] 组合ID溢出: modId={modId}, selfId={selfId}，回退到安全值");
        int safeModId = modId % 9224;
        if (safeModId == 0) safeModId = 1;
        return safeModId * 100000000000000L + selfId;
    }
}
