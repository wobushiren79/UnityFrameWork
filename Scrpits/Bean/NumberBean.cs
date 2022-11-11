using UnityEditor;
using UnityEngine;
using System;

[Serializable]
public class NumberBean 
{
    public long id;
    public long number;
    
    public NumberBean()
    {
        this.id = 0;
        this.number = 1;
    }

    public NumberBean(long id, long number)
    {
        this.id = id;
        this.number = number;
    }

    public NumberBean(long id)
    {
        this.id = id;
        this.number = 1;
    }
}