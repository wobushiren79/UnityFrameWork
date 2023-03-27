using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using UnityEngine.ResourceManagement.AsyncOperations;

public class BaseManager : BaseMonoBehaviour
{

    public virtual void InitData<T>(Dictionary<long, T> dic, List<T> listData) where T : BaseBean
    {
        if (dic == null)
            dic = new Dictionary<long, T>();
        dic.Clear();
        for (int i = 0; i < listData.Count; i++)
        {
            T itemHairInfo = listData[i];
            dic.Add(itemHairInfo.id, itemHairInfo);
        }
    }

    public virtual void InitData<T>(Dictionary<int, T> dic, List<T> listData) where T : BaseBean
    {
        if (dic == null)
            dic = new Dictionary<int, T>();
        dic.Clear();
        for (int i = 0; i < listData.Count; i++)
        {
            T itemHairInfo = listData[i];
            dic.Add((int)itemHairInfo.id, itemHairInfo);
        }
    }

    protected List<T> GetAllModel<T>(string assetBundlePath) where T : UnityEngine.Object
    {
        return GetAllModel<T>(assetBundlePath, null);
    }
    protected List<T> GetAllModel<T>(string assetBundlePath, string remarkResourcesPath) where T : UnityEngine.Object
    {
        List<T> models = null;
#if UNITY_EDITOR
        //编辑器模式下直接加载资源
        if (!remarkResourcesPath.IsNull())
        {
            models = LoadAssetUtil.LoadAllAssetAtPathForEditor<T>(remarkResourcesPath);
        }
        else
        {
            models = LoadAssetUtil.SyncLoadAllAsset<T>(assetBundlePath);
        }
#else
            models = LoadAssetUtil.SyncLoadAllAsset<T>(assetBundlePath);
#endif
        return models;
    }
    protected T GetModel<T>(string assetBundlePath, string name) where T : UnityEngine.Object
    {
        return GetModel<T>(assetBundlePath, name, null);
    }
    protected T GetModel<T>(string assetBundlePath, string name, string remarkResourcesPath) where T : UnityEngine.Object
    {
        if (name == null)
            return null;
        T model = null;
#if UNITY_EDITOR
        //编辑器模式下直接加载资源
        if (!remarkResourcesPath.IsNull())
        {
            model = LoadAssetUtil.LoadAssetAtPathForEditor<T>(remarkResourcesPath);
        }
        else
        {
            model = LoadAssetUtil.SyncLoadAsset<T>(assetBundlePath, name);
        }
#else
        model = LoadAssetUtil.SyncLoadAsset<T>(assetBundlePath, name);
#endif
        return model;
    }

    protected T GetModel<T>(Dictionary<string, T> listModel, string assetBundlePath, string name) where T : UnityEngine.Object
    {
        return GetModel<T>(listModel, assetBundlePath, name, null);
    }

    protected T GetModel<T>(Dictionary<string, T> listModel, string assetBundlePath, string name, string remarkResourcesPath) where T : UnityEngine.Object
    {
        if (name == null)
            return null;
        if (listModel.TryGetValue(name, out T value))
        {
            return value;
        }

        T model = null;
#if UNITY_EDITOR
        //编辑器模式下直接加载资源
        if (!remarkResourcesPath.IsNull())
        {
            model = LoadAssetUtil.LoadAssetAtPathForEditor<T>(remarkResourcesPath);
        }
        else
        {
            model = LoadAssetUtil.SyncLoadAsset<T>(assetBundlePath, name);
        }
#else
        model = LoadAssetUtil.SyncLoadAsset<T>(assetBundlePath, name);
#endif
        if (model != null)
        {
            listModel.Add(name, model);
        }
        return model;
    }

    protected T GetModel<T>(SerializableDictionaryBase<string, T> listModel, string assetBundlePath, string name) where T : UnityEngine.Object
    {
        return GetModel<T>(listModel, assetBundlePath, name, null);
    }

