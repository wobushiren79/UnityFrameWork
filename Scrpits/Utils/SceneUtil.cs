using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneUtil
{

    public static void SceneChange(ScenesEnum scenenName,bool hasLoadingScene = false)
    {
        //获取当前场景名字
        string beforeSceneName = SceneManager.GetActiveScene().name;
        GameCommonInfo.ScenesChangeData.beforeScene = beforeSceneName.GetEnum<ScenesEnum>();
        GameCommonInfo.ScenesChangeData.loadingScene = scenenName;
        if (hasLoadingScene)
        {
            SceneManager.LoadScene(EnumExtension.GetEnumName(ScenesEnum.LoadingScene));
        }
        else
        {
            SceneManager.LoadScene(scenenName.GetEnumName());
        }
    }

    /// <summary>
    /// 获取当前场景
    /// </summary>
    /// <returns></returns>
    public static ScenesEnum GetCurrentScene()
    {
        //获取当前场景名字
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName.GetEnum<ScenesEnum>();
    }

}
