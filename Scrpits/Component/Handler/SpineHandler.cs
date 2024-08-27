using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;

public class SpineHandler : BaseHandler<SpineHandler, SpineManager>
{
    /// <summary>
    /// Ԥ��������
    /// </summary>
    public void PreLoadSkeletonDataAsset(List<string> listPreAssetName, Action<Dictionary<string, SkeletonDataAsset>> actionForComplete)
    {
        int completeNum = 0;
        Dictionary<string, SkeletonDataAsset> dicData = new Dictionary<string, SkeletonDataAsset>();
        for (int i = 0; i < listPreAssetName.Count; i++)
        {
            var itemData = listPreAssetName[i];
            manager.GetSkeletonDataAsset(itemData, (skeletonDataAsset) =>
            {
                dicData.Add(itemData, skeletonDataAsset);
                completeNum++;
                if (completeNum == listPreAssetName.Count)
                {
                    actionForComplete?.Invoke(dicData);
                }
            });
        }
    }

    /// <summary>
    /// ����SkeletonAnimation
    /// </summary>
    public SkeletonAnimation AddSkeletonAnimation(GameObject targetObj, string assetName, string[] skinArray = null)
    {
        var skeletonDataAsset = manager.GetSkeletonDataAssetSync(assetName);
        SkeletonAnimation skeletonAnimation = SkeletonAnimation.AddToGameObject(targetObj, skeletonDataAsset);
        if (skinArray != null)
        {
            ChangeSkeletonSkin(skeletonAnimation.skeleton, skinArray);
        }
        return skeletonAnimation;
    }

    /// <summary>
    /// ���ù�������
    /// </summary>
    public void SetSkeletonDataAsset(SkeletonAnimation skeletonAnimation, string assetName, bool isSync = true)
    {
        Action<SkeletonDataAsset> actionForSetData = (skeletonDataAsset) =>
        {
            if (skeletonAnimation != null && skeletonDataAsset != null)
            {
                skeletonAnimation.skeletonDataAsset = skeletonDataAsset;
                skeletonAnimation.Initialize(true);
            }
        };

        if (isSync)
        {
            var skeletonDataAsset = manager.GetSkeletonDataAssetSync(assetName);
            actionForSetData?.Invoke(skeletonDataAsset);
        }
        else
        {
            manager.GetSkeletonDataAsset(assetName, (skeletonDataAsset) =>
            {
                actionForSetData?.Invoke(skeletonDataAsset);
            });
        }
    }
    public void SetSkeletonDataAsset(SkeletonGraphic skeletonGraphic, string assetName, bool isSync = true)
    {
        Action<SkeletonDataAsset> actionForSetData = (skeletonDataAsset) =>
        {
            if (skeletonGraphic != null && skeletonDataAsset != null)
            {
                skeletonGraphic.skeletonDataAsset = skeletonDataAsset;
                skeletonGraphic.Initialize(true);

                Atlas atlas = skeletonDataAsset.atlasAssets[0].GetAtlas();
                if (atlas.Pages.Count > 1)
                {
                    skeletonGraphic.allowMultipleCanvasRenderers = true;
                }
                else
                {
                    skeletonGraphic.allowMultipleCanvasRenderers = false;
                }
            }
        };
        if (isSync)
        {
            var skeletonDataAsset = manager.GetSkeletonDataAssetSync(assetName);
            actionForSetData?.Invoke(skeletonDataAsset);
        }
        else
        {
            manager.GetSkeletonDataAsset(assetName, (skeletonDataAsset) =>
            {
                actionForSetData?.Invoke(skeletonDataAsset);
            });
        }
    }

    /// <summary>
    /// ����skeletonDataAsset
    /// </summary>
    /// <returns></returns>
    public SkeletonGraphic AddSkeletonGraphic(GameObject targetObj, string assetName, string[] skinArray, Material material)
    {
        var skeletonDataAsset = manager.GetSkeletonDataAssetSync(assetName);
        SkeletonGraphic skeletonGraphic = SkeletonGraphic.AddSkeletonGraphicComponent(targetObj, skeletonDataAsset, material);
        ChangeSkeletonSkin(skeletonGraphic.Skeleton, skinArray);
        return skeletonGraphic;
    }

