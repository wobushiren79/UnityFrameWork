using UnityEngine;
using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

public class SQLiteHandle 
{
    private static string DB_PATH = "";

    public static SQLiteHelper GetSQLiteHelper(string dbName)
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            DB_PATH = "data source=" + Application.persistentDataPath + "/" + dbName;
        }
        else
        {
            DB_PATH = "data source=" + Application.streamingAssetsPath + "/SQLiteDataBase/" + dbName;
        }
        SQLiteHelper helper = new SQLiteHelper(DB_PATH);
        return helper;
    }

    /// <summary>
    /// 创建表
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="dataTypeList"></param>
    public static void CreateTable(string dbName, string tableName, Dictionary<string, string> dataTypeList)
    {
        SQLiteHelper sql = GetSQLiteHelper(dbName);
        if (tableName == null)
        {
            LogUtil.Log("创建表失败，没有表名");
            return;
        }
        if (dataTypeList == null || dataTypeList.Count == 0)
        {
            LogUtil.Log("创建表失败，没有数据");
            return;
        }
        string[] keyNameList = new string[dataTypeList.Count];
        string[] valueNameList = new string[dataTypeList.Count];
        int position = 0;
        foreach (var item in dataTypeList)
        {
            keyNameList[position] = item.Key;
            valueNameList[position] = item.Value;
            position++;
        }
        try
        {
            sql.CreateTable(tableName, keyNameList, valueNameList);
        }
        catch (Exception e)
        {
            LogUtil.Log("创建表失败-" + e.Message);
        }
        finally
        {
            if (sql != null)
                sql.CloseConnection();
        }
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="dbName"></param>
    /// <param name="mainTable"></param>
    /// <param name="dataNames"></param>
    /// <param name="dataValue"></param>
    /// <param name="key"></param>
    /// <param name="operation"></param>
    /// <param name="value"></param>
    public static void UpdateTableData(string dbName,string mainTable,string[] dataNames,string[] dataValue,string key,string operation,string value)
    {
        if (key == null|| operation==null|| value==null) {
            key = "id";
            operation = "!=";
            value = "0";
        }
        SQLiteHelper sql = GetSQLiteHelper(dbName);
        try
        {
            sql.UpdateValues(mainTable, dataNames, dataValue, key, operation, value);
        }
        catch (Exception e)
        {
            LogUtil.Log("更新数据:" + e.Message);
        }
        finally
        {
            if (sql != null)
                sql.CloseConnection();
        }
    }
    public static void UpdateTableData(string dbName, string mainTable, string[] dataNames, string[] dataValue)
    {
        UpdateTableData( dbName,  mainTable,  dataNames,  dataValue, null, null, null);
    }

    /// <summary>
    /// 读取表数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbName"></param>
    /// <param name="mainTable"></param>
    /// <param name="leftTableName"></param>
    /// <param name="mainKey"></param>
    /// <param name="leftKey"></param>
    /// <param name="mainColNames"></param>
    /// <param name="mainOperations"></param>
    /// <param name="mainColValues"></param>
    /// <returns></returns>
    public static List<T> LoadTableData<T>(string dbName, string mainTable, string[] leftTableName, string[] mainKey, string[] leftKey, string[] mainColNames, string[] mainOperations, string[] mainColValues)
    {
        SQLiteHelper sql = GetSQLiteHelper(dbName);
        SqliteDataReader reader = null;
        List<T> listData = new List<T>();
        try
        {
            List<string> dataNameList = ReflexUtil.GetAllName<T>();
            reader = sql.ReadTable(mainTable, leftTableName, mainKey, leftKey, mainColNames, mainOperations, mainColValues);
            while (reader.Read())
            {
                T itemData = Activator.CreateInstance<T>();

                int dataNameSize = dataNameList.Count;
                for (int i = 0; i < dataNameSize; i++)
                {
                    string dataName = dataNameList[i];
                    int ordinal = reader.GetOrdinal(dataName);
                    if (ordinal == -1)
                        continue;

                    string name = reader.GetName(ordinal);
                    object value = reader.GetValue(ordinal);
                    if (value != null && !value.ToString().Equals("")) 
                    ReflexUtil.SetValueByName(itemData, dataName, value);
                }
                listData.Add(itemData);
            }
            return listData;
        }
        catch (Exception e)
        {
            LogUtil.Log("查询表失败-" + e.Message);
            return null;
        }
        finally
        {
            if (sql != null)
                sql.CloseConnection();
            if (reader != null)
                reader.Close();
        }
    }
    public static List<T> LoadTableData<T>(string dbName, string mainTable)
    {
        return LoadTableData<T>(dbName, mainTable, null, null, null, null, null, null);
    }
    public static List<T> LoadTableData<T>(string dbName, string mainTable, string[] leftTableName, string mainKey, string[] leftKey)
    {
        string[] tempMainKey = new string[] { mainKey };
        return LoadTableData<T>(dbName, mainTable, leftTableName, tempMainKey, leftKey, null, null, null);
    }
    public static List<T> LoadTableData<T>(string dbName, string mainTable, string[] leftTableName, string[] mainKey, string[] leftKey)
    {
        return LoadTableData<T>(dbName, mainTable, leftTableName, mainKey, leftKey, null, null, null);
    }
    public static List<T> LoadTableDataByCol<T>(string dbName, string mainTable, string[] mainColNames, string[] mainOperations, string[] mainColValue)
    {
        return LoadTableData<T>(dbName, mainTable, null, null, null, mainColNames, mainOperations, mainColValue);
    }

    /// <summary>
    /// 删除表数据
    /// </summary>
    /// <param name="dbName">数据库名字</param>
    /// <param name="tableName"></param>
    /// <param name="colNames"></param>
    /// <param name="operations"></param>
    /// <param name="colValues"></param>
    public static bool DeleteTableData(string dbName, string tableName, string[] colNames, string[] operations, string[] colValues)
    {
        SQLiteHelper sql = GetSQLiteHelper(dbName);
        try
        {
            sql.DeleteValuesAND(tableName, colNames, operations, colValues);
            return true;
        }
        catch (Exception e)
        {
            LogUtil.Log("更新数据失败:" + e.Message);
            return false;
        }
        finally
        {
            if (sql != null)
                sql.CloseConnection();
        }
    }
    public static bool DeleteTableDataAndLeft(string dbName, string tableName, string[] colNames, string[] operations, string[] colValues)
    {
        SQLiteHelper sql = GetSQLiteHelper(dbName);
        try
        {
            sql.DeleteValuesANDAndLeft(tableName, colNames, operations, colValues);
            return true;
        }
        catch (Exception e)
        {
            LogUtil.Log("更新数据失败:" + e.Message);
            return false;
        }
        finally
        {
            if (sql != null)
                sql.CloseConnection();
        }
    }
    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="dbName"></param>
    /// <param name="tableName"></param>
    /// <param name="values"></param>
    public static bool InsertValues(string dbName, string tableName,string[] values)
    {
        SQLiteHelper sql = GetSQLiteHelper(dbName);
        try
        {
            sql.InsertValues(tableName, values);
            return true;
        }
        catch (Exception e)
        {
            LogUtil.Log("插入数据失败:" + e.Message);
            return false;
        }
        finally
        {
            if (sql != null)
                sql.CloseConnection();
        }
    }

    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="dbName"></param>
    /// <param name="tableName"></param>
    /// <param name="keys"></param>
    /// <param name="values"></param>
    public static bool InsertValues(string dbName, string tableName, string[] keys, string[] values)
    {
        SQLiteHelper sql = GetSQLiteHelper(dbName);
        try
        {
            sql.InsertValues(tableName, keys, values);
            return true;
        }
        catch (Exception e)
        {
            LogUtil.Log("插入数据失败:" + e.Message);
            return false;
        }
        finally
        {
            if (sql != null)
                sql.CloseConnection();
        }
    }
}