using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 列表展示组件（带对象缓存机制）
/// 用于动态生成指定数量的子预制到父级UI中，支持对象复用
/// </summary>
public class ListCustomView : BaseMonoBehaviour
{
    // 子预制
    public GameObject prefab;
    // 父级UI容器
    public Transform parentContainer;
    // 是否启用缓存机制（默认启用）
    public bool enableCache = true;

    // 对象池：缓存所有创建的子对象
    private List<GameObject> itemPool = new List<GameObject>();
    // 当前正在使用的对象数量
    private int activeCount = 0;

    /// <summary>
    /// 设置列表数据（基础版本）
    /// </summary>
    /// <param name="count">需要显示的数量</param>
    /// <param name="onItemCreate">每个子对象的回调 (index, gameObject)</param>
    public void SetData(int count, Action<int, GameObject> onItemCreate)
    {
        if (prefab == null)
        {
            LogUtil.LogError("ListCustomView: Prefab is null!");
            return;
        }

        if (count < 0) count = 0;

        // 确定父容器
        Transform container = parentContainer != null ? parentContainer : transform;

        // 复用或创建对象
        for (int i = 0; i < count; i++)
        {
            GameObject item;
            if (i < itemPool.Count)
            {
                // 复用缓存的对象
                item = itemPool[i];
            }
            else
            {
                // 创建新对象
                item = Instantiate(prefab, container);
                itemPool.Add(item);
            }
            item.SetActive(true);
            onItemCreate?.Invoke(i, item);
        }

        // 隐藏多余的对象
        for (int i = count; i < itemPool.Count; i++)
        {
            itemPool[i].SetActive(false);
        }

        activeCount = count;
    }

