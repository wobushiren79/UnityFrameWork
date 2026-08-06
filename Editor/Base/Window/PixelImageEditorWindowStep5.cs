using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelImageEditor
{
    /// <summary>
    /// 像素图编辑器 · 步骤⑤ 像素图颜色调节：
    /// 并排载入两张互相独立的图（可拖拽设置），各自提取调色板（该图出现的所有颜色）并可编辑，
    /// 编辑某个颜色即把该图中所有该颜色的像素全局替换（操作参考步骤③调色板）；
    /// 每个颜色旁带「吸」吸管取色按钮：点它后直接在任一图预览上单击取色应用到该色，无需打开颜色选择器；
    /// 每张图各有「覆盖原图导出」与「同原图目录导出」两个导出按钮（参考步骤③）。
    /// 与步骤④一样为独立辅助工具，不依赖步骤①~③的数据。
    /// </summary>
    public partial class PixelImageEditorWindow : EditorWindow
    {
        #region 字段 - 步骤⑤ 颜色调节

        /// <summary>步骤⑤单张待调色图的全部状态（两张图各持一份，互不影响）。</summary>
        private class ColorAdjustImage
        {
            /// <summary>选中的纹理：工程内资源，或外部图片解码出的临时纹理（externalPath 非空时表示临时纹理，需自行释放）。</summary>
            public Texture2D sourceTexture;
            /// <summary>来自工程外时的外部文件绝对路径（空串=工程内资源）。</summary>
            public string externalPath = "";
            /// <summary>图像素（自上而下，row0 在顶部），既是预览也是导出的真实数据源。</summary>
            public Color32[] topDown;
            /// <summary>图宽（像素）。</summary>
            public int w;
            /// <summary>图高（像素）。</summary>
            public int h;
            /// <summary>预览纹理（Point 过滤）。</summary>
            public Texture2D displayTex;
            /// <summary>调色板：图中出现的不同颜色（按 RGBA 区分、忽略透明），按使用量降序。</summary>
            public readonly List<Color> palette = new List<Color>();
            /// <summary>每种调色板颜色在图中的使用数量（与 palette 一一对应）。</summary>
            public readonly List<int> paletteCounts = new List<int>();
            /// <summary>每个像素映射到的调色板索引（-1=透明），按索引整体替换颜色避免碰撞。</summary>
            public int[] pixelPaletteIndices;
            /// <summary>预览缩放（每像素显示边长，1~16）。</summary>
            public int previewZoom = 4;
            /// <summary>预览滚动位置。</summary>
            public Vector2 previewScroll;
        }

        /// <summary>步骤⑤左侧图 A。</summary>
        private readonly ColorAdjustImage _caImageA = new ColorAdjustImage();
        /// <summary>步骤⑤右侧图 B。</summary>
        private readonly ColorAdjustImage _caImageB = new ColorAdjustImage();

        /// <summary>吸管取色：当前取色目标图（null=未处于取色模式）。</summary>
        private ColorAdjustImage _caPickImage;
        /// <summary>吸管取色：当前取色目标的调色板索引（配合 _caPickImage）。</summary>
        private int _caPickIndex = -1;

        #endregion

        #region UI - 步骤⑤ 颜色调节

        /// <summary>步骤⑤入口：说明卡 + 左右两列各自独立的调色图列。</summary>
        private void DrawStep5()
        {
            BeginCard("像素图颜色调节");
            EditorGUILayout.LabelField(
                "并排载入两张互相独立的图（可拖拽设置），各自列出图中出现的所有颜色并可编辑。\n" +
                "编辑某个颜色即把该图中所有该颜色的像素全局替换（同步骤③调色板）；\n" +
                "每张图各有「覆盖原图导出」与「同原图目录导出」两个导出按钮。\n" +
                "颜色旁「吸」= 吸管取色：点它后直接在任一图预览上单击取色应用到该色，无需打开颜色选择器。",
                _hintStyle);
            EndCard();

            // 吸管取色模式提示条 + Esc 取消
            if (_caPickImage != null)
            {
                string who = _caPickImage == _caImageA ? "图 A" : "图 B";
                EditorGUILayout.HelpBox($"吸管取色中：在任一图预览上单击取色 → 应用到 {who} 第 {_caPickIndex + 1} 个颜色。按 Esc 或再次点「吸」取消。", MessageType.Info);
                Event ev = Event.current;
                if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape) { CancelColorAdjustPick(); ev.Use(); Repaint(); }
            }

            float colW = Mathf.Max(240f, (EditorGUIUtility.currentViewWidth - 34f) / 2f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(colW)))
                    DrawColorAdjustColumn(_caImageA, "图 A", colW);
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(colW)))
                    DrawColorAdjustColumn(_caImageB, "图 B", colW);
            }
        }

        /// <summary>绘制单列调色图：拖放/选择、预览、调色板、导出。</summary>
        private void DrawColorAdjustColumn(ColorAdjustImage img, string title, float colW)
        {
            BeginCard($"{title} · 源图片（支持拖拽替换）");
            DrawColorAdjustDropArea(img);
            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            Texture2D picked = (Texture2D)EditorGUILayout.ObjectField("图片", img.sourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
            {
                FreeColorAdjustTempTexture(img);
                img.sourceTexture = picked;
                img.externalPath = "";
                LoadColorAdjustImage(img);
            }

            if (img.displayTex != null)
            {
                EditorGUILayout.LabelField($"尺寸 {img.w} × {img.h} 像素 · 颜色 {img.palette.Count} 种", _hintStyle);
                img.previewZoom = EditorGUILayout.IntSlider(new GUIContent("预览缩放", "每像素显示边长(1~16)"), img.previewZoom, 1, 16);
                float dw = img.w * img.previewZoom;
                float dh = img.h * img.previewZoom;
                float viewH = Mathf.Min(dh, 260f);
                img.previewScroll = EditorGUILayout.BeginScrollView(img.previewScroll, GUILayout.Height(viewH + 4));
                Rect area = GUILayoutUtility.GetRect(dw, dh, GUILayout.Width(dw), GUILayout.Height(dh));
                DrawChecker(area);
                GUI.DrawTexture(area, img.displayTex, ScaleMode.StretchToFill, true);
                DrawRectOutline(area, new Color(0, 0, 0, 0.5f));
                // 吸管取色模式：预览为取色源，加光标与高亮并处理单击采样
                if (_caPickImage != null)
                {
                    EditorGUIUtility.AddCursorRect(area, MouseCursor.Link);
                    DrawRectOutline(area, kAccent);
                    HandleColorAdjustPickSample(img, area);
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("（拖入或选择一张图片后显示预览与颜色）", _hintStyle);
            }
            EndCard();

            if (img.topDown != null)
            {
                DrawColorAdjustPalette(img, colW);
                DrawColorAdjustExport(img);
            }
        }

        /// <summary>某列的拖放放置区：支持工程内 Texture 或外部图片文件，拖入即载入该列。</summary>
        private void DrawColorAdjustDropArea(ColorAdjustImage img)
        {
            Rect drop = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            Event e = Event.current;
            bool hover = drop.Contains(e.mousePosition);
            bool dragging = e.type == EventType.DragUpdated || e.type == EventType.DragPerform;
            bool valid = hover && dragging && IsDragValid();

            EditorGUI.DrawRect(drop, valid ? new Color(kAccent.r, kAccent.g, kAccent.b, 0.18f) : new Color(1, 1, 1, 0.04f));
            DrawRectOutline(drop, valid ? kAccent : new Color(1, 1, 1, 0.18f));
            var centered = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } };
            GUI.Label(drop, img.sourceTexture != null ? $"✔ 已选择：{img.sourceTexture.name}（可再拖入替换）" : "⬇ 把图片拖到这里（工程内 Texture 或外部图片）", centered);

            if (hover && dragging)
            {
                DragAndDrop.visualMode = IsDragValid() ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                if (e.type == EventType.DragPerform && IsDragValid())
                {
                    DragAndDrop.AcceptDrag();
                    AcceptColorAdjustDraggedImage(img);
                }
                e.Use();
            }
        }

        /// <summary>接收拖入某列的图片：优先工程内 Texture，其次外部图片文件，随后载入。</summary>
        private void AcceptColorAdjustDraggedImage(ColorAdjustImage img)
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Texture2D tex)
                {
                    FreeColorAdjustTempTexture(img);
                    img.sourceTexture = tex; img.externalPath = "";
                    GUI.FocusControl(null);
                    LoadColorAdjustImage(img);
                    return;
                }
            }
            foreach (var p in DragAndDrop.paths)
            {
                if (!IsImagePath(p)) continue;
                Texture2D asset = LoadAsProjectAsset(p);
                FreeColorAdjustTempTexture(img);
                if (asset != null) { img.sourceTexture = asset; img.externalPath = ""; }
                else
                {
                    Texture2D ext = DecodeExternalImage(p);
                    if (ext == null) return;
                    img.sourceTexture = ext; img.externalPath = p;
                }
                GUI.FocusControl(null);
                LoadColorAdjustImage(img);
                return;
            }
        }

        /// <summary>绘制某列的调色板网格（编辑色块即全局替换该图的对应颜色像素）。</summary>
        private void DrawColorAdjustPalette(ColorAdjustImage img, float colW)
        {
            BeginCard($"颜色 · {img.palette.Count} 种（编辑色块即全局替换该图的像素）");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("重新提取颜色", GUILayout.Width(110)))
                    ExtractColorAdjustPalette(img);
                EditorGUILayout.LabelField("吸=吸管取色，A0=设为透明，A1=设为不透明。", _hintStyle);
            }

            if (img.palette.Count == 0) { EndCard(); return; }

            long total = 0;
            for (int i = 0; i < img.paletteCounts.Count; i++) total += img.paletteCounts[i];
            if (total <= 0) total = 1;

            int perRow = Mathf.Max(2, Mathf.FloorToInt((colW - 16f) / 66f));
            float cellW = Mathf.Max(52f, (colW - 22f) / perRow - 4f);
            for (int row = 0; row * perRow < img.palette.Count; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int c = 0; c < perRow; c++)
                    {
                        int i = row * perRow + c;
                        if (i >= img.palette.Count) break;
                        DrawColorAdjustPaletteCell(img, i, cellW, total);
                    }
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.Space(4);
            }
            EndCard();
        }

        /// <summary>绘制某列单个调色板色块：颜色选择(替换)、吸管取色、透明快捷、占比。</summary>
        private void DrawColorAdjustPaletteCell(ColorAdjustImage img, int i, float cellW, long total)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(cellW)))
            {
                EditorGUI.BeginChangeCheck();
                Color nc = EditorGUILayout.ColorField(GUIContent.none, img.palette[i], false, true, false, GUILayout.Width(cellW));
                if (EditorGUI.EndChangeCheck())
                {
                    img.palette[i] = nc;
                    ReplaceColorAdjustByIndex(img, i);
                }

                // 吸管取色：点它进入取色模式，再在任一图预览上单击取色应用到本色（无需打开颜色选择器）
                bool isPickTarget = _caPickImage == img && _caPickIndex == i;
                Color prevBg = GUI.backgroundColor;
                if (isPickTarget) GUI.backgroundColor = kAccent;
                if (GUILayout.Button(new GUIContent(isPickTarget ? "吸取中…" : "吸", "吸管取色：点此后在任一图预览上单击取色应用到本色，无需打开颜色选择器"), EditorStyles.miniButton, GUILayout.Width(cellW)))
                {
                    if (isPickTarget) CancelColorAdjustPick();
                    else { _caPickImage = img; _caPickIndex = i; }
                    GUI.FocusControl(null);
                    Repaint();
                }
                GUI.backgroundColor = prevBg;

                using (new EditorGUILayout.HorizontalScope())
                {
                    float bw = (cellW - 2f) / 2f;
                    if (GUILayout.Button(new GUIContent("A0", "把该颜色像素设为透明"), EditorStyles.miniButton, GUILayout.Width(bw)))
                    {
                        Color cc = img.palette[i]; cc.a = 0f; img.palette[i] = cc; ReplaceColorAdjustByIndex(img, i);
                    }
                    if (GUILayout.Button(new GUIContent("A1", "把该颜色设为不透明"), EditorStyles.miniButton, GUILayout.Width(bw)))
                    {
                        Color cc = img.palette[i]; cc.a = 1f; img.palette[i] = cc; ReplaceColorAdjustByIndex(img, i);
                    }
                }

                int cnt = i < img.paletteCounts.Count ? img.paletteCounts[i] : 0;
                float pct = cnt / (float)total;
                EditorGUILayout.LabelField(new GUIContent($"{pct * 100f:0.0}%", $"{cnt} px"), EditorStyles.miniLabel, GUILayout.Width(cellW));
            }
        }

        /// <summary>绘制某列的两个导出按钮：覆盖原图导出 / 同原图目录导出（需原图有磁盘文件）。</summary>
        private void DrawColorAdjustExport(ColorAdjustImage img)
        {
            BeginCard("导出");
            bool hasFile = GetSlotFilePath(img.sourceTexture, img.externalPath) != null;
            using (new EditorGUI.DisabledScope(!hasFile))
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = kGenColor;
                if (GUILayout.Button(new GUIContent("覆盖原图导出", "用调色后的图覆盖写回原始图片文件（不可撤销）"), GUILayout.Height(28)))
                    ExportColorAdjustOverwrite(img);
                GUI.backgroundColor = prev;
                if (GUILayout.Button(new GUIContent("同原图目录导出", "导出到原图所在目录，文件名 = 原图名_recolor.png")))
                    ExportColorAdjustToSourceDir(img);
            }
            if (!hasFile)
                EditorGUILayout.LabelField("（原图无磁盘文件，覆盖/同目录导出不可用）", _hintStyle);
            EndCard();
        }

        /// <summary>
        /// 吸管取色模式下：在某图预览区(area 对应 srcImg)单击时，采样鼠标处像素颜色，
        /// 直接应用到当前取色目标槽位(_caPickImage/_caPickIndex)并退出取色模式，无需打开颜色选择器。
        /// </summary>
        private void HandleColorAdjustPickSample(ColorAdjustImage srcImg, Rect area)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0) return;
            if (!area.Contains(e.mousePosition)) return;
            if (_caPickImage == null || srcImg.topDown == null) return;

            // 预览为 displayTex 拉伸铺满 area，area 顶部对应图 row0，屏幕坐标可直接换算像素
            int px = Mathf.Clamp(Mathf.FloorToInt((e.mousePosition.x - area.x) / area.width * srcImg.w), 0, srcImg.w - 1);
            int py = Mathf.Clamp(Mathf.FloorToInt((e.mousePosition.y - area.y) / area.height * srcImg.h), 0, srcImg.h - 1);
            Color32 sampled = srcImg.topDown[py * srcImg.w + px];

            if (_caPickIndex >= 0 && _caPickIndex < _caPickImage.palette.Count)
            {
                _caPickImage.palette[_caPickIndex] = sampled;
                ReplaceColorAdjustByIndex(_caPickImage, _caPickIndex);
            }
            CancelColorAdjustPick();
            e.Use();
            Repaint();
        }

        /// <summary>退出吸管取色模式。</summary>
        private void CancelColorAdjustPick()
        {
            _caPickImage = null;
            _caPickIndex = -1;
        }

        #endregion

        #region 步骤⑤ 载入 / 调色板 / 导出

        /// <summary>读取某列源图像素（转自上而下）、建预览纹理并提取调色板；无源图时清空。</summary>
        private void LoadColorAdjustImage(ColorAdjustImage img)
        {
            // 重新载入会重建调色板，取消对该图的吸管取色，避免索引指向旧调色板
            if (_caPickImage == img) CancelColorAdjustPick();
            if (img.sourceTexture == null) { ClearColorAdjustData(img); return; }

            Color32[] raw;
            int rw, rh;
            ReadSourcePixels(img.sourceTexture, img.externalPath, out raw, out rw, out rh);

            // raw 来自 GetPixels32：自下而上 → 转为自上而下
            var topDown = new Color32[rw * rh];
            for (int y = 0; y < rh; y++)
                for (int x = 0; x < rw; x++)
                    topDown[y * rw + x] = raw[(rh - 1 - y) * rw + x];
            img.topDown = topDown;
            img.w = rw;
            img.h = rh;

            // 显示纹理：raw 本就是自下而上，可直接写入
            if (img.displayTex != null) DestroyImmediate(img.displayTex);
            img.displayTex = new Texture2D(rw, rh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            img.displayTex.SetPixels32(raw);
            img.displayTex.Apply();

            ExtractColorAdjustPalette(img);
        }

        /// <summary>从某列 topDown 提取所有不同颜色（按 RGBA、忽略透明），按使用量降序填充调色板并记录每像素索引。</summary>
        private void ExtractColorAdjustPalette(ColorAdjustImage img)
        {
            img.palette.Clear();
            img.paletteCounts.Clear();
            if (img.topDown == null) { img.pixelPaletteIndices = null; return; }

            var map = new Dictionary<uint, int>();
            int[] idxArr = new int[img.topDown.Length];
            var colors = new List<Color32>();
            var counts = new List<int>();
            for (int k = 0; k < img.topDown.Length; k++)
            {
                Color32 p = img.topDown[k];
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
                img.palette.Add(colors[order[newI]]);
                img.paletteCounts.Add(counts[order[newI]]);
            }
            img.pixelPaletteIndices = idxArr;
        }

        /// <summary>把某列映射到调色板索引 i 的所有像素替换为 palette[i]（alpha≈0 视为透明），并重建预览。</summary>
        private void ReplaceColorAdjustByIndex(ColorAdjustImage img, int i)
        {
            if (img.pixelPaletteIndices == null || img.topDown == null) return;
            Color c = img.palette[i];
            bool transparent = c.a <= 0.0039f; // ≈ 1/255
            Color32 c32 = new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
            for (int k = 0; k < img.topDown.Length; k++)
                if (img.pixelPaletteIndices[k] == i)
                    img.topDown[k] = transparent ? kTransparent : c32;
            BuildColorAdjustDisplayTex(img);
            Repaint();
        }

        /// <summary>由某列 topDown 重建预览纹理（自上而下数组翻成 Texture2D 的自下而上）。</summary>
        private void BuildColorAdjustDisplayTex(ColorAdjustImage img)
        {
            if (img.topDown == null) return;
            if (img.displayTex == null || img.displayTex.width != img.w || img.displayTex.height != img.h)
            {
                if (img.displayTex != null) DestroyImmediate(img.displayTex);
                img.displayTex = new Texture2D(img.w, img.h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            }
            var flip = new Color32[img.w * img.h];
            for (int y = 0; y < img.h; y++)
                for (int x = 0; x < img.w; x++)
                    flip[(img.h - 1 - y) * img.w + x] = img.topDown[y * img.w + x];
            img.displayTex.SetPixels32(flip);
            img.displayTex.Apply();
        }

        /// <summary>覆盖原图导出：用某列调色后的图写回原始图片文件（destructive，需确认）。</summary>
        private void ExportColorAdjustOverwrite(ColorAdjustImage img)
        {
            if (img.topDown == null) return;
            string src = GetSlotFilePath(img.sourceTexture, img.externalPath);
            if (string.IsNullOrEmpty(src))
            {
                EditorUtility.DisplayDialog("无法覆盖", "原图没有可写回的磁盘文件。", "确定");
                return;
            }
            if (!EditorUtility.DisplayDialog("确认覆盖原图",
                    $"将用调色后的图（{img.w}×{img.h}）覆盖写回原始文件：\n{src}\n\n此操作不可撤销，确定吗？", "覆盖", "取消"))
                return;
            File.WriteAllBytes(src, BuildPngFromTopDown(img.topDown, img.w, img.h));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已覆盖", $"已写回：\n{src}", "确定");
            Debug.Log($"[PixelImageEditor] 颜色调节覆盖原图：{src}");
        }

        /// <summary>同原图目录导出：导出到原图所在目录，文件名 = 原图名_recolor.png。</summary>
        private void ExportColorAdjustToSourceDir(ColorAdjustImage img)
        {
            if (img.topDown == null) return;
            string src = GetSlotFilePath(img.sourceTexture, img.externalPath);
            if (string.IsNullOrEmpty(src))
            {
                EditorUtility.DisplayDialog("无法导出", "原图没有可定位的磁盘目录。", "确定");
                return;
            }
            string dir = Path.GetDirectoryName(src);
            string baseName = Path.GetFileNameWithoutExtension(src);
            string path = Path.Combine(dir, $"{baseName}_recolor.png");
            File.WriteAllBytes(path, BuildPngFromTopDown(img.topDown, img.w, img.h));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("已导出", $"已保存：\n{path}", "确定");
            Debug.Log($"[PixelImageEditor] 颜色调节同目录导出：{path}");
        }

        /// <summary>清空某列的像素/调色板/预览数据（保留 sourceTexture 引用交由外部处理）。</summary>
        private void ClearColorAdjustData(ColorAdjustImage img)
        {
            img.topDown = null;
            img.w = img.h = 0;
            img.palette.Clear();
            img.paletteCounts.Clear();
            img.pixelPaletteIndices = null;
            if (img.displayTex != null) { DestroyImmediate(img.displayTex); img.displayTex = null; }
        }

        /// <summary>若某列当前源图是外部图片解码出的临时纹理（externalPath 非空），释放它，避免泄漏。</summary>
        private void FreeColorAdjustTempTexture(ColorAdjustImage img)
        {
            if (!string.IsNullOrEmpty(img.externalPath) && img.sourceTexture != null)
                DestroyImmediate(img.sourceTexture);
        }

        /// <summary>窗口关闭时释放某列的临时源纹理与预览纹理（供 OnDestroy 调用）。</summary>
        private void DisposeColorAdjustImage(ColorAdjustImage img)
        {
            if (img == null) return;
            FreeColorAdjustTempTexture(img);
            if (img.displayTex != null) { DestroyImmediate(img.displayTex); img.displayTex = null; }
        }

        #endregion
    }
}
