using System;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

public partial class IconManager : BaseManager
{
    //UI图标
    public Dictionary<string, Dictionary<string, Sprite>> dicSprite = new Dictionary<string, Dictionary<string, Sprite>>();
    public Dictionary<string, SpriteAtlas> dicSpriteAtlas = new Dictionary<string, SpriteAtlas>();

    public static string PathSpriteAtlas = "Assets/LoadResources/Textures/SpriteAtlas";

    public string PathSpriteAtlasForUI = $"{PathSpriteAtlas}/AtlasForUI.spriteatlas";
    public string PathSpriteAtlasForItems = $"{PathSpriteAtlas}/AtlasForItems.spriteatlas";
    public string PathSpriteAtlasForSky = $"{PathSpriteAtlas}/AtlasForSky.spriteatlas";
    public string PathSpriteAtlasForSkin = $"{PathSpriteAtlas}/AtlasForSkin.spriteatlas";
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
    /// 获取sprite
    /// </summary>
    public void GetSprite(string atlasName, string name, Action<Sprite> callBack)
    {
        bool isNewAtlas = false;
        SpriteAtlas atlas = null;

        if (!dicSprite.TryGetValue(atlasName, out var spriteDict))
        {
            spriteDict = new Dictionary<string, Sprite>();
            dicSprite.Add(atlasName, spriteDict);
            dicSpriteAtlas.Add(atlasName, null);
            isNewAtlas = true;
        }
        else
        {
            atlas = dicSpriteAtlas[atlasName];
        }

        GetSpriteByName(spriteDict, ref atlas, $"{PathSpriteAtlas}/{atlasName}.spriteatlas", name, callBack);

        if (isNewAtlas)
        {
            dicSpriteAtlas[atlasName] = atlas;
        }
    }
}