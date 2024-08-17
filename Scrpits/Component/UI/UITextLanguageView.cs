using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UITextLanguageView : BaseMonoBehaviour
{
    //文本ID
    [Header("文本ID")]
    public long textId;

    public void Awake()
    {
        if (textId != 0)
        {
            string textContent = TextHandler.Instance.GetTextById(textId);
            if (textContent.IsNull())
                return;
            Text textUI = GetComponent<Text>();
            if (textUI != null)
            {
                textUI.text = $"{textContent}";
                return;
            }
            TextMeshProUGUI textMeshUI = GetComponent<TextMeshProUGUI>();
            if (textMeshUI != null)
            {
                textMeshUI.text = $"{textContent}";
                return;
            }
        }
    }
}