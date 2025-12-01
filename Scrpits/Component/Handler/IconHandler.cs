using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

public partial class IconHandler : BaseHandler<IconHandler, IconManager>
{
    //是否初始化图集
    protected bool isInitAtlas = false;


    public void InitData()
    {
        if (isInitAtlas)
            return;
        isInitAtlas = true;
        SpriteAtlasManager.atlasRequested += RequestAtlas;
    }

    public void RequestAtlas(string tag, Action<SpriteAtlas> callback)
    {
        // 1. 自定义加载 ab 的逻辑. (这里最好不要用异步加载的方式, 否则会闪现一下空白图片, 因为此时资源还未被加载出来)
        string pathSpriteatlas = $"{IconManager.PathSpriteAtlas}/{tag}.spriteatlas";
        SpriteAtlas loadAtlas = LoadAddressablesUtil.LoadAssetSync<SpriteAtlas>($"{pathSpriteatlas}");
        // 2. 加载完 SpriteAtlas 回传给引擎 
        if (callback != null && loadAtlas != null)
            callback?.Invoke(loadAtlas);
    }

    /// <summary>
    /// 获取未知图标
    /// </summary>
    /// <returns></returns>
    public void GetUnKnowSprite(Action<Sprite> callBack)
    {
        manager.GetSprite($"AtlasFor{SpriteAtlasType.UI.GetEnumName()}", "icon_unknow", callBack);
    }

    /// <summary>
    /// 获取图标
    /// </summary>
    public void GetIconSprite(SpriteAtlasType spriteAtlasType, string spriteName, Action<Sprite> callBack)
    {
        Action<Sprite> callBackForComplete = (sprite) =>
        {
            if (sprite == null)
            {
                GetUnKnowSprite(callBack);
            }
            else
            {
                callBack?.Invoke(sprite);
            }
        };
        manager.GetSprite($"AtlasFor{spriteAtlasType.GetEnumName()}", spriteName, callBackForComplete);
    }
}