using UnityEngine;

/// <summary>
/// 精灵残影：适用于 SpriteRenderer（2D 精灵角色）。
///
/// 做法最轻：残影自身挂一个 SpriteRenderer，复制当前 sprite / 翻转 / 材质 / 排序，按淡出系数降 color（含透明度）。
/// 无需拷网格（SpriteRenderer 内部自建四边形），也无需 MPB —— 直接改 SpriteRenderer.color 即可淡出。
/// 若源用 Sliced/Tiled 等非 Simple 绘制模式，会一并复制 size。
/// </summary>
public class AfterimageGhostSprite : AfterimageGhostBase
{
    #region 绑定源

    /// <summary>本体 SpriteRenderer</summary>
    protected SpriteRenderer srcRenderer;

    /// <summary>精灵残影运行时数据</summary>
    protected class SpriteGhostItem : GhostItem
    {
        public SpriteRenderer spriteRenderer;
    }

    #endregion

    #region 绑定接口

    /// <summary>
    /// 绑定源 SpriteRenderer
    /// </summary>
    /// <param name="renderer">源精灵渲染器</param>
    public void Init(SpriteRenderer renderer)
    {
        srcRenderer = renderer;
    }

    #endregion

    #region 基类实现

    /// <summary>源就绪判定：渲染器与当前 sprite 齐备</summary>
    protected override bool CanCapture()
    {
        return srcRenderer != null && srcRenderer.sprite != null;
    }

    /// <summary>新建精灵残影对象(挂 SpriteRenderer)</summary>
    protected override GhostItem CreateGhost()
    {
        var go = new GameObject("AfterimageGhost_Sprite");
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        return new SpriteGhostItem { go = go, spriteRenderer = spriteRenderer, age = 0 };
    }

    /// <summary>复制当前 sprite / 翻转 / 材质 / 排序 / 位置</summary>
    protected override void CaptureInto(GhostItem item)
    {
        var g = (SpriteGhostItem)item;
        var sr = g.spriteRenderer;
        sr.sprite = srcRenderer.sprite;
        sr.flipX = srcRenderer.flipX;
        sr.flipY = srcRenderer.flipY;
        sr.sharedMaterial = srcRenderer.sharedMaterial;
        sr.drawMode = srcRenderer.drawMode;
        //非 Simple 绘制模式(Sliced/Tiled)需复制 size
        if (sr.drawMode != SpriteDrawMode.Simple)
        {
            sr.size = srcRenderer.size;
        }
        sr.sortingLayerID = srcRenderer.sortingLayerID;
        sr.sortingOrder = srcRenderer.sortingOrder - 1;
        var srcTF = srcRenderer.transform;
        g.go.transform.SetPositionAndRotation(srcTF.position, srcTF.rotation);
        g.go.transform.localScale = srcTF.lossyScale;
    }

    /// <summary>按淡出系数降 color(含透明度)</summary>
    protected override void ApplyFade(GhostItem item, float t)
    {
        var g = (SpriteGhostItem)item;
        g.spriteRenderer.color = ghostTint * t;
    }

    /// <summary>销毁精灵残影</summary>
    protected override void DestroyGhost(GhostItem item)
    {
        if (item.go != null) Destroy(item.go);
    }

    #endregion
}
