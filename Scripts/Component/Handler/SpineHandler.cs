using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class SpineHandler : BaseHandler<SpineHandler, SpineManager>
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
    public SkeletonAnimation AddSkeletonAnimation(GameObject targetObj, string assetName, Dictionary<string, SpineSkinBean> skinData = null)
    {
        var skeletonDataAsset = GetSkeletonDataAssetWithMod(assetName);
        // 4.3: AddToGameObject 返回 SkeletonComponents<SkeletonRenderer, SkeletonAnimation>
        var components = SkeletonAnimation.AddToGameObject(targetObj, skeletonDataAsset);
        SkeletonAnimation skeletonAnimation = components.skeletonAnimation;
        //世界骨架(战斗/基地生物)强制开线程化: 全局线程化保持关闭(UI骨架线程化时, 网格生成会被工作线程竞态读到无约束中间态致UI闪烁; spine-unity三态语义只支持全局关时单独开、不支持全局开时单独关, 故全局关+本处单独开, UI骨架不走本方法保持主线程串行)
        skeletonAnimation.ThreadedAnimation = SettingsTriState.Enable;
        components.skeletonRenderer.ThreadedMeshGeneration = SettingsTriState.Enable;
        if (skinData != null)
        {
            ChangeSkeletonSkin(skeletonAnimation.skeleton, skinData);
        }
        return skeletonAnimation;
    }

    public void SetSkeletonDataAsset(SkeletonAnimation skeletonAnimation, SkeletonDataAsset skeletonDataAsset)
    {
        if (skeletonAnimation != null && skeletonDataAsset != null)
        {
            //换骨前先清空旧动画队列：spine-unity 4.3 的 OnAnimationDisposed 在非 FullUpdate 模式(UpdateWhenInvisible=None 时不可见即触发)下会把旧动画重新 Apply 到骨架；
            //而换骨时 Initialize(true) 会把骨架懒重建为新资源, 旧动画的骨骼索引超出新骨架骨骼数 → IndexOutOfRangeException
            skeletonAnimation.AnimationState?.ClearTracks();
            skeletonAnimation.skeletonDataAsset = skeletonDataAsset;
            skeletonAnimation.Initialize(true);
        }
    }

    public void SetSkeletonDataAsset(SkeletonGraphic skeletonGraphic, SkeletonDataAsset skeletonDataAsset)
    {
        if (skeletonGraphic != null && skeletonDataAsset != null)
        {
            //同 SkeletonAnimation 重载：换骨前先清空旧动画队列, 避免旧动画在 dispose 时被 Apply 到(懒重建的)新骨架导致骨骼索引越界
            (skeletonGraphic.Animation as SkeletonAnimation)?.AnimationState?.ClearTracks();
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
    }

    /// <summary>
    /// 设置骨骼数据
    /// </summary>
    public void SetSkeletonDataAsset(SkeletonAnimation skeletonAnimation, string assetName, bool isSync = true)
    {
        Action<SkeletonDataAsset> actionForSetData = (skeletonDataAsset) =>
        {
            //骨骼资源与当前一致时跳过, 避免复用同一魔物对象时无谓重建
            if (skeletonAnimation != null && skeletonAnimation.skeletonDataAsset == skeletonDataAsset)
                return;
            SetSkeletonDataAsset(skeletonAnimation, skeletonDataAsset);
        };

        if (isSync)
        {
            var skeletonDataAsset = GetSkeletonDataAssetWithMod(assetName);
            actionForSetData?.Invoke(skeletonDataAsset);
        }
        else
        {
            GetSkeletonDataAssetWithMod(assetName, (skeletonDataAsset) =>
            {
                actionForSetData?.Invoke(skeletonDataAsset);
            });
        }
    }
    public void SetSkeletonDataAsset(SkeletonGraphic skeletonGraphic, string assetName, bool isSync = true)
    {
        Action<SkeletonDataAsset> actionForSetData = (skeletonDataAsset) =>
        {
            //骨骼资源与当前一致时跳过, 避免复用同一魔物对象时无谓重建
            if (skeletonGraphic != null && skeletonGraphic.skeletonDataAsset == skeletonDataAsset)
                return;
            SetSkeletonDataAsset(skeletonGraphic, skeletonDataAsset);
        };
        if (isSync)
        {
            var skeletonDataAsset = GetSkeletonDataAssetWithMod(assetName);
            actionForSetData?.Invoke(skeletonDataAsset);
        }
        else
        {
            GetSkeletonDataAssetWithMod(assetName, (skeletonDataAsset) =>
            {
                actionForSetData?.Invoke(skeletonDataAsset);
            });
        }
    }

    /// <summary>
    /// 增加skeletonDataAsset
    /// </summary>
    /// <returns></returns>
    public SkeletonGraphic AddSkeletonGraphic(GameObject targetObj, string assetName, Dictionary<string, SpineSkinBean> skinData, Material material)
    {
        var skeletonDataAsset = GetSkeletonDataAssetWithMod(assetName);
        //创建扩展版 SkeletonGraphic（修复 RectMask2D 按 rect 整体误剔除：非居中骨架 pos 偏移出 mask 时内容仍正常显示）
        //逻辑参照 SkeletonGraphic.AddSkeletonGraphicAnimationComponents，仅把渲染组件换成 SkeletonGraphicExtend
        SkeletonGraphicExtend skeletonGraphic = targetObj.AddComponent<SkeletonGraphicExtend>();
        if (skeletonDataAsset != null)
        {
            skeletonGraphic.material = material;
            skeletonGraphic.skeletonDataAsset = skeletonDataAsset;
            skeletonGraphic.Initialize(false);
        }
        SkeletonAnimation skeletonAnimation = targetObj.AddComponent<SkeletonAnimation>();
        if (skeletonDataAsset != null)
        {
            skeletonAnimation.Initialize(false);
        }
        skeletonGraphic.Animation = skeletonAnimation;
        CanvasRenderer canvasRenderer = targetObj.GetComponent<CanvasRenderer>();
        if (canvasRenderer) canvasRenderer.cullTransparentMesh = false;
        if (skinData != null)
        {
            ChangeSkeletonSkin(skeletonGraphic.Skeleton, skinData);
        }
        return skeletonGraphic;
    }

    /// <summary>
    /// 改变皮肤
    /// </summary>
    public void ChangeSkeletonSkin(Skeleton skeleton, Dictionary<string, SpineSkinBean> dicSkin)
    {
        if (skeleton == null || skeleton.Data == null)
        {
            LogUtil.LogError("ChangeSkeletonSkin失败 缺少Skeleton资源");
            return;
        }
        Skin newSkin = new Skin($"skin_{skeleton.Data.Hash}");
        if (dicSkin != null)
        {
            foreach (var itemData in dicSkin)
            {
                var itemSkinName = itemData.Key;
                if (itemSkinName.IsNull())
                {
                    continue;
                }
                var itemSkin = manager.GetSkeletonDataSkin(skeleton, itemSkinName);
                if (itemSkin == null)
                {
                    continue;
                }
                //添加皮肤
                newSkin.AddSkin(itemSkin);
            }
        }
        skeleton.SetSkin(newSkin);
        skeleton.SetupPoseSlots();

        //改变皮肤颜色
        if (dicSkin != null)
        {
            foreach (var itemData in dicSkin)
            {
                var itemSkinName = itemData.Key;
                var itemSkinData = itemData.Value;
                if (itemSkinName.IsNull())
                {
                    continue;
                }
                //改变皮肤颜色
                if (itemSkinData.hasColor)
                {
                    int lastSlashIndex = itemSkinName.LastIndexOf('/');
                    string slot = itemSkinName.Substring(lastSlashIndex + 1);
                    ChangeSlotColor(skeleton, slot, itemSkinData.skinColor.GetColor());
                }
            }
        }
    }

    /// <summary>
    /// 按单个皮肤名整皮替换（幻化药 ui_show_skin 等指定骨架内皮肤的场景；皮肤名不存在时 GetSkeletonDataSkin 内部报错并保持原皮肤）
    /// </summary>
    public void ChangeSkeletonSkin(Skeleton skeleton, string skinName)
    {
        if (skeleton == null || skeleton.Data == null)
        {
            LogUtil.LogError("ChangeSkeletonSkin失败 缺少Skeleton资源");
            return;
        }
        if (skinName.IsNull())
            return;
        Skin targetSkin = manager.GetSkeletonDataSkin(skeleton, skinName);
        if (targetSkin == null)
            return;
        skeleton.SetSkin(targetSkin);
        skeleton.SetupPoseSlots();
    }

    /// <summary>
    /// 按多个皮肤名叠加换肤（幻化药 ui_show_skin「|」分隔组合皮肤场景，如 CherryTaleSpine 的 Eye_01|Mouth_01；
    /// 未在组合内的部件自动回落骨架默认皮肤；等价 Dictionary 重载但不带染色）
    /// </summary>
    public void ChangeSkeletonSkin(Skeleton skeleton, params string[] skinNames)
    {
        if (skeleton == null || skeleton.Data == null)
        {
            LogUtil.LogError("ChangeSkeletonSkin失败 缺少Skeleton资源");
            return;
        }
        if (skinNames == null || skinNames.Length == 0)
            return;
        Skin newSkin = new Skin($"skin_combo_{skeleton.Data.Hash}");
        foreach (var skinName in skinNames)
        {
            if (skinName.IsNull())
                continue;
            var itemSkin = manager.GetSkeletonDataSkin(skeleton, skinName);
            if (itemSkin == null)
            {
                continue;
            }
            //添加皮肤
            newSkin.AddSkin(itemSkin);
        }
        skeleton.SetSkin(newSkin);
        skeleton.SetupPoseSlots();
    }

    /// <summary>
    /// 设置部件颜色
    /// </summary>
    public void ChangeSlotColor(Skeleton skeleton, string slotName, Color color)
    {
        // 根据Slot名称查找Slot
        var slot = skeleton.FindSlot(slotName);
        if (slot == null)
        {
            LogUtil.LogError($"ChangeSlotColor没有找到slotName_{slotName}");
            return;
        }
        // 修改Slot的颜色
        slot.SetColor(color);
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
        skeleton.SetupPoseSlots();
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

    #region Mod资源加载支持

    /// <summary>
    /// 同步获取SkeletonDataAsset，若assetName属于已加载Mod则走Mod加载路径，并缓存至SpineManager
    /// </summary>
    private SkeletonDataAsset GetSkeletonDataAssetWithMod(string assetName)
    {
        string modName = ModHandler.Instance.GetModNameForAsset(assetName);
        if (modName != null)
        {
            var modAsset = ModHandler.Instance.LoadAssetSync<SkeletonDataAsset>(modName, assetName);
            if (modAsset != null)
            {
                manager.dicSkeletonDataAsset[$"{assetName}"] = modAsset;
                return modAsset;
            }
        }
        return manager.GetSkeletonDataAssetSync(assetName);
    }

    /// <summary>
    /// 异步获取SkeletonDataAsset，若assetName属于已加载Mod则走Mod加载路径，并缓存至SpineManager
    /// </summary>
    private void GetSkeletonDataAssetWithMod(string assetName, Action<SkeletonDataAsset> callback)
    {
        string modName = ModHandler.Instance.GetModNameForAsset(assetName);
        if (modName != null)
        {
            ModHandler.Instance.LoadAsset<SkeletonDataAsset>(modName, assetName, (modAsset) =>
            {
                if (modAsset != null)
                {
                    manager.dicSkeletonDataAsset[$"{assetName}"] = modAsset;
                    callback?.Invoke(modAsset);
                }
                else
                {
                    manager.GetSkeletonDataAsset(assetName, callback);
                }
            });
            return;
        }
        manager.GetSkeletonDataAsset(assetName, callback);
    }

    #endregion

    #region  动画相关
    /// <summary>
    /// 播放动画
    /// </summary>
    public TrackEntry PlayAnim(
        SkeletonAnimation skeletonAnimation, SpineAnimationStateEnum spineAnimationState, bool isLoop,
        string animNameAppoint = null, float animStartTime = 0, float animSpeed = 1)
    {
        if (skeletonAnimation == null)
        {
            LogUtil.LogError("播放动画失败 缺少SkeletonAnimation资源");
            return null;
        }
        return PlayAnim(skeletonAnimation.skeletonDataAsset, skeletonAnimation.AnimationState, spineAnimationState, isLoop, animNameAppoint, animStartTime, animSpeed);
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    public TrackEntry PlayAnim(
        SkeletonGraphic skeletonGraphic, SpineAnimationStateEnum spineAnimationState, bool isLoop,
        string animNameAppoint = null, float animStartTime = 0, float animSpeed = 1)
    {
        if (skeletonGraphic == null)
        {
            LogUtil.LogError("播放动画失败 缺少SkeletonGraphic资源");
            return null;
        }
        return PlayAnim(skeletonGraphic.skeletonDataAsset, ((SkeletonAnimation)skeletonGraphic.Animation).AnimationState, spineAnimationState, isLoop, animNameAppoint, animStartTime, animSpeed);
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    public TrackEntry PlayAnim(
        SkeletonDataAsset skeletonDataAsset, Spine.AnimationState animationState, SpineAnimationStateEnum spineAnimationState, bool isLoop,
        string animNameAppoint = null, float animStartTime = 0, float animSpeed = 1)
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
        TrackEntry trackEntry = animationState.SetAnimation(0, animName, isLoop);
        if (animStartTime != 0)
        {
            trackEntry.TrackTime = animStartTime;
        }
        if (trackEntry != null)
        {
            trackEntry.TimeScale = animSpeed;
        }
        return trackEntry;
    }

    /// <summary>
    /// 添加动画
    /// </summary>
    public TrackEntry AddAnimation(
        SkeletonAnimation skeletonAnimation, int trackIndex, SpineAnimationStateEnum spineAnimationState, bool isLoop, float delay,
        string animNameAppoint = null, float animStartTime = 0, float animSpeed = 1)
    {
        if (skeletonAnimation == null)
        {
            LogUtil.LogError("播放动画失败 缺少SkeletonAnimation资源");
            return null;
        }
        return AddAnimation(skeletonAnimation.skeletonDataAsset, skeletonAnimation.AnimationState, trackIndex, spineAnimationState, isLoop, delay, animNameAppoint, animStartTime, animSpeed);
    }

    /// <summary>
    /// 添加动画
    /// </summary>
    public TrackEntry AddAnimation(
        SkeletonGraphic skeletonGraphic, int trackIndex, SpineAnimationStateEnum spineAnimationState, bool isLoop, float delay,
        string animNameAppoint = null, float animStartTime = 0, float animSpeed = 1)
    {
        if (skeletonGraphic == null)
        {
            LogUtil.LogError("播放动画失败 缺少SkeletonGraphic资源");
            return null;
        }
        return AddAnimation(skeletonGraphic.skeletonDataAsset, ((SkeletonAnimation)skeletonGraphic.Animation).AnimationState, trackIndex, spineAnimationState, isLoop, delay, animNameAppoint, animStartTime, animSpeed);
    }

    /// <summary>
    /// 添加动画
    /// </summary>
    public TrackEntry AddAnimation(
        SkeletonDataAsset skeletonDataAsset, Spine.AnimationState animationState, int trackIndex, SpineAnimationStateEnum spineAnimationState, bool isLoop, float delay,
        string animNameAppoint = null, float animStartTime = 0, float animSpeed = 1)
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
        TrackEntry trackEntry = animationState.AddAnimation(trackIndex, animName, isLoop, delay);
        if (animStartTime != 0)
        {
            trackEntry.TrackTime = animStartTime;
        }
        if (trackEntry != null)
        {
            trackEntry.TimeScale = animSpeed;
        }
        return trackEntry;
    }
    #endregion
}
