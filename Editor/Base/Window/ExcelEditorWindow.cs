using DG.Tweening.Plugins.Core.PathCore;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UIElements;

public class ExcelEditorWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/处理Excel")]
    static void CreateWindows()
    {
        EditorWindow.GetWindowWithRect(typeof(ExcelEditorWindow), new Rect(0, 0, 600, 800));
    }

    public string queryStr;//查询
    public string excelFolderPath = "";
    public string entityFolderPath = "";
    public string entityFolderPathForFrameWork = "";
    public string jsonFolderPath = "";

    public FileInfo[] queryFileInfos;

    protected Vector2 scrollPosForList = new Vector2();

    [InitializeOnLoadMethod]
    public static void Init()
    {
        ToolbarExtension.ToolbarZoneLeftAlign += OnToolbarGUI;
    }

    static void OnToolbarGUI(VisualElement rootVisualElement)
    {
        var refresh = new EditorToolbarDropdown();
        refresh.text = "处理Excel";
        refresh.clicked += () =>
        {
            CreateWindows();
        };

        //var m_TextElement = refresh.Q<TextElement>(className: "unity-editor-toolbar-element__label");
        //var ArrowElement = refresh.Q(className: "unity-icon-arrow");

        //m_TextElement.style.width = 100;
        //m_TextElement.style.textOverflow = TextOverflow.Clip;
        //m_TextElement.style.unityTextAlign = TextAnchor.MiddleCenter;
        //ArrowElement.style.display = DisplayStyle.None;

        rootVisualElement.Add(refresh);
    }

    private void OnEnable()
    {
        excelFolderPath = Application.dataPath + "/Data/Excel";
        entityFolderPath = Application.dataPath + "/Scrpits/Bean/MVC/Game";
        entityFolderPathForFrameWork = Application.dataPath + "/FrameWork/Scrpits/Bean/MVC";
        //jsonFolderPath = Application.streamingAssetsPath + "/JsonText";
        jsonFolderPath = Application.dataPath + "/Resources/JsonText";
    }

    private void OnDestroy()
    {
        DestroyImmediate(GameDataHandler.Instance.gameObject);
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("选择Excel所在文件夹", 200))
        {
            excelFolderPath = EditorUI.GetFolderPanel("选择目录");
        }
        if (EditorUI.GUIButton("打开所在文件夹", 200))
        {
            EditorUI.OpenFolder(excelFolderPath);
        }
        GUILayout.EndHorizontal();
        excelFolderPath = EditorUI.GUIEditorText(excelFolderPath, 500);
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("选择Entity所在文件夹", 200))
        {
            entityFolderPath = EditorUI.GetFolderPanel("选择目录");
        }
        if (EditorUI.GUIButton("打开所在文件夹", 200))
        {
            EditorUI.OpenFolder(entityFolderPath);
        }
        GUILayout.EndHorizontal();
        entityFolderPath = EditorUI.GUIEditorText(entityFolderPath, 500);
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("选择Json文本所在文件夹", 200))
        {
            jsonFolderPath = EditorUI.GetFolderPanel("选择目录");
        }
        if (EditorUI.GUIButton("打开所在文件夹", 200))
        {
            EditorUI.OpenFolder(jsonFolderPath);
        }
        GUILayout.EndHorizontal();
        jsonFolderPath = EditorUI.GUIEditorText(jsonFolderPath, 500);
        GUILayout.Space(10);
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("生成所有相关Entity", 200))
        {
            CreateEntities();
        }
        if (EditorUI.GUIButton("所有Excel转Json文本", 200))
        {
            ExcelToJson();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        UIForListExcel();
    }

    /// <summary>
    /// Excel列表
    /// </summary>
    public void UIForListExcel()
    {
        if (EditorUI.GUIButton("刷新Excel", 500))
        {
            queryFileInfos = FileUtil.GetFilesByPath(excelFolderPath);
            if (!queryStr.IsNull())
            {
                List<FileInfo> listQueryData = new List<FileInfo>();
                for (int i = 0; i < queryFileInfos.Length; i++)
                {
                    var itemInfo = queryFileInfos[i];
                    if (itemInfo.Name.ToLower().Contains(queryStr.ToLower()))
                    {
                        listQueryData.Add(itemInfo);
                    }
                    else
                    {
                        ExcelUtil.GetExcelPackage(itemInfo, (ep) =>
                        {
                            //获得所有工作表
                            ExcelWorksheets workSheets = ep.Workbook.Worksheets;
                            //workSheets.Add("IgnoreErrors");
                            List<object> lst = new List<object>();
                            //遍历所有工作表
                            for (int w = 1; w <= workSheets.Count; w++)
                            {
                                //当前工作表 
                                ExcelWorksheet sheet = workSheets[w];
                                if (sheet.Name.ToLower().Contains(queryStr.ToLower()) || sheet.Name.ToLower().Contains(queryStr.Replace("Cfg", "").ToLower()))
                                {
                                    listQueryData.Add(itemInfo);
                                    break;
                                }
                            }
                        });
                    }
                }
                queryFileInfos = listQueryData.ToArray();
            }
            queryFileInfos = queryFileInfos.OrderByDescending(f => f.LastWriteTime).ToArray();
        }

        GUILayout.BeginHorizontal();
        EditorUI.GUIText("搜索", 50);
        queryStr = EditorUI.GUIEditorText(queryStr, 500);
        if (queryFileInfos == null)
        {
            queryFileInfos = FileUtil.GetFilesByPath(excelFolderPath);
            queryFileInfos = queryFileInfos.OrderByDescending(f => f.LastWriteTime).ToArray();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (queryFileInfos != null)
        {
            scrollPosForList = EditorGUILayout.BeginScrollView(scrollPosForList);
            for (int i = 0; i < queryFileInfos.Length; i++)
            {
                FileInfo fileInfo = queryFileInfos[i];
                if (fileInfo.Name.Contains(".meta"))
                    continue;
                string filePath = fileInfo.FullName;
                if (filePath.Contains(".meta"))
                    continue;
                if (filePath.Contains("~$"))
                    continue;
                GUILayout.BeginHorizontal();
                if (EditorUI.GUIButton("生成Entity"))
                {
                    CreateEntitiesItem(fileInfo);
                }
                if (EditorUI.GUIButton("生成Json文本"))
                {
                    ExcelToJsonItem(fileInfo);
                }
                if (EditorUI.GUIButton($"{fileInfo.Name}", 400))
                {
                    EditorUI.OpenFolder(fileInfo.FullName);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    #region Json处理
    /// <summary>
    /// Excel转Json文本
    /// </summary>
    public void ExcelToJson()
    {
        if (excelFolderPath.IsNull())
        {
            LogUtil.LogError("Excel文件目录为null");
            return;
        }
        if (jsonFolderPath.IsNull())
        {
            LogUtil.LogError("Json文件目录为null");
            return;
        }
        FileInfo[] fileInfos = FileUtil.GetFilesByPath(excelFolderPath);
        for (int i = 0; i < fileInfos.Length; i++)
        {
            FileInfo fileInfo = fileInfos[i];
            ExcelToJsonItem(fileInfo);
        }
        EditorUtil.RefreshAsset();
    }

    public void ExcelToJsonItem(FileInfo fileInfo)
    {
        ExcelUtil.GetExcelPackage(fileInfo, (ep) =>
        {
            //获得所有工作表
            ExcelWorksheets workSheets = ep.Workbook.Worksheets;
            //遍历所有工作表
            for (int w = 1; w <= workSheets.Count; w++)
            {
                //当前工作表 
                ExcelWorksheet sheet = workSheets[w];
                Assembly assembly = Assembly.Load("Assembly-CSharp");
                Type type;
                if (fileInfo.Name.Contains("excel_language"))
                {
                    type = assembly.GetType("LanguageBean");
                }
                else
                {
                    type = assembly.GetType(sheet.Name + "Bean");
                }
                if (type == null)
                {
                    LogUtil.LogError("你还没有创建对应的实体类!");
                    return;
                }
                if (!Directory.Exists(jsonFolderPath))
                    Directory.CreateDirectory(jsonFolderPath);
                //如果是多语言表 特殊处理----------------------------------------------------------------------------------------------------------
                if (fileInfo.Name.Contains("excel_language"))
                {
                    ExcelToJsonItemForLanguage(sheet, assembly, type);
                }
                else
                {
                    ExcelToJsonItemForBase(sheet, assembly, type);
                }
            }
            LogUtil.Log($"转换完成 {fileInfo.FullName}");
        });
    }

    public void ExcelToJsonItemForLanguage(ExcelWorksheet sheet, Assembly assembly, Type type)
    {
        List<object> listData = new List<object>();
        string[] languageNames = EnumExtension.GetEnumNames<LanguageEnum>();
        for (int l = 0; l < languageNames.Length; l++)
        {
            bool hasLanguageData = false;
            //初始化集合
            listData.Clear();
            //横排
            int columnCount = sheet.Dimension.End.Column;
            //竖排
            int rowCount = sheet.Dimension.End.Row;
            var languageName = languageNames[l];
            //从第四行开始，前3行分别是属性名字，属性字段，属性描述
            for (int row = 4; row <= rowCount; row++)
            {
                object o = assembly.CreateInstance(type.ToString());
                for (int column = 1; column <= columnCount; column++)
                {
                    string sheetCellName = sheet.Cells[1, column].Text;
                    if (sheetCellName.Equals("remark"))
                    {
                        continue;
                    }
                    if (sheetCellName.Contains("content_"))
                    {
                        if (!languageName.Equals(sheetCellName.Substring("content_".Length)))
                        {
                            continue;
                        }
                        hasLanguageData = true;
                        sheetCellName = "content";
                    }
                    FieldInfo fieldInfo = type.GetField(sheetCellName); //先获得字段信息，方便获得字段类型
                    if (fieldInfo == null)
                    {
                        LogUtil.LogError($"没有找到 第{column}竖排：{sheetCellName}的字段信息");
                        continue;
                    }
                    string textData = sheet.Cells[row, column].Text;
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
                    object value = Convert.ChangeType(textData, fieldInfo.FieldType);
                    type.GetField(sheetCellName).SetValue(o, value);
                }
                listData.Add(o);
            }
            //如果没有当前语种的数据 则不生成
            if (hasLanguageData == false)
                continue;
            //写入json文件
            string jsonPath = $"{jsonFolderPath}/Language_{sheet.Name}_{languageName}.txt";
            if (!File.Exists(jsonPath))
            {
                File.Create(jsonPath).Dispose();
            }
            string jsonData = JsonUtil.ToJsonByNet(listData.ToArray());
            File.WriteAllText(jsonPath, jsonData);
        }
    }

    public void ExcelToJsonItemForBase(ExcelWorksheet sheet, Assembly assembly, Type type)
    {
        //横排
        int columnCount = sheet.Dimension.End.Column;
        //竖排
        int rowCount = sheet.Dimension.End.Row;

        List<object> listData = new List<object>();
        //从第四行开始，前3行分别是属性名字，属性字段，属性描述------------------------------------------------------------------------
        for (int row = 4; row <= rowCount; row++)
        {
            object o = assembly.CreateInstance(type.ToString());
            for (int column = 1; column <= columnCount; column++)
            {
                string sheetCellName = sheet.Cells[1, column].Text;
                if (sheetCellName.Contains("[language]"))
                {
                    sheetCellName = sheetCellName.Replace("[language]", "");
                }
                FieldInfo fieldInfo = type.GetField(sheetCellName); //先获得字段信息，方便获得字段类型
                if (fieldInfo == null)
                {
                    LogUtil.LogError($"没有找到 第{column}竖排：{sheetCellName}的字段信息");
                    continue;
                }
                string textData = sheet.Cells[row, column].Text;
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
                object value = Convert.ChangeType(textData, fieldInfo.FieldType);
                type.GetField(sheetCellName).SetValue(o, value);
            }
            listData.Add(o);
        }
        //写入json文件
        string jsonPath = $"{jsonFolderPath}/{sheet.Name}.txt";
        if (!File.Exists(jsonPath))
        {
            File.Create(jsonPath).Dispose();
        }
        string jsonData = JsonUtil.ToJsonByNet(listData.ToArray());
        File.WriteAllText(jsonPath, jsonData);
    }
    #endregion

    #region  生成Entity
    /// <summary>
    /// 生成相关Entity
    /// </summary>
    public void CreateEntities()
    {
        if (excelFolderPath.IsNull())
        {
            LogUtil.LogError("Excel文件目录为null");
            return;
        }
        if (entityFolderPath.IsNull())
        {
            LogUtil.LogError("Entity文件目录为null");
            return;
        }
        if (entityFolderPathForFrameWork.IsNull())
        {
            LogUtil.LogError("Entity FrameWork文件目录为null");
            return;
        }
        FileInfo[] fileInfos = FileUtil.GetFilesByPath(excelFolderPath);
        for (int i = 0; i < fileInfos.Length; i++)
        {
            FileInfo fileInfo = fileInfos[i];
            CreateEntitiesItem(fileInfo);
        }
        AssetDatabase.Refresh();
    }

    public void CreateEntitiesItem(FileInfo fileInfo)
    {
        ExcelUtil.GetExcelPackage(fileInfo, (ep) =>
        {
            bool isFrameWork = false;
            bool isLangauge = false;
            if (fileInfo.Name.Contains("_FrameWork"))
            {
                isFrameWork = true;
            }
            if (fileInfo.Name.Contains("excel_language"))
            {
                isLangauge = true;
            }
            //获得所有工作表
            ExcelWorksheets workSheets = ep.Workbook.Worksheets;
            //遍历所有工作表
            for (int w = 1; w <= workSheets.Count; w++)
            {
                CreateEntityPartial(workSheets[w], isFrameWork, isLangauge);
                CreateEntity(workSheets[w], isFrameWork, isLangauge);
            }
            AssetDatabase.Refresh();
            LogUtil.Log("生成完成");
        });
    }

    void CreateEntityPartial(ExcelWorksheet sheet, bool isFrameWork, bool isLangauge)
    {
        string dir;
        if (isFrameWork)
        {
            dir = entityFolderPathForFrameWork;
        }
        else
        {
            dir = entityFolderPath;
        }
        string path;
        if (isLangauge)
        {
            path = $"{dir}/LanguageBeanPartial.cs";
        }
        else
        {
            path = $"{dir}/{sheet.Name}BeanPartial.cs";
        }
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"using System;");
        sb.AppendLine($"using System.Collections.Generic;");
        //创建bean
        sb.AppendLine($"public partial class {sheet.Name}Bean");
        sb.AppendLine("{");
        sb.AppendLine("}");

        //创建cfg
        sb.AppendLine($"public partial class {sheet.Name}Cfg");
        sb.AppendLine("{");
        sb.AppendLine("}");
        try
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (!File.Exists(path))
            {
                File.Create(path).Dispose(); //避免资源占用
                File.WriteAllText(path, sb.ToString());
            }
        }
        catch (System.Exception e)
        {
            LogUtil.LogError($"Excel转json时创建对应的实体类出错，实体类为：{sheet.Name},e:{e.Message}");
        }
        AssetDatabase.Refresh();
    }

    void CreateEntity(ExcelWorksheet sheet, bool isFrameWork, bool isLangauge)
    {
        string dir;
        if (isFrameWork)
        {
            dir = entityFolderPathForFrameWork;
        }
        else
        {
            dir = entityFolderPath;
        }

        StringBuilder sb = new StringBuilder();
        string path;
        if (isLangauge)
        {
            path = $"{dir}/LanguageBean.cs";
            string beanPath = Application.dataPath + "/FrameWork/Editor/ScrpitsTemplates/Excel_LanguageEntity.txt";
            EditorUtil.CreateClass(new Dictionary<string, string>(), beanPath,"LanguageBean", "Assets/FrameWork/Scrpits/Bean/MVC");
        }
        else
        {
            path = $"{dir}/{sheet.Name}Bean.cs";
            sb.AppendLine($"using System;");
            sb.AppendLine($"using System.Collections.Generic;");
            sb.AppendLine($"using Newtonsoft.Json;");
            //创建bean
            sb.AppendLine($"[Serializable]");
            sb.AppendLine($"public partial class {sheet.Name}Bean : BaseBean");
            sb.AppendLine("{");

            //key的类型0为默认long 1为int 2为string
            string keyTypeName = "long";
            string keyName = "id";
            //遍历sheet首行每个字段描述的值
            for (int i = 1; i <= sheet.Dimension.End.Column; i++)
            {
                string cellName = sheet.Cells[1, i].Text;
                string typeName = sheet.Cells[2, i].Text;
                string remarkName = sheet.Cells[3, i].Text;
                if (remarkName.Contains("(key)"))
                {
                    keyName = cellName;
                    keyTypeName = typeName;
                }

                if (cellName.Equals("id"))
                    continue;
                sb.AppendLine("\t/// <summary>");
                sb.AppendLine($"\t///{remarkName}");
                sb.AppendLine("\t/// </summary>");

                //如果是多语言指向
                if (cellName.Contains("[language]"))
                {
                    string originCellName = cellName.Replace("[language]", "");
                    sb.AppendLine($"\tpublic {typeName} {originCellName};");
                    sb.AppendLine($"\t[JsonIgnore]");
                    sb.AppendLine($"\tpublic string {originCellName}_language {{ get {{ return TextHandler.Instance.GetTextById({sheet.Name}Cfg.fileName, {originCellName}); }} }}");
                }
                else
                {
                    sb.AppendLine($"\tpublic {typeName} {cellName};");
                }
            }
            sb.AppendLine("}");

            //创建cfg
            sb.AppendLine($"public partial class {sheet.Name}Cfg : BaseCfg<{keyTypeName}, {sheet.Name}Bean>");
            sb.AppendLine("{");

            sb.AppendLine($"\tpublic static string fileName = \"{sheet.Name}\";");
            sb.AppendLine($"\tprotected static Dictionary<{keyTypeName}, {sheet.Name}Bean> dicData = null;");

            sb.AppendLine($"\tpublic static Dictionary<{keyTypeName}, {sheet.Name}Bean> GetAllData()");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tif (dicData == null)");
            sb.AppendLine("\t\t{");
            sb.AppendLine($"\t\t\tvar arrayData = GetAllArrayData();");
            sb.AppendLine($"\t\t\tInitData(arrayData);");
            sb.AppendLine("\t\t}");
            sb.AppendLine($"\t\treturn dicData;");
            sb.AppendLine("\t}");

            sb.AppendLine($"\tpublic static {sheet.Name}Bean[] GetAllArrayData()");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tif (arrayData == null)");
            sb.AppendLine("\t\t{");
            sb.AppendLine($"\t\t\tarrayData = GetInitData(fileName);");
            sb.AppendLine("\t\t}");
            sb.AppendLine($"\t\treturn arrayData;");
            sb.AppendLine("\t}");

            sb.AppendLine($"\tpublic static {sheet.Name}Bean GetItemData({keyTypeName} key)");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tif (dicData == null)");
            sb.AppendLine("\t\t{");
            sb.AppendLine($"\t\t\t{sheet.Name}Bean[] arrayData = GetInitData(fileName);");

            sb.AppendLine($"\t\t\tInitData(arrayData);");
            sb.AppendLine("\t\t}");
            sb.AppendLine($"\t\treturn GetItemData(key, dicData);");
            sb.AppendLine("\t}");

            sb.AppendLine($"\tpublic static void InitData({sheet.Name}Bean[] arrayData)");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tdicData = new Dictionary<long, {sheet.Name}Bean>();");
            sb.AppendLine($"\t\tfor (int i = 0; i < arrayData.Length; i++)");
            sb.AppendLine("\t\t{");
            sb.AppendLine($"\t\t\t{sheet.Name}Bean itemData = arrayData[i];");
            sb.AppendLine($"\t\t\tdicData.Add(itemData.id, itemData);");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");

            sb.AppendLine("}");
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (!File.Exists(path))
                {
                    File.Create(path).Dispose(); //避免资源占用
                }
                File.WriteAllText(path, sb.ToString());
            }
            catch (System.Exception e)
            {
                LogUtil.LogError($"Excel转json时创建对应的实体类出错，实体类为：{sheet.Name},e:{e.Message}");
            }
        }
        AssetDatabase.Refresh();
    }
    #endregion
}