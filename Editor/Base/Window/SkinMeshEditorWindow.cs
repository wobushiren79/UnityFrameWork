using UnityEditor;
using UnityEngine;

public class SkinMeshEditorWindow : EditorWindow
{
    [MenuItem("Custom/工具弹窗/创建蒙皮Mesh")]
    public static void Open()
    {
        EditorWindow.GetWindow(typeof(SkinMeshEditorWindow));
    }

    protected Vector2 scrollPosition;

    public string excelFolderPath = "";

    private void OnEnable()
    {
        excelFolderPath = "Assets";
    }

    public void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginVertical();

        UIForBase();

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }


    /// <summary>
    /// 基础
    /// </summary>
    public void UIForBase()
    {
        GUILayout.BeginHorizontal();
        if (EditorUI.GUIButton("选择生成新mesh所在文件夹", 300))
        {
            excelFolderPath = EditorUI.GetFolderPanel("选择目录");
            excelFolderPath = "Assets/"+ excelFolderPath.Replace(Application.dataPath, "");
        }
        GUILayout.EndHorizontal();
        excelFolderPath = EditorUI.GUIEditorText(excelFolderPath, 500);
        GUILayout.Space(10);

        if (EditorUI.GUIButton("选中mesh生成蒙皮数据", 300))
        {
            CreateSkinMesh();
        }
    }

    public void CreateSkinMesh()
    {
        GameObject[] objList = Selection.gameObjects;
        LogUtil.Log($"共选中 {objList.Length} 个物体");
        for (int i = 0; i < objList.Length; i++)
        {
            var itemObj = objList[i];
            //获取原始mesh数据
            Mesh oldMesh = itemObj.GetComponentInChildren<MeshFilter>().sharedMesh;
            BoneWeight[] newBoneWeight = new BoneWeight[oldMesh.vertices.Length];
            //Matrix4x4[] newBind = new Matrix4x4[oldMesh.vertices.Length];

            Mesh newMesh = new Mesh();
            if (newMesh == null)
            {
                newMesh = new Mesh();
            }
            newMesh.name = itemObj.name;
            //newMesh.bindposes
            newMesh.boneWeights = newBoneWeight;
            newMesh.SetVertices(oldMesh.vertices);
            newMesh.SetTriangles(oldMesh.triangles, 0);
            newMesh.SetUVs(0, oldMesh.uv);
            newMesh.RecalculateBounds();
            newMesh.RecalculateNormals();
            //保存mesh
            string pathMesh = $"{excelFolderPath}/{newMesh.name}.asset";
            EditorUtil.CreateAsset(newMesh, pathMesh);
        }
        EditorUtil.RefreshAsset();
    }
}