using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings;

public class AddressableWindow : EditorWindow
{
    [InitializeOnLoadMethod]
    static void EditorApplication_ProjectChanged()
    {
        //--projectWindowChanged已过时
        //--全局监听Project视图下的资源是否发生变化（添加 删除 移动等）
        //EditorApplication.projectChanged += HandleForAssetsChange;
        //PrefabStage.prefabSaving += HandleForAssetsChange;
        AddressableUtil.AddCallBackForAssetChange(HandleForAssetChange);
    }

    [MenuItem("Custom/工具弹窗/资源Addressable")]
    static void CreateWindows()
    {
        EditorWindow.GetWindow(typeof(AddressableWindow));
    }

    protected Vector2 scrollPosition;
    protected static List<AddressableAssetGroup> allGroup;
    protected static AddressableSaveBean addressableSaveData;

    protected static string pathSaveData = "Assets/Data/Addressable";
    protected static string saveDataFileName = "AddressableSaveData";

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
        //获取所有组
        allGroup = AddressableUtil.FindAllGrop();
        //获取保存数据
        string dataSave = FileUtil.LoadTextFile($"{pathSaveData}/{saveDataFileName}");
        if (dataSave.IsNull())
        {
            addressableSaveData = new AddressableSaveBean();
        }
        else
        {
            addressableSaveData = JsonUtil.FromJsonByNet<AddressableSaveBean>(dataSave);
        }
    }

    /// <summary>
    /// 基础
    /// </summary>
    public void UIForBase()
    {
        GUILayout.BeginHorizontal();

        if (EditorUI.GUIButton("刷新所有资源", 200))
        {
            InitData();
            HandleForAllAssetChange();
        }
        if (EditorUI.GUIButton("保存所有数据", 200))
        {
            string saveData = JsonUtil.ToJsonByNet(addressableSaveData);
            FileUtil.CreateTextFile(pathSaveData, saveDataFileName, saveData);
            EditorUtil.RefreshAsset();
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
        if (allGroup.IsNull())
            return;
        for (int i = 0; i < allGroup.Count; i++)
        {
            AddressableAssetGroup itemGroup = allGroup[i];
            if (addressableSaveData.dicSaveData.TryGetValue(itemGroup.name, out AddressableSaveItemBean value))
            {
                UIForItemGroup(itemGroup, value);
            }
            else
            {
                value = new AddressableSaveItemBean();
                addressableSaveData.dicSaveData.Add(itemGroup.name, value);
                UIForItemGroup(itemGroup, null);
            }
        }

    }

    /// <summary>
    /// UI单个Group
    /// </summary>
    /// <param name="itemGroup"></param>
    /// <param name="value"></param>
    public void UIForItemGroup(AddressableAssetGroup itemGroup, AddressableSaveItemBean value)
    {
        GUILayout.BeginHorizontal();

        EditorUI.GUIText(itemGroup.name, 150);

        EditorUI.GUIText("文件路径地址：", 100);
        if (EditorUI.GUIButton("+", 20))
        {
            value.listPathSave.Add("");
        }

        GUILayout.BeginVertical(GUILayout.Width(220));
        for (int i = 0; i < value.listPathSave.Count; i++)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(220), GUILayout.Height(30));
            value.listPathSave[i] = EditorUI.GUIEditorText(value.listPathSave[i], 200);
            if (EditorUI.GUIButton("-", 20))
            {
                value.listPathSave.RemoveAt(i);
                i--;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        EditorUI.GUIText("Label：", 50);
        if (EditorUI.GUIButton("+", 20))
        {
            value.listLabel.Add("");
        }
        GUILayout.BeginVertical(GUILayout.Width(100));
        for (int i = 0; i < value.listLabel.Count; i++)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(100), GUILayout.Height(30));
            value.listLabel[i] = EditorUI.GUIEditorText(value.listLabel[i], 200);
            if (EditorUI.GUIButton("-", 20))
            {
                value.listLabel.RemoveAt(i);
                i--;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();


        GUILayout.EndHorizontal();
        EditorUI.GUIText("---------------------------------------------------------------------", 1000);
    }

    /// <summary>
    /// 所有资源刷新
    /// </summary>
    public static void HandleForAllAssetChange()
    {
        List<AddressableAssetEntry> listAllData = AddressableUtil.FindAllAsset();
        List<AddressableAssetEntry> listData = new List<AddressableAssetEntry>();
        for (int i = 0; i < listAllData.Count; i++)
        {
            AddressableAssetEntry addressableAsset = listAllData[i];
            if (addressableAsset.parentGroup.name.Equals("Built In Data"))
            {

            }
            else
            {
                listData.Add(addressableAsset);
            }
        }
        HandleForRefreshAssets(listData);
    }


    /// <summary>
    /// 资源修改监听处理
    /// </summary>
    public static void HandleForAssetChange(AddressableAssetSettings addressableAsset, ModificationEvent modificationEvent, object obj)
    {
        switch (modificationEvent)
        {
            //case ModificationEvent.EntryCreated:
            //case ModificationEvent.EntryMoved:
            //case ModificationEvent.EntryRemoved:
            case ModificationEvent.EntryAdded:
                if (obj is List<AddressableAssetEntry> listData)
                {
                    HandleForRefreshAssets(listData);
                }
                break;
        }
    }

    /// <summary>
    /// 刷新Addressable资源
    /// </summary>
    /// <param name="listChangeAssetEntry"></param>
    public static void HandleForRefreshAssets(List<AddressableAssetEntry> listChangeAssetEntry)
    {
        InitData();
        if (addressableSaveData == null)
            return;
        for (int i = 0; i < listChangeAssetEntry.Count; i++)
        {
            EditorUI.GUIShowProgressBar("刷新进度", "资源", (float)i / listChangeAssetEntry.Count);

            AddressableAssetEntry itemAssetEntry = listChangeAssetEntry[i];
            LogUtil.Log($"资源修改 address:{itemAssetEntry.address} AssetPath:{itemAssetEntry.AssetPath}");
            if(itemAssetEntry.AssetPath.LastIndexOf("/")==0)
                LogUtil.Log($"--------- address:{itemAssetEntry.address} AssetPath:{itemAssetEntry.AssetPath}");
            string assetPathFile = itemAssetEntry.AssetPath.Remove(itemAssetEntry.AssetPath.LastIndexOf("/"));
            //查询保存的路径
            foreach (var itemSaveGroup in addressableSaveData.dicSaveData)
            {
                string groupName = itemSaveGroup.Key;

                List<string> listSavePath = itemSaveGroup.Value.listPathSave;
                //遍历路径 如果再这个路径里 则分配要这个组
                for (int f = 0; f < listSavePath.Count; f++)
                {
                    string savePath = listSavePath[f];
                    if (assetPathFile.Equals(savePath))
                    {
                        AddressableUtil.MoveAssetEntry(itemAssetEntry, groupName);
                        AddressableUtil.ClearAllLabel(itemAssetEntry);
                        AddressableUtil.SetLabel(itemAssetEntry, itemSaveGroup.Value.listLabel);
                        break;
                    }
                }
            }
        }
        EditorUtil.RefreshAsset();
        EditorUI.GUIHideProgressBar();
    }
}