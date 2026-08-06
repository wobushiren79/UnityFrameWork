using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelImageEditor
{
    /// <summary>像素图编辑器 · 步骤④ 辅助功能：帧排版重排 / 多图合并图集。</summary>
    public partial class PixelImageEditorWindow : EditorWindow
    {
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
            Debug.Log($"[PixelImageEditor] 图片合并导出：{path}");
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
            Debug.Log($"[PixelImageEditor] 图片合并同目录导出：{path}");
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
            Debug.Log($"[PixelImageEditor] 帧排版导出：{path}");
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
            Debug.Log($"[PixelImageEditor] 帧排版覆盖原图：{src}");
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
            Debug.Log($"[PixelImageEditor] 帧排版同目录导出：{path}");
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
    }
}
