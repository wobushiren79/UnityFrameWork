using DG.Tweening.Plugins.Core.PathCore;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SpineEditor : Editor
{
    //spine����·��
    public static string pathSkeletonData = "Assets/LoadResources/Spine";
    public static string shaderName = "Universal Render Pipeline/Spine/Sprite";

    [MenuItem("Custom/Spine/����������Դ")]
    public static void SpineInit()
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
                ImageEditor.ChangeImageFilterMode(FilterMode.Point, targetPath);
                ImageEditor.ChangeImageTextureImporterType(TextureImporterType.Sprite, targetPath);
                ImageEditor.ChangeImageTextureImporterType(TextureImporterType.Default, targetPath);
            }
            else if (item.Name.Length >= 4 && item.Name.Substring(item.Name.Length - 4).Equals(".mat"))
            {
                //LogUtil.Log($"SpineInit Item Mat {item.Name}");
                Material mat = EditorUtil.GetAssetByPath<Material>(targetPath);
                mat.shader = Shader.Find(shaderName);
                mat.EnableKeyword("_FIXED_NORMALS_VIEWSPACE");
                mat.SetVector("_FixedNormal", new Vector4(0, 0, 1, 1));
            }
        }
        EditorUtil.RefreshAsset();
        LogUtil.Log($"��ʼ�����");
    }


    //[MenuItem("Custom/Spine/�޸İ��")]
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
