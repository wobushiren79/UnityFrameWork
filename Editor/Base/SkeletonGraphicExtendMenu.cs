using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SkeletonGraphicExtend 一键替换工具：把 UI 预制体中的 SkeletonGraphic 批量升级为 SkeletonGraphicExtend
/// （修复 RectMask2D 按 rect 整体误剔除非居中骨架的问题）。
/// 原理：SerializedObject 直接改写组件的 m_Script 引用（Unity 官方升级工具的换脚本技巧），
/// 组件 fileID/字段/外部引用全部原样保留，预制体 diff 只有一行 guid 变化。可重复执行（幂等）。
/// </summary>
public static class SkeletonGraphicExtendMenu
{
    #region 菜单入口
    /// <summary>Spine 原生 SkeletonGraphic 脚本 GUID（预制体文本预筛用）</summary>
    const string SkeletonGraphicGUID = "d85b887af7e6c3f45a2e2d2920d641bc";
    /// <summary>预制体扫描目录</summary>
    static readonly string[] SearchFolders = { "Assets/Resources/UI" };

    /// <summary>
    /// 批量把 UI 预制体中的 SkeletonGraphic 替换为 SkeletonGraphicExtend（换 m_Script，引用零改动）
    /// </summary>
    [MenuItem("Custom/Spine/替换 SkeletonGraphic 为 SkeletonGraphicExtend")]
    static void ReplaceSkeletonGraphicInPrefabs()
    {
        if (!EditorUtility.DisplayDialog("替换 SkeletonGraphic",
            "将把 UI 预制体中的 SkeletonGraphic 全部替换为 SkeletonGraphicExtend（修复 RectMask2D 整体误剔除）。\n建议先提交 git 以便审计 diff。是否继续？",
            "继续", "取消"))
        {
            return;
        }

        MonoScript extendScript = GetExtendMonoScript();
        if (extendScript == null)
        {
            Debug.LogError("[SkeletonGraphicExtend] 找不到 SkeletonGraphicExtend.cs 的 MonoScript，替换中止");
            return;
        }

        int prefabNum = 0;
        int replaceNum = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", SearchFolders);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            //文本预筛：不含原生 SkeletonGraphic 的预制体直接跳过
            if (!File.ReadAllText(path).Contains(SkeletonGraphicGUID))
                continue;
            try
            {
                int count = ReplaceInPrefab(path, extendScript);
                if (count > 0)
                {
                    prefabNum++;
                    replaceNum += count;
                    Debug.Log($"[SkeletonGraphicExtend] {path} 替换 {count} 处");
                }
            }
            catch (System.Exception e)
            {
                //单个预制体失败不中断整体流程
                Debug.LogError($"[SkeletonGraphicExtend] {path} 替换失败：{e.Message}");
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[SkeletonGraphicExtend] 替换完成：共 {prefabNum} 个预制体、{replaceNum} 处");
    }

    /// <summary>
    /// 获取 SkeletonGraphicExtend 类的 MonoScript 资产
    /// </summary>
    static MonoScript GetExtendMonoScript()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript SkeletonGraphicExtend"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("/SkeletonGraphicExtend.cs"))
                return AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        }
        return null;
    }
    #endregion

    #region 单个预制体替换
    /// <summary>
    /// 处理单个预制体，返回替换数量（0 = 无需替换）
    /// </summary>
    static int ReplaceInPrefab(string path, MonoScript extendScript)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            int count = 0;
            foreach (SkeletonGraphic oldComp in root.GetComponentsInChildren<SkeletonGraphic>(true))
            {
                //已是扩展版则跳过（幂等）
                if (oldComp is SkeletonGraphicExtend)
                    continue;
                //嵌套预制体实例内的组件不可直接替换
                if (PrefabUtility.GetPrefabInstanceStatus(oldComp.gameObject) != PrefabInstanceStatus.NotAPrefab)
                {
                    Debug.LogWarning($"[SkeletonGraphicExtend] {path} 跳过嵌套实例内的组件：{oldComp.name}");
                    continue;
                }
                //换脚本：组件 fileID/字段/引用全部保留，新增字段 cullByMeshBounds 取脚本默认值 true
                SerializedObject so = new SerializedObject(oldComp);
                so.FindProperty("m_Script").objectReferenceValue = extendScript;
                so.ApplyModifiedPropertiesWithoutUndo();
                count++;
            }
            if (count > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                //保存后自检：预制体文本里必须出现扩展组件的脚本 guid，否则视为失败报出
                string extendGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(extendScript));
                if (!File.ReadAllText(path).Contains(extendGuid))
                    Debug.LogError($"[SkeletonGraphicExtend] {path} 保存后未找到 SkeletonGraphicExtend 组件，请检查 Console 前置报错");
            }
            return count;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
    #endregion
}
