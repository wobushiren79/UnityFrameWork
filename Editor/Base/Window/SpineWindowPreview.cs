using UnityEngine;
using UnityEditor;
using Spine;
using Spine.Unity;
using Spine.Unity.Editor;
using System.Collections.Generic;

/// <summary>
/// SpineWindow 动画预览页签（partial）。
/// 解决骨架数据版本与 spine-unity 运行时 minor 版本不一致（如数据 4.2 / 运行时 4.3）时，
/// 官方 SkeletonDataAsset Inspector 因兼容性检查拒绝初始化 Preview 的问题：
/// 运行时 GetSkeletonData 读取成功即返回（兼容检查仅在读取失败时执行），
/// 因此此处绕过官方 Inspector，自行实例化 SkeletonAnimation 并手动驱动实现预览。
/// </summary>
public partial class SpineWindow : EditorWindow
{
    #region 字段

    // 预览-资源与实例
    private SkeletonDataAsset previewAsset;
    private PreviewRenderUtility previewUtility;
    private GameObject previewGO;
    private SkeletonAnimation previewSkeletonAnimation;
    private Renderer previewRenderer;
    private string previewLoadError;
    private string previewDataVersion;

    // 预览-动画
    private List<string> previewAnimNames = new List<string>();
    private int previewAnimIndex = -1;
    private bool previewPlaying = false;
    private bool previewLoop = true;
    private float previewTimeScale = 1f;
    private double previewLastUpdateTime;

    // 预览-皮肤分组（按 "/" 前缀分组，每组单选一个或不显示）
    private List<PreviewSkinGroup> previewSkinGroups = new List<PreviewSkinGroup>();

    // 预览-相机
    private float previewCameraOrtho = 1f;
    private Vector3 previewCameraPos = new Vector3(0, 0, -10);
    private const int PreviewLayer = 30;

    // 预览-UI
    private Vector2 previewLeftScroll;
    private GUIStyle previewBackgroundStyle;

    /// <summary>
    /// 皮肤分组数据：组内皮肤列表 + 当前选中（0=不显示，1..N=皮肤索引+1）
    /// </summary>
    private class PreviewSkinGroup
    {
        public string GroupName;
        public List<Skin> Skins = new List<Skin>();
        public int SelectedIndex = 1;
    }

    #endregion

    #region 生命周期

    /// <summary>
    /// 窗口启用：订阅编辑器更新与播放模式变化
    /// </summary>
    void OnEnable()
    {
        EditorApplication.update += OnPreviewEditorUpdate;
        EditorApplication.playModeStateChanged += OnPreviewPlayModeChanged;
    }

    /// <summary>
    /// 窗口禁用：退订并销毁预览实例
    /// </summary>
    void OnDisable()
    {
        EditorApplication.update -= OnPreviewEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPreviewPlayModeChanged;
        DisposePreview();
    }

    /// <summary>
    /// 进入/退出播放模式时销毁预览实例（HideAndDontSave 物体会随场景卸载销毁，引用需同步清理）
    /// </summary>
    void OnPreviewPlayModeChanged(PlayModeStateChange state)
    {
        DisposePreview();
    }

    /// <summary>
    /// 编辑器帧更新：播放中手动推进动画并重绘
    /// </summary>
    void OnPreviewEditorUpdate()
    {
        if (previewSkeletonAnimation == null) return;
        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - previewLastUpdateTime);
        previewLastUpdateTime = now;
        if (!previewPlaying) return;

        previewSkeletonAnimation.Update(dt * previewTimeScale);
        previewSkeletonAnimation.Renderer.LateUpdate();

