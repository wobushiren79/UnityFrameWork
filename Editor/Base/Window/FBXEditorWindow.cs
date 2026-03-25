using UnityEditor;
using UnityEngine;

/// <summary>
/// FBX批量材质替换工具
/// 用途：在Project中选中一个或多个FBX文件（支持选中文件夹递归查找），
///       指定目标材质后一键批量替换所有选中FBX的默认材质。
/// 入口：Custom/工具弹窗/FBX处理
/// </summary>
public class FBXEditorWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/FBX处理")]
    static void CreateWindows()
    {
        var window = GetWindowWithRect<FBXEditorWindow>(new Rect(0, 0, 400, 200));
        window.titleContent = new GUIContent("FBX批量材质替换");
    }

    public Material targetSelectMat;
    private Vector2 scrollPosition;

    private void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // 说明区域
        EditorGUILayout.HelpBox(
            "使用说明：\n" +
            "1. 拖入目标材质\n" +
            "2. 在Project中选中FBX文件或文件夹\n" +
            "3. 点击「批量替换材质」执行",
            MessageType.Info);

        GUILayout.Space(10);

        // 材质选择
        targetSelectMat = EditorUI.GUIObj<Material>("目标材质", targetSelectMat);

        GUILayout.Space(10);

        // 操作按钮
        GUI.enabled = targetSelectMat != null;
        if (EditorUI.GUIButton("批量替换材质", 150))
        {
            OnClickForSetFbxMat();
        }
        GUI.enabled = true;

        GUILayout.EndScrollView();
    }

    /// <summary>
    /// 批量替换选中FBX的材质
    /// </summary>
    private void OnClickForSetFbxMat()
    {
        var gameobjects = EditorUtil.GetSelectionAll<GameObject>();
        if (gameobjects.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在Project中选中FBX文件或文件夹", "确定");
            return;
        }

        if (!EditorUI.GUIDialog("确认操作", $"即将对 {gameobjects.Count} 个FBX替换材质为 [{targetSelectMat.name}]，是否继续？"))
            return;

        int total = gameobjects.Count;
        for (int i = 0; i < total; i++)
        {
            var itemObj = gameobjects[i];
            EditorUI.GUIShowProgressBar("替换材质", $"({i + 1}/{total}) {itemObj.name}", (float)(i + 1) / total);
            FBXEditor.ChangeMaterial(itemObj, targetSelectMat);
        }
        EditorUI.GUIHideProgressBar();
        LogUtil.Log($"材质替换完成，共处理 {total} 个FBX");
    }
}