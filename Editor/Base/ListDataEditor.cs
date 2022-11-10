using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ListDataEditor : Editor
{

    /// <summary>
    /// 根据指定文件添加字典
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="map"></param>
    public static void AddIconBeanDictionaryByFile(string filePath, IconBeanDictionary map)
    {
        Object[] objs = AssetDatabase.LoadAllAssetsAtPath(filePath);
        objs.ToList().ForEach(obj =>
        {
            if (obj as Sprite != null)
            {
                map.Add(obj.name, obj as Sprite);
            }
        });
    }

    /// <summary>
    /// 根据文件夹下所有文件添加字典
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="mapFood"></param>
    public static void AddIconBeanDictionaryByFolder(string folderPath, IconBeanDictionary map)
    {
        FileInfo[] files = FileUtil.GetFilesByPath(folderPath);
        foreach (FileInfo item in files)
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(folderPath + item.Name);
            objs.ToList().ForEach(obj =>
            {
                if (obj as Sprite != null)
                {
                    map.Add(obj.name, obj as Sprite);
                }
            });
        }
    }
    /// <summary>
    /// 根据文件夹下所有文件添加字典
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="mapFood"></param>
    public static void AddAnimBeanDictionaryByFolder(string folderPath, AnimBeanDictionary map)
    {
        FileInfo[] files = FileUtil.GetFilesByPath(folderPath);
        if (files == null)
            return;
        foreach (FileInfo item in files)
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(folderPath + item.Name);
            objs.ToList().ForEach(obj =>
            {
                if (obj as AnimationClip != null)
                {
                    map.Add(obj.name, obj as AnimationClip);
                }
            });
        }
    }
    /// <summary>
    ///  根据文件夹下所有文件添加字典
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="map"></param>
    public static void AddTileBeanDictionaryByFolder(string folderPath, TileBeanDictionary map)
    {
        FileInfo[] files = FileUtil.GetFilesByPath(folderPath);
        foreach (FileInfo item in files)
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(folderPath + item.Name);
            objs.ToList().ForEach(obj =>
            {
                if (obj as TileBase != null)
                {
                    map.Add(obj.name, obj as TileBase);
                }
            });
        }
    }

    /// <summary>
    /// 根据文件夹下所有文件添加字典
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="mapFood"></param>
    public static void AddAudioBeanDictionaryByFolder(string folderPath, AudioBeanDictionary map)
    {
        FileInfo[] files = FileUtil.GetFilesByPath(folderPath);
        foreach (FileInfo item in files)
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(folderPath + item.Name);
            objs.ToList().ForEach(obj =>
            {
                if (obj as AudioClip != null)
                {
                    map.Add(obj.name, obj as AudioClip);
                }
            });
        }
    }

    public static void AddGameObjectDictionaryByFolder(string folderPath, GameObjectDictionary map)
    {
        FileInfo[] files = FileUtil.GetFilesByPath(folderPath);
        foreach (FileInfo item in files)
        {
            Object obj = AssetDatabase.LoadMainAssetAtPath(folderPath + item.Name);
            if (obj as GameObject != null)
            {
                map.Add(obj.name + "", obj as GameObject);
            }
        }
    }
}
