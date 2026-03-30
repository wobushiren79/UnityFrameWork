using System;
using System.Collections.Generic;
using UnityEngine;

public static class ComponentExtension
{

    /// <summary>
    /// 获取包含名字的控件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="name"></param>
    /// <param name="includeInactive"></param>
    /// <returns></returns>
    public static List<T> GetComponentsInChildrenContainsName<T>(this T selfComponent, string name, bool includeInactive = false) where T : Component
    {
        List<T> listData = new List<T>();
        T[] cptList = selfComponent.GetComponentsInChildren<T>(includeInactive);
        for (int i = 0; i < cptList.Length; i++)
        {
            T item = cptList[i];
            if (item.name.Contains(name))
            {
                listData.Add(item);
            }
        }
        return listData;
    }

    /// <summary>
    /// 获取包含名字的控件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="name"></param>
    /// <param name="includeInactive"></param>
    /// <returns></returns>
    public static T GetComponentInChildrenContainsName<T>(this T selfComponent, string name, bool includeInactive = false) where T : Component
    {
        T[] cptList = selfComponent.GetComponentsInChildren<T>(includeInactive);
        for (int i = 0; i < cptList.Length; i++)
        {
            T item = cptList[i];
            if (item.name.Contains(name))
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取指定名字的控件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="name"></param>
    /// <param name="includeInactive"></param>
    /// <returns></returns>
    public static List<T> GetComponentsInChildren<T>(this T selfComponent, string name, bool includeInactive = false) where T : Component
    {
        List<T> listData = new List<T>();
        T[] cptList = selfComponent.GetComponentsInChildren<T>(includeInactive);
        for (int i = 0; i < cptList.Length; i++)
        {
            T item = cptList[i];
            if (item.name.Equals(name))
            {
                listData.Add(item);
            }
        }
        return listData;
    }

    public static List<Component> GetComponentsInChildren<T>(this T selfComponent, string name, Type type, bool includeInactive = false) where T : Component
    {
        List<Component> listData = new List<Component>();
        Component[] cptList = selfComponent.GetComponentsInChildren(type, includeInactive);
        for (int i = 0; i < cptList.Length; i++)
        {
            Component item = cptList[i];
            if (item.name.Equals(name))
            {
                listData.Add(item);
            }
        }
        return listData;
    }

    public static Component GetComponentInChildren<T>(this T selfComponent, string name, Type type, bool includeInactive = false) where T : Component
    {
        Component[] cptList = selfComponent.GetComponentsInChildren(type, includeInactive);
        for (int i = 0; i < cptList.Length; i++)
        {
            Component item = cptList[i];
            if (item.name.Equals(name))
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取指定名字的控件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="name"></param>
    /// <param name="includeInactive"></param>
    /// <returns></returns>
    public static C GetComponentInChildren<T,C>(this C selfComponent, string name, bool includeInactive = false) where T : Component where C : Component
    {
        C[] cptList = selfComponent.GetComponentsInChildren<C>(includeInactive);
        for (int i = 0; i < cptList.Length; i++)
        {
            C item = cptList[i];
            if (item.name.Equals(name))
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// 添加组件-扩展
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <returns></returns>
    public static T AddComponentEX<T>(this GameObject selfComponent)
        where T : Component
    {
        T addComponet = selfComponent.GetComponent<T>();
        if (addComponet == null)
        {
            addComponet = selfComponent.AddComponent<T>();
        }
        return addComponet;
    }
    public static T AddComponentEX<T>(this Transform selfComponent)
    where T : Component
    {
        T addComponet = selfComponent.GetComponent<T>();
        if (addComponet == null)
        {
            addComponet = selfComponent.gameObject.AddComponent<T>();
        }
        return addComponet;
    }

    /// <summary>
    /// 获取子物体数量
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="isActive"></param>
    /// <returns></returns>
    public static int GetChildCount<T>(this T selfComponent, bool isActive) where T : Component
    {
        int number = 0;
        for (int i = 0; i < selfComponent.transform.childCount; i++)
        {
            if (selfComponent.transform.GetChild(i).gameObject.activeSelf == isActive)
            {
                number++;
            }
        }
        return number;
    }

    /// <summary>
    /// 获取子物体数量
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <returns></returns>
    public static int GetChildCount<T>(this T selfComponent) where T : Component
    {
        return selfComponent.transform.childCount;
    }

    /// <summary>
    /// 删除包含名字的物体
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="name"></param>
    /// <param name="mode">0:Destroy 1:DestroyImmediate</param>
    /// <returns></returns>
    public static T DestoryChildContainsName<T>(this T selfComponent, string name, int mode = 0) where T : Component
    {
        for (int i = 0; i < selfComponent.transform.childCount; i++)
        {
            Transform childTF = selfComponent.transform.GetChild(i);
            if (childTF.name.Contains(name))
            {
                if (mode == 1)
                {
                    GameObject.DestroyImmediate(childTF.gameObject);
                    i--;
                }
                else
                {
                    GameObject.Destroy(childTF.gameObject);
                }
            }
        }
        return selfComponent;
    }

    /// <summary>
    ///  删除包含名字的物体
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="name"></param>
    /// <param name="isActive"></param>
    /// <param name="mode">0:Destroy 1:DestroyImmediate</param>
    /// <returns></returns>
    public static T DestoryChildContainsName<T>(this T selfComponent, string name, bool isActive, int mode = 0) where T : Component
    {
        for (int i = 0; i < selfComponent.transform.childCount; i++)
        {
            Transform childTF = selfComponent.transform.GetChild(i);
            if (childTF.name.Contains(name))
            {
                if (childTF.gameObject.activeSelf == isActive)
                {
                    if (mode == 1)
                    {
                        GameObject.DestroyImmediate(childTF.gameObject);
                        i--;
                    }
                    else
                    {
                        GameObject.Destroy(childTF.gameObject);
                    }
                }
            }
        }
        return selfComponent;
    }

    /// <summary>
    /// 删除同名子物体
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="name"></param>
    /// <param name="mode">0:Destroy 1:DestroyImmediate</param>
    /// <returns></returns>
    public static T DestoryChild<T>(this T selfComponent, string name, int mode = 0) where T : Component
    {
        for (int i = 0; i < selfComponent.transform.childCount; i++)
        {
            Transform childTF = selfComponent.transform.GetChild(i);
            if (childTF.name.Equals(name))
            {
                if (mode == 1)
                {
                    GameObject.DestroyImmediate(childTF.gameObject);
                    i--;
                }
                else
                {
                    GameObject.Destroy(childTF.gameObject);
                }
            }
        }
        return selfComponent;
    }

    /// <summary>
    /// 删除同名子物体
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="name"></param>
    /// <param name="isActive"></param>
    /// <param name="mode">0:Destroy 1:DestroyImmediate</param>
    /// <returns></returns>
    public static T DestoryChild<T>(this T selfComponent, string name, bool isActive, int mode = 0) where T : Component
    {
        for (int i = 0; i < selfComponent.transform.childCount; i++)
        {
            Transform childTF = selfComponent.transform.GetChild(i);
            if (childTF.name.Equals(name))
            {
                if (childTF.gameObject.activeSelf == isActive)
                {
                    if (mode == 1)
                    {
                        GameObject.DestroyImmediate(childTF.gameObject);
                        i--;
                    }
                    else
                    {
                        GameObject.Destroy(childTF.gameObject);
                    }
                }
            }
        }
        return selfComponent;
    }

    /// <summary>
    /// 删除所有子物体
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="mode">0:Destroy 1:DestroyImmediate</param>
    /// <returns></returns>
    public static T DestroyAllChild<T>(this T selfComponent, int mode = 0) where T : Component
    {
        for (int i = 0; i < selfComponent.transform.childCount; i++)
        {
            Transform childTF = selfComponent.transform.GetChild(i);
            if (mode == 1)
            {
                GameObject.DestroyImmediate(childTF.gameObject);
                i--;
            }
            else
            {
                GameObject.Destroy(childTF.gameObject);
            }
        }
        return selfComponent;
    }

    /// <summary>
    /// 删除所有子物体
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="isActive"></param>
    /// <param name="mode">0:Destroy 1:DestroyImmediate</param>
    /// <returns></returns>
    public static T DestroyAllChild<T>(this T selfComponent, bool isActive, int mode = 0) where T : Component
    {
        for (int i = 0; i < selfComponent.transform.childCount; i++)
        {
            Transform childTF = selfComponent.transform.GetChild(i);
            if (childTF.gameObject.activeSelf == isActive)
            {
                if (mode == 1)
                {
                    GameObject.DestroyImmediate(childTF.gameObject);
                    i--;
                }
                else
                {
                    GameObject.Destroy(childTF.gameObject);
                }
            }
        }
        return selfComponent;
    }

    /// <summary>
    /// 删除自己
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="mode"></param>
    public static void DestorySelf<T>(this T selfComponent, int mode = 0) where T : Component
    {
        if (mode == 1)
        {
            GameObject.DestroyImmediate(selfComponent.gameObject);
        }
        else
        {
            GameObject.Destroy(selfComponent.gameObject);
        }
    }

    /// <summary>
    /// 展示OBJ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfComponent"></param>
    /// <param name="isShow"></param>
    /// <returns></returns>
    public static T ShowObj<T>(this T selfComponent, bool isShow = true) where T : Component
    {
        selfComponent.gameObject.SetActive(isShow);
        return selfComponent;
    }

    /// <summary>
    /// 展示OBJ
    /// </summary>
    /// <param name="selfObj"></param>
    /// <param name="isShow"></param>
    /// <returns></returns>
    public static GameObject ShowObj(this GameObject selfObj, bool isShow = true)
    {
        selfObj.SetActive(isShow);
        return selfObj;
    }

    /// <summary>
    /// 查找
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selfObj"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T FindChild<T>(this GameObject selfObj, string name) where T : Component
    {
        Transform tfFind = selfObj.transform.Find(name);
        if (tfFind)
        {
            return tfFind.GetComponent<T>();
        }
        return null;
    }

    /// <summary>
    /// 给所有子物体设置层级
    /// </summary>
    public static void SetLayerAllChild(this GameObject selfObj, int layer, bool hasSelf = true)
    {
        //遍历当前物体及其所有子物体
        foreach (Transform childTF in selfObj.GetComponentsInChildren<Transform>(hasSelf))
        {
            childTF.gameObject.layer = layer;//更改物体的Layer层
        }
    }

    /// <summary>
    /// 给所有子物体设置层级
    /// </summary>
    public static void SetLayerAllChild<T>(this T selfComponent, int layer, bool hasSelf = true) where T : Component
    {
        //遍历当前物体及其所有子物体
        foreach (Transform childTF in selfComponent.GetComponentsInChildren<Transform>(hasSelf))
        {
            childTF.gameObject.layer = layer;//更改物体的Layer层
        }
    }
}