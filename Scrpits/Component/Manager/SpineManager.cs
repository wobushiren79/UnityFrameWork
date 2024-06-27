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
    //���е�spine����
    public Dictionary<string, SkeletonDataAsset> dicSkeletonDataAsset = new Dictionary<string, SkeletonDataAsset>();
    //���е�Ƥ��
    public Dictionary<string, Skin> dicSkeletonDataSkin = new Dictionary<string, Skin>();
    //spine����·��
    public string pathSkeletonData = "Assets/LoadResources/Spine";

    /// <summary>
    /// ��ȡspine��Դ ͬ��
    /// </summary>
    public SkeletonDataAsset GetSkeletonDataAssetSync(string assetName)
    {
        if (assetName.IsNull())
            return null;
        return GetModelForAddressablesSync(dicSkeletonDataAsset, $"{pathSkeletonData}/{assetName}");
    }

    /// <summary>
    /// ��ȡspine��Դ �첽
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
    /// ��ȡSkeletonData ͬ��
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
        //falseΪ��ʾ������־ tureΪ����ʾ������־
        var skeletonData = skeletonDataAsset.GetSkeletonData(false);
        if (skeletonData == null)
        {
            return null;
        }
        return skeletonData;
    }

    /// <summary>
    /// ��ȡSkeletonData �첽
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
            //falseΪ��ʾ������־ tureΪ����ʾ������־
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
    /// ��ȡƤ�� ͬ��
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
    /// ��ȡƤ�� �첽
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
