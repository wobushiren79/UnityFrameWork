using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 渐变归一化 UV 网格修改器。
/// 把当前 Graphic(Image/RawImage 等) 整个矩形的 0~1 归一化坐标写入顶点的 UV1(TEXCOORD1)，
/// 供 FrameWork/UI/Shader_UI_ImageGradient 取作渐变坐标，从而规避图集子矩形 / 9-slice 造成的
/// sprite 贴图 UV 压缩问题。会自动给所在 Canvas 开启 TexCoord1 附加通道，否则 UV1 会被剔除。
/// </summary>
[RequireComponent(typeof(Graphic))]
public class UIGradientMeshUV : BaseMeshEffect
{
    #region 生命周期
    /// <summary>启用时确保 Canvas 输出 TexCoord1 通道，并刷新一次网格</summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureCanvasTexCoord1();
        if (graphic != null)
            graphic.SetVerticesDirty();
    }
    #endregion

    #region 网格修改
    /// <summary>
    /// 按所有顶点的包围盒把位置归一化为 0~1，写入每个顶点的 UV1
    /// </summary>
    /// <param name="vh">UGUI 顶点助手</param>
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        int count = vh.currentVertCount;
        UIVertex vertex = default;

        //第一遍：求顶点包围盒
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            Vector3 p = vertex.position;
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        //防止除零(退化为一个点/一条线时)
        float width = Mathf.Max(maxX - minX, 1e-5f);
        float height = Mathf.Max(maxY - minY, 1e-5f);

        //第二遍：写入归一化 UV1
        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            Vector3 p = vertex.position;
            float u = (p.x - minX) / width;
            float v = (p.y - minY) / height;
            vertex.uv1 = new Vector4(u, v, 0, 0);
            vh.SetUIVertex(vertex, i);
        }
    }
    #endregion

    #region 私有方法
    /// <summary>给所在(根)Canvas 追加 TexCoord1 附加通道，保证 UV1 不被 UGUI 剔除</summary>
    private void EnsureCanvasTexCoord1()
    {
        Canvas canvas = (graphic != null && graphic.canvas != null)
            ? graphic.canvas
            : GetComponentInParent<Canvas>();
        if (canvas == null)
            return;
        Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
        rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
    }
    #endregion
}
