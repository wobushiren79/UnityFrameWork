using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SpineHandler : BaseHandler<SpineHandler, SpineManager>
{
    /// <summary>
    /// 预加载数据
    /// </summary>
    public void PreLoadSkeletonDataAsset(List<string> listPreAssetName, Action<Dictionary<string, SkeletonDataAsset>> actionForComplete)
    {
        //容错 做一个去重操作 重复的资源没必要预加载多次
        listPreAssetName = listPreAssetName.DistinctEx();

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
    /// 增加SkeletonAnimation
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
    /// 设置骨骼数据
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
    /// 增加skeletonDataAsset
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
    /// 改变皮肤
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
    /// 移除皮肤
    /// </summary>
    public void RemoveSkeletonSkin(Skeleton skeleton, string slotName)
    {
        skeleton.SetAttachment(slotName, null);
    }

    /// <summary>
    /// 优化皮肤
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

    #region  动画相关
    /// <summary>
    /// 播放动画
    /// </summary>
    public TrackEntry PlayAnim(SkeletonAnimation skeletonAnimation, SpineAnimationStateEnum spineAnimationState, bool isLoop, string animNameAppoint = null)
    {
        if (skeletonAnimation == null)
        {
            LogUtil.LogError("播放动画失败 缺少SkeletonAnimation资源");
            return null;
        }
        return PlayAnim(skeletonAnimation.skeletonDataAsset, skeletonAnimation.AnimationState, spineAnimationState, isLoop, animNameAppoint);
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    public TrackEntry PlayAnim(SkeletonGraphic skeletonGraphic, SpineAnimationStateEnum spineAnimationState, bool isLoop, string animNameAppoint = null)
    {
        if (skeletonGraphic == null)
        {
            LogUtil.LogError("播放动画失败 缺少SkeletonGraphic资源");
            return null;
        }
        return PlayAnim(skeletonGraphic.skeletonDataAsset, skeletonGraphic.AnimationState, spineAnimationState, isLoop, animNameAppoint);
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    public TrackEntry PlayAnim(SkeletonDataAsset skeletonDataAsset, Spine.AnimationState animationState, SpineAnimationStateEnum spineAnimationState, bool isLoop, string animNameAppoint = null)
    {
        if (skeletonDataAsset == null)
        {
            LogUtil.LogError("播放动画失败 缺少skeletonDataAsset资源");
            return null;
        }
        string animName = null;
        if (!animNameAppoint.IsNull())
        {
            animName = animNameAppoint;
        }
        else
        {
            animName = manager.GetSkeletonDataAnimName(skeletonDataAsset, spineAnimationState);
        }

        if (animName.IsNull())
        {
            LogUtil.LogError("播放动画失败 缺少skeletonAnimation资源");
            return null;
        }
        return animationState.SetAnimation(0, animName, isLoop);
    }

    /// <summary>
    /// 添加动画
    /// </summary>
    public TrackEntry AddAnimation(SkeletonAnimation skeletonAnimation, int trackIndex, SpineAnimationStateEnum spineAnimationState, bool isLoop, float delay, string animNameAppoint = null)
    {
        if (skeletonAnimation == null)
        {
            LogUtil.LogError("播放动画失败 缺少SkeletonAnimation资源");
            return null;
        }
        return AddAnimation(skeletonAnimation.skeletonDataAsset, skeletonAnimation.AnimationState, trackIndex, spineAnimationState, isLoop, delay, animNameAppoint);
    }

    /// <summary>
    /// 添加动画
    /// </summary>
    public TrackEntry AddAnimation(SkeletonGraphic skeletonGraphic, int trackIndex, SpineAnimationStateEnum spineAnimationState, bool isLoop, float delay, string animNameAppoint = null)
    {
        if (skeletonGraphic == null)
        {
            LogUtil.LogError("播放动画失败 缺少SkeletonGraphic资源");
            return null;
        }
        return AddAnimation(skeletonGraphic.skeletonDataAsset, skeletonGraphic.AnimationState, trackIndex, spineAnimationState, isLoop, delay, animNameAppoint);
    }

    /// <summary>
    /// 添加动画
    /// </summary>
    public TrackEntry AddAnimation(SkeletonDataAsset skeletonDataAsset, Spine.AnimationState animationState, int trackIndex, SpineAnimationStateEnum spineAnimationState, bool isLoop, float delay, string animNameAppoint = null)
    {
        if (animationState == null)
            return null;
        string animName = null;
        if (!animNameAppoint.IsNull())
        {
            animName = animNameAppoint;
        }
        else
        {
            animName = manager.GetSkeletonDataAnimName(skeletonDataAsset, spineAnimationState);
        }

        if (animName.IsNull())
        {
            LogUtil.LogError("添加动画失败 缺少skeletonAnimation资源");
            return null;
        }
        return animationState.AddAnimation(trackIndex, animName, isLoop, delay);
    }
    #endregion
}
