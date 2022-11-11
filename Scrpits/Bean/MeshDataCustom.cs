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

    public Vector3 colliderSize;

    public MeshDataCustom(Collider collider, Mesh mesh, float size, Vector3 offset, Vector3 rotate)
    {
        mainMeshData = new MeshDataDetailsCustom(mesh, size, offset, rotate);
        InitMeshCollider(collider);
    }

    public MeshDataCustom(Collider collider, float size, Vector3 offset, Vector3 rotate)
    {
        mainMeshData = new MeshDataDetailsCustom(size, offset, rotate);
        InitMeshCollider(collider);
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
    public void InitMeshCollider(Collider collider)
    {
        Mesh meshCollider = GetColliderMesh(collider);
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
    public Mesh GetColliderMesh(Collider collider)
    {
        Mesh mesh = new Mesh();
        if (collider is BoxCollider boxCollider)
        {
            Vector3 size = boxCollider.size;
            Vector3 center = boxCollider.center;
            mesh.vertices = new Vector3[]
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
            mesh.triangles = new int[]
            {
                0,2,1, 0,3,2,//左
                4,5,6, 4,6,7,//右
                2,6,5, 2,5,1,//上
                0,4,7, 0,7,3,//下
                0,1,5, 0,5,4,//前
                3,6,2, 3,7,6 //后
            };
            colliderSize = size;
        }
        return mesh;
    }


}