using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

public static class IconHelper
{
    /// <summary>
    /// 安全地获取图标，如果找不到则返回 null
    /// </summary>
    public static Texture2D GetIcon(string iconName)
    {
        if (string.IsNullOrEmpty(iconName))
            return null;
        
        var content = EditorGUIUtility.IconContent(iconName);
        return content?.image as Texture2D;
    }
}

public class BaseUICreateWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/UI脚本创建")]
    static void CreateWindows()
    {
        BaseUICreateWindow window = GetWindow<BaseUICreateWindow>();
        var titleIcon = IconHelper.GetIcon("ScriptableObject Icon") ?? IconHelper.GetIcon("cs Script Icon");
        window.titleContent = titleIcon != null 
            ? new GUIContent("UI脚本创建工具", titleIcon)
            : new GUIContent("UI脚本创建工具");
        window.minSize = new Vector2(440, 560);
        window.Show();
    }

    protected readonly static string scriptsTemplatesPath_UI = "/FrameWork/Editor/ScriptsTemplates/UI_BaseUI.txt";
    protected readonly static string scriptsTemplatesPath_UIView = "/FrameWork/Editor/ScriptsTemplates/UI_BaseUIView.txt";
    protected readonly static string scriptsTemplatesPath_UIDialog = "/FrameWork/Editor/ScriptsTemplates/UI_BaseUIDialog.txt";
    protected readonly static string scriptsTemplatesPath_UIPopup = "/FrameWork/Editor/ScriptsTemplates/UI_BaseUIPopup.txt";
    protected readonly static string scriptsTemplatesPath_UIToast = "/FrameWork/Editor/ScriptsTemplates/UI_BaseUIToast.txt";

    public static string pathCreateBase = "Assets/Scripts/Component/UI";
    public static string pathCreateGame = "Assets/Scripts/Component/UI/Game";
    public static string modelName = "";

    public static string keyEditorPrefs = "InspectorBaseUICreate";

    private Vector2 _scrollPos;
    private string _statusMessage = "";
    private MessageType _statusType = MessageType.None;

    // View 脚本生成目标路径选项
    private enum ViewCreateTarget
    {
        UI = 0,
        Dialog = 1,
        Popup = 2,
        Toast = 3,
    }

    private static readonly string[] ViewCreateTargetLabels = new string[]
    {
        "UI (默认)",
        "Dialog",
        "Popup",
        "Toast",
    };

    private ViewCreateTarget _viewCreateTarget = ViewCreateTarget.UI;

    // 按钮样式配置 - 使用通用的Unity内置图标
    private static readonly (string label, string icon, int type)[] ScriptButtons = new[]
    {
        ("UI 脚本",       "UnityLogo",           1),
        ("View 脚本",     "ViewToolOrbit",       2),
        ("Dialog 脚本",   "Toolbar Plus",        3),
        ("Popup 脚本",    "Toolbar Minus",       4),
        ("Toast 脚本",    "UnityEditor.Notification", 5),
        ("Common 脚本",   "GridLayoutGroup Icon",6),
    };

    [InitializeOnLoadMethod]
    public static void Init()
    {
        #if UNITY_6000_3_OR_NEWER
        #else
        ToolbarExtension.ToolbarZoneLeftAlign += OnToolbarGUI;
        #endif
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }

    #if UNITY_6000_3_OR_NEWER
    // defaultDockPosition 可选：Left / Middle / Right
    [MainToolbarElement("自定义标题/UI脚本创建", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement CreateSettingsButton()
    {
        var content = new MainToolbarContent("UI脚本创建");
        
        return new MainToolbarButton(content, () =>
        {
            CreateWindows();
        });
    }

    #endif
    static void OnToolbarGUI(VisualElement rootVisualElement)
    {
        var refresh = new EditorToolbarDropdown();
        refresh.text = "UI脚本创建";
        refresh.clicked += CreateWindows;
        rootVisualElement.Add(refresh);
    }

    private static void OnAfterAssemblyReload()
    {
        bool isAutoAdd = EditorPrefs.GetBool(keyEditorPrefs, false);
        if (isAutoAdd)
        {
            GameObject objSelect = Selection.activeGameObject;
            string createfileName = GetCreateScriptFileName(objSelect);
            AddComponentByFileName(objSelect, createfileName);
            EditorPrefs.SetBool(keyEditorPrefs, false);
        }
    }

    public void OnGUI()
    {
        if (!EditorUtil.CheckIsPrefabMode())
        {
            DrawCenteredMessage("请先进入 Prefab 编辑模式", MessageType.Warning);
            return;
        }

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        DrawHeader();
        EditorGUILayout.Space(4);
        DrawSettingsSection();
        EditorGUILayout.Space(6);
        DrawCreateButtonsSection();
        EditorGUILayout.Space(6);
        DrawStatusSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // 左侧提示
        var logoIcon = IconHelper.GetIcon("UnityLogo");
        if (logoIcon != null)
            GUILayout.Label(logoIcon, GUILayout.Width(20), GUILayout.Height(20));
        GUILayout.Label("UI脚本创建工具", EditorStyles.boldLabel);
        
        GUILayout.FlexibleSpace();
        
        // 刷新按钮 - 带图标
        var refreshIcon = IconHelper.GetIcon("Refresh");
        GUIContent refreshContent = refreshIcon != null 
            ? new GUIContent(" 刷新名称", refreshIcon)
            : new GUIContent(" 刷新名称");
        if (GUILayout.Button(refreshContent, EditorStyles.toolbarButton, GUILayout.Width(90)))
        {
            HandleForRefresh();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSettingsSection()
    {
        EditorGUILayout.BeginVertical("box");
        
        // 标题栏带图标
        EditorGUILayout.BeginHorizontal();
        var settingsIcon = IconHelper.GetIcon("SettingsIcon");
        if (settingsIcon != null)
            GUILayout.Label(settingsIcon, GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField("基本设置", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        DrawSeparator();

        // 模块名字输入 - 带图标
        EditorGUILayout.BeginHorizontal();
        var textIcon = IconHelper.GetIcon("TextAsset Icon");
        if (textIcon != null)
            GUILayout.Label(textIcon, GUILayout.Width(16), GUILayout.Height(16));
        EditorGUILayout.LabelField("模块名字", GUILayout.Width(70));
        string newModelName = EditorGUILayout.TextField(modelName, EditorStyles.textField);
        if (newModelName != modelName)
        {
            modelName = newModelName;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // 生成路径输入 - 带图标
        EditorGUILayout.BeginHorizontal();
        var folderIcon = IconHelper.GetIcon("Folder Icon");
        if (folderIcon != null)
            GUILayout.Label(folderIcon, GUILayout.Width(16), GUILayout.Height(16));
        EditorGUILayout.LabelField("生成路径", GUILayout.Width(70));
        pathCreateGame = EditorGUILayout.TextField(pathCreateGame, EditorStyles.textField);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // 显示当前选中对象信息 - 美化版
        GameObject selected = Selection.activeGameObject;
        if (selected != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            var selectableIcon = IconHelper.GetIcon("Selectable Icon");
        if (selectableIcon != null)
            GUILayout.Label(selectableIcon, GUILayout.Width(16), GUILayout.Height(16));
            EditorGUILayout.LabelField("当前选中对象", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(selected, typeof(GameObject), true, GUILayout.Height(18));
            EditorGUI.EndDisabledGroup();
            
            // 显示预览名称
            string previewName = GetCreateScriptFileName(selected);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("生成文件名:", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField(previewName, EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("未选中对象，请在 Hierarchy 中选中一个 GameObject", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    // 按钮样式缓存
    private GUIStyle _buttonStyle;
    private GUIStyle _createButtonStyle;
    private GUIStyle _toolbarButtonStyle;

    private void InitStyles()
    {
        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.alignment = TextAnchor.MiddleLeft;
            _buttonStyle.padding = new RectOffset(8, 8, 4, 4);
            _buttonStyle.fontSize = 11;
        }
        if (_createButtonStyle == null)
        {
            _createButtonStyle = new GUIStyle(GUI.skin.button);
            _createButtonStyle.alignment = TextAnchor.MiddleCenter;
            _createButtonStyle.fontStyle = FontStyle.Bold;
            _createButtonStyle.fontSize = 12;
            _createButtonStyle.padding = new RectOffset(8, 8, 6, 6);
            // 使用深绿色背景
            _createButtonStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.6f, 0.3f, 1f));
            _createButtonStyle.normal.textColor = Color.white;
            _createButtonStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.7f, 0.4f, 1f));
            _createButtonStyle.hover.textColor = Color.white;
            _createButtonStyle.active.background = MakeTex(2, 2, new Color(0.15f, 0.5f, 0.25f, 1f));
            _createButtonStyle.active.textColor = Color.white;
        }
        if (_toolbarButtonStyle == null)
        {
            _toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
            _toolbarButtonStyle.alignment = TextAnchor.MiddleLeft;
            _toolbarButtonStyle.padding = new RectOffset(6, 6, 2, 2);
        }
    }

    private Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = color;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void DrawCreateButtonsSection()
    {
        InitStyles();

        EditorGUILayout.BeginVertical("box");
        
        // 标题栏带图标
        EditorGUILayout.BeginHorizontal();
        var scriptIcon = IconHelper.GetIcon("cs Script Icon");
        if (scriptIcon != null)
            GUILayout.Label(scriptIcon, GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField("创建脚本", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        DrawSeparator();

        // View 脚本路径选择 - 美化版
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        var folderIcon2 = IconHelper.GetIcon("Folder Icon");
        if (folderIcon2 != null)
            GUILayout.Label(folderIcon2, GUILayout.Width(16), GUILayout.Height(16));
        EditorGUILayout.LabelField("View 脚本生成位置", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(2);
        _viewCreateTarget = (ViewCreateTarget)GUILayout.Toolbar(
            (int)_viewCreateTarget, ViewCreateTargetLabels, GUILayout.Height(24));
        
        EditorGUILayout.Space(2);
        string viewPreviewPath = GetViewCreatePath();
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField(viewPreviewPath, EditorStyles.textField);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 按钮分组标题
        EditorGUILayout.BeginHorizontal();
        var unityIcon = IconHelper.GetIcon("UnityLogo");
        if (unityIcon != null)
            GUILayout.Label(unityIcon, GUILayout.Width(16), GUILayout.Height(16));
        EditorGUILayout.LabelField("点击生成对应脚本", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);

        // 两列按钮布局 - 美化版
        int columns = 2;
        for (int i = 0; i < ScriptButtons.Length; i += columns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < columns && i + j < ScriptButtons.Length; j++)
            {
                var btn = ScriptButtons[i + j];
                var btnIcon = IconHelper.GetIcon(btn.icon);
                GUIContent content = btnIcon != null 
                    ? new GUIContent($" {btn.label}", btnIcon)
                    : new GUIContent($" {btn.label}");
                if (GUILayout.Button(content, _createButtonStyle, GUILayout.Height(36)))
                {
                    HandleForCreateWithConfirm(btn.type, btn.label);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(3);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStatusSection()
    {
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            // 根据状态类型显示不同图标
            string statusIconName = _statusType switch
            {
                MessageType.Error => "console.erroricon",
                MessageType.Warning => "console.warnicon",
                MessageType.Info => "console.infoicon",
                _ => "console.infoicon"
            };
            Texture2D statusIcon = IconHelper.GetIcon(statusIconName);
            if (statusIcon != null)
                GUILayout.Label(statusIcon, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField("操作结果", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
            EditorGUILayout.Space(2);
            
            // 清除按钮 - 居中
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var closeIcon = IconHelper.GetIcon("winbtn_win_close");
            GUIContent closeContent = closeIcon != null 
                ? new GUIContent(" 清除提示", closeIcon)
                : new GUIContent(" 清除提示");
            if (GUILayout.Button(closeContent, GUILayout.Width(100), GUILayout.Height(24)))
            {
                _statusMessage = "";
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawCenteredMessage(string message, MessageType type)
    {
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(400));
        EditorGUILayout.Space(20);
        
        // 居中显示图标
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        string iconName = type switch
        {
            MessageType.Error => "console.erroricon.sml",
            MessageType.Warning => "console.warnicon.sml",
            _ => "console.infoicon.sml"
        };
        Texture2D icon = IconHelper.GetIcon(iconName);
        if (icon != null)
            GUILayout.Label(icon, GUILayout.Width(32), GUILayout.Height(32));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(message, type);
        EditorGUILayout.Space(20);
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
    }

    private void DrawSeparator()
    {
        EditorGUILayout.Space(2);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(4);
    }

    private void SetStatus(string message, MessageType type)
    {
        _statusMessage = message;
        _statusType = type;
        Repaint();
    }

    /// <summary>
    /// 根据当前 View 路径选择获取实际生成路径
    /// </summary>
    private string GetViewCreatePath()
    {
        switch (_viewCreateTarget)
        {
            case ViewCreateTarget.Dialog:
                return modelName.IsNull()
                    ? $"{pathCreateBase}/Dialog"
                    : $"{pathCreateBase}/Dialog/{modelName}";
            case ViewCreateTarget.Popup:
                return modelName.IsNull()
                    ? $"{pathCreateBase}/Popup"
                    : $"{pathCreateBase}/Popup/{modelName}";
            case ViewCreateTarget.Toast:
                return modelName.IsNull()
                    ? $"{pathCreateBase}/Toast"
                    : $"{pathCreateBase}/Toast/{modelName}";
            case ViewCreateTarget.UI:
            default:
                return modelName.IsNull()
                    ? pathCreateGame
                    : $"{pathCreateGame}/{modelName}";
        }
    }

    public string GetOriginTargetName(string targetName)
    {
        if (targetName.StartsWith("UI"))
            targetName = targetName.Remove(0, 2);
        if (targetName.StartsWith("View"))
            targetName = targetName.Remove(0, 4);
        if (targetName.StartsWith("Dialog"))
            targetName = targetName.Remove(0, 6);
        if (targetName.StartsWith("Popup"))
            targetName = targetName.Remove(0, 5);
        if (targetName.StartsWith("Toast"))
            targetName = targetName.Remove(0, 5);
        if (targetName.EndsWith("Item"))
            targetName = targetName.Remove(targetName.Length - 4, 4);
        return targetName;
    }

    public void HandleForRefresh()
    {
        GameObject objSelect = Selection.activeGameObject;
        if (objSelect == null)
        {
            SetStatus("请先选中一个 GameObject", MessageType.Warning);
            return;
        }
        modelName = GetOriginTargetName(objSelect.name);
        var directoryInfos = FileUtil.GetDirectoriesByPath(pathCreateGame);
        foreach (var itemDirectory in directoryInfos)
        {
            if (modelName.Contains(itemDirectory.Name))
            {
                modelName = itemDirectory.Name;
                SetStatus($"已刷新模块名: {modelName}", MessageType.Info);
                return;
            }
        }
        SetStatus($"已刷新模块名: {modelName} (未匹配到已有目录)", MessageType.Info);
    }

    /// <summary>
    /// 带确认弹窗的创建处理
    /// </summary>
    public void HandleForCreateWithConfirm(int typeCreate, string scriptTypeName)
    {
        GameObject objSelect = Selection.activeGameObject;
        if (objSelect == null)
        {
            SetStatus("请先选中一个 GameObject", MessageType.Warning);
            return;
        }

        string createfileName = GetCreateScriptFileName(objSelect);
        string pathCreateFinal = GetCreatePath(typeCreate);
        
        // 确认弹窗
        string title = $"确认创建 {scriptTypeName}";
        string message = $"确定要创建以下脚本吗?\n\n" +
                        $"脚本名称: {createfileName}\n" +
                        $"脚本类型: {scriptTypeName}\n" +
                        $"生成路径: {pathCreateFinal}\n\n" +
                        $"点击 [确定] 继续生成，点击 [取消] 返回。";
        string okButton = "确定生成";
        string cancelButton = "取消";

        if (EditorUtility.DisplayDialog(title, message, okButton, cancelButton))
        {
            HandleForCreate(typeCreate, true);
        }
    }

    /// <summary>
    /// 获取创建路径（用于预览）
    /// </summary>
    private string GetCreatePath(int typeCreate)
    {
        if (typeCreate == 2)
        {
            return GetViewCreatePath();
        }
        else if (typeCreate == 3)
        {
            return $"{pathCreateBase}/Dialog";
        }
        else if (typeCreate == 4)
        {
            return $"{pathCreateBase}/Popup";
        }
        else if (typeCreate == 5)
        {
            return $"{pathCreateBase}/Toast";
        }
        else if (typeCreate == 6)
        {
            return modelName.IsNull()
                ? $"{pathCreateBase}/Common"
                : $"{pathCreateBase}/Common/{modelName}";
        }
        else
        {
            return $"{pathCreateGame}/{modelName}";
        }
    }

    public void HandleForCreate(int typeCreate, bool skipConfirm = false)
    {
        string scriptsTemplatesPath = "";

        GameObject objSelect = Selection.activeGameObject;
        if (objSelect == null)
        {
            SetStatus("请先选中一个 GameObject", MessageType.Warning);
            return;
        }

        string createfileName = GetCreateScriptFileName(objSelect);
        switch (typeCreate)
        {
            case 1:
                scriptsTemplatesPath = scriptsTemplatesPath_UI;
                break;
            case 2:
            case 6:
                scriptsTemplatesPath = scriptsTemplatesPath_UIView;
                break;
            case 3:
                scriptsTemplatesPath = scriptsTemplatesPath_UIDialog;
                break;
            case 4:
                scriptsTemplatesPath = scriptsTemplatesPath_UIPopup;
                break;
            case 5:
                scriptsTemplatesPath = scriptsTemplatesPath_UIToast;
                break;
        }
        string templatesPath = Application.dataPath + scriptsTemplatesPath;

        if (!EditorUtil.CheckIsPrefabMode(out var prefabStage))
        {
            SetStatus("没有进入编辑模式", MessageType.Error);
            return;
        }
        string[] path = EditorUtil.GetScriptPath(createfileName);
        if (path.Length > 0)
        {
            SetStatus($"已存在 {createfileName} 类，已自动添加组件", MessageType.Warning);
            AddComponentByFileName(objSelect, createfileName);
            return;
        }

        Dictionary<string, string> dicReplace = InspectorBaseUIComponent.ReplaceRole(createfileName);

        string pathCreateFinal;
        if (modelName.IsNull())
        {
            SetStatus("还未输入模块名字", MessageType.Error);
            return;
        }

        // View 脚本：根据选择的目标路径生成
        if (typeCreate == 2)
        {
            pathCreateFinal = GetViewCreatePath();
        }
        else if (typeCreate == 3)
        {
            pathCreateFinal = $"{pathCreateBase}/Dialog";
        }
        else if (typeCreate == 4)
        {
            pathCreateFinal = $"{pathCreateBase}/Popup";
        }
        else if (typeCreate == 5)
        {
            pathCreateFinal = $"{pathCreateBase}/Toast";
        }
        else if (typeCreate == 6)
        {
            pathCreateFinal = modelName.IsNull()
                ? $"{pathCreateBase}/Common"
                : $"{pathCreateBase}/Common/{modelName}";
        }
        else
        {
            pathCreateFinal = $"{pathCreateGame}/{modelName}";
        }

        EditorUtil.CreateClass(dicReplace, templatesPath, createfileName, pathCreateFinal);
        EditorPrefs.SetBool(keyEditorPrefs, true);

        Undo.RecordObject(objSelect, objSelect.gameObject.name);
        EditorUtility.SetDirty(objSelect);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SetStatus($"成功创建: {createfileName} -> {pathCreateFinal}", MessageType.Info);
    }

    public static void AddComponentByFileName(GameObject objSelect, string createfileName)
    {
        System.Type componentType = System.Type.GetType($"{createfileName}, Assembly-CSharp");
        if (componentType != null && objSelect.GetComponent(componentType) == null)
        {
            objSelect.AddComponent(componentType);
        }
    }

    public static string GetCreateScriptFileName(GameObject objSelect)
    {
        string fileName = objSelect.name;
        if (!fileName.Substring(0, 2).Equals("UI"))
        {
            fileName = fileName.Insert(0, "UI");
        }
        return fileName;
    }
}
