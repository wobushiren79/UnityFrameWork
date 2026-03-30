using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMonoBehaviour : MonoBehaviour
{
    /// <summary>
    /// 实例化一个物体
    /// </summary>
    /// <param name="objContent"></param>
    /// <param name="objModel"></param>
    /// <returns></returns>
    public GameObject Instantiate(GameObject objContent, GameObject objModel)
    {
        GameObject objItem = Instantiate(objModel, objContent.transform);
        objItem.SetActive(true);
        return objItem;
    }

    /// <summary>
    /// 实例化一个物体
    /// </summary>
    /// <param name="objContent"></param>
    /// <param name="objModel"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public GameObject Instantiate(GameObject objContent, GameObject objModel, Vector3 position)
    {
        GameObject objItem = Instantiate(objModel, objContent.transform);
        objItem.SetActive(true);
        objItem.transform.position = position;
        return objItem;
    }

    /// <summary>
    /// 查找数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tagType"></param>
    /// <returns></returns>
    //public T FindObjectOfType<T>(GameObjectTagEnum tagType)
    //{
    //    GameObject objsFind=  GameObject.FindGameObjectWithTag(EnumExtension.GetEnumName(tagType));
    //    return objsFind.GetComponent<T>();
    //}


    /// <summary>
    /// 查找数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="importantType"></param>
    /// <returns></returns>
    public T Find<T>(string name)
    {
        GameObject objFind = GameObject.Find(name);
        if (objFind == null)
        {
            return default;
        }
        else
        {
            return objFind.GetComponent<T>();
        }
    }

    public Component Find(string name, Type type)
    {
        GameObject objFind = GameObject.Find(name);
        if (objFind == null)
        {
            return default;
        }
        else
        {
            return objFind.GetComponent(type);
        }
    }
    public T FindInChildren<T>(string name)
    {
        GameObject objFind = GameObject.Find(name);
        if (objFind == null)
        {
            return default;
        }
        else
        {
            for (int i = 0; i < objFind.transform.childCount; i++)
            {
                T data = objFind.transform.GetChild(i).GetComponent<T>();
                if (data != null)
                    return data;
            }
            return default;
        }
    }

    /// <summary>
    /// 通过标签找到物体（该方法不能找到隐藏的物体）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tag"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public T FindWithTag<T>(string tag, string name = null)
    {
        GameObject[] objArray = GameObject.FindGameObjectsWithTag(tag);
        if (objArray.IsNull())
        {
            return default(T);
        }
        for (int i = 0; i < objArray.Length; i++)
        {
            GameObject itemObj = objArray[i];
            if (name != null && !itemObj.name.Equals(name))
                continue;
            T data = itemObj.GetComponent<T>();
            if (data != null)
                return data;
        }
        return default(T);
    }

    public List<T> FindListWithTag<T>(string tag)
    {
        List<T> listData = new List<T>();
        GameObject[] objArray = GameObject.FindGameObjectsWithTag(tag);
        if (objArray.IsNull())
        {
            return listData;
        }
        for (int i = 0; i < objArray.Length; i++)
        {
            GameObject itemObj = objArray[i];
            T itemCpt = itemObj.GetComponent<T>();
            listData.Add(itemCpt);
        }
        return listData;
    }

    /// <summary>
    /// 通过反射链接UI控件
    /// </summary>
    public void AutoLinkUI()
    {
        ReflexUtil.AutoLinkDataForChild(this, "ui_");
    }
}
