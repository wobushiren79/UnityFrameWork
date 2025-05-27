using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class GameObjectExtension
{
    /// <summary>
    /// 异步实例化
    /// </summary>
    public static void InstantiateAsync(this GameObject selfGameObject, Transform tfParent, Action<GameObject> actionForComplete)
    {
        InstantiateAsync(selfGameObject, tfParent, 1, (gameobjects) =>
        {
            if (gameobjects.IsNull())
            {
                actionForComplete?.Invoke(null);
            }
            else
            {
                actionForComplete?.Invoke(gameobjects[0]);
            }
        });
    }
    
    /// <summary>
    /// 异步实例化-多个
    /// </summary>
    public async static void InstantiateAsync(this GameObject selfGameObject, Transform tfParent, int num, Action<GameObject[]> actionForComplete)
    {
        AsyncInstantiateOperation<GameObject> instantiateOperation = GameObject.InstantiateAsync<GameObject>(selfGameObject, num, tfParent);
        // 等待实例化完成
        await instantiateOperation;
        GameObject[] targetGameObjects = instantiateOperation.Result;
        actionForComplete?.Invoke(targetGameObjects);
    }

    public static GameObject Instantiate(this GameObject selfGameObject, Transform tfParent)
    {
        GameObject targetGameObject = GameObject.Instantiate(selfGameObject, tfParent);
        return targetGameObject;
    }

    public static GameObject Instantiate(this GameObject selfComponent, Transform tfParent, Vector3 startPosition, Vector3 startAngle)
    {
        GameObject targetComponent = Instantiate(selfComponent, tfParent);
        targetComponent.transform.position = startPosition;
        targetComponent.transform.eulerAngles = startAngle;
        return targetComponent;
    }

    public static T Instantiate<T>(this T selfComponent, Transform tfParent) where T : Component
    {
        T targetComponent = GameObject.Instantiate(selfComponent, tfParent);
        return targetComponent;
    }

    public static T Instantiate<T>(this T selfComponent, Transform tfParent, Vector3 startPosition, Vector3 startAngle) where T : Component
    {
        T targetComponent = Instantiate(selfComponent, tfParent);
        targetComponent.transform.position = startPosition;
        targetComponent.transform.eulerAngles = startAngle;
        return targetComponent;
    }


}
