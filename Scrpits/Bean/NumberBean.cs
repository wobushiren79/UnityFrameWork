using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class NumberBean 
{
    public long id;
    public long number;
    
    public NumberBean()
    {
        this.id = 0;
        this.number = 1;
    }

    public NumberBean(long id, long number)
    {
        this.id = id;
        this.number = number;
    }

    public NumberBean(long id)
    {
        this.id = id;
        this.number = 1;
    }

    /// <summary>
    /// 获取列表数据
    /// </summary>
    /// <param name="listDataStr"></param>
    /// <returns></returns>
    public static List<NumberBean> GetListNumberBean(string listDataStr)
    {
        List<NumberBean> listData = new List<NumberBean>();
        string[] listItemsData = listDataStr.SplitForArrayStr('&');
        for (int i = 0; i < listItemsData.Length; i++)
        {
            string itemData1 = listItemsData[i];
            string[] itemData2 = itemData1.SplitForArrayStr(':');
            long itemId = long.Parse(itemData2[0]);
            if (itemData2.Length == 1)
            {
                listData.Add(new NumberBean(itemId, 1));
            }
            else
            {
                int itemNumber = int.Parse(itemData2[1]);
                listData.Add(new NumberBean(itemId, itemNumber));
            }
        }
        return listData;
    }
}