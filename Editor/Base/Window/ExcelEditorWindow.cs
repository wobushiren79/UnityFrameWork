
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

/// <summary>
/// Excel 编辑器窗口 - 用于处理 Excel 文件转 Entity 和 Json
/// 提供批量操作和单个文件操作功能
/// </summary>
public class ExcelEditorWindow : EditorWindow
{
    #region 菜单项与窗口创建

    /// <summary>
    /// 菜单项：在 Unity 菜单栏创建"处理 Excel"入口
    /// 路径：Custom/工具弹窗/处理 Excel
    /// </summary>
    [MenuItem("Custom/工具弹窗/处理 Excel")]
    private static void CreateWindow()
    {
        var window = EditorWindow.GetWindow<ExcelEditorWindow>();
        window.titleContent = new GUIContent("Excel 处理工具");
        window.minSize = new Vector2(650, 800); // 设置窗口最小尺寸
        window.Show();
    }

    #endregion

    #region 成员变量

    // ==================== 路径配置 ====================
    /// <summary>搜索关键词 - 用于过滤 Excel 文件名或工作表名</summary>
    public string queryStr;

    /// <summary>Excel 文件存储目录路径</summary>
    public string excelFolderPath = "";

    /// <summary>Entity 脚本生成目录路径（游戏逻辑层）</summary>
    public string entityFolderPath = "";

    /// <summary>Entity 脚本生成目录路径（框架层）</summary>
    public string entityFolderPathForFrameWork = "";

    /// <summary>Json 输出文件存储目录路径</summary>
    public string jsonFolderPath = "";

    // ==================== 文件列表数据 ====================
    /// <summary>查询到的 Excel 文件信息数组</summary>
    public FileInfo[] queryFileInfos;

    /// <summary>默认显示最近修改的文件数量</summary>
    public int maxDisplayCount = 10;

    /// <summary>是否显示所有文件（false 时仅显示最近 N 条）</summary>
    public bool showAllFiles = false;

    // ==================== UI 滚动状态 ====================
    /// <summary>主滚动区域滚动位置</summary>
    protected Vector2 scrollPosForList = Vector2.zero;

    /// <summary>文件列表滚动位置</summary>
    protected Vector2 fileListScroll = Vector2.zero;

    // ==================== 样式资源 ====================
    /// <summary>样式是否已初始化标记</summary>
    private bool stylesInitialized = false;

    /// <summary>分组框样式 - 用于区域划分</summary>
    private GUIStyle boxStyle;

    /// <summary>按钮样式 - 统一按钮外观</summary>
    private GUIStyle buttonStyle;

    /// <summary>文本输入框样式</summary>
    private GUIStyle textFieldStyle;

    /// <summary>列表项背景样式</summary>
    private GUIStyle listItemStyle;

    /// <summary>分区标题样式</summary>
    private GUIStyle sectionHeaderStyle;

    #endregion

    #region 工具栏集成

    /// <summary>
    /// 初始化时注册工具栏事件
    /// Unity 6000.3 以下版本使用自定义工具栏
    /// </summary>
    [InitializeOnLoadMethod]
    public static void Init()
    {
        #if UNITY_6000_3_OR_NEWER
        // Unity 6000.3+ 使用新的 MainToolbarElement 特性
        #else
        ToolbarExtension.ToolbarZoneLeftAlign += OnToolbarGUI;
        #endif
    }

