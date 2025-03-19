using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PopupButtonCommonView : BaseUIView, IPointerEnterHandler, IPointerExitHandler
{    
    //延迟展示
    public float timeDelayShow = 0;
    protected float timeDelayShowUpdate = 0;
    protected bool timeDelayShowStart = false;
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

    public void Update()
    {
        if(timeDelayShowStart && timeDelayShow > 0)
        {
            timeDelayShowUpdate += Time.deltaTime;
            if(timeDelayShowUpdate > timeDelayShow)
            {
                ShowPopupUI();
                timeDelayShowStart = false;
                timeDelayShowUpdate = 0;
            }
        }
    }

    public void SetData(object targetData, PopupEnum popupEnum)
    {
        this.targetData = targetData;
        this.popupEnum = popupEnum;
    }

    public void ShowPopupUI()
    {
        PopupShowView popupShowView = UIHandler.Instance.ShowPopup(new PopupBean(popupEnum));
        PopupShowCommonView popupShowCommonView = popupShowView as PopupShowCommonView;
        popupShowCommonView.SetData(targetData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(targetData == null)
            return;
        timeDelayShowStart = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearData();
    }

    public void OnClickForTarget()
    {

    }

    /// <summary>
    /// 清除数据
    /// </summary>
    public virtual void ClearData()
    {               
        timeDelayShowUpdate = 0;
        timeDelayShowStart = false;
        UIHandler.Instance.HidePopup(popupEnum);
    }

}