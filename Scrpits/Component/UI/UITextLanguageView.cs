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
            Text textUI = GetComponent<Text>();
            string textContent = TextHandler.Instance.GetTextById(textId);
            if (textUI != null && !textContent.IsNull())
            {
                textUI.text = $"{textContent}";
            }
        }
    }
}