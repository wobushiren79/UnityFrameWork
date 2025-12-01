using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 条件编译指令，检查是否导入了 TextMeshPro
#if UNITY_2017_4_OR_NEWER
using TMPro;
#endif

public class UIEditorWindow : EditorWindow
{
    // 菜单项
    [MenuItem("Custom/工具弹窗/字体处理")]
    public static void Open()
    {
        GetWindow<UIEditorWindow>("字体处理工具");
    }
    
    // 原生 UI 字体
    public Font selectOldFont;
    public Font selectNewFont;
    
    // TextMeshPro 字体
#if UNITY_2017_4_OR_NEWER
    public TMP_FontAsset selectOldTMPFont;
    public TMP_FontAsset selectNewTMPFont;
#endif
    
    // UI 状态
    private Vector2 scrollPosition;
    private bool showNativeUI = true;
    private bool showTextMeshPro = true;
    private bool showOptions = true;

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHeader();
        DrawNativeUISection();
        
#if UNITY_2017_4_OR_NEWER
        DrawTextMeshProSection();
#endif
        
        DrawOptionsSection();
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("字体批量处理工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("此工具用于批量替换场景或预制体中的字体", MessageType.Info);
        EditorGUILayout.Space(10);
    }

    private void DrawNativeUISection()
    {
        showNativeUI = EditorGUILayout.Foldout(showNativeUI, "原生 UI 字体替换", true);
        if (!showNativeUI) return;
        
        EditorGUILayout.BeginVertical("box");
        
        selectOldFont = (Font)EditorGUILayout.ObjectField("原字体", selectOldFont, typeof(Font), true);
        selectNewFont = (Font)EditorGUILayout.ObjectField("新字体", selectNewFont, typeof(Font), true);
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("替换选中对象的字体", GUILayout.Height(30)))
        {
            if (ValidateFonts(selectOldFont, selectNewFont))
            {
                ReplaceFontsForSelection<Text>(selectOldFont, selectNewFont);
            }
        }
        
