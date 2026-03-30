using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

public class BaseUICreateWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/UI脚本创建")]
    static void CreateWindows()
    {
        BaseUICreateWindow window = GetWindow<BaseUICreateWindow>();
        window.titleContent = new GUIContent("UI脚本创建工具", EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
        window.minSize = new Vector2(440, 560);
        window.Show();
    }

    protected readonly static string scriptsTemplatesPath_UI = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUI.txt";
    protected readonly static string scriptsTemplatesPath_UIView = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUIView.txt";
    protected readonly static string scriptsTemplatesPath_UIDialog = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUIDialog.txt";
    protected readonly static string scriptsTemplatesPath_UIPopup = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUIPopup.txt";
    protected readonly static string scriptsTemplatesPath_UIToast = "/FrameWork/Editor/ScrpitsTemplates/UI_BaseUIToast.txt";

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

    // 按钮样式配置
    private static readonly (string label, string icon, int type)[] ScriptButtons = new[]
    {
        ("UI 脚本",       "d_RawImage Icon",       1),
        ("View 脚本",     "d_Image Icon",          2),
        ("Dialog 脚本",   "d_CanvasGroup Icon",    3),
        ("Popup 脚本",    "d_Canvas Icon",         4),
        ("Toast 脚本",    "d_Text Icon",           5),
        ("Common 脚本",   "d_GridLayoutGroup Icon",6),
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
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(new GUIContent(" 刷新", EditorGUIUtility.IconContent("d_Refresh").image),
            EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            HandleForRefresh();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSettingsSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("基本设置", EditorStyles.boldLabel);
        DrawSeparator();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("模块名字", GUILayout.Width(60));
        string newModelName = EditorGUILayout.TextField(modelName);
        if (newModelName != modelName)
        {
            modelName = newModelName;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("生成路径", GUILayout.Width(60));
        pathCreateGame = EditorGUILayout.TextField(pathCreateGame);
        EditorGUILayout.EndHorizontal();

        // 显示当前选中对象信息
        GameObject selected = Selection.activeGameObject;
        if (selected != null)
        {
            EditorGUILayout.Space(2);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("当前选中", selected, typeof(GameObject), true);
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCreateButtonsSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("创建脚本", EditorStyles.boldLabel);
        DrawSeparator();

        // View 脚本路径选择
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("View 脚本生成位置", EditorStyles.miniLabel);
        _viewCreateTarget = (ViewCreateTarget)GUILayout.Toolbar(
            (int)_viewCreateTarget, ViewCreateTargetLabels, GUILayout.Height(22));
        string viewPreviewPath = GetViewCreatePath();
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField(viewPreviewPath);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // 两列按钮布局
        int columns = 2;
        for (int i = 0; i < ScriptButtons.Length; i += columns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < columns && i + j < ScriptButtons.Length; j++)
            {
                var btn = ScriptButtons[i + j];
                GUIContent content = new GUIContent(
                    $" 生成 {btn.label}",
                    EditorGUIUtility.IconContent(btn.icon).image
                );
                if (GUILayout.Button(content, GUILayout.Height(32)))
                {
                    HandleForCreate(btn.type);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStatusSection()
    {
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
            EditorGUILayout.Space(2);
            if (GUILayout.Button("清除提示", GUILayout.Height(20)))
            {
                _statusMessage = "";
            }
        }
    }

    private void DrawCenteredMessage(string message, MessageType type)
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.HelpBox(message, type);
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

    public void HandleForCreate(int typeCreate)
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
