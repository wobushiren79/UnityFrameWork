using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public class MeshDataDetailsCustom
{
    [SerializeField]
    public Color[] texColor;
    public Vector3[] vertices;
    public Vector2[] uv;
    public int[] triangles;

    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="size">大小</param>
    /// <param name="offset">偏移</param>
    public MeshDataDetailsCustom(Mesh mesh, float size, Vector3 offset, Vector3 rotate, Texture2D tex = null)
    {
        vertices = mesh.vertices;
        uv = mesh.uv;
        triangles = mesh.triangles;
        vertices = VectorUtil.GetRotatedPosition(Vector3.zero, vertices, rotate);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 itemVer = vertices[i];
            Vector3 newVer = itemVer * size + offset;
            vertices[i] = newVer;
        }
        if (tex != null)
        {
            texColor = new Color[uv.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                Vector2 uvItem = uv[i];
                int pixX = (int)(uvItem.x / (1f / tex.width));
                int pixY = (int)(uvItem.y / (1f / tex.height));
                Color colorItem = tex.GetPixel(pixX, pixY);
                texColor[i] = colorItem;
            }
        }
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