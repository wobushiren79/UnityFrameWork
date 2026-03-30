using UnityEngine;
using UnityEditor;
using System;

[Serializable]
public class Vector3IntBean
{
    public int x;
    public int y;
    public int z;

    public Vector3IntBean(Vector3Int vector)
    {
        this.x = vector.x;
        this.y = vector.y;
        this.z = vector.z;
    }
    public Vector3IntBean(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.z = 0;
    }

    public Vector3IntBean(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3Int GetVector3Int()
    {
        return new Vector3Int(x, y, z);
    }
}