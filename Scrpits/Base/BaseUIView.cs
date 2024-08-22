using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BaseUIView : BaseUIInit
{
    public RectTransform _rectTransform;

    public RectTransform rectTransform
    {
        get
        {
            if (_rectTransform == null)
            {
                _rectTransform = ((RectTransform)transform);
            }
            return _rectTransform;
        }
    }

    //原始UI大小
    protected Vector2 uiSizeOriginal;

    public override void Awake()
    {
        base.Awake();
        uiSizeOriginal = rectTransform.sizeDelta;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        RegisterInputAction();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        UnRegisterInputAction();
    }
}