using System;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

public partial class IconManager : BaseManager
{
    //UI图标
    public SpriteAtlas atlasForUI;
    public SpriteAtlas atlasForItems;
    public SpriteAtlas atlasForSky;

    public Dictionary<string, Sprite> dicUI = new Dictionary<string, Sprite>();
    public Dictionary<string, Sprite> dicItems = new Dictionary<string, Sprite>();
    public Dictionary<string, Sprite> dicSky = new Dictionary<string, Sprite>();

    public static string PathSpriteAtlas = "Assets/LoadResources/Textures/SpriteAtlas";

    public string PathSpriteAtlasForUI = $"{PathSpriteAtlas}/AtlasForUI.spriteatlas";
    public string PathSpriteAtlasForItems = $"{PathSpriteAtlas}/AtlasForItems.spriteatlas";
    public string PathSpriteAtlasForSky = $"{PathSpriteAtlas}/AtlasForSky.spriteatlas";

    //贴图列表
    public Dictionary<string, Texture2D> dicTex = new Dictionary<string, Texture2D>();

    /// <summary>
    /// 获取贴图 - 同步
    /// </summary>
    public Texture2D GetTextureSync(string texResPath)
    {
       return GetModelForAddressablesSync(dicTex, texResPath);
    }

    /// <summary>
    /// 获取贴图-异步
    /// </summary>
    public void GetTexture(string texResPath,Action<Texture2D> callBackForComplete) 
    {
        GetModelForAddressables(dicTex, texResPath, callBackForComplete);
    }


    /// <summary>
    /// 根据名字获取UI图标
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callBack"></param>
    public void GetUISpriteByName(string name, Action<Sprite> callBack)
    {
        GetSpriteByName(dicUI, ref atlasForUI, PathSpriteAtlasForUI, name, callBack);
    }

    /// <summary>
    ///  根据名字获取物品图标
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callBack"></param>
    public void GetItemsSpriteByName(string name, Action<Sprite> callBack)
    {
        GetSpriteByName(dicUI, ref atlasForItems, PathSpriteAtlasForItems, name, callBack);
    }

    /// <summary>
    /// 根据名字获取天空图标
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callBack"></param>
    public void GetSkySpriteByName(string name, Action<Sprite> callBack)
    {
        GetSpriteByName(dicSky, ref atlasForSky, PathSpriteAtlasForSky, name, callBack);
    }
}