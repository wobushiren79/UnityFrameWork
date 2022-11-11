using UnityEngine;
using UnityEngine.UI;

public class BaseUIHandler<T, M> : BaseHandler<T, M>
    where M : BaseManager
    where T : BaseMonoBehaviour
{

    /// <summary>
    /// 修改UI的大小
    /// </summary>
    /// <param name="size"></param>
    public void ChangeUISize(float size)
    {
        CanvasScaler[] listCanvasScaler = gameObject.GetComponentsInChildren<CanvasScaler>();
        for (int i = 0; i < listCanvasScaler.Length; i++)
        {
            CanvasScaler canvasScaler = listCanvasScaler[i];
            canvasScaler.referenceResolution = new Vector2(1920 / size, 1080 / size);
        }
    }
}