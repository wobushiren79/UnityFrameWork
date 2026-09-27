using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// SkeletonGraphicExtend 一键替换工具：把 UI 预制体中的 SkeletonGraphic 批量升级为 SkeletonGraphicExtend
/// （修复 RectMask2D 按 rect 整体误剔除非居中骨架的问题）。
/// 字段经 CopyComponent/PasteComponentValues 迁移、预制体内引用自动重定向、组件顺序保持，可重复执行（幂等）。
/// </summary>
public static class SkeletonGraphicExtendMenu
{
    #region 菜单入口
    /// <summary>Spine 原生 SkeletonGraphic 脚本 GUID（预制体文本预筛用）</summary>
    const string SkeletonGraphicGUID = "d85b887af7e6c3f45a2e2d2920d641bc";
    /// <summary>预制体扫描目录</summary>
    static readonly string[] SearchFolders = { "Assets/Resources/UI" };

    /// <summary>
    /// 批量把 UI 预制体中的 SkeletonGraphic 替换为 SkeletonGraphicExtend（字段迁移 + 引用重定向）
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
                int count = ReplaceInPrefab(path);
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
    #endregion

    #region 单个预制体替换
    /// <summary>
    /// 处理单个预制体，返回替换数量（0 = 无需替换）
    /// </summary>
    static int ReplaceInPrefab(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            int count = 0;
            //先取快照数组，遍历中会销毁旧组件
            SkeletonGraphic[] list = root.GetComponentsInChildren<SkeletonGraphic>(true);
            foreach (SkeletonGraphic oldComp in list)
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
                ReplaceComponent(root, oldComp);
                count++;
            }
            if (count > 0)
                PrefabUtility.SaveAsPrefabAsset(root, path);
            return count;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// 把单个 SkeletonGraphic 替换为 SkeletonGraphicExtend：字段迁移 + 引用重定向 + 保持组件顺序
    /// </summary>
    static void ReplaceComponent(GameObject root, SkeletonGraphic oldComp)
    {
        GameObject go = oldComp.gameObject;
        //记录旧组件在组件列表中的位置，替换后保持顺序不变
        int oldIndex = System.Array.IndexOf(go.GetComponents<Component>(), oldComp);

        SkeletonGraphicExtend newComp = go.AddComponent<SkeletonGraphicExtend>();
        //CopyComponent/PasteComponentValues = Inspector 右键「复制/粘贴组件值」，按属性名迁移全部字段且不改目标类型
        ComponentUtility.CopyComponent(oldComp);
        ComponentUtility.PasteComponentValues(newComp);
        //重定向预制体内所有指向旧组件的引用（UI Component 的 ui_Icon 等序列化字段）
        RedirectReferences(root, oldComp, newComp);
        //新组件先上移到旧组件之后，销毁旧组件后即占据原位置
        int moveNum = go.GetComponents<Component>().Length - 1 - (oldIndex + 1);
        for (int i = 0; i < moveNum; i++)
            ComponentUtility.MoveComponentUp(newComp);
        Object.DestroyImmediate(oldComp);
    }
    #endregion

    #region 引用重定向
    /// <summary>
    /// 遍历预制体内所有组件的序列化属性，把指向旧组件的引用改指新组件
    /// </summary>
    static void RedirectReferences(GameObject root, SkeletonGraphic oldComp, SkeletonGraphicExtend newComp)
    {
        foreach (Component comp in root.GetComponentsInChildren<Component>(true))
        {
            //missing script 槽位为 null，跳过；新旧组件自身不处理
            if (comp == null || comp == oldComp || comp == newComp)
                continue;
            SerializedObject so = new SerializedObject(comp);
            SerializedProperty prop = so.GetIterator();
            bool enterChildren = true;
            while (prop.Next(enterChildren))
            {
                //仅结构/数组进入子级，ObjectReference 不展开
                enterChildren = prop.propertyType == SerializedPropertyType.Generic;
                if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue == oldComp)
                    prop.objectReferenceValue = newComp;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    #endregion
}
