using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
[CustomEditor(typeof(BaseUIView), true)]
public class InspectorBaseUIView : InspectorBaseUIComponent
{
    ///// <summary>
    ///// 获取创建脚本名字
    ///// </summary>
    ///// <param name="objSelect"></param>
    ///// <returns></returns>
    //public override string GetCreateScriptFileName(GameObject objSelect)
    //{
    //    string fileName = objSelect.name + classSuffix;
    //    return fileName;
    //}

    ///// <summary>
    ///// 获取当前脚本名字
    ///// </summary>
    ///// <param name="objSelect"></param>
    ///// <returns></returns>
    //public override string GetCurrentScriptFileName(GameObject objSelect)
    //{
    //    string fileName =  objSelect.name;
    //    return fileName;
    //}
}