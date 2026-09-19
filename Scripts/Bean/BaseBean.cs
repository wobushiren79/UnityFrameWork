using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class BaseBean
{
    public long id;

    /// <summary>
    /// Mod数据合并钩子：BaseCfg.GetInitDataForMods 合并 Mod JsonText 行时，在 id 拼接 modId 后调用。
    /// 用于把「指向 Mod 自带配置的引用字段」按同一 modId 拼接（如 ItemsInfoBean.name 指向 Mod 自带语言表 Language_ItemsInfo_* 的行 id）。
    /// **通常无需手工重写**：由 ExcelEditorWindow.CreateEntity 按 Excel 列头标记（[language]/[language_1]/[language_2]/[mode_id]）在 *Bean.cs 中自动生成重写；
    /// 仅特殊手写 Bean（无 Excel 表）需要拼接时才手工重写。默认无操作。
    /// </summary>
    /// <param name="modId">该行所属 Mod 的 modId</param>
    public virtual void CombineModReferenceIds(int modId) { }
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
                    //拼接引用字段（如 Mod 道具的 name 指向 Mod 自带语言表行 id）
                    bean.CombineModReferenceIds(info.modId);
                    listModsData.Add(bean);
                }
            }
        }
        return listModsData;
    }

    /// <summary>
    /// 组合Mod ID：modId(5位) + selfId(14位)。Mod JsonText 行及其引用字段统一用本方法拼接
    /// </summary>
    public static long CombineModId(int modId, long selfId)
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
