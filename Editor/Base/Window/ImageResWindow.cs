using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings;

/// <summary>
/// 图片资源批量导入设置工具
///
/// 用途：批量管理项目中图片资源的导入参数（TextureImporter设置）
/// - 可对指定文件夹下的所有图片统一设置：纹理类型、压缩格式、WrapMode、最大尺寸、PixelsPerUnit等
/// - 支持创建多组不同的导入配置规则，分别应用于不同的资源目录
/// - 配置数据持久化保存，方便团队协作时统一资源标准
///
/// 典型使用场景：
/// 1. UI图片统一设置为 Sprite 类型 + 特定压缩质量
/// 2. 场景贴图统一设置最大尺寸和压缩方式
/// 3. 新增图片资源后一键刷新导入设置，保持项目资源规范一致
/// </summary>
public class ImageResWindow : EditorWindow
{
    [InitializeOnLoadMethod]
    static void EditorApplication_ProjectChanged()
    {
        //--projectWindowChanged已过时
        //--全局监听Project视图下的资源是否发生变化（添加 删除 移动等）
        //EditorApplication.projectChanged += HandleForAssetsChange;
        //PrefabStage.prefabSaving += HandleForAssetsChange;
    }

    [MenuItem("Custom/工具弹窗/图片资源批量导入设置")]
    static void CreateWindows()
    {
        var window = GetWindow<ImageResWindow>();
        window.titleContent = new GUIContent("图片资源批量导入设置");
        window.minSize = new Vector2(520, 400);
    }

    protected Vector2 scrollPosition;
    protected static ImageResBean imageResSaveData;

    protected static string pathSaveData = "Assets/Data/ImageRes";
    protected static string saveDataFileName = "ImageResSaveData";

    // 折叠状态
    private Dictionary<ImageResBeanItemBean, bool> foldoutStates = new Dictionary<ImageResBeanItemBean, bool>();

    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private bool stylesInitialized;

    public void OnEnable()
    {
        InitData();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        boxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(8, 8, 8, 8),
            margin = new RectOffset(4, 4, 4, 4)
        };

