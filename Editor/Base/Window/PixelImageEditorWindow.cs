using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelImageEditor
{
    /// <summary>
    /// 像素图编辑器（Pixel Image Editor）。菜单：Custom/工具弹窗/像素图编辑器
    ///
    /// 把 AI 生成的“伪像素画”重采样为真正像素对齐的像素图，并提供编辑、导出与颜色调节。
    /// 类拆分为多个 partial：本文件为核心（枚举/常量/共享字段/生命周期/菜单/快捷键/横幅步骤条/
    /// 步骤切换与源图载入/颜色工具/UI 辅助/拖拽读取），各步骤逻辑分别在 Step1~Step5.cs：
    ///
    /// 步骤①（设置）：选择网格宽/高 + 源图片。
    /// 步骤②（定位与转换）：拖拽定位/取色算法/智能网格检测，生成像素图。
    /// 步骤③（编辑与导出）：画笔/橡皮/魔棒编辑、调色板换色、撤销重做、PNG 导出。
    /// 步骤④（辅助功能）：帧排版重排 / 多图合并图集。
    /// 步骤⑤（像素图颜色调节）：并排载入两张图，各自提取调色板全局换色，分别覆盖/同目录导出。
    ///
    /// 快捷键：Alt+B 切棋盘背景明暗；Alt+G 切网格线明暗；Ctrl+Z 撤销；Ctrl+Y 重做。
    /// </summary>
    public partial class PixelImageEditorWindow : EditorWindow
    {
        #region 枚举

        /// <summary>取色（转换）算法，与原工具的 most / most_light / most_dark / average / neighbor 一一对应。</summary>
        private enum ConvMethod
        {
            /// <summary>最常用颜色：格内相近色聚类后取主簇中使用最多的颜色。</summary>
            Most = 0,
            /// <summary>最常用颜色（偏亮）：按亮度加权聚类，偏向更亮的颜色。</summary>
            MostLight = 1,
            /// <summary>最常用颜色（偏暗）：按亮度加权聚类，偏向更暗的颜色。</summary>
            MostDark = 2,
            /// <summary>平均颜色：格内所有非透明像素 RGB 的算术平均。</summary>
            Average = 3,
            /// <summary>邻域颜色：在格子四周各外扩 25% 后再做平均，过渡更柔和。</summary>
            Neighbor = 4,
        }

        /// <summary>最终颜色数量限制模式。</summary>
        private enum ColorLimitMode
        {
            /// <summary>以下：生成图颜色数不超过上限；原图色数已≤上限则保持实际色数不做量化。</summary>
            AtMost = 0,
            /// <summary>固定：颜色超过上限时精确量化到该数量（补足空簇尽量凑满）；原图色数不足时无法造色，仍按实际色数输出。</summary>
            Exact = 1,
        }

        /// <summary>步骤③的编辑工具。</summary>
        private enum EditTool
        {
            /// <summary>画笔：以当前颜色按笔刷尺寸方形涂格。</summary>
            Brush = 0,
            /// <summary>橡皮：把格子设为透明。</summary>
            Eraser = 1,
            /// <summary>魔棒：4 向洪水填充把相近色区域抹成透明。</summary>
            MagicWand = 2,
        }

        /// <summary>步骤④（辅助功能）的子模式，用顶部页签切换。</summary>
        private enum AuxMode
        {
            /// <summary>帧排版：把一张精灵表按列×行重排（拆分/换布局）。</summary>
            Relayout = 0,
            /// <summary>图片合并：把多张单图按列×行拼成一张图集。</summary>
            Merge = 1,
        }

        #endregion

        #region 常量

        /// <summary>网格宽/高可选档位（像素），与原工具下拉项一致。</summary>
        private static readonly int[] kGridOptions = { 16, 32, 48, 64, 80, 96, 112, 128, 256, 512, 1024 };

        /// <summary>源图最大边长（超过则等比缩小），对应原工具的 maxSize = 1024。</summary>
        private const int kMaxImageSize = 1024;

        /// <summary>聚类相似度阈值默认值（原工具硬编码为 30）。</summary>
        private const int kDefaultSimilarityThreshold = 30;

        /// <summary>透明哨兵：alpha 为 0 视为透明格。</summary>
        private static readonly Color32 kTransparent = new Color32(0, 0, 0, 0);

        /// <summary>步骤②预览区在屏幕上的最大显示边长。</summary>
        private const float kPreviewMaxDisp = 460f;

        /// <summary>步骤③编辑区在屏幕上的最大显示边长。</summary>
        private const float kEditMaxDisp = 560f;

        #endregion

        #region 字段 - 流程与源图

        /// <summary>当前步骤（1/2/3）。</summary>
        private int _step = 1;

        /// <summary>网格宽（列数）。</summary>
        private int _gridWidth = 32;
        /// <summary>网格高（行数）。</summary>
        private int _gridHeight = 32;

        /// <summary>步骤①里通过 ObjectField 选择的工程内源纹理。</summary>
        private Texture2D _sourceTexture;
        /// <summary>从系统拖入的外部图片文件绝对路径（若有）。</summary>
        private string _sourceExternalPath = "";

        /// <summary>用于显示的源图纹理（已按 kMaxImageSize 缩小，Point 过滤）。</summary>
        private Texture2D _srcDisplayTex;
        /// <summary>源图自上而下排列的像素（row0 在顶部），供取色采样，坐标系与原工具 getImageData 一致。</summary>
        private Color32[] _srcTopDown;
        /// <summary>源图宽（缩小后）。</summary>
        private int _srcW;
        /// <summary>源图高（缩小后）。</summary>
        private int _srcH;

        #endregion

        #region 字段 - 撤销/重做与外观

        /// <summary>撤销历史栈（每项为一份像素深拷贝）。</summary>
        private readonly List<Color32[]> _historyStack = new List<Color32[]>();
        /// <summary>重做栈。</summary>
        private readonly List<Color32[]> _redoStack = new List<Color32[]>();

        /// <summary>棋盘背景是否为暗色。</summary>
        private bool _isDarkBackground = true;
        /// <summary>网格线是否为亮色（白）。</summary>
        private bool _isDarkGrid = true;

        /// <summary>步骤③左侧是否展开源图缩略图面板。</summary>
        private bool _foldSource = true;

        /// <summary>滚动位置。</summary>
        private Vector2 _scroll;

        #endregion

        #region 样式缓存

        private static GUIStyle _titleStyle;
        private static GUIStyle _subTitleStyle;
        private static GUIStyle _cardHeaderStyle;
        private static GUIStyle _hintStyle;
        private static GUIStyle _warnStyle;
        private static Texture2D _checkerTex;

        private static readonly Color kAccent = new Color(0.26f, 0.59f, 0.98f);
        private static readonly Color kGenColor = new Color(0.30f, 0.70f, 0.45f);

        #endregion

        #region 菜单项与窗口创建

        /// <summary>菜单项：打开像素图编辑器窗口。</summary>
        [MenuItem("Custom/工具弹窗/像素图编辑器")]
        public static void Open()
        {
            var win = GetWindow<PixelImageEditorWindow>("像素图编辑器");
            win.minSize = new Vector2(720, 640);
            win.Show();
        }

        /// <summary>右键菜单项：在 Project 窗口选中一张图片后右键，直接用该图打开转换器并自动载入进入步骤②。</summary>
        [MenuItem("Assets/像素图编辑器", false, 2000)]
        public static void OpenFromSelection()
        {
            var tex = Selection.activeObject as Texture2D;
            var win = GetWindow<PixelImageEditorWindow>("像素图编辑器");
            win.minSize = new Vector2(720, 640);
            win.Show();
            if (tex == null) return;

            win._sourceTexture = tex;
            win._sourceExternalPath = "";
            try
            {
                win.LoadSource();
                win.EnterStep2();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("载入失败", ex.Message, "确定");
                Debug.LogException(ex);
            }
            win.Repaint();
        }

        /// <summary>右键菜单校验：仅当当前选中对象为 Texture2D（图片）时启用该菜单项。</summary>
        [MenuItem("Assets/像素图编辑器", true)]
        private static bool OpenFromSelectionValidate()
        {
            return Selection.activeObject is Texture2D;
        }

        /// <summary>确保 GUIStyle 已初始化。</summary>
        private static void EnsureStyles()
        {
            if (_titleStyle != null) return;
            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            _subTitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
            _cardHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.55f, 0.78f, 1f) }
            };
            _hintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
            _warnStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.35f, 0.35f) }
            };
        }

        #endregion

        #region 生命周期

        /// <summary>窗口主绘制入口：分发顶部横幅、步骤条与各步骤内容，并处理全局快捷键。</summary>
        private void OnGUI()
        {
            EnsureStyles();
            HandleShortcutKeys();

            DrawBanner();
            DrawStepBar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space(4);

            switch (_step)
            {
                case 1: DrawStep1(); break;
                case 2: DrawStep2(); break;
                case 3: DrawStep3(); break;
                case 4: DrawStep4(); break;
                case 5: DrawStep5(); break;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>窗口关闭时释放运行期创建的纹理。</summary>
        private void OnDestroy()
        {
            if (_srcDisplayTex != null) DestroyImmediate(_srcDisplayTex);
            if (_artTex != null) DestroyImmediate(_artTex);
            if (_auxDisplayTex != null) DestroyImmediate(_auxDisplayTex);
            if (_auxResultTex != null) DestroyImmediate(_auxResultTex);
            if (_mergeResultTex != null) DestroyImmediate(_mergeResultTex);
            DisposeColorAdjustImage(_caImageA);
            DisposeColorAdjustImage(_caImageB);
        }

        #endregion

        #region 全局快捷键

        /// <summary>处理 Alt+B / Alt+G 外观切换与 Ctrl+Z / Ctrl+Y 撤销重做。</summary>
        private void HandleShortcutKeys()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;

            // Alt+B：切换棋盘背景明暗
            if (e.alt && e.keyCode == KeyCode.B)
            {
                _isDarkBackground = !_isDarkBackground;
                e.Use();
                Repaint();
                return;
            }
            // Alt+G：切换网格线明暗
            if (e.alt && e.keyCode == KeyCode.G)
            {
                _isDarkGrid = !_isDarkGrid;
                e.Use();
                Repaint();
                return;
            }
            // 仅步骤③响应撤销重做
            if (_step == 3 && (e.control || e.command))
            {
                if (e.keyCode == KeyCode.Z) { Undo(); e.Use(); }
                else if (e.keyCode == KeyCode.Y) { Redo(); e.Use(); }
            }
        }

        #endregion

        #region UI - 横幅与步骤条

        /// <summary>顶部标题横幅。</summary>
        private void DrawBanner()
        {
            Rect r = GUILayoutUtility.GetRect(0, 46, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.20f, 0.22f, 0.28f));
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2, r.width, 2), kAccent);
            GUI.Label(new Rect(r.x + 14, r.y + 5, r.width - 28, 22), "🎮 像素图编辑器 · Pixel Perfect", _titleStyle);
            GUI.Label(new Rect(r.x + 16, r.y + 25, r.width - 28, 16),
                "把 AI 像素画重采样为真正像素对齐的图 · 定位 → 取色转换 → 编辑导出", _subTitleStyle);
        }

        /// <summary>步骤进度条（①设置 ②定位转换 ③编辑导出 ④辅助功能 ⑤颜色调节）。</summary>
        private void DrawStepBar()
        {
            string[] names = { "① 设置", "② 定位与转换", "③ 编辑与导出", "④ 辅助功能", "⑤ 颜色调节" };
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < names.Length; i++)
                {
                    int step = i + 1;
                    bool active = _step == step;
                    // 步骤④/⑤为独立辅助工具，任何时候都可进入
                    bool reachable = step <= _step || step == 4 || step == 5;
                    Color prev = GUI.backgroundColor;
                    GUI.backgroundColor = active ? kAccent : (reachable ? new Color(0.4f, 0.45f, 0.5f) : new Color(0.3f, 0.3f, 0.3f));
                    using (new EditorGUI.DisabledScope(!CanGoToStep(step)))
                    {
                        if (GUILayout.Button(names[i], GUILayout.Height(24)))
                            _step = step;
                    }
                    GUI.backgroundColor = prev;
                }
            }
            DrawSeparator();
        }

        /// <summary>判断能否跳转到指定步骤（需满足前置条件）。</summary>
        private bool CanGoToStep(int step)
        {
            if (step == 1) return true;
            if (step == 2) return _srcTopDown != null;
            if (step == 3) return _pixels != null;
            if (step == 4) return true; // 辅助功能（帧动画排版），独立于主流程，随时可进入
            if (step == 5) return true; // 像素图颜色调节，独立于主流程，随时可进入
            return false;
        }

        #endregion

        #region 步骤切换 / 源图载入

        /// <summary>进入步骤②：按当前网格与格子尺寸居中适配源图。</summary>
        private void EnterStep2()
        {
            _step = 2;
            RefitImage();
        }

        /// <summary>进入步骤③：构建编辑纹理并初始化历史栈。</summary>
        private void EnterStep3()
        {
            _step = 3;
            _canvasZoom = 1;
            _tool = EditTool.Brush;
            _hoverI = _hoverJ = -1;
            BuildArtTexture();
            _historyStack.Clear();
            _redoStack.Clear();
            SaveHistory();
            ExtractPalette();
            _paletteDirty = false;
        }

        /// <summary>把源图在当前画布中居中适配（重置缩放与偏移）。</summary>
        private void RefitImage()
        {
            if (_srcTopDown == null) return;
            float canvasW = _gridWidth * _previewCellSize;
            float canvasH = _gridHeight * _previewCellSize;
            _imageScale = Mathf.Min(canvasW / _srcW, canvasH / _srcH);
            _offsetX = (canvasW - _srcW * _imageScale) / 2f;
            _offsetY = (canvasH - _srcH * _imageScale) / 2f;
            _zoomPercent = 100f; // 与原工具一致：滑杆默认回到 100
            Repaint();
        }

        /// <summary>应用缩放滑杆：scale = 百分比/100，并以画布中心为锚调整偏移。</summary>
        private void ApplyZoom(float percent)
        {
            if (_srcTopDown == null) return;
            float canvasW = _gridWidth * _previewCellSize;
            float canvasH = _gridHeight * _previewCellSize;
            float newScale = percent / 100f;
            float centerX = canvasW / 2f;
            float centerY = canvasH / 2f;
            float centerOrigX = (centerX - _offsetX) / _imageScale;
            float centerOrigY = (centerY - _offsetY) / _imageScale;
            _imageScale = newScale;
            _offsetX = centerX - centerOrigX * _imageScale;
            _offsetY = centerY - centerOrigY * _imageScale;
            Repaint();
        }

        /// <summary>读取源图像素并按 kMaxImageSize 等比缩小，准备显示纹理与采样数组。</summary>
        private void LoadSource()
        {
            Color32[] raw;
            int rw, rh;
            ReadSourcePixels(_sourceTexture, _sourceExternalPath, out raw, out rw, out rh);

            // 缩小到最大边长
            int w = rw, h = rh;
            if (w > kMaxImageSize || h > kMaxImageSize)
            {
                float f = Mathf.Min((float)kMaxImageSize / w, (float)kMaxImageSize / h);
                w = Mathf.Max(1, Mathf.RoundToInt(w * f));
                h = Mathf.Max(1, Mathf.RoundToInt(h * f));
            }

            // raw 来自 GetPixels32：自下而上(row0 在底部)。转成自上而下并缩放（最近邻）。
            Color32[] topDown = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int sy = Mathf.Min(rh - 1, Mathf.FloorToInt((y + 0.5f) / h * rh)); // y 自上而下
                int srcRowBottomUp = rh - 1 - sy;
                for (int x = 0; x < w; x++)
                {
                    int sx = Mathf.Min(rw - 1, Mathf.FloorToInt((x + 0.5f) / w * rw));
                    topDown[y * w + x] = raw[srcRowBottomUp * rw + sx];
                }
            }

            _srcTopDown = topDown;
            _srcW = w;
            _srcH = h;

            // 显示纹理（自上而下数组 → Texture2D 需翻回自下而上）
            if (_srcDisplayTex != null) DestroyImmediate(_srcDisplayTex);
            _srcDisplayTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Color32[] disp = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    disp[(h - 1 - y) * w + x] = topDown[y * w + x];
            _srcDisplayTex.SetPixels32(disp);
            _srcDisplayTex.Apply();

            // 载入时统计一次源图颜色数并缓存，供步骤② UI 显示（避免每帧 OnGUI 重算，性能优化）
            ComputeSourceColorStats();
        }

        /// <summary>
        /// 统计源图不同颜色数并缓存：精确色数(按 RGB) 与近似色数(每通道量化到 32 级，合并抗锯齿/渐变相近色)。
        /// 仅在 LoadSource 时调用一次；全透明像素不计入。
        /// </summary>
        private void ComputeSourceColorStats()
        {
            _srcColorCountExact = -1;
            _srcColorCountApprox = -1;
            if (_srcTopDown == null) return;
            var exact = new HashSet<int>();
            var approx = new HashSet<int>();
            for (int k = 0; k < _srcTopDown.Length; k++)
            {
                Color32 c = _srcTopDown[k];
                if (c.a == 0) continue; // 透明像素不计入颜色统计
                exact.Add((c.r << 16) | (c.g << 8) | c.b);
                // 每通道右移 3 位(保留高 5 位=32 级)，把抗锯齿/渐变产生的相近色并到同一近似色
                approx.Add(((c.r >> 3) << 10) | ((c.g >> 3) << 5) | (c.b >> 3));
            }
            _srcColorCountExact = exact.Count;
            _srcColorCountApprox = approx.Count;
        }

        /// <summary>重置全部状态并回到步骤①。</summary>
        private void ResetAll()
        {
            _step = 1;
            _gridWidth = 16; _gridHeight = 16;
            _sourceTexture = null; _sourceExternalPath = "";
            if (_srcDisplayTex != null) { DestroyImmediate(_srcDisplayTex); _srcDisplayTex = null; }
            _srcTopDown = null; _srcW = _srcH = 0;
            _previewCellSize = 4; _zoomPercent = 100f; _imageScale = 1f; _offsetX = _offsetY = 0f;
            _method = ConvMethod.Most;
            _similarityThreshold = kDefaultSimilarityThreshold; _limitColors = false; _maxColors = 16; _colorLimitMode = ColorLimitMode.AtMost;
            _srcColorCountExact = _srcColorCountApprox = -1;
            _autoUseRefine = true; _refineIntensity = 0.25f; _autoFixSquare = true; _autoMessage = "";
            _pixels = null;
            if (_artTex != null) { DestroyImmediate(_artTex); _artTex = null; }
            _canvasZoom = 1; _resultZoom = 6; _resultScroll = Vector2.zero;
            _drawColor = Color.black; _tool = EditTool.Brush; _brushSize = 1;
            _magicWandThreshold = 5; _showGrid = true;
            _exportScale = 1; _splitCols = 2; _splitRows = 2;
            _recentColors.Clear();
            _palette.Clear(); _paletteCounts.Clear(); _pixelPaletteIndices = null;
            _paletteDirty = false; _palettePendingHistory = false;
            _historyStack.Clear(); _redoStack.Clear();
            _hoverI = _hoverJ = -1; _drawingActive = false; _drawingOccurred = false;
            Repaint();
        }

        #endregion

        #region 颜色工具

        /// <summary>把颜色打包为 0xRRGGBB 整数。</summary>
        private static int PackRGB(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;

        /// <summary>把 0xRRGGBB 整数解包为不透明 Color32。</summary>
        private static Color32 UnpackRGB(int v) => new Color32((byte)((v >> 16) & 0xFF), (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF), 255);

        /// <summary>把颜色打包为 0xRRGGBBAA 无符号整数（含透明度，用于调色板按完整颜色区分）。</summary>
        private static uint PackRGBA(Color32 c) => ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a;

        /// <summary>两色的欧氏 RGB 距离。</summary>
        private static float ColorDistance(Color32 a, Color32 b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        #endregion

        #region UI 辅助

        /// <summary>开始一个带标题的卡片容器。</summary>
        private void BeginCard(string title)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, _cardHeaderStyle);
            DrawSeparator();
            EditorGUILayout.Space(2);
        }

        /// <summary>结束卡片容器。</summary>
        private void EndCard()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        /// <summary>画一条横向分隔线。</summary>
        private static void DrawSeparator()
        {
            Rect r = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(1, 1, 1, 0.08f));
        }

        /// <summary>在指定矩形内绘制 gridW×gridH 的网格线。</summary>
        private static void DrawGridLines(Rect area, int gw, int gh, Color color)
        {
            float cw = area.width / gw;
            float ch = area.height / gh;
            // 列数过多时不逐线画（避免编辑器卡顿与视觉糊成一片）
            if (cw >= 2f)
                for (int i = 1; i < gw; i++)
                    EditorGUI.DrawRect(new Rect(area.x + i * cw, area.y, 1, area.height), color);
            if (ch >= 2f)
                for (int j = 1; j < gh; j++)
                    EditorGUI.DrawRect(new Rect(area.x, area.y + j * ch, area.width, 1), color);
        }

        /// <summary>画矩形描边（1px）。</summary>
        private static void DrawRectOutline(Rect r, Color color)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), color);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1, r.width, 1), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1, r.height), color);
            EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y, 1, r.height), color);
        }

        /// <summary>在矩形内画棋盘格底（表示透明区域），随背景明暗切换深浅。</summary>
        private void DrawChecker(Rect r)
        {
            if (_checkerTex == null)
            {
                _checkerTex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            }
            Color a, b;
            if (_isDarkBackground) { a = new Color(0.20f, 0.20f, 0.20f); b = new Color(0.27f, 0.27f, 0.27f); }
            else { a = new Color(0.82f, 0.82f, 0.82f); b = new Color(0.57f, 0.57f, 0.57f); }
            _checkerTex.SetPixels(new[] { a, b, b, a });
            _checkerTex.Apply();
            float tile = 8f;
            GUI.DrawTextureWithTexCoords(r, _checkerTex, new Rect(0, 0, r.width / tile, r.height / tile));
        }

        #endregion

        #region 拖拽 / 源图读取

        private static readonly string[] kImageExts = { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".gif", ".psd", ".exr" };

        /// <summary>步骤①的拖拽放置区：支持工程内 Texture 或外部图片文件。</summary>
        private void DrawSourceDropArea()
        {
            Rect drop = GUILayoutUtility.GetRect(0, 56, GUILayout.ExpandWidth(true));
            Event e = Event.current;
            bool hover = drop.Contains(e.mousePosition);
            bool dragging = e.type == EventType.DragUpdated || e.type == EventType.DragPerform;
            bool valid = hover && dragging && IsDragValid();

            EditorGUI.DrawRect(drop, valid ? new Color(kAccent.r, kAccent.g, kAccent.b, 0.18f) : new Color(1, 1, 1, 0.04f));
            DrawRectOutline(drop, valid ? kAccent : new Color(1, 1, 1, 0.18f));
            var centered = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } };
            GUI.Label(drop, _sourceTexture != null ? $"✔ 已选择：{_sourceTexture.name}（可再拖入替换）" : "⬇ 把图片拖到这里（工程内 Texture 或外部图片）", centered);

            if (hover && dragging)
            {
                DragAndDrop.visualMode = IsDragValid() ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                if (e.type == EventType.DragPerform && IsDragValid())
                {
                    DragAndDrop.AcceptDrag();
                    AcceptDraggedImage();
                }
                e.Use();
            }
        }

        /// <summary>判断当前拖拽内容是否包含有效图片。</summary>
        private static bool IsDragValid()
        {
            foreach (var obj in DragAndDrop.objectReferences)
                if (obj is Texture2D) return true;
            foreach (var p in DragAndDrop.paths)
                if (IsImagePath(p)) return true;
            return false;
        }

        /// <summary>判断路径是否是支持的图片扩展名。</summary>
        private static bool IsImagePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return Array.IndexOf(kImageExts, Path.GetExtension(path).ToLowerInvariant()) >= 0;
        }

        /// <summary>接收拖入的图片：优先工程内 Texture，其次外部文件。</summary>
        private void AcceptDraggedImage()
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Texture2D tex)
                {
                    _sourceTexture = tex;
                    _sourceExternalPath = "";
                    GUI.FocusControl(null);
                    return;
                }
            }
            foreach (var p in DragAndDrop.paths)
            {
                if (!IsImagePath(p)) continue;
                Texture2D asset = LoadAsProjectAsset(p);
                if (asset != null) { _sourceTexture = asset; _sourceExternalPath = ""; }
                else LoadExternalImageRef(p);
                GUI.FocusControl(null);
                return;
            }
        }

        /// <summary>若文件位于工程内，返回其资源引用，否则 null。</summary>
        private static Texture2D LoadAsProjectAsset(string fullPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
            string norm = Path.GetFullPath(fullPath).Replace('\\', '/');
            if (norm.StartsWith(projectRoot + "/Assets/", StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(norm.Substring(projectRoot.Length + 1));
            return null;
        }

        /// <summary>把外部图片文件解码成临时 Texture2D 作为源图引用。</summary>
        private void LoadExternalImageRef(string fullPath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes)) { DestroyImmediate(tex); EditorUtility.DisplayDialog("无法加载", $"无法解码图片：\n{fullPath}", "确定"); return; }
                tex.name = Path.GetFileNameWithoutExtension(fullPath);
                _sourceTexture = tex;
                _sourceExternalPath = fullPath;
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("读取失败", ex.Message, "确定");
                Debug.LogException(ex);
            }
        }

        /// <summary>读取源纹理的可读像素（不受 Read/Write 导入设置影响）。优先按文件解码，回退 RenderTexture。</summary>
        private static void ReadSourcePixels(Texture2D tex, string externalPath, out Color32[] pixels, out int w, out int h)
        {
            string filePath = !string.IsNullOrEmpty(externalPath) && File.Exists(externalPath) ? externalPath : null;
            if (filePath == null)
            {
                string assetPath = AssetDatabase.GetAssetPath(tex);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string abs = GetAbsolutePath(assetPath);
                    if (File.Exists(abs)) filePath = abs;
                }
            }
            if (filePath != null)
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                var tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tmp.LoadImage(bytes))
                {
                    w = tmp.width; h = tmp.height; pixels = tmp.GetPixels32();
                    DestroyImmediate(tmp);
                    return;
                }
                DestroyImmediate(tmp);
            }
            // 回退：RenderTexture 拷贝
            w = tex.width; h = tex.height;
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            pixels = readable.GetPixels32();
            DestroyImmediate(readable);
        }

        /// <summary>把 "Assets/xxx" 资源路径转为绝对路径。</summary>
        private static string GetAbsolutePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return assetPath;
            if (Path.IsPathRooted(assetPath)) return assetPath;
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        #endregion
    }
}
