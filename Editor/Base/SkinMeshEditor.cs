using System.IO;
using UnityEditor;
using UnityEngine;

public class SkinMeshEditor : Editor
{
    /// <summary>
    /// 创建mesh数据
    /// </summary>
    public static void CreateCopyMesh(string fbxPath, string savePath)
    {
        //获取老方块
        FileInfo[] files = FileUtil.GetFilesByPath($"{fbxPath}");
        LogUtil.Log($"共有 {files.Length} 个待处理文件");
        foreach (var itemFile in files)
        {
            if (itemFile.Name.Contains(".meta"))
                continue;

            Mesh fbxMesh = EditorUtil.GetAssetByPath<Mesh>($"{fbxPath}/{itemFile.Name}");
            if (fbxMesh == null)
            {
                continue;
            }
            LogUtil.Log($"{fbxMesh.name}");
            LogUtil.Log($"{fbxMesh.boneWeights.Length}");
            //保存mesh
            string savePathName = $"{savePath}/{fbxMesh.name}.asset";
            Mesh targetMesh = EditorUtil.GetAssetByPath<Mesh>(savePathName);
            bool hasOld = false;
            if (targetMesh != null)
            {
                hasOld = true;
            }
            else
            {
                targetMesh = new Mesh();
            }
            ////获取原始mesh数据
            targetMesh.name = fbxMesh.name;
            targetMesh.SetVertices(fbxMesh.vertices);
            targetMesh.SetTriangles(fbxMesh.triangles, 0);
            targetMesh.SetUVs(0, fbxMesh.uv);
            targetMesh.RecalculateBounds();
            targetMesh.RecalculateNormals();
            targetMesh.bindposes = fbxMesh.bindposes;
            targetMesh.boneWeights = fbxMesh.boneWeights;

            if (hasOld)
            {
                EditorUtil.SaveAsset(targetMesh);
            }
            else
            {
                EditorUtil.CreateAsset(targetMesh, savePathName);
            }
        }

        EditorUtil.RefreshAsset();
    }
}