        stylesInitialized = true;
    }

    public void OnGUI()
    {
        InitStyles();

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginVertical();

        UIForHeader();
        GUILayout.Space(4);
        UIForToolbar();
        GUILayout.Space(6);
        UIForListGroup();

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public static void InitData()
    {
        string dataSave = FileUtil.LoadTextFile($"{pathSaveData}/{saveDataFileName}");
        if (dataSave.IsNull())
        {
            imageResSaveData = new ImageResBean();
        }
        else
        {
            imageResSaveData = JsonUtil.FromJsonByNet<ImageResBean>(dataSave);
        }
    }

    /// <summary>
    /// 窗口标题区域
    /// </summary>
    private void UIForHeader()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        GUILayout.Label("图片资源批量导入设置工具", headerStyle);
        EditorGUILayout.HelpBox(
            "对指定文件夹下的图片资源统一设置导入参数（纹理类型、压缩、尺寸等）。\n" +
            "添加配置规则 -> 设置路径和参数 -> 点击刷新应用到图片资源。",
            MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 工具栏按钮
    /// </summary>
    private void UIForToolbar()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("+ 添加配置规则", 150, 24))
        {
            imageResSaveData.listSaveData.Add(new ImageResBeanItemBean());
            SaveAllData();
        }
        GUILayout.FlexibleSpace();
        if (EditorUI.GUIButton("刷新数据", 100, 24))
        {
            InitData();
        }
        if (EditorUI.GUIButton("保存所有数据", 120, 24))
        {
            SaveAllData();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("刷新所有图片", 150, 24))
        {
            RefreshAllImages();
        }
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (EditorUI.GUIButton("清除所有数据", 120, 24))
        {
            if (EditorUI.GUIDialog("确认", "是否清除所有配置数据？此操作不可撤销。"))
            {
                FileUtil.DeleteFile($"{pathSaveData}/{saveDataFileName}");
                InitData();
                foldoutStates.Clear();
                EditorUtil.RefreshAsset();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // 显示当前配置数量
        int count = imageResSaveData?.listSaveData?.Count ?? 0;
        EditorGUILayout.LabelField($"当前配置规则数: {count}", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 配置列表
    /// </summary>
    public void UIForListGroup()
    {
        if (imageResSaveData == null || imageResSaveData.listSaveData.IsNull())
            return;

        for (int i = 0; i < imageResSaveData.listSaveData.Count; i++)
        {
            UIForItemGroup(i, imageResSaveData.listSaveData[i]);
        }
    }

    /// <summary>
    /// 单条配置规则UI
    /// </summary>
    public void UIForItemGroup(int index, ImageResBeanItemBean itemData)
    {
        if (!foldoutStates.ContainsKey(itemData))
            foldoutStates[itemData] = true;

        EditorGUILayout.BeginVertical(boxStyle);

        // 标题栏：折叠 + 序号 + 路径预览 + 操作按钮
        EditorGUILayout.BeginHorizontal();

        string displayName = string.IsNullOrEmpty(itemData.pathRes) ? "(未设置路径)" : itemData.pathRes;
        foldoutStates[itemData] = EditorGUILayout.Foldout(foldoutStates[itemData], $"[{index + 1}] {displayName}", true, EditorStyles.foldoutHeader);

        GUILayout.FlexibleSpace();

        if (EditorUI.GUIButton("刷新资源", 80, 20))
        {
            RefreshImage(itemData);
        }

        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (EditorUI.GUIButton("删除", 50, 20))
        {
            if (EditorUI.GUIDialog("确认", $"是否删除配置规则 [{index + 1}]？"))
            {
                foldoutStates.Remove(itemData);
                imageResSaveData.listSaveData.Remove(itemData);
                SaveAllData();
                GUIUtility.ExitGUI();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        // 折叠内容
        if (foldoutStates.ContainsKey(itemData) && foldoutStates[itemData])
        {
            GUILayout.Space(4);
            EditorGUI.indentLevel++;

            // 路径设置
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图片路径:", GUILayout.Width(70));
            itemData.pathRes = EditorGUILayout.TextField(itemData.pathRes);
            if (GUILayout.Button("选择", GUILayout.Width(50), GUILayout.Height(18)))
            {
                string selected = EditorUI.GetFolderPanel("选择图片资源目录", "Assets");
                if (!string.IsNullOrEmpty(selected))
                {
                    // 转换为相对于项目的路径
                    if (selected.Contains("Assets"))
                    {
                        itemData.pathRes = "Assets" + selected.Substring(selected.IndexOf("Assets") + 6);
                    }
                    else
                    {
                        itemData.pathRes = selected;
                    }
                    SaveAllData();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);

            // 导入参数
            itemData.textureImporterType = (int)EditorUI.GUIEnum<TextureImporterType>("纹理类型:", itemData.textureImporterType, 350);
            itemData.textureImporterCompression = (int)EditorUI.GUIEnum<TextureImporterCompression>("压缩质量:", itemData.textureImporterCompression, 350);
            itemData.wrapMode = (int)EditorUI.GUIEnum<TextureWrapMode>("Wrap Mode:", itemData.wrapMode, 350);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("最大尺寸:", GUILayout.Width(70));
            itemData.maxTextureSize = EditorGUILayout.IntField(itemData.maxTextureSize, GUILayout.Width(80));
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Pixels Per Unit:", GUILayout.Width(100));
            itemData.spritePixelsPerUnit = EditorGUILayout.IntField(itemData.spritePixelsPerUnit, GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 刷新所有配置规则对应的图片
    /// </summary>
    private void RefreshAllImages()
    {
        if (imageResSaveData.listSaveData.IsNull())
            return;

        int totalGroups = imageResSaveData.listSaveData.Count;
        for (int i = 0; i < totalGroups; i++)
        {
            var data = imageResSaveData.listSaveData[i];
            EditorUI.GUIShowProgressBar("刷新图片资源",
                $"正在处理 ({i + 1}/{totalGroups}): {data.pathRes}",
                (float)i / totalGroups);
            RefreshImage(data);
        }
        EditorUI.GUIHideProgressBar();
    }

    /// <summary>
    /// 刷新单条规则对应目录的图片导入设置
    /// </summary>
    protected void RefreshImage(ImageResBeanItemBean data)
    {
        if (string.IsNullOrEmpty(data.pathRes))
            return;

        FileInfo[] arrayFile = FileUtil.GetFilesByPath(data.pathRes);
        if (arrayFile == null || arrayFile.Length == 0)
            return;

        for (int i = 0; i < arrayFile.Length; i++)
        {
            FileInfo fileInfo = arrayFile[i];
            if (fileInfo.Name.Contains(".meta"))
                continue;
            EditorUtil.SetTextureData($"{data.pathRes}/{fileInfo.Name}",
                spritePixelsPerUnit: data.spritePixelsPerUnit,
                wrapMode: (TextureWrapMode)data.wrapMode,
                textureImporterType: (TextureImporterType)data.textureImporterType,
                textureImporterCompression: (TextureImporterCompression)data.textureImporterCompression,
                maxTextureSize: data.maxTextureSize);
        }
        EditorUtil.RefreshAsset();
    }

    /// <summary>
    /// 保存所有数据
    /// </summary>
    protected void SaveAllData()
    {
        string saveData = JsonUtil.ToJsonByNet(imageResSaveData);
        FileUtil.CreateTextFile(pathSaveData, saveDataFileName, saveData);
        EditorUtil.RefreshAsset();
    }
}