    /// <summary>
    /// 设置列表数据（泛型版本）
    /// </summary>
    /// <typeparam name="T">子预制上挂载的组件类型</typeparam>
    /// <param name="count">需要显示的数量</param>
    /// <param name="onItemCreate">每个子对象的回调 (index, component)</param>
    public void SetData<T>(int count, Action<int, T> onItemCreate) where T : Component
    {
        SetData(count, (index, gameObject) =>
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                LogUtil.LogError($"ListCustomView: Component {typeof(T).Name} not found on prefab!");
                return;
            }
            onItemCreate?.Invoke(index, component);
        });
    }

    /// <summary>
    /// 设置列表数据（带数据列表的泛型版本）
    /// </summary>
    /// <typeparam name="T">子预制上挂载的组件类型</typeparam>
    /// <typeparam name="TData">数据类型</typeparam>
    /// <param name="dataList">数据列表</param>
    /// <param name="onItemCreate">每个子对象的回调 (data, component)</param>
    public void SetData<T, TData>(List<TData> dataList, Action<TData, T> onItemCreate)
        where T : Component
    {
        if (dataList == null)
        {
            HideAllItems();
            return;
        }

        SetData<T>(dataList.Count, (index, component) =>
        {
            onItemCreate?.Invoke(dataList[index], component);
        });
    }

    /// <summary>
    /// 设置列表数据（带数据数组的泛型版本）
    /// </summary>
    /// <typeparam name="T">子预制上挂载的组件类型</typeparam>
    /// <typeparam name="TData">数据类型</typeparam>
    /// <param name="dataArray">数据数组</param>
    /// <param name="onItemCreate">每个子对象的回调 (data, component)</param>
    public void SetData<T, TData>(TData[] dataArray, Action<TData, T> onItemCreate)
        where T : Component
    {
        if (dataArray == null)
        {
            HideAllItems();
            return;
        }

        SetData<T>(dataArray.Length, (index, component) =>
        {
            onItemCreate?.Invoke(dataArray[index], component);
        });
    }

    /// <summary>
    /// 获取指定索引的活跃子对象
    /// </summary>
    public GameObject GetItem(int index)
    {
        if (index < 0 || index >= activeCount)
        {
            return null;
        }
        return itemPool[index];
    }

    /// <summary>
    /// 获取指定索引的活跃子对象的组件
    /// </summary>
    public T GetItemComponent<T>(int index) where T : Component
    {
        GameObject item = GetItem(index);
        if (item == null) return null;
        return item.GetComponent<T>();
    }

    /// <summary>
    /// 获取当前活跃的子对象列表
    /// </summary>
    public List<GameObject> GetActiveItems()
    {
        List<GameObject> activeItems = new List<GameObject>();
        for (int i = 0; i < activeCount; i++)
        {
            if (itemPool[i] != null && itemPool[i].activeSelf)
            {
                activeItems.Add(itemPool[i]);
            }
        }
        return activeItems;
    }

    /// <summary>
    /// 获取当前活跃对象数量
    /// </summary>
    public int GetActiveCount()
    {
        return activeCount;
    }

    /// <summary>
    /// 获取对象池中总对象数量（包括隐藏的）
    /// </summary>
    public int GetPoolCount()
    {
        return itemPool.Count;
    }

    /// <summary>
    /// 刷新指定索引的项（重新触发回调）
    /// </summary>
    public void RefreshItem(int index, Action<int, GameObject> onItemRefresh)
    {
        if (index < 0 || index >= activeCount) return;
        onItemRefresh?.Invoke(index, itemPool[index]);
    }

    /// <summary>
    /// 刷新指定索引的项（泛型版本）
    /// </summary>
    public void RefreshItem<T>(int index, Action<int, T> onItemRefresh) where T : Component
    {
        RefreshItem(index, (i, gameObject) =>
        {
            T component = gameObject.GetComponent<T>();
            onItemRefresh?.Invoke(i, component);
        });
    }

    /// <summary>
    /// 刷新所有活跃的项
    /// </summary>
    public void RefreshAll(Action<int, GameObject> onItemRefresh)
    {
        for (int i = 0; i < activeCount; i++)
        {
            onItemRefresh?.Invoke(i, itemPool[i]);
        }
    }

    /// <summary>
    /// 刷新所有活跃的项（泛型版本）
    /// </summary>
    public void RefreshAll<T>(Action<int, T> onItemRefresh) where T : Component
    {
        RefreshAll((index, gameObject) =>
        {
            T component = gameObject.GetComponent<T>();
            onItemRefresh?.Invoke(index, component);
        });
    }

    /// <summary>
    /// 隐藏所有对象（不清除缓存）
    /// </summary>
    public void HideAllItems()
    {
        foreach (GameObject item in itemPool)
        {
            if (item != null)
            {
                item.SetActive(false);
            }
        }
        activeCount = 0;
    }

    /// <summary>
    /// 清理所有对象（销毁实例）
    /// </summary>
    public void ClearItems()
    {
        foreach (GameObject item in itemPool)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        itemPool.Clear();
        activeCount = 0;
    }

    /// <summary>
    /// 清理多余缓存（保留指定数量）
    /// </summary>
    /// <param name="keepCount">保留的对象数量</param>
    public void TrimPool(int keepCount)
    {
        if (keepCount < 0) keepCount = 0;
        if (keepCount >= itemPool.Count) return;

        // 确保不删除正在使用的对象
        if (keepCount < activeCount)
        {
            keepCount = activeCount;
        }

        // 销毁多余的对象
        for (int i = itemPool.Count - 1; i >= keepCount; i--)
        {
            if (itemPool[i] != null)
            {
                Destroy(itemPool[i]);
            }
            itemPool.RemoveAt(i);
        }
    }

    /// <summary>
    /// 设置子预制
    /// </summary>
    public void SetPrefab(GameObject prefab)
    {
        this.prefab = prefab;
    }

    /// <summary>
    /// 设置父级容器
    /// </summary>
    public void SetParentContainer(Transform parentContainer)
    {
        this.parentContainer = parentContainer;
    }

    private void OnDestroy()
    {
        ClearItems();
    }
}
