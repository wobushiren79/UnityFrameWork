using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class EditorUtil
{

    /// <summary>
    /// 创建资源 
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="path">Assets/TexArray.asset</param>
    public static void CreateAsset(UnityEngine.Object asset, string path)
    {
        //首先查询是否有资源
        AssetDatabase.CreateAsset(asset, path);
    }

    /// <summary>
    /// 保存资源
    /// </summary>
    /// <param name="asset"></param>
    public static void SaveAsset(UnityEngine.Object asset)
    {
        EditorUtility.SetDirty(asset);
    }

    /// <summary>
    /// 创建预置
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="path"></param>
    public static void CreatePrefab(GameObject obj, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(obj, $"{path}.prefab");
    }


    /// <summary>
    /// 通过资源文件的唯一ID获取选择文件的路径
    /// 注：仅适用于 选中Project 中的物体
    /// </summary>
    /// <returns></returns>
    public static string[] GetSelectionPathByGUIDS()
    {
        string[] strs = Selection.assetGUIDs;
        string[] arrayData = new string[strs.Length];
        for (int i = 0; i < strs.Length; i++)
        {
            string guid = strs[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            arrayData[i] = path;
        }
        return arrayData;
    }

    /// <summary>
    /// 获取选中的obj物体获取路径
    /// 注：仅适用于 选中Project 中的物体
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string GetSelectionPathByObj(GameObject obj)
    {
        if (obj == null)
            return null;
        return AssetDatabase.GetAssetPath(obj);
    }

    public static string GetSelectionPathByObj(UnityEngine.Object obj)
    {
        if (obj == null)
            return null;
        return AssetDatabase.GetAssetPath(obj);
    }
    /// <summary>
    /// 获取选中obj在场景中的位置
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string GetSelectionScenePathByObj(GameObject obj)
    {
        if (obj == null)
            return null;
        return AssetDatabase.GetAssetOrScenePath(obj);
    }

    /// <summary>
    /// 获取选中obj物体原文件的路径
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string GetSelectionSourcePathByObj(GameObject obj)
    {
        GameObject sourceObj = GetSourceGameObject(obj);
        return GetSelectionPathByObj(sourceObj);
    }

    /// <summary>
    /// 获取obj的原obj
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static GameObject GetSourceGameObject(GameObject obj)
    {
        if (obj == null)
            return null;
        return PrefabUtility.GetCorrespondingObjectFromSource(obj);
    }

    /// <summary>
    /// 通过路径获取资源 具体到每一个资源路径
    /// </summary>
    /// <param name="path">例如：“Assets/MyTextures/hello.png”</param>
    /// <param name="type">0所有子资源 1只返回可见的子资源</param>
    /// <returns></returns>
    public static UnityEngine.Object[] GetAssetsByPath(string path, int type = 0)
    {
        switch (type)
        {
            case 0:
                return AssetDatabase.LoadAllAssetsAtPath(path);
            case 1:
                return AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        }
        return null;
    }

    /// <summary>
    /// 获取资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path">Assets开头</param>
    /// <returns></returns>
    public static T GetAssetByPath<T>(string path) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    /// <summary>
    /// 获取脚本路径
    /// </summary>
    /// <param name="scriptName"></param>
    /// <returns></returns>
    public static string[] GetScriptPath(string scriptName)
    {
        string[] uuids = AssetDatabase.FindAssets(scriptName, new string[] { "Assets" });
        List<string> listData = new List<string>();
        for (int i = 0; i < uuids.Length; i++)
        {
            string uuid = uuids[i];
            string uuidPath = AssetDatabase.GUIDToAssetPath(uuid);
            if (uuidPath.Contains(scriptName + ".cs"))
            {
                listData.Add(uuidPath.Replace((@"/" + scriptName + ".cs"), ""));
            }
        }
        return listData.ToArray();
    }

    /// <summary>
    /// 创建.cs文件
    /// </summary>
    /// <param name="dicReplace">替换数据</param>
    /// <param name="templatesPath">模板路径</param>
    /// <param name="fileName">文件名（不用加.cs）</param>
    /// <param name="createPath">创建路径</param>
    public static void CreateClass(Dictionary<string, string> dicReplace, string templatesPath, string fileName, string createPath)
    {
        if (templatesPath.IsNull())
        {
            LogUtil.LogError("模板路径为空");
            return;
        }
        if (fileName.IsNull())
        {
            LogUtil.LogError("文件名为空");
            return;
        }
        if (createPath.IsNull())
        {
            LogUtil.LogError("生成路径为空");
            return;
        }
        //读取模板
        string viewScriptContent = File.ReadAllText(templatesPath);
        //替换数据
        foreach (var itemData in dicReplace)
        {
            viewScriptContent = viewScriptContent.Replace(itemData.Key, itemData.Value);
        }
        //创建文件
        FileUtil.CreateTextFile(createPath, fileName + ".cs", viewScriptContent);
    }

    /// <summary>
    /// 检测是否处于Prefab Mode
    /// </summary>
    /// <param name="prefabStage"></param>
    /// <returns></returns>
    public static bool CheckIsPrefabMode(out UnityEditor.SceneManagement.PrefabStage prefabStage)
    {
        prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            // 当前正处于Prefab Mode
            return true;
        }
        else
        {
            // 当前没有处于Prefab Mode
            return false;
        }
    }
    public static bool CheckIsPrefabMode()
    {
        return CheckIsPrefabMode(out UnityEditor.SceneManagement.PrefabStage prefabStage);
    }


    /// <summary>
    /// 刷新资源
    /// </summary>
    public static void RefreshAsset()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 刷新单个资源
    /// </summary>
    /// <param name="objSelect"></param>
    public static void RefreshAsset(UnityEngine.Object objSelect)
    {
        Undo.RecordObject(objSelect, objSelect.name);
        EditorUtility.SetDirty(objSelect);
        RefreshAsset();
    }

    /// <summary>
    /// 创建一个材质球
    /// </summary>
    public static void CreateMaterial(string texPath, string shaderName, string matCreatePath,
        float metallic = -1f, float smoothness = -1f)
    {
        //获取贴图
        Texture2D texMat = GetAssetByPath<Texture2D>(texPath);
        Material mat = new Material(Shader.Find(shaderName));
        mat.mainTexture = texMat;
        if (metallic != -1)
            mat.SetFloat("_Metallic", metallic);
        if (smoothness != -1)
            mat.SetFloat("_Smoothness", smoothness);
        CreateAsset(mat, $"{matCreatePath}.mat");
        RefreshAsset();
    }

    /// <summary>
    /// 设置贴图数据
    /// </summary>
    public static void SetTextureData(string texturePath,
        TextureImporterType textureImporterType = TextureImporterType.Default,
        int spritePixelsPerUnit = 16,
        bool isReadable = true,//是否可读
        bool mipmapEnabled = false,//是否开启mipmap
        TextureWrapMode wrapMode = TextureWrapMode.Repeat,//循环模式
        FilterMode filterMode = FilterMode.Point,//像素模式
        TextureImporterFormat format = TextureImporterFormat.RGBA32,//颜色
        string platform = "Standalone",
        int maxTextureSize = 2048,
        TextureImporterCompression textureImporterCompression = TextureImporterCompression.CompressedHQ)//平台
    {
        TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        textureImporter.textureType = textureImporterType;

        textureImporter.spriteImportMode = SpriteImportMode.Single;
        textureImporter.spritePixelsPerUnit = spritePixelsPerUnit;
        textureImporter.isReadable = isReadable;
        textureImporter.mipmapEnabled = mipmapEnabled;
        textureImporter.wrapMode = wrapMode;
        textureImporter.filterMode = filterMode;
        textureImporter.crunchedCompression = true;
        textureImporter.compressionQuality = 100;
        textureImporter.maxTextureSize = maxTextureSize;
        textureImporter.textureCompression = textureImporterCompression;
        var settingPlatform = textureImporter.GetPlatformTextureSettings(platform);
        settingPlatform.format = format;
        settingPlatform.maxTextureSize = maxTextureSize;
        settingPlatform.textureCompression = textureImporterCompression;
        textureImporter.SetPlatformTextureSettings(settingPlatform);

        AssetDatabase.ImportAsset(texturePath);
        AssetDatabase.Refresh();
    }
}