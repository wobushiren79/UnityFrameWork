using UnityEditor;
using UnityEngine;
using OfficeOpenXml;
using System.IO;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
public static class ExcelUtil
{
    public struct ExcelChangeData
    {
        public long id;
        public string propertyName;
        public string propertyValue;

        public ExcelChangeData(long id, string propertyName, string propertyValue)
        {
            this.id = id;
            this.propertyName = propertyName;
            this.propertyValue = propertyValue;
        }
    }

    /// <summary>
    /// 获取ExcelPackage
    /// </summary>
    public static void GetExcelPackage(FileInfo fileInfo, Action<ExcelPackage> actionDo)
    {
        if (fileInfo.Name.Contains(".meta"))
            return;
        string filePath = fileInfo.FullName;
        if (filePath.Contains(".meta"))
            return;
        if (filePath.Contains("~$"))
            return;
        LogUtil.Log($"filePath:{filePath}");
        FileStream fs;
        try
        {
            fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }
        catch
        {
            LogUtil.LogError("请先关闭对应的Excel文档");
            return;
        }
        try
        {
            ExcelPackage ep = new ExcelPackage(fs);
            actionDo?.Invoke(ep);
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
        finally
        {
            fs.Close();
        }
    }

    /// <summary>
    /// 设置表格数据
    /// </summary>
    /// <param name="excelPath">文件路径</param>
    /// <param name="workSheetName">表名</param>
    /// <param name="listChangeData">需要修改的数据</param>
    public static void SetExcelData(string excelPath, string workSheetName, List<ExcelChangeData> listChangeData)
    {
        FileInfo file = new FileInfo(excelPath);
        using (ExcelPackage pack = new ExcelPackage(file))
        {
            ExcelWorksheet worksheet = pack.Workbook.Worksheets[workSheetName];
            for (int i = 0; i < listChangeData.Count; i++)
            {
                //横排
                int columnCount = worksheet.Dimension.End.Column;
                //竖排
                int rowCount = worksheet.Dimension.End.Row;
                var itemChangeData = listChangeData[i];
                bool hasData = false;
                for (int y = 4; y <= rowCount; y++)
                {
                    //查询ID
                    var cellItemID = long.Parse(worksheet.Cells[y, 1].Text);
                    if (cellItemID == itemChangeData.id)
                    {
                        for (int x = 1; x <= columnCount; x++)
                        {
                            var itemName = worksheet.Cells[1, x].Text;
                            if (itemName.Equals($"{itemChangeData.propertyName}"))
                            {
                                var cellItem = worksheet.Cells[y, x];
                                cellItem.Value = itemChangeData.propertyValue;
                                hasData = true;
                                break;
                            }
                        }
                        break;
                    }
                }
                if (!hasData)
                {
                    for (int x = 1; x <= columnCount; x++)
                    {
                        var itemName = worksheet.Cells[1, x].Text;
                        var cellItem = worksheet.Cells[rowCount + 1, x];
                        if (x == 1)
                        {
                            cellItem.Value = itemChangeData.id;
                        }
                        else
                        {
                            if (itemName.Equals($"{itemChangeData.propertyName}"))
                            {
                                cellItem.Value = itemChangeData.propertyValue;
                            }
                        }
                    }
                }
            }
            pack.Save();
            LogUtil.Log("设置数据表完成");
        }
    }
}