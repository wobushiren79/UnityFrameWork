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

    public Dictionary<string,Sprite> dicUI = new Dictionary<string, Sprite>();
    public Dictionary<string, Sprite> dicItems = new Dictionary<string, Sprite>();
    public Dictionary<string, Sprite> dicSky = new Dictionary<string, Sprite>();

    public Dictionary<string, Texture2D> dicTextureUI = new Dictionary<string, Texture2D>();

    public static string PathSpriteAtlas = "Assets/Texture/SpriteAtlas";

    public string PathSpriteAtlasForUI = $"{PathSpriteAtlas}/SpriteAtlasForUI.spriteatlas";
    public string PathSpriteAtlasForItems = $"{PathSpriteAtlas}/SpriteAtlasForItems.spriteatlas";
    public string PathSpriteAtlasForSky = $"{PathSpriteAtlas}/SpriteAtlasForSky.spriteatlas";

    /// <summary>
    /// 根据名字获取UI图标
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callBack"></param>
    public void GetUISpriteByName(string name,Action<Sprite> callBack)
    {
        Action<SpriteAtlas> callBackForSpriteAtlas = (spriteAtlas) =>
        {
            this.atlasForUI = spriteAtlas;
            GetSpriteByName(dicUI, spriteAtlas, PathSpriteAtlasForUI, name, null, callBack);
        };
        GetSpriteByName(dicUI, atlasForUI, PathSpriteAtlasForUI, name, callBackForSpriteAtlas, callBack);
    }

    /// <summary>
    ///  根据名字获取物品图标
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callBack"></param>
    public void GetItemsSpriteByName(string name, Action<Sprite> callBack)
    {
        Action<SpriteAtlas> callBackForSpriteAtlas = (spriteAtlas) =>
        {
            this.atlasForItems = spriteAtlas;
            GetSpriteByName(dicUI, atlasForItems, PathSpriteAtlasForItems, name, null, callBack);
        };
        GetSpriteByName(dicUI, atlasForItems, PathSpriteAtlasForItems, name, callBackForSpriteAtlas, callBack);
    }

    /// <summary>
    /// 根据名字获取天空图标
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callBack"></param>
    public void GetSkySpriteByName(string name, Action<Sprite> callBack)
    {
        Action<SpriteAtlas> callBackForSpriteAtlas = (spriteAtlas) =>
        {
            this.atlasForSky = spriteAtlas;
            GetSpriteByName(dicSky, atlasForSky, PathSpriteAtlasForSky, name, null, callBack);
        };
        GetSpriteByName(dicSky, atlasForSky, PathSpriteAtlasForSky, name, callBackForSpriteAtlas, callBack);
    }

    public Texture2D GetTexture2DByName(string name)
    {
        return GetModel(dicTextureUI, "texture/ui", name);
    }
}