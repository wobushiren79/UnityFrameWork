using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIHandler
{
    #region 光标-字段
    [Header("光标")]
    public Texture2D cursorDef;
    public Texture2D cursorDown;
    public Vector2 offsetCursor = new Vector2(40, 0);

    protected bool isCustomCursor = false;
    protected List<Texture2D> listCursorCustom = new List<Texture2D>();
    protected Coroutine cursorAnimCoroutine;

    //通过图集图标名生成的Cursor贴图缓存 key: {atlasType}_{iconName}
    protected Dictionary<string, Texture2D> dicCursorIconCache = new Dictionary<string, Texture2D>();

    // 场景中若已存在 CursorView 则跳过本Handler的光标逻辑
    protected bool hasExternalCursorView;
    #endregion

    #region 光标-生命周期
    /// <summary>
    /// 光标初始化（由 Awake 调用）
    /// </summary>
    private void AwakeCursor()
    {
        hasExternalCursorView = FindAnyObjectByType<CursorView>() != null;
        if (hasExternalCursorView)
            return;
        if (cursorDef != null)
            Cursor.SetCursor(cursorDef, Vector2.zero, CursorMode.Auto);
    }

    /// <summary>
    /// 光标按下/抬起检测（由 Update 调用）
    /// </summary>
    private void UpdateCursor()
    {
        if (hasExternalCursorView)
            return;
        if (isCustomCursor)
            return;
        if (cursorDef == null || cursorDown == null)
            return;
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.SetCursor(cursorDown, offsetCursor, CursorMode.Auto);
        }
        if (Input.GetMouseButtonUp(0))
        {
            Cursor.SetCursor(cursorDef, offsetCursor, CursorMode.Auto);
        }
    }
    #endregion

    #region 光标-公共方法
    /// <summary>
    /// 设置自定义光标（单图，使用默认 offsetCursor 作为 hotspot）
    /// </summary>
    public void SetCursor(Texture2D cursorIcon)
    {
        SetCursor(cursorIcon, offsetCursor, CursorMode.Auto);
    }

    /// <summary>
    /// 设置自定义光标（单图，指定 hotspot 与 CursorMode）
    /// </summary>
    /// <param name="cursorIcon">光标贴图</param>
    /// <param name="hotspot">点击热点 (相对贴图左上角的像素偏移)</param>
    /// <param name="cursorMode">Auto 优先硬件光标(可能受 DPI/最小尺寸影响)；ForceSoftware 由 Unity 按像素精确渲染</param>
    public void SetCursor(Texture2D cursorIcon, Vector2 hotspot, CursorMode cursorMode = CursorMode.Auto)
    {
        if (hasExternalCursorView)
            return;
        StopCursorAnim();
        isCustomCursor = true;
        Cursor.SetCursor(cursorIcon, hotspot, cursorMode);
    }

    /// <summary>
    /// 设置自定义光标（动画序列）
    /// </summary>
    public void SetCursor(List<Texture2D> listCursorAnim)
    {
        if (hasExternalCursorView)
            return;
        StopCursorAnim();
        isCustomCursor = true;
        listCursorCustom.Clear();
        listCursorCustom.AddRange(listCursorAnim);
        cursorAnimCoroutine = StartCoroutine(CoroutineForCursorAnim());
    }

    /// <summary>
    /// 通过图集图标名设置自定义光标 (异步加载Sprite -> Blit -> Texture2D)
    /// hotspot 默认使用图标中心；可通过 hotspotOverride 显式指定
    /// 小图标默认放大 3 倍 (16→48) 以适配高 DPI 屏幕; 可通过 pixelScale 调整
    /// </summary>
    /// <param name="atlasTag">图集 tag（最终拼接为 AtlasFor{tag}）</param>
    /// <param name="iconName">图标名</param>
    /// <param name="hotspotOverride">显式指定点击热点（相对放大后贴图左上角的像素偏移），null 表示用贴图中心</param>
    /// <param name="pixelScale">像素放大倍数 (1 = 原始尺寸)</param>
    public void SetCursorByIconName(string atlasTag, string iconName, Vector2? hotspotOverride = null, int pixelScale = 3)
    {
        if (hasExternalCursorView)
            return;
        string cacheKey = $"{atlasTag}_{iconName}_x{pixelScale}";
        if (dicCursorIconCache.TryGetValue(cacheKey, out Texture2D cachedTex) && cachedTex != null)
        {
            Vector2 hotspot = hotspotOverride ?? new Vector2(cachedTex.width * 0.5f, cachedTex.height * 0.5f);
            //小图标走 ForceSoftware: 避免 Windows 硬件光标对 16~32px 小图最小尺寸放大导致 hotspot 错位
            SetCursor(cachedTex, hotspot, CursorMode.ForceSoftware);
            return;
        }
        IconHandler.Instance.GetIconSprite(atlasTag, iconName, (sprite) =>
        {
            if (sprite == null)
                return;
            Texture2D tex = TextureUtil.SpriteToTexture2DByBlit(sprite, FilterMode.Point, pixelScale);
            if (tex == null)
                return;
            dicCursorIconCache[cacheKey] = tex;
            Vector2 hotspot = hotspotOverride ?? new Vector2(tex.width * 0.5f, tex.height * 0.5f);
            SetCursor(tex, hotspot, CursorMode.ForceSoftware);
        });
    }

    /// <summary>
    /// 恢复默认光标 (无 cursorDef 时回退到系统默认指针)
    /// </summary>
    public void SetCursorDef()
    {
        if (hasExternalCursorView)
            return;
        StopCursorAnim();
        isCustomCursor = false;
        listCursorCustom.Clear();
        if (cursorDef != null)
            Cursor.SetCursor(cursorDef, offsetCursor, CursorMode.Auto);
        else
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
    #endregion

    #region 光标-内部方法
    /// <summary>
    /// 停止光标动画协程
    /// </summary>
    private void StopCursorAnim()
    {
        if (cursorAnimCoroutine != null)
        {
            StopCoroutine(cursorAnimCoroutine);
            cursorAnimCoroutine = null;
        }
    }

    /// <summary>
    /// 光标动画协程
    /// </summary>
    private IEnumerator CoroutineForCursorAnim()
    {
        int cursorAnimPosition = 0;
        while (listCursorCustom.Count > 0)
        {
            Cursor.SetCursor(listCursorCustom[cursorAnimPosition], offsetCursor, CursorMode.Auto);
            yield return new WaitForSecondsRealtime(0.1f);
            cursorAnimPosition++;
            if (cursorAnimPosition >= listCursorCustom.Count)
            {
                cursorAnimPosition = 0;
            }
        }
    }
    #endregion
}
