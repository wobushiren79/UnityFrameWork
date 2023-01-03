using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class MeshDataCustom
{
    public MeshDataDetailsCustom mainMeshData;
    public MeshDataDetailsCustom[] otherMeshData;

    public Vector3[] verticesCollider;
    public int[] trianglesCollider;

    public MeshDataCustom(Collider[] colliderList, Mesh mesh, float size, Vector3 offset, Vector3 rotate)
    {
        mainMeshData = new MeshDataDetailsCustom(mesh, size, offset, rotate);
        InitMeshCollider(colliderList);
    }

    public MeshDataCustom(Collider[] colliderList, float size, Vector3 offset, Vector3 rotate)
    {
        mainMeshData = new MeshDataDetailsCustom(size, offset, rotate);
        InitMeshCollider(colliderList);
    }

    /// <summary>
    /// 设置其余的meshdata
    /// </summary>
    /// <param name="listMesh"></param>
    /// <param name="listSize"></param>
    /// <param name="listOffset"></param>
    public void SetOtherMeshData(List<Mesh> listMesh, List<float> listSize, List<Vector3> listOffset, List<Vector3> listRotate)
    {
        otherMeshData = new MeshDataDetailsCustom[listMesh.Count];
        for (int i = 0; i < listMesh.Count; i++)
        {
            Mesh itemMesh = listMesh[i];
            float itemSize = listSize[i];
            Vector3 itemOffset = listOffset[i];
            Vector3 itemRotate = listRotate[i];
            MeshDataDetailsCustom itemMeshData = new MeshDataDetailsCustom(itemMesh, itemSize, itemOffset, itemRotate);
            otherMeshData[i] = itemMeshData;
        }
    }

    /// <summary>
    /// 初始化碰撞mesh
    /// </summary>
    /// <param name="collider"></param>
    public void InitMeshCollider(Collider[] colliderList)
    {
        Mesh meshCollider = GetColliderMesh(colliderList);
        verticesCollider = meshCollider.vertices;
        trianglesCollider = meshCollider.triangles;
    }

    /// <summary>
    /// 获取mesh
    /// </summary>
    /// <returns></returns>
    public Mesh GetMainMesh()
    {
        return mainMeshData.GetMesh();
    }

    public Mesh GetOtherMesh(int index)
    {
        return otherMeshData[index].GetMesh();
    }

    /// <summary>
    /// 获取碰撞的mesh
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public Mesh GetColliderMesh(Collider[] colliderList)
    {
        Mesh mesh = new Mesh();
        int index = 0;
        List<Vector3> listVertices = new List<Vector3>();
        List<int> listTriangles = new List<int>();
        foreach (var itemCollider in colliderList)
        {
            if (itemCollider is BoxCollider boxCollider)
            {
                Vector3 size = boxCollider.size;
                Vector3 center = boxCollider.center;
                List<Vector3> verts = new List<Vector3>
                {
                    //左面顶点
                    center + new Vector3(-size.x/2,-size.y/2,-size.z/2),
                    center + new Vector3(-size.x/2,size.y/2,-size.z/2),
                    center + new Vector3(-size.x/2,size.y/2,size.z/2),
                    center + new Vector3(-size.x/2,-size.y/2,size.z/2),

                    //右面顶点
                    center + new Vector3(size.x/2,-size.y/2,-size.z/2),
                    center + new Vector3(size.x/2,size.y/2,-size.z/2),
                    center + new Vector3(size.x/2,size.y/2,size.z/2),
                    center + new Vector3(size.x/2,-size.y/2,size.z/2),
                };
                listVertices.AddRange(verts);

                List<int> triangles = new List<int>
                {
                    0+index,2+index,1+index, 0+index,3+index,2+index,//左
                    4+index,5+index,6+index, 4+index,6+index,7+index,//右
                    2+index,6+index,5+index, 2+index,5+index,1+index,//上
                    0+index,4+index,7+index, 0+index,7+index,3+index,//下
                    0+index,1+index,5+index, 0+index,5+index,4+index,//前
                    3+index,6+index,2+index, 3+index,7+index,6+index //后
                };
                listTriangles.AddRange(triangles);
                index += 8;
            }

            //else if (itemCollider is MeshCollider meshCollider)
            //{
            //    //mesh = meshCollider.sharedMesh;
            //    mesh.vertices = meshCollider.sharedMesh.vertices;
            //    mesh.triangles = meshCollider.sharedMesh.triangles;
            //}
        }
        mesh.SetVertices(listVertices);
        mesh.SetTriangles(listTriangles,0);
        return mesh;
    }


}