    /// <summary>
    /// �ı�Ƥ��
    /// </summary>
    public void ChangeSkeletonSkin(Skeleton skeleton, string[] skinArray)
    {
        Skin newSkin = new Skin($"skin_{skeleton.Data.Name}");
        for (int i = 0; i < skinArray.Length; i++)
        {
            var itemSkinName = skinArray[i];
            if (itemSkinName.IsNull())
            {
                continue;
            }
            var itemSkin = manager.GetSkeletonDataSkin(skeleton, itemSkinName);
            if (itemSkin == null)
            {
                continue;
            }
            newSkin.AddSkin(itemSkin);
        }
        skeleton.SetSkin(newSkin);
        skeleton.SetSlotsToSetupPose();
    }

    /// <summary>
    /// �Ƴ�Ƥ��
    /// </summary>
    public void RemoveSkeletonSkin(Skeleton skeleton, string slotName)
    {
        skeleton.SetAttachment(slotName, null);
    }

    /// <summary>
    /// �Ż�Ƥ��
    /// </summary>
    public void OptimizeSkeletonAnimationSkin(SkeletonAnimation skeletonAnimation, Material oldMat, Texture2D oldTex, out Material newMat, out Texture2D newTex)
    {
        Skeleton skeleton = skeletonAnimation.skeleton;
        Skin previousSkin = skeletonAnimation.Skeleton.Skin;
        if (oldMat)
            Destroy(oldMat);
        if (oldTex)
            Destroy(oldTex);
        Skin repackedSkin = previousSkin.GetRepackedSkin("optimize skin", skeletonAnimation.SkeletonDataAsset.atlasAssets[0].PrimaryMaterial, out newMat, out newTex);
        previousSkin.Clear();

        skeleton.Skin = repackedSkin;
        skeleton.SetSlotsToSetupPose();
        skeletonAnimation.AnimationState.Apply(skeleton);

        AtlasUtilities.ClearCache();
        Resources.UnloadUnusedAssets();
    }

    //public void CreateSprite(SkeletonAnimation skeletonAnimation,string SpriteName)
    //{
    //    var skeletonDataAsset = skeletonAnimation.skeletonDataAsset;
    //    var atals = skeletonDataAsset.atlasAssets[0].GetAtlas();
    //    AtlasRegion atlasRegion = atals.FindRegion(SpriteName);
    //}

    /// <summary>
    /// ���Ŷ���
    /// </summary>
    public TrackEntry PlayAnim(SkeletonAnimation skeletonAnimation, SpineAnimationStateEnum spineAnimationState, bool isLoop)
    {
        if (skeletonAnimation == null)
        {
            LogUtil.LogError("���Ŷ���ʧ�� ȱ��SkeletonAnimation��Դ");
            return null;
        }
        return PlayAnim(skeletonAnimation.skeletonDataAsset, skeletonAnimation.AnimationState, spineAnimationState, isLoop);
    }

    public TrackEntry PlayAnim(SkeletonGraphic skeletonGraphic, SpineAnimationStateEnum spineAnimationState, bool isLoop)
    {
        if (skeletonGraphic == null)
        {
            LogUtil.LogError("���Ŷ���ʧ�� ȱ��SkeletonGraphic��Դ");
            return null;
        }
        return PlayAnim(skeletonGraphic.skeletonDataAsset, skeletonGraphic.AnimationState, spineAnimationState, isLoop);
    }

    public TrackEntry PlayAnim(SkeletonDataAsset skeletonDataAsset, Spine.AnimationState animationState, SpineAnimationStateEnum spineAnimationState, bool isLoop)
    {
        if (skeletonDataAsset == null)
        {
            LogUtil.LogError("���Ŷ���ʧ�� ȱ��skeletonDataAsset��Դ");
            return null;
        }
        var animName = manager.GetSkeletonDataAnimName(skeletonDataAsset, spineAnimationState);
        if (animName.IsNull())
        {
            LogUtil.LogError("���Ŷ���ʧ�� ȱ��skeletonAnimation��Դ");
            return null;
        }
        return animationState.SetAnimation(0, animName, isLoop);
    }
}
