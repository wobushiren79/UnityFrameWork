using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity.AttachmentTools;
using Spine;
using Spine.Unity;
using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
public class SpineManager : BaseManager
{
    //所有的spine数据
    public Dictionary<string, SkeletonDataAsset> dicSkeletonDataAsset = new Dictionary<string, SkeletonDataAsset>();
    //所有的皮肤
    public Dictionary<string, Skin> dicSkeletonDataSkin = new Dictionary<string, Skin>();
    //spine数据路径
    public string pathSkeletonData = "Assets/LoadResources/Spine";

    /// <summary>
    /// 获取spine资源 同步
    /// </summary>
    public SkeletonDataAsset GetSkeletonDataAssetSync(string assetName)
    {
        if (assetName.IsNull())
            return null;
        return GetModelForAddressablesSync(dicSkeletonDataAsset, $"{pathSkeletonData}/{assetName}");
    }

    /// <summary>
    /// 获取spine资源 异步
    /// </summary>
    public void GetSkeletonDataAsset(string assetName, Action<SkeletonDataAsset> actionForComplete)
    {
        if (assetName.IsNull())
        {
            actionForComplete?.Invoke(null);
            return;
        }
        GetModelForAddressables(dicSkeletonDataAsset, $"{pathSkeletonData}/{assetName}", actionForComplete);
    }

    /// <summary>
    /// 获取SkeletonData 同步
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public SkeletonData GetSkeletonDataSync(string assetName)
    {
        var skeletonDataAsset = GetSkeletonDataAssetSync(assetName);
        if (skeletonDataAsset == null)
        {
            return null;
        }
        //false为显示错误日志 ture为不显示错误日志
        var skeletonData = skeletonDataAsset.GetSkeletonData(false);
        if (skeletonData == null)
        {
            return null;
        }
        return skeletonData;
    }

    /// <summary>
    /// 获取SkeletonData 异步
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public void GetSkeletonData(string assetName, Action<SkeletonData> actionForComplete)
    {
        GetSkeletonDataAsset(assetName, (skeletonDataAsset) =>
        {
            if (skeletonDataAsset == null)
            {
                actionForComplete?.Invoke(null);
                return;
            }
            //false为显示错误日志 ture为不显示错误日志
            var skeletonData = skeletonDataAsset.GetSkeletonData(false);
            if (skeletonData == null)
            {
                actionForComplete?.Invoke(null);
                return;
            }
            actionForComplete?.Invoke(skeletonData);
        });
    }

    /// <summary>
    /// 获取皮肤 同步
    /// </summary>
    public Skin GetSkeletonDataSkinSync(string skinName, string assetName)
    {
        if (skinName.IsNull())
        {
            return null;
        }
        string keyName = $"{assetName}_{skinName}";
        if (dicSkeletonDataSkin.TryGetValue(keyName, out Skin targetSkin))
        {
            return targetSkin;
        }
        else
        {
            var skeletonData = GetSkeletonDataSync(assetName);
            Skin targetSkinNew = skeletonData.FindSkin(skinName);
            if (targetSkinNew == null)
            {
                return null;
            }
            dicSkeletonDataSkin.Add(keyName, targetSkinNew);
            return targetSkinNew;
        }
    }

    /// <summary>
    /// 获取皮肤 异步
    /// </summary>
    public void GetSkeletonDataSkin(string skinName, string assetName, Action<Skin> actitonForComplete)
    {
        if (skinName.IsNull())
        {
            actitonForComplete?.Invoke(null);
            return;
        }
        string keyName = $"{assetName}_{skinName}";
        if (dicSkeletonDataSkin.TryGetValue(keyName, out Skin targetSkin))
        {
            actitonForComplete?.Invoke(targetSkin);
        }
        else
        {
            GetSkeletonData(assetName, (skeletonData) =>
            {

                Skin targetSkinNew = skeletonData.FindSkin(skinName);
                if (targetSkinNew == null)
                {
                    actitonForComplete?.Invoke(null);
                    return;
                }
                dicSkeletonDataSkin.Add(keyName, targetSkinNew);
                actitonForComplete?.Invoke(targetSkinNew);
            });
        }
    }
}
