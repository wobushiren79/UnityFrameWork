using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;
public class StrengthTestView : BaseMonoBehaviour
{
    //标题
    public Text tvTitle;
    //力度
    public Slider sliderStrength;
    //速度
    public float strengthSpeed = 0.5f;
    //是否正在测试
    private bool mIsTest = false;

    private int mDirection = 1;
    private ICallBack mCallBack;

    private void Update()
    {
        if (mIsTest)
        {
            if (Input.GetButtonDown(InputInfo.Interactive_E))
            {
                mIsTest = false;
                if (mCallBack != null)
                    mCallBack.StrengthTestEnd(this, sliderStrength.value);
                return;
            }
            //if (sliderStrength.value >= 1)
            //    mDirection = -1;
            //else if (sliderStrength.value <= 0)
            //    mDirection = 1;
            if (sliderStrength.value >= 1)
                sliderStrength.value = 0;
            sliderStrength.value += (strengthSpeed * Time.deltaTime * mDirection);
        }
    }

    private void FixedUpdate()
    {

    }

    /// <summary>
    /// 开始测试
    /// </summary>
    public void StartTest()
    {
        StartCoroutine(CoroutineForStart());
    }

    /// <summary>
    /// 设置回调
    /// </summary>
    /// <param name="callBack"></param>
    public void SetCallBack(ICallBack callBack)
    {
        mCallBack = callBack;
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="name"></param>
    /// <param name="speed"></param>
    public void SetData(string name, float speed)
    {
        mDirection = 1;
        sliderStrength.value = 0;
        SetTitle(name);
        SetSpeed(speed);
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    /// <param name="name"></param>
    public void SetTitle(string name)
    {
        if (tvTitle != null)
        {
            tvTitle.text = name;
        }
    }

    /// <summary>
    /// 设置速度
    /// </summary>
    /// <param name="speed"></param>
    public void SetSpeed(float speed)
    {
        this.strengthSpeed = speed;
    }

    private IEnumerator CoroutineForStart()
    {
        yield return new WaitForEndOfFrame();
        mDirection = 1;
        sliderStrength.value = 0;
        mIsTest = true;
    }

    public interface ICallBack
    {
        /// <summary>
        /// 力度测试结束
        /// </summary>
        /// <param name="view"></param>
        /// <param name="value"></param>
        void StrengthTestEnd(StrengthTestView view, float value);
    }
}