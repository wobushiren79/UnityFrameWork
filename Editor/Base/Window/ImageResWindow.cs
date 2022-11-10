using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings;

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

    [MenuItem("Custom/资源/图片")]
    static void CreateWindows()
    {
        EditorWindow.GetWindow(typeof(ImageResWindow));
    }

    protected Vector2 scrollPosition;
    protected static ImageResBean imageResSaveData;

    protected static string pathSaveData = "Assets/Data/ImageRes";
    protected static string saveDataFileName = "ImageResSaveData";

    public void OnEnable()
    {
        InitData();
    }

    public void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginVertical();

        UIForBase();
        UIForListGroup();

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public static void InitData()
    {
        //获取保存数据
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
    /// 基础
    /// </summary>
    public void UIForBase()
    {
        GUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("添加", 200))
        {
            imageResSaveData.listSaveData.Add(new ImageResBeanItemBean());
            SaveAllData();
        }
        if (EditorUI.GUIButton("刷新数据", 200))
        {
            InitData();
        }
        if (EditorUI.GUIButton("刷新所有图片", 200))
        {
            if (imageResSaveData.listSaveData.IsNull())
                return;
            for (int i = 0; i < imageResSaveData.listSaveData.Count; i++)
            {
                RefreshImage(imageResSaveData.listSaveData[i]);
            }
        }
        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();

        if (EditorUI.GUIButton("保存所有数据", 200))
        {
            SaveAllData();
        }
        if (EditorUI.GUIButton("清除所有数据", 200))
        {
            if (EditorUI.GUIDialog("确认", "是否清除所有数据"))
            {
                FileUtil.DeleteFile($"{pathSaveData}/{saveDataFileName}");
                InitData();
                EditorUtil.RefreshAsset();
            }
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 列表
    /// </summary>
    public void UIForListGroup()
    {
        if (imageResSaveData == null || imageResSaveData.listSaveData.IsNull())
            return;
        foreach (var itemData in imageResSaveData.listSaveData)
        {
            UIForItemGroup(itemData);
        }
    }

    /// <summary>
    /// UI单个Group
    /// </summary>
    /// <param name="itemGroup"></param>
    /// <param name="value"></param>
    public void UIForItemGroup(ImageResBeanItemBean itemData)
    {
        EditorUI.GUIText("---------------------------------------------------------------------", 1000);
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("删除"))
        {
            if (EditorUI.GUIDialog("确认", "是否清除这条数据"))
            {
                imageResSaveData.listSaveData.Remove(itemData);
                SaveAllData();
            }
        }
        if (EditorUI.GUIButton("刷新资源"))
        {
            RefreshImage(itemData);
        }
        EditorUI.GUIText("图片路径地址：", 100);
        itemData.pathRes = EditorUI.GUIEditorText(itemData.pathRes, 250);

        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();
        itemData.textureImporterType = (int)EditorUI.GUIEnum<TextureImporterType>("类型：", itemData.textureImporterType, 250);
        itemData.textureImporterCompression = (int)EditorUI.GUIEnum<TextureImporterCompression>("压缩质量：", itemData.textureImporterCompression, 250);
        itemData.wrapMode = (int)EditorUI.GUIEnum<TextureWrapMode>("适配：", itemData.wrapMode, 250);
        GUILayout.BeginHorizontal();
        EditorUI.GUIText("大小：", 50);
        itemData.maxTextureSize = EditorUI.GUIEditorText(itemData.maxTextureSize, 50);
        EditorUI.GUIText("PixelsPerUnit：", 50);
        itemData.spritePixelsPerUnit = EditorUI.GUIEditorText(itemData.spritePixelsPerUnit, 50);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.EndVertical();
        GUILayout.EndVertical();
    }

    protected void RefreshImage(ImageResBeanItemBean data)
    {
        FileInfo[] arrayFile = FileUtil.GetFilesByPath(data.pathRes);
        for (int i = 0; i < arrayFile.Length; i++)
        {
            FileInfo fileInfo = arrayFile[i];
            if (fileInfo.Name.Contains(".meta"))
                continue;
            EditorUtil.SetTextureData($"{data.pathRes}/{fileInfo.Name}",
                spritePixelsPerUnit : data.spritePixelsPerUnit,
                wrapMode: (TextureWrapMode)data.wrapMode,
                textureImporterType : (TextureImporterType)data.textureImporterType,
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