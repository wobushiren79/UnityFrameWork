using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings;

public class AddressableUtil
{
    /// <summary>
    ///创建分组
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="groupName"></param>
    /// <returns></returns>
    public static AddressableAssetGroup FindOrCreateGroup(string groupName)
    {
        AddressableAssetSettings Settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetGroup group = Settings.FindGroup(groupName);
        if (group == null)
            group = Settings.CreateGroup(groupName, false, false, false, null);
        //Settings.AddLabel(groupName, false);
        return group;
    }

    /// <summary>
    /// 查找所有的group
    /// </summary>
    /// <returns></returns>
    public static List<AddressableAssetGroup> FindAllGrop()
    {
        AddressableAssetSettings Settings = AddressableAssetSettingsDefaultObject.Settings;
        return Settings.groups;
    }

    /// <summary>
    /// 获取所有资源
    /// </summary>
    /// <param name="includeSubObjects"></param>
    /// <returns></returns>
    public static List<AddressableAssetEntry> FindAllAsset(bool includeSubObjects = false)
    {
        AddressableAssetSettings Settings = AddressableAssetSettingsDefaultObject.Settings;
        List<AddressableAssetEntry> listData = new List<AddressableAssetEntry>();
        Settings.GetAllAssets(listData, includeSubObjects);
        return listData;
    }

    /// <summary>
    /// 增加修改回调
    /// </summary>
    /// <param name="callBack"></param>
    public static void AddCallBackForAssetChange(Action<AddressableAssetSettings, ModificationEvent, object> callBack)
    {
        AddressableAssetSettings Settings = AddressableAssetSettingsDefaultObject.Settings;
        Settings.OnModification += callBack;
    }

    /// <summary>
    /// 给某分组添加资源
    /// </summary>
    /// <param name="group"></param>
    /// <param name="assetPath"></param>
    /// <param name="address"></param>
    /// <returns></returns>
    public static AddressableAssetEntry AddAssetEntry(AddressableAssetGroup group, string assetPath, string address)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (guid.IsNull())
        {
            return null;
        }
        else
        {
            AddressableAssetSettings Settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = group.entries.FirstOrDefault(e => e.guid == guid);
            if (entry == null)
            {
                entry = Settings.CreateOrMoveEntry(guid, group);
            }
            entry.address = address;
            entry.SetLabel(group.Name, true, false, false);
            return entry;
        }
    }


    /// <summary>
    /// 移动资源
    /// </summary>
    public static void MoveAssetEntry(AddressableAssetEntry entry, AddressableAssetGroup targetParent, bool readOnly = false, bool postEvent = true)
    {
        AddressableAssetSettings Settings = AddressableAssetSettingsDefaultObject.Settings;
        Settings.MoveEntry(entry, targetParent, readOnly, postEvent);
    }

    /// <summary>
    /// 移动资源
    /// </summary>
    public static void MoveAssetEntry(AddressableAssetEntry entry, string groupName, bool readOnly = false, bool postEvent = true)
    {
        AddressableAssetGroup group = FindOrCreateGroup(groupName);
        MoveAssetEntry(entry, group, readOnly, postEvent);
    }

    /// <summary>
    /// 关闭所有label
    /// </summary>
    /// <param name="entry"></param>
    public static void ClearAllLabel(AddressableAssetEntry entry)
    {
        AddressableAssetSettings Settings = AddressableAssetSettingsDefaultObject.Settings;
        List<string> listLabel = Settings.GetLabels();

        for (int i = 0; i < listLabel.Count; i++)
        {
            entry.SetLabel(listLabel[i], false);
        }
    }

    /// <summary>
    /// 设置label
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="listLabel"></param>
    public static void SetLabel(AddressableAssetEntry entry, List<string> listLabel)
    {
        for (int i = 0; i < listLabel.Count; i++)
        {
            entry.SetLabel(listLabel[i], true);
        }
    }
}