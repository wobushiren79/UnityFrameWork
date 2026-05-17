using UnityEditor;
using UnityEngine;

/// <summary>
/// UITextLanguageView的Inspector扩展，支持在编辑器中预览多语言文本内容
/// </summary>
[CustomEditor(typeof(UITextLanguageView))]
public class InspectorUITextLanguageView : Editor
{
    private UITextLanguageView targetView;

    #region 预览数据
    private string previewText = "";
    private bool isPreviewError = false;
    private int selectedLanguageIndex = 0;
    private readonly string[] languageOptions = new string[] { "cn", "en" };
    private GUIStyle previewTextStyle;
    private GUIStyle previewErrorStyle;
    private GUIStyle previewBoxStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialized = false;
    #endregion

    private void OnEnable()
    {
        targetView = (UITextLanguageView)target;
    }

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        previewTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            fontSize = 13,
            richText = true,
            padding = new RectOffset(8, 8, 8, 8)
        };

        previewErrorStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            fontSize = 13,
            richText = true,
            padding = new RectOffset(8, 8, 8, 8),
            normal = { textColor = Color.red }
        };

        previewBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            margin = new RectOffset(5, 5, 10, 10),
            padding = new RectOffset(5, 5, 5, 5)
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 28
        };

        stylesInitialized = true;
    }

    public override void OnInspectorGUI()
    {
        InitializeStyles();

        // 绘制默认Inspector（包含textId字段）
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(5);

        #region 多语言预览区域
        EditorGUILayout.LabelField("多语言文本预览", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 语言选择
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("预览语言:", GUILayout.Width(70));
        selectedLanguageIndex = EditorGUILayout.Popup(selectedLanguageIndex, languageOptions);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // 加载按钮
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("加载预览", buttonStyle, GUILayout.Width(120)))
        {
            LoadPreviewText();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 预览内容显示
        EditorGUILayout.LabelField("预览内容:", EditorStyles.boldLabel);
        using (new GUILayout.VerticalScope(previewBoxStyle))
        {
            if (string.IsNullOrEmpty(previewText))
            {
                EditorGUILayout.LabelField("(点击\"加载预览\"按钮查看文本内容)",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    {
                        fontSize = 12,
                        padding = new RectOffset(0, 0, 20, 20)
                    });
            }
            else
            {
                GUIStyle style = isPreviewError ? previewErrorStyle : previewTextStyle;
                EditorGUILayout.SelectableLabel(previewText, style,
                    GUILayout.MinHeight(60), GUILayout.ExpandHeight(true));
            }
        }
        #endregion

        // 应用修改
        if (GUI.changed)
        {
            EditorUtility.SetDirty(targetView);
        }
    }

    /// <summary>
    /// 加载并显示预览文本
    /// </summary>
    private void LoadPreviewText()
    {
        if (targetView.textId == 0)
        {
            previewText = "[错误] 文本ID为0，请设置有效的文本ID";
            return;
        }

        // 保存当前语言设置
        string originalLanguage = LanguageCfg.currentLanguage;
        string targetLanguage = languageOptions[selectedLanguageIndex];

        try
        {
            // LanguageCfg.dicData 以 cfgName 为键缓存数据，不包含语言信息
            // 切换语言时必须通过 ChangeLanguageData 触发缓存清空，否则返回的是旧语言数据
            LanguageCfg.ChangeLanguageData("__temp__");
            LanguageCfg.ChangeLanguageData(targetLanguage);

            // 通过LanguageCfg直接获取文本内容（编辑器模式下不依赖TextHandler单例）
            LanguageBean languageBean = LanguageCfg.GetItemData(UITextCfg.fileName, targetView.textId);

            if (languageBean == null)
            {
                previewText = $"[未找到对应多语言内容] ID为 {targetView.textId} 的文本在语言 \"{targetLanguage}\" 下不存在";
                isPreviewError = true;
            }
            else if (string.IsNullOrEmpty(languageBean.content))
            {
                previewText = $"[无内容] ID为 {targetView.textId} 的多语言内容为空，请检查Excel配置";
                isPreviewError = true;
            }
            else
            {
                previewText = languageBean.content;
                isPreviewError = false;
            }
        }
        finally
        {
            // 恢复原始语言设置，同样需要触发缓存清空
            LanguageCfg.ChangeLanguageData("__temp__");
            LanguageCfg.ChangeLanguageData(originalLanguage);
        }
    }
}
