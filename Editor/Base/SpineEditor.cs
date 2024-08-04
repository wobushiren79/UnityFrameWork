using DG.Tweening.Plugins.Core.PathCore;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class SpineEditor : Editor
{
    //spine数据路径
    public static string pathSkeletonData = "Assets/LoadResources/Spine";
    public static string shaderName = "Universal Render Pipeline/Spine/Sprite";

    [MenuItem("Custom/Spine/设置所有资源")]
    public static void SpineAllInit()
    {
        SpineInit(pathSkeletonData);
    }

    [MenuItem("Assets/设置Spine资源", false, 0)]
    static void CopyFilePathToClipboard()
    {
        Object[] selectedObjects = Selection.GetFiltered(typeof(DefaultAsset), SelectionMode.Assets);
        if (selectedObjects.Length == 0)
        {
            Debug.LogError("请选择一个目录");
            return;
        }
        foreach (var itemPath in selectedObjects)
        {
            string selectedFilePath = AssetDatabase.GetAssetPath(itemPath);
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                Debug.LogError("路径不对");
                return;
            }
            SpineInit(selectedFilePath);
        }
    }

    public static void SpineInit(string pathSkeletonData)
    {
        FileInfo[] arrayFile = FileUtil.GetFilesByPath(pathSkeletonData);
        foreach (var item in arrayFile)
        {
            if (item.Name.Contains(".meta"))
                continue;

            string convertedPath = item.FullName.Replace('\\', '/');
            int indexTemp = convertedPath.IndexOf(pathSkeletonData);
            string targetPath = convertedPath.Substring(indexTemp);

            if (item.Name.Length >= 5 && item.Name.Substring(item.Name.Length - 5).Equals(".json"))
            {
                //LogUtil.Log($"SpineInit Item Json {item.Name}");
                TextAsset textAsset = EditorUtil.GetAssetByPath<TextAsset>(targetPath);
                SpineChangeVersion(targetPath, textAsset);
            }
            else if (item.Name.Length >= 4 && item.Name.Substring(item.Name.Length - 4).Equals(".png"))
            {
                //LogUtil.Log($"SpineInit Item Png indexTemp_{indexTemp} targetPath_{targetPath}");
                if (targetPath.Contains("Assets/LoadResources/Spine/Creature/Other"))
                {
                    ImageEditor.ChangeImageFilterMode(FilterMode.Bilinear, targetPath);
                }
                else
                {
                    ImageEditor.ChangeImageFilterMode(FilterMode.Point, targetPath);
                }

                ImageEditor.ChangeImageTextureImporterType(TextureImporterType.Sprite, targetPath);
                ImageEditor.ChangeImageTextureImporterType(TextureImporterType.Default, targetPath);
            }
            else if (item.Name.Length >= 4 && item.Name.Substring(item.Name.Length - 4).Equals(".mat"))
            {
                //LogUtil.Log($"SpineInit Item Mat {item.Name}");
                Material mat = EditorUtil.GetAssetByPath<Material>(targetPath);
                mat.shader = Shader.Find(shaderName);
                mat.EnableKeyword("_FIXED_NORMALS_VIEWSPACE");
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");

                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

                mat.SetVector("_FixedNormal", new Vector4(0, 0, 1, 1));
            }
        }
        EditorUtil.RefreshAsset();
        LogUtil.Log($"初始化完成");
    }

    //[MenuItem("Custom/Spine/修改版号")]
    public static void SpineChangeVersion()
    {
        Object obj = Selection.activeObject;
        if (obj == null)
            return;
        TextAsset textAsset = obj as TextAsset;
        if (textAsset == null)
            return;

        string path = EditorUtil.GetSelectionPathByObj(obj);

        SpineChangeVersion(path, textAsset);
    }

    public static void SpineChangeVersion(string path, TextAsset textAsset)
    {
        string contentStr = textAsset.text;
        //LogUtil.Log($"contentStr:{contentStr}");
        string contentStrNew = contentStr.Replace("\"spine\": \"3.8.75\",", "\"spine\": \"3.8\"");
        //LogUtil.Log($"contentStrNew:{contentStrNew}");

        File.WriteAllText(path, contentStrNew);
        EditorUtil.RefreshAsset();
    }

}
