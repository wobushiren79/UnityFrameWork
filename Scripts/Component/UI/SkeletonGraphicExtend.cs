using Spine.Unity;
using UnityEngine;

/// <summary>
/// SkeletonGraphic 扩展：修复 RectMask2D 整体误剔除。
/// Unity 默认 Cull 只用 RectTransform 的 rect 与 mask 求交，rect 完全跑出 mask 即整体隐藏；
/// 而 spine 的 mesh 顶点以骨架原点(pivot)为基准、可大幅超出 rect（原点不居中的骨架需较大 pos 偏移摆正），
/// 导致「rect 出 mask 但内容还在 mask 内」时被误隐藏。本组件在 rect 出 mask 时改用实时 mesh
/// 包围盒精细判定：内容还在 mask 内就照常渲染（mask 外部分仍由 EnableRectClipping 精确裁剪）。
/// </summary>
[DefaultExecutionOrder(1)]
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Spine/SkeletonGraphic Extend (Unity UI Canvas)")]
public class SkeletonGraphicExtend : SkeletonGraphic
{
    #region 剔除修复
    /// <summary>按 mesh 实际范围判定剔除（默认开；关闭则退回 Unity 默认 rect 判定）</summary>
    [Tooltip("按 mesh 实际范围判定 RectMask2D 剔除；关闭则退回默认 rect 判定")]
    public bool cullByMeshBounds = true;

    /// <summary>
    /// 覆写剔除判定：rect 与 mask 相交走原版快路径；rect 出 mask 时改用 mesh 包围盒再判一次
    /// </summary>
    public override void Cull(Rect clipRect, bool validRect)
    {
        //无裁剪/开关关闭/不在 canvas 下 → 回退默认 rect 判定
        Canvas rootCanvas = canvas == null ? null : canvas.rootCanvas;
        if (!validRect || !cullByMeshBounds || rootCanvas == null)
        {
            base.Cull(clipRect, validRect);
            return;
        }
        //快路径：rect 与 mask 相交 = 正常显示，行为与原版一致直接放行（rootCanvasRect 是 internal，自算等价 rect）
        if (clipRect.Overlaps(LocalRectToCanvasRect(rootCanvas, rectTransform.rect), true))
        {
            UpdateCullByMeshBounds(false);
            return;
        }
        //慢路径：rect 完全出 mask（原版此刻会整体误隐藏），用实时 mesh 包围盒判定内容是否还在 mask 内
        Bounds bounds = new Bounds();
        bool hasBounds = allowMultipleCanvasRenderers
            ? GetMeshBoundsMultipleRenderers(ref bounds)
            : GetMeshBoundsSingleRenderer(ref bounds);
        //mesh 未生成(初始化前) → 回退默认判定
        if (!hasBounds)
        {
            base.Cull(clipRect, validRect);
            return;
        }
        Rect canvasRect = LocalRectToCanvasRect(rootCanvas, new Rect(bounds.min, bounds.size));
        UpdateCullByMeshBounds(!clipRect.Overlaps(canvasRect, true));
    }

    /// <summary>
    /// 把 RectTransform local 空间的矩形四角变换到 root canvas 空间，取包围盒
    /// </summary>
    Rect LocalRectToCanvasRect(Canvas rootCanvas, Rect localRect)
    {
        Matrix4x4 matrix = rootCanvas.transform.worldToLocalMatrix * rectTransform.localToWorldMatrix;
        Vector3 min = localRect.min;
        Vector3 max = localRect.max;
        //变换四角取 min/max，兼容负缩放与旋转
        Vector3 p0 = matrix.MultiplyPoint(new Vector3(min.x, min.y, 0));
        Vector3 p1 = matrix.MultiplyPoint(new Vector3(min.x, max.y, 0));
        Vector3 p2 = matrix.MultiplyPoint(new Vector3(max.x, min.y, 0));
        Vector3 p3 = matrix.MultiplyPoint(new Vector3(max.x, max.y, 0));
        float minX = Mathf.Min(Mathf.Min(p0.x, p1.x), Mathf.Min(p2.x, p3.x));
        float maxX = Mathf.Max(Mathf.Max(p0.x, p1.x), Mathf.Max(p2.x, p3.x));
        float minY = Mathf.Min(Mathf.Min(p0.y, p1.y), Mathf.Min(p2.y, p3.y));
        float maxY = Mathf.Max(Mathf.Max(p0.y, p1.y), Mathf.Max(p2.y, p3.y));
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    /// <summary>
    /// 应用剔除结果（复刻 MaskableGraphic 私有的 UpdateCull）：
    /// 必须同步触发 onCullStateChanged（Awake 里订阅了它做不可见降频更新）并刷新裁剪材质
    /// </summary>
    void UpdateCullByMeshBounds(bool cull)
    {
        if (canvasRenderer.cull != cull)
        {
            canvasRenderer.cull = cull;
            onCullStateChanged.Invoke(cull);
            OnCullingChanged();
        }
    }
    #endregion
}
