using UnityEditor;
using UnityEngine;

public class TextureUtil
{
    /// <summary>
    /// 获取像素
    /// </summary>
    /// <param name="tex"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Color GetPixel(Texture2D tex, Vector2Int position)
    {
        return tex.GetPixel(position.x, position.y);
    }

    /// <summary>
    /// sprite转Texture2d 用于图集里的图片
    /// </summary>
    /// <param name="sprite"></param>
    /// <returns></returns>
    //public static Texture2D SpriteToTexture2D(Sprite sprite)
    //{
    //    Texture2D texture = new Texture2D
    //    (
    //        (int)sprite.rect.width,
    //        (int)sprite.rect.height
    //    );

    //    Color[] pixels = sprite.texture.GetPixels(
    //        (int)sprite.textureRect.x,
    //        (int)sprite.textureRect.y,
    //        (int)sprite.textureRect.width,
    //        (int)sprite.textureRect.height
    //    );
    //    texture.SetPixels(pixels);
    //    texture.filterMode = FilterMode.Point;
    //    texture.Apply();
    //    return texture;
    //}

    /// <summary>
    /// sprite转Texture2d 用于图集里的图片
    /// </summary>
    /// <param name="sprite"></param>
    /// <param name="filterMode">适配模式</param>
    /// <param name="isSameWH">是否相同宽高</param>
    /// <param name="texW">固定宽</param>
    /// <param name="texH">固定高</param>
    /// <returns></returns>
    public static Texture2D SpriteToTexture2D(Sprite sprite, FilterMode filterMode = FilterMode.Point,
        bool isSameWH = false,int texW = 0,int texH = 0)
    {
        try
        {
            if (sprite.rect.width != sprite.texture.width)
            {
                Texture2D texture;
                //如果是指定宽高
                if(texW != 0 && texH != 0)
                {
                    texture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
                }
                else
                {          
                    //如果是需要设置相同的长宽
                    if (isSameWH)
                    {
                        int moreSize = (int)(sprite.rect.width > sprite.rect.height ? sprite.rect.width : sprite.rect.height);
                        texture = new Texture2D(moreSize, moreSize, TextureFormat.RGBA32, false);
                    }
                    else
                    {
                        texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
                    }
                }
                Color[] pixels = sprite.texture.GetPixels
                    (
                        (int)(sprite.textureRect.x),
                        (int)(sprite.textureRect.y),
                        (int)(sprite.textureRect.width),
                        (int)(sprite.textureRect.height)
                    );
                //如果大小不对 则需要调整pixel的位置
                if (pixels.Length < texture.width * texture.height)
                {        
                    //默认设置所有的像素为透明
                    texture.SetPixels(new Color[texture.width * texture.height]);
                    //如果是指定的大小
                    if (texW != 0 && texH != 0)
                    {
                        int offsetX = (int)Mathf.Abs(texW - sprite.rect.width);
                        int offsetY = (int)Mathf.Abs(texH - sprite.rect.height);
                        texture.SetPixels(offsetX / 2, offsetY / 2, (int)sprite.rect.width, (int)sprite.rect.height, pixels);
                    }
                    else 
                    {
                        int offset = (int)Mathf.Abs(sprite.rect.width - sprite.rect.height);       
                        if (sprite.rect.width > sprite.rect.height)
                        {
                            //如果原图 W>H
                            texture.SetPixels(0, offset / 2, (int)sprite.rect.width, (int)sprite.rect.height, pixels);
                        }
                        else
                        {
                            //如果原图 H>W
                            texture.SetPixels(offset / 2, 0, (int)sprite.rect.width, (int)sprite.rect.height, pixels);
                        }
                    }
                }
                else
                {
                    texture.SetPixels(pixels);
                }

                texture.filterMode = filterMode;
                texture.Apply();
                return texture;
            }
            else
                return sprite.texture;
        }
        catch
        {
            return sprite.texture;
        }
    }

    public static Texture2D SpriteToTexture2D(Sprite[] arraySprite, FilterMode filterMode = FilterMode.Point)
    {
        Texture2D texture = new Texture2D((int)arraySprite[0].rect.width, (int)arraySprite[0].rect.height, TextureFormat.RGBA32, false);
        //默认设置所有的像素为透明
        Color[] pixels = new Color[texture.width * texture.height];
        try
        {
            for (int i = 0; i < arraySprite.Length; i++)
            {
                Sprite itemSprite = arraySprite[i];
                if (itemSprite.rect.width != itemSprite.texture.width)
                {
                    Color[] pixelsItem = itemSprite.texture.GetPixels
                    (
                            (int)(itemSprite.textureRect.x),
                            (int)(itemSprite.textureRect.y),
                            (int)(itemSprite.textureRect.width),
                            (int)(itemSprite.textureRect.height)
                    );
                    for (int p = 0; p < pixels.Length; p++)
                    {
                        Color itemColor = pixelsItem[p];
                        if (itemColor.a == 0)
                        {
                            continue;
                        }
                        pixels[p] = itemColor;
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.filterMode = filterMode;
            texture.Apply();
            return texture;
        }
        catch
        {
            return texture;
        }
    }

    /// <summary>
    /// 重新定义texture2D大小（拉长）
    /// </summary>
    /// <param name="texture2D"></param>
    /// <param name="targetX"></param>
    /// <param name="targetY"></param>
    /// <returns></returns>
    public static Texture2D ResizeTexture2D(Texture2D texture2D, int targetX, int targetY)
    {
        RenderTexture rt = new RenderTexture(targetX, targetY, 24);
        RenderTexture.active = rt;
        Graphics.Blit(texture2D, rt);
        Texture2D result = new Texture2D(targetX, targetY);
        result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
        result.Apply();
        return result;
    }

}