        if (GUILayout.Button("替换文件夹下所有预制体的字体", GUILayout.Height(30)))
        {
            if (ValidateFonts(selectOldFont, selectNewFont))
            {
                ReplaceFontsForSelectedFolder<Text>(selectOldFont, selectNewFont);
            }
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

#if UNITY_2017_4_OR_NEWER
    private void DrawTextMeshProSection()
    {
        showTextMeshPro = EditorGUILayout.Foldout(showTextMeshPro, "TextMeshPro 字体替换", true);
        if (!showTextMeshPro) return;
        
        EditorGUILayout.BeginVertical("box");
        
        selectOldTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField("原 TMP 字体", selectOldTMPFont, typeof(TMP_FontAsset), true);
        selectNewTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField("新 TMP 字体", selectNewTMPFont, typeof(TMP_FontAsset), true);
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("替换选中对象的 TMP 字体", GUILayout.Height(30)))
        {
            if (ValidateTMPFonts(selectOldTMPFont, selectNewTMPFont))
            {
                ReplaceFontsForSelection<TMP_Text>(selectOldTMPFont, selectNewTMPFont);
            }
        }
        
        if (GUILayout.Button("替换文件夹下所有预制体的 TMP 字体", GUILayout.Height(30)))
        {
            if (ValidateTMPFonts(selectOldTMPFont, selectNewTMPFont))
            {
                ReplaceFontsForSelectedFolder<TMP_Text>(selectOldTMPFont, selectNewTMPFont);
            }
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }
#endif

    private void DrawOptionsSection()
    {
        showOptions = EditorGUILayout.Foldout(showOptions, "高级选项", true);
        if (!showOptions) return;
        
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("BestFit 修正", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("将启用 BestFit 的 Text 组件的 fontSize 设置为 resizeTextMaxSize", MessageType.Info);
        
        if (GUILayout.Button("修正选中对象的 BestFit", GUILayout.Height(30)))
        {
            FixTextBestFitForSelection();
        }
        
        if (GUILayout.Button("修正文件夹下所有预制体的 BestFit", GUILayout.Height(30)))
        {
            FixTextBestFitForSelectedFolder();
        }
        
        EditorGUILayout.EndVertical();
    }

    #region 验证方法
    private bool ValidateFonts(Font oldFont, Font newFont)
    {
        if (oldFont == null || newFont == null)
        {
            EditorUtility.DisplayDialog("错误", "请选择原字体和新字体！", "确定");
            return false;
        }
        
        if (oldFont == newFont)
        {
            EditorUtility.DisplayDialog("警告", "原字体和新字体相同，无需替换！", "确定");
            return false;
        }
        
        return true;
    }

#if UNITY_2017_4_OR_NEWER
    private bool ValidateTMPFonts(TMP_FontAsset oldFont, TMP_FontAsset newFont)
    {
        if (oldFont == null || newFont == null)
        {
            EditorUtility.DisplayDialog("错误", "请选择原TMP字体和新TMP字体！", "确定");
            return false;
        }
        
        if (oldFont == newFont)
        {
            EditorUtility.DisplayDialog("警告", "原TMP字体和新TMP字体相同，无需替换！", "确定");
            return false;
        }
        
        return true;
    }
#endif
    #endregion

    #region 通用替换方法
    private void ReplaceFontsForSelection<T>(object oldFont, object newFont) where T : Component
    {
        List<T> components = GetComponentsFromSelection<T>();
        
        if (components.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到选中的对象或组件！", "确定");
            return;
        }
        
        int replacedCount = 0;
        int processedCount = 0;
        
        try
        {
            foreach (T component in components)
            {
                processedCount++;
                EditorUtility.DisplayProgressBar("处理中", 
                    $"正在处理第 {processedCount} 个对象...", 
                    (float)processedCount / components.Count);
                
                if (ReplaceFontOnComponent(component, oldFont, newFont))
                {
                    replacedCount++;
                    EditorUtility.SetDirty(component);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        if (replacedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        EditorUtility.DisplayDialog("完成", 
            $"处理完成！\n共处理 {components.Count} 个对象\n成功替换 {replacedCount} 个字体", 
            "确定");
    }
    
    private void ReplaceFontsForSelectedFolder<T>(object oldFont, object newFont) where T : Component
    {
        List<T> components = GetComponentsFromSelectedFolder<T>();
        
        if (components.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到选中的文件夹或组件！", "确定");
            return;
        }
        
        if (!EditorUtility.DisplayDialog("确认", 
            $"即将处理 {components.Count} 个组件，确定要继续吗？", 
            "确定", "取消"))
        {
            return;
        }
        
        int replacedCount = 0;
        int processedCount = 0;
        
        try
        {
            foreach (T component in components)
            {
                processedCount++;
                EditorUtility.DisplayProgressBar("处理中", 
                    $"正在处理第 {processedCount} 个对象...", 
                    (float)processedCount / components.Count);
                
                if (ReplaceFontOnComponent(component, oldFont, newFont))
                {
                    replacedCount++;
                    EditorUtility.SetDirty(component);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        if (replacedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        EditorUtility.DisplayDialog("完成", 
            $"处理完成！\n共处理 {components.Count} 个对象\n成功替换 {replacedCount} 个字体", 
            "确定");
    }
    
    private bool ReplaceFontOnComponent<T>(T component, object oldFont, object newFont) where T : Component
    {
        Undo.RecordObject(component, $"Replace Font on {component.name}");
        
        if (component is Text text)
        {
            Font old = oldFont as Font;
            Font @new = newFont as Font;
            
            if (text.font == old)
            {
                text.font = @new;
                return true;
            }
        }
#if UNITY_2017_4_OR_NEWER
        else if (component is TMP_Text tmpText)
        {
            TMP_FontAsset old = oldFont as TMP_FontAsset;
            TMP_FontAsset @new = newFont as TMP_FontAsset;
            
            if (tmpText.font == old)
            {
                tmpText.font = @new;
                return true;
            }
        }
#endif
        
        return false;
    }
    #endregion

    #region 获取组件方法
    private List<T> GetComponentsFromSelection<T>() where T : Component
    {
        List<T> components = new List<T>();
        
        // 获取选中的 GameObject
        GameObject[] selectedGameObjects = Selection.gameObjects;
        
        foreach (GameObject go in selectedGameObjects)
        {
            T[] foundComponents = go.GetComponentsInChildren<T>(true);
            components.AddRange(foundComponents);
        }
        
        // 同时获取直接选中的组件
        Object[] selectedComponents = Selection.GetFiltered(typeof(T), SelectionMode.Deep);
        foreach (Object obj in selectedComponents)
        {
            if (obj is T component)
            {
                if (!components.Contains(component))
                {
                    components.Add(component);
                }
            }
        }
        
        return components;
    }
    
    private List<T> GetComponentsFromSelectedFolder<T>() where T : Component
    {
        List<T> components = new List<T>();
        Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        
        foreach (Object obj in selectedObjects)
        {
            // 只处理预制体
            if (!(obj is GameObject go)) continue;
            
            T[] foundComponents = go.GetComponentsInChildren<T>(true);
            components.AddRange(foundComponents);
        }
        
        return components;
    }
    #endregion

    #region BestFit 修正方法
    private void FixTextBestFitForSelection()
    {
        List<Text> textComponents = GetComponentsFromSelection<Text>();
        
        if (textComponents.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到选中的 Text 组件！", "确定");
            return;
        }
        
        int fixedCount = 0;
        
        foreach (Text text in textComponents)
        {
            if (text.resizeTextForBestFit && text.fontSize != text.resizeTextMaxSize)
            {
                Undo.RecordObject(text, $"Fix BestFit on {text.name}");
                text.fontSize = text.resizeTextMaxSize;
                EditorUtility.SetDirty(text);
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        EditorUtility.DisplayDialog("完成", 
            $"修正完成！\n共处理 {textComponents.Count} 个 Text 组件\n修正了 {fixedCount} 个 BestFit 设置", 
            "确定");
    }
    
    private void FixTextBestFitForSelectedFolder()
    {
        List<Text> textComponents = GetComponentsFromSelectedFolder<Text>();
        
        if (textComponents.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到文件夹中的 Text 组件！", "确定");
            return;
        }
        
        if (!EditorUtility.DisplayDialog("确认", 
            $"即将处理 {textComponents.Count} 个 Text 组件，确定要继续吗？", 
            "确定", "取消"))
        {
            return;
        }
        
        int fixedCount = 0;
        
        try
        {
            for (int i = 0; i < textComponents.Count; i++)
            {
                Text text = textComponents[i];
                EditorUtility.DisplayProgressBar("处理中", 
                    $"正在处理第 {i + 1} 个 Text...", 
                    (float)i / textComponents.Count);
                
                if (text.resizeTextForBestFit && text.fontSize != text.resizeTextMaxSize)
                {
                    Undo.RecordObject(text, $"Fix BestFit on {text.name}");
                    text.fontSize = text.resizeTextMaxSize;
                    EditorUtility.SetDirty(text);
                    fixedCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        EditorUtility.DisplayDialog("完成", 
            $"修正完成！\n共处理 {textComponents.Count} 个 Text 组件\n修正了 {fixedCount} 个 BestFit 设置", 
            "确定");
    }
    #endregion
}