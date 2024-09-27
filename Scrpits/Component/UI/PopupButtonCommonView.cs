using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PopupButtonCommonView : BaseUIView, IPointerEnterHandler, IPointerExitHandler
{
    protected Button btnTarget;
    //弹窗数据
    protected PopupEnum popupEnum;

    protected object targetData;
    public void Start()
    {
        btnTarget = transform.GetComponent<Button>();
        if (btnTarget != null)
            btnTarget.onClick.AddListener(OnClickForTarget);
    }

    public void SetData(object targetData, PopupEnum popupEnum)
    {
        this.targetData = targetData;
        this.popupEnum = popupEnum;

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PopupShowView popupShowView = UIHandler.Instance.ShowPopup(new PopupBean(popupEnum));
        PopupShowCommonView popupShowCommonView = popupShowView as PopupShowCommonView;
        popupShowCommonView.SetData(targetData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CleanData();
    }

    public void OnClickForTarget()
    {

    }

    /// <summary>
    /// 清除数据
    /// </summary>
    public virtual void CleanData()
    {
        UIHandler.Instance.HidePopup(popupEnum);
    }

}