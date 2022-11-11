using UnityEngine;
using UnityEditor;

public interface ISteamWorkshopUpdate 
{

    /// <summary>
    ///  创建item
    /// </summary>
    /// <param name="updateBean">上传数据</param>
    /// <param name="callBack">返回</param>
    void CreateWorkshopItem(SteamWorkshopUpdateBean updateBean,ISteamWorkshopUpdateCallBack callBack);
}