    #if UNITY_6000_3_OR_NEWER
    /// <summary>
    /// Unity 6000.3+ 主工具栏元素
    /// defaultDockPosition: 工具栏停靠位置 (Left/Middle/Right)
    /// </summary>
    [MainToolbarElement("自定义标题/处理 Excel", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement CreateSettingsButton()
    {
        var content = new MainToolbarContent("处理 Excel");
        return new MainToolbarButton(content, () => CreateWindow());
    }
    #endif

    /// <summary>
    /// 旧版工具栏 UI 渲染（Unity 6000.3 以下）
    /// </summary>
    static void OnToolbarGUI(VisualElement rootVisualElement)
    {
        var refresh = new EditorToolbarDropdown();
        refresh.text = "处理 Excel";
        refresh.clicked += () => CreateWindow();
        rootVisualElement.Add(refresh);
    }

    #endregion

    #region Unity 生命周期

    /// <summary>
    /// 窗口启用时初始化路径和样式
    /// </summary>
    private void OnEnable()
    {
        // 初始化默认路径
        excelFolderPath = Application.dataPath + "/Data/Excel";
        entityFolderPath = Application.dataPath + "/Scripts/Bean/MVC/Game";
        entityFolderPathForFrameWork = Application.dataPath + "/FrameWork/Scripts/Bean/MVC";
        jsonFolderPath = Application.dataPath + "/Resources/JsonText";

        InitializeStyles();
        RefreshFileList();
    }

    /// <summary>
    /// 窗口销毁时清理资源
    /// </summary>
    private void OnDestroy()
    {
        DestroyImmediate(GameDataHandler.Instance.gameObject);
    }

    /// <summary>
    /// GUI 渲染入口 - 绘制窗口全部内容
    /// </summary>
    private void OnGUI()
    {
        // 确保样式已初始化
        if (!stylesInitialized)
        {
            InitializeStyles();
        }

        // 主滚动区域
        EditorGUILayout.BeginScrollView(scrollPosForList);

        // 区域 1：路径设置
        DrawPathSettings();

        GUILayout.Space(10);

        // 区域 2：批量操作
        DrawBatchOperations();

        GUILayout.Space(10);

        // 区域 3：Excel 文件列表
        DrawExcelList();

        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region 样式初始化

    /// <summary>
    /// 初始化所有自定义 UI 样式
    /// 使用单例模式确保只初始化一次
    /// </summary>
    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        // 分区标题样式 - 大号加粗字体，自适应明暗主题
        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            fixedHeight = 28,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 0, 8, 8),
            normal = { textColor = EditorGUIUtility.isProSkin ?
                new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f) }
        };

        // 分组框样式 - 使用 HelpBox 背景
        boxStyle = new GUIStyle("HelpBox")
        {
            padding = new RectOffset(15, 15, 15, 15),
            margin = new RectOffset(5, 5, 10, 10)
        };

        // 按钮样式 - 加粗字体，增加内边距
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            padding = new RectOffset(12, 12, 8, 8),
            margin = new RectOffset(2, 2, 2, 2),
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        // 文本框样式 - 增加内边距提升点击体验
        textFieldStyle = new GUIStyle(EditorStyles.textField)
        {
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(2, 2, 2, 2)
        };

        // 列表项样式 - 动态背景色，区分明暗主题
        listItemStyle = new GUIStyle()
        {
            padding = new RectOffset(5, 5, 5, 5),
            margin = new RectOffset(2, 2, 2, 2),
            normal = { background = MakeTex(2, 2, EditorGUIUtility.isProSkin ?
                new Color(0.3f, 0.3f, 0.3f, 0.8f) : new Color(0.95f, 0.95f, 0.95f)) }
        };

