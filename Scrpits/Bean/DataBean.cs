using UnityEngine;
using UnityEditor;

public class DataBean<E>
{
    public E dataType;
    public string data;

    public DataBean(E dataType, string data)
    {
        this.dataType = dataType;
        this.data = data;
    }
}