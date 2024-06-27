using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpineHandler : BaseHandler<SpineHandler, SpineManager>
{
    /// <summary>
    /// 改变皮肤
    /// </summary>
    public void ChangeSkeletonAnimationSkinSync(SkeletonAnimation skeletonAnimation, string assetName, string[] skinArray)
    {
        Skeleton skeleton = skeletonAnimation.skeleton;
        Skin newSkin = new Skin($"skin_{assetName}");
        for (int i = 0; i < skinArray.Length; i++)
        {
            var itemSkinName = skinArray[i];
            if (itemSkinName.IsNull())
            {
                continue;
            }
            var itemSkin = manager.GetSkeletonDataSkinSync(itemSkinName, assetName);
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