        stylesInitialized = true;
    }

    /// <summary>
    /// 创建纯色纹理 - 用于 GUI 背景
    /// </summary>
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

    #endregion

    #region UI 绘制 - 路径设置区域

    /// <summary>
    /// 绘制路径设置区域 - 包含 Excel、Entity、Json 三个路径配置
    /// </summary>
    private void DrawPathSettings()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("📁 路径设置", sectionHeaderStyle);
        GUILayout.Space(10);

        // Excel 文件夹路径
        DrawPathField("Excel 文件夹路径:", ref excelFolderPath, "选择 Excel 目录");

        GUILayout.Space(10);

        // Entity 文件夹路径
        DrawPathField("Entity 文件夹路径:", ref entityFolderPath, "选择 Entity 目录");

        GUILayout.Space(10);

        // Json 文件夹路径
        DrawPathField("Json 文件夹路径:", ref jsonFolderPath, "选择 Json 目录");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制单个路径配置行 - 包含标签、文本框、选择/打开按钮
    /// </summary>
    /// <param name="label">字段标签</param>
    /// <param name="path">路径引用</param>
    /// <param name="dialogTitle">文件夹选择对话框标题</param>
    private void DrawPathField(string label, ref string path, string dialogTitle)
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();

        // 路径文本框 - 可手动编辑
        path = EditorGUILayout.TextField(path, textFieldStyle, GUILayout.Height(25));

        // 选择按钮 - 打开文件夹选择对话框
        if (GUILayout.Button("选择", buttonStyle, GUILayout.Width(60), GUILayout.Height(25)))
        {
            string newPath = EditorUI.GetFolderPanel(dialogTitle);
            if (!string.IsNullOrEmpty(newPath))
            {
                path = newPath;
                GUI.FocusControl(null); // 移除焦点，触发 UI 刷新
            }
        }

        // 打开按钮 - 使用系统文件管理器打开目录
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

    #endregion

    #region UI 绘制 - 批量操作区域

    /// <summary>
    /// 绘制批量操作区域 - 提供一键生成 Entity 和 Json 的快捷按钮
    /// </summary>
    private void DrawBatchOperations()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("⚡ 批量操作", sectionHeaderStyle);
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // 生成所有 Entity 按钮
        GUIContent entityContent = new GUIContent(" 📜 生成所有 Entity",
            EditorGUIUtility.IconContent("d_CreateAddNew").image);

        if (GUILayout.Button(entityContent, buttonStyle, GUILayout.Width(200), GUILayout.Height(35)))
        {
            CreateEntities();
        }

        GUILayout.Space(20);

        // Excel 转 Json 按钮
        GUIContent jsonContent = new GUIContent(" 📄 所有 Excel 转 Json",
            EditorGUIUtility.IconContent("d_TextAsset Icon").image);

        if (GUILayout.Button(jsonContent, buttonStyle, GUILayout.Width(200), GUILayout.Height(35)))
        {
            ExcelToJson();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region UI 绘制 - Excel 文件列表区域

    /// <summary>
    /// 绘制 Excel 文件列表区域 - 包含搜索、显示模式切换、文件列表
    /// </summary>
    private void DrawExcelList()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        // --- 顶部工具栏：标题、搜索框、显示模式、刷新按钮 ---
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("📊 Excel 文件列表", sectionHeaderStyle, GUILayout.Width(120));
        GUILayout.FlexibleSpace();

        // 搜索框
        EditorGUILayout.BeginHorizontal(GUILayout.Width(250));
        EditorGUILayout.LabelField("🔍 搜索:", GUILayout.Width(40));
        queryStr = EditorGUILayout.TextField(queryStr, textFieldStyle, GUILayout.Height(25));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 显示模式切换按钮
        GUIContent toggleContent = new GUIContent(showAllFiles ? "📋 显示全部" : "⏱ 仅显示最近 10 条");
        if (GUILayout.Button(toggleContent, buttonStyle, GUILayout.Width(140), GUILayout.Height(25)))
        {
            showAllFiles = !showAllFiles;
            RefreshFileList();
        }

        GUILayout.Space(10);

        // 刷新按钮
        GUIContent refreshContent = new GUIContent(" 🔄 刷新",
            EditorGUIUtility.IconContent("d_Refresh").image);

        if (GUILayout.Button(refreshContent, buttonStyle, GUILayout.Width(100), GUILayout.Height(25)))
        {
            RefreshFileList();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(15);

        // --- 文件列表内容 ---
        if (queryFileInfos == null || queryFileInfos.Length == 0)
        {
            EditorGUILayout.HelpBox("📭 没有找到 Excel 文件，请检查路径设置", MessageType.Info);
        }
        else
        {
            // 动态计算文件列表高度（根据窗口高度自适应）
            float listHeight = Mathf.Max(300, position.height - 450);

            // 文件列表滚动区域
            fileListScroll = EditorGUILayout.BeginScrollView(fileListScroll, GUILayout.Height(listHeight));

            DrawFileList();

            EditorGUILayout.EndScrollView();

            // 底部统计信息
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            string displayMode = showAllFiles ? "全部" : $"最近{maxDisplayCount}条";
            EditorGUILayout.LabelField($"📈 显示模式：{displayMode} | 共 {queryFileInfos.Length} 个 Excel 文件",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 刷新文件列表 - 扫描目录、应用搜索过滤、排序、限制显示数量
    /// </summary>
    private void RefreshFileList()
    {
        // 获取目录下所有文件
        queryFileInfos = FileUtil.GetFilesByPath(excelFolderPath);

        // 应用搜索过滤
        if (!queryStr.IsNull())
        {
            List<FileInfo> listQueryData = new List<FileInfo>();
            for (int i = 0; i < queryFileInfos.Length; i++)
            {
                var itemInfo = queryFileInfos[i];

                // 文件名匹配
                if (itemInfo.Name.ToLower().Contains(queryStr.ToLower()))
                {
                    listQueryData.Add(itemInfo);
                }
                else
                {
                    // 工作表名匹配（异步读取 Excel 内容）
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

        // 按最后修改时间倒序排序（最新的在前）
        queryFileInfos = queryFileInfos.OrderByDescending(f => f.LastWriteTime).ToArray();

        // 限制显示数量（非全部显示模式）
        if (!showAllFiles)
        {
            if (queryFileInfos.Length > maxDisplayCount)
            {
                var limitedArray = new FileInfo[maxDisplayCount];
                Array.Copy(queryFileInfos, limitedArray, maxDisplayCount);
                queryFileInfos = limitedArray;
            }
        }
    }

    /// <summary>
    /// 绘制文件列表内容 - 遍历文件数组，绘制每个文件的详细信息和操作按钮
    /// </summary>
    private void DrawFileList()
    {
        int validFileCount = 0;

        for (int i = 0; i < queryFileInfos.Length; i++)
        {
            FileInfo fileInfo = queryFileInfos[i];

            // 跳过无效文件（.meta 文件和临时文件）
            if (fileInfo.Name.Contains(".meta") || fileInfo.Name.Contains("~$"))
                continue;

            validFileCount++;

            // 文件项容器 - 使用自定义背景样式
            EditorGUILayout.BeginVertical(listItemStyle);

            // --- 第一行：文件信息（图标、名称、大小、修改时间）---
            EditorGUILayout.BeginHorizontal();

            // 左侧：文件图标 + 文件名
            EditorGUILayout.BeginHorizontal(GUILayout.Width(300));
            GUILayout.Space(5);
            GUILayout.Label(EditorGUIUtility.IconContent("d_SpreadsheetAsset Icon"), GUILayout.Width(20));
            GUILayout.Space(5);
            EditorGUILayout.LabelField(fileInfo.Name, EditorStyles.boldLabel, GUILayout.Width(400));
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            // 右侧：文件大小 + 修改时间
            EditorGUILayout.BeginHorizontal(GUILayout.Width(200));
            string size = FormatFileSize(fileInfo.Length);
            EditorGUILayout.LabelField(size, EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField(fileInfo.LastWriteTime.ToString("MM-dd HH:mm"),
                EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();

            // --- 第二行：操作按钮（生成 Entity、生成 Json、打开文件）---
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // 生成 Entity 按钮
            GUIContent entityBtnContent = new GUIContent("📜 生成 Entity",
                EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            if (GUILayout.Button(entityBtnContent, buttonStyle, GUILayout.Width(110), GUILayout.Height(25)))
            {
                CreateEntitiesItem(fileInfo);
            }

            GUILayout.Space(10);

            // 生成 Json 按钮
            GUIContent jsonBtnContent = new GUIContent("📄 生成 Json",
                EditorGUIUtility.IconContent("d_TextAsset Icon").image);
            if (GUILayout.Button(jsonBtnContent, buttonStyle, GUILayout.Width(100), GUILayout.Height(25)))
            {
                ExcelToJsonItem(fileInfo);
            }

            GUILayout.Space(10);

            // 打开文件按钮 - 使用系统默认应用打开
            GUIContent openBtnContent = new GUIContent("📂 打开",
                EditorGUIUtility.IconContent("d_FolderOpened Icon").image);
            if (GUILayout.Button(openBtnContent, buttonStyle, GUILayout.Width(80), GUILayout.Height(25)))
            {
                System.Diagnostics.Process.Start(fileInfo.FullName);
            }

            GUILayout.Space(10);

            // 打开 Json 按钮 - 直接打开生成的 JSON 文件文本
            // 多语言表添加特殊标识
            bool isLanguageFile = fileInfo.Name.Contains("excel_language");
            string jsonBtnText = isLanguageFile ? "📄 打开 Json🌐" : "📄 打开 Json";
            string jsonBtnTooltip = isLanguageFile ? "多语言表 - 点击选择语言版本" : "打开生成的 JSON 文件";
            GUIContent openJsonBtnContent = new GUIContent(jsonBtnText, 
                EditorGUIUtility.IconContent("d_TextAsset Icon").image, 
                jsonBtnTooltip);
            if (GUILayout.Button(openJsonBtnContent, buttonStyle, GUILayout.Width(110), GUILayout.Height(25)))
            {
                OpenJsonFileForExcel(fileInfo);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // --- 分隔线（最后一项除外）---
            if (i < queryFileInfos.Length - 1)
            {
                GUILayout.Space(5);
                Rect separatorRect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(separatorRect, EditorGUIUtility.isProSkin ?
                    new Color(0.4f, 0.4f, 0.4f, 0.5f) : new Color(0.8f, 0.8f, 0.8f, 0.5f));
                GUILayout.Space(5);
            }
        }

        // 无有效文件提示
        if (validFileCount == 0)
        {
            EditorGUILayout.HelpBox("⚠️ 没有找到有效的 Excel 文件", MessageType.Warning);
        }
    }

    /// <summary>
    /// 格式化文件大小 - 将字节转换为人类可读格式 (B/KB/MB/GB)
    /// </summary>
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

    #endregion

    /// <summary>
    /// Excel 列表（保持原有方法，但 UI 已在 DrawFileList 中实现）
    /// </summary>
    public void UIForListExcel()
    {
        // 此方法的功能已迁移到 DrawExcelList 和 DrawFileList 中
        // 保留此方法以避免破坏原有代码结构
        DrawExcelList();
    }

    #region Json 数据处理

    /// <summary>
    /// 打开 Excel 文件对应的 JSON 文件
    /// 如果只有一个工作表则直接打开，多个则弹出选择菜单
    /// </summary>
    /// <param name="fileInfo">Excel 文件信息</param>
    private void OpenJsonFileForExcel(FileInfo fileInfo)
    {
        ExcelUtil.GetExcelPackage(fileInfo, (ep) =>
        {
            ExcelWorksheets workSheets = ep.Workbook.Worksheets;
            List<string> sheetNames = new List<string>();

            // 收集所有工作表名称
            for (int w = 1; w <= workSheets.Count; w++)
            {
                sheetNames.Add(workSheets[w].Name);
            }

            if (sheetNames.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "该 Excel 文件没有包含任何工作表", "确定");
                return;
            }

            // 如果只有一个工作表，直接打开
            bool isLanguage = fileInfo.Name.Contains("excel_language");
            if (sheetNames.Count == 1)
            {
                OpenJsonFileBySheetName(sheetNames[0], isLanguage);
            }
            else if (isLanguage)
            {
                // 多语言表且有多个工作表：直接列出所有可用的语言文件
                GenericMenu menu = new GenericMenu();
                string[] languageNames = EnumExtension.GetEnumNames<LanguageEnum>();
                
                foreach (string sheetName in sheetNames)
                {
                    foreach (string languageName in languageNames)
                    {
                        string jsonPath = $"{jsonFolderPath}/Language_{sheetName}_{languageName}.txt";
                        if (File.Exists(jsonPath))
                        {
                            string path = jsonPath;
                            string fileName = Path.GetFileName(path);
                            menu.AddItem(new GUIContent($"打开 {fileName}"), false, () =>
                            {
                                System.Diagnostics.Process.Start(path);
                            });
                        }
                    }
                }
                
                if (menu.GetItemCount() == 0)
                {
                    EditorUtility.DisplayDialog("提示", "未找到生成的 JSON 文件，请先生成 Json", "确定");
                    return;
                }
                
                menu.ShowAsContext();
            }
            else
            {
                // 非多语言表且有多个工作表，弹出选择菜单
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < sheetNames.Count; i++)
                {
                    string sheetName = sheetNames[i];
                    menu.AddItem(new GUIContent($"打开 {sheetName}.txt"), false, () =>
                    {
                        OpenJsonFileBySheetName(sheetName, false);
                    });
                }
                menu.ShowAsContext();
            }
        });
    }

    /// <summary>
    /// 根据工作表名称打开对应的 JSON 文件
    /// </summary>
    /// <param name="sheetName">工作表名称</param>
    /// <param name="isLanguage">是否为多语言表</param>
    private void OpenJsonFileBySheetName(string sheetName, bool isLanguage)
    {
        string jsonPath;
        
        if (isLanguage)
        {
            // 多语言表：遍历所有语言版本
            string[] languageNames = EnumExtension.GetEnumNames<LanguageEnum>();
            List<string> existingLanguageFiles = new List<string>();
            
            foreach (string languageName in languageNames)
            {
                string path = $"{jsonFolderPath}/Language_{sheetName}_{languageName}.txt";
                if (File.Exists(path))
                {
                    existingLanguageFiles.Add(path);
                }
            }
            
            if (existingLanguageFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", $"未找到 {sheetName} 生成的 JSON 文件，请先生成 Json", "确定");
                return;
            }
            else if (existingLanguageFiles.Count == 1)
            {
                jsonPath = existingLanguageFiles[0];
            }
            else
            {
                // 多个语言版本，弹出选择
                GenericMenu menu = new GenericMenu();
                foreach (string path in existingLanguageFiles)
                {
                    string pathCopy = path; // 捕获变量副本
                    string fileName = Path.GetFileName(pathCopy);
                    menu.AddItem(new GUIContent($"打开 {fileName}"), false, () =>
                    {
                        System.Diagnostics.Process.Start(pathCopy);
                    });
                }
                menu.ShowAsContext();
                return;
            }
        }
        else
        {
            jsonPath = $"{jsonFolderPath}/{sheetName}.txt";
        }

        if (File.Exists(jsonPath))
        {
            System.Diagnostics.Process.Start(jsonPath);
        }
        else
        {
            EditorUtility.DisplayDialog("提示", $"未找到 {sheetName}.txt 文件，请先生成 Json", "确定");
        }
    }

    /// <summary>
    /// 批量转换：将所有 Excel 文件转换为 Json 格式
    /// 用于游戏运行时数据加载
    /// </summary>
    public void ExcelToJson()
    {
        // 路径验证
        if (excelFolderPath.IsNull())
        {
            LogUtil.LogError("Excel 文件目录为 null");
            return;
        }
        if (jsonFolderPath.IsNull())
        {
            LogUtil.LogError("Json 文件目录为 null");
            return;
        }

        // 遍历所有 Excel 文件
        FileInfo[] fileInfos = FileUtil.GetFilesByPath(excelFolderPath);
        for (int i = 0; i < fileInfos.Length; i++)
        {
            FileInfo fileInfo = fileInfos[i];
            ExcelToJsonItem(fileInfo);
        }

        // 刷新资源数据库并显示完成提示
        EditorUtil.RefreshAsset();
        EditorUtility.DisplayDialog("完成", "所有 Excel 文件已成功转换为 Json 文本", "确定");
    }

    /// <summary>
    /// 单个 Excel 文件转 Json
    /// 读取 Excel 所有工作表，根据表名自动匹配对应的 Bean 类型
    /// </summary>
    /// <param name="fileInfo">Excel 文件信息</param>
    public void ExcelToJsonItem(FileInfo fileInfo)
    {
        ExcelUtil.GetExcelPackage(fileInfo, (ep) =>
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
                    ExcelToJsonItemForLanguage(sheet, assembly, type);
                }
                else
                {
                    ExcelToJsonItemForBase(sheet, assembly, type);
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
    public void ExcelToJsonItemForLanguage(ExcelWorksheet sheet, Assembly assembly, Type type)
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
                object o = assembly.CreateInstance(type.ToString());

                // 遍历所有列
                for (int column = 1; column <= columnCount; column++)
                {
                    string sheetCellName = sheet.Cells[1, column].Text;

                    // 跳过备注列
                    if (sheetCellName.Equals("remark"))
                        continue;

                    // 处理多语言列（content_语言名格式）
                    if (sheetCellName.Contains("content_"))
                    {
                        // 只处理当前语种对应的列
                        if (!languageName.Equals(sheetCellName.Substring("content_".Length)))
                            continue;

                        hasLanguageData = true;
                        sheetCellName = "content";
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
    public void ExcelToJsonItemForBase(ExcelWorksheet sheet, Assembly assembly, Type type)
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

    #endregion

    #region 生成 Entity

    /// <summary>
    /// 批量生成：为所有 Excel 文件生成对应的 Entity 类
    /// 包括 Bean（数据类）和 Cfg（配置管理类）
    /// </summary>
    public void CreateEntities()
    {
        // 路径验证
        if (excelFolderPath.IsNull())
        {
            LogUtil.LogError("Excel 文件目录为 null");
            return;
        }
        if (entityFolderPath.IsNull())
        {
            LogUtil.LogError("Entity 文件目录为 null");
            return;
        }
        if (entityFolderPathForFrameWork.IsNull())
        {
            LogUtil.LogError("Entity Framework 文件目录为 null");
            return;
        }

        // 遍历所有 Excel 文件
        FileInfo[] fileInfos = FileUtil.GetFilesByPath(excelFolderPath);
        for (int i = 0; i < fileInfos.Length; i++)
        {
            FileInfo fileInfo = fileInfos[i];
            CreateEntitiesItem(fileInfo);
        }

        // 刷新资源数据库并显示完成提示
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", "所有 Entity 文件已生成", "确定");
    }

    /// <summary>
    /// 为单个 Excel 文件生成 Entity 类
    /// 自动识别 Framework 层和多语言表
    /// </summary>
    /// <param name="fileInfo">Excel 文件信息</param>
    public void CreateEntitiesItem(FileInfo fileInfo)
    {
        ExcelUtil.GetExcelPackage(fileInfo, (ep) =>
        {
            bool isFrameWork = fileInfo.Name.Contains("_FrameWork");
            bool isLanguage = fileInfo.Name.Contains("excel_language");

            // 获取所有工作表
            ExcelWorksheets workSheets = ep.Workbook.Worksheets;

            // 遍历所有工作表
            for (int w = 1; w <= workSheets.Count; w++)
            {
                // 生成空的部分类（用于扩展）
                CreateEntityPartial(workSheets[w], isFrameWork, isLanguage);
                // 生成完整的 Bean 和 Cfg 类
                CreateEntity(workSheets[w], isFrameWork, isLanguage);
            }

            AssetDatabase.Refresh();
            LogUtil.Log($"Entity 生成完成：{fileInfo.Name}");
        });
    }

    /// <summary>
    /// 创建空的部分类文件（Partial Class）
    /// 用于开发者手动扩展，不会被覆盖
    /// </summary>
    /// <param name="sheet">Excel 工作表</param>
    /// <param name="isFrameWork">是否 Framework 层</param>
    /// <param name="isLanguage">是否多语言表</param>
    void CreateEntityPartial(ExcelWorksheet sheet, bool isFrameWork, bool isLanguage)
    {
        // 确定输出目录
        string dir = isFrameWork ? entityFolderPathForFrameWork : entityFolderPath;

        // 确定输出路径
        string path = isLanguage
            ? $"{dir}/LanguageBeanPartial.cs"
            : $"{dir}/{sheet.Name}BeanPartial.cs";

        // 构建代码内容
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");

        // 空的 Bean 部分类
        sb.AppendLine($"public partial class {sheet.Name}Bean");
        sb.AppendLine("{");
        sb.AppendLine("}");

        // 空的 Cfg 部分类
        sb.AppendLine($"public partial class {sheet.Name}Cfg");
        sb.AppendLine("{");
        sb.AppendLine("}");

        try
        {
            // 确保目录存在
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // 文件不存在时创建（已存在则跳过，保护手动扩展内容）
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
                File.WriteAllText(path, sb.ToString());
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"创建部分类失败：{e.Message}");
        }
    }

    /// <summary>
    /// 创建完整的 Entity 类文件
    /// 包含 Bean（数据类）和 Cfg（配置管理类）
    /// 文件已存在时自动增量更新字段
    /// </summary>
    /// <param name="sheet">Excel 工作表</param>
    /// <param name="isFrameWork">是否 Framework 层</param>
    /// <param name="isLanguage">是否多语言表</param>
    void CreateEntity(ExcelWorksheet sheet, bool isFrameWork, bool isLanguage)
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
        if (isLanguage)
        {
            path = $"{dir}/LanguageBean.cs";
            string beanPath = Application.dataPath + "/FrameWork/Editor/ScriptsTemplates/Excel_LanguageEntity.txt";
            EditorUtil.CreateClass(new Dictionary<string, string>(), beanPath, "LanguageBean", "Assets/FrameWork/Scripts/Bean/MVC");
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

                // if (cellName.Equals("id"))
                //     continue;
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
