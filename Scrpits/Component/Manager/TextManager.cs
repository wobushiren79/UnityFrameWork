using System.Collections;
using UnityEngine;

public class TextManager : BaseManager
{
    public UITextController controllerForText;

    private void Awake()
    {
        controllerForText = new UITextController(this, null);
        controllerForText.GetAllData();
    }

    /// <summary>
    /// 通过ID获取文本
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public string GetTextById(long id)
    {
       return controllerForText.GetTextById(id);
    }

}