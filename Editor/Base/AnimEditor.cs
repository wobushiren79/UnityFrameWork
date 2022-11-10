using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class AnimEditor : Editor
{
    [MenuItem("Custom/Anim/CreateAnim 10张每秒 Render")]
    public static void CreateAnim10sForSR()
    {
        Object obj = Selection.activeObject;
        if (obj == null)
            return;
        Texture2D tex2d = obj as Texture2D;
        if (tex2d == null)
            return;

        string path = EditorUtil.GetSelectionPathByObj(obj);
        path = path.Replace($"/{tex2d.name}.png", "");
        CreateAnimForImage(tex2d, path, 10);
    }

    [MenuItem("Custom/Anim/CreateAnim 10张每秒 UI")]
    public static void CreateAnim10sForImage()
    {
        Object obj = Selection.activeObject;
        if (obj == null)
            return;
        Texture2D tex2d = obj as Texture2D;
        if (tex2d == null)
            return;

        string path = EditorUtil.GetSelectionPathByObj(obj);
        path = path.Replace($"/{tex2d.name}.png", "");
        CreateAnimForImage(tex2d, path, 10);
    }

    public static void CreateAnimForSpriteRenderer(Texture2D itemPicTex, string animPath, int numberForS)
    {
        CreateAnimForTex(1, itemPicTex, animPath, numberForS);
    }

    public static void CreateAnimForImage(Texture2D itemPicTex, string animPath, int numberForS)
    {
        CreateAnimForTex(2, itemPicTex, animPath, numberForS);
    }

    /// <summary>
    /// 创建图片的动画
    /// </summary>
    /// <param name="type">1.UI图片  2.SpriteRenderer</param>
    /// <param name="itemPicTex"></param>
    /// <param name="numberForS">1秒多少张</param>
    public static void CreateAnimForTex(int type, Texture2D itemPicTex, string animPath, int numberForS)
    {
        AnimationClip clip = new AnimationClip();
        //设置帧率为30
        clip.frameRate = 30;

        //属性参数
        EditorCurveBinding curveBinding = new EditorCurveBinding();
        switch (type)
        {
            case 1:
                curveBinding.type = typeof(SpriteRenderer);
                break;
            case 2:
                curveBinding.type = typeof(Image);
                break;
        }
        curveBinding.path = "";
        curveBinding.propertyName = "m_Sprite";

        //帧数相关
        ObjectReferenceKeyframe[] keyFrames = null;

        string path = AssetDatabase.GetAssetPath(itemPicTex);
        Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
        keyFrames = new ObjectReferenceKeyframe[objs.Length];
        //动画长度是按秒为单位，1/10就表示1秒切10张图片，根据项目的情况可以自己调节
        float frameTime = 1 / (float)numberForS;
        int i = 0;
        objs.ToList().ForEach(obj =>
        {
            if (obj as Sprite != null)
            {
                Sprite itemSprite = obj as Sprite;
                keyFrames[i] = new ObjectReferenceKeyframe();
                keyFrames[i].time = frameTime * i;
                keyFrames[i].value = itemSprite;
                i++;
            }
        });
        //最后结束再加一个第一帧
        keyFrames[i] = new ObjectReferenceKeyframe();
        keyFrames[i].time = frameTime * i;
        keyFrames[i].value = keyFrames[0].value;

        //设置idle文件为循环动画
        AnimationClipSettings clipSetting = AnimationUtility.GetAnimationClipSettings(clip);
        clipSetting.loopTime = !clip.isLooping;
        AnimationUtility.SetAnimationClipSettings(clip, clipSetting);
        //设置参数
        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
        AssetDatabase.CreateAsset(clip, animPath + "/" + itemPicTex.name + ".anim");
        AssetDatabase.SaveAssets();
    }

}