using UnityEngine;
using Mono.Data.Sqlite;
using System;

public class SQLiteHelper
{
    /// <summary>
    /// 数据库连接定义
    /// </summary>
    private SqliteConnection mDbConnection;

    /// <summary>
    /// SQL命令定义
    /// </summary>
    private SqliteCommand mDbCommand;

    /// <summary>
    /// 数据读取定义
    /// </summary>
    private SqliteDataReader mDataReader;

    /// <summary>
    /// 构造函数    
    /// </summary>
    /// <param name="connectionString">数据库连接字符串</param>
    public SQLiteHelper(string connectionString)
    {
        try
        {
            //构造数据库连接
            mDbConnection = new SqliteConnection(connectionString);
            //打开数据库
            mDbConnection.Open();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    /// <summary>
    /// 执行SQL命令
    /// </summary>
    /// <returns>The query.</returns>
    /// <param name="queryString">SQL命令字符串</param>
    public SqliteDataReader ExecuteQuery(string queryString)
    {
        mDbCommand = mDbConnection.CreateCommand();
        mDbCommand.CommandText = queryString;
        mDataReader = mDbCommand.ExecuteReader();
        return mDataReader;
    }

    /// <summary>
    /// 关闭数据库连接
    /// </summary>
    public void CloseConnection()
    {
        //销毁Command
        if (mDbCommand != null)
        {
            mDbCommand.Cancel();
        }
        mDbCommand = null;

        //销毁Reader
        if (mDataReader != null)
        {
            mDataReader.Close();
        }
        mDataReader = null;

        //销毁Connection
        if (mDbConnection != null)
        {
            mDbConnection.Close();
        }
        mDbConnection = null;
    }

    /// <summary>
    /// 获取表信息
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    public SqliteDataReader GetTableInfo(string tableName)
    {
        string queryString = "PRAGMA table_info("+ tableName + ")";
        return ExecuteQuery(queryString);
    }

    /// <summary>
    /// 向指定数据表中插入数据
    /// </summary>
    /// <returns>The values.</returns>
    /// <param name="tableName">数据表名称</param>
    /// <param name="values">插入的数值</param>
    public SqliteDataReader InsertValues(string tableName, string[] values)
    {
        //获取数据表中字段数目
        int fieldCount = ReadTable(tableName).FieldCount;
        //当插入的数据长度不等于字段数目时引发异常
        if (values.Length != fieldCount)
        {
            throw new SqliteException("values.Length!=fieldCount");
        }

        string queryString = "INSERT INTO " + tableName + " VALUES (" + values[0];
        for (int i = 1; i < values.Length; i++)
        {
            queryString += ", " + values[i];
        }
        queryString += " )";
#if UNITY_EDITOR
        LogUtil.Log(queryString);
#endif
        return ExecuteQuery(queryString);
    }

    public SqliteDataReader InsertValues(string tableName, string[] keys, string[] values)
    {
        //获取数据表中字段数目
        //当插入的数据长度不等于字段数目时引发异常
        if (keys.Length != values.Length)
        {
            throw new SqliteException("插入数据键值不相等");
        }

        string keyString = "";
        for (int i = 0; i < keys.Length; i++)
        {
            if (i == 0)
                keyString += keys[i];
            else
                keyString += ("," + keys[i]);
        }
        string queryString = "INSERT INTO " + tableName + "(" + keyString + ") VALUES (" + values[0];

        for (int i = 1; i < values.Length; i++)
        {
            queryString += ", " + values[i];
        }
        queryString += " )";
#if UNITY_EDITOR
        LogUtil.Log(queryString);
#endif
        return ExecuteQuery(queryString);
    }

    /// <summary>
    /// 更新指定数据表内的数据
    /// </summary>
    /// <returns>The values.</returns>
    /// <param name="tableName">数据表名称</param>
    /// <param name="colNames">字段名</param>
    /// <param name="colValues">字段名对应的数据</param>
    /// <param name="key">关键字</param>
    /// <param name="value">关键字对应的值</param>
    public SqliteDataReader UpdateValues(string tableName, string[] colNames, string[] colValues, string key, string operation, string value)
    {
        //当字段名称和字段数值不对应时引发异常
        if (colNames.Length != colValues.Length)
        {
            throw new SqliteException("colNames.Length!=colValues.Length");
        }

        string queryString = "UPDATE " + tableName + " SET " + colNames[0] + "=" + colValues[0];
        for (int i = 1; i < colValues.Length; i++)
        {
            queryString += ", " + colNames[i] + "=" + colValues[i];
        }
        queryString += " WHERE " + key + operation + value;
#if UNITY_EDITOR
        LogUtil.Log(queryString);
#endif
        return ExecuteQuery(queryString);
    }

    /// <summary>
    /// 删除指定数据表内的数据
    /// </summary>
    /// <returns>The values.</returns>
    /// <param name="tableName">数据表名称</param>
    /// <param name="colNames">字段名</param>
    /// <param name="colValues">字段名对应的数据</param>
    public SqliteDataReader DeleteValuesOR(string tableName, string[] colNames, string[] operations, string[] colValues)
    {
        //当字段名称和字段数值不对应时引发异常
        if (colNames.Length != colValues.Length || operations.Length != colNames.Length || operations.Length != colValues.Length)
        {
            throw new SqliteException("colNames.Length!=colValues.Length || operations.Length!=colNames.Length || operations.Length!=colValues.Length");
        }

        string queryString = "DELETE FROM " + tableName + " WHERE " + colNames[0] + operations[0] + colValues[0];
        for (int i = 1; i < colValues.Length; i++)
        {
            queryString += "OR " + colNames[i] + operations[0] + colValues[i];
        }
#if UNITY_EDITOR
        LogUtil.Log(queryString);
#endif
        return ExecuteQuery(queryString);
    }

    /// <summary>
    /// 删除指定数据表内的数据
    /// </summary>
    /// <returns>The values.</returns>
    /// <param name="tableName">数据表名称</param>
    /// <param name="colNames">字段名</param>
    /// <param name="colValues">字段名对应的数据</param>
    public SqliteDataReader DeleteValuesAND(string tableName, string[] colNames, string[] operations, string[] colValues)
    {
        //当字段名称和字段数值不对应时引发异常
        if (colNames.Length != colValues.Length || operations.Length != colNames.Length || operations.Length != colValues.Length)
        {
            throw new SqliteException("colNames.Length!=colValues.Length || operations.Length!=colNames.Length || operations.Length!=colValues.Length");
        }

        string queryString = "DELETE FROM " + tableName + " WHERE " + colNames[0] + operations[0] + colValues[0];
        for (int i = 1; i < colValues.Length; i++)
        {
            queryString += " AND " + colNames[i] + operations[i] + colValues[i];
        }
#if UNITY_EDITOR
        LogUtil.Log(queryString);
#endif
        return ExecuteQuery(queryString);
    }

    public SqliteDataReader DeleteValuesANDAndLeft(string tableName, string[] colNames, string[] operations, string[] colValues)
    {
        //当字段名称和字段数值不对应时引发异常
        if (colNames.Length != colValues.Length || operations.Length != colNames.Length || operations.Length != colValues.Length)
        {
            throw new SqliteException("colNames.Length!=colValues.Length || operations.Length!=colNames.Length || operations.Length!=colValues.Length");
        }

        string queryString = "PRAGMA foreign_keys=ON; DELETE FROM " + tableName + " WHERE " + colNames[0] + operations[0] + colValues[0];
        for (int i = 1; i < colValues.Length; i++)
        {
            queryString += " AND " + colNames[i] + operations[i] + colValues[i];
        }
#if UNITY_EDITOR
        LogUtil.Log(queryString);
#endif
        return ExecuteQuery(queryString);
    }

    /// <summary>
    /// 创建数据表
    /// </summary> +
    /// <returns>The table.</returns>
    /// <param name="tableName">数据表名</param>
    /// <param name="colNames">字段名</param>
    /// <param name="colTypes">字段名类型</param>
    public SqliteDataReader CreateTable(string tableName, string[] colNames, string[] colTypes)
    {
        string queryString = "CREATE TABLE " + tableName + "( " + colNames[0] + " " + colTypes[0];
        for (int i = 1; i < colNames.Length; i++)
        {
            queryString += ", " + colNames[i] + " " + colTypes[i];
        }
        queryString += "  ) ";
#if UNITY_EDITOR
        LogUtil.Log(queryString);
#endif
        return ExecuteQuery(queryString);
    }


    /// <summary>
    /// 连表查询
    /// </summary>
    /// <param name="mainTableName"></param>
    /// <param name="leftTableName"></param>
    /// <param name="mainKey"></param>
    /// <param name="leftKey"></param>
    /// <param name="mainColNames"></param>
    /// <param name="mainOperations"></param>
    /// <param name="mainColValues"></param>
    /// <returns></returns>
    public SqliteDataReader ReadTable(string mainTableName, string[] leftTableName, string[] mainKey, string[] leftKey, string[] mainColNames, string[] mainOperations, string[] mainColValues)
    {
        if (mainTableName == null)
        {
            LogUtil.Log("查询失败，没有表名");
            return null;
        }
        string selectStr = "SELECT * ";

        string fromStr = "FROM " + mainTableName;
        if (mainKey != null && leftTableName != null && leftTableName.Length > 0)
        {
            int leftTableList = leftTableName.Length;
            for (int i = 0; i < leftTableList; i++)
            {
                string mainKeyStr = "";
                if (mainKey.Length == 1 || mainKey.Length != leftTableList)
                {
                    mainKeyStr = mainKey[0];
                }
                else
                {
                    mainKeyStr = mainKey[i];
                }
                fromStr += " LEFT OUTER JOIN " + leftTableName[i] + " ON " + mainTableName + "." + mainKeyStr + " = " + leftTableName[i] + "." + leftKey[i] + " ";
            }
        }

        string whereStr = "";
        if (mainColNames != null && mainColNames.Length > 0)
        {
            whereStr = " WHERE " + mainColNames[0] + " " + mainOperations[0] + " " + mainColValues[0];
            if (mainColNames.Length > 1)
            {
                for (int i = 1; i < mainColNames.Length; i++)
                {
                    whereStr += " AND " + mainColNames[i] + " " + mainOperations[i] + " " + mainColValues[i] + " ";
                }
            }
        }

        string queryString = selectStr + fromStr + whereStr;
#if UNITY_EDITOR
        LogUtil.Log(queryString);
#endif
        return ExecuteQuery(queryString);
    }

    public SqliteDataReader ReadTable(string tableName)
    {
        return ReadTable(tableName, null, null, null, null, null, null);
    }
    public SqliteDataReader ReadTable(string tableName, string[] colNames, string[] operations, string[] colValues)
    {
        return ReadTable(tableName, null, null, null, colNames, operations, colValues);
    }
    public SqliteDataReader ReadTable(string mainTableName, string[] leftTableName, string mainKey, string[] leftKey)
    {
        string[] tempMainkey = new string[] { mainKey };
        return ReadTable(mainTableName, leftTableName, tempMainkey, leftKey, null, null, null);
    }
}