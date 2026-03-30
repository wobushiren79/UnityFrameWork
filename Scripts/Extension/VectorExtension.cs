using UnityEditor;
using UnityEngine;

public static class VectorExtension
{
    //------------Transform------------
    public static void AddPositionXYZ(this Transform self, float x, float y, float z)
    {
        self.position = self.position.AddXYZ(x, y, z);
    }

    public static void AddPositionXY(this Transform self, float x, float y)
    {
        self.position = self.position.AddXY(x, y);
    }

    public static void AddPositionXZ(this Transform self, float x, float z)
    {
        self.position = self.position.AddXZ(x, z);
    }

    public static void AddPositionYZ(this Transform self, float y, float z)
    {
        self.position = self.position.AddYZ(y, z);
    }

    public static void AddPositionX(this Transform self, float x)
    {
        self.position = self.position.AddX(x);
    }

    public static void AddPositionY(this Transform self, float y)
    {
        self.position = self.position.AddY(y);
    }

    public static void AddPositionZ(this Transform self, float z)
    {
        self.position = self.position.AddZ(z);
    }

    public static void SetPositionXYZ(this Transform self, float x, float y, float z)
    {
        self.position = new Vector3(x, y, z);
    }

    public static void SetPositionXY(this Transform self, float x, float y)
    {
        self.SetPositionXYZ(x, y, self.position.z);
    }

    public static void SetPositionXZ(this Transform self, float x, float z)
    {
        self.SetPositionXYZ(x, self.position.y, z);
    }

    public static void SetPositionYZ(this Transform self, float y, float z)
    {
        self.SetPositionXYZ(self.position.x, y, z);
    }

    public static void SetPositionX(this Transform self, float x)
    {
        self.SetPositionXYZ(x, self.position.y, self.position.z);
    }

    public static void SetPositionY(this Transform self, float y)
    {
        self.SetPositionXYZ(self.position.x, y, self.position.z);
    }

    public static void SetPositionZ(this Transform self, float z)
    {
        self.SetPositionXYZ(self.position.x, self.position.y, z);
    }

    //-------Vector2--------
    public static Vector2 AddX(this Vector2 self, float add)
    {
        self.x += add;
        return self;
    }
    public static Vector2 AddY(this Vector2 self, float add)
    {
        self.y += add;
        return self;
    }
    public static Vector2 AddXY(this Vector2 self, float x, float y)
    {
        self.x += x;
        self.y += y;
        return self;
    }

    //-------Vector3--------

    public static Vector3 MultiplyX(this Vector3 self, float multiply)
    {
        self.x *= multiply;
        return self;
    }
    public static Vector3 MultiplyY(this Vector3 self, float multiply)
    {
        self.y *= multiply;
        return self;
    }
    public static Vector3 MultiplyZ(this Vector3 self, float multiply)
    {
        self.z *= multiply;
        return self;
    }

    public static Vector3 AddX(this Vector3 self, float add)
    {
        self.x += add;
        return self;
    }
    public static Vector3 AddY(this Vector3 self, float add)
    {
        self.y += add;
        return self;
    }
    public static Vector3 AddZ(this Vector3 self, float add)
    {
        self.z += add;
        return self;
    }
    public static Vector3 AddXY(this Vector3 self, float x, float y)
    {
        self.x += x;
        self.y += y;
        return self;
    }
    public static Vector3 AddXZ(this Vector3 self, float x, float z)
    {
        self.x += x;
        self.z += z;
        return self;
    }
    public static Vector3 AddYZ(this Vector3 self, float y, float z)
    {
        self.y += y;
        self.z += z;
        return self;
    }
    public static Vector3 AddXYZ(this Vector3 self, float x, float y, float z)
    {
        self.x += x;
        self.y += y;
        self.z += z;
        return self;
    }

