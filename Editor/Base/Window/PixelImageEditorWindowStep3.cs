using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelImageEditor
{
    /// <summary>像素图编辑器 · 步骤③ 编辑与导出：画笔/橡皮/魔棒、调色板换色、撤销重做、PNG 导出。</summary>
    public partial class PixelImageEditorWindow : EditorWindow
    {
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
            Debug.Log($"[PixelImageEditor] 导出：{path}");
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
            Debug.Log($"[PixelImageEditor] 覆盖原图：{src}");
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
            Debug.Log($"[PixelImageEditor] 同目录导出：{path}");
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
            Debug.Log($"[PixelImageEditor] 拆分导出 {written} 张到：{dir}");
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
    }
}
