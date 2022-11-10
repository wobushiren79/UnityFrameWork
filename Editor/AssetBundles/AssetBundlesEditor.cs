using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetBundlesEditor : Editor
{

    [MenuItem("Custom/AssetBundles/创建PC 64位平台资源")]
    public static void BuildAssetBundleForBuildSetting()
    {
        BuildAssetBundle(BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows64);
    }

    [MenuItem("Custom/AssetBundles/创建安卓平台资源")]
    public static void BuildAssetBundleForAndroid()
    {
        BuildAssetBundle(BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.Android);
    }

    [MenuItem("Custom/AssetBundles/创建苹果平台资源")]
    public static void BuildAssetBundleForIOS()
    {
        BuildAssetBundle(BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.iOS);
    }

    protected static void BuildAssetBundle(BuildAssetBundleOptions options, BuildTarget buildTarget)
    {
        string dir = "Assets/StreamingAssets/AssetBundles";
        if (Directory.Exists(dir) == false)
        {
            Directory.CreateDirectory(dir);
        }
        BuildPipeline.BuildAssetBundles(dir, options, buildTarget);
        AssetDatabase.Refresh();
        Debug.Log("打包完成");
    }

    /// <summary>
    /// 清除之前设置过的AssetBundleName，避免产生不必要的资源也打包
    /// 之前说过，只要设置了AssetBundleName的，都会进行打包，不论在什么目录下
    /// </summary>
    [MenuItem("Custom/AssetBundles/ClearAssetBundlesName")]
    public static void ClearAssetBundlesName()
    {
        int length = AssetDatabase.GetAllAssetBundleNames().Length;
        Debug.Log(length);
        string[] oldAssetBundleNames = new string[length];
        for (int i = 0; i < length; i++)
        {
            oldAssetBundleNames[i] = AssetDatabase.GetAllAssetBundleNames()[i];
        }

        for (int j = 0; j < oldAssetBundleNames.Length; j++)
        {
            AssetDatabase.RemoveAssetBundleName(oldAssetBundleNames[j], true);
        }
        length = AssetDatabase.GetAllAssetBundleNames().Length;
        Debug.Log(length);
    }
}