    public static Vector3 SetX(this Vector3 self, float data)
    {
        self.x = data;
        return self;
    }
    public static Vector3 SetY(this Vector3 self, float data)
    {
        self.y = data;
        return self;
    }
    public static Vector3 SetZ(this Vector3 self, float data)
    {
        self.z = data;
        return self;
    }
    public static Vector3 SetXY(this Vector3 self, float x, float y)
    {
        self.x = x;
        self.y = y;
        return self;
    }
    public static Vector3 SetXZ(this Vector3 self, float x, float z)
    {
        self.x = x;
        self.z = z;
        return self;
    }
    public static Vector3 SetYZ(this Vector3 self, float y, float z)
    {
        self.y = y;
        self.z = z;
        return self;
    }
    public static Vector3 SetXYZ(this Vector3 self, float x, float y, float z)
    {
        self.x = x;
        self.y = y;
        self.z = z;
        return self;
    }
    //-------Vector2Int--------
    public static Vector2Int AddX(this Vector2Int self, int add)
    {
        self.x += add;
        return self;
    }
    public static Vector2Int AddY(this Vector2Int self, int add)
    {
        self.y += add;
        return self;
    }
    public static Vector2Int AddXY(this Vector2Int self, int x, int y)
    {
        self.x += x;
        self.y += y;
        return self;
    }

    //-------Vector3Int--------
    public static Vector3Int AddX(this Vector3Int self, int add)
    {
        self.x += add;
        return self;
    }
    public static Vector3Int AddY(this Vector3Int self, int add)
    {
        self.y += add;
        return self;
    }
    public static Vector3Int AddZ(this Vector3Int self, int add)
    {
        self.z += add;
        return self;
    }
    public static Vector3Int AddXY(this Vector3Int self, int x, int y)
    {
        self.x += x;
        self.y += y;
        return self;
    }
    public static Vector3Int AddXZ(this Vector3Int self, int x, int z)
    {
        self.x += x;
        self.z += z;
        return self;
    }
    public static Vector3Int AddYZ(this Vector3Int self, int y, int z)
    {
        self.y += y;
        self.z += z;
        return self;
    }
    public static Vector3Int AddXYZ(this Vector3Int self, int x, int y, int z)
    {
        self.x += x;
        self.y += y;
        self.z += z;
        return self;
    }

    //-------Vector3[]--------
    public static Vector3[] MultiplyX(this Vector3[] self, float multiply)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].MultiplyX(multiply);
        }
        return newSelf;
    }

    public static Vector3[] MultiplyY(this Vector3[] self, float multiply)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].MultiplyY(multiply);
        }
        return newSelf;
    }

    public static Vector3[] MultiplyZ(this Vector3[] self, float multiply)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].MultiplyZ(multiply);
        }
        return newSelf;
    }

    public static Vector3[] AddX(this Vector3[] self, float add)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].AddX(add);
        }
        return newSelf;
    }

    public static Vector3[] AddY(this Vector3[] self, float add)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].AddY(add);
        }
        return newSelf;
    }

    public static Vector3[] AddZ(this Vector3[] self, float add)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].AddZ(add);
        }
        return newSelf;
    }


    public static Vector3[] AddXY(this Vector3[] self, float x, float y)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].AddXY(x, y);
        }
        return newSelf;
    }

    public static Vector3[] AddXZ(this Vector3[] self, float x, float z)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].AddXZ(x, z);
        }
        return newSelf;
    }

    public static Vector3[] AddYZ(this Vector3[] self, float y, float z)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].AddYZ(y, z);
        }
        return newSelf;
    }

    public static Vector3[] AddXYZ(this Vector3[] self, float x, float y, float z)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].AddXYZ(x, y, z);
        }
        return newSelf;
    }

    public static Vector3[] SetX(this Vector3[] self, float data)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].SetX(data);
        }
        return newSelf;
    }

    public static Vector3[] SetY(this Vector3[] self, float data)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].SetY(data);
        }
        return newSelf;
    }

    public static Vector3[] SetZ(this Vector3[] self, float data)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].SetZ(data);
        }
        return newSelf;
    }


    public static Vector3[] SetXY(this Vector3[] self, float x, float y)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].SetXY(x, y);
        }
        return newSelf;
    }

    public static Vector3[] SetXZ(this Vector3[] self, float x, float z)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].SetXZ(x, z);
        }
        return newSelf;
    }

    public static Vector3[] SetYZ(this Vector3[] self, float y, float z)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].SetYZ(y, z);
        }
        return newSelf;
    }

    public static Vector3[] SetXYZ(this Vector3[] self, float x, float y, float z)
    {
        Vector3[] newSelf = new Vector3[self.Length];
        for (int i = 0; i < self.Length; i++)
        {
            newSelf[i] = self[i].SetXYZ(x, y, z);
        }
        return newSelf;
    }
}