    protected T GetModel<T>(SerializableDictionaryBase<string, T> listModel, string assetBundlePath, string name, string remarkResourcesPath) where T : UnityEngine.Object
    {
        if (name == null)
            return null;
        if (listModel.TryGetValue(name, out T value))
        {
            return value;
        }

        T model = null;
#if UNITY_EDITOR
        //编辑器模式下直接加载资源
        if (!remarkResourcesPath.IsNull())
        {
            model = LoadAssetUtil.LoadAssetAtPathForEditor<T>(remarkResourcesPath);
        }
        else
        {
            model = LoadAssetUtil.SyncLoadAsset<T>(assetBundlePath, name);
        }
#else
        model = LoadAssetUtil.SyncLoadAsset<T>(assetBundlePath, name);
#endif

        if (model != null)
        {
            listModel.Add(name, model);
        }
        return model;
    }

    protected T GetModelForResources<T>(Dictionary<string, T> listModel, string resPath) where T : UnityEngine.Object
    {
        if (resPath == null)
            return null;
        if (listModel.TryGetValue(resPath, out T value))
        {
            return value;
        }

        T model = LoadResourcesUtil.SyncLoadData<T>(resPath);

        if (model != null)
        {
            listModel.Add(resPath, model);
        }
        return model;
    }

    protected void GetModelForAddressables<T>(Dictionary<string, T> listModel, string keyName, Action<T> callBack) where T : UnityEngine.Object
    {
        if (keyName == null)
        {
            callBack?.Invoke(null);
            return;
        }

        if (listModel.TryGetValue(keyName, out T value))
        {
            callBack?.Invoke(value);
            return;
        }

        LoadAddressablesUtil.LoadAssetAsync<T>(keyName, data =>
        {
            if (listModel.TryGetValue(keyName, out T result))
            {
                callBack?.Invoke(result);
            }
            else
            {
                listModel.Add(keyName, data.Result);
                callBack?.Invoke(data.Result);
            }
        });
    }

    protected void GetModelForAddressables<T>(Dictionary<long, T> listModel, long id, string keyName, Action<T> callBack) where T : UnityEngine.Object
    {
        if (keyName == null)
        {
            callBack?.Invoke(null);
            return;
        }

        if (listModel.TryGetValue(id, out T value))
        {
            callBack?.Invoke(value);
            return;
        }

        LoadAddressablesUtil.LoadAssetAsync<T>(keyName, data =>
        {
            if (data.Result != null)
            {
                if (listModel.TryGetValue(id, out T result))
                {
                    callBack?.Invoke(result);
                }
                else
                {
                    listModel.Add(id, data.Result);
                    callBack?.Invoke(data.Result);
                }
            }
        });
    }

    protected void GetModelsForAddressables<T>(Dictionary<long, IList<T>> listModel, long id, List<string> listKeyName, Action<IList<T>> callBack) where T : UnityEngine.Object
    {
        if (listKeyName.IsNull())
        {
            callBack?.Invoke(null);
            return;
        }

        if (listModel.TryGetValue(id, out IList<T> value))
        {
            callBack?.Invoke(value);
            return;
        }

        LoadAddressablesUtil.LoadAssetsAsync<T>(listKeyName, data =>
        {
            if (data.Result != null)
            {
                if (listModel.TryGetValue(id, out IList<T> result))
                {
                    callBack?.Invoke(result);
                }
                else
                {
                    listModel.Add(id, data.Result);
                    callBack?.Invoke(data.Result);
                }
            }
        });
    }

    protected void GetModelsForAddressables<T>(List<string> listKeyName, Action<IList<T>> callBack) where T : UnityEngine.Object
    {
        if (listKeyName == null)
        {
            callBack?.Invoke(null);
            return;
        }
        LoadAddressablesUtil.LoadAssetsAsync<T>(listKeyName, listData =>
        {
            callBack?.Invoke(listData.Result);
        });
    }

