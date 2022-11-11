using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class BaseMVCService
{
    public string tableNameForMain;//主表名称
    public string tableNameForLeft;//副标名称

    public BaseMVCService(string tableName) : this(tableName, null)
    {

    }

    public BaseMVCService(string tableName, string leftDetailsTableName)
    {
        tableNameForMain = tableName;
        tableNameForLeft = leftDetailsTableName;
    }

    public string GetTableName()
    {
        return tableNameForMain;
    }

    public string GetLeftTableName()
    {
        return tableNameForLeft;
    }

    /// <summary>
    /// 查询所有数据
    /// </summary>
    /// <returns></returns>
    public List<T> BaseQueryAllData<T>()
    {
        if (tableNameForMain == null)
        {
            LogUtil.LogError("查询数据失败，没有表名");
            return null;
        }
        return SQLiteHandle.LoadTableData<T>(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableNameForMain);
    }

    /// <summary>
    /// 链表查询所有数据
    /// </summary>
    /// <param name="leftId"></param>
    /// <returns></returns>
    public List<T> BaseQueryAllData<T>(string leftId)
    {
        if (tableNameForLeft == null)
        {
            LogUtil.LogError("查询数据失败，没有关联的副表");
            return null;
        }
        return SQLiteHandle.LoadTableData<T>
            (ProjectConfigInfo.DATA_BASE_INFO_NAME, tableNameForMain,
            new string[] { tableNameForLeft },
            new string[] { "id" },
            new string[] { leftId });
    }

    /// <summary>
    /// 查询数据 单个条件
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public List<T> BaseQueryData<T>(string key, string value)
    {
        string[] colName = new string[] { key };
        string[] operations = new string[] { "=" };
        string[] colValue = new string[] { value };
        return SQLiteHandle.LoadTableDataByCol<T>(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableNameForMain, colName, operations, colValue);
    }

    /// <summary>
    /// 链表查询数据 单个数据
    /// </summary>
    /// <param name="leftId"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public List<T> BaseQueryData<T>(string leftId, string key, string value)
    {
        if (leftId.IsNull() || tableNameForLeft.IsNull())
        {
            return BaseQueryData<T>(key, value);
        }
        else
        {
            return BaseQueryData<T>(leftId, key, "=", value);
        }
    }

    public List<T> BaseQueryData<T>(string leftId, string key, string operation, string value)
    {
        if (tableNameForMain == null)
        {
            LogUtil.LogError("查询数据失败，没有表名");
            return null;
        }
        if (tableNameForLeft == null)
        {
            LogUtil.LogError("查询数据失败，没有关联的副表");
            return null;
        }
        string[] leftTable = new string[] { tableNameForLeft };
        string[] mainKey = new string[] { "id" };
        string[] leftKey = new string[] { leftId };
        string[] colName = new string[] { key };
        string[] operations = new string[] { operation };
        string[] colValue = new string[] { value };
        return SQLiteHandle.LoadTableData<T>(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableNameForMain, leftTable, mainKey, leftKey, colName, operations, colValue);
    }

    public List<T> BaseQueryData<T>(string leftId, string key1, string value1, string key2, string value2)
    {
        return BaseQueryData<T>(leftId, key1, "=", value1, key2, "=", value2);
    }

    public List<T> BaseQueryData<T>(string leftId, string key1, string operation1, string value1, string key2, string operation2, string value2)
    {
        if (tableNameForMain == null)
        {
            LogUtil.LogError("查询数据失败，没有表名");
            return null;
        }
        if (tableNameForLeft == null)
        {
            LogUtil.LogError("查询数据失败，没有关联的副表");
            return null;
        }
        string[] leftTable = new string[] { tableNameForLeft };
        string[] mainKey = new string[] { "id" };
        string[] leftKey = new string[] { leftId };
        string[] colName = new string[] { key1, key2 };
        string[] operations = new string[] { operation1, operation2 };
        string[] colValue = new string[] { value1, value2 };
        return SQLiteHandle.LoadTableData<T>(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableNameForMain, leftTable, mainKey, leftKey, colName, operations, colValue);
    }

    public List<T> BaseQueryData<T>(string leftId, string key1, string operation1, string value1, string key2, string operation2, string value2, string key3, string operation3, string value3)
    {
        if (tableNameForMain == null)
        {
            LogUtil.LogError("查询数据失败，没有表名");
            return null;
        }
        if (tableNameForLeft == null)
        {
            LogUtil.LogError("查询数据失败，没有关联的副表");
            return null;
        }
        string[] leftTable = new string[] { tableNameForLeft };
        string[] mainKey = new string[] { "id" };
        string[] leftKey = new string[] { leftId };
        string[] colName = new string[] { key1, key2, key3 };
        string[] operations = new string[] { operation1, operation2, operation3 };
        string[] colValue = new string[] { value1, value2, value3 };
        return SQLiteHandle.LoadTableData<T>(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableNameForMain, leftTable, mainKey, leftKey, colName, operations, colValue);
    }
   
    /// <summary>
    /// 通过ID删除数据
    /// </summary>
    /// <param name="itemsInfo"></param>
    public bool BaseDeleteDataById(long id)
    {
        if (id == 0)
            return false;
        string[] colKeys = new string[] { "id" };
        string[] operations = new string[] { "=" };
        string[] colValues = new string[] { id + "" };
        return SQLiteHandle.DeleteTableDataAndLeft(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableNameForMain, colKeys, operations, colValues);
    }

    /// <summary>
    /// 链表删除
    /// </summary>
    /// <param name="mainName"></param>
    /// <param name="leftName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool BaseDeleteDataWithLeft(string mainName,string leftName,string value)
    {
        bool isDeleteAll = true;
        if (BaseDeleteData(tableNameForMain, mainName, value))
        {
            if (tableNameForLeft.IsNull())
            {
                isDeleteAll = true;
            }
            else
            {
                isDeleteAll = BaseDeleteData(tableNameForLeft, leftName, value);
            }
        }
        else
            isDeleteAll = false;
        return isDeleteAll;
    }

    public bool BaseDeleteData(string key, string value)
    {
        string[] colKeys = new string[] { key };
        string[] operations = new string[] { "=" };
        string[] colValues = new string[] { value };
        return SQLiteHandle.DeleteTableDataAndLeft(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableNameForMain, colKeys, operations, colValues);
    }

    public bool BaseDeleteData(string tableName, string key, string value)
    {
        string[] colKeys = new string[] { key };
        string[] operations = new string[] { "=" };
        string[] colValues = new string[] { value };
        return SQLiteHandle.DeleteTableDataAndLeft(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableName, colKeys, operations, colValues);
    }

    public bool BaseDeleteData(string tableName, string key1, string value1, string key2, string value2)
    {
        string[] colKeys = new string[] { key1, key2 };
        string[] operations = new string[] { "=", "=" };
        string[] colValues = new string[] { value1, value2 };
        return SQLiteHandle.DeleteTableDataAndLeft(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableName, colKeys, operations, colValues);
    }

    public bool BaseDeleteData(string tableName, string key1, string value1, string key2, string value2, string key3, string value3)
    {
        string[] colKeys = new string[] { key1, key2,key3 };
        string[] operations = new string[] { "=", "=", "=" };
        string[] colValues = new string[] { value1, value2, value3 };
        return SQLiteHandle.DeleteTableDataAndLeft(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableName, colKeys, operations, colValues);
    }
    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="itemData"></param>
    public bool BaseInsertData<T>(string tableName, T itemData)
    {
        //插入数据
        Dictionary<string, object> mapData = ReflexUtil.GetAllNameAndValue(itemData);
        List<string> listKeys = new List<string>();
        List<string> listValues = new List<string>();
        foreach (var item in mapData)
        {
            string itemKey = item.Key;
            string valueStr = Convert.ToString(item.Value);
            listKeys.Add(item.Key);
            if (item.Value == null)
            {
                listValues.Add("null");
            }
            else if (item.Value is string)
            {
                if (valueStr.IsNull())
                    listValues.Add("null");
                else
                    listValues.Add("'" + valueStr + "'");
            }
            else
            {
                listValues.Add(valueStr);
            }
        }
        return SQLiteHandle.InsertValues(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableName, listKeys.ToArray(), listValues.ToArray());
    }

    /// <summary>
    /// 链表插入数据
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="listLeftName"></param>
    public bool BaseInsertDataWithLeft<T>(T itemData, List<string> listLeftName)
    {
        //插入数据
        Dictionary<string, object> mapData = ReflexUtil.GetAllNameAndValue(itemData);
        List<string> listMainKeys = new List<string>();
        List<string> listMainValues = new List<string>();
        List<string> listLeftKeys = new List<string>();
        List<string> listLeftValues = new List<string>();
        foreach (var item in mapData)
        {
            string itemKey = item.Key;
            if (listLeftName.Contains(itemKey))
            {
                string valueStr = Convert.ToString(item.Value);
                listLeftKeys.Add(item.Key);
                if (item.Value == null)
                {
                    listLeftValues.Add("null");
                }
                else if (item.Value is string)
                {
                    if (valueStr.IsNull())
                        listLeftValues.Add("null");
                    else
                        listLeftValues.Add("'" + valueStr + "'");
                }
                else if (item.Value == null)
                {
                    listLeftValues.Add("null");
                }
                else
                {
                    listLeftValues.Add(valueStr);
                }
            }
            else
            {
                string valueStr = Convert.ToString(item.Value);
                listMainKeys.Add(item.Key);
                if (item.Value == null)
                {
                    listMainValues.Add("null");
                }
                else if (item.Value is string)
                {
                    if (valueStr.IsNull())
                        listMainValues.Add("null");
                    else
                        listMainValues.Add("'" + valueStr + "'");
                }
                else if (item.Value == null)
                {
                    listMainValues.Add("null");
                }
                else
                {
                    listMainValues.Add(valueStr);
                }
            }
        }
        bool isInsert = true;
        isInsert = SQLiteHandle.InsertValues(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableNameForMain, listMainKeys.ToArray(), listMainValues.ToArray());
        if (isInsert)
        {
            SQLiteHandle.InsertValues(ProjectConfigInfo.DATA_BASE_INFO_NAME, tableNameForLeft, listLeftKeys.ToArray(), listMainValues.ToArray());
        }
        return isInsert;
    }
}