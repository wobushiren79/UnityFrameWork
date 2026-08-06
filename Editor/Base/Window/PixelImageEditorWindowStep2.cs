using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelImageEditor
{
    /// <summary>像素图编辑器 · 步骤② 定位与转换：拖拽定位、取色算法、智能网格检测、转换核心。</summary>
    public partial class PixelImageEditorWindow : EditorWindow
    {
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
                "最常用颜色", "最常用颜色（偏亮）", "最常用颜色（偏暗）", "平均颜色", "邻域颜色", "最近邻颜色"
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
                EditorGUILayout.LabelField("（当前算法为平均/邻域/最近邻，不使用相似度阈值）", _hintStyle);

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
                case ConvMethod.NearestNeighbor: return "取格中心映射到源图后最近的一个像素原色，不做混合；边缘最锐利，但细线/小细节可能丢失。";
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
                    if (_method == ConvMethod.NearestNeighbor)
                        hasColor = SampleNearestNeighbor((cx0 + cx1) * 0.5f, (cy0 + cy1) * 0.5f, out result);
                    else if (_method == ConvMethod.Average || _method == ConvMethod.Neighbor)
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

        /// <summary>最近邻：把画布坐标映射到源图，取距离最近的一个源像素原色（不混合，保留原 alpha）；越界或该像素全透明时返回 false。</summary>
        private bool SampleNearestNeighbor(float canvasX, float canvasY, out Color32 result)
        {
            int sx = Mathf.FloorToInt((canvasX - _offsetX) / _imageScale + 0.5f);
            int sy = Mathf.FloorToInt((canvasY - _offsetY) / _imageScale + 0.5f);
            if (sx >= 0 && sx < _srcW && sy >= 0 && sy < _srcH)
            {
                Color32 c = _srcTopDown[sy * _srcW + sx];
                if (c.a != 0) { result = c; return true; }
            }
            result = kTransparent;
            return false;
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

        /// <summary>按当前取色算法对一个源图矩形采样（复用 Convert 的采样器）；邻域算法四周外扩 25%，最近邻取矩形中心像素。</summary>
        private bool SampleSourceRect(int x0, int y0, int x1, int y1, out Color32 result)
        {
            if (_method == ConvMethod.Neighbor)
            {
                int mx = Mathf.RoundToInt((x1 - x0) * 0.25f), my = Mathf.RoundToInt((y1 - y0) * 0.25f);
                x0 = Mathf.Max(0, x0 - mx); y0 = Mathf.Max(0, y0 - my);
                x1 = Mathf.Min(_srcW, x1 + mx); y1 = Mathf.Min(_srcH, y1 + my);
            }
            if (_method == ConvMethod.NearestNeighbor)
            {
                // 矩形已在源图空间，直接取中心处最近的像素原色
                int cx = Mathf.Clamp((x0 + x1) / 2, 0, _srcW - 1);
                int cy = Mathf.Clamp((y0 + y1) / 2, 0, _srcH - 1);
                Color32 c = _srcTopDown[cy * _srcW + cx];
                result = c.a != 0 ? c : kTransparent;
                return c.a != 0;
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
    }
}