    protected void GetSpriteByName(Dictionary<string, Sprite> dicIcon, SpriteAtlas spriteAtlas, string resName, string name, 
        Action<SpriteAtlas> callBackForSpriteAtlas = null, Action<Sprite> callBackForSprite = null)
    {
        if (name == null)
            return;
        //从字典获取sprite
        if (dicIcon.TryGetValue(name, out Sprite value))
        {
            callBackForSprite?.Invoke(value);
            return;
        }
        //如果字典没有 尝试从atlas获取sprite
        if (spriteAtlas != null)
        {
            Sprite itemSprite = GetSpriteByName(name, spriteAtlas);
            if (itemSprite != null)
                dicIcon.Add(name, itemSprite);
            callBackForSprite?.Invoke(itemSprite);
            return;
        }
        SpriteAtlas spriteAtlasNew = LoadAddressablesUtil.LoadAssetSync<SpriteAtlas>(resName);
        if (spriteAtlasNew != null)
        {
            callBackForSpriteAtlas?.Invoke(spriteAtlasNew);
        }
    }

    protected Sprite GetSpriteByName(IconBeanDictionary dicIcon, ref SpriteAtlas spriteAtlas, string atlasName, string assetBundlePath, string name)
    {
        return GetSpriteByName(dicIcon, ref spriteAtlas, atlasName, assetBundlePath, name, null);
    }

    protected Sprite GetSpriteByName(IconBeanDictionary dicIcon, ref SpriteAtlas spriteAtlas, string atlasName, string assetBundlePath, string name, string remarkResourcesPath)
    {
        if (name == null)
            return null;
        //从字典获取sprite
        if (dicIcon.TryGetValue(name, out Sprite value))
        {
            return value;
        }
        //如果字典没有 尝试从atlas获取sprite
        if (spriteAtlas != null)
        {
            Sprite itemSprite = GetSpriteByName(name, spriteAtlas);
            if (itemSprite != null)
                dicIcon.Add(name, itemSprite);
            return itemSprite;
        }
#if UNITY_EDITOR
        //编辑器模式下直接加载资源
        if (!remarkResourcesPath.IsNull())
        {
            spriteAtlas = LoadAssetUtil.LoadAssetAtPathForEditor<SpriteAtlas>(remarkResourcesPath);
        }
        else
        {
            //如果没有atlas 先加载atlas
            spriteAtlas = LoadAssetUtil.SyncLoadAsset<SpriteAtlas>(assetBundlePath, atlasName);
            //spriteAtlas = LoadResourcesUtil.SyncLoadData<SpriteAtlas>(assetBundlePath+ atlasName);
        }
#else
        //如果没有atlas 先加载atlas
        spriteAtlas = LoadAssetUtil.SyncLoadAsset<SpriteAtlas>(assetBundlePath, atlasName);
        //spriteAtlas = LoadResourcesUtil.SyncLoadData<SpriteAtlas>(assetBundlePath + atlasName);
#endif

        //加载成功后在读取一次
        if (spriteAtlas != null)
            return GetSpriteByName(dicIcon, ref spriteAtlas, atlasName, assetBundlePath, name, remarkResourcesPath);
        return null;
    }

    protected Sprite GetSpriteByName(Dictionary<string, Sprite> dicIcon, ref SpriteAtlas spriteAtlas, string atlasName, string assetBundlePath, string name)
    {
        return GetSpriteByName(dicIcon, ref spriteAtlas, atlasName, assetBundlePath, name, null);
    }

    protected Sprite GetSpriteByName(Dictionary<string, Sprite> dicIcon, ref SpriteAtlas spriteAtlas, string atlasName, string assetBundlePath, string name, string remarkResourcesPath)
    {
        if (name == null)
            return null;
        //从字典获取sprite
        if (dicIcon.TryGetValue(name, out Sprite value))
        {
            return value;
        }
        //如果字典没有 尝试从atlas获取sprite
        if (spriteAtlas != null)
        {
            Sprite itemSprite = GetSpriteByName(name, spriteAtlas);
            if (itemSprite != null)
                dicIcon.Add(name, itemSprite);
            return itemSprite;
        }
#if UNITY_EDITOR
        //编辑器模式下直接加载资源
        if (!remarkResourcesPath.IsNull())
        {
            spriteAtlas = LoadAssetUtil.LoadAssetAtPathForEditor<SpriteAtlas>(remarkResourcesPath);
        }
        else
        {
            //如果没有atlas 先加载atlas
            spriteAtlas = LoadAssetUtil.SyncLoadAsset<SpriteAtlas>(assetBundlePath, atlasName);
            //spriteAtlas = LoadResourcesUtil.SyncLoadData<SpriteAtlas>(assetBundlePath+ atlasName);
        }
#else
        //如果没有atlas 先加载atlas
        spriteAtlas = LoadAssetUtil.SyncLoadAsset<SpriteAtlas>(assetBundlePath, atlasName);
        //spriteAtlas = LoadResourcesUtil.SyncLoadData<SpriteAtlas>(assetBundlePath + atlasName);
#endif
        //加载成功后在读取一次
        if (spriteAtlas != null)
        {
            return GetSpriteByName(dicIcon, ref spriteAtlas, atlasName, assetBundlePath, name, remarkResourcesPath);
        }
        return null;
    }

