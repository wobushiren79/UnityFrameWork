using OfficeOpenXml;
using System.IO;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
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
        // 跳过备份文件（如 xxx.xlsx.bak.20260524_150955）。
        // 备份文件内含与源表同名的工作表，若被当作 Excel 处理会按工作表名覆盖正确导出的 JSON，
        // 仅识别后缀为 .xlsx / .xls 的真实 Excel 文件。
        string ext = fileInfo.Extension.ToLower();
        if (ext != ".xlsx" && ext != ".xls")
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

    /// <summary>
    /// 单个 Excel 文件转 Json（使用默认输出目录 Assets/Resources/JsonText，并刷新 AssetDatabase）
    /// 供编辑器内的工具（如 TestNpcCreateGUI 保存配置后）同步重新生成运行时 JSON
    /// </summary>
    /// <param name="excelPath">Excel 文件路径</param>
    public static void ExcelToJsonItem(string excelPath)
    {
        ExcelToJsonItem(new FileInfo(excelPath), Application.dataPath + "/Resources/JsonText");
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    /// <summary>
    /// 单个 Excel 文件转 Json
    /// 读取 Excel 所有工作表，根据表名自动匹配对应的 Bean 类型
    /// </summary>
    /// <param name="fileInfo">Excel 文件信息</param>
    /// <param name="jsonFolderPath">Json 输出目录</param>
    public static void ExcelToJsonItem(FileInfo fileInfo, string jsonFolderPath)
    {
        GetExcelPackage(fileInfo, (ep) =>
        {
            // 获取所有工作表
            ExcelWorksheets workSheets = ep.Workbook.Worksheets;

            // 遍历所有工作表
            for (int w = 1; w <= workSheets.Count; w++)
            {
                ExcelWorksheet sheet = workSheets[w];

                // 加载程序集并获取对应的 Bean 类型
                Assembly assembly = Assembly.Load("Assembly-CSharp");
                Type type;

                // 多语言表使用特殊的 LanguageBean 类型
                if (fileInfo.Name.Contains("excel_language"))
                {
                    type = assembly.GetType("LanguageBean");
                }
                else
                {
                    type = assembly.GetType(sheet.Name + "Bean");
                }

                // 类型不存在则报错
                if (type == null)
                {
                    LogUtil.LogError($"未找到对应的实体类：{sheet.Name}Bean");
                    return;
                }

                // 确保输出目录存在
                if (!Directory.Exists(jsonFolderPath))
                    Directory.CreateDirectory(jsonFolderPath);

                // 多语言表特殊处理
                if (fileInfo.Name.Contains("excel_language"))
                {
                    ExcelToJsonItemForLanguage(sheet, assembly, type, jsonFolderPath);
                }
                else
                {
                    ExcelToJsonItemForBase(sheet, assembly, type, jsonFolderPath);
                }
            }

            LogUtil.Log($"转换完成：{fileInfo.FullName}");
        });
    }

    /// <summary>
    /// 多语言表转 Json - 按语种拆分输出
    /// 支持按 content_语言名 格式区分不同语种数据
    /// </summary>
    /// <param name="sheet">Excel 工作表</param>
    /// <param name="assembly">程序集</param>
    /// <param name="type">Bean 类型</param>
    /// <param name="jsonFolderPath">Json 输出目录</param>
    private static void ExcelToJsonItemForLanguage(ExcelWorksheet sheet, Assembly assembly, Type type, string jsonFolderPath)
    {
        List<object> listData = new List<object>();
        string[] languageNames = EnumExtension.GetEnumNames<LanguageEnum>();

        // 遍历所有语种
        for (int l = 0; l < languageNames.Length; l++)
        {
            bool hasLanguageData = false;
            listData.Clear();

            int columnCount = sheet.Dimension.End.Column; // 列数
            int rowCount = sheet.Dimension.End.Row;       // 行数
            var languageName = languageNames[l];

            // 从第 4 行开始读取数据（前 3 行为元数据：属性名、字段名、描述）
            for (int row = 4; row <= rowCount; row++)
            {
                Dictionary<string, object> dictData = new Dictionary<string, object>();

                // 遍历所有列
                for (int column = 1; column <= columnCount; column++)
                {
                    string sheetCellName = sheet.Cells[1, column].Text;

                    // 跳过备注列
                    if (sheetCellName.Equals("remark"))
                        continue;

                    //ID也要保存
                    if (sheetCellName.Equals("id"))
                    {

                    }
                    else
                    {
                        // 处理多语言列（content_语言名格式）
                        if (sheetCellName.Contains($"_{languageName}"))
                        {
                            hasLanguageData = true;
                            sheetCellName = $"{sheetCellName.Replace($"_{languageName}", "")}";
                        }
                        else
                        {
                            continue;
                        }
                    }
                    // 获取字段信息
                    FieldInfo fieldInfo = type.GetField(sheetCellName);
                    if (fieldInfo == null)
                    {
                        LogUtil.LogError($"未找到字段：第{column}列 - {sheetCellName}");
                        continue;
                    }

                    // 读取单元格数据
                    string textData = sheet.Cells[row, column].Text;

                    // 空值处理：没有值的字段不保存到json
                    if (textData.IsNull())
                    {
                        continue;
                    }

                    // 类型转换并赋值
                    object value = Convert.ChangeType(textData, fieldInfo.FieldType);
                    dictData[sheetCellName] = value;
                }

                if (dictData.Count > 0)
                {
                    listData.Add(dictData);
                }
            }

            // 跳过没有当前语种数据的语言
            if (!hasLanguageData)
                continue;

            // 生成输出路径并写入文件
            string jsonPath = $"{jsonFolderPath}/Language_{sheet.Name}_{languageName}.txt";
            if (!File.Exists(jsonPath))
            {
                File.Create(jsonPath).Dispose();
            }

            string jsonData = JsonUtil.ToJsonByNet(listData.ToArray());
            File.WriteAllText(jsonPath, jsonData);
        }
    }

    /// <summary>
    /// 普通表转 Json - 标准数据格式处理
    /// 支持 [language] 标记的列名处理
    /// </summary>
    /// <param name="sheet">Excel 工作表</param>
    /// <param name="assembly">程序集</param>
    /// <param name="type">Bean 类型</param>
    /// <param name="jsonFolderPath">Json 输出目录</param>
    private static void ExcelToJsonItemForBase(ExcelWorksheet sheet, Assembly assembly, Type type, string jsonFolderPath)
    {
        int columnCount = sheet.Dimension.End.Column; // 列数
        int rowCount = sheet.Dimension.End.Row;       // 行数

        List<object> listData = new List<object>();

        // 从第 4 行开始读取数据（前 3 行为元数据）
        for (int row = 4; row <= rowCount; row++)
        {
            object o = assembly.CreateInstance(type.ToString());

            // 遍历所有列
            for (int column = 1; column <= columnCount; column++)
            {
                string sheetCellName = sheet.Cells[1, column].Text;

                // 移除 [language] 标记
                if (sheetCellName.Contains("[language]"))
                {
                    sheetCellName = sheetCellName.Replace("[language]", "");
                }
                else if (sheetCellName.Contains("[language_1]"))
                {
                    sheetCellName = sheetCellName.Replace("[language_1]", "");
                }
                else if (sheetCellName.Contains("[language_2]"))
                {
                    sheetCellName = sheetCellName.Replace("[language_2]", "");
                }

                // 获取字段信息
                FieldInfo fieldInfo = type.GetField(sheetCellName);
                if (fieldInfo == null)
                {
                    LogUtil.LogError($"未找到字段：第{column}列 - {sheetCellName}");
                    continue;
                }

                // 读取单元格数据
                string textData = sheet.Cells[row, column].Text;

                // 空值处理：数值类型默认为 0
                if (textData.IsNull())
                {
                    if (fieldInfo.FieldType == typeof(int)
                        || fieldInfo.FieldType == typeof(float)
                        || fieldInfo.FieldType == typeof(double)
                        || fieldInfo.FieldType == typeof(long))
                    {
                        textData = "0";
                    }
                }

                // 类型转换并赋值
                object value = Convert.ChangeType(textData, fieldInfo.FieldType);
                type.GetField(sheetCellName).SetValue(o, value);
            }

            listData.Add(o);
        }

        // 生成输出路径并写入文件
        string jsonPath = $"{jsonFolderPath}/{sheet.Name}.txt";
        if (!File.Exists(jsonPath))
        {
            File.Create(jsonPath).Dispose();
        }

        string jsonData = JsonUtil.ToJsonByNet(listData.ToArray());
        File.WriteAllText(jsonPath, jsonData);
    }
}