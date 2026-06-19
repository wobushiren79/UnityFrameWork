using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PixelDa
{
    /// <summary>
    /// PixelDa 像素美术生成编辑器窗口：纯 C# 直连豆包/通义，复刻 dada-x/pixelda 工具的全部功能
    /// （文生图、图编辑、图生视频、视频抽帧、去背景、精灵表合成、音乐生成、历史、设置）。
    /// </summary>
    public class PixelDaEditorWindow : EditorWindow
    {
        #region 菜单项与窗口创建

        /// <summary>
        /// 菜单项：Custom/AI/像素图生成
        /// </summary>
        [MenuItem("Custom/AI/像素图生成")]
        private static void OpenWindow()
        {
            var window = GetWindow<PixelDaEditorWindow>();
            window.titleContent = new GUIContent("PixelDa");
            window.minSize = new Vector2(560, 640);
            window.Show();
        }

        #endregion

        #region 页签与共享状态

        /// <summary>页签名称</summary>
        private static readonly string[] TabNames =
        {
            "文生图", "图编辑", "图生视频", "视频抽帧", "音乐生成", "历史", "设置",
        };

        /// <summary>当前选中页签</summary>
        private int tabIndex = 0;

        /// <summary>滚动位置</summary>
        private Vector2 scroll;

        /// <summary>是否正在执行网络任务</summary>
        private bool isBusy = false;

        /// <summary>当前进度/状态文本</summary>
        private string statusText = "";

        /// <summary>当前选中的提供商</summary>
        private PixelDaProvider provider;

        /// <summary>视频任务取消标记</summary>
        private bool cancelFlag = false;

        #endregion

        #region 尺寸/参数预设

        /// <summary>图片尺寸预设</summary>
        private static readonly string[] SizeOptions =
        {
            "1024*1024", "1024*768", "768*1024", "1280*720", "720*1280",
        };

        /// <summary>视频分辨率预设</summary>
        private static readonly string[] ResolutionOptions = { "480P", "720P", "1080P" };

        /// <summary>音乐风格预设</summary>
        private static readonly string[] GenreOptions =
        {
            "pop", "rock", "jazz", "classical", "electronic", "ambient", "chiptune",
        };

        /// <summary>音乐节奏预设</summary>
        private static readonly string[] TempoOptions = { "slow", "medium", "fast" };

        #endregion

        #region 文生图状态

        private string imgPrompt = "A high-resolution pixel-art game asset, with black outline, against a solid gray background.";
        private string imgNegative = "";
        private int imgSeed = -1;
        private int imgSizeIndex = 0;
        private Texture2D imgResultTex;
        private string imgResultUrl;
        private string imgResultPath;

        #endregion

        #region 图编辑状态

        private string editPrompt = "";
        private string editNegative = "";
        private string editSourceUrl = "";
        private Texture2D editSourceTex;
        private string editSourcePath;
        private int editSeed = -1;
        private int editSizeIndex = 0;
        private Texture2D editResultTex;
        private string editResultUrl;
        private string editResultPath;

        #endregion

        #region 图生视频状态

        private string videoBaseImageUrl = "";
        private Texture2D videoBaseTex;
        private string videoBaseSourcePath;
        private string videoPrompt = "";
        private string videoNegative = "";
        private int videoResolutionIndex = 0;
        private string videoResultPath;

        #endregion

        #region 视频抽帧状态

        private string framesVideoPath = "";
        private float framesFrom = 0f;
        private float framesTo = 5f;
        private int framesCount = 8;
        private bool framesRemoveBg = false;
        private List<string> framePaths = new List<string>();
        private List<Texture2D> frameTextures = new List<Texture2D>();
        private Vector2 framesScroll;

        #endregion

        #region 音乐状态

        private string musicPrompt = "A cheerful chiptune melody for a pixel-art game menu.";
        private int musicDuration = 30;
        private int musicGenreIndex = 6;
        private int musicTempoIndex = 1;
        private int musicSeed = -1;
        private string musicAbc = "";
        private string musicComments = "";
        private AudioClip musicClip;
        private float[] musicSamples;
        private int musicSampleRate;

        #endregion

        #region 设置状态

        private bool showAdvanced = false;

        #endregion

        #region 历史状态

        private int historyFilter = 0;
        private static readonly string[] HistoryFilters = { "全部", "图片", "视频", "音频" };
        private List<string> historyFiles = new List<string>();
        private Dictionary<string, Texture2D> historyThumbs = new Dictionary<string, Texture2D>();
        private Vector2 historyScroll;

        #endregion

        #region 生命周期

        /// <summary>
        /// 启用时读取持久化的提供商
        /// </summary>
        private void OnEnable()
        {
            provider = PixelDaConfig.Provider;
        }

        /// <summary>
        /// 关闭时释放运行时创建的预览纹理，避免内存泄漏
        /// </summary>
        private void OnDisable()
        {
            if (imgResultTex != null) DestroyImmediate(imgResultTex);
            if (editResultTex != null) DestroyImmediate(editResultTex);
            ClearFrameTextures();
        }

        /// <summary>
        /// 绘制主界面
        /// </summary>
        private void OnGUI()
        {
            InitStyles();
            DrawHeader();
            DrawTabBar();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Space(4);
            EditorGUILayout.BeginVertical(contentStyle);
            switch (tabIndex)
            {
                case 0: DrawImageTab(); break;
                case 1: DrawEditTab(); break;
                case 2: DrawVideoTab(); break;
                case 3: DrawFramesTab(); break;
                case 4: DrawMusicTab(); break;
                case 5: DrawHistoryTab(); break;
                case 6: DrawSettingsTab(); break;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            DrawStatusBar();
        }

        #endregion

        #region 样式系统与绘制助手

        /// <summary>强调色（主操作按钮 / 强调元素）</summary>
        private static readonly Color Accent = new Color(0.40f, 0.60f, 1.00f);

        /// <summary>成功色</summary>
        private static readonly Color Ok = new Color(0.36f, 0.78f, 0.45f);

        /// <summary>警告色</summary>
        private static readonly Color Warn = new Color(0.95f, 0.65f, 0.20f);

        /// <summary>强调色十六进制（富文本用）</summary>
        private const string HexAccent = "#6699FF";
        private const string HexOk = "#5AC772";
        private const string HexWarn = "#F2A63A";

        /// <summary>样式是否已初始化</summary>
        private bool stylesReady;

        private GUIStyle titleStyle;
        private GUIStyle subTitleStyle;
        private GUIStyle bannerStyle;
        private GUIStyle contentStyle;
        private GUIStyle cardStyle;
        private GUIStyle sectionStyle;
        private GUIStyle hintStyle;
        private GUIStyle tabStyle;
        private GUIStyle previewBoxStyle;

        /// <summary>
        /// 懒初始化所有 GUIStyle（OnGUI 内才能安全访问 EditorStyles）
        /// </summary>
        private void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            subTitleStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10, richText = true };
            bannerStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(12, 12, 8, 8), margin = new RectOffset(0, 0, 0, 2) };
            contentStyle = new GUIStyle { padding = new RectOffset(6, 6, 2, 2) };
            cardStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(12, 12, 10, 12), margin = new RectOffset(0, 0, 4, 6) };
            sectionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, richText = true, margin = new RectOffset(0, 0, 2, 4) };
            hintStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, richText = true };
            tabStyle = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 26, fontSize = 12 };
            previewBoxStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(6, 6, 6, 6), alignment = TextAnchor.MiddleCenter };
        }

        /// <summary>
        /// 绘制顶部页签栏（加大、加粗）
        /// </summary>
        private void DrawTabBar()
        {
            GUILayout.Space(2);
            tabIndex = GUILayout.Toolbar(tabIndex, TabNames, tabStyle, GUILayout.Height(26));
        }

        /// <summary>
        /// 开始一张卡片容器
        /// </summary>
        private void BeginCard()
        {
            EditorGUILayout.BeginVertical(cardStyle);
        }

        /// <summary>
        /// 结束卡片容器
        /// </summary>
        private void EndCard()
        {
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制带强调色竖条的小节标题
        /// </summary>
        private void Section(string title)
        {
            EditorGUILayout.LabelField($"<color={HexAccent}>▌</color> {title}", sectionStyle);
        }

        /// <summary>
        /// 绘制一个带颜色的主操作按钮
        /// </summary>
        private bool AccentButton(string label, float height = 32f, Color? color = null)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = color ?? Accent;
            bool clicked = GUILayout.Button(label, GUILayout.Height(height));
            GUI.backgroundColor = old;
            return clicked;
        }

        /// <summary>
        /// 绘制一个多行提示输入框（带标题）
        /// </summary>
        private string PromptField(string label, string value, float height)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            return EditorGUILayout.TextArea(value, GUILayout.Height(height));
        }

        #endregion

        #region 顶部与状态栏

        /// <summary>
        /// 绘制顶部品牌横幅：标题/副标题 + 提供商选择 + Key 状态徽标
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(bannerStyle);
            EditorGUILayout.BeginHorizontal();

            // 左侧：标题与副标题
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("PixelDa  像素美术生成", titleStyle);
            EditorGUILayout.LabelField("豆包 / 通义 · 文生图 · 图编辑 · 图生视频 · 抽帧 · 音乐", subTitleStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // 右侧：提供商选择 + Key 状态
            EditorGUILayout.BeginVertical(GUILayout.Width(230));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("提供商", GUILayout.Width(44));
            PixelDaProvider newProvider = (PixelDaProvider)EditorGUILayout.EnumPopup(provider, GUILayout.Width(120));
            if (newProvider != provider)
            {
                provider = newProvider;
                PixelDaConfig.Provider = provider;
            }
            EditorGUILayout.EndHorizontal();

            bool hasKey = !string.IsNullOrEmpty(PixelDaConfig.GetActiveApiKey(provider));
            string keyText = hasKey
                ? $"<color={HexOk}>●</color> API Key 已配置"
                : $"<color={HexWarn}>●</color> 未配置 API Key（去设置页）";
            EditorGUILayout.LabelField(keyText, hintStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制底部状态栏：忙碌时显示动态进度条 + 可选取消
        /// </summary>
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (isBusy)
            {
                Rect r = GUILayoutUtility.GetRect(10, 18, GUILayout.ExpandWidth(true));
                float fill = Mathf.Repeat((float)EditorApplication.timeSinceStartup * 0.6f, 1f);
                EditorGUI.ProgressBar(r, fill, statusText);
                if (tabIndex == 2)
                {
                    Color old = GUI.backgroundColor;
                    GUI.backgroundColor = Warn;
                    if (GUILayout.Button("取消", GUILayout.Width(60)))
                    {
                        cancelFlag = true;
                    }
                    GUI.backgroundColor = old;
                }
                Repaint();
            }
            else
            {
                string txt = string.IsNullOrEmpty(statusText)
                    ? $"<color={HexOk}>●</color> 就绪"
                    : statusText;
                EditorGUILayout.LabelField(txt, hintStyle);
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 文生图页

        /// <summary>
        /// 绘制文生图页
        /// </summary>
        private void DrawImageTab()
        {
            BeginCard();
            Section("文生图");
            imgPrompt = PromptField("提示词", imgPrompt, 60);
            imgNegative = PromptField("负向提示词（仅通义有效）", imgNegative, 30);
            imgSizeIndex = EditorGUILayout.Popup("尺寸", imgSizeIndex, SizeOptions);
            imgSeed = EditorGUILayout.IntField("随机种子(-1随机)", imgSeed);
            EditorGUILayout.Space(4);
            EditorGUI.BeginDisabledGroup(isBusy);
            if (AccentButton("生成图片")) StartImageGeneration();
            EditorGUI.EndDisabledGroup();
            EndCard();

            if (imgResultTex != null)
            {
                BeginCard();
                Section("生成结果");
                DrawTexturePreview(imgResultTex, 256);
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("定位到工程文件", GUILayout.Height(24))) PingFileAsset(imgResultPath);
                if (GUILayout.Button("用作图编辑源", GUILayout.Height(24)))
                {
                    SetSourceFromGenerated(ref editSourceTex, ref editSourcePath, ref editSourceUrl);
                    tabIndex = 1;
                }
                if (GUILayout.Button("用作视频首帧", GUILayout.Height(24)))
                {
                    SetSourceFromGenerated(ref videoBaseTex, ref videoBaseSourcePath, ref videoBaseImageUrl);
                    tabIndex = 2;
                }
                EditorGUILayout.EndHorizontal();
                EndCard();
            }
        }

        /// <summary>
        /// 启动文生图任务
        /// </summary>
        private void StartImageGeneration()
        {
            if (!ValidateKey()) return;
            isBusy = true;
            statusText = "提交中...";
            imgResultTex = null;
            string key = PixelDaConfig.GetActiveApiKey(provider);

            PixelDaApi.GenerateImage(provider, key, imgPrompt, imgNegative, imgSeed, SizeOptions[imgSizeIndex],
                (success, url, error) =>
                {
                    if (success)
                    {
                        imgResultUrl = url;
                        DownloadAndPreviewImage(url, "images", "img", (tex, path) => { imgResultTex = tex; imgResultPath = path; });
                    }
                    else
                    {
                        FinishWithError(error);
                    }
                },
                msg => { statusText = msg; Repaint(); });
        }

        #endregion

        #region 图编辑页

        /// <summary>
        /// 绘制图编辑页
        /// </summary>
        private void DrawEditTab()
        {
            BeginCard();
            Section("图编辑（图 + 提示词 → 新图）");
            DrawImageSourceField("源图", ref editSourceTex, ref editSourcePath, ref editSourceUrl);
            if (!string.IsNullOrEmpty(imgResultUrl) && GUILayout.Button("用上次文生图结果作为源图", GUILayout.Height(22)))
            {
                SetSourceFromGenerated(ref editSourceTex, ref editSourcePath, ref editSourceUrl);
            }
            editPrompt = PromptField("编辑提示词", editPrompt, 50);
            editNegative = PromptField("负向提示词（仅通义有效）", editNegative, 30);
            editSizeIndex = EditorGUILayout.Popup("尺寸", editSizeIndex, SizeOptions);
            editSeed = EditorGUILayout.IntField("随机种子(-1随机)", editSeed);
            EditorGUILayout.Space(4);
            EditorGUI.BeginDisabledGroup(isBusy);
            if (AccentButton("生成编辑结果")) StartImageEdit();
            EditorGUI.EndDisabledGroup();
            EndCard();

            if (editResultTex != null)
            {
                BeginCard();
                Section("编辑结果");
                DrawTexturePreview(editResultTex, 256);
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("定位到工程文件", GUILayout.Height(24))) PingFileAsset(editResultPath);
                if (GUILayout.Button("用作视频首帧", GUILayout.Height(24)))
                {
                    if (!string.IsNullOrEmpty(editResultPath) && File.Exists(editResultPath))
                    {
                        videoBaseTex = PixelDaImageUtil.LoadTextureFromFile(editResultPath);
                        videoBaseSourcePath = editResultPath;
                        videoBaseImageUrl = "";
                    }
                    else
                    {
                        videoBaseImageUrl = editResultUrl;
                        videoBaseTex = null;
                        videoBaseSourcePath = null;
                    }
                    tabIndex = 2;
                }
                EditorGUILayout.EndHorizontal();
                EndCard();
            }
        }

        /// <summary>
        /// 启动图编辑任务
        /// </summary>
        private void StartImageEdit()
        {
            if (!ValidateKey()) return;
            string src = ResolveImageSource(editSourceTex, editSourcePath, editSourceUrl);
            if (string.IsNullOrEmpty(src))
            {
                EditorUtility.DisplayDialog("PixelDa", "请提供源图：拖拽工程图片，或填写图片 URL", "确定");
                return;
            }
            isBusy = true;
            statusText = "提交中...";
            editResultTex = null;
            string key = PixelDaConfig.GetActiveApiKey(provider);

            PixelDaApi.EditImage(provider, key, editPrompt, src, editNegative, editSeed, SizeOptions[editSizeIndex],
                (success, url, error) =>
                {
                    if (success)
                    {
                        editResultUrl = url;
                        DownloadAndPreviewImage(url, "images", "edit", (tex, path) => { editResultTex = tex; editResultPath = path; });
                    }
                    else
                    {
                        FinishWithError(error);
                    }
                },
                msg => { statusText = msg; Repaint(); });
        }

        #endregion

        #region 图生视频页

        /// <summary>
        /// 绘制图生视频页
        /// </summary>
        private void DrawVideoTab()
        {
            BeginCard();
            Section("图生视频（首帧图 + 提示词 → 约5秒动画）");
            DrawImageSourceField("首帧图", ref videoBaseTex, ref videoBaseSourcePath, ref videoBaseImageUrl);
            if (!string.IsNullOrEmpty(imgResultUrl) && GUILayout.Button("用上次文生图结果作为首帧", GUILayout.Height(22)))
            {
                SetSourceFromGenerated(ref videoBaseTex, ref videoBaseSourcePath, ref videoBaseImageUrl);
            }
            videoPrompt = PromptField("提示词", videoPrompt, 50);
            videoNegative = PromptField("负向提示词（仅通义有效）", videoNegative, 30);
            videoResolutionIndex = EditorGUILayout.Popup("分辨率", videoResolutionIndex, ResolutionOptions);
            EditorGUILayout.Space(4);
            EditorGUI.BeginDisabledGroup(isBusy);
            if (AccentButton("生成视频")) StartVideoGeneration();
            EditorGUI.EndDisabledGroup();
            EndCard();

            if (!string.IsNullOrEmpty(videoResultPath) && File.Exists(videoResultPath))
            {
                BeginCard();
                Section("生成结果");
                EditorGUILayout.HelpBox("视频已保存：" + videoResultPath, MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("定位到工程文件", GUILayout.Height(24))) PingFileAsset(videoResultPath);
                if (AccentButton("去抽帧 →", 24, Ok))
                {
                    framesVideoPath = videoResultPath;
                    tabIndex = 3;
                }
                EditorGUILayout.EndHorizontal();
                EndCard();
            }
        }

        /// <summary>
        /// 启动图生视频任务
        /// </summary>
        private void StartVideoGeneration()
        {
            if (!ValidateKey()) return;
            string baseSrc = ResolveImageSource(videoBaseTex, videoBaseSourcePath, videoBaseImageUrl);
            if (string.IsNullOrEmpty(baseSrc))
            {
                EditorUtility.DisplayDialog("PixelDa", "请提供首帧图：拖拽工程图片，或填写图片 URL", "确定");
                return;
            }
            isBusy = true;
            cancelFlag = false;
            statusText = "提交中...";
            videoResultPath = null;
            string key = PixelDaConfig.GetActiveApiKey(provider);

            PixelDaApi.GenerateVideo(provider, key, baseSrc, videoPrompt, videoNegative,
                ResolutionOptions[videoResolutionIndex],
                (success, url, error) =>
                {
                    if (success)
                    {
                        statusText = "下载视频...";
                        string folder = PixelDaConfig.GetOutputFolderAbsolute("videos");
                        string fileName = $"vid_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                        string savePath = Path.Combine(folder, fileName);
                        PixelDaApi.DownloadFile(url, savePath, (ok, result) =>
                        {
                            if (ok)
                            {
                                videoResultPath = savePath;
                                RefreshAndFinish("视频生成完成", savePath);
                            }
                            else
                            {
                                FinishWithError("下载视频失败: " + result);
                            }
                        });
                    }
                    else
                    {
                        FinishWithError(error);
                    }
                },
                msg => { statusText = msg; Repaint(); },
                () => cancelFlag);
        }

        #endregion

        #region 视频抽帧页

        /// <summary>
        /// 绘制视频抽帧页
        /// </summary>
        private void DrawFramesTab()
        {
            BeginCard();
            Section("视频抽帧 / 去背景 / 精灵表");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("视频文件", GUILayout.Width(60));
            framesVideoPath = EditorGUILayout.TextField(framesVideoPath);
            if (GUILayout.Button("浏览", GUILayout.Width(50)))
            {
                string p = EditorUtility.OpenFilePanel("选择视频", PixelDaConfig.GetOutputFolderAbsolute("videos"), "mp4");
                if (!string.IsNullOrEmpty(p)) framesVideoPath = p;
            }
            EditorGUILayout.EndHorizontal();

            framesFrom = EditorGUILayout.FloatField("起始时间(秒)", framesFrom);
            framesTo = EditorGUILayout.FloatField("结束时间(秒)", framesTo);
            framesCount = EditorGUILayout.IntSlider("帧数", framesCount, 1, 30);
            framesRemoveBg = EditorGUILayout.Toggle("去纯色背景", framesRemoveBg);

            if (!PixelDaFrameUtil.IsFfmpegAvailable())
            {
                EditorGUILayout.HelpBox("未检测到 ffmpeg，抽帧需要 ffmpeg。请在「设置」页配置 ffmpeg 路径或将其加入系统 PATH。", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            EditorGUI.BeginDisabledGroup(isBusy);
            if (AccentButton("抽取帧")) StartExtractFrames();
            EditorGUI.EndDisabledGroup();
            EndCard();

            if (frameTextures.Count > 0)
            {
                BeginCard();
                Section($"已抽取 {frameTextures.Count} 帧");
                DrawFrameGrid();
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (AccentButton("合成精灵表(PNG)", 26)) MergeSprite();
                if (AccentButton("导出 ZIP", 26, Ok)) ExportZip();
                EditorGUILayout.EndHorizontal();
                EndCard();
            }
        }

        /// <summary>
        /// 绘制帧缩略图网格
        /// </summary>
        private void DrawFrameGrid()
        {
            framesScroll = EditorGUILayout.BeginScrollView(framesScroll, GUILayout.Height(140));
            EditorGUILayout.BeginHorizontal();
            foreach (var tex in frameTextures)
            {
                if (tex == null) continue;
                Rect r = GUILayoutUtility.GetRect(96, 96, GUILayout.Width(96), GUILayout.Height(96));
                EditorGUI.DrawTextureTransparent(r, tex, ScaleMode.ScaleToFit);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 启动抽帧任务
        /// </summary>
        private void StartExtractFrames()
        {
            if (string.IsNullOrEmpty(framesVideoPath) || !File.Exists(framesVideoPath))
            {
                EditorUtility.DisplayDialog("PixelDa", "请选择有效的视频文件", "确定");
                return;
            }
            isBusy = true;
            statusText = "抽帧中...";
            ClearFrameTextures();
            string outDir = PixelDaConfig.GetOutputFolderAbsolute("frames/" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            PixelDaFrameUtil.ExtractFramesAsync(framesVideoPath, framesFrom, framesTo, framesCount, outDir,
                (success, paths, error) =>
                {
                    if (success)
                    {
                        framePaths = paths;
                        LoadFrameTextures();
                        RefreshAndFinish($"抽取完成，共 {paths.Count} 帧", outDir);
                    }
                    else
                    {
                        FinishWithError(error);
                    }
                });
        }

        /// <summary>
        /// 把帧文件加载为预览纹理（按需去背景）
        /// </summary>
        private void LoadFrameTextures()
        {
            ClearFrameTextures();
            foreach (string p in framePaths)
            {
                Texture2D tex = PixelDaImageUtil.LoadTextureFromFile(p);
                if (tex == null) continue;
                if (framesRemoveBg)
                {
                    Texture2D removed = PixelDaImageUtil.RemoveSolidBackground(tex);
                    DestroyImmediate(tex);
                    tex = removed;
                }
                frameTextures.Add(tex);
            }
        }

        /// <summary>
        /// 合成精灵表并保存
        /// </summary>
        private void MergeSprite()
        {
            try
            {
                Texture2D sprite = PixelDaImageUtil.MergeFramesToSprite(frameTextures);
                string folder = PixelDaConfig.GetOutputFolderAbsolute("sprites");
                string path = Path.Combine(folder, $"sprite_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                PixelDaImageUtil.SavePng(sprite, path);
                DestroyImmediate(sprite);
                RefreshAndFinish("精灵表已保存", path);
            }
            catch (Exception e)
            {
                FinishWithError(e.Message);
            }
        }

        /// <summary>
        /// 把帧（按需去背景后）导出为 zip
        /// </summary>
        private void ExportZip()
        {
            try
            {
                List<string> sources = framePaths;
                // 去背景时先把处理后的纹理另存
                if (framesRemoveBg)
                {
                    string tmpDir = PixelDaConfig.GetOutputFolderAbsolute("frames_nobg/" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                    sources = new List<string>();
                    for (int i = 0; i < frameTextures.Count; i++)
                    {
                        string fp = Path.Combine(tmpDir, $"frame_{i:D4}.png");
                        PixelDaImageUtil.SavePng(frameTextures[i], fp);
                        sources.Add(fp);
                    }
                }
                string folder = PixelDaConfig.GetOutputFolderAbsolute("zips");
                string zipPath = Path.Combine(folder, $"frames_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                PixelDaFrameUtil.ZipFrames(sources, "frame", zipPath);
                RefreshAndFinish("ZIP 已导出", zipPath);
            }
            catch (Exception e)
            {
                FinishWithError(e.Message);
            }
        }

        #endregion

        #region 音乐页

        /// <summary>
        /// 绘制音乐生成页
        /// </summary>
        private void DrawMusicTab()
        {
            BeginCard();
            Section("音乐生成（AI 生成 ABC 记谱 → chiptune 方波合成）");
            musicPrompt = PromptField("描述", musicPrompt, 50);
            musicDuration = EditorGUILayout.IntSlider("时长(秒)", musicDuration, 5, 120);
            musicGenreIndex = EditorGUILayout.Popup("风格", musicGenreIndex, GenreOptions);
            musicTempoIndex = EditorGUILayout.Popup("节奏", musicTempoIndex, TempoOptions);
            musicSeed = EditorGUILayout.IntField("随机种子(-1随机)", musicSeed);
            EditorGUILayout.Space(4);
            EditorGUI.BeginDisabledGroup(isBusy);
            if (AccentButton("生成音乐")) StartMusicGeneration();
            EditorGUI.EndDisabledGroup();
            EndCard();

            if (!string.IsNullOrEmpty(musicAbc))
            {
                BeginCard();
                Section("ABC 记谱（可手动微调后重新合成）");
                musicAbc = EditorGUILayout.TextArea(musicAbc, GUILayout.Height(140));
                if (!string.IsNullOrEmpty(musicComments))
                {
                    EditorGUILayout.HelpBox(musicComments, MessageType.None);
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (AccentButton("合成/重新合成", 26)) SynthesizeMusic();
                EditorGUI.BeginDisabledGroup(musicClip == null);
                if (AccentButton("▶ 试听", 26, Ok)) PlayClip(musicClip);
                if (GUILayout.Button("■ 停止", GUILayout.Height(26))) StopClip();
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(musicSamples == null);
                if (GUILayout.Button("保存 WAV", GUILayout.Height(24))) SaveMusicWav();
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("保存 ABC", GUILayout.Height(24))) SaveMusicAbc();
                EditorGUILayout.EndHorizontal();
                EndCard();
            }
        }

        /// <summary>
        /// 启动音乐生成任务
        /// </summary>
        private void StartMusicGeneration()
        {
            if (!ValidateKey()) return;
            isBusy = true;
            statusText = "生成 ABC...";
            string key = PixelDaConfig.GetActiveApiKey(provider);

            PixelDaApi.GenerateMusic(provider, key, musicPrompt, musicDuration,
                GenreOptions[musicGenreIndex], TempoOptions[musicTempoIndex], musicSeed,
                (success, notation, comments, error) =>
                {
                    if (success)
                    {
                        musicAbc = notation;
                        musicComments = comments;
                        SynthesizeMusic();
                        isBusy = false;
                        statusText = "音乐生成完成，可试听";
                        Repaint();
                    }
                    else
                    {
                        FinishWithError(error);
                    }
                },
                msg => { statusText = msg; Repaint(); });
        }

        /// <summary>
        /// 合成方波音频
        /// </summary>
        private void SynthesizeMusic()
        {
            try
            {
                musicSamples = PixelDaMusicUtil.Synthesize(musicAbc, out musicSampleRate);
                musicClip = PixelDaMusicUtil.SynthesizeToClip(musicAbc, "PixelDaMusic");
                statusText = "合成完成";
            }
            catch (Exception e)
            {
                FinishWithError("合成失败: " + e.Message);
            }
        }

        /// <summary>
        /// 保存 WAV 到输出目录
        /// </summary>
        private void SaveMusicWav()
        {
            string folder = PixelDaConfig.GetOutputFolderAbsolute("music");
            string path = Path.Combine(folder, $"music_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
            PixelDaMusicUtil.SaveWav(musicSamples, musicSampleRate, path);
            RefreshAndFinish("WAV 已保存", path);
        }

        /// <summary>
        /// 保存 ABC 源文件
        /// </summary>
        private void SaveMusicAbc()
        {
            string folder = PixelDaConfig.GetOutputFolderAbsolute("music");
            string path = Path.Combine(folder, $"music_{DateTime.Now:yyyyMMdd_HHmmss}.abc");
            PixelDaMusicUtil.SaveAbc(musicAbc, path);
            RefreshAndFinish("ABC 已保存", path);
        }

        #endregion

        #region 历史页

        /// <summary>
        /// 绘制历史页
        /// </summary>
        private void DrawHistoryTab()
        {
            EditorGUILayout.BeginHorizontal();
            Section("生成历史　" + PixelDaConfig.OutputFolder);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新", GUILayout.Width(60))) RefreshHistory();
            EditorGUILayout.EndHorizontal();
            historyFilter = GUILayout.Toolbar(historyFilter, HistoryFilters, tabStyle, GUILayout.Height(24));
            EditorGUILayout.Space(2);

            if (historyFiles.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无历史，点击「刷新」扫描输出目录。", MessageType.Info);
                return;
            }

            historyScroll = EditorGUILayout.BeginScrollView(historyScroll, GUILayout.Height(360));
            foreach (string file in historyFiles)
            {
                if (!PassHistoryFilter(file)) continue;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                if (IsImage(file))
                {
                    Texture2D thumb = GetHistoryThumb(file);
                    Rect r = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
                    if (thumb != null) EditorGUI.DrawTextureTransparent(r, thumb, ScaleMode.ScaleToFit);
                }
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(Path.GetFileName(file));
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("定位", GUILayout.Width(60))) PingFileAsset(file);
                if (GUILayout.Button("打开目录", GUILayout.Width(80))) EditorUtility.RevealInFinder(file);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 重新扫描输出目录
        /// </summary>
        private void RefreshHistory()
        {
            historyFiles.Clear();
            string root = PixelDaConfig.GetOutputFolderAbsolute();
            if (Directory.Exists(root))
            {
                foreach (string f in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(f).ToLower();
                    if (ext == ".png" || ext == ".jpg" || ext == ".mp4" || ext == ".wav" || ext == ".abc" || ext == ".zip")
                    {
                        historyFiles.Add(f);
                    }
                }
                historyFiles.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
            }
            statusText = $"历史共 {historyFiles.Count} 个文件";
        }

        /// <summary>
        /// 历史过滤判断
        /// </summary>
        private bool PassHistoryFilter(string file)
        {
            switch (historyFilter)
            {
                case 1: return IsImage(file);
                case 2: return Path.GetExtension(file).ToLower() == ".mp4";
                case 3: { string e = Path.GetExtension(file).ToLower(); return e == ".wav" || e == ".abc"; }
                default: return true;
            }
        }

        /// <summary>
        /// 是否图片文件
        /// </summary>
        private bool IsImage(string file)
        {
            string e = Path.GetExtension(file).ToLower();
            return e == ".png" || e == ".jpg";
        }

        /// <summary>
        /// 获取历史缩略图（缓存）
        /// </summary>
        private Texture2D GetHistoryThumb(string file)
        {
            if (historyThumbs.TryGetValue(file, out Texture2D tex) && tex != null) return tex;
            tex = PixelDaImageUtil.LoadTextureFromFile(file);
            historyThumbs[file] = tex;
            return tex;
        }

        #endregion

        #region 设置页

        /// <summary>
        /// 绘制设置页
        /// </summary>
        private void DrawSettingsTab()
        {
            BeginCard();
            Section("API Key（保存在项目内，随 git 共享给团队）");
            PixelDaConfig.DoubaoApiKey = EditorGUILayout.PasswordField("豆包 API Key", PixelDaConfig.DoubaoApiKey);
            PixelDaConfig.TongyiApiKey = EditorGUILayout.PasswordField("通义 API Key", PixelDaConfig.TongyiApiKey);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("配置文件", PixelDaConfig.PROJECT_CONFIG_PATH, EditorStyles.miniLabel);
            if (GUILayout.Button("重新加载", GUILayout.Width(80)))
            {
                PixelDaConfig.ReloadProjectData();
                statusText = "已从项目配置文件重新加载";
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "API Key / 端点 / 模型 / 尺寸 / 输出目录 保存在上述项目文件中，提交后其他成员拉取即可直接使用。\n" +
                "ffmpeg 路径与当前提供商选择属机器/个人设置，不入库。\n" +
                "⚠ 注意：密钥会进入 git 历史，请确保仓库为私有，避免泄露。",
                MessageType.Warning);
            EndCard();

            BeginCard();
            Section("输出与工具");
            PixelDaConfig.OutputFolder = EditorGUILayout.TextField("输出目录(相对项目)", PixelDaConfig.OutputFolder);
            EditorGUILayout.BeginHorizontal();
            PixelDaConfig.FfmpegPath = EditorGUILayout.TextField("ffmpeg 路径(留空走PATH)", PixelDaConfig.FfmpegPath);
            if (GUILayout.Button("浏览", GUILayout.Width(50)))
            {
                string p = EditorUtility.OpenFilePanel("选择 ffmpeg", "", "exe");
                if (!string.IsNullOrEmpty(p)) PixelDaConfig.FfmpegPath = p;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("检测 ffmpeg"))
            {
                bool ok = PixelDaFrameUtil.IsFfmpegAvailable();
                EditorUtility.DisplayDialog("PixelDa", ok ? "ffmpeg 可用 ✔" : "未检测到 ffmpeg ✘", "确定");
            }
            EditorGUILayout.EndHorizontal();
            EndCard();

            BeginCard();
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "高级：端点与模型名", true);
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("豆包", EditorStyles.miniBoldLabel);
                PixelDaConfig.DoubaoBaseUrl = EditorGUILayout.TextField("Base URL", PixelDaConfig.DoubaoBaseUrl);
                PixelDaConfig.DoubaoImageModel = EditorGUILayout.TextField("图片模型", PixelDaConfig.DoubaoImageModel);
                PixelDaConfig.DoubaoVideoModel = EditorGUILayout.TextField("视频模型", PixelDaConfig.DoubaoVideoModel);
                PixelDaConfig.DoubaoChatModel = EditorGUILayout.TextField("聊天模型", PixelDaConfig.DoubaoChatModel);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("通义", EditorStyles.miniBoldLabel);
                PixelDaConfig.TongyiBaseUrl = EditorGUILayout.TextField("Base URL", PixelDaConfig.TongyiBaseUrl);
                PixelDaConfig.TongyiChatBaseUrl = EditorGUILayout.TextField("聊天 Base URL", PixelDaConfig.TongyiChatBaseUrl);
                PixelDaConfig.TongyiImageModel = EditorGUILayout.TextField("文生图模型", PixelDaConfig.TongyiImageModel);
                PixelDaConfig.TongyiEditModel = EditorGUILayout.TextField("图编辑模型", PixelDaConfig.TongyiEditModel);
                PixelDaConfig.TongyiVideoModel = EditorGUILayout.TextField("视频模型", PixelDaConfig.TongyiVideoModel);
                PixelDaConfig.TongyiChatModel = EditorGUILayout.TextField("聊天模型", PixelDaConfig.TongyiChatModel);

                if (GUILayout.Button("恢复默认端点/模型"))
                {
                    ResetEndpoints();
                }
                EditorGUI.indentLevel--;
            }
            EndCard();

            EditorGUILayout.HelpBox(
                "纯 C# 实现说明：\n" +
                "· 去背景为「纯色背景剔除」(适配纯色背景像素图)，非原工具的 rembg AI 抠图。\n" +
                "· 抽帧依赖系统 ffmpeg。\n" +
                "· 音乐用方波合成 ABC 记谱模拟 chiptune，非原工具的 8bit 音色库渲染。\n" +
                "· 输出统一存放在「输出目录」下并自动导入工程。",
                MessageType.Info);
        }

        /// <summary>
        /// 恢复默认端点与模型名
        /// </summary>
        private void ResetEndpoints()
        {
            PixelDaConfig.DoubaoBaseUrl = PixelDaConfig.DEFAULT_DOUBAO_BASE_URL;
            PixelDaConfig.DoubaoImageModel = PixelDaConfig.DEFAULT_DOUBAO_IMAGE_MODEL;
            PixelDaConfig.DoubaoVideoModel = PixelDaConfig.DEFAULT_DOUBAO_VIDEO_MODEL;
            PixelDaConfig.DoubaoChatModel = PixelDaConfig.DEFAULT_DOUBAO_CHAT_MODEL;
            PixelDaConfig.TongyiBaseUrl = PixelDaConfig.DEFAULT_TONGYI_BASE_URL;
            PixelDaConfig.TongyiChatBaseUrl = PixelDaConfig.DEFAULT_TONGYI_CHAT_BASE_URL;
            PixelDaConfig.TongyiImageModel = PixelDaConfig.DEFAULT_TONGYI_IMAGE_MODEL;
            PixelDaConfig.TongyiEditModel = PixelDaConfig.DEFAULT_TONGYI_EDIT_MODEL;
            PixelDaConfig.TongyiVideoModel = PixelDaConfig.DEFAULT_TONGYI_VIDEO_MODEL;
            PixelDaConfig.TongyiChatModel = PixelDaConfig.DEFAULT_TONGYI_CHAT_MODEL;
        }

        #endregion

        #region 公共辅助

        /// <summary>
        /// 校验当前提供商是否已配置 API Key
        /// </summary>
        private bool ValidateKey()
        {
            if (string.IsNullOrEmpty(PixelDaConfig.GetActiveApiKey(provider)))
            {
                EditorUtility.DisplayDialog("PixelDa", "请先在「设置」页配置 API Key", "去设置");
                tabIndex = 6;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 下载远程图片，保存到输出目录并生成预览纹理，回传纹理与本地保存路径
        /// </summary>
        private void DownloadAndPreviewImage(string url, string subFolder, string prefix, Action<Texture2D, string> assign)
        {
            statusText = "下载图片...";
            string folder = PixelDaConfig.GetOutputFolderAbsolute(subFolder);
            string fileName = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string savePath = Path.Combine(folder, fileName);
            PixelDaApi.DownloadFile(url, savePath, (ok, result) =>
            {
                if (ok)
                {
                    Texture2D tex = PixelDaImageUtil.LoadTextureFromFile(savePath);
                    assign(tex, savePath);
                    RefreshAndFinish("生成完成", savePath);
                }
                else
                {
                    FinishWithError("下载图片失败: " + result);
                }
            });
        }

        /// <summary>
        /// 绘制「图片源」控件：缩略图 + 拖拽区(支持工程资源/外部文件) + ObjectField + 远程 URL 兜底。
        /// 本地图片优先；提交时由 <see cref="ResolveImageSource"/> 编码为 base64 data URI 发给 AI。
        /// </summary>
        private void DrawImageSourceField(string label, ref Texture2D tex, ref string localPath, ref string url)
        {
            Section(label + "（拖拽工程图片到此，或填远程 URL）");
            EditorGUILayout.BeginHorizontal();

            // 左侧缩略图
            Rect prev = GUILayoutUtility.GetRect(72, 72, GUILayout.Width(72), GUILayout.Height(72));
            if (tex != null) EditorGUI.DrawTextureTransparent(prev, tex, ScaleMode.ScaleToFit);
            else GUI.Box(prev, "无图", EditorStyles.helpBox);

            // 右侧：拖拽区 + 资源选择
            EditorGUILayout.BeginVertical();
            Rect drop = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));
            GUI.Box(drop, tex != null ? "已选择本地图片（再次拖拽可替换）" : "把图片拖到这里", previewBoxStyle);
            HandleImageDrop(drop, ref tex, ref localPath);

            Texture2D newTex = (Texture2D)EditorGUILayout.ObjectField(tex, typeof(Texture2D), false);
            if (newTex != tex)
            {
                tex = newTex;
                localPath = newTex != null ? AssetToAbsolutePath(newTex) : null;
                if (newTex != null) url = "";
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (tex != null && GUILayout.Button("清除本地图片", GUILayout.Width(100), GUILayout.Height(18)))
            {
                tex = null;
                localPath = null;
            }
            GUILayout.Label(tex != null ? "（使用本地图片）" : "远程 URL：", EditorStyles.miniLabel, GUILayout.Width(90));
            using (new EditorGUI.DisabledScope(tex != null))
            {
                url = EditorGUILayout.TextField(url);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 处理拖拽：工程内 Texture2D/Sprite 资源 或 外部图片文件
        /// </summary>
        private void HandleImageDrop(Rect area, ref Texture2D tex, ref string localPath)
        {
            Event e = Event.current;
            if (!area.Contains(e.mousePosition)) return;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type != EventType.DragPerform) return;

            DragAndDrop.AcceptDrag();
            bool got = false;

            // 工程内资源（Texture2D / Sprite）
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Texture2D t)
                {
                    tex = t; localPath = AssetToAbsolutePath(t); got = true; break;
                }
                if (obj is Sprite sp && sp.texture != null)
                {
                    tex = sp.texture; localPath = AssetToAbsolutePath(sp); got = true; break;
                }
            }

            // 外部图片文件
            if (!got && DragAndDrop.paths != null)
            {
                foreach (var p in DragAndDrop.paths)
                {
                    string ext = Path.GetExtension(p).ToLower();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".webp")
                    {
                        localPath = p;
                        tex = PixelDaImageUtil.LoadTextureFromFile(p);
                        got = true;
                        break;
                    }
                }
            }

            e.Use();
            Repaint();
        }

        /// <summary>
        /// 解析最终图片源：本地图片(路径/纹理)→base64 data URI，否则用远程 URL
        /// </summary>
        private string ResolveImageSource(Texture2D tex, string localPath, string url)
        {
            if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
            {
                return PixelDaImageUtil.FileToDataUri(localPath);
            }
            if (tex != null)
            {
                return PixelDaImageUtil.TextureToDataUri(tex);
            }
            return url;
        }

        /// <summary>
        /// 把「上次/当前生成结果」设为图片源：优先用已落盘的本地文件，否则退回远程 URL
        /// </summary>
        private void SetSourceFromGenerated(ref Texture2D tex, ref string localPath, ref string url)
        {
            if (!string.IsNullOrEmpty(imgResultPath) && File.Exists(imgResultPath))
            {
                tex = PixelDaImageUtil.LoadTextureFromFile(imgResultPath);
                localPath = imgResultPath;
                url = "";
            }
            else
            {
                tex = null;
                localPath = null;
                url = imgResultUrl;
            }
        }

        /// <summary>
        /// 取工程资源对应的磁盘绝对路径（非工程资源返回 null）
        /// </summary>
        private static string AssetToAbsolutePath(UnityEngine.Object obj)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath)) return null;
            return Path.Combine(PixelDaConfig.GetProjectRoot(), assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// 绘制纹理预览（按比例缩放）
        /// </summary>
        private void DrawTexturePreview(Texture2D tex, float maxSize)
        {
            if (tex == null) return;
            float aspect = (float)tex.width / tex.height;
            float w = maxSize, h = maxSize;
            if (aspect > 1f) h = maxSize / aspect;
            else w = maxSize * aspect;
            // 用居中的预览框包裹，附尺寸信息
            EditorGUILayout.BeginVertical(previewBoxStyle, GUILayout.Width(w + 16));
            Rect r = GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(false));
            EditorGUI.DrawTextureTransparent(r, tex, ScaleMode.ScaleToFit);
            EditorGUILayout.LabelField($"{tex.width} × {tex.height}", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 刷新资源并结束任务，定位到生成的资源
        /// </summary>
        private void RefreshAndFinish(string msg, string absolutePath)
        {
            AssetDatabase.Refresh();
            PingFileAsset(absolutePath);
            isBusy = false;
            statusText = msg;
            Repaint();
        }

        /// <summary>
        /// 以错误结束任务
        /// </summary>
        private void FinishWithError(string error)
        {
            isBusy = false;
            statusText = "❌ " + error;
            Debug.LogError("[PixelDa] " + error);
            Repaint();
        }

        /// <summary>
        /// 把本地文件（须在 Assets 下）在工程视图中定位
        /// </summary>
        private void PingFileAsset(string absolutePath)
        {
            string assetPath = PixelDaConfig.ToUnityAssetPath(absolutePath);
            if (string.IsNullOrEmpty(assetPath)) return;
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (obj != null) EditorGUIUtility.PingObject(obj);
        }

        /// <summary>
        /// 释放帧预览纹理
        /// </summary>
        private void ClearFrameTextures()
        {
            foreach (var t in frameTextures)
            {
                if (t != null) DestroyImmediate(t);
            }
            frameTextures.Clear();
        }

        #endregion

        #region 编辑器内音频播放

        /// <summary>
        /// 在编辑器内试听 AudioClip（反射调用内部 AudioUtil，兼容多版本）
        /// </summary>
        private static void PlayClip(AudioClip clip)
        {
            if (clip == null) return;
            Assembly asm = typeof(AudioImporter).Assembly;
            Type audioUtil = asm.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null) return;

            MethodInfo m = audioUtil.GetMethod("PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            if (m != null)
            {
                m.Invoke(null, new object[] { clip, 0, false });
                return;
            }
            m = audioUtil.GetMethod("PlayClip",
                BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(AudioClip) }, null);
            m?.Invoke(null, new object[] { clip });
        }

        /// <summary>
        /// 停止编辑器内试听
        /// </summary>
        private static void StopClip()
        {
            Assembly asm = typeof(AudioImporter).Assembly;
            Type audioUtil = asm.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null) return;
            MethodInfo m = audioUtil.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)
                           ?? audioUtil.GetMethod("StopAllClips", BindingFlags.Static | BindingFlags.Public);
            m?.Invoke(null, null);
        }

        #endregion
    }
}
