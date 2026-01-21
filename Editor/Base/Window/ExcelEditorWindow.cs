
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

public class ExcelEditorWindow : EditorWindow
{
 [MenuItem("Custom/工具弹窗/处理Excel")]
    static void CreateWindows()
    {
        var window = EditorWindow.GetWindow<ExcelEditorWindow>();
        window.titleContent = new GUIContent("Excel处理工具");
        window.minSize = new Vector2(650, 800);
        window.Show();
    }

    public string queryStr;//查询
    public string excelFolderPath = "";
    public string entityFolderPath = "";
    public string entityFolderPathForFrameWork = "";
    public string jsonFolderPath = "";

    public FileInfo[] queryFileInfos;

    protected Vector2 scrollPosForList = Vector2.zero;
   protected  Vector2 fileListScroll = Vector2.zero;
    // 添加一个变量来跟踪窗口是否已初始化
    private bool stylesInitialized = false;
    
    // 自定义样式
    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private GUIStyle textFieldStyle;
    private GUIStyle listItemStyle;
    private GUIStyle sectionHeaderStyle;

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

        rootVisualElement.Add(refresh);
    }

    private void OnEnable()
    {
        excelFolderPath = Application.dataPath + "/Data/Excel";
        entityFolderPath = Application.dataPath + "/Scrpits/Bean/MVC/Game";
        entityFolderPathForFrameWork = Application.dataPath + "/FrameWork/Scrpits/Bean/MVC";
        jsonFolderPath = Application.dataPath + "/Resources/JsonText";
        
        // 初始化样式
        InitializeStyles();
    }
    
    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        // 分区标题样式
        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            fixedHeight = 28,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 0, 8, 8),
            normal = { textColor = EditorGUIUtility.isProSkin ? 
                new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f) }
        };
        
        // 盒子样式
        boxStyle = new GUIStyle("HelpBox")
        {
            padding = new RectOffset(15, 15, 15, 15),
            margin = new RectOffset(5, 5, 10, 10)
        };
        
        // 按钮样式
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            padding = new RectOffset(12, 12, 8, 8),
            margin = new RectOffset(2, 2, 2, 2),
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        
        // 文本框样式
        textFieldStyle = new GUIStyle(EditorStyles.textField)
        {
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(2, 2, 2, 2)
        };
        
        // 列表项样式
        listItemStyle = new GUIStyle()
        {
            padding = new RectOffset(5, 5, 5, 5),
            margin = new RectOffset(2, 2, 2, 2),
            normal = { background = MakeTex(2, 2, EditorGUIUtility.isProSkin ? 
                new Color(0.3f, 0.3f, 0.3f, 0.8f) : new Color(0.95f, 0.95f, 0.95f)) }
        };
        
        stylesInitialized = true;
    }
    
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OnDestroy()
    {
        DestroyImmediate(GameDataHandler.Instance.gameObject);
    }

    private void OnGUI()
    {
        // 确保样式已初始化
        if (!stylesInitialized)
        {
            InitializeStyles();
        }
        EditorGUILayout.BeginScrollView(scrollPosForList);

        // 第一部分：路径设置
        DrawPathSettings();
        
        GUILayout.Space(10);
        
        // 第二部分：批量操作
        DrawBatchOperations();
        
        GUILayout.Space(10);
        
        // 第三部分：Excel文件列表
        DrawExcelList();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawPathSettings()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("路径设置", sectionHeaderStyle);
        GUILayout.Space(10);
        
        // Excel文件夹路径
        DrawPathField("Excel文件夹路径:", ref excelFolderPath, "选择Excel目录");
        
        GUILayout.Space(10);
        
        // Entity文件夹路径
        DrawPathField("Entity文件夹路径:", ref entityFolderPath, "选择Entity目录");
        
        GUILayout.Space(10);
        
        // Json文件夹路径
        DrawPathField("Json文件夹路径:", ref jsonFolderPath, "选择Json目录");
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawPathField(string label, ref string path, string dialogTitle)
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        
        // 路径文本框
        path = EditorGUILayout.TextField(path, textFieldStyle, GUILayout.Height(25));
        
        // 按钮组
        if (GUILayout.Button("选择", buttonStyle, GUILayout.Width(60), GUILayout.Height(25)))
        {
            string newPath = EditorUI.GetFolderPanel(dialogTitle);
            if (!string.IsNullOrEmpty(newPath))
            {
                path = newPath;
                GUI.FocusControl(null); // 移除焦点，刷新UI
            }
        }
        
        if (GUILayout.Button("打开", buttonStyle, GUILayout.Width(60), GUILayout.Height(25)))
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                EditorUI.OpenFolder(path);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "文件夹不存在，请先选择有效的路径", "确定");
            }
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
    
    private void DrawBatchOperations()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("批量操作", sectionHeaderStyle);
        GUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        // 添加图标
        GUIContent entityContent = new GUIContent(" 生成所有Entity", 
            EditorGUIUtility.IconContent("d_CreateAddNew").image);
        
        if (GUILayout.Button(entityContent, buttonStyle, GUILayout.Width(200), GUILayout.Height(35)))
        {
            CreateEntities();
        }
        
        GUILayout.Space(20);
        
        GUIContent jsonContent = new GUIContent(" 所有Excel转Json", 
            EditorGUIUtility.IconContent("d_TextAsset Icon").image);
        
        if (GUILayout.Button(jsonContent, buttonStyle, GUILayout.Width(200), GUILayout.Height(35)))
        {
            ExcelToJson();
        }
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }

   private void DrawExcelList()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        
        // 搜索和刷新区域
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Excel文件列表", sectionHeaderStyle, GUILayout.Width(120));
        GUILayout.FlexibleSpace();
        
        // 搜索框
        EditorGUILayout.BeginHorizontal(GUILayout.Width(250));
        EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
        queryStr = EditorGUILayout.TextField(queryStr, textFieldStyle, GUILayout.Height(25));
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        GUIContent refreshContent = new GUIContent(" 刷新", 
            EditorGUIUtility.IconContent("d_Refresh").image);
        
        if (GUILayout.Button(refreshContent, buttonStyle, GUILayout.Width(100), GUILayout.Height(25)))
        {
            RefreshFileList();
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(15);
        
        // 文件列表 - 使用固定高度的滚动区域
        if (queryFileInfos == null || queryFileInfos.Length == 0)
        {
            EditorGUILayout.HelpBox("没有找到Excel文件，请检查路径设置", MessageType.Info);
        }
        else
        {
            // 计算文件列表区域的高度（窗口高度减去其他部分的高度）
            float listHeight = Mathf.Max(300, position.height - 450);
            
            // 文件列表滚动区域
            fileListScroll = EditorGUILayout.BeginScrollView(fileListScroll, 
                GUILayout.Height(listHeight));
            
            DrawFileList();
            
            EditorGUILayout.EndScrollView();
            
            // 统计信息
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"共找到 {queryFileInfos.Length} 个Excel文件", 
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void RefreshFileList()
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
                        ExcelWorksheets workSheets = ep.Workbook.Worksheets;
                        for (int w = 1; w <= workSheets.Count; w++)
                        {
                            ExcelWorksheet sheet = workSheets[w];
                            if (sheet.Name.ToLower().Contains(queryStr.ToLower()) ||
                                sheet.Name.ToLower().Contains(queryStr.Replace("Cfg", "").ToLower()))
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

   private void DrawFileList()
    {
        int validFileCount = 0;
        
        for (int i = 0; i < queryFileInfos.Length; i++)
        {
            FileInfo fileInfo = queryFileInfos[i];
            if (fileInfo.Name.Contains(".meta") || fileInfo.Name.Contains("~$"))
                continue;
                
            validFileCount++;
            
            EditorGUILayout.BeginVertical(listItemStyle);
            
            // 文件信息行
            EditorGUILayout.BeginHorizontal();
            
            // 文件图标和名称
            EditorGUILayout.BeginHorizontal(GUILayout.Width(300));
            GUILayout.Space(5);
            GUILayout.Label(EditorGUIUtility.IconContent("d_SpreadsheetAsset Icon"), GUILayout.Width(20));
            GUILayout.Space(5);

            EditorGUILayout.LabelField(fileInfo.Name, EditorStyles.boldLabel, GUILayout.Width(400));
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            
            // 文件大小和时间
            EditorGUILayout.BeginHorizontal(GUILayout.Width(200));
            string size = FormatFileSize(fileInfo.Length);
            EditorGUILayout.LabelField(size, EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField(fileInfo.LastWriteTime.ToString("MM-dd HH:mm"), 
                EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndHorizontal();
            
            // 操作按钮行
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUIContent entityBtnContent = new GUIContent("生成Entity", 
                EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            
            if (GUILayout.Button(entityBtnContent, buttonStyle, GUILayout.Width(100), GUILayout.Height(25)))
            {
                CreateEntitiesItem(fileInfo);
            }
            
            GUILayout.Space(10);
            
            GUIContent jsonBtnContent = new GUIContent("生成Json", 
                EditorGUIUtility.IconContent("d_TextAsset Icon").image);
            
            if (GUILayout.Button(jsonBtnContent, buttonStyle, GUILayout.Width(100), GUILayout.Height(25)))
            {
                ExcelToJsonItem(fileInfo);
            }
            
            GUILayout.Space(10);
            
            GUIContent openBtnContent = new GUIContent("打开", 
                EditorGUIUtility.IconContent("d_FolderOpened Icon").image);
            
            if (GUILayout.Button(openBtnContent, buttonStyle, GUILayout.Width(80), GUILayout.Height(25)))
            {
                System.Diagnostics.Process.Start(fileInfo.FullName);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            // 分隔线（除了最后一个）
            if (i < queryFileInfos.Length - 1)
            {
                GUILayout.Space(5);
                Rect separatorRect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(separatorRect, EditorGUIUtility.isProSkin ? 
                    new Color(0.4f, 0.4f, 0.4f, 0.5f) : new Color(0.8f, 0.8f, 0.8f, 0.5f));
                GUILayout.Space(5);
            }
        }
        
        // 如果没有有效文件，显示提示
        if (validFileCount == 0)
        {
            EditorGUILayout.HelpBox("没有找到有效的Excel文件", MessageType.Warning);
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Excel列表（保持原有方法，但UI已在DrawFileList中实现）
    /// </summary>
    public void UIForListExcel()
    {
        // 此方法的功能已迁移到DrawExcelList和DrawFileList中
        // 保留此方法以避免破坏原有代码结构
        DrawExcelList();
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
        EditorUtility.DisplayDialog("完成", "所有Excel文件已成功转换为Json文本", "确定");
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
        EditorUtility.DisplayDialog("完成", "所有Entity文件已生成", "确定");
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
            EditorUtil.CreateClass(new Dictionary<string, string>(), beanPath, "LanguageBean", "Assets/FrameWork/Scrpits/Bean/MVC");
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