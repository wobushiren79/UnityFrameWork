using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class OnAssetsEvent : UnityEditor.AssetModificationProcessor
{
    [InitializeOnLoadMethod]
    static void EditorApplication_ProjectChanged()
    {
        //--projectWindowChanged已过时
        //--全局监听Project视图下的资源是否发生变化（添加 删除 移动等）
        EditorApplication.projectChanged += delegate ()
        {

        };
        // 打开Prefab编辑界面回调
        //PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        // Prefab被保存之前回调
        //PrefabStage.prefabSaving += OnPrefabSaving;
        // Prefab被保存之后回调
        //PrefabStage.prefabSaved += OnPrefabSaved;
        // 关闭Prefab编辑界面回调
        //PrefabStage.prefabStageClosing += OnPrefabStageClosing;
    }

    //--监听“双击鼠标左键，打开资源”事件
    public static bool IsOpenForEdit(string assetPath, out string message)
    {
        message = null;

        return true;
    }

    //--监听“资源即将被创建”事件
    public static void OnWillCreateAsset(string path)
    {

    }

    //--监听“资源即将被保存”事件
    public static string[] OnWillSaveAssets(string[] paths)
    {
        if (paths != null)
        {

        }
        return paths;
    }

    //--监听“资源即将被移动”事件
    public static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
    {

        return AssetMoveResult.DidNotMove;
    }

    //--监听“资源即将被删除”事件
    public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
    {

        return AssetDeleteResult.DidNotDelete;
    }
}