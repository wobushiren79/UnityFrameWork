using UnityEngine;
using System;

/// <summary>
/// Vector2 的可序列化封装
/// 用于规避 Newtonsoft.Json 直接序列化 UnityEngine.Vector2 时
/// 因递归访问 normalized 属性导致的栈溢出
/// </summary>
[Serializable]
public class Vector2Bean
{
    public float x;
    public float y;

    public Vector2Bean()
    {

    }

    public Vector2Bean(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2Bean(Vector2 vector)
    {
        this.x = vector.x;
        this.y = vector.y;
    }

    /// <summary>
    /// 用 Vector2 覆盖当前数据
    /// </summary>
    public void SetVector(Vector2 vector)
    {
        this.x = vector.x;
        this.y = vector.y;
    }

    /// <summary>
    /// 转换为 Unity Vector2
    /// </summary>
    public Vector2 GetVector()
    {
        return new Vector2(x, y);
    }

    public static Vector2Bean Zero()
    {
        return new Vector2Bean(0, 0);
    }
}
