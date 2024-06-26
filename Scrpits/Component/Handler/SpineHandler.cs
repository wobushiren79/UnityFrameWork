using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpineHandler : BaseHandler<SpineHandler, SpineManager>
{
    /// <summary>
    /// 设置SkeletonAnimation
    /// </summary>
    public SkeletonAnimation SetSkeletonAnimation(GameObject targetObj, string assetName, string[] skinArray)
    {
        var skeletonDataAsset = manager.GetSkeletonDataAssetSync(assetName);
        SkeletonAnimation skeletonAnimation = SkeletonAnimation.AddToGameObject(targetObj, skeletonDataAsset);
        ChangeSkeletonSkin(skeletonAnimation.skeleton, skinArray);
        return skeletonAnimation;
    }

    /// <summary>
    /// 设置skeletonDataAsset
    /// </summary>
    /// <returns></returns>
    public SkeletonGraphic SetSkeletonGraphic(GameObject targetObj, string assetName, string[] skinArray, Material material)
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
}
