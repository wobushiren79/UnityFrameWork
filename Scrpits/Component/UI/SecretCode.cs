using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SecretCode : BaseMonoBehaviour{

    public int secretCodeMaxLength=20;
    public  string tempCode = "";

    private void Update()
    {
        DetectPressedKeyOrButton();
    }

    /// <summary>
    /// 遍历按键
    /// </summary>
    public void DetectPressedKeyOrButton()
    {
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
                CheckCode(kcode.ToString());
        }
    }

    /// <summary>
    /// 秘密代码检测
    /// </summary>
    /// <param name="itemCode"></param>
    private void CheckCode(string itemCode)
    {
        if (itemCode.Equals("Return"))
        {
            SecretCodeHandler(tempCode);
            tempCode = "";
        }
        else
        {
            tempCode += itemCode;
        }
        if (tempCode.Length > secretCodeMaxLength)
        {
            tempCode = "";
        }
    }

    /// <summary>
    /// 秘密代码处理
    /// </summary>
    /// <param name="code"></param>
    public abstract void SecretCodeHandler(string code);
}
