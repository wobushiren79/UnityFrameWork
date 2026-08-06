using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PixelDa
{
    /// <summary>
    /// PixelDa 图像处理工具：纯 C# 实现去背景、精灵表横向合成、纹理读写。
    /// 原 Python 后端用 rembg(u2net) 做通用去背景，这里针对工具生成的「纯色背景像素图」
    /// 改用轻量级纯色背景剔除（采样四角颜色 + 阈值 + 边缘洪水填充），无需 ONNX 模型。
    /// </summary>
    public static class PixelDaImageUtil
    {
        #region 纹理读写

        /// <summary>
        /// 从字节加载为可读写纹理
        /// </summary>
        public static Texture2D LoadTexture(byte[] data)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(data);
            return tex;
        }

        /// <summary>
        /// 从本地文件加载纹理
        /// </summary>
        public static Texture2D LoadTextureFromFile(string path)
        {
            if (!File.Exists(path)) return null;
            return LoadTexture(File.ReadAllBytes(path));
        }

        /// <summary>
        /// 保存纹理为 PNG
        /// </summary>
        public static void SavePng(Texture2D tex, string path)
        {
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
        }

        #endregion

        #region Base64 Data URI（供本地图片喂给 AI 接口）

        /// <summary>
        /// 把本地图片文件编码为 data URI（豆包/通义的图编辑、图生视频均可接收 base64 图片，无需上传图床）
        /// </summary>
        public static string FileToDataUri(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            string ext = Path.GetExtension(path).ToLower();
            string mime;
            switch (ext)
            {
                case ".jpg":
                case ".jpeg": mime = "image/jpeg"; break;
                case ".webp": mime = "image/webp"; break;
                default: mime = "image/png"; break;
            }
            string b64 = Convert.ToBase64String(File.ReadAllBytes(path));
            return $"data:{mime};base64,{b64}";
        }

        /// <summary>
        /// 把纹理编码为 PNG data URI（无源文件路径时的兜底；要求纹理可读）
        /// </summary>
        public static string TextureToDataUri(Texture2D tex)
        {
            if (tex == null) return null;
            try
            {
                byte[] png = tex.EncodeToPNG();
                if (png == null || png.Length == 0) return null;
                return "data:image/png;base64," + Convert.ToBase64String(png);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region 纯色去背景

        /// <summary>
        /// 剔除纯色背景：采样四角主色，颜色距离在阈值内的像素从图像边缘做洪水填充置透明。
        /// 只清边缘连通区，避免误删主体内部同色像素。
        /// </summary>
        /// <param name="src">源纹理</param>
        /// <param name="threshold">颜色距离阈值（0~1，越大越激进），默认 0.12</param>
        public static Texture2D RemoveSolidBackground(Texture2D src, float threshold = 0.12f)
        {
            int w = src.width;
            int h = src.height;
            Color32[] pixels = src.GetPixels32();

            // 采样四角，取出现最多的颜色作为背景色
            Color32[] corners =
            {
                pixels[0],
                pixels[w - 1],
                pixels[(h - 1) * w],
                pixels[(h - 1) * w + w - 1],
            };
            Color32 bg = MostCommonColor(corners);

            int thr = Mathf.RoundToInt(threshold * 255f);
            bool[] visited = new bool[w * h];
            Queue<int> queue = new Queue<int>();

            // 从所有边缘像素入队
            for (int x = 0; x < w; x++)
            {
                EnqueueIfBackground(pixels, x, visited, queue, bg, thr);
                EnqueueIfBackground(pixels, (h - 1) * w + x, visited, queue, bg, thr);
            }
            for (int y = 0; y < h; y++)
            {
                EnqueueIfBackground(pixels, y * w, visited, queue, bg, thr);
                EnqueueIfBackground(pixels, y * w + w - 1, visited, queue, bg, thr);
            }

            // 洪水填充：把与背景色相近且与边缘连通的像素置透明
            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                int x = idx % w;
                int y = idx / w;
                pixels[idx] = new Color32(pixels[idx].r, pixels[idx].g, pixels[idx].b, 0);

                if (x > 0) EnqueueIfBackground(pixels, idx - 1, visited, queue, bg, thr);
                if (x < w - 1) EnqueueIfBackground(pixels, idx + 1, visited, queue, bg, thr);
                if (y > 0) EnqueueIfBackground(pixels, idx - w, visited, queue, bg, thr);
                if (y < h - 1) EnqueueIfBackground(pixels, idx + w, visited, queue, bg, thr);
            }

            Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
            result.SetPixels32(pixels);
            result.Apply();
            return result;
        }

        /// <summary>
        /// 若像素接近背景色且未访问则入队并标记
        /// </summary>
        private static void EnqueueIfBackground(Color32[] pixels, int idx, bool[] visited,
            Queue<int> queue, Color32 bg, int thr)
        {
            if (visited[idx]) return;
            visited[idx] = true;
            if (IsClose(pixels[idx], bg, thr))
            {
                queue.Enqueue(idx);
            }
        }

        /// <summary>
        /// 颜色是否在曼哈顿距离阈值内
        /// </summary>
        private static bool IsClose(Color32 a, Color32 b, int thr)
        {
            return Mathf.Abs(a.r - b.r) <= thr
                && Mathf.Abs(a.g - b.g) <= thr
                && Mathf.Abs(a.b - b.b) <= thr;
        }

        /// <summary>
        /// 取数组中出现最多的颜色
        /// </summary>
        private static Color32 MostCommonColor(Color32[] colors)
        {
            Dictionary<int, int> count = new Dictionary<int, int>();
            int bestKey = 0;
            int bestCount = -1;
            foreach (var c in colors)
            {
                int key = (c.r << 16) | (c.g << 8) | c.b;
                count.TryGetValue(key, out int n);
                n++;
                count[key] = n;
                if (n > bestCount)
                {
                    bestCount = n;
                    bestKey = key;
                }
            }
            return new Color32((byte)((bestKey >> 16) & 0xFF), (byte)((bestKey >> 8) & 0xFF), (byte)(bestKey & 0xFF), 255);
        }

        #endregion

        #region 精灵表合成

        /// <summary>
        /// 横向拼接多张等高纹理为一张精灵表（对应原工具 merge_frames_to_sprite）
        /// </summary>
        /// <param name="frames">帧纹理列表（必须等高）</param>
        public static Texture2D MergeFramesToSprite(List<Texture2D> frames)
        {
            if (frames == null || frames.Count == 0)
            {
                throw new ArgumentException("没有可合成的帧");
            }

            int height = frames[0].height;
            int totalWidth = 0;
            foreach (var f in frames)
            {
                if (f.height != height)
                {
                    throw new Exception($"所有帧必须等高才能横向合成（期望 {height}，实际 {f.height}）");
                }
                totalWidth += f.width;
            }

            Texture2D sprite = new Texture2D(totalWidth, height, TextureFormat.RGBA32, false);
            // 先填透明
            Color32[] clear = new Color32[totalWidth * height];
            sprite.SetPixels32(clear);

            int xOffset = 0;
            foreach (var f in frames)
            {
                Color32[] px = f.GetPixels32();
                sprite.SetPixels32(xOffset, 0, f.width, f.height, px);
                xOffset += f.width;
            }
            sprite.Apply();
            return sprite;
        }

        /// <summary>
        /// 按 columns×rows 网格合成精灵表。格子尺寸取所有帧的最大宽高，帧居中放入格子；
        /// 帧按从左到右、从上到下顺序排列（第 0 帧在左上角）。
        /// </summary>
        /// <param name="frames">帧纹理列表</param>
        /// <param name="columns">列数（每行多少帧），>=1</param>
        /// <param name="rows">行数，>=1</param>
        public static Texture2D MergeFramesToSprite(List<Texture2D> frames, int columns, int rows)
        {
            if (frames == null || frames.Count == 0)
            {
                throw new ArgumentException("没有可合成的帧");
            }
            if (columns < 1 || rows < 1)
            {
                throw new ArgumentException("列数与行数必须 >= 1");
            }
            if (frames.Count > columns * rows)
            {
                throw new Exception($"布局 {columns}×{rows}={columns * rows} 个格子放不下 {frames.Count} 帧，请增大列数或行数");
            }

            // 统一格子尺寸为所有帧的最大宽高，保证每帧对齐到固定格子
            int cellW = 0, cellH = 0;
            foreach (var f in frames)
            {
                if (f.width > cellW) cellW = f.width;
                if (f.height > cellH) cellH = f.height;
            }

            int texW = cellW * columns;
            int texH = cellH * rows;
            Texture2D sprite = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            // 先填透明
            sprite.SetPixels32(new Color32[texW * texH]);

            for (int i = 0; i < frames.Count; i++)
            {
                Texture2D f = frames[i];
                int col = i % columns;
                int row = i / columns;
                int cellX = col * cellW;
                // Unity 纹理 (0,0) 在左下角，故第 0 行（视觉最上方）应放到纹理最高处
                int cellY = (rows - 1 - row) * cellH;
                // 帧在格子内居中
                int ox = cellX + (cellW - f.width) / 2;
                int oy = cellY + (cellH - f.height) / 2;
                sprite.SetPixels32(ox, oy, f.width, f.height, f.GetPixels32());
            }
            sprite.Apply();
            return sprite;
        }

        #endregion
    }
}