        // 非循环动画播放完毕后自动停止
        if (previewSkeletonAnimation.AnimationState.GetTrack(0) == null)
            previewPlaying = false;
        Repaint();
    }

    #endregion

    #region 预览构建与销毁

    /// <summary>
    /// 重建预览实例；forceReload 时先清空 SkeletonDataAsset 缓存强制重读数据
    /// </summary>
    void RebuildPreview(bool forceReload = false)
    {
        DisposePreview();
        previewLoadError = null;
        previewDataVersion = null;
        previewAnimNames.Clear();
        previewSkinGroups.Clear();
        previewAnimIndex = -1;
        if (previewAsset == null) return;

        try
        {
            if (forceReload) previewAsset.Clear();
            SpineEditorUtilities.ConfirmInitialization();

            // 预览相机：正交 + 只渲染 PreviewLayer
            previewUtility = new PreviewRenderUtility(true);
            Camera cam = previewUtility.camera;
            cam.orthographic = true;
            cam.cullingMask = 1 << PreviewLayer;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1000f;
            cam.orthographicSize = previewCameraOrtho;
            cam.transform.position = previewCameraPos;

            // 与官方预览同款实例化方式（内部 Initialize(false)，数据读取成功即兼容）
            previewSkeletonAnimation = EditorInstantiation.InstantiateSkeletonAnimation(
                previewAsset, skinName: "", destroyInvalid: true, useObjectFactory: false);
            if (previewSkeletonAnimation == null)
            {
                previewLoadError = "无法加载骨架数据（版本不兼容或资源损坏）";
                return;
            }

            previewGO = previewSkeletonAnimation.gameObject;
            previewGO.hideFlags = HideFlags.HideAndDontSave;
            previewGO.layer = PreviewLayer;
            previewRenderer = previewGO.GetComponent<Renderer>();
            // 关键：URP/新版本中预览相机只渲染预览场景内的物体，留在活动场景会渲染为空
            previewUtility.AddSingleGO(previewGO);
            // 关键：renderer 必须保持 enabled——4.3 中 LateUpdate 在 renderer 禁用时直接跳过网格重建
            // （NeedsToGenerateMesh = meshRenderer.enabled），物体在预览场景内只有预览相机可见，常开无副作用
            previewSkeletonAnimation.Renderer.LateUpdate();

            // 收集动画与皮肤数据
            SkeletonData data = previewSkeletonAnimation.Skeleton.Data;
            previewDataVersion = data.Version;
            foreach (Spine.Animation anim in data.Animations)
                previewAnimNames.Add(anim.Name);
            BuildPreviewSkinGroups(data);

            ApplyPreviewSkin();
            FramePreviewCamera();
            previewLastUpdateTime = EditorApplication.timeSinceStartup;
        }
        catch (System.Exception ex)
        {
            DisposePreview();
            previewLoadError = $"加载失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 销毁预览实例与渲染工具
    /// </summary>
    void DisposePreview()
    {
        previewPlaying = false;
        if (previewGO != null)
        {
            DestroyImmediate(previewGO);
            previewGO = null;
        }
        previewSkeletonAnimation = null;
        previewRenderer = null;
        if (previewUtility != null)
        {
            previewUtility.Cleanup();
            previewUtility = null;
        }
    }

    /// <summary>
    /// 相机取景：按渲染包围盒居中并适配大小
    /// </summary>
    void FramePreviewCamera()
    {
        if (previewRenderer == null) return;
        Bounds b = previewRenderer.bounds;
        // 网格尚未生成时包围盒为空，用默认取景兜底
        if (b.size.y < 0.0001f)
        {
            previewCameraOrtho = 2f;
            previewCameraPos = new Vector3(0, 1f, -10f);
            return;
        }
        previewCameraOrtho = Mathf.Max(0.01f, b.size.y);
        previewCameraPos = b.center + new Vector3(0, 0, -10f);
    }

    #endregion

    #region 皮肤搭配

    /// <summary>
    /// 按 "/" 前缀把皮肤分组（如 Clothes/A 与 Clothes/B 归入 Clothes 组），无 "/" 的皮肤自成一组
    /// </summary>
    void BuildPreviewSkinGroups(SkeletonData data)
    {
        var dict = new Dictionary<string, PreviewSkinGroup>();
        var order = new List<string>();
        foreach (Skin skin in data.Skins)
        {
            string skinName = skin.Name;
            int slashIndex = skinName.IndexOf('/');
            string groupName = slashIndex >= 0 ? skinName.Substring(0, slashIndex) : skinName;
            if (!dict.TryGetValue(groupName, out PreviewSkinGroup group))
            {
                group = new PreviewSkinGroup { GroupName = groupName };
                dict[groupName] = group;
                order.Add(groupName);
            }
            group.Skins.Add(skin);
        }
        foreach (string name in order)
            previewSkinGroups.Add(dict[name]);
    }

    /// <summary>
    /// 按各分组选中项合成皮肤并应用到骨架，随后重摆姿势刷新网格
    /// </summary>
    void ApplyPreviewSkin()
    {
        if (previewSkeletonAnimation == null) return;
        Skeleton skeleton = previewSkeletonAnimation.Skeleton;
        Skin combined = new Skin("preview-combined");
        foreach (PreviewSkinGroup group in previewSkinGroups)
            if (group.SelectedIndex > 0)
                combined.AddSkin(group.Skins[group.SelectedIndex - 1]);

        skeleton.SetSkin(combined);
        skeleton.SetupPose();
        // Update(0) 不推进时间，仅按当前动画轨道重摆姿势
        previewSkeletonAnimation.Update(0);
        previewSkeletonAnimation.Renderer.LateUpdate();
        Repaint();
    }

    /// <summary>
    /// 皮肤显示名：去掉分组前缀（如 Clothes/A 显示为 A）
    /// </summary>
    string GetSkinDisplayName(string skinName, string groupName)
    {
        int slashIndex = skinName.IndexOf('/');
        return slashIndex >= 0 ? skinName.Substring(slashIndex + 1) : skinName;
    }

    #endregion

    #region 动画控制

    /// <summary>
    /// 播放指定索引的动画（按当前循环设置）
    /// </summary>
    void PlayPreviewAnimation(int index)
    {
        if (previewSkeletonAnimation == null || index < 0 || index >= previewAnimNames.Count) return;
        previewAnimIndex = index;
        previewSkeletonAnimation.AnimationState.SetAnimation(0, previewAnimNames[index], previewLoop);
        previewPlaying = true;
        previewLastUpdateTime = EditorApplication.timeSinceStartup;
    }

    /// <summary>
    /// 停止播放并回到初始姿势
    /// </summary>
    void StopPreviewAnimation()
    {
        if (previewSkeletonAnimation == null) return;
        previewPlaying = false;
        previewSkeletonAnimation.AnimationState.ClearTracks();
        previewSkeletonAnimation.Skeleton.SetupPose();
        previewSkeletonAnimation.Update(0);
        previewSkeletonAnimation.Renderer.LateUpdate();
        Repaint();
    }

    #endregion

    #region 界面绘制

    /// <summary>
    /// 绘制动画预览页签
    /// </summary>
    void DrawPreviewTab()
    {
        EditorGUILayout.Space(6);
        GUILayout.Label("Spine 动画预览（绕过官方版本检查）", headerStyle);
        EditorGUILayout.Space(4);
        DrawSeparator();
        EditorGUILayout.Space(6);

        // 数据源选择
        EditorGUILayout.BeginHorizontal();
        SkeletonDataAsset newAsset = EditorGUILayout.ObjectField(
            new GUIContent("骨架数据源", "拖入 SkeletonDataAsset；版本与运行时不一致（如 4.2 数据配 4.3 运行时）也可预览"),
            previewAsset, typeof(SkeletonDataAsset), false) as SkeletonDataAsset;
        if (newAsset != previewAsset)
        {
            previewAsset = newAsset;
            RebuildPreview();
        }
        if (GUILayout.Button("重新加载", GUILayout.Width(70)))
            RebuildPreview(true);
        EditorGUILayout.EndHorizontal();

        if (!previewLoadError.IsNull())
            EditorGUILayout.HelpBox(previewLoadError, MessageType.Error);
        if (previewAsset == null)
        {
            EditorGUILayout.HelpBox("拖入 SkeletonDataAsset 后即可预览动画与自由搭配皮肤。", MessageType.Info);
            return;
        }
        // 域重载后资源等序列化字段还在但预览实例已销毁，自动重建一次
        if (previewSkeletonAnimation == null)
        {
            if (previewLoadError.IsNull())
                RebuildPreview();
            if (previewSkeletonAnimation == null) return;
        }

        // 主区域：左侧面板 + 预览画面
        EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
        DrawPreviewLeftPanel();
        DrawPreviewArea();
        EditorGUILayout.EndHorizontal();

        DrawPreviewControls();
        EditorGUILayout.Space(4);
    }

    /// <summary>
    /// 绘制左侧面板（皮肤搭配 + 动画列表）
    /// </summary>
    void DrawPreviewLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(240), GUILayout.ExpandHeight(true));
        previewLeftScroll = EditorGUILayout.BeginScrollView(previewLeftScroll);

        // 皮肤搭配
        DrawSectionHeader($"皮肤搭配 ({previewSkinGroups.Count})");
        if (previewSkinGroups.Count == 0)
            EditorGUILayout.LabelField("该骨架没有皮肤", EditorStyles.miniLabel);
        foreach (PreviewSkinGroup group in previewSkinGroups)
        {
            string[] options = new string[group.Skins.Count + 1];
            options[0] = "（不显示）";
            for (int i = 0; i < group.Skins.Count; i++)
                options[i + 1] = GetSkinDisplayName(group.Skins[i].Name, group.GroupName);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(group.GroupName, GUILayout.Width(70));
            int newIndex = EditorGUILayout.Popup(group.SelectedIndex, options);
            if (newIndex != group.SelectedIndex)
            {
                group.SelectedIndex = newIndex;
                ApplyPreviewSkin();
            }
            EditorGUILayout.EndHorizontal();
        }
        if (previewSkinGroups.Count > 1)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全部显示"))
            {
                foreach (PreviewSkinGroup group in previewSkinGroups) group.SelectedIndex = 1;
                ApplyPreviewSkin();
            }
            if (GUILayout.Button("全部隐藏"))
            {
                foreach (PreviewSkinGroup group in previewSkinGroups) group.SelectedIndex = 0;
                ApplyPreviewSkin();
            }
            EditorGUILayout.EndHorizontal();
        }

        // 动画列表
        DrawSectionHeader($"动画列表 ({previewAnimNames.Count})");
        Color defaultColor = GUI.backgroundColor;
        for (int i = 0; i < previewAnimNames.Count; i++)
        {
            bool isCurrent = i == previewAnimIndex;
            if (isCurrent) GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button((isCurrent ? "▶ " : "") + previewAnimNames[i]))
                PlayPreviewAnimation(i);
            GUI.backgroundColor = defaultColor;
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制预览画面（滚轮缩放、拖拽平移）
    /// </summary>
    void DrawPreviewArea()
    {
        Rect rect = GUILayoutUtility.GetRect(200, float.MaxValue, 200, float.MaxValue,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        HandlePreviewInput(rect);
        if (UnityEngine.Event.current.type != UnityEngine.EventType.Repaint) return;
        if (previewUtility == null || previewGO == null) return;

        previewBackgroundStyle = previewBackgroundStyle ?? new GUIStyle("PreBackground");
        previewUtility.BeginPreview(rect, previewBackgroundStyle);
        Camera cam = previewUtility.camera;
        cam.orthographicSize = previewCameraOrtho;
        cam.transform.position = previewCameraPos;

        cam.Render();

        Texture tex = previewUtility.EndPreview();
        GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
    }

    /// <summary>
    /// 处理预览区域内的鼠标输入：滚轮缩放、左/右键拖拽平移
    /// </summary>
    void HandlePreviewInput(Rect rect)
    {
        UnityEngine.Event e = UnityEngine.Event.current;
        if (!rect.Contains(e.mousePosition)) return;

        if (e.type == UnityEngine.EventType.ScrollWheel)
        {
            previewCameraOrtho = Mathf.Max(0.01f, previewCameraOrtho * (1f + e.delta.y * 0.05f));
            e.Use();
            Repaint();
        }
        else if (e.type == UnityEngine.EventType.MouseDrag && (e.button == 0 || e.button == 2))
        {
            // 像素增量换算成世界单位：orthoSize*2 是视图高度对应的世界高度
            float worldPerPixel = previewCameraOrtho * 2f / rect.height;
            previewCameraPos -= new Vector3(e.delta.x, -e.delta.y, 0) * worldPerPixel;
            e.Use();
            Repaint();
        }
    }

    /// <summary>
    /// 绘制底部播放控制条（播放/暂停、停止、循环、速度、进度、视角复位、版本信息）
    /// </summary>
    void DrawPreviewControls()
    {
        if (previewSkeletonAnimation == null) return;
        TrackEntry track = previewSkeletonAnimation.AnimationState.GetTrack(0);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(previewPlaying ? "暂停" : "播放", GUILayout.Width(50)))
        {
            if (previewPlaying)
            {
                previewPlaying = false;
            }
            else if (previewAnimIndex >= 0)
            {
                // 轨道已被清空（播完）时重新 SetAnimation
                if (track == null)
                    PlayPreviewAnimation(previewAnimIndex);
                else
                    previewPlaying = true;
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请先在左侧动画列表中选择一个动画", "OK");
            }
        }
        if (GUILayout.Button("停止", GUILayout.Width(50)))
            StopPreviewAnimation();

        bool newLoop = EditorGUILayout.ToggleLeft("循环", previewLoop, GUILayout.Width(50));
        if (newLoop != previewLoop)
        {
            previewLoop = newLoop;
            if (track != null) track.Loop = previewLoop;
        }

        GUILayout.Label("速度", GUILayout.Width(30));
        previewTimeScale = EditorGUILayout.Slider(previewTimeScale, 0f, 2f, GUILayout.Width(120));

        if (GUILayout.Button("重置视角", GUILayout.Width(70)))
            FramePreviewCamera();

        GUILayout.FlexibleSpace();
        if (!previewDataVersion.IsNull())
            GUILayout.Label($"数据版本: {previewDataVersion}", EditorStyles.miniLabel, GUILayout.Width(130));
        EditorGUILayout.EndHorizontal();

        // 进度条：拖动可定位到任意帧
        if (track != null)
        {
            float duration = track.Animation.Duration;
            float time = Mathf.Min(track.TrackTime, duration);
            EditorGUILayout.BeginHorizontal();
            float newTime = EditorGUILayout.Slider(time, 0f, duration);
            if (newTime != time)
            {
                track.TrackTime = newTime;
                previewSkeletonAnimation.Update(0);
                previewSkeletonAnimation.Renderer.LateUpdate();
            }
            GUILayout.Label($"{time:F2}/{duration:F2}s", EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();
        }
    }

    #endregion
}