    /// <summary>
    /// 根据名字获取
    /// </summary>
    /// <param name="name"></param>
    /// <param name="map"></param>
    /// <returns></returns>
    public virtual GameObject GetGameObjectByName(string name, GameObjectDictionary map)
    {
        if (name == null)
            return null;
        if (map.TryGetValue(name, out GameObject obj))
            return obj;
        else
            return null;
    }

    /// <summary>
    /// 根据名字获取音频
    /// </summary>
    /// <param name="name"></param>
    /// <param name="map"></param>
    /// <returns></returns>
    public virtual AudioClip GetAudioClipByName(string name, AudioBeanDictionary map)
    {
        if (name == null)
            return null;
        if (map.TryGetValue(name, out AudioClip audioClip))
            return audioClip;
        else
            return null;
    }

    /// <summary>
    /// 根据名字获取动画
    /// </summary>
    /// <param name="name"></param>
    /// <param name="map"></param>
    /// <returns></returns>
    public virtual AnimationClip GetAnimClipByName(string name, AnimBeanDictionary map)
    {
        if (name == null)
            return null;
        if (map.TryGetValue(name, out AnimationClip animClip))
            return animClip;
        else
            return null;
    }


    /// <summary>
    /// 根据名字获取tile
    /// </summary>
    /// <param name="name"></param>
    /// <param name="map"></param>
    /// <returns></returns>
    public virtual TileBase GetTileBaseByName(string name, TileBeanDictionary map)
    {
        if (name == null)
            return null;
        if (map.TryGetValue(name, out TileBase tile))
            return tile;
        else
            return null;
    }

    /// <summary>
    /// 根据名字获取图标
    /// </summary>
    /// <param name="name"></param>
    /// <param name="listdata"></param>
    /// <returns></returns>
    public virtual Sprite GetSpriteByName(string name, List<IconBean> listdata)
    {
        IconBean iconData = BeanUtil.GetIconBeanByName(name, listdata);
        if (iconData == null)
            return null;
        return iconData.value;
    }

    /// <summary>
    /// 根据位置获取图标
    /// </summary>
    /// <param name="positon"></param>
    /// <param name="listdata"></param>
    /// <returns></returns>
    public virtual Sprite GetSpriteByPosition(int position, List<IconBean> listdata)
    {
        IconBean iconData = BeanUtil.GetIconBeanByPosition(position, listdata);
        if (iconData == null)
            return null;
        return iconData.value;
    }

    /// <summary>
    /// 通过名字获取Icon
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual Sprite GetSpriteByName(string name, IconBeanDictionary map)
    {
        if (name == null)
            return null;
        if (map.TryGetValue(name, out Sprite spIcon))
            return spIcon;
        else
            return null;
    }

    public virtual Sprite GetSpriteByName(string name, SpriteAtlas spriteAtlas)
    {
        return spriteAtlas.GetSprite(name);
    }

    /// <summary>
    /// 通过ID获取数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="map"></param>
    /// <returns></returns>
    public virtual T GetDataById<T>(long name, Dictionary<long, T> map) where T : class
    {
        if (map == null)
            return null;
        if (map.TryGetValue(name, out T itemData))
            return itemData;
        else
            return null;
    }

    public virtual T GetDataById<T>(int name, Dictionary<int, T> map) where T : class
    {
        if (map == null)
            return null;
        if (map.TryGetValue(name, out T itemData))
            return itemData;
        else
            return null;
    }
}