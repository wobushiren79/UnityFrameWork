using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public class MeshDataDetailsCustom
{
    public Vector3[] vertices;
    public Vector2[] uv;
    public int[] triangles;

    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="size">大小</param>
    /// <param name="offset">偏移</param>
    public MeshDataDetailsCustom(Mesh mesh, float size, Vector3 offset, Vector3 rotate)
    {
        vertices = mesh.vertices;
        uv = mesh.uv;
        triangles = mesh.triangles;
        vertices = VectorUtil.GetRotatedPosition(Vector3.zero, vertices, rotate);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 itemVer = vertices[i];

            Vector3 newVer = itemVer * size + offset;
            ////保留小数点后5位
            //float newFX = float.Parse(newVer.x.ToString("f5"));
            //float newFY = float.Parse(newVer.y.ToString("f5"));
            //float newFZ = float.Parse(newVer.z.ToString("f5"));
            //newVer = new Vector3(newFX, newFY, newFZ);
            vertices[i] = newVer;
        }
        //for (int i = 0; i < uv.Length; i++)
        //{
        //    Vector2 itemUV = uv[i];
        //    //保留小数点后5位
        //    float newFX = float.Parse(itemUV.x.ToString("f5"));
        //    float newFY = float.Parse(itemUV.y.ToString("f5"));
        //    itemUV = new Vector2(newFX, newFY);
        //    uv[i] = itemUV;
        //}
    }
    public MeshDataDetailsCustom(float size, Vector3 offset, Vector3 rotate)
    {
        vertices = new Vector3[0];
        uv = new Vector2[0];
        triangles = new int[0];
        vertices = VectorUtil.GetRotatedPosition(Vector3.zero, vertices, rotate);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 itemVer = vertices[i];
            Vector3 newVer = itemVer * size + offset;
            vertices[i] = newVer;
        }
    }


    public Mesh GetMesh()
    {
        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

}