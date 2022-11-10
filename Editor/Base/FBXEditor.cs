using System.IO;
using UnityEditor;
using UnityEngine;

public class FBXEditor : Editor
{

    /// <summary>
    /// 修改材质
    /// </summary>
    /// <param name="FBXPath">FBX的路径</param>
    /// <param name="expectedMaterialPath">替换材质的路径</param>
    public static void ChangeMaterial(string FBXPath, string expectedMaterialPath)
    {
        if (FBXPath.Contains(".meta"))
            return;
        AssetImporter assetImporter = AssetImporter.GetAtPath(FBXPath);
        if (assetImporter == null)
            return;
        ModelImporter modelImporter = assetImporter as ModelImporter;
        if (!modelImporter)
            return;
        
        //获取替换的材质
        Material expectedMaterial= EditorUtil.GetAssetByPath<Material>(expectedMaterialPath);
        if (expectedMaterial == null)
            return;

        //保存数据
        SerializedObject modelImporterObj = new SerializedObject(modelImporter);

        var materials = modelImporterObj.FindProperty("m_Materials");
        var externalObjects = modelImporterObj.FindProperty("m_ExternalObjects");

        for (int materialIndex = 0; materialIndex < materials.arraySize; materialIndex++)
        {
            var id = materials.GetArrayElementAtIndex(materialIndex);
            var name = id.FindPropertyRelative("name").stringValue;
            var type = id.FindPropertyRelative("type").stringValue;
            var assembly = id.FindPropertyRelative("assembly").stringValue;

            SerializedProperty materialProperty = null;

            for (int externalObjectIndex = 0; externalObjectIndex < externalObjects.arraySize; externalObjectIndex++)
            {
                var currentSerializedProperty = externalObjects.GetArrayElementAtIndex(externalObjectIndex);
                var externalName = currentSerializedProperty.FindPropertyRelative("first.name").stringValue;
                var externalType = currentSerializedProperty.FindPropertyRelative("first.type").stringValue;

                if (externalType == type && externalName == name)
                {
                    materialProperty = currentSerializedProperty.FindPropertyRelative("second");
                    break;
                }
            }

            if (materialProperty == null)
            {
                var lastIndex = externalObjects.arraySize++;
                var currentSerializedProperty = externalObjects.GetArrayElementAtIndex(lastIndex);
                currentSerializedProperty.FindPropertyRelative("first.name").stringValue = name;
                currentSerializedProperty.FindPropertyRelative("first.type").stringValue = type;
                currentSerializedProperty.FindPropertyRelative("first.assembly").stringValue = assembly;
                currentSerializedProperty.FindPropertyRelative("second").objectReferenceValue = expectedMaterial;
            }
            else
            {
                materialProperty.objectReferenceValue = expectedMaterial;
            }
        }
        modelImporterObj.ApplyModifiedPropertiesWithoutUndo();
        modelImporterObj.ApplyModifiedProperties();

        modelImporter.SaveAndReimport();
        AssetDatabase.ImportAsset(FBXPath);
    }

    /// <summary>
    /// 修改动画
    /// </summary>
    public static void ChangeAnim(string FBXPath,
        string clipName = null,
        bool isLoop = true,
        WrapMode wrapMode = WrapMode.Loop,
        ModelImporterAnimationCompression animationCompression = ModelImporterAnimationCompression.Off)
    {
        if (FBXPath.Contains(".meta"))
            return;
        AssetImporter assetImporter = AssetImporter.GetAtPath(FBXPath);
        if (assetImporter == null)
            return;
        ModelImporter modelImporter = assetImporter as ModelImporter;
        if (!modelImporter)
            return;
        GameObject objFBX = EditorUtil.GetAssetByPath<GameObject>(FBXPath);

        modelImporter.generateAnimations = ModelImporterGenerateAnimations.InRoot;
        modelImporter.importAnimation = true;

        var clips = modelImporter.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = modelImporter.defaultClipAnimations;
        //如果有一个动画。修改名字为文件名字
        if (clips != null && clips.Length == 1)
        {
            ModelImporterClipAnimation modelImporterClip = clips[0];
            if(clipName == null)
                modelImporterClip.name = objFBX.name;
            else
                modelImporterClip.name = clipName;
            modelImporterClip.loop = isLoop;
            modelImporterClip.loopTime = isLoop;
            modelImporterClip.wrapMode = wrapMode;
        }

        AvatarMask mask = new AvatarMask();
        mask.AddTransformPath(objFBX.transform, true);
        //修改动画属性
        foreach (var itemClip in clips)
        {
            //itemClip.maskType = ClipAnimationMaskType.CreateFromThisModel;
            //itemClip.ConfigureClipFromMask(mask);
            itemClip.loop = isLoop;
            itemClip.loopTime = isLoop;
            itemClip.wrapMode = wrapMode;
        }
        modelImporter.clipAnimations = clips;
        //modelImporter.motionNodeName = "root";
        modelImporter.animationCompression = animationCompression;

        //RIG
        //modelImporter.animationType = ModelImporterAnimationType.Human;
        //modelImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        //SerializedObject modelImporterObj = new SerializedObject(modelImporter);
        //modelImporterObj.ApplyModifiedProperties();

        //modelImporter.SaveAndReimport();
        //AssetDatabase.ImportAsset(FBXPath);

        //var avatarAsset = AssetDatabase.LoadAssetAtPath("Assets/GamePlay/Resources/NewPlayer/BaseBallPlayer.FBX", typeof(Avatar)) as Avatar;
        //modelImporter.sourceAvatar = avatarAsset;
        modelImporter.SaveAndReimport();
        AssetDatabase.ImportAsset(FBXPath);
    }
}