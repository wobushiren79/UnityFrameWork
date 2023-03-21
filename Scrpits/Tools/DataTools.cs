using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DataTools 
{
    /// <summary>
    /// 获取前置条件
    /// </summary>
    /// <returns></returns>
    public static List<T> GetListData<T,E>(string data) where T : DataBean<E>,new()
    {
        List<T> listData = new List<T>();
        List<string> listDataStr = data.SplitForListStr('|');
        foreach (string itemData in listDataStr)
        {
            if (itemData.IsNull())
                continue;
            List<string> itemListData = itemData.SplitForListStr(':');
            E dataType = EnumExtension.GetEnum<E>(itemListData[0]);
            string dataValue = itemListData[1];

            T dataBean = new T();
            dataBean.dataType = dataType;
            dataBean.data = dataValue;

            listData.Add(dataBean);
        }
        return listData;
    }
}