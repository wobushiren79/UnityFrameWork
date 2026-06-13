using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelArtTool
{
    /// <summary>
    /// 图片转像素图工具。
    /// 菜单：Custom/图转像素图
    ///
    /// 功能：
    /// 1. 选择任意像素的图片，转换为指定高宽的像素图；
    /// 2. 可自定义输出目录与文件名（默认 {原图名}_{宽}_{高}）；
    /// 3. 可指定输出颜色个数，使用「频率加权最远点采样 + K-means 精化」做颜色量化：
    ///    a. 相近颜色归并为同一种颜色（每个像素映射到最近的基础色）；
    ///    b. 每个聚类的代表色取该聚类中原图使用最多的颜色；
    ///    c. 基础色的选取兼顾「彼此区别最大」与「使用最多」。
    /// 4. 生成后可预览、列出基础色并手动调整，按调整后的颜色重新预览，并可保存覆盖导出图。
    /// </summary>
    public class PixelArtConverterWindow : EditorWindow
    {
        // ---------- 输入参数 ----------
        private Texture2D _sourceTexture;
        private int _targetWidth = 32;
        private int _targetHeight = 32;
        private int _colorCount = 8;
        private string _outputDir = "";
        private string _outputName = "";
        private bool _useAreaAverage = true; // 缩放采样方式：true=区域平均，false=最近邻
        // 拖入工程外图片时记录原始文件路径（用于读取像素与默认输出目录）
        private string _sourceExternalPath = "";

        // ---------- 生成结果（中间数据） ----------
        // 缩放后的像素（含 alpha，未量化），用于在调整调色板后重新映射
        private Color32[] _downscaledPixels;
        private int _outW;
        private int _outH;

        // 当前调色板（可被用户手动调整）
        private List<Color> _palette = new List<Color>();
        // 每个基础色在原图缩放后的使用数量（用于展示）
        private List<int> _paletteCounts = new List<int>();
        // 每个像素分配到哪个调色板索引（首次映射时记录，后续调整颜色时直接用索引取色，避免重新最近邻匹配导致颜色替换失效）
        private int[] _pixelPaletteIndices;

        // 预览纹理
        private Texture2D _previewTexture;

        // ---------- UI 状态 ----------
        private Vector2 _scroll;
        private const int kMaxPreviewSize = 360;

        // ---------- 样式缓存 ----------
        private static GUIStyle _titleStyle;
        private static GUIStyle _subTitleStyle;
        private static GUIStyle _cardStyle;
        private static GUIStyle _cardHeaderStyle;
        private static GUIStyle _hintStyle;
        private static GUIStyle _swatchIndexStyle;
        private static Texture2D _bannerTex;

        private static readonly Color kAccent = new Color(0.26f, 0.59f, 0.98f);
        private static readonly Color kGenColor = new Color(0.30f, 0.70f, 0.45f);
        private static readonly Color kSaveColor = new Color(0.95f, 0.62f, 0.25f);

        [MenuItem("Custom/工具弹窗/图转像素图")]
        public static void Open()
        {
            var win = GetWindow<PixelArtConverterWindow>("图转像素图");
            win.minSize = new Vector2(440, 600);
            win.Show();
        }

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
            _cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 12),
                margin = new RectOffset(4, 4, 4, 4)
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
            _swatchIndexStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
        }

        private void OnGUI()
        {
            EnsureStyles();

            DrawBanner();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space(4);

            DrawInputSection();
            DrawGenerateButton();

            if (_previewTexture != null)
            {
                DrawResultSection();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.EndScrollView();
        }

        // ============================================================
        // UI 绘制
        // ============================================================

        private void DrawBanner()
        {
            Rect r = GUILayoutUtility.GetRect(0, 46, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.20f, 0.22f, 0.28f));
            // 底部一条 accent 线
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2, r.width, 2), kAccent);

            Rect labelRect = new Rect(r.x + 14, r.y + 5, r.width - 28, 22);
            GUI.Label(labelRect, "🎨 图片转像素图", _titleStyle);
            Rect subRect = new Rect(r.x + 16, r.y + 25, r.width - 28, 16);
            GUI.Label(subRect, "缩放 · 颜色量化 · 调色板手动微调 · 导出", _subTitleStyle);
        }

        /// <summary>卡片容器：画一个带标题的圆角盒。</summary>
        private void BeginCard(string title)
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField(title, _cardHeaderStyle);
            DrawSeparator();
            EditorGUILayout.Space(2);
        }

        private void EndCard()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private static void DrawSeparator()
        {
            Rect r = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(1, 1, 1, 0.08f));
        }

        private void DrawInputSection()
        {
            BeginCard("① 源图片");

            // 拖拽放置区（支持工程内 Texture 资源 / 外部图片文件）
            DrawSourceDropArea();
            EditorGUILayout.Space(2);

            EditorGUI.BeginChangeCheck();
            _sourceTexture = (Texture2D)EditorGUILayout.ObjectField("图片", _sourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && _sourceTexture != null)
            {
                _sourceExternalPath = ""; // 通过 ObjectField 选择的是工程内资源
                string path = AssetDatabase.GetAssetPath(_sourceTexture);
                if (!string.IsNullOrEmpty(path) && string.IsNullOrEmpty(_outputDir))
                {
                    _outputDir = Path.GetDirectoryName(GetAbsolutePath(path));
                }
            }

            if (_sourceTexture != null)
            {
                string p = AssetDatabase.GetAssetPath(_sourceTexture);
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 缩略图
                    Rect thumb = GUILayoutUtility.GetRect(56, 56, GUILayout.Width(56), GUILayout.Height(56));
                    DrawChecker(thumb);
                    GUI.DrawTexture(thumb, _sourceTexture, ScaleMode.ScaleToFit, true);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("源尺寸", $"{_sourceTexture.width} x {_sourceTexture.height}");
                        EditorGUILayout.LabelField("路径", string.IsNullOrEmpty(p) ? "(非资源对象)" : p, _hintStyle);
                    }
                }
            }
            EndCard();

            BeginCard("② 目标尺寸与采样");
            using (new EditorGUILayout.HorizontalScope())
            {
                _targetWidth = Mathf.Max(1, EditorGUILayout.IntField("像素宽", _targetWidth));
                GUILayout.Space(8);
                _targetHeight = Mathf.Max(1, EditorGUILayout.IntField("像素高", _targetHeight));
            }
            _useAreaAverage = EditorGUILayout.Toggle(new GUIContent("区域平均采样", "勾选用区域平均缩放（更平滑），取消用最近邻（更锐利）"), _useAreaAverage);
            EditorGUILayout.LabelField(_useAreaAverage ? "当前：区域平均（平滑）" : "当前：最近邻（锐利）", _hintStyle);
            EndCard();

            BeginCard("③ 颜色量化");
            _colorCount = Mathf.Clamp(EditorGUILayout.IntSlider("输出颜色个数", _colorCount, 1, 64), 1, 256);
            EditorGUILayout.LabelField("把原图所有颜色优化到指定数量：相近颜色归并、代表色取使用最多色、基础色兼顾区别大与使用多。", _hintStyle);
            EndCard();

            BeginCard("④ 输出设置");
            using (new EditorGUILayout.HorizontalScope())
            {
                _outputDir = EditorGUILayout.TextField(new GUIContent("输出目录", "可拖入文件夹设置路径"), _outputDir);
                // 输出目录支持拖入文件夹
                Rect dirFieldRect = GUILayoutUtility.GetLastRect();
                HandleFolderDrop(dirFieldRect);
                if (GUILayout.Button("浏览", GUILayout.Width(50)))
                {
                    string start = string.IsNullOrEmpty(_outputDir) ? Application.dataPath : _outputDir;
                    string chosen = EditorUtility.OpenFolderPanel("选择输出目录", start, "");
                    if (!string.IsNullOrEmpty(chosen)) _outputDir = chosen;
                }
            }
            _outputName = EditorGUILayout.TextField(new GUIContent("文件名", "留空则默认 {原图名}_{宽}_{高}"), _outputName);
            EditorGUILayout.LabelField($"实际文件名：{GetEffectiveFileName()}.png", _hintStyle);
            EndCard();
        }

        private static readonly string[] kImageExts = { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".gif", ".psd", ".exr" };

        /// <summary>拖拽放置区：支持从 Project 拖入 Texture 资源，或从系统拖入图片文件。</summary>
        private void DrawSourceDropArea()
        {
            Rect drop = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
            Event e = Event.current;
            bool hover = drop.Contains(e.mousePosition);
            bool dragging = (e.type == EventType.DragUpdated || e.type == EventType.DragPerform);
            bool validHover = hover && dragging && IsDragValid();

            // 背景与边框
            Color bg = validHover ? new Color(kAccent.r, kAccent.g, kAccent.b, 0.18f)
                                  : new Color(1, 1, 1, 0.04f);
            EditorGUI.DrawRect(drop, bg);
            DrawRectOutline(drop, validHover ? kAccent : new Color(1, 1, 1, 0.18f));

            var centered = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = validHover ? kAccent : new Color(0.7f, 0.7f, 0.7f) }
            };
            string label = _sourceTexture != null
                ? $"✔ 已选择：{_sourceTexture.name}\n（可再拖入新图替换）"
                : "⬇ 把图片拖到这里\n支持工程内 Texture 资源或外部图片文件";
            GUI.Label(drop, label, centered);

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

        /// <summary>处理拖入文件夹到指定 Rect 上的 Drop 事件，用于设置输出目录。</summary>
        private void HandleFolderDrop(Rect dropRect)
        {
            Event e = Event.current;
            if (!dropRect.Contains(e.mousePosition)) return;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;

            // 检查是否有有效文件夹路径
            string folderPath = GetFirstFolderFromDrag();
            bool valid = !string.IsNullOrEmpty(folderPath);

            if (valid)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    _outputDir = folderPath;
                    GUI.FocusControl(null);
                }
                e.Use();
            }
        }

        /// <summary>从拖入内容中提取第一个文件夹的绝对路径，没有则返回 null。</summary>
        private static string GetFirstFolderFromDrag()
        {
            foreach (string p in DragAndDrop.paths)
            {
                // 文件系统绝对路径
                if (Path.IsPathRooted(p) && Directory.Exists(p))
                    return p;
                // Project 窗口中的文件夹
                if (AssetDatabase.IsValidFolder(p))
                    return GetAbsolutePath(p);
            }
            return null;
        }

        private static bool IsDragValid()
        {
            foreach (var obj in DragAndDrop.objectReferences)
                if (obj is Texture2D) return true;
            foreach (var p in DragAndDrop.paths)
                if (IsImagePath(p)) return true;
            return false;
        }

        private static bool IsImagePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return Array.IndexOf(kImageExts, ext) >= 0;
        }

        private void AcceptDraggedImage()
        {
            // 1. 优先工程内 Texture2D 资源
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Texture2D tex)
                {
                    _sourceTexture = tex;
                    _sourceExternalPath = "";
                    string ap = AssetDatabase.GetAssetPath(tex);
                    if (!string.IsNullOrEmpty(ap)) _outputDir = Path.GetDirectoryName(GetAbsolutePath(ap));
                    GUI.FocusControl(null);
                    return;
                }
            }
            // 2. 外部图片文件
            foreach (var p in DragAndDrop.paths)
            {
                if (!IsImagePath(p)) continue;
                // 若该文件其实在工程内，转成资源引用
                Texture2D asset = LoadAsProjectAsset(p);
                if (asset != null)
                {
                    _sourceTexture = asset;
                    _sourceExternalPath = "";
                    _outputDir = Path.GetDirectoryName(p);
                }
                else
                {
                    LoadExternalImage(p);
                }
                GUI.FocusControl(null);
                return;
            }
        }

        /// <summary>若文件位于本工程 Assets 下，返回其资源引用，否则 null。</summary>
        private static Texture2D LoadAsProjectAsset(string fullPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
            string norm = Path.GetFullPath(fullPath).Replace('\\', '/');
            if (norm.StartsWith(projectRoot + "/Assets/", StringComparison.OrdinalIgnoreCase))
            {
                string rel = norm.Substring(projectRoot.Length + 1);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(rel);
            }
            return null;
        }

        /// <summary>把外部图片文件解码成一张运行时 Texture2D 作为源图。</summary>
        private void LoadExternalImage(string fullPath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes))
                {
                    DestroyImmediate(tex);
                    EditorUtility.DisplayDialog("无法加载", $"无法解码图片：\n{fullPath}", "确定");
                    return;
                }
                tex.name = Path.GetFileNameWithoutExtension(fullPath);
                _sourceTexture = tex;
                _sourceExternalPath = fullPath;
                if (string.IsNullOrEmpty(_outputDir))
                    _outputDir = Path.GetDirectoryName(fullPath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("读取失败", ex.Message, "确定");
                Debug.LogException(ex);
            }
        }

        private void DrawGenerateButton()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(_sourceTexture == null))
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = kGenColor;
                if (GUILayout.Button(_previewTexture == null ? "生成像素图" : "重新生成", GUILayout.Height(34)))
                {
                    try { Generate(); }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("生成失败", e.Message, "确定");
                        Debug.LogException(e);
                    }
                }
                GUI.backgroundColor = prev;
            }
            if (_sourceTexture == null)
                EditorGUILayout.LabelField("请先选择一张源图片。", _hintStyle);
            EditorGUILayout.Space(4);
        }

        private void DrawResultSection()
        {
            BeginCard($"⑤ 预览  ({_outW} x {_outH})");

            float scale = Mathf.Min((float)kMaxPreviewSize / _outW, (float)kMaxPreviewSize / _outH);
            scale = Mathf.Max(1f, Mathf.Floor(scale));
            float w = _outW * scale;
            float h = _outH * scale;

            // 居中显示
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                Rect r = GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(false));
                // 外框
                Rect frame = new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6);
                EditorGUI.DrawRect(frame, new Color(0, 0, 0, 0.35f));
                DrawChecker(r);
                GUI.DrawTexture(r, _previewTexture, ScaleMode.ScaleToFit, true);
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.LabelField($"放大显示 {(int)scale}×（点过滤，保持像素锐利）", _hintStyle);
            EndCard();

            DrawPaletteSection();
            DrawActionButtons();
        }

        private void DrawPaletteSection()
        {
            BeginCard($"⑥ 基础色 · {_palette.Count} 种（可手动调整）");

            // 顶部：色板预览条
            DrawPaletteStrip();
            EditorGUILayout.Space(6);

            // 计算总使用量用于百分比
            long total = 0;
            for (int i = 0; i < _paletteCounts.Count; i++) total += _paletteCounts[i];
            if (total <= 0) total = 1;

            for (int i = 0; i < _palette.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 序号色块
                    Rect sw = GUILayoutUtility.GetRect(22, 22, GUILayout.Width(22), GUILayout.Height(22));
                    EditorGUI.DrawRect(sw, new Color(_palette[i].r, _palette[i].g, _palette[i].b, 1f));
                    Rect border = new Rect(sw.x - 1, sw.y - 1, sw.width + 2, sw.height + 2);
                    DrawRectOutline(border, new Color(0, 0, 0, 0.4f));
                    GUI.Label(new Rect(sw.x, sw.y + 24, 22, 12), $"#{i + 1}", _swatchIndexStyle);

                    GUILayout.Space(4);
                    _palette[i] = EditorGUILayout.ColorField(GUIContent.none, _palette[i], true, true, false, GUILayout.Width(110));

                    int cnt = (i < _paletteCounts.Count) ? _paletteCounts[i] : 0;
                    float pct = cnt / (float)total;
                    GUILayout.Space(6);
                    // 占比条
                    Rect bar = GUILayoutUtility.GetRect(60, 14, GUILayout.ExpandWidth(true), GUILayout.Height(14));
                    EditorGUI.DrawRect(bar, new Color(1, 1, 1, 0.06f));
                    EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * pct, bar.height), new Color(_palette[i].r, _palette[i].g, _palette[i].b, 0.9f));
                    EditorGUILayout.LabelField($"{cnt} px ({pct * 100f:0.0}%)", GUILayout.Width(110));
                }
                EditorGUILayout.Space(2);
            }
            EndCard();
        }

        /// <summary>画一条完整调色板预览长条。</summary>
        private void DrawPaletteStrip()
        {
            if (_palette.Count == 0) return;
            Rect r = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            float seg = r.width / _palette.Count;
            for (int i = 0; i < _palette.Count; i++)
            {
                Rect cell = new Rect(r.x + seg * i, r.y, seg, r.height);
                EditorGUI.DrawRect(cell, new Color(_palette[i].r, _palette[i].g, _palette[i].b, 1f));
            }
            DrawRectOutline(r, new Color(0, 0, 0, 0.4f));
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = kAccent;
                if (GUILayout.Button(new GUIContent("🔄 预览调整后颜色", "按当前调色板重新映射生成预览"), GUILayout.Height(30)))
                {
                    RebuildPreviewFromPalette();
                }
                GUI.backgroundColor = kSaveColor;
                if (GUILayout.Button(new GUIContent("💾 保存预览（覆盖导出）", "将当前预览导出为 PNG"), GUILayout.Height(30)))
                {
                    SavePreview();
                }
                GUI.backgroundColor = prev;
            }
        }

        // ============================================================
        // 核心流程
        // ============================================================

        private void Generate()
        {
            // 1. 读取可读像素（无视导入设置）
            Color32[] srcPixels;
            int srcW, srcH;
            ReadSourcePixels(_sourceTexture, _sourceExternalPath, out srcPixels, out srcW, out srcH);

            _outW = _targetWidth;
            _outH = _targetHeight;

            // 2. 缩放到目标尺寸
            _downscaledPixels = Downscale(srcPixels, srcW, srcH, _outW, _outH, _useAreaAverage);

            // 3. 颜色量化，得到调色板
            _palette = QuantizeColors(_downscaledPixels, _colorCount, out _paletteCounts);

            // 4. 用调色板映射生成预览（重新生成时清除旧的像素索引分配）
            _pixelPaletteIndices = null;
            RebuildPreviewFromPalette();

            Repaint();
        }

        /// <summary>
        /// 用当前 _palette 把缩放后的像素映射成预览图（最近邻匹配，保留原 alpha）。
        /// </summary>
        private void RebuildPreviewFromPalette()
        {
            if (_downscaledPixels == null || _palette.Count == 0) return;

            Color32[] outPixels = new Color32[_downscaledPixels.Length];
            Color[] pal = _palette.ToArray();

            // 首次映射时记录每个像素的调色板索引；后续调整颜色后重建时直接用索引取色
            bool needAssign = _pixelPaletteIndices == null || _pixelPaletteIndices.Length != _downscaledPixels.Length;
            if (needAssign)
                _pixelPaletteIndices = new int[_downscaledPixels.Length];

            for (int i = 0; i < _downscaledPixels.Length; i++)
            {
                Color32 src = _downscaledPixels[i];
                if (src.a == 0)
                {
                    outPixels[i] = new Color32(0, 0, 0, 0);
                    if (needAssign) _pixelPaletteIndices[i] = 0;
                    continue;
                }
                int idx = needAssign ? NearestPaletteIndex(src, pal) : _pixelPaletteIndices[i];
                if (needAssign) _pixelPaletteIndices[i] = idx;
                // 防止索引越界（极端情况下调色板数量可能变化）
                if (idx >= pal.Length) idx = 0;
                Color c = pal[idx];
                outPixels[i] = new Color32(
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
            }

            if (_previewTexture == null || _previewTexture.width != _outW || _previewTexture.height != _outH)
            {
                if (_previewTexture != null) DestroyImmediate(_previewTexture);
                _previewTexture = new Texture2D(_outW, _outH, TextureFormat.RGBA32, false);
                _previewTexture.filterMode = FilterMode.Point;
                _previewTexture.wrapMode = TextureWrapMode.Clamp;
            }
            _previewTexture.SetPixels32(outPixels);
            _previewTexture.Apply();

            Repaint();
        }

        private void SavePreview()
        {
            if (_previewTexture == null)
            {
                EditorUtility.DisplayDialog("提示", "请先生成预览。", "确定");
                return;
            }
            if (string.IsNullOrEmpty(_outputDir))
            {
                EditorUtility.DisplayDialog("提示", "请先设置输出目录。", "确定");
                return;
            }

            try
            {
                if (!Directory.Exists(_outputDir)) Directory.CreateDirectory(_outputDir);

                string fullPath = Path.Combine(_outputDir, GetEffectiveFileName() + ".png");
                byte[] png = _previewTexture.EncodeToPNG();
                File.WriteAllBytes(fullPath, png);

                // 若在工程内，刷新并设为可点击的 Sprite 导入（不强制）
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("已保存", $"已导出：\n{fullPath}", "确定");
                Debug.Log($"[PixelArt] 已保存：{fullPath}");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("保存失败", e.Message, "确定");
                Debug.LogException(e);
            }
        }

        // ============================================================
        // 像素读取 / 缩放
        // ============================================================

        /// <summary>
        /// 通过原始字节解码，得到一份始终可读的像素数据（不受 Read/Write 导入设置影响）。
        /// 若拿不到资源路径（如运行时生成的纹理），回退用 RenderTexture 拷贝。
        /// </summary>
        private static void ReadSourcePixels(Texture2D tex, string externalPath, out Color32[] pixels, out int w, out int h)
        {
            // 优先用文件路径直接解码（外部文件 or 工程内资源），保证可读且无 gamma/Blit 干扰
            string filePath = !string.IsNullOrEmpty(externalPath) && File.Exists(externalPath)
                ? externalPath
                : null;
            if (filePath == null)
            {
                string assetPath = AssetDatabase.GetAssetPath(tex);
                if (!string.IsNullOrEmpty(assetPath) && File.Exists(GetAbsolutePath(assetPath)))
                    filePath = GetAbsolutePath(assetPath);
            }

            if (filePath != null)
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tmp.LoadImage(bytes))
                {
                    w = tmp.width;
                    h = tmp.height;
                    pixels = tmp.GetPixels32();
                    DestroyImmediate(tmp);
                    return;
                }
                DestroyImmediate(tmp);
            }

            // 回退：通过 RenderTexture 把任意纹理拷成可读
            w = tex.width;
            h = tex.height;
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            pixels = readable.GetPixels32();
            DestroyImmediate(readable);
        }

        /// <summary>
        /// 缩放到目标尺寸。区域平均或最近邻。注意：纹理像素以左下角为原点。
        /// </summary>
        private static Color32[] Downscale(Color32[] src, int srcW, int srcH, int dstW, int dstH, bool areaAverage)
        {
            Color32[] dst = new Color32[dstW * dstH];

            for (int y = 0; y < dstH; y++)
            {
                for (int x = 0; x < dstW; x++)
                {
                    Color32 c;
                    if (areaAverage)
                    {
                        // 该目标像素对应的源区域
                        int sx0 = Mathf.FloorToInt((float)x / dstW * srcW);
                        int sx1 = Mathf.Max(sx0 + 1, Mathf.FloorToInt((float)(x + 1) / dstW * srcW));
                        int sy0 = Mathf.FloorToInt((float)y / dstH * srcH);
                        int sy1 = Mathf.Max(sy0 + 1, Mathf.FloorToInt((float)(y + 1) / dstH * srcH));
                        sx1 = Mathf.Min(sx1, srcW);
                        sy1 = Mathf.Min(sy1, srcH);

                        // 用 premultiplied 累加，避免透明边缘颜色被污染
                        double rr = 0, gg = 0, bb = 0, aa = 0;
                        int n = 0;
                        for (int sy = sy0; sy < sy1; sy++)
                        {
                            for (int sx = sx0; sx < sx1; sx++)
                            {
                                Color32 s = src[sy * srcW + sx];
                                double a = s.a / 255.0;
                                rr += s.r * a;
                                gg += s.g * a;
                                bb += s.b * a;
                                aa += s.a;
                                n++;
                            }
                        }
                        if (n == 0) n = 1;
                        double avgA = aa / n; // 0..255
                        double aFactor = avgA > 0 ? (n * (avgA / 255.0)) : 0;
                        if (aFactor <= 0)
                        {
                            c = new Color32(0, 0, 0, 0);
                        }
                        else
                        {
                            c = new Color32(
                                (byte)Mathf.Clamp(Mathf.RoundToInt((float)(rr / aFactor)), 0, 255),
                                (byte)Mathf.Clamp(Mathf.RoundToInt((float)(gg / aFactor)), 0, 255),
                                (byte)Mathf.Clamp(Mathf.RoundToInt((float)(bb / aFactor)), 0, 255),
                                (byte)Mathf.Clamp(Mathf.RoundToInt((float)avgA), 0, 255));
                        }
                    }
                    else
                    {
                        int sx = Mathf.Min(srcW - 1, Mathf.FloorToInt((x + 0.5f) / dstW * srcW));
                        int sy = Mathf.Min(srcH - 1, Mathf.FloorToInt((y + 0.5f) / dstH * srcH));
                        c = src[sy * srcW + sx];
                    }
                    dst[y * dstW + x] = c;
                }
            }
            return dst;
        }

        // ============================================================
        // 颜色量化
        // 算法：
        //  1) 统计颜色直方图（忽略全透明像素）；
        //  2) 频率加权最远点采样选初始调色板：
        //     - 第一个取使用最多的颜色；
        //     - 之后每次取「频率 × 到已选色的最小距离」最大的颜色
        //       （兼顾要求 c：区别大 + 使用多）；
        //  3) K-means 精化若干轮，每个聚类的代表色取该聚类内使用最多的颜色
        //     （满足要求 a 归并 + 要求 b 取最多使用色）。
        // ============================================================

        private static List<Color> QuantizeColors(Color32[] pixels, int colorCount, out List<int> outCounts)
        {
            // 1. 直方图
            Dictionary<int, int> hist = new Dictionary<int, int>();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                if (p.a == 0) continue;
                int key = (p.r << 16) | (p.g << 8) | p.b;
                hist.TryGetValue(key, out int cnt);
                hist[key] = cnt + 1;
            }

            int distinct = hist.Count;
            // 拆成数组方便遍历
            int[] colors = new int[distinct];
            int[] counts = new int[distinct];
            {
                int idx = 0;
                foreach (var kv in hist)
                {
                    colors[idx] = kv.Key;
                    counts[idx] = kv.Value;
                    idx++;
                }
            }

            outCounts = new List<int>();
            List<Color> palette = new List<Color>();

            if (distinct == 0)
            {
                palette.Add(Color.clear);
                outCounts.Add(0);
                return palette;
            }

            int k = Mathf.Min(colorCount, distinct);

            // 2. 频率加权最远点采样
            int[] seedIdx = new int[k];
            // 第一个：使用最多
            int firstIdx = 0;
            for (int i = 1; i < distinct; i++)
                if (counts[i] > counts[firstIdx]) firstIdx = i;
            seedIdx[0] = firstIdx;

            // minDistSq[i] = 颜色 i 到当前所有种子的最小距离平方
            double[] minDistSq = new double[distinct];
            for (int i = 0; i < distinct; i++)
                minDistSq[i] = ColorDistSq(colors[i], colors[firstIdx]);

            for (int s = 1; s < k; s++)
            {
                int best = -1;
                double bestScore = -1;
                for (int i = 0; i < distinct; i++)
                {
                    // 频率 × 距离：兼顾使用多 + 区别大
                    double score = (double)counts[i] * minDistSq[i];
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = i;
                    }
                }
                if (best < 0) best = firstIdx;
                seedIdx[s] = best;
                // 更新最小距离
                for (int i = 0; i < distinct; i++)
                {
                    double d = ColorDistSq(colors[i], colors[best]);
                    if (d < minDistSq[i]) minDistSq[i] = d;
                }
            }

            // 当前调色板（用 int 颜色表示）
            int[] centers = new int[k];
            for (int s = 0; s < k; s++) centers[s] = colors[seedIdx[s]];

            // 3. K-means 精化（代表色取聚类内使用最多色）
            const int kIterations = 6;
            int[] assign = new int[distinct];
            for (int iter = 0; iter < kIterations; iter++)
            {
                // 分配
                bool changed = false;
                for (int i = 0; i < distinct; i++)
                {
                    int nearest = 0;
                    double nd = double.MaxValue;
                    for (int s = 0; s < k; s++)
                    {
                        double d = ColorDistSq(colors[i], centers[s]);
                        if (d < nd) { nd = d; nearest = s; }
                    }
                    if (assign[i] != nearest) { assign[i] = nearest; changed = true; }
                }

                // 更新代表色 = 聚类内使用最多的颜色
                int[] newCenters = new int[k];
                int[] newCenterCount = new int[k];
                bool[] hasMember = new bool[k];
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
                for (int s = 0; s < k; s++)
                {
                    if (hasMember[s]) centers[s] = newCenters[s];
                }

                if (!changed && iter > 0) break;
            }

            // 统计每个中心的总使用量（用于展示与排序）
            int[] totalCounts = new int[k];
            for (int i = 0; i < distinct; i++)
                totalCounts[assign[i]] += counts[i];

            // 按使用量降序输出
            List<int> order = new List<int>();
            for (int s = 0; s < k; s++) order.Add(s);
            order.Sort((a, b) => totalCounts[b].CompareTo(totalCounts[a]));

            foreach (int s in order)
            {
                int c = centers[s];
                palette.Add(IntToColor(c));
                outCounts.Add(totalCounts[s]);
            }

            return palette;
        }

        // ============================================================
        // 工具方法
        // ============================================================

        private static int NearestPaletteIndex(Color32 c, Color[] pal)
        {
            int best = 0;
            double bd = double.MaxValue;
            for (int i = 0; i < pal.Length; i++)
            {
                double dr = c.r - pal[i].r * 255.0;
                double dg = c.g - pal[i].g * 255.0;
                double db = c.b - pal[i].b * 255.0;
                double d = dr * dr + dg * dg + db * db;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        private static double ColorDistSq(int a, int b)
        {
            int ar = (a >> 16) & 0xFF, ag = (a >> 8) & 0xFF, ab = a & 0xFF;
            int br = (b >> 16) & 0xFF, bg = (b >> 8) & 0xFF, bb = b & 0xFF;
            double dr = ar - br, dg = ag - bg, db = ab - bb;
            return dr * dr + dg * dg + db * db;
        }

        private static Color IntToColor(int c)
        {
            int r = (c >> 16) & 0xFF, g = (c >> 8) & 0xFF, b = c & 0xFF;
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        private string GetEffectiveFileName()
        {
            if (!string.IsNullOrEmpty(_outputName)) return _outputName;
            string baseName = _sourceTexture != null ? _sourceTexture.name : "image";
            return $"{baseName}_{_targetWidth}_{_targetHeight}";
        }

        private static string GetAbsolutePath(string assetPath)
        {
            // assetPath 形如 "Assets/xxx/a.png"
            if (string.IsNullOrEmpty(assetPath)) return assetPath;
            if (Path.IsPathRooted(assetPath)) return assetPath;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath);
        }

        /// <summary>画一个矩形描边（1px）。</summary>
        private static void DrawRectOutline(Rect r, Color color)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), color);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1, r.width, 1), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1, r.height), color);
            EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y, 1, r.height), color);
        }

        private static Texture2D _checkerTex;

        private static void DrawChecker(Rect r)
        {
            if (_checkerTex == null)
            {
                _checkerTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                _checkerTex.filterMode = FilterMode.Point;
                _checkerTex.wrapMode = TextureWrapMode.Repeat;
                Color a = new Color(0.75f, 0.75f, 0.75f, 1f);
                Color b = new Color(0.55f, 0.55f, 0.55f, 1f);
                _checkerTex.SetPixels(new[] { a, b, b, a });
                _checkerTex.Apply();
            }
            float tile = 8f;
            GUI.DrawTextureWithTexCoords(r, _checkerTex, new Rect(0, 0, r.width / tile, r.height / tile));
        }

        private void OnDestroy()
        {
            if (_previewTexture != null) DestroyImmediate(_previewTexture);
        }
    }
}
