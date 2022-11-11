using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SelectColorView : BaseUIView
{
    private ICallBack callBack;

    public Slider colorR;
    public Slider colorG;
    public Slider colorB;

    public Text tvTitle;

    private void Start()
    {
        if (colorR != null)
            colorR.onValueChanged.AddListener(ChangeValue);
        if (colorG != null)
            colorG.onValueChanged.AddListener(ChangeValue);
        if (colorB != null)
            colorB.onValueChanged.AddListener(ChangeValue);
    }

    protected void ChangeValue(float value)
    {
        if (this.callBack != null)
            this.callBack.SelectColorChange(this,colorR.value, colorG.value, colorB.value);
    }

    /// <summary>
    /// 设置随机颜色
    /// </summary>
    public void SetRandomColor()
    {
        SetData(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }

   /// <summary>
   /// 设置数据
   /// </summary>
   /// <param name="colorValueR"></param>
   /// <param name="colorValueG"></param>
   /// <param name="colorValueB"></param>
    public void SetData(float colorValueR, float colorValueG, float colorValueB)
    {
        colorR.value = colorValueR;
        colorG.value = colorValueG;
        colorB.value = colorValueB;
    }

    /// <summary>
    /// 设置回调
    /// </summary>
    /// <param name="callBack"></param>
    public void SetCallBack(ICallBack callBack)
    {
        this.callBack = callBack;
    }

    /// <summary>
    /// 获取颜色
    /// </summary>
    /// <returns></returns>
    public ColorBean GetColorBean()
    {
        if (colorR == null || colorG == null || colorB == null)
            return null;
        ColorBean colorBean = new ColorBean();
        colorBean.r = colorR.value;
        colorBean.g = colorG.value;
        colorBean.b = colorB.value;
        colorBean.a = 1;
        return colorBean;
    }
    public Color GetColor()
    {
        if (colorR == null || colorG == null || colorB == null)
            return new Color();
        Color color = new Color(colorR.value, colorG.value, colorB.value);
        return color;
    }

    /// <summary>
    /// 回调
    /// </summary>
    public interface ICallBack
    {
        void SelectColorChange(SelectColorView colorView,float r, float g, float b);
    }
}