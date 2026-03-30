using System.Collections.Generic;
using UnityEngine;

public class MeshUtil : ScriptableObject
{
    public class MeshUtilData
    {
        public Sprite sprite;
        public Texture2D targetTex;
        public int alphaTheshold = 0;
        public float depth;
        public Color32[] colors;
        public int width;
        public int height;

        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector2> texCoords = new List<Vector2>();
        public Color colorMesh;
        public MeshUtilData(Sprite sprite, Color colorAdd, float depth = 0.0625f) : this(TextureUtil.SpriteToTexture2D(sprite), colorAdd, depth)
        {

        }

        public MeshUtilData(Texture2D targetTex, Color colorAdd, float depth = 0.0625f)
        {
            this.targetTex = targetTex;
            this.depth = depth;
            colors = targetTex.GetPixels32();
            width = targetTex.width;
            height = targetTex.height;
            colorMesh = colorAdd;
        }
    }

    /// <summary>
    /// 通过图片创建 mesh
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Mesh GenerateMeshPicture(MeshUtilData data)
    {
        //添加前后
        AddQuad(data,
            new Vector3(-0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), Vector3.right, Vector3.back, Vector2.zero, Vector2.one, false);
        AddQuad(data,
            new Vector3(-0.5f, -0.5f, data.depth), new Vector3(0.5f, -0.5f, data.depth), Vector3.up, Vector3.forward, Vector2.zero, Vector2.one, true);

        for (int y = 0; y < data.height; y++) // bottom to top
        {
            for (int x = 0; x < data.width; x++) // left to right
            {
                if (HasPixel(data, x, y))
                {
                    if (x == 0 || !HasPixel(data, x - 1, y))
                        AddEdge(data, x, y, DirectionEnum.Left);

                    if (x == data.width - 1 || !HasPixel(data, x + 1, y))
                        AddEdge(data, x, y, DirectionEnum.Right);

                    if (y == 0 || !HasPixel(data, x, y - 1))
                        AddEdge(data, x, y, DirectionEnum.Down);

                    if (y == data.height - 1 || !HasPixel(data, x, y + 1))
                        AddEdge(data, x, y, DirectionEnum.Up);
                }
            }
        }
        var mesh = new Mesh();
        mesh.vertices = data.vertices.ToArray();
        mesh.normals = data.normals.ToArray();
        mesh.uv = data.texCoords.ToArray();
        //设置mesh颜色
        //Color[] colorMesh = new Color[mesh.vertices.Length];
        //for (int i = 0; i < colorMesh.Length; i++)
        //{
        //    colorMesh[i] = data.colorMesh;
        //}
        //mesh.colors = colorMesh;

        int[] quads = new int[data.vertices.Count];
        for (int i = 0; i < quads.Length; i++)
            quads[i] = i;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);
        return mesh;
    }

    protected static bool HasPixel(MeshUtilData data, int aX, int aY)
    {
        return data.colors[aX + aY * data.width].a > data.alphaTheshold;
    }

    /// <summary>
    /// 增加面
    /// </summary>
    protected static void AddEdge(MeshUtilData data, int aX, int aY, DirectionEnum direction)
    {
        Vector2 size = new Vector2(1.0f / data.width, 1.0f / data.height);
        Vector2 uv = new Vector3(aX * size.x, aY * size.y);
        Vector2 P = uv - Vector2.one * 0.5f;
        uv += size * 0.5f;
        Vector2 P2 = P;
        Vector3 normal;
        if (direction == DirectionEnum.Up)
        {
            P += size;
            P2.y += size.y;
            normal = Vector3.up;
        }
        else if (direction == DirectionEnum.Left)
        {
            P.y += size.y;
            normal = Vector3.left;
        }
        else if (direction == DirectionEnum.Down)
        {
            P2.x += size.x;
            normal = Vector3.down;
        }
        else
        {
            P2 += size;
            P.x += size.x;
            normal = Vector3.right;
        }
        AddQuad(data, P, P2, Vector3.forward * data.depth, normal, uv, uv, false);
    }

    /// <summary>
    /// 增加面
    /// </summary>
    protected static void AddQuad(
        MeshUtilData data,
        Vector3 aFirstEdgeP1, Vector3 aFirstEdgeP2, Vector3 aSecondRelative, Vector3 aNormal, Vector2 aUV1, Vector2 aUV2, bool aFlipUVs)
    {
        data.vertices.Add(aFirstEdgeP1);
        data.vertices.Add(aFirstEdgeP2);
        data.vertices.Add(aFirstEdgeP2 + aSecondRelative);
        data.vertices.Add(aFirstEdgeP1 + aSecondRelative);
        data.normals.Add(aNormal);
        data.normals.Add(aNormal);
        data.normals.Add(aNormal);
        data.normals.Add(aNormal);
        if (aFlipUVs)
        {
            data.texCoords.Add(new Vector2(aUV1.x, aUV1.y));
            data.texCoords.Add(new Vector2(aUV2.x, aUV1.y));
            data.texCoords.Add(new Vector2(aUV2.x, aUV2.y));
            data.texCoords.Add(new Vector2(aUV1.x, aUV2.y));
        }
        else
        {
            data.texCoords.Add(new Vector2(aUV1.x, aUV1.y));
            data.texCoords.Add(new Vector2(aUV1.x, aUV2.y));
            data.texCoords.Add(new Vector2(aUV2.x, aUV2.y));
            data.texCoords.Add(new Vector2(aUV2.x, aUV1.y));
        }
    }
}