using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

public class IconHandler : BaseHandler<IconHandler, IconManager>
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
        SpriteAtlas loadAtlas = LoadAddressablesUtil.LoadAssetSync<SpriteAtlas>($"{IconManager.PathSpriteAtlas}/{tag}.spriteatlas");
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
        manager.GetUISpriteByName("icon_unknow", callBack);
    }

    /// <summary>
    /// 获取图标sprite
    /// </summary>
    /// <param name="spriteData">前图集 后名字 用,分割</param>
    public void GetIconSprite(string spriteData, Action<Sprite> callBack)
    {
        string[] spriteArrayData = spriteData.SplitForArrayStr(',');

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
        if (spriteArrayData[0].Equals("0"))
        {
            manager.GetUISpriteByName(spriteArrayData[1], callBackForComplete);
        }
        else if (spriteArrayData[0].Equals("1"))
        {
            manager.GetItemsSpriteByName(spriteArrayData[1], callBackForComplete);
        }
        else if (spriteArrayData[0].Equals("2"))
        {
            manager.GetSkySpriteByName(spriteArrayData[1], callBackForComplete);
        }
    }
}