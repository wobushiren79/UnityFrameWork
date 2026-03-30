using UnityEngine;
using UnityEditor;
using System;

[Serializable]
public class Vector3Bean 
{
    public float x;
    public float y;
    public float z;

    public Vector3Bean(Vector3 vector)
    {
        this.x = vector.x;
        this.y = vector.y;
        this.z = vector.z;
    }
    public Vector3Bean(float x,float y)
    {
        this.x = x;
        this.y = y;
        this.z = 0;
    }

    public Vector3Bean(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z =z;
    }

    public Vector3 GetVector3()
    {
        return this.ToVector3();
    }
}