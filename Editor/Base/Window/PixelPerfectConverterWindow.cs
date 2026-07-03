using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelPerfectTool
{
    /// <summary>
    /// 像素完美转换器（Pixel Perfect - AI Art Converter 的 Unity 移植版）。
    /// 菜单：Custom/工具弹窗/像素完美转换器
    ///
    /// 忠实移植自 https://github.com/Void8Bit/Pixel-Perfect-AI-Art-Converter （JS/HTML 网页工具），
    /// 把 AI 生成的“伪像素画”重采样为真正像素对齐的像素图，并提供编辑与导出。
    /// 完整保留原工具三步式工作流与全部功能：
    ///
    /// 步骤①（设置）：选择网格宽/高（16~1024 档位）+ 选择源图片。
    /// 步骤②（定位与转换）：调节预览格子尺寸(4~16)、源图缩放(10~300%)、拖拽定位源图，
    ///   选择 5 种取色算法之一（最常用 / 最常用偏亮 / 最常用偏暗 / 平均 / 邻域），生成像素图。
    ///   另含“智能网格检测”（移植自 https://github.com/theamusing/perfectPixel 的纯 numpy 后端）：
    ///   用 Sobel 梯度自动识别网格尺寸(estimate_grid_gradient)并把网格线吸附到真实像素块边缘(refine_grids)，
    ///   免手动拖拽定位——支持“仅检测尺寸(填入网格数)”与“智能一键转换→步骤③”，可选边缘对齐/对齐强度/近正方形强制正方；
    ///   采样复用上述 5 种取色算法。原实现的 FFT 主检测器未移植（避免手写 2D-FFT），改以梯度法为主检测器。
    /// 步骤③（编辑与导出）：画布缩放(1~20)、网格开关、画笔/橡皮/魔棒三种工具、笔刷颜色、
    ///   笔刷尺寸(1~5)、魔棒阈值(0~30)、最近使用颜色(最多6)、撤销/重做(Ctrl+Z/Ctrl+Y)、
    ///   导出 PNG：x1/x4/x8 另存为、覆盖原图导出、同原图目录导出(原图名_输出宽x高)、
    ///   按列×行拆分导出（共用“导出倍数”）。
    /// 步骤④（辅助功能）：顶部页签切换两个独立子工具——
    ///   ·帧排版：精灵表重排工具，输入原图帧数(列×行)与输出帧数(列×行)，单帧尺寸不变
    ///     (=原图宽/原列、原图高/原行)，按行优先顺序把每帧搬到新布局，支持拖拽替换原图、实时预览，
    ///     并提供不覆盖导出(另存为)/覆盖原图导出/同目录导出。
    ///   ·图片合并：帧排版的逆操作，先设列×行生成对应数量槽位，逐个拖入/选择单图，
    ///     格子尺寸取所有图最大宽×最大高、每张图格子内居中(空白透明)，拼成一张图集
    ///     (如 4 张 32×32 → 2×2 得 64×64 / 4×1 得 128×32)，提供另存为/导出到首张图目录。
    ///
    /// 快捷键：Alt+B 切换棋盘背景明暗；Alt+G 切换网格线明暗；Ctrl+Z 撤销；Ctrl+Y 重做。
    /// </summary>
    public class PixelPerfectConverterWindow : EditorWindow
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

        #region 字段 - 步骤② 定位与转换

        /// <summary>预览格子尺寸（画布每格的“画布像素”数，4~16）。</summary>
        private int _previewCellSize = 4;
        /// <summary>源图缩放滑杆百分比（10~300），实际 imageScale = 值/100。</summary>
        private float _zoomPercent = 100f;
        /// <summary>源图缩放系数（绝对值）。</summary>
        private float _imageScale = 1f;
        /// <summary>源图在画布上的水平偏移（画布像素）。</summary>
        private float _offsetX = 0f;
        /// <summary>源图在画布上的垂直偏移（画布像素）。</summary>
        private float _offsetY = 0f;
        /// <summary>当前选择的取色算法。</summary>
        private ConvMethod _method = ConvMethod.Most;
        /// <summary>聚类相似度阈值（仅最常用/偏亮/偏暗算法使用，越大越容易归并相近色）。</summary>
        private int _similarityThreshold = kDefaultSimilarityThreshold;
        /// <summary>是否限制最终颜色数量（转换后对生成图做颜色量化）。</summary>
        private bool _limitColors = false;
        /// <summary>最终颜色数量上限（启用限制时，生成图的不同颜色不超过此值）。</summary>
        private int _maxColors = 16;
        /// <summary>颜色数量限制模式：以下(上限)/固定(精确)。</summary>
        private ColorLimitMode _colorLimitMode = ColorLimitMode.AtMost;
        /// <summary>源图精确不同颜色数（载入时缓存一次，忽略全透明像素）；-1 表示未统计。</summary>
        private int _srcColorCountExact = -1;
        /// <summary>源图近似颜色数（每通道量化到 32 级后的不同色数，载入时缓存一次）；-1 表示未统计。</summary>
        private int _srcColorCountApprox = -1;

        #endregion

        #region 字段 - 步骤② 智能网格检测（perfectPixel 移植）

        /// <summary>自动检测后是否用 Sobel 梯度把网格线吸附到真实像素块边缘（refine 非均匀网格）；关闭则按检测尺寸均匀切分。</summary>
        private bool _autoUseRefine = true;
        /// <summary>refine 强度（0~0.5）：每条网格线向边缘吸附的搜索范围占格宽比例。</summary>
        private float _refineIntensity = 0.25f;
        /// <summary>检测结果近正方形（长宽仅差 1）时强制补/裁一行一列使其为正方形（对应原工具 fix_square）。</summary>
        private bool _autoFixSquare = true;
        /// <summary>智能检测最近一次的提示/结果信息（空表示无）。</summary>
        private string _autoMessage = "";

        #endregion

        #region 字段 - 步骤③ 编辑与导出

        /// <summary>像素数据（扁平，索引 = row*宽 + col，row0 在顶部）；alpha=0 表示透明。</summary>
        private Color32[] _pixels;
        /// <summary>编辑画布缩放倍数（1~20）。</summary>
        private int _canvasZoom = 1;
        /// <summary>最终效果图预览缩放（每格像素边长，1~20）。</summary>
        private int _resultZoom = 6;
        /// <summary>最终效果图面板滚动位置。</summary>
        private Vector2 _resultScroll;
        /// <summary>当前画笔颜色。</summary>
        private Color _drawColor = Color.black;
        /// <summary>当前工具。</summary>
        private EditTool _tool = EditTool.Brush;
        /// <summary>笔刷尺寸（1~5，方形边长）。</summary>
        private int _brushSize = 1;
        /// <summary>魔棒颜色距离阈值（0~30）。</summary>
        private int _magicWandThreshold = 5;
        /// <summary>是否显示网格线。</summary>
        private bool _showGrid = true;

        /// <summary>覆盖原图导出 / 同原图目录导出 / 拆分导出三者共用的放大倍数（1/2/4/8）。</summary>
        private int _exportScale = 1;
        /// <summary>拆分导出的列数（横向切块数量，≥1）。</summary>
        private int _splitCols = 2;
        /// <summary>拆分导出的行数（纵向切块数量，≥1）。</summary>
        private int _splitRows = 2;

        /// <summary>最近使用颜色（最多 6 个）。</summary>
        private readonly List<Color> _recentColors = new List<Color>();

        /// <summary>调色板：当前像素图中出现的不同颜色（编辑色块即可全局替换该颜色的所有像素）。</summary>
        private readonly List<Color> _palette = new List<Color>();
        /// <summary>每种调色板颜色在像素图中的使用数量（按使用量降序与 _palette 对应）。</summary>
        private readonly List<int> _paletteCounts = new List<int>();
        /// <summary>每个像素映射到的调色板索引（-1 表示透明），用于按索引整体替换颜色，避免颜色碰撞。</summary>
        private int[] _pixelPaletteIndices;
        /// <summary>调色板是否相对当前像素已过期（发生过笔刷/橡皮编辑后置位）。</summary>
        private bool _paletteDirty = false;
        /// <summary>本次拖拽调色板期间是否有改动待在松手时压入历史。</summary>
        private bool _palettePendingHistory = false;

        /// <summary>编辑画布渲染纹理（gridW × gridH，Point 过滤），按显示缩放绘制。</summary>
        private Texture2D _artTex;
        /// <summary>当前鼠标悬停的格子列（用于笔刷高亮），-1 表示无。</summary>
        private int _hoverI = -1;
        /// <summary>当前鼠标悬停的格子行（用于笔刷高亮），-1 表示无。</summary>
        private int _hoverJ = -1;
        /// <summary>是否正在按住左键连续绘制。</summary>
        private bool _drawingActive = false;
        /// <summary>本次按住期间是否发生过绘制（用于决定是否压入历史）。</summary>
        private bool _drawingOccurred = false;

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

        #region 字段 - 步骤④ 辅助功能（帧动画排版）

        /// <summary>步骤④源图：通过 ObjectField/拖拽选择的待重排帧动画图。</summary>
        private Texture2D _auxSourceTexture;
        /// <summary>步骤④源图来自工程外时的外部文件绝对路径（若有）。</summary>
        private string _auxSourceExternalPath = "";
        /// <summary>步骤④源图像素（自上而下，row0 在顶部）。</summary>
        private Color32[] _auxSrcTopDown;
        /// <summary>步骤④源图宽（像素）。</summary>
        private int _auxSrcW;
        /// <summary>步骤④源图高（像素）。</summary>
        private int _auxSrcH;
        /// <summary>步骤④源图显示纹理（参考缩略图）。</summary>
        private Texture2D _auxDisplayTex;

        /// <summary>原图横向帧数（列）。</summary>
        private int _auxSrcCols = 8;
        /// <summary>原图纵向帧数（行）。</summary>
        private int _auxSrcRows = 1;
        /// <summary>输出横向帧数（列）。</summary>
        private int _auxOutCols = 4;
        /// <summary>输出纵向帧数（行）。</summary>
        private int _auxOutRows = 2;

        /// <summary>步骤④重排结果像素（自上而下）。</summary>
        private Color32[] _auxResultTopDown;
        /// <summary>步骤④重排结果宽（像素）。</summary>
        private int _auxResultW;
        /// <summary>步骤④重排结果高（像素）。</summary>
        private int _auxResultH;
        /// <summary>步骤④重排结果预览纹理。</summary>
        private Texture2D _auxResultTex;
        /// <summary>步骤④结果预览缩放（每像素显示边长，1~16）。</summary>
        private int _auxResultZoom = 2;
        /// <summary>步骤④结果预览滚动位置。</summary>
        private Vector2 _auxResultScroll;
        /// <summary>步骤④最近一次重排的提示/警告信息（空表示正常）。</summary>
        private string _auxMessage = "";

        #endregion

        #region 字段 - 步骤④ 辅助功能（图片合并）

        /// <summary>步骤④当前子模式：帧排版 / 图片合并。</summary>
        private AuxMode _auxMode = AuxMode.Relayout;

        /// <summary>合并图集横向槽位数（列，≥1）。</summary>
        private int _mergeCols = 2;
        /// <summary>合并图集纵向槽位数（行，≥1）。</summary>
        private int _mergeRows = 2;

        /// <summary>各槽位的单图纹理引用（行优先顺序，长度 = 列×行；null 表示空槽）。</summary>
        private readonly List<Texture2D> _mergeSlotTex = new List<Texture2D>();
        /// <summary>各槽位单图来自工程外时的外部文件绝对路径（与 _mergeSlotTex 一一对应，空串表示工程内资源）。</summary>
        private readonly List<string> _mergeSlotPath = new List<string>();

        /// <summary>合并结果像素（自上而下）。</summary>
        private Color32[] _mergeResultTopDown;
        /// <summary>合并结果宽（像素）。</summary>
        private int _mergeResultW;
        /// <summary>合并结果高（像素）。</summary>
        private int _mergeResultH;
        /// <summary>合并结果预览纹理。</summary>
        private Texture2D _mergeResultTex;
        /// <summary>合并结果预览缩放（每像素显示边长，1~16）。</summary>
        private int _mergeResultZoom = 2;
        /// <summary>合并结果预览滚动位置。</summary>
        private Vector2 _mergeResultScroll;
        /// <summary>合并功能最近一次的提示/警告信息（空表示正常）。</summary>
        private string _mergeMessage = "";

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

        /// <summary>菜单项：打开像素完美转换器窗口。</summary>
        [MenuItem("Custom/工具弹窗/像素完美转换器")]
        public static void Open()
        {
            var win = GetWindow<PixelPerfectConverterWindow>("像素完美转换器");
            win.minSize = new Vector2(720, 640);
            win.Show();
        }

        /// <summary>右键菜单项：在 Project 窗口选中一张图片后右键，直接用该图打开转换器并自动载入进入步骤②。</summary>
        [MenuItem("Assets/像素完美转换器", false, 2000)]
        public static void OpenFromSelection()
        {
            var tex = Selection.activeObject as Texture2D;
            var win = GetWindow<PixelPerfectConverterWindow>("像素完美转换器");
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
        [MenuItem("Assets/像素完美转换器", true)]
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
            GUI.Label(new Rect(r.x + 14, r.y + 5, r.width - 28, 22), "🎮 像素完美转换器 · Pixel Perfect", _titleStyle);
            GUI.Label(new Rect(r.x + 16, r.y + 25, r.width - 28, 16),
                "把 AI 像素画重采样为真正像素对齐的图 · 定位 → 取色转换 → 编辑导出", _subTitleStyle);
        }

        /// <summary>步骤进度条（①设置 ②定位转换 ③编辑导出）。</summary>
        private void DrawStepBar()
        {
            string[] names = { "① 设置", "② 定位与转换", "③ 编辑与导出", "④ 辅助功能" };
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < names.Length; i++)
                {
                    int step = i + 1;
                    bool active = _step == step;
                    // 步骤④为独立辅助工具，任何时候都可进入
                    bool reachable = step <= _step || step == 4;
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
            return false;
        }

        #endregion

        #region UI - 步骤① 设置

        /// <summary>步骤①：选择网格尺寸与源图片。</summary>
        private void DrawStep1()
        {
            BeginCard("① 网格尺寸");
            _gridWidth = GridOptionPopup("网格宽（列）", _gridWidth);
            _gridHeight = GridOptionPopup("网格高（行）", _gridHeight);
            EditorGUILayout.LabelField($"输出像素图：{_gridWidth} × {_gridHeight}", _hintStyle);
            EndCard();

            BeginCard("② 源图片");
            DrawSourceDropArea();
            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            _sourceTexture = (Texture2D)EditorGUILayout.ObjectField("图片", _sourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && _sourceTexture != null)
                _sourceExternalPath = "";

            if (_srcDisplayTex != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect thumb = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
                    DrawChecker(thumb);
                    GUI.DrawTexture(thumb, _srcDisplayTex, ScaleMode.ScaleToFit, true);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("已载入尺寸", $"{_srcW} × {_srcH}");
                        EditorGUILayout.LabelField("（超过 1024 已等比缩小）", _hintStyle);
                    }
                }
            }
            EndCard();

            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(_sourceTexture == null))
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = kGenColor;
                if (GUILayout.Button("载入并进入步骤②", GUILayout.Height(34)))
                {
                    try
                    {
                        LoadSource();
                        EnterStep2();
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.DisplayDialog("载入失败", ex.Message, "确定");
                        Debug.LogException(ex);
                    }
                }
                GUI.backgroundColor = prev;
            }
            if (_sourceTexture == null)
                EditorGUILayout.LabelField("请先选择一张源图片。", _hintStyle);
        }

        /// <summary>网格档位下拉。</summary>
        private int GridOptionPopup(string label, int value)
        {
            int idx = Array.IndexOf(kGridOptions, value);
            if (idx < 0) idx = 0;
            string[] labels = new string[kGridOptions.Length];
            for (int i = 0; i < kGridOptions.Length; i++) labels[i] = kGridOptions[i].ToString();
            idx = EditorGUILayout.Popup(label, idx, labels);
            return kGridOptions[idx];
        }

        #endregion

        #region UI - 步骤② 定位与转换

        /// <summary>步骤②：调节预览参数、拖拽定位源图、选择算法并转换。</summary>
        private void DrawStep2()
        {
            BeginCard("定位与采样设置");
            EditorGUI.BeginChangeCheck();
            _previewCellSize = EditorGUILayout.IntSlider(new GUIContent("预览格子尺寸", "每个网格在画布上的像素边长(4~16)"), _previewCellSize, 4, 16);
            if (EditorGUI.EndChangeCheck())
                RefitImage();

            EditorGUI.BeginChangeCheck();
            _zoomPercent = EditorGUILayout.Slider(new GUIContent("源图缩放(%)", "源图缩放百分比(10~300)，以画布中心为锚"), _zoomPercent, 10f, 300f);
            if (EditorGUI.EndChangeCheck())
                ApplyZoom(_zoomPercent);

            _method = (ConvMethod)EditorGUILayout.Popup("取色算法", (int)_method, new[]
            {
                "最常用颜色", "最常用颜色（偏亮）", "最常用颜色（偏暗）", "平均颜色", "邻域颜色"
            });
            EditorGUILayout.LabelField(MethodHint(_method), _hintStyle);

            // 聚类相似度阈值（仅最常用/偏亮/偏暗算法生效）
            bool clustering = _method == ConvMethod.Most || _method == ConvMethod.MostLight || _method == ConvMethod.MostDark;
            using (new EditorGUI.DisabledScope(!clustering))
            {
                _similarityThreshold = EditorGUILayout.IntSlider(
                    new GUIContent("相似度阈值", "相近色归并阈值(RGB 欧氏距离,0~100)；仅最常用/偏亮/偏暗算法生效，越大越容易合并相近色"),
                    _similarityThreshold, 0, 100);
            }
            if (!clustering)
                EditorGUILayout.LabelField("（当前算法为平均/邻域，不使用相似度阈值）", _hintStyle);

            // 最终颜色数量限制（转换后对生成图做颜色量化）
            using (new EditorGUILayout.HorizontalScope())
            {
                _limitColors = EditorGUILayout.ToggleLeft(new GUIContent("限制最终颜色数", "勾选后，生成图的不同颜色数按右侧上限做颜色量化"), _limitColors, GUILayout.Width(120));
                using (new EditorGUI.DisabledScope(!_limitColors))
                {
                    _maxColors = Mathf.Clamp(EditorGUILayout.IntField(_maxColors, GUILayout.Width(48)), 1, 256);
                    _colorLimitMode = (ColorLimitMode)EditorGUILayout.Popup((int)_colorLimitMode,
                        new[] { "种以下(上限)", "种固定(精确)" }, GUILayout.Width(110));
                }
                // 源图颜色数（载入时已缓存，仅读显示，不每帧重算）
                GUILayout.FlexibleSpace();
                if (_srcColorCountExact >= 0)
                    EditorGUILayout.LabelField(
                        new GUIContent($"原图色数：近似 {_srcColorCountApprox} / 精确 {_srcColorCountExact}",
                            "近似=每通道量化到 32 级合并相近色后的色数(可作为上限参考)；精确=完全不同的 RGB 色数"),
                        _hintStyle, GUILayout.Width(210));
            }
            if (_limitColors)
                EditorGUILayout.LabelField(_colorLimitMode == ColorLimitMode.AtMost
                    ? "以下：颜色不超过上限；生成图实际色数少于上限时按实际色数输出。"
                    : "固定：颜色超过上限时精确削到该数量；原图色数不足时无法造色，仍按实际输出。",
                    _hintStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("重置定位（居中适配）"))
                    RefitImage();
                if (GUILayout.Button("◀ 返回步骤①（清空）"))
                {
                    if (EditorUtility.DisplayDialog("确认", "返回步骤①将清空当前数据，确定吗？", "确定", "取消"))
                        ResetAll();
                }
            }
            EndCard();

            DrawStep2AutoDetect();

            BeginCard("源图定位预览（在画布内拖拽移动源图）");
            DrawStep2Preview();
            EditorGUILayout.LabelField("提示：在预览区按住左键拖动可移动源图；Alt+G 切换网格线明暗。", _hintStyle);
            EndCard();

            EditorGUILayout.Space(4);
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = kGenColor;
            if (GUILayout.Button("转换为像素图 → 步骤③", GUILayout.Height(34)))
            {
                Convert();
                EnterStep3();
            }
            GUI.backgroundColor = prev;
        }

        /// <summary>绘制步骤②预览画布：棋盘底、源图（按 offset/scale 定位）、网格线，并处理拖拽。</summary>
        private void DrawStep2Preview()
        {
            float canvasW = _gridWidth * _previewCellSize;
            float canvasH = _gridHeight * _previewCellSize;
            float disp = Mathf.Min(kPreviewMaxDisp / canvasW, kPreviewMaxDisp / canvasH);
            float dw = canvasW * disp;
            float dh = canvasH * disp;

            Rect area;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                area = GUILayoutUtility.GetRect(dw, dh, GUILayout.ExpandWidth(false));
                GUILayout.FlexibleSpace();
            }

            // 棋盘背景
            DrawChecker(area);

            // 源图（裁剪到画布范围内绘制）
            if (_srcDisplayTex != null)
            {
                GUI.BeginGroup(area);
                Rect imgRect = new Rect(_offsetX * disp, _offsetY * disp,
                    _srcW * _imageScale * disp, _srcH * _imageScale * disp);
                GUI.DrawTexture(imgRect, _srcDisplayTex, ScaleMode.StretchToFill, true);
                GUI.EndGroup();
            }

            // 网格线
            DrawGridLines(area, _gridWidth, _gridHeight, _isDarkGrid ? new Color(1, 1, 1, 0.5f) : new Color(0, 0, 0, 0.5f));
            DrawRectOutline(area, new Color(0, 0, 0, 0.5f));

            // 拖拽定位
            HandleStep2Drag(area, disp);
        }

        /// <summary>处理步骤②预览区内的左键拖拽，更新源图偏移。</summary>
        private void HandleStep2Drag(Rect area, float disp)
        {
            Event e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && area.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        // 屏幕位移 → 画布像素位移
                        _offsetX += e.delta.x / disp;
                        _offsetY += e.delta.y / disp;
                        e.Use();
                        Repaint();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }
        }

        /// <summary>各算法的提示文案。</summary>
        private static string MethodHint(ConvMethod m)
        {
            switch (m)
            {
                case ConvMethod.Most: return "格内相近色聚类（按下方相似度阈值），取主簇里使用最多的颜色。";
                case ConvMethod.MostLight: return "按亮度加权聚类，权重偏向更亮的颜色。";
                case ConvMethod.MostDark: return "按亮度加权聚类，权重偏向更暗的颜色。";
                case ConvMethod.Average: return "格内所有非透明像素 RGB 的算术平均。";
                case ConvMethod.Neighbor: return "向四周外扩 25% 区域后再做平均，边缘过渡更柔和。";
                default: return "";
            }
        }

        /// <summary>步骤②智能网格检测卡片：Sobel 梯度自动识别网格尺寸并对齐边缘，免手动拖拽定位。</summary>
        private void DrawStep2AutoDetect()
        {
            BeginCard("智能网格检测（perfectPixel 移植）");
            EditorGUILayout.LabelField("⚠ 仅适用于“已经是像素图的大图”修正——检测的是现成像素块的网格边界；对非像素风格的普通图片无效。", _warnStyle);
            EditorGUILayout.LabelField("基于 Sobel 梯度自动识别网格尺寸并把网格线对齐到真实像素块边缘，无需手动拖拽定位。", _hintStyle);

            _autoUseRefine = EditorGUILayout.ToggleLeft(
                new GUIContent("边缘对齐(refine)", "用 Sobel 梯度把每条网格线吸附到真实像素块边缘；关闭则按检测尺寸均匀切分"),
                _autoUseRefine);
            using (new EditorGUI.DisabledScope(!_autoUseRefine))
            {
                _refineIntensity = EditorGUILayout.Slider(
                    new GUIContent("对齐强度", "网格线吸附搜索范围占格宽的比例(0~0.5)，越大允许偏移越多"),
                    _refineIntensity, 0f, 0.5f);
            }
            _autoFixSquare = EditorGUILayout.ToggleLeft(
                new GUIContent("近正方形强制正方", "当检测结果长宽仅差 1 时补/裁一行一列使其为正方形"),
                _autoFixSquare);

            EditorGUILayout.LabelField("采样沿用上方“取色算法”。检测失败时可回退手动模式。", _hintStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("仅检测网格尺寸"))
                    AutoDetectGridSizeOnly();
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = kGenColor;
                if (GUILayout.Button("智能一键转换 → 步骤③"))
                    AutoDetectAndConvert();
                GUI.backgroundColor = prev;
            }

            if (!string.IsNullOrEmpty(_autoMessage))
                EditorGUILayout.HelpBox(_autoMessage, MessageType.Info);
            EndCard();
        }

        #endregion

        #region UI - 步骤③ 编辑与导出

        /// <summary>步骤③：左侧工具面板 + 右侧编辑画布。</summary>
        private void DrawStep3()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // 左侧工具面板
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(248)))
                {
                    DrawStep3ToolPanel();
                }
                // 中间编辑画布
                using (new EditorGUILayout.VerticalScope())
                {
                    BeginCard($"编辑画布  ({_gridWidth} × {_gridHeight})");
                    DrawEditCanvas();
                    EditorGUILayout.LabelField("左键涂格/拖拽连续绘制；魔棒为单击洪水填充。Ctrl+Z 撤销 / Ctrl+Y 重做。", _hintStyle);
                    EndCard();
                }
                // 右侧最终效果图（宽度封顶，超出由内部滚动）
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Min(_gridWidth * _resultZoom, 380) + 24)))
                {
                    DrawResultPreview();
                }
            }

            // 全宽：调色板 / 颜色替换
            DrawPaletteSection();

            // 调色板拖拽结束后压入一次历史（把整段拖拽合并为一次撤销）
            // 注意：此处【不能】调用 ExtractPalette()。否则若刚把某颜色改成透明，
            // ExtractPalette 会跳过所有透明像素（见其 p.a==0 分支），导致该颜色槽因“没有像素”
            // 而被移除——表现为“设为透明 0.几秒后颜色从列表消失”。不重建调色板即可让透明槽常驻，
            // 且 _pixelPaletteIndices 仍指向原槽位，用户可再把该槽改回有色将这些像素恢复。
            if (_palettePendingHistory && Event.current.type == EventType.MouseUp)
            {
                _palettePendingHistory = false;
                SaveHistory();
            }
        }

        /// <summary>步骤③左侧：工具/颜色/缩放/导出等控件。</summary>
        private void DrawStep3ToolPanel()
        {
            BeginCard("视图");
            _canvasZoom = EditorGUILayout.IntSlider(new GUIContent("画布缩放", "放大显示倍数(1~20)，不影响像素数据"), _canvasZoom, 1, 20);
            _showGrid = EditorGUILayout.Toggle("显示网格", _showGrid);
            EndCard();

            BeginCard("工具");
            using (new EditorGUILayout.HorizontalScope())
            {
                ToolButton("画笔", EditTool.Brush);
                ToolButton("橡皮", EditTool.Eraser);
                ToolButton("魔棒", EditTool.MagicWand);
            }
            EditorGUI.BeginChangeCheck();
            _drawColor = EditorGUILayout.ColorField("笔刷颜色", _drawColor);
            if (EditorGUI.EndChangeCheck())
            {
                _tool = EditTool.Brush; // 改色即切回画笔（与原工具一致）
            }
            _brushSize = EditorGUILayout.IntSlider(new GUIContent("笔刷尺寸", "方形笔刷边长(1~5)"), _brushSize, 1, 5);
            using (new EditorGUI.DisabledScope(_tool != EditTool.MagicWand))
            {
                _magicWandThreshold = EditorGUILayout.IntSlider(new GUIContent("魔棒阈值", "颜色距离阈值(0~30)"), _magicWandThreshold, 0, 30);
            }
            EndCard();

            DrawRecentColors();

            BeginCard("撤销 / 重做");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_historyStack.Count <= 1))
                    if (GUILayout.Button("↶ 撤销")) Undo();
                using (new EditorGUI.DisabledScope(_redoStack.Count == 0))
                    if (GUILayout.Button("↷ 重做")) Redo();
            }
            EndCard();

            BeginCard("导出 PNG");

            // 另存为：弹窗选路径，固定倍数快捷导出
            EditorGUILayout.LabelField("另存为（弹窗选择路径）", _hintStyle);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("×1")) ExportImage(1);
                if (GUILayout.Button("×4")) ExportImage(4);
                if (GUILayout.Button("×8")) ExportImage(8);
            }
            EditorGUILayout.LabelField("每格渲染为 scale×scale 块，透明格留空。", _hintStyle);

            EditorGUILayout.Space(4);
            DrawSeparator();
            EditorGUILayout.Space(2);

            // 导出倍数：覆盖原图 / 同目录 / 拆分导出三者共用
            _exportScale = EditorGUILayout.IntPopup(
                new GUIContent("导出倍数", "覆盖原图 / 同目录 / 拆分导出共用的放大倍数"),
                _exportScale,
                new[] { new GUIContent("×1"), new GUIContent("×2"), new GUIContent("×4"), new GUIContent("×8") },
                new[] { 1, 2, 4, 8 });

            // 覆盖原图 / 同原图目录导出（需原图有磁盘文件）
            bool hasSrcFile = GetSourceFilePath() != null;
            using (new EditorGUI.DisabledScope(!hasSrcFile))
            {
                if (GUILayout.Button(new GUIContent("覆盖原图导出", "用当前像素图（×导出倍数）覆盖写回原始图片文件")))
                    ExportOverwriteOriginal(_exportScale);
                if (GUILayout.Button(new GUIContent("同原图目录导出", "导出到原图所在目录，文件名 = 原图名_输出宽x高.png")))
                    ExportToSourceDir(_exportScale);
            }
            if (!hasSrcFile)
                EditorGUILayout.LabelField("（原图无磁盘文件，覆盖/同目录导出不可用）", _hintStyle);

            EditorGUILayout.Space(4);
            DrawSeparator();
            EditorGUILayout.Space(2);

            // 拆分导出：按列/行数把整图切成若干块分别导出
            EditorGUILayout.LabelField("拆分导出（按列/行数切块）", _hintStyle);
            using (new EditorGUILayout.HorizontalScope())
            {
                _splitCols = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("列数", "横向切成几块"), _splitCols), 1, _gridWidth);
                _splitRows = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("行数", "纵向切成几块"), _splitRows), 1, _gridHeight);
            }
            EditorGUILayout.LabelField(
                $"每块约 {_gridWidth / Mathf.Max(1, _splitCols)}×{_gridHeight / Mathf.Max(1, _splitRows)} 格，输出 ×{_exportScale}，共 {_splitCols * _splitRows} 张。",
                _hintStyle);
            if (GUILayout.Button(new GUIContent("拆分导出（弹窗选基名）", "选择保存基名后，按列×行切块逐张导出")))
                ExportSplit(_splitCols, _splitRows, _exportScale);
            using (new EditorGUI.DisabledScope(!hasSrcFile))
            {
                if (GUILayout.Button(new GUIContent("拆分导出到原图目录", "导出到原图所在目录，名 = 原图名_r{行}_c{列}.png")))
                    ExportSplitToSourceDir(_splitCols, _splitRows, _exportScale);
            }

            EndCard();

            // 源图缩略图（替代原工具的可拖拽浮窗）
            _foldSource = EditorGUILayout.Foldout(_foldSource, "源图片参考", true);
            if (_foldSource && _srcDisplayTex != null)
            {
                Rect r = GUILayoutUtility.GetRect(220, 160, GUILayout.Width(220), GUILayout.Height(160));
                DrawChecker(r);
                GUI.DrawTexture(r, _srcDisplayTex, ScaleMode.ScaleToFit, true);
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀ 返回步骤②"))
                    _step = 2;
                if (GUILayout.Button("重置全部"))
                {
                    if (EditorUtility.DisplayDialog("确认", "重置全部将清空数据并回到步骤①，确定吗？", "确定", "取消"))
                        ResetAll();
                }
            }
        }

        /// <summary>工具切换按钮。</summary>
        private void ToolButton(string label, EditTool tool)
        {
            bool active = _tool == tool;
            Color prev = GUI.backgroundColor;
            if (active) GUI.backgroundColor = kAccent;
            if (GUILayout.Button(label, GUILayout.Height(26)))
            {
                _tool = tool;
                if (tool == EditTool.Eraser) { /* 橡皮使用透明 */ }
            }
            GUI.backgroundColor = prev;
        }

        /// <summary>最近使用颜色面板（最多 6 个，点击回填为画笔色）。</summary>
        private void DrawRecentColors()
        {
            if (_recentColors.Count == 0) return;
            BeginCard("最近颜色");
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < _recentColors.Count; i++)
                {
                    Rect r = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28), GUILayout.Height(28));
                    DrawChecker(r);
                    EditorGUI.DrawRect(r, _recentColors[i]);
                    DrawRectOutline(r, new Color(0, 0, 0, 0.4f));
                    if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                    {
                        _drawColor = _recentColors[i];
                        _tool = EditTool.Brush;
                        Event.current.Use();
                        Repaint();
                    }
                }
            }
            EndCard();
        }

        /// <summary>绘制右侧「最终效果图」：干净的像素图（无网格/无高亮），缩放可调，所见即导出效果。</summary>
        private void DrawResultPreview()
        {
            BeginCard("最终效果图");
            _resultZoom = EditorGUILayout.IntSlider(new GUIContent("效果图缩放", "效果图每格的像素边长(1~20)"), _resultZoom, 1, 20);

            if (_artTex == null) BuildArtTexture();

            float dw = _gridWidth * _resultZoom;
            float dh = _gridHeight * _resultZoom;

            // 预览区限定一个可视高度，超出可滚动，避免大网格撑爆窗口
            float viewH = Mathf.Min(dh, 420f);
            _resultScroll = EditorGUILayout.BeginScrollView(_resultScroll, GUILayout.Height(viewH + 4));
            Rect area = GUILayoutUtility.GetRect(dw, dh, GUILayout.Width(dw), GUILayout.Height(dh));
            DrawChecker(area);
            if (_artTex != null) GUI.DrawTexture(area, _artTex, ScaleMode.StretchToFill, true);
            DrawRectOutline(area, new Color(0, 0, 0, 0.5f));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField($"{_gridWidth}×{_gridHeight} · 放大 {_resultZoom}×（无网格/无高亮，所见即导出）", _hintStyle);
            EndCard();
        }

        /// <summary>绘制步骤③编辑画布：像素纹理 + 网格线 + 笔刷高亮，并处理绘制交互。</summary>
        private void DrawEditCanvas()
        {
            if (_artTex == null) BuildArtTexture();

            float disp = Mathf.Min(kEditMaxDisp / _gridWidth, kEditMaxDisp / _gridHeight) * _canvasZoom;
            // 至少 1px/格
            disp = Mathf.Max(1f, disp);
            float dw = _gridWidth * disp;
            float dh = _gridHeight * disp;

            Rect area;
            using (new EditorGUILayout.HorizontalScope())
            {
                area = GUILayoutUtility.GetRect(dw, dh, GUILayout.ExpandWidth(false));
            }

            DrawChecker(area);
            GUI.DrawTexture(area, _artTex, ScaleMode.StretchToFill, true);

            if (_showGrid)
                DrawGridLines(area, _gridWidth, _gridHeight, _isDarkGrid ? new Color(1, 1, 1, 0.3f) : new Color(0, 0, 0, 0.3f));
            DrawRectOutline(area, new Color(0, 0, 0, 0.5f));

            // 笔刷高亮
            if (_hoverI >= 0 && _hoverJ >= 0)
            {
                float cw = area.width / _gridWidth;
                float ch = area.height / _gridHeight;
                Rect hl = new Rect(area.x + _hoverI * cw, area.y + _hoverJ * ch, _brushSize * cw, _brushSize * ch);
                DrawRectOutline(hl, new Color(1, 0, 0, 0.8f));
            }

            HandleEditInteraction(area);
        }

        /// <summary>处理编辑画布上的鼠标交互：高亮、绘制、魔棒填充。</summary>
        private void HandleEditInteraction(Rect area)
        {
            Event e = Event.current;
            bool inside = area.Contains(e.mousePosition);

            // 更新悬停格（仅在鼠标事件中改状态，避免在 Repaint 中改状态导致重绘循环）
            if (inside && (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown))
            {
                CellAt(area, e.mousePosition, out int hi, out int hj);
                if (hi != _hoverI || hj != _hoverJ) { _hoverI = hi; _hoverJ = hj; Repaint(); }
            }

            int id = GUIUtility.GetControlID(FocusType.Passive);
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && inside)
                    {
                        if (_tool == EditTool.MagicWand)
                        {
                            DoMagicWand(area, e.mousePosition);
                            SaveHistory();
                            ExtractPalette();
                        }
                        else
                        {
                            GUIUtility.hotControl = id;
                            _drawingActive = true;
                            _drawingOccurred = false;
                            if (_tool == EditTool.Brush)
                                AddRecentColor(_drawColor);
                            PaintAt(area, e.mousePosition);
                        }
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id && _drawingActive)
                    {
                        PaintAt(area, e.mousePosition);
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        _drawingActive = false;
                        if (_drawingOccurred) { SaveHistory(); ExtractPalette(); _drawingOccurred = false; }
                        e.Use();
                    }
                    break;
            }

            // 鼠标移出区域时清除高亮
            if (!inside && _hoverI >= 0)
            {
                _hoverI = _hoverJ = -1;
                Repaint();
            }

            // 让窗口持续接收 MouseMove
            wantsMouseMove = true;
        }

        /// <summary>把屏幕坐标换算为网格格子坐标（i 列, j 行），并夹紧到有效范围。</summary>
        private void CellAt(Rect area, Vector2 mouse, out int i, out int j)
        {
            float cw = area.width / _gridWidth;
            float ch = area.height / _gridHeight;
            i = Mathf.Clamp(Mathf.FloorToInt((mouse.x - area.x) / cw), 0, _gridWidth - 1);
            j = Mathf.Clamp(Mathf.FloorToInt((mouse.y - area.y) / ch), 0, _gridHeight - 1);
        }

        #endregion

        #region UI - 步骤④ 辅助功能（帧动画排版）

        /// <summary>
        /// 步骤④（辅助功能）入口：顶部用页签切换「帧排版」与「图片合并」两个独立子工具，
        /// 分别派发到 DrawStep4Relayout / DrawStep4Merge。
        /// </summary>
        private void DrawStep4()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int sel = GUILayout.Toolbar((int)_auxMode, new[] { "帧排版（拆分/重排）", "图片合并（拼图集）" }, GUILayout.Height(26));
                if (sel != (int)_auxMode) { _auxMode = (AuxMode)sel; GUI.FocusControl(null); }
            }
            EditorGUILayout.Space(4);

            if (_auxMode == AuxMode.Relayout) DrawStep4Relayout();
            else DrawStep4Merge();
        }

        /// <summary>
        /// 步骤④·帧排版：帧动画图片排版修改。
        /// 把按「列×行」帧排布的精灵表（每帧尺寸 = 原图宽/列、原图高/行）按行优先(从左到右、从上到下)
        /// 顺序重新排版为另一种「列×行」布局，单帧尺寸保持不变，仅改变帧的行列排布。
        /// 例：256×32 原图填 8×1，输出填 4×2 → 单帧 32×32，结果 128×64。
        /// </summary>
        private void DrawStep4Relayout()
        {
            BeginCard("帧动画图片排版修改");
            EditorGUILayout.LabelField(
                "把按「列×行」帧排布的精灵表重新排版为另一种布局（单帧尺寸不变，仅改变帧的行列排布）。\n" +
                "例：256×32 原图填原图帧数 8×1、输出帧数 4×2 → 单帧 32×32，结果拆成 128×64。",
                _hintStyle);
            EndCard();

            BeginCard("① 原图（支持拖拽替换）");
            DrawAuxDropArea();
            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            _auxSourceTexture = (Texture2D)EditorGUILayout.ObjectField("帧动画图", _auxSourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
            {
                _auxSourceExternalPath = "";
                LoadAuxSource();
            }

            if (_auxDisplayTex != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect thumb = GUILayoutUtility.GetRect(72, 72, GUILayout.Width(72), GUILayout.Height(72));
                    DrawChecker(thumb);
                    GUI.DrawTexture(thumb, _auxDisplayTex, ScaleMode.ScaleToFit, true);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("原图尺寸", $"{_auxSrcW} × {_auxSrcH} 像素");
                        int fw = _auxSrcW / Mathf.Max(1, _auxSrcCols);
                        int fh = _auxSrcH / Mathf.Max(1, _auxSrcRows);
                        EditorGUILayout.LabelField("单帧尺寸", $"{fw} × {fh} 像素");
                    }
                }
            }
            EndCard();

            BeginCard("② 帧数参数");
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("原图帧数（列 × 行）", "原图横向、纵向各有几帧"), GUILayout.Width(150));
                _auxSrcCols = Mathf.Max(1, EditorGUILayout.IntField(_auxSrcCols, GUILayout.Width(60)));
                EditorGUILayout.LabelField("×", GUILayout.Width(14));
                _auxSrcRows = Mathf.Max(1, EditorGUILayout.IntField(_auxSrcRows, GUILayout.Width(60)));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("输出帧数（列 × 行）", "输出图横向、纵向各排几帧"), GUILayout.Width(150));
                _auxOutCols = Mathf.Max(1, EditorGUILayout.IntField(_auxOutCols, GUILayout.Width(60)));
                EditorGUILayout.LabelField("×", GUILayout.Width(14));
                _auxOutRows = Mathf.Max(1, EditorGUILayout.IntField(_auxOutRows, GUILayout.Width(60)));
            }
            if (EditorGUI.EndChangeCheck())
                RebuildAuxResult();

            if (_auxSrcTopDown != null)
            {
                int fw = _auxSrcW / Mathf.Max(1, _auxSrcCols);
                int fh = _auxSrcH / Mathf.Max(1, _auxSrcRows);
                EditorGUILayout.LabelField(
                    $"原帧数 {_auxSrcCols * _auxSrcRows} → 输出帧位 {_auxOutCols * _auxOutRows}；输出尺寸 {_auxOutCols * fw} × {_auxOutRows * fh} 像素。",
                    _hintStyle);
            }
            EndCard();

            if (!string.IsNullOrEmpty(_auxMessage))
                EditorGUILayout.HelpBox(_auxMessage, MessageType.Warning);

            BeginCard("③ 重排预览");
            if (_auxResultTex != null)
            {
                _auxResultZoom = EditorGUILayout.IntSlider(new GUIContent("预览缩放", "结果预览每像素显示边长(1~16)"), _auxResultZoom, 1, 16);
                float dw = _auxResultW * _auxResultZoom;
                float dh = _auxResultH * _auxResultZoom;
                float viewH = Mathf.Min(dh, 360f);
                _auxResultScroll = EditorGUILayout.BeginScrollView(_auxResultScroll, GUILayout.Height(viewH + 4));
                Rect area = GUILayoutUtility.GetRect(dw, dh, GUILayout.Width(dw), GUILayout.Height(dh));
                DrawChecker(area);
                GUI.DrawTexture(area, _auxResultTex, ScaleMode.StretchToFill, true);
                DrawRectOutline(area, new Color(0, 0, 0, 0.5f));
                EditorGUILayout.EndScrollView();
                EditorGUILayout.LabelField($"输出 {_auxResultW} × {_auxResultH} 像素 · 放大 {_auxResultZoom}×", _hintStyle);
            }
            else
            {
                EditorGUILayout.LabelField("（选择原图并设置帧数后显示重排预览）", _hintStyle);
            }
            EndCard();

            BeginCard("④ 导出");
            using (new EditorGUI.DisabledScope(_auxResultTopDown == null))
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = kGenColor;
                if (GUILayout.Button(new GUIContent("不覆盖导出（另存为）", "弹窗选择路径，导出重排后的新图，不改动原图"), GUILayout.Height(30)))
                    ExportAuxAs();
                GUI.backgroundColor = prev;

                bool hasFile = GetAuxSourceFilePath() != null;
                using (new EditorGUI.DisabledScope(!hasFile))
                {
                    if (GUILayout.Button(new GUIContent("覆盖原图导出", "用重排后的图覆盖写回原始文件（不可撤销）")))
                        ExportAuxOverwrite();
                    if (GUILayout.Button(new GUIContent("同原图目录导出", "导出到原图所在目录，文件名 = 原图名_relayout_列x行.png")))
                        ExportAuxToSourceDir();
                }
                if (!hasFile)
                    EditorGUILayout.LabelField("（原图无磁盘文件，覆盖/同目录导出不可用）", _hintStyle);
            }
            EndCard();
        }

        /// <summary>
        /// 步骤④·图片合并：把多张单图按「列×行」拼成一张图集（帧排版的逆操作）。
        /// 先设列×行生成对应数量的槽位，逐个拖入/选择单图；单帧格子尺寸取所有图中的最大宽×最大高，
        /// 每张图在自己格子内居中放置、空白透明。例：4 张 32×32 填 2×2 → 64×64；填 4×1 → 128×32。
        /// </summary>
        private void DrawStep4Merge()
        {
            BeginCard("多张单图合并为图集");
            EditorGUILayout.LabelField(
                "把多张单图按「列×行」拼成一张图集（与帧排版相反）。\n" +
                "格子尺寸 = 所有图中的最大宽 × 最大高，每张图在格子内居中、空白透明。\n" +
                "例：4 张 32×32 填 2×2 → 64×64；填 4×1 → 128×32。",
                _hintStyle);
            EndCard();

            BeginCard("① 图集布局（列 × 行）");
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("槽位数（列 × 行）", "横向、纵向各放几张单图"), GUILayout.Width(150));
                _mergeCols = Mathf.Clamp(EditorGUILayout.IntField(_mergeCols, GUILayout.Width(60)), 1, 32);
                EditorGUILayout.LabelField("×", GUILayout.Width(14));
                _mergeRows = Mathf.Clamp(EditorGUILayout.IntField(_mergeRows, GUILayout.Width(60)), 1, 32);
            }
            if (EditorGUI.EndChangeCheck())
            {
                EnsureMergeSlotCount();
                RebuildMergeResult();
            }
            EnsureMergeSlotCount();
            EditorGUILayout.LabelField($"共 {_mergeCols * _mergeRows} 个槽位，按行优先（从左到右、从上到下）填入。", _hintStyle);
            EndCard();

            BeginCard("② 单图槽位（逐个拖入工程内 Texture 或外部图片）");
            DrawMergeSlotGrid();
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("清空所有槽位", "移除所有已放入的单图")))
                {
                    for (int i = 0; i < _mergeSlotTex.Count; i++) { _mergeSlotTex[i] = null; _mergeSlotPath[i] = ""; }
                    RebuildMergeResult();
                }
            }
            EndCard();

            if (!string.IsNullOrEmpty(_mergeMessage))
                EditorGUILayout.HelpBox(_mergeMessage, MessageType.Info);

            BeginCard("③ 合并预览");
            if (_mergeResultTex != null)
            {
                _mergeResultZoom = EditorGUILayout.IntSlider(new GUIContent("预览缩放", "结果预览每像素显示边长(1~16)"), _mergeResultZoom, 1, 16);
                float dw = _mergeResultW * _mergeResultZoom;
                float dh = _mergeResultH * _mergeResultZoom;
                float viewH = Mathf.Min(dh, 360f);
                _mergeResultScroll = EditorGUILayout.BeginScrollView(_mergeResultScroll, GUILayout.Height(viewH + 4));
                Rect area = GUILayoutUtility.GetRect(dw, dh, GUILayout.Width(dw), GUILayout.Height(dh));
                DrawChecker(area);
                GUI.DrawTexture(area, _mergeResultTex, ScaleMode.StretchToFill, true);
                DrawRectOutline(area, new Color(0, 0, 0, 0.5f));
                EditorGUILayout.EndScrollView();
                EditorGUILayout.LabelField($"输出 {_mergeResultW} × {_mergeResultH} 像素 · 放大 {_mergeResultZoom}×", _hintStyle);
            }
            else
            {
                EditorGUILayout.LabelField("（放入至少一张单图后显示合并预览）", _hintStyle);
            }
            EndCard();

            BeginCard("④ 导出");
            using (new EditorGUI.DisabledScope(_mergeResultTopDown == null))
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = kGenColor;
                if (GUILayout.Button(new GUIContent("导出图集（另存为）", "弹窗选择路径，导出合并后的图集 PNG"), GUILayout.Height(30)))
                    ExportMergeAs();
                GUI.backgroundColor = prev;

                string firstDir = GetMergeFirstSourceDir();
                using (new EditorGUI.DisabledScope(firstDir == null))
                {
                    if (GUILayout.Button(new GUIContent("导出到首张图目录", "导出到第一张有磁盘文件的单图所在目录，文件名 = merged_列x行.png")))
                        ExportMergeToFirstDir();
                }
                if (firstDir == null)
                    EditorGUILayout.LabelField("（槽位内单图均无磁盘文件，同目录导出不可用）", _hintStyle);
            }
            EndCard();
        }

        /// <summary>把 _mergeSlotTex / _mergeSlotPath 的长度对齐到 列×行，保留已有槽位内容。</summary>
        private void EnsureMergeSlotCount()
        {
            int need = Mathf.Max(1, _mergeCols) * Mathf.Max(1, _mergeRows);
            while (_mergeSlotTex.Count < need) _mergeSlotTex.Add(null);
            while (_mergeSlotPath.Count < need) _mergeSlotPath.Add("");
            if (_mergeSlotTex.Count > need) _mergeSlotTex.RemoveRange(need, _mergeSlotTex.Count - need);
            if (_mergeSlotPath.Count > need) _mergeSlotPath.RemoveRange(need, _mergeSlotPath.Count - need);
        }

        /// <summary>按列×行画出槽位网格：每格支持拖入图片 + 缩略图预览 + ObjectField 选择，改动即重建合并结果。</summary>
        private void DrawMergeSlotGrid()
        {
            int cols = Mathf.Max(1, _mergeCols), rows = Mathf.Max(1, _mergeRows);
            for (int r = 0; r < rows; r++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int c = 0; c < cols; c++)
                        DrawMergeSlot(r * cols + c);
                }
            }
        }

        /// <summary>画单个合并槽位：拖放区（工程内 Texture / 外部图片）+ 序号角标 + ObjectField。</summary>
        private void DrawMergeSlot(int index)
        {
            const float box = 68f;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(box)))
            {
                Rect cell = GUILayoutUtility.GetRect(box, box, GUILayout.Width(box), GUILayout.Height(box));
                Event e = Event.current;
                bool hover = cell.Contains(e.mousePosition);
                bool dragging = e.type == EventType.DragUpdated || e.type == EventType.DragPerform;
                bool valid = hover && dragging && IsMergeDragValid();

                DrawChecker(cell);
                Texture2D tex = index < _mergeSlotTex.Count ? _mergeSlotTex[index] : null;
                if (tex != null) GUI.DrawTexture(cell, tex, ScaleMode.ScaleToFit, true);
                DrawRectOutline(cell, valid ? kAccent : new Color(1, 1, 1, 0.22f));
                // 左上角序号角标
                var badge = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1, 1, 1, 0.85f) }, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(cell.x + 2, cell.y + 1, 24, 14), $"#{index + 1}", badge);

                if (hover && dragging)
                {
                    DragAndDrop.visualMode = IsMergeDragValid() ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    if (e.type == EventType.DragPerform && IsMergeDragValid())
                    {
                        DragAndDrop.AcceptDrag();
                        AcceptMergeDraggedImage(index);
                    }
                    e.Use();
                }

                EditorGUI.BeginChangeCheck();
                Texture2D picked = (Texture2D)EditorGUILayout.ObjectField(tex, typeof(Texture2D), false, GUILayout.Width(box));
                if (EditorGUI.EndChangeCheck())
                {
                    _mergeSlotTex[index] = picked;
                    _mergeSlotPath[index] = "";
                    RebuildMergeResult();
                }
            }
        }

        /// <summary>合并槽位当前拖拽是否为可接受的图片（工程内 Texture 或外部图片文件）。</summary>
        private static bool IsMergeDragValid()
        {
            foreach (var obj in DragAndDrop.objectReferences)
                if (obj is Texture2D) return true;
            return IsDragValid();
        }

        /// <summary>把拖入某个合并槽位的图片存入该槽：优先工程内 Texture，其次外部图片文件。</summary>
        private void AcceptMergeDraggedImage(int index)
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Texture2D tex)
                {
                    _mergeSlotTex[index] = tex; _mergeSlotPath[index] = "";
                    GUI.FocusControl(null); RebuildMergeResult();
                    return;
                }
            }
            foreach (var p in DragAndDrop.paths)
            {
                if (!IsImagePath(p)) continue;
                Texture2D asset = LoadAsProjectAsset(p);
                if (asset != null) { _mergeSlotTex[index] = asset; _mergeSlotPath[index] = ""; }
                else
                {
                    Texture2D ext = DecodeExternalImage(p);
                    if (ext == null) return;
                    _mergeSlotTex[index] = ext; _mergeSlotPath[index] = p;
                }
                GUI.FocusControl(null); RebuildMergeResult();
                return;
            }
        }

        /// <summary>把外部图片文件解码成临时 Texture2D（失败弹窗提示并返回 null）。</summary>
        private Texture2D DecodeExternalImage(string fullPath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes)) { DestroyImmediate(tex); EditorUtility.DisplayDialog("无法加载", $"无法解码图片：\n{fullPath}", "确定"); return null; }
                tex.name = Path.GetFileNameWithoutExtension(fullPath);
                return tex;
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("读取失败", ex.Message, "确定");
                Debug.LogException(ex);
                return null;
            }
        }

        /// <summary>清空合并结果数据与预览纹理。</summary>
        private void ClearMergeResult()
        {
            _mergeResultTopDown = null;
            _mergeResultW = _mergeResultH = 0;
            if (_mergeResultTex != null) { DestroyImmediate(_mergeResultTex); _mergeResultTex = null; }
        }

        /// <summary>
        /// 按当前槽位重建合并结果：格子 = 所有非空图的最大宽×最大高，
        /// 每张图按行优先放到对应列×行位置并在格子内居中，空白透明。
        /// </summary>
        private void RebuildMergeResult()
        {
            _mergeMessage = "";
            ClearMergeResult();
            EnsureMergeSlotCount();

            int cols = Mathf.Max(1, _mergeCols), rows = Mathf.Max(1, _mergeRows);
            int count = cols * rows;

            var pxArr = new Color32[count][];
            var wArr = new int[count];
            var hArr = new int[count];
            int cellW = 0, cellH = 0, filled = 0;
            bool sizeMixed = false;
            int firstW = -1, firstH = -1;

            for (int i = 0; i < count; i++)
            {
                Texture2D t = _mergeSlotTex[i];
                if (t == null) continue;
                ReadSourcePixels(t, _mergeSlotPath[i], out Color32[] raw, out int rw, out int rh);
                var td = new Color32[rw * rh];
                for (int y = 0; y < rh; y++)
                    for (int x = 0; x < rw; x++)
                        td[y * rw + x] = raw[(rh - 1 - y) * rw + x];
                pxArr[i] = td; wArr[i] = rw; hArr[i] = rh;
                cellW = Mathf.Max(cellW, rw); cellH = Mathf.Max(cellH, rh);
                if (firstW < 0) { firstW = rw; firstH = rh; }
                else if (rw != firstW || rh != firstH) sizeMixed = true;
                filled++;
            }

            if (filled == 0) { _mergeMessage = "尚未放入任何单图。"; return; }

            int outW = cols * cellW, outH = rows * cellH;
            var outPx = new Color32[outW * outH];
            for (int i = 0; i < outPx.Length; i++) outPx[i] = kTransparent;

            for (int i = 0; i < count; i++)
            {
                if (pxArr[i] == null) continue;
                int col = i % cols, row = i / cols;
                int dx0 = col * cellW, dy0 = row * cellH;
                int ox = (cellW - wArr[i]) / 2, oy = (cellH - hArr[i]) / 2;   // 格子内居中
                for (int y = 0; y < hArr[i]; y++)
                    for (int x = 0; x < wArr[i]; x++)
                        outPx[(dy0 + oy + y) * outW + (dx0 + ox + x)] = pxArr[i][y * wArr[i] + x];
            }

            _mergeResultTopDown = outPx;
            _mergeResultW = outW;
            _mergeResultH = outH;
            BuildMergeResultTexture();

            if (sizeMixed)
                _mergeMessage = $"存在尺寸不一的单图，已按最大格子 {cellW}×{cellH} 居中放置，空白透明。";
        }

        /// <summary>由 _mergeResultTopDown 重建预览纹理（自上而下翻成 Texture2D 的自下而上）。</summary>
        private void BuildMergeResultTexture()
        {
            if (_mergeResultTex != null) DestroyImmediate(_mergeResultTex);
            if (_mergeResultTopDown == null) { _mergeResultTex = null; return; }
            _mergeResultTex = new Texture2D(_mergeResultW, _mergeResultH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var flip = new Color32[_mergeResultW * _mergeResultH];
            for (int y = 0; y < _mergeResultH; y++)
                for (int x = 0; x < _mergeResultW; x++)
                    flip[(_mergeResultH - 1 - y) * _mergeResultW + x] = _mergeResultTopDown[y * _mergeResultW + x];
            _mergeResultTex.SetPixels32(flip);
            _mergeResultTex.Apply();
        }

        /// <summary>导出合并图集：弹窗选路径，另存为新 PNG。</summary>
        private void ExportMergeAs()
        {
            if (_mergeResultTopDown == null) return;
            string defaultName = $"merged_{_mergeCols}x{_mergeRows}.png";
            string path = EditorUtility.SaveFilePanel("导出合并图集 PNG", Application.dataPath, defaultName, "png");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllBytes(path, BuildPngFromTopDown(_mergeResultTopDown, _mergeResultW, _mergeResultH));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已导出", $"已保存：\n{path}", "确定");
            Debug.Log($"[PixelPerfect] 图片合并导出：{path}");
        }

        /// <summary>导出合并图集到首张有磁盘文件的单图所在目录，文件名 = merged_列x行.png。</summary>
        private void ExportMergeToFirstDir()
        {
            if (_mergeResultTopDown == null) return;
            string dir = GetMergeFirstSourceDir();
            if (string.IsNullOrEmpty(dir))
            {
                EditorUtility.DisplayDialog("无法导出", "槽位内单图均无可定位的磁盘目录。", "确定");
                return;
            }
            string path = Path.Combine(dir, $"merged_{_mergeCols}x{_mergeRows}.png");
            File.WriteAllBytes(path, BuildPngFromTopDown(_mergeResultTopDown, _mergeResultW, _mergeResultH));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已导出", $"已保存：\n{path}", "确定");
            Debug.Log($"[PixelPerfect] 图片合并同目录导出：{path}");
        }

        /// <summary>取第一张有磁盘文件的槽位单图所在目录；均无文件则返回 null。</summary>
        private string GetMergeFirstSourceDir()
        {
            for (int i = 0; i < _mergeSlotTex.Count; i++)
            {
                string abs = GetSlotFilePath(_mergeSlotTex[i], i < _mergeSlotPath.Count ? _mergeSlotPath[i] : "");
                if (abs != null) return Path.GetDirectoryName(abs);
            }
            return null;
        }

        /// <summary>取某槽位单图在磁盘上的绝对路径：优先外部文件，其次工程内资源；无文件返回 null。</summary>
        private static string GetSlotFilePath(Texture2D tex, string externalPath)
        {
            if (!string.IsNullOrEmpty(externalPath) && File.Exists(externalPath)) return externalPath;
            if (tex != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(tex);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string abs = GetAbsolutePath(assetPath);
                    if (File.Exists(abs)) return abs;
                }
            }
            return null;
        }

        /// <summary>步骤④的拖拽放置区：支持工程内 Texture 或外部图片文件，拖入即替换并重排。</summary>
        private void DrawAuxDropArea()
        {
            Rect drop = GUILayoutUtility.GetRect(0, 56, GUILayout.ExpandWidth(true));
            Event e = Event.current;
            bool hover = drop.Contains(e.mousePosition);
            bool dragging = e.type == EventType.DragUpdated || e.type == EventType.DragPerform;
            bool valid = hover && dragging && IsDragValid();

            EditorGUI.DrawRect(drop, valid ? new Color(kAccent.r, kAccent.g, kAccent.b, 0.18f) : new Color(1, 1, 1, 0.04f));
            DrawRectOutline(drop, valid ? kAccent : new Color(1, 1, 1, 0.18f));
            var centered = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } };
            GUI.Label(drop, _auxSourceTexture != null ? $"✔ 已选择：{_auxSourceTexture.name}（可再拖入替换）" : "⬇ 把帧动画图拖到这里（工程内 Texture 或外部图片）", centered);

            if (hover && dragging)
            {
                DragAndDrop.visualMode = IsDragValid() ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                if (e.type == EventType.DragPerform && IsDragValid())
                {
                    DragAndDrop.AcceptDrag();
                    AcceptAuxDraggedImage();
                }
                e.Use();
            }
        }

        /// <summary>接收拖入到步骤④的图片：优先工程内 Texture，其次外部文件，载入后立即重排。</summary>
        private void AcceptAuxDraggedImage()
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Texture2D tex)
                {
                    _auxSourceTexture = tex;
                    _auxSourceExternalPath = "";
                    GUI.FocusControl(null);
                    LoadAuxSource();
                    return;
                }
            }
            foreach (var p in DragAndDrop.paths)
            {
                if (!IsImagePath(p)) continue;
                Texture2D asset = LoadAsProjectAsset(p);
                if (asset != null) { _auxSourceTexture = asset; _auxSourceExternalPath = ""; }
                else LoadAuxExternalImageRef(p);
                GUI.FocusControl(null);
                LoadAuxSource();
                return;
            }
        }

        /// <summary>把拖入步骤④的外部图片文件解码成临时 Texture2D 作为源图引用。</summary>
        private void LoadAuxExternalImageRef(string fullPath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes)) { DestroyImmediate(tex); EditorUtility.DisplayDialog("无法加载", $"无法解码图片：\n{fullPath}", "确定"); return; }
                tex.name = Path.GetFileNameWithoutExtension(fullPath);
                _auxSourceTexture = tex;
                _auxSourceExternalPath = fullPath;
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("读取失败", ex.Message, "确定");
                Debug.LogException(ex);
            }
        }

        /// <summary>读取步骤④源图像素（转为自上而下数组）并构建显示纹理，随后立即重排。</summary>
        private void LoadAuxSource()
        {
            if (_auxSourceTexture == null) { ClearAuxSource(); RebuildAuxResult(); return; }

            Color32[] raw;
            int rw, rh;
            ReadSourcePixels(_auxSourceTexture, _auxSourceExternalPath, out raw, out rw, out rh);

            // raw 来自 GetPixels32：自下而上 → 转为自上而下
            var topDown = new Color32[rw * rh];
            for (int y = 0; y < rh; y++)
                for (int x = 0; x < rw; x++)
                    topDown[y * rw + x] = raw[(rh - 1 - y) * rw + x];
            _auxSrcTopDown = topDown;
            _auxSrcW = rw;
            _auxSrcH = rh;

            // 显示纹理：raw 本就是自下而上，可直接写入
            if (_auxDisplayTex != null) DestroyImmediate(_auxDisplayTex);
            _auxDisplayTex = new Texture2D(rw, rh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            _auxDisplayTex.SetPixels32(raw);
            _auxDisplayTex.Apply();

            RebuildAuxResult();
        }

        /// <summary>清空步骤④源图相关数据与纹理。</summary>
        private void ClearAuxSource()
        {
            if (_auxDisplayTex != null) { DestroyImmediate(_auxDisplayTex); _auxDisplayTex = null; }
            _auxSrcTopDown = null;
            _auxSrcW = _auxSrcH = 0;
        }

        /// <summary>清空步骤④重排结果数据与纹理。</summary>
        private void ClearAuxResult()
        {
            _auxResultTopDown = null;
            _auxResultW = _auxResultH = 0;
            if (_auxResultTex != null) { DestroyImmediate(_auxResultTex); _auxResultTex = null; }
        }

        /// <summary>
        /// 按帧数参数把原图重排：单帧 = 原图宽/原列、原图高/原行；
        /// 按行优先顺序把每帧搬到输出布局的对应行列位置。透明哨兵填充空帧位。
        /// </summary>
        private void RebuildAuxResult()
        {
            _auxMessage = "";
            if (_auxSrcTopDown == null) { ClearAuxResult(); return; }

            int sc = Mathf.Max(1, _auxSrcCols), sr = Mathf.Max(1, _auxSrcRows);
            int oc = Mathf.Max(1, _auxOutCols), or = Mathf.Max(1, _auxOutRows);
            int frameW = _auxSrcW / sc;
            int frameH = _auxSrcH / sr;
            if (frameW <= 0 || frameH <= 0)
            {
                ClearAuxResult();
                _auxMessage = "帧数过大：单帧尺寸不足 1 像素，请减小原图帧数。";
                return;
            }

            var notes = new List<string>();
            if (_auxSrcW % sc != 0 || _auxSrcH % sr != 0)
                notes.Add($"原图 {_auxSrcW}×{_auxSrcH} 不能被帧数整除，单帧按 {frameW}×{frameH} 取整，右/下边缘多余像素被忽略。");

            int srcFrames = sc * sr;
            int outCap = oc * or;
            if (outCap < srcFrames)
                notes.Add($"输出帧位 {outCap} 少于原帧数 {srcFrames}，多出的 {srcFrames - outCap} 帧被丢弃。");

            int outW = oc * frameW, outH = or * frameH;
            var outPx = new Color32[outW * outH];
            for (int i = 0; i < outPx.Length; i++) outPx[i] = kTransparent;

            int copy = Mathf.Min(srcFrames, outCap);
            for (int f = 0; f < copy; f++)
            {
                int scol = f % sc, srow = f / sc;
                int dcol = f % oc, drow = f / oc;
                int sx0 = scol * frameW, sy0 = srow * frameH;
                int dx0 = dcol * frameW, dy0 = drow * frameH;
                for (int y = 0; y < frameH; y++)
                    for (int x = 0; x < frameW; x++)
                        outPx[(dy0 + y) * outW + (dx0 + x)] = _auxSrcTopDown[(sy0 + y) * _auxSrcW + (sx0 + x)];
            }

            _auxResultTopDown = outPx;
            _auxResultW = outW;
            _auxResultH = outH;
            BuildAuxResultTexture();
            if (notes.Count > 0) _auxMessage = string.Join("\n", notes);
        }

        /// <summary>由 _auxResultTopDown 重建预览纹理（自上而下数组翻成 Texture2D 的自下而上）。</summary>
        private void BuildAuxResultTexture()
        {
            if (_auxResultTex != null) DestroyImmediate(_auxResultTex);
            if (_auxResultTopDown == null) { _auxResultTex = null; return; }
            _auxResultTex = new Texture2D(_auxResultW, _auxResultH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var flip = new Color32[_auxResultW * _auxResultH];
            for (int y = 0; y < _auxResultH; y++)
                for (int x = 0; x < _auxResultW; x++)
                    flip[(_auxResultH - 1 - y) * _auxResultW + x] = _auxResultTopDown[y * _auxResultW + x];
            _auxResultTex.SetPixels32(flip);
            _auxResultTex.Apply();
        }

        /// <summary>把重排结果（自上而下）编码为 PNG 字节。</summary>
        private byte[] BuildAuxResultPng() => BuildPngFromTopDown(_auxResultTopDown, _auxResultW, _auxResultH);

        /// <summary>把自上而下的像素数组翻成 Texture 自下而上后编码为 PNG 字节（帧排版/图片合并共用）。</summary>
        private static byte[] BuildPngFromTopDown(Color32[] topDown, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var flip = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    flip[(h - 1 - y) * w + x] = topDown[y * w + x];
            tex.SetPixels32(flip);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            DestroyImmediate(tex);
            return png;
        }

        /// <summary>不覆盖导出：弹窗选路径，把重排结果另存为新 PNG，不改动原图。</summary>
        private void ExportAuxAs()
        {
            if (_auxResultTopDown == null) return;
            string baseName = _auxSourceTexture != null ? _auxSourceTexture.name : "frame_relayout";
            string defaultName = $"{baseName}_relayout_{_auxOutCols}x{_auxOutRows}.png";
            string path = EditorUtility.SaveFilePanel("导出重排图 PNG", Application.dataPath, defaultName, "png");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllBytes(path, BuildAuxResultPng());
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已导出", $"已保存：\n{path}", "确定");
            Debug.Log($"[PixelPerfect] 帧排版导出：{path}");
        }

        /// <summary>覆盖原图导出：用重排结果写回原始图片文件（destructive，需确认）。</summary>
        private void ExportAuxOverwrite()
        {
            if (_auxResultTopDown == null) return;
            string src = GetAuxSourceFilePath();
            if (string.IsNullOrEmpty(src))
            {
                EditorUtility.DisplayDialog("无法覆盖", "原图没有可写回的磁盘文件。", "确定");
                return;
            }
            if (!EditorUtility.DisplayDialog("确认覆盖原图",
                    $"将用重排后的图（{_auxResultW}×{_auxResultH}）覆盖写回原始文件：\n{src}\n\n此操作不可撤销，确定吗？", "覆盖", "取消"))
                return;
            File.WriteAllBytes(src, BuildAuxResultPng());
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已覆盖", $"已写回：\n{src}", "确定");
            Debug.Log($"[PixelPerfect] 帧排版覆盖原图：{src}");
        }

        /// <summary>同原图目录导出：导出到原图所在目录，文件名 = 原图名_relayout_列x行.png。</summary>
        private void ExportAuxToSourceDir()
        {
            if (_auxResultTopDown == null) return;
            string src = GetAuxSourceFilePath();
            if (string.IsNullOrEmpty(src))
            {
                EditorUtility.DisplayDialog("无法导出", "原图没有可定位的磁盘目录。", "确定");
                return;
            }
            string dir = Path.GetDirectoryName(src);
            string baseName = Path.GetFileNameWithoutExtension(src);
            string path = Path.Combine(dir, $"{baseName}_relayout_{_auxOutCols}x{_auxOutRows}.png");
            File.WriteAllBytes(path, BuildAuxResultPng());
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已导出", $"已保存：\n{path}", "确定");
            Debug.Log($"[PixelPerfect] 帧排版同目录导出：{path}");
        }

        /// <summary>取步骤④源图在磁盘上的绝对路径：优先外部文件，其次工程内资源；无文件返回 null。</summary>
        private string GetAuxSourceFilePath()
        {
            if (!string.IsNullOrEmpty(_auxSourceExternalPath) && File.Exists(_auxSourceExternalPath))
                return _auxSourceExternalPath;
            if (_auxSourceTexture != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(_auxSourceTexture);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string abs = GetAbsolutePath(assetPath);
                    if (File.Exists(abs)) return abs;
                }
            }
            return null;
        }

        #endregion

        #region 编辑操作 - 绘制 / 橡皮 / 魔棒

        /// <summary>按当前工具与笔刷尺寸在指定位置涂格。</summary>
        private void PaintAt(Rect area, Vector2 mouse)
        {
            CellAt(area, mouse, out int i, out int j);
            Color32 col = _tool == EditTool.Eraser ? kTransparent : (Color32)_drawColor;
            if (_tool != EditTool.Eraser) col.a = 255;
            for (int dj = 0; dj < _brushSize; dj++)
            {
                for (int di = 0; di < _brushSize; di++)
                {
                    int ni = Mathf.Min(i + di, _gridWidth - 1);
                    int nj = Mathf.Min(j + dj, _gridHeight - 1);
                    _pixels[nj * _gridWidth + ni] = col;
                }
            }
            _drawingOccurred = true;
            _paletteDirty = true;
            BuildArtTexture();
            Repaint();
        }

        /// <summary>魔棒：收集笔刷区域内所有非透明色，分别从首个匹配格洪水填充为透明。</summary>
        private void DoMagicWand(Rect area, Vector2 mouse)
        {
            CellAt(area, mouse, out int i, out int j);
            int startI = i, startJ = j;
            int endI = Mathf.Min(_gridWidth - 1, i + _brushSize - 1);
            int endJ = Mathf.Min(_gridHeight - 1, j + _brushSize - 1);

            var colorsToDelete = new HashSet<int>();
            for (int m = startJ; m <= endJ; m++)
                for (int n = startI; n <= endI; n++)
                {
                    Color32 c = _pixels[m * _gridWidth + n];
                    if (c.a != 0) colorsToDelete.Add(PackRGB(c));
                }

            foreach (int packed in colorsToDelete)
            {
                bool filled = false;
                for (int m = startJ; m <= endJ && !filled; m++)
                    for (int n = startI; n <= endI && !filled; n++)
                    {
                        Color32 c = _pixels[m * _gridWidth + n];
                        if (c.a != 0 && PackRGB(c) == packed)
                        {
                            FloodFill(m, n, c);
                            filled = true;
                        }
                    }
            }
            BuildArtTexture();
            Repaint();
        }

        /// <summary>4 向洪水填充：从 (row,col) 出发，把与 target 颜色距离不超过阈值的非透明格抹为透明。</summary>
        private void FloodFill(int startRow, int startCol, Color32 target)
        {
            var stack = new Stack<(int row, int col)>();
            stack.Push((startRow, startCol));
            while (stack.Count > 0)
            {
                var (row, col) = stack.Pop();
                if (row < 0 || row >= _gridHeight || col < 0 || col >= _gridWidth) continue;
                Color32 cur = _pixels[row * _gridWidth + col];
                if (cur.a == 0) continue;
                if (ColorDistance(cur, target) > _magicWandThreshold) continue;
                _pixels[row * _gridWidth + col] = kTransparent;
                stack.Push((row - 1, col));
                stack.Push((row + 1, col));
                stack.Push((row, col - 1));
                stack.Push((row, col + 1));
            }
        }

        /// <summary>把颜色加入“最近使用”列表（去重、置顶、最多 6 个）。</summary>
        private void AddRecentColor(Color color)
        {
            Color32 c32 = color; c32.a = 255;
            int packed = PackRGB(c32);
            _recentColors.RemoveAll(c => { Color32 t = c; t.a = 255; return PackRGB(t) == packed; });
            _recentColors.Insert(0, color);
            while (_recentColors.Count > 6) _recentColors.RemoveAt(_recentColors.Count - 1);
        }

        #endregion

        #region 调色板 / 颜色替换

        /// <summary>
        /// 从当前 _pixels 提取所有不同颜色（按 RGBA 区分、忽略透明像素），
        /// 按使用量降序填充 _palette / _paletteCounts，并记录每个像素的调色板索引。
        /// </summary>
        private void ExtractPalette()
        {
            _palette.Clear();
            _paletteCounts.Clear();
            if (_pixels == null) { _pixelPaletteIndices = null; return; }

            var map = new Dictionary<uint, int>();
            int[] idxArr = new int[_pixels.Length];
            var colors = new List<Color32>();
            var counts = new List<int>();
            for (int k = 0; k < _pixels.Length; k++)
            {
                Color32 p = _pixels[k];
                if (p.a == 0) { idxArr[k] = -1; continue; }
                uint key = PackRGBA(p);
                if (!map.TryGetValue(key, out int pi))
                {
                    pi = colors.Count;
                    map[key] = pi;
                    colors.Add(p);
                    counts.Add(0);
                }
                idxArr[k] = pi;
                counts[pi]++;
            }

            // 按使用量降序排序，并把像素索引重映射到新顺序
            int n = colors.Count;
            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            Array.Sort(order, (a, b) => counts[b].CompareTo(counts[a]));
            int[] remap = new int[n];
            for (int newI = 0; newI < n; newI++) remap[order[newI]] = newI;
            for (int k = 0; k < idxArr.Length; k++)
                if (idxArr[k] >= 0) idxArr[k] = remap[idxArr[k]];

            for (int newI = 0; newI < n; newI++)
            {
                _palette.Add(colors[order[newI]]);
                _paletteCounts.Add(counts[order[newI]]);
            }
            _pixelPaletteIndices = idxArr;
        }

        /// <summary>把所有映射到调色板索引 i 的像素替换为 _palette[i]（alpha≈0 视为透明）。</summary>
        private void ReplaceByIndex(int i)
        {
            if (_pixelPaletteIndices == null || _pixels == null) return;
            Color c = _palette[i];
            bool transparent = c.a <= 0.0039f; // ≈ 1/255
            Color32 c32 = new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
            for (int k = 0; k < _pixels.Length; k++)
                if (_pixelPaletteIndices[k] == i)
                    _pixels[k] = transparent ? kTransparent : c32;
            BuildArtTexture();
            Repaint();
        }

        /// <summary>全宽绘制调色板/颜色替换面板：刷新、预览条、可编辑色块网格。</summary>
        private void DrawPaletteSection()
        {
            BeginCard($"调色板 / 颜色替换 · {_palette.Count} 种（编辑色块即全局替换该颜色的像素）");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("提取 / 刷新颜色", GUILayout.Width(120)))
                {
                    ExtractPalette();
                    _paletteDirty = false;
                }
                EditorGUILayout.LabelField(
                    _paletteDirty ? "⚠ 笔刷编辑后颜色可能已变，建议点刷新。"
                                  : "编辑色块 → 该颜色的所有像素被替换；A0=设为透明，A1=设为不透明，笔刷=取此色作画。",
                    _hintStyle);
            }

            if (_palette.Count == 0) { EndCard(); return; }

            // 色板预览条
            Rect strip = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            float seg = strip.width / _palette.Count;
            for (int i = 0; i < _palette.Count; i++)
            {
                Rect cell = new Rect(strip.x + seg * i, strip.y, Mathf.Ceil(seg), strip.height);
                EditorGUI.DrawRect(cell, new Color(_palette[i].r, _palette[i].g, _palette[i].b, 1f));
            }
            DrawRectOutline(strip, new Color(0, 0, 0, 0.4f));
            EditorGUILayout.Space(4);

            long total = 0;
            for (int i = 0; i < _paletteCounts.Count; i++) total += _paletteCounts[i];
            if (total <= 0) total = 1;

            const int perRow = 8;
            float cellW = Mathf.Max(60f, (EditorGUIUtility.currentViewWidth - 40f) / perRow - 4f);
            for (int row = 0; row * perRow < _palette.Count; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int c = 0; c < perRow; c++)
                    {
                        int i = row * perRow + c;
                        if (i >= _palette.Count) break;
                        DrawPaletteCell(i, cellW, total);
                    }
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.Space(4);
            }
            EndCard();
        }

        /// <summary>绘制单个调色板色块：颜色选择(替换)、透明快捷、取作画笔、占比。</summary>
        private void DrawPaletteCell(int i, float cellW, long total)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(cellW)))
            {
                EditorGUI.BeginChangeCheck();
                Color nc = EditorGUILayout.ColorField(GUIContent.none, _palette[i], false, true, false, GUILayout.Width(cellW));
                if (EditorGUI.EndChangeCheck())
                {
                    _palette[i] = nc;
                    ReplaceByIndex(i);
                    _palettePendingHistory = true;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    float bw = (cellW - 2f) / 2f;
                    if (GUILayout.Button(new GUIContent("A0", "把该颜色像素设为透明"), EditorStyles.miniButton, GUILayout.Width(bw)))
                    {
                        Color cc = _palette[i]; cc.a = 0f; _palette[i] = cc; ReplaceByIndex(i); _palettePendingHistory = true;
                    }
                    if (GUILayout.Button(new GUIContent("A1", "把该颜色设为不透明"), EditorStyles.miniButton, GUILayout.Width(bw)))
                    {
                        Color cc = _palette[i]; cc.a = 1f; _palette[i] = cc; ReplaceByIndex(i); _palettePendingHistory = true;
                    }
                }

                if (GUILayout.Button(new GUIContent("笔刷", "用此色作为画笔颜色"), EditorStyles.miniButton, GUILayout.Width(cellW)))
                {
                    _drawColor = _palette[i];
                    _tool = EditTool.Brush;
                }

                int cnt = i < _paletteCounts.Count ? _paletteCounts[i] : 0;
                float pct = cnt / (float)total;
                EditorGUILayout.LabelField(new GUIContent($"{pct * 100f:0.0}%", $"{cnt} px"), EditorStyles.miniLabel, GUILayout.Width(cellW));
            }
        }

        #endregion

        #region 撤销 / 重做

        /// <summary>压入当前像素状态到历史栈，并清空重做栈。</summary>
        private void SaveHistory()
        {
            _historyStack.Add((Color32[])_pixels.Clone());
            _redoStack.Clear();
        }

        /// <summary>撤销：回退到上一历史状态。</summary>
        private void Undo()
        {
            if (_historyStack.Count <= 1) return;
            _redoStack.Add(_historyStack[_historyStack.Count - 1]);
            _historyStack.RemoveAt(_historyStack.Count - 1);
            _pixels = (Color32[])_historyStack[_historyStack.Count - 1].Clone();
            BuildArtTexture();
            ExtractPalette();
            _paletteDirty = false;
            Repaint();
        }

        /// <summary>重做：恢复到下一历史状态。</summary>
        private void Redo()
        {
            if (_redoStack.Count == 0) return;
            Color32[] state = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _historyStack.Add((Color32[])state.Clone());
            _pixels = (Color32[])state.Clone();
            BuildArtTexture();
            ExtractPalette();
            _paletteDirty = false;
            Repaint();
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

        #region 转换核心 - 按算法采样每个网格

        /// <summary>遍历所有网格，按所选算法采样源图，生成 _pixels 像素数据。</summary>
        private void Convert()
        {
            _pixels = new Color32[_gridWidth * _gridHeight];
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = kTransparent;

            float cellW = _previewCellSize; // 画布每格 = previewCellSize 像素（canvas.width/gridWidth）
            float cellH = _previewCellSize;

            for (int j = 0; j < _gridHeight; j++)
            {
                for (int i = 0; i < _gridWidth; i++)
                {
                    float cx0 = i * cellW, cy0 = j * cellH;
                    float cx1 = cx0 + cellW, cy1 = cy0 + cellH;

                    // 邻域算法：四周外扩 25%
                    if (_method == ConvMethod.Neighbor)
                    {
                        float mx = cellW * 0.25f, my = cellH * 0.25f;
                        cx0 -= mx; cy0 -= my; cx1 += mx; cy1 += my;
                    }

                    // 画布坐标 → 源图坐标
                    float ox0 = (cx0 - _offsetX) / _imageScale;
                    float oy0 = (cy0 - _offsetY) / _imageScale;
                    float ox1 = (cx1 - _offsetX) / _imageScale;
                    float oy1 = (cy1 - _offsetY) / _imageScale;
                    int rOx0 = Mathf.Max(0, Mathf.FloorToInt(ox0));
                    int rOy0 = Mathf.Max(0, Mathf.FloorToInt(oy0));
                    int rOx1 = Mathf.Min(_srcW, Mathf.CeilToInt(ox1));
                    int rOy1 = Mathf.Min(_srcH, Mathf.CeilToInt(oy1));
                    if (rOx1 <= rOx0 || rOy1 <= rOy0) continue; // 透明

                    Color32 result;
                    bool hasColor;
                    if (_method == ConvMethod.Average || _method == ConvMethod.Neighbor)
                        hasColor = SampleAverage(rOx0, rOy0, rOx1, rOy1, out result);
                    else if (_method == ConvMethod.Most)
                        hasColor = SampleMostUsed(rOx0, rOy0, rOx1, rOy1, out result);
                    else // MostLight / MostDark
                        hasColor = SampleWeighted(rOx0, rOy0, rOx1, rOy1, _method, out result);

                    if (hasColor)
                        _pixels[j * _gridWidth + i] = result;
                }
            }

            // 限制最终颜色数量：超出上限则对生成图做颜色量化
            if (_limitColors)
                QuantizeToMaxColors(_maxColors, _colorLimitMode);
        }

        /// <summary>
        /// 把生成图 _pixels 的不同颜色量化到不超过 maxColors 种：
        /// 频率加权最远点采样选初始代表色 → K-means 精化（代表色取簇内最常用色）→ 每个像素映射到最近代表色。
        /// 透明像素保持透明。
        /// </summary>
        private void QuantizeToMaxColors(int maxColors, ColorLimitMode mode)
        {
            if (_pixels == null || maxColors < 1) return;

            // 1. 直方图（忽略透明像素，按 RGB 统计）
            var hist = new Dictionary<int, int>();
            for (int k = 0; k < _pixels.Length; k++)
            {
                Color32 p = _pixels[k];
                if (p.a == 0) continue;
                int key = PackRGB(p);
                hist.TryGetValue(key, out int n);
                hist[key] = n + 1;
            }
            int distinct = hist.Count;
            if (distinct <= maxColors) return; // 已满足，无需量化

            int[] colors = new int[distinct];
            int[] counts = new int[distinct];
            {
                int idx = 0;
                foreach (var kv in hist) { colors[idx] = kv.Key; counts[idx] = kv.Value; idx++; }
            }

            int kc = Mathf.Min(maxColors, distinct);

            // 2. 频率加权最远点采样选种子
            int[] seedIdx = new int[kc];
            int firstIdx = 0;
            for (int i = 1; i < distinct; i++) if (counts[i] > counts[firstIdx]) firstIdx = i;
            seedIdx[0] = firstIdx;
            double[] minDistSq = new double[distinct];
            for (int i = 0; i < distinct; i++) minDistSq[i] = ColorDistSq(colors[i], colors[firstIdx]);
            for (int s = 1; s < kc; s++)
            {
                int best = -1; double bestScore = -1;
                for (int i = 0; i < distinct; i++)
                {
                    double score = (double)counts[i] * minDistSq[i];
                    if (score > bestScore) { bestScore = score; best = i; }
                }
                if (best < 0) best = firstIdx;
                seedIdx[s] = best;
                for (int i = 0; i < distinct; i++)
                {
                    double d = ColorDistSq(colors[i], colors[best]);
                    if (d < minDistSq[i]) minDistSq[i] = d;
                }
            }

            int[] centers = new int[kc];
            for (int s = 0; s < kc; s++) centers[s] = colors[seedIdx[s]];

            // 3. K-means 精化（代表色取簇内使用最多的颜色）
            const int kIterations = 6;
            int[] assign = new int[distinct];
            for (int iter = 0; iter < kIterations; iter++)
            {
                bool changed = false;
                for (int i = 0; i < distinct; i++)
                {
                    int nearest = 0; double nd = double.MaxValue;
                    for (int s = 0; s < kc; s++)
                    {
                        double d = ColorDistSq(colors[i], centers[s]);
                        if (d < nd) { nd = d; nearest = s; }
                    }
                    if (assign[i] != nearest) { assign[i] = nearest; changed = true; }
                }
                int[] newCenters = new int[kc];
                int[] newCenterCount = new int[kc];
                bool[] hasMember = new bool[kc];
                for (int i = 0; i < distinct; i++)
                {
                    int s = assign[i];
                    if (!hasMember[s] || counts[i] > newCenterCount[s])
                    {
                        hasMember[s] = true;
                        newCenters[s] = colors[i];
                        newCenterCount[s] = counts[i];
                    }
                }
                for (int s = 0; s < kc; s++) if (hasMember[s]) centers[s] = newCenters[s];
                if (!changed && iter > 0) break;
            }

            // 3.5 固定模式：若有空簇致代表色不足 kc，用"当前量化误差最大的元色"补成新代表色，尽量凑满 kc 种（色数不足时自然停止）
            if (mode == ColorLimitMode.Exact)
            {
                bool[] used = new bool[kc];
                for (int i = 0; i < distinct; i++) used[assign[i]] = true;
                for (int s = 0; s < kc; s++)
                {
                    if (used[s]) continue;
                    int far = -1; double farD = 0;
                    for (int i = 0; i < distinct; i++)
                    {
                        double d = ColorDistSq(colors[i], centers[assign[i]]);
                        if (d > farD) { farD = d; far = i; }
                    }
                    if (far < 0 || farD <= 0) break; // 色数已用尽，无法再分裂
                    centers[s] = colors[far]; assign[far] = s; used[s] = true;
                }
            }

            // 4. 把每个非透明像素映射到最近的代表色
            for (int k = 0; k < _pixels.Length; k++)
            {
                Color32 p = _pixels[k];
                if (p.a == 0) continue;
                int pkey = PackRGB(p);
                int nearest = 0; double nd = double.MaxValue;
                for (int s = 0; s < kc; s++)
                {
                    double d = ColorDistSq(pkey, centers[s]);
                    if (d < nd) { nd = d; nearest = s; }
                }
                Color32 c = UnpackRGB(centers[nearest]);
                _pixels[k] = new Color32(c.r, c.g, c.b, 255);
            }
        }

        /// <summary>两个 0xRRGGBB 整数颜色的 RGB 距离平方。</summary>
        private static double ColorDistSq(int a, int b)
        {
            int ar = (a >> 16) & 0xFF, ag = (a >> 8) & 0xFF, ab = a & 0xFF;
            int br = (b >> 16) & 0xFF, bg = (b >> 8) & 0xFF, bb = b & 0xFF;
            double dr = ar - br, dg = ag - bg, db = ab - bb;
            return dr * dr + dg * dg + db * db;
        }

        /// <summary>区域平均：返回非透明像素 RGB 均值。无有效像素返回 false。</summary>
        private bool SampleAverage(int x0, int y0, int x1, int y1, out Color32 result)
        {
            long sumR = 0, sumG = 0, sumB = 0; int count = 0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    Color32 c = _srcTopDown[y * _srcW + x];
                    if (c.a == 0) continue;
                    sumR += c.r; sumG += c.g; sumB += c.b; count++;
                }
            if (count == 0) { result = kTransparent; return false; }
            result = new Color32((byte)Mathf.RoundToInt(sumR / (float)count),
                                 (byte)Mathf.RoundToInt(sumG / (float)count),
                                 (byte)Mathf.RoundToInt(sumB / (float)count), 255);
            return true;
        }

        /// <summary>最常用颜色：精确色直方图 → 相近色聚类 → 主簇内取使用最多色。</summary>
        private bool SampleMostUsed(int x0, int y0, int x1, int y1, out Color32 result)
        {
            // 直方图
            var counts = new Dictionary<int, int>();
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    Color32 c = _srcTopDown[y * _srcW + x];
                    if (c.a == 0) continue;
                    int key = PackRGB(c);
                    counts.TryGetValue(key, out int n);
                    counts[key] = n + 1;
                }
            if (counts.Count == 0) { result = kTransparent; return false; }
            if (counts.Count == 1)
            {
                foreach (var kv in counts) { result = UnpackRGB(kv.Key); return true; }
            }

            // 按频率降序
            var entries = new List<KeyValuePair<int, int>>(counts);
            entries.Sort((a, b) => b.Value.CompareTo(a.Value));

            // 聚类
            var clusters = new List<Cluster>();
            int thrSq = _similarityThreshold * _similarityThreshold;
            foreach (var entry in entries)
            {
                Color32 color = UnpackRGB(entry.Key);
                int count = entry.Value;
                Cluster matched = null;
                foreach (var cl in clusters)
                {
                    int dr = color.r - cl.repR, dg = color.g - cl.repG, db = color.b - cl.repB;
                    if (dr * dr + dg * dg + db * db <= thrSq) { matched = cl; break; }
                }
                if (matched != null)
                {
                    int t = matched.totalCount + count;
                    int w = count;
                    matched.repR = Mathf.RoundToInt((matched.repR * (t - w) + color.r * w) / (float)t);
                    matched.repG = Mathf.RoundToInt((matched.repG * (t - w) + color.g * w) / (float)t);
                    matched.repB = Mathf.RoundToInt((matched.repB * (t - w) + color.b * w) / (float)t);
                    matched.totalCount = t;
                    if (count > matched.bestMemberCount)
                    {
                        matched.bestMemberCount = count;
                        matched.bestMember = color;
                    }
                }
                else
                {
                    clusters.Add(new Cluster
                    {
                        repR = color.r, repG = color.g, repB = color.b,
                        totalCount = count, bestMemberCount = count, bestMember = color
                    });
                }
            }

            // 取总数最大的簇 → 簇内使用最多的颜色
            Cluster dom = clusters[0];
            foreach (var cl in clusters) if (cl.totalCount > dom.totalCount) dom = cl;
            result = dom.bestMember;
            return true;
        }

        /// <summary>亮度加权（偏亮/偏暗）：按权重聚类并取主簇加权平均色。</summary>
        private bool SampleWeighted(int x0, int y0, int x1, int y1, ConvMethod method, out Color32 result)
        {
            var clusters = new List<WeightedCluster>();
            int thrSq = _similarityThreshold * _similarityThreshold;
            bool any = false;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    Color32 c = _srcTopDown[y * _srcW + x];
                    if (c.a == 0) continue;
                    any = true;
                    float brightness = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                    float raw = method == ConvMethod.MostLight ? brightness / 255f : (255f - brightness) / 255f;
                    float weight = 0.25f + 0.50f * raw;

                    WeightedCluster matched = null;
                    foreach (var cl in clusters)
                    {
                        int dr = c.r - Mathf.RoundToInt(cl.repR), dg = c.g - Mathf.RoundToInt(cl.repG), db = c.b - Mathf.RoundToInt(cl.repB);
                        if (dr * dr + dg * dg + db * db <= thrSq) { matched = cl; break; }
                    }
                    if (matched != null)
                    {
                        matched.totalWeight += weight;
                        matched.sumR += c.r * weight; matched.sumG += c.g * weight; matched.sumB += c.b * weight;
                        matched.repR = matched.sumR / matched.totalWeight;
                        matched.repG = matched.sumG / matched.totalWeight;
                        matched.repB = matched.sumB / matched.totalWeight;
                    }
                    else
                    {
                        clusters.Add(new WeightedCluster
                        {
                            repR = c.r, repG = c.g, repB = c.b,
                            totalWeight = weight,
                            sumR = c.r * weight, sumG = c.g * weight, sumB = c.b * weight
                        });
                    }
                }
            if (!any || clusters.Count == 0) { result = kTransparent; return false; }

            WeightedCluster dom = clusters[0];
            foreach (var cl in clusters) if (cl.totalWeight > dom.totalWeight) dom = cl;
            result = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(dom.repR), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(dom.repG), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(dom.repB), 0, 255), 255);
            return true;
        }

        /// <summary>普通聚类簇（最常用算法用）。</summary>
        private class Cluster
        {
            public int repR, repG, repB;
            public int totalCount;
            public int bestMemberCount;
            public Color32 bestMember;
        }

        /// <summary>加权聚类簇（偏亮/偏暗算法用）。</summary>
        private class WeightedCluster
        {
            public float repR, repG, repB;
            public float totalWeight;
            public float sumR, sumG, sumB;
        }

        #endregion

        #region 智能网格检测 - perfectPixel 移植（Sobel 梯度）

        // 移植自 https://github.com/theamusing/perfectPixel 的纯 numpy 后端 perfect_pixel_noCV2.py。
        // 原实现主检测器为 FFT 频谱分析、梯度法为回退；本移植采用梯度法(estimate_grid_gradient)作为主检测器，
        // 免去在 C# 手写 2D-FFT 的复杂度与正确性风险（FFT 主检测器可作为后续增强）；
        // refine_grids / find_best_grid / 采样与 fix_square 逻辑忠实移植，采样复用本工具已有的 5 种取色算法。

        /// <summary>仅检测网格尺寸并填入 _gridWidth/_gridHeight + 居中适配预览，供用户核对或手动微调后再转换。</summary>
        private void AutoDetectGridSizeOnly()
        {
            if (_srcTopDown == null) { _autoMessage = "尚未载入源图。"; return; }
            if (!DetectGridScale(out int gw, out int gh))
            {
                _autoMessage = "检测失败：未找到规则网格（梯度峰值不足）。请手动设置网格尺寸。";
                return;
            }
            _gridWidth = Mathf.Clamp(gw, 1, kMaxImageSize);
            _gridHeight = Mathf.Clamp(gh, 1, kMaxImageSize);
            RefitImage();
            _autoMessage = $"检测到网格 {_gridWidth}×{_gridHeight}，已填入并居中适配。可微调后手动转换，或点“智能一键转换”。";
        }

        /// <summary>一键：检测尺寸 →（可选）Sobel 边缘对齐 refine → 采样生成像素图 → 进入步骤③。</summary>
        private void AutoDetectAndConvert()
        {
            if (_srcTopDown == null) { _autoMessage = "尚未载入源图。"; return; }
            if (!DetectGridScale(out int gw, out int gh))
            {
                _autoMessage = "检测失败：未找到规则网格（梯度峰值不足）。请手动设置网格尺寸后转换。";
                return;
            }

            List<int> xs, ys;
            if (_autoUseRefine)
                RefineGrids(gw, gh, _refineIntensity, out xs, out ys);
            else
                BuildUniformGrid(gw, gh, out xs, out ys);

            if (xs.Count < 2 || ys.Count < 2)
            {
                _autoMessage = "检测到网格但网格线对齐失败，请改用手动模式。";
                return;
            }

            ConvertByCoords(xs, ys);
            _autoMessage = $"完成：检测 {gw}×{gh} → 输出 {_gridWidth}×{_gridHeight}。";
            EnterStep3();
        }

        /// <summary>梯度法检测网格尺寸（格子数）。对应 detect_grid_scale 的梯度分支。返回 false 表示检测失败。</summary>
        private bool DetectGridScale(out int gridW, out int gridH)
        {
            gridW = gridH = 0;
            int W = _srcW, H = _srcH;
            if (W < 8 || H < 8) return false;

            float[] gray = RgbToGray(_srcTopDown, W, H);
            SobelAbsProjections(gray, W, H, out float[] gxSum, out float[] gySum);
            if (!EstimateGridGradient(gxSum, gySum, W, H, 0.2f, 4, out int gw, out int gh))
                return false;

            // 由检测尺寸推像素块边长，再回推格子数：对齐长宽比、稳定结果（对应原逻辑）。
            double pxX = (double)W / gw, pxY = (double)H / gh;
            const double maxRatio = 1.5;
            double pixelSize = (pxX / pxY > maxRatio || pxY / pxX > maxRatio)
                ? Math.Min(pxX, pxY)
                : (pxX + pxY) * 0.5;
            if (pixelSize < 1e-6) return false;

            gridW = (int)Math.Round(W / pixelSize);
            gridH = (int)Math.Round(H / pixelSize);
            return gridW >= 1 && gridH >= 1;
        }

        /// <summary>梯度法估计网格尺寸：找梯度投影峰 → 峰间距中位数 → 尺寸/中位间距。峰不足 4 返回 false。</summary>
        private static bool EstimateGridGradient(float[] gxSum, float[] gySum, int W, int H, float relThr, int minInterval, out int gridW, out int gridH)
        {
            gridW = gridH = 0;
            List<int> px = FindProjectionPeaks(gxSum, relThr, minInterval);
            List<int> py = FindProjectionPeaks(gySum, relThr, minInterval);
            if (px.Count < 4 || py.Count < 4) return false;

            double mx = MedianInterval(px), my = MedianInterval(py);
            if (mx < 1e-6 || my < 1e-6) return false;

            gridW = (int)Math.Round(W / mx);
            gridH = (int)Math.Round(H / my);
            return gridW >= 1 && gridH >= 1;
        }

        /// <summary>在一维梯度投影里找局部峰：值大于左右邻且 ≥ relThr×峰值，且与上个峰间隔 ≥ minInterval。</summary>
        private static List<int> FindProjectionPeaks(float[] v, float relThr, int minInterval)
        {
            var peaks = new List<int>();
            float mx = 0f;
            for (int i = 0; i < v.Length; i++) if (v[i] > mx) mx = v[i];
            float thr = relThr * mx;
            for (int i = 1; i < v.Length - 1; i++)
            {
                if (v[i] > v[i - 1] && v[i] > v[i + 1] && v[i] >= thr)
                {
                    if (peaks.Count == 0 || i - peaks[peaks.Count - 1] >= minInterval)
                        peaks.Add(i);
                }
            }
            return peaks;
        }

        /// <summary>相邻峰间距的中位数（空返回 0）。</summary>
        private static double MedianInterval(List<int> peaks)
        {
            var iv = new List<int>();
            for (int i = 1; i < peaks.Count; i++) iv.Add(peaks[i] - peaks[i - 1]);
            if (iv.Count == 0) return 0;
            iv.Sort();
            int m = iv.Count / 2;
            return (iv.Count % 2 == 1) ? iv[m] : (iv[m - 1] + iv[m]) / 2.0;
        }

        /// <summary>Sobel 边缘对齐：从中心向两侧按格宽步进，每条网格线用 find_best_grid 吸附到梯度峰，输出排序去重后的网格线坐标（源图空间）。</summary>
        private void RefineGrids(int gridX, int gridY, float intensity, out List<int> xs, out List<int> ys)
        {
            int W = _srcW, H = _srcH;
            float[] gray = RgbToGray(_srcTopDown, W, H);
            SobelAbsProjections(gray, W, H, out float[] gxSum, out float[] gySum);
            double cellW = (double)W / gridX, cellH = (double)H / gridY;
            double range = intensity;
            xs = new List<int>();
            ys = new List<int>();

            int guardX = gridX + gridY + 16, guardY = guardX;

            // X 正向：中心向右
            double x = FindBestGrid(W / 2.0, cellW, cellW, gxSum);
            int guard = 0;
            while (x < W + cellW / 2.0 && guard++ < guardX)
            {
                x = FindBestGrid(x, cellW * range, cellW * range, gxSum);
                xs.Add((int)Math.Round(x));
                x += cellW;
            }
            // X 负向：中心向左
            x = FindBestGrid(W / 2.0, cellW, cellW, gxSum) - cellW;
            guard = 0;
            while (x > -cellW / 2.0 && guard++ < guardX)
            {
                x = FindBestGrid(x, cellW * range, cellW * range, gxSum);
                xs.Add((int)Math.Round(x));
                x -= cellW;
            }
            // Y 正向：中心向下
            double y = FindBestGrid(H / 2.0, cellH, cellH, gySum);
            guard = 0;
            while (y < H + cellH / 2.0 && guard++ < guardY)
            {
                y = FindBestGrid(y, cellH * range, cellH * range, gySum);
                ys.Add((int)Math.Round(y));
                y += cellH;
            }
            // Y 负向：中心向上
            y = FindBestGrid(H / 2.0, cellH, cellH, gySum) - cellH;
            guard = 0;
            while (y > -cellH / 2.0 && guard++ < guardY)
            {
                y = FindBestGrid(y, cellH * range, cellH * range, gySum);
                ys.Add((int)Math.Round(y));
                y -= cellH;
            }

            xs.Sort();
            ys.Sort();
            DedupSortedCoords(xs);
            DedupSortedCoords(ys);
        }

        /// <summary>在 [origin-rangeMin, origin+rangeMax] 内找梯度最强的局部峰作为网格线位置；无峰则返回 round(origin)。对应 find_best_grid(thr=0)。</summary>
        private static int FindBestGrid(double origin, double rangeMin, double rangeMax, float[] grad)
        {
            int best = (int)Math.Round(origin);
            float mx = 0f;
            for (int i = 0; i < grad.Length; i++) if (grad[i] > mx) mx = grad[i];
            if (mx < 1e-6f) return best;

            int lo = -(int)Math.Round(rangeMin), hi = (int)Math.Round(rangeMax);
            float bestVal = -1f;
            for (int i = lo; i <= hi; i++)
            {
                int cand = (int)Math.Round(origin + i);
                if (cand <= 0 || cand >= grad.Length - 1) continue;
                if (grad[cand] > grad[cand - 1] && grad[cand] > grad[cand + 1])
                {
                    if (grad[cand] > bestVal) { bestVal = grad[cand]; best = cand; }
                }
            }
            return best;
        }

        /// <summary>不做 refine 时按检测尺寸生成均匀网格线（源图空间）。</summary>
        private void BuildUniformGrid(int gridX, int gridY, out List<int> xs, out List<int> ys)
        {
            xs = new List<int>();
            ys = new List<int>();
            for (int i = 0; i <= gridX; i++) xs.Add((int)Math.Round((double)_srcW * i / gridX));
            for (int j = 0; j <= gridY; j++) ys.Add((int)Math.Round((double)_srcH * j / gridY));
        }

        /// <summary>按给定网格线坐标（源图空间）逐格采样生成 _pixels，并同步 _gridWidth/_gridHeight；支持 fix_square 与颜色数量限制。</summary>
        private void ConvertByCoords(List<int> xs, List<int> ys)
        {
            int nx = xs.Count - 1, ny = ys.Count - 1;
            Color32[] outPix = new Color32[nx * ny];
            for (int k = 0; k < outPix.Length; k++) outPix[k] = kTransparent;

            for (int j = 0; j < ny; j++)
            {
                int y0 = Mathf.Clamp(ys[j], 0, _srcH), y1 = Mathf.Clamp(ys[j + 1], 0, _srcH);
                if (y1 <= y0) y1 = Mathf.Min(y0 + 1, _srcH);
                for (int i = 0; i < nx; i++)
                {
                    int x0 = Mathf.Clamp(xs[i], 0, _srcW), x1 = Mathf.Clamp(xs[i + 1], 0, _srcW);
                    if (x1 <= x0) x1 = Mathf.Min(x0 + 1, _srcW);
                    if (SampleSourceRect(x0, y0, x1, y1, out Color32 c))
                        outPix[j * nx + i] = c;
                }
            }

            if (_autoFixSquare && Math.Abs(nx - ny) == 1)
                FixSquare(ref outPix, ref nx, ref ny);

            _gridWidth = nx;
            _gridHeight = ny;
            _pixels = outPix;
            if (_limitColors) QuantizeToMaxColors(_maxColors, _colorLimitMode);
        }

        /// <summary>按当前取色算法对一个源图矩形采样（复用 Convert 的采样器）；邻域算法四周外扩 25%。</summary>
        private bool SampleSourceRect(int x0, int y0, int x1, int y1, out Color32 result)
        {
            if (_method == ConvMethod.Neighbor)
            {
                int mx = Mathf.RoundToInt((x1 - x0) * 0.25f), my = Mathf.RoundToInt((y1 - y0) * 0.25f);
                x0 = Mathf.Max(0, x0 - mx); y0 = Mathf.Max(0, y0 - my);
                x1 = Mathf.Min(_srcW, x1 + mx); y1 = Mathf.Min(_srcH, y1 + my);
            }
            if (_method == ConvMethod.Average || _method == ConvMethod.Neighbor)
                return SampleAverage(x0, y0, x1, y1, out result);
            if (_method == ConvMethod.Most)
                return SampleMostUsed(x0, y0, x1, y1, out result);
            return SampleWeighted(x0, y0, x1, y1, _method, out result);
        }

        /// <summary>近正方形强制正方（对应原工具 fix_square）：长宽仅差 1 时按奇偶裁末列/末行或复制首行/首列补齐。</summary>
        private static void FixSquare(ref Color32[] pix, ref int nx, ref int ny)
        {
            if (nx > ny)
            {
                if (nx % 2 == 1) // 裁掉最后一列
                {
                    int nnx = nx - 1;
                    var np = new Color32[nnx * ny];
                    for (int j = 0; j < ny; j++)
                        for (int i = 0; i < nnx; i++) np[j * nnx + i] = pix[j * nx + i];
                    pix = np; nx = nnx;
                }
                else // 顶部复制首行补一行
                {
                    int nny = ny + 1;
                    var np = new Color32[nx * nny];
                    for (int i = 0; i < nx; i++) np[i] = pix[i];
                    for (int j = 0; j < ny; j++)
                        for (int i = 0; i < nx; i++) np[(j + 1) * nx + i] = pix[j * nx + i];
                    pix = np; ny = nny;
                }
            }
            else // ny > nx
            {
                if (ny % 2 == 1) // 裁掉最后一行
                {
                    int nny = ny - 1;
                    var np = new Color32[nx * nny];
                    for (int j = 0; j < nny; j++)
                        for (int i = 0; i < nx; i++) np[j * nx + i] = pix[j * nx + i];
                    pix = np; ny = nny;
                }
                else // 左侧复制首列补一列
                {
                    int nnx = nx + 1;
                    var np = new Color32[nnx * ny];
                    for (int j = 0; j < ny; j++)
                    {
                        np[j * nnx] = pix[j * nx];
                        for (int i = 0; i < nx; i++) np[j * nnx + (i + 1)] = pix[j * nx + i];
                    }
                    pix = np; nx = nnx;
                }
            }
        }

        /// <summary>已排序坐标去除相邻重复项（吸附到同一峰会产生重复网格线，避免出现零宽格子）。</summary>
        private static void DedupSortedCoords(List<int> c)
        {
            for (int i = c.Count - 1; i > 0; i--)
                if (c[i] == c[i - 1]) c.RemoveAt(i);
        }

        /// <summary>源图（自上而下）转灰度：0.299R+0.587G+0.114B，忽略透明。</summary>
        private static float[] RgbToGray(Color32[] src, int w, int h)
        {
            float[] g = new float[w * h];
            for (int i = 0; i < g.Length; i++)
            {
                Color32 c = src[i];
                g[i] = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            }
            return g;
        }

        /// <summary>3×3 Sobel 卷积（边缘 clamp），输出按列求和的 |gx|（长 W）与按行求和的 |gy|（长 H），用于网格线检测。</summary>
        private static void SobelAbsProjections(float[] gray, int w, int h, out float[] gxSum, out float[] gySum)
        {
            gxSum = new float[w];
            gySum = new float[h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float p00 = Gp(gray, w, h, x - 1, y - 1), p01 = Gp(gray, w, h, x, y - 1), p02 = Gp(gray, w, h, x + 1, y - 1);
                    float p10 = Gp(gray, w, h, x - 1, y), p12 = Gp(gray, w, h, x + 1, y);
                    float p20 = Gp(gray, w, h, x - 1, y + 1), p21 = Gp(gray, w, h, x, y + 1), p22 = Gp(gray, w, h, x + 1, y + 1);
                    float gx = (-p00 + p02) + (-2f * p10 + 2f * p12) + (-p20 + p22);
                    float gy = (-p00 - 2f * p01 - p02) + (p20 + 2f * p21 + p22);
                    gxSum[x] += Mathf.Abs(gx);
                    gySum[y] += Mathf.Abs(gy);
                }
            }
        }

        /// <summary>灰度取样（越界 clamp 到边缘），供 Sobel 卷积用。</summary>
        private static float Gp(float[] g, int w, int h, int x, int y)
        {
            if (x < 0) x = 0; else if (x >= w) x = w - 1;
            if (y < 0) y = 0; else if (y >= h) y = h - 1;
            return g[y * w + x];
        }

        #endregion

        #region 渲染 / 导出

        /// <summary>根据 _pixels 重建编辑画布纹理（自上而下数组翻成 Texture2D 的自下而上）。</summary>
        private void BuildArtTexture()
        {
            if (_pixels == null) return;
            if (_artTex == null || _artTex.width != _gridWidth || _artTex.height != _gridHeight)
            {
                if (_artTex != null) DestroyImmediate(_artTex);
                _artTex = new Texture2D(_gridWidth, _gridHeight, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            }
            Color32[] tex = new Color32[_gridWidth * _gridHeight];
            for (int j = 0; j < _gridHeight; j++)
                for (int i = 0; i < _gridWidth; i++)
                    tex[(_gridHeight - 1 - j) * _gridWidth + i] = _pixels[j * _gridWidth + i];
            _artTex.SetPixels32(tex);
            _artTex.Apply();
        }

        /// <summary>按指定倍数导出整图 PNG（弹窗选择路径）：每格渲染为 scale×scale 块，透明格留空。</summary>
        private void ExportImage(int scale)
        {
            if (_pixels == null) return;
            string defaultName = $"pixel_art_{_gridWidth}x{_gridHeight}_x{scale}.png";
            string path = EditorUtility.SaveFilePanel("导出像素图 PNG", Application.dataPath, defaultName, "png");
            if (string.IsNullOrEmpty(path)) return;

            byte[] png = BuildRegionPng(0, 0, _gridWidth, _gridHeight, scale);
            File.WriteAllBytes(path, png);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已导出", $"已保存：\n{path}", "确定");
            Debug.Log($"[PixelPerfect] 导出：{path}");
        }

        /// <summary>覆盖原图导出：用当前像素图（×scale）写回原始图片文件（destructive，需确认）。</summary>
        private void ExportOverwriteOriginal(int scale)
        {
            if (_pixels == null) return;
            string src = GetSourceFilePath();
            if (string.IsNullOrEmpty(src))
            {
                EditorUtility.DisplayDialog("无法覆盖", "原图没有可写回的磁盘文件。", "确定");
                return;
            }
            int w = _gridWidth * scale, h = _gridHeight * scale;
            if (!EditorUtility.DisplayDialog("确认覆盖原图",
                    $"将用当前像素图（{w}×{h}）覆盖写回原始文件：\n{src}\n\n此操作不可撤销，确定吗？", "覆盖", "取消"))
                return;

            byte[] png = BuildRegionPng(0, 0, _gridWidth, _gridHeight, scale);
            File.WriteAllBytes(src, png);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已覆盖", $"已写回：\n{src}", "确定");
            Debug.Log($"[PixelPerfect] 覆盖原图：{src}");
        }

        /// <summary>同原图目录导出：导出到原图所在目录，文件名 = 原图名_输出宽x高.png。</summary>
        private void ExportToSourceDir(int scale)
        {
            if (_pixels == null) return;
            string src = GetSourceFilePath();
            if (string.IsNullOrEmpty(src))
            {
                EditorUtility.DisplayDialog("无法导出", "原图没有可定位的磁盘目录。", "确定");
                return;
            }
            string dir = Path.GetDirectoryName(src);
            string baseName = Path.GetFileNameWithoutExtension(src);
            int w = _gridWidth * scale, h = _gridHeight * scale;
            string path = Path.Combine(dir, $"{baseName}_{w}x{h}.png");

            byte[] png = BuildRegionPng(0, 0, _gridWidth, _gridHeight, scale);
            File.WriteAllBytes(path, png);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已导出", $"已保存：\n{path}", "确定");
            Debug.Log($"[PixelPerfect] 同目录导出：{path}");
        }

        /// <summary>
        /// 拆分导出：按 cols 列 × rows 行把整图切成若干块，逐块导出 PNG。
        /// 弹窗选择基名与目录；文件名 = 基名_r{行}_c{列}.png。
        /// </summary>
        private void ExportSplit(int cols, int rows, int scale)
        {
            if (_pixels == null) return;
            string defaultName = $"pixel_art_{_gridWidth}x{_gridHeight}_split";
            string basePath = EditorUtility.SaveFilePanel("拆分导出（选择保存基名）", Application.dataPath, defaultName, "png");
            if (string.IsNullOrEmpty(basePath)) return;
            ExportSplitCore(cols, rows, scale, Path.GetDirectoryName(basePath), Path.GetFileNameWithoutExtension(basePath));
        }

        /// <summary>拆分导出到原图所在目录：基名取原图名，文件名 = 原图名_r{行}_c{列}.png。</summary>
        private void ExportSplitToSourceDir(int cols, int rows, int scale)
        {
            if (_pixels == null) return;
            string src = GetSourceFilePath();
            if (string.IsNullOrEmpty(src))
            {
                EditorUtility.DisplayDialog("无法导出", "原图没有可定位的磁盘目录。", "确定");
                return;
            }
            ExportSplitCore(cols, rows, scale, Path.GetDirectoryName(src), Path.GetFileNameWithoutExtension(src));
        }

        /// <summary>
        /// 拆分导出核心：把整图按 cols×rows 切块，逐块写到 dir/baseName_r{行}_c{列}.png。
        /// 切块边界用整数比例 i*grid/n 计算（容忍宽/高不能整除：各块尺寸尽量均分）。
        /// </summary>
        private void ExportSplitCore(int cols, int rows, int scale, string dir, string baseName)
        {
            if (_pixels == null || string.IsNullOrEmpty(dir)) return;
            cols = Mathf.Clamp(cols, 1, _gridWidth);
            rows = Mathf.Clamp(rows, 1, _gridHeight);
            int written = 0;
            for (int rj = 0; rj < rows; rj++)
            {
                int y0 = rj * _gridHeight / rows;
                int y1 = (rj + 1) * _gridHeight / rows;
                if (y1 <= y0) continue;
                for (int ci = 0; ci < cols; ci++)
                {
                    int x0 = ci * _gridWidth / cols;
                    int x1 = (ci + 1) * _gridWidth / cols;
                    if (x1 <= x0) continue;

                    byte[] png = BuildRegionPng(x0, y0, x1, y1, scale);
                    string path = Path.Combine(dir, $"{baseName}_r{rj}_c{ci}.png");
                    File.WriteAllBytes(path, png);
                    written++;
                }
            }
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已拆分导出", $"已保存 {written} 张到：\n{dir}", "确定");
            Debug.Log($"[PixelPerfect] 拆分导出 {written} 张到：{dir}");
        }

        /// <summary>
        /// 把像素区域 [col0,row0) ~ [col1,row1)（顶部为 row0）按 scale 放大编码为 PNG 字节。
        /// 每格渲染为 scale×scale 实色块，透明格留空；PNG 自上而下，Texture 自下而上故翻转写入。
        /// </summary>
        private byte[] BuildRegionPng(int col0, int row0, int col1, int row1, int scale)
        {
            int gw = col1 - col0, gh = row1 - row0;
            int w = gw * scale, h = gh * scale;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] outPx = new Color32[w * h];
            for (int j = 0; j < gh; j++)
                for (int i = 0; i < gw; i++)
                {
                    Color32 c = _pixels[(row0 + j) * _gridWidth + (col0 + i)]; // a==0 透明
                    for (int sj = 0; sj < scale; sj++)
                        for (int si = 0; si < scale; si++)
                        {
                            int px = i * scale + si;
                            int py = j * scale + sj;       // 自上而下
                            int texRow = h - 1 - py;       // Texture 自下而上
                            outPx[texRow * w + px] = c;
                        }
                }
            tex.SetPixels32(outPx);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            DestroyImmediate(tex);
            return png;
        }

        /// <summary>取原始源图在磁盘上的绝对路径：优先外部文件，其次工程内资源；无文件返回 null。</summary>
        private string GetSourceFilePath()
        {
            if (!string.IsNullOrEmpty(_sourceExternalPath) && File.Exists(_sourceExternalPath))
                return _sourceExternalPath;
            if (_sourceTexture != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(_sourceTexture);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string abs = GetAbsolutePath(assetPath);
                    if (File.Exists(abs)) return abs;
                }
            }
            return null;
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
