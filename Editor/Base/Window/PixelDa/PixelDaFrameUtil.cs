using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace PixelDa
{
    #region 回调

    /// <summary>
    /// 抽帧结果回调，返回提取出的帧文件绝对路径列表
    /// </summary>
    public delegate void FrameSplitCallback(bool success, List<string> framePaths, string error);

    #endregion

    /// <summary>
    /// PixelDa 视频抽帧与打包工具：纯 C# 调系统 ffmpeg 按均匀时间戳抽帧（对应原工具 OpenCV 抽帧），
    /// 以及把帧打包为 zip（对应原工具 zip_frames）。
    /// </summary>
    public static class PixelDaFrameUtil
    {
        #region ffmpeg 定位

        /// <summary>
        /// 解析 ffmpeg 可执行路径：优先用设置里的路径，否则回退到 PATH 中的 ffmpeg
        /// </summary>
        public static string ResolveFfmpeg()
        {
            string custom = PixelDaConfig.FfmpegPath;
            if (!string.IsNullOrEmpty(custom) && File.Exists(custom))
            {
                return custom;
            }
            return "ffmpeg";
        }

        /// <summary>
        /// 检测 ffmpeg 是否可用
        /// </summary>
        public static bool IsFfmpegAvailable()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ResolveFfmpeg(),
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit(5000);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 抽帧

        /// <summary>
        /// 后台抽帧：在 [fromTime, toTime] 区间内均匀取 count 帧，结果回主线程
        /// </summary>
        public static void ExtractFramesAsync(string videoPath, float fromTime, float toTime, int count,
            string outputDir, FrameSplitCallback callback)
        {
            // ffmpeg 路径需读取 EditorPrefs，只能在主线程解析，故在开后台任务前先取好再传入
            string ffmpeg = ResolveFfmpeg();
            Task.Run(() =>
            {
                try
                {
                    List<string> frames = ExtractFrames(videoPath, fromTime, toTime, count, outputDir, ffmpeg);
                    PixelDaDispatcher.Enqueue(() => callback?.Invoke(true, frames, null));
                }
                catch (Exception e)
                {
                    string err = e.Message;
                    PixelDaDispatcher.Enqueue(() => callback?.Invoke(false, null, err));
                }
            });
        }

        /// <summary>
        /// 同步抽帧：调 ffmpeg 逐时间戳精确抽帧
        /// </summary>
        /// <param name="ffmpeg">ffmpeg 可执行路径；为空则在主线程上下文回退解析（后台线程调用时务必由调用方先用 ResolveFfmpeg 取好传入）</param>
        public static List<string> ExtractFrames(string videoPath, float fromTime, float toTime, int count, string outputDir, string ffmpeg = null)
        {
            if (!File.Exists(videoPath))
            {
                throw new FileNotFoundException("视频文件不存在: " + videoPath);
            }
            if (count < 1) count = 1;
            if (toTime <= fromTime) throw new Exception("结束时间必须大于开始时间");

            Directory.CreateDirectory(outputDir);
            if (string.IsNullOrEmpty(ffmpeg)) ffmpeg = ResolveFfmpeg();

            // 计算均匀时间戳（与原工具一致：count==1 取起点，否则含首尾均分）
            List<float> timestamps = new List<float>();
            if (count == 1)
            {
                timestamps.Add(fromTime);
            }
            else
            {
                float interval = (toTime - fromTime) / (count - 1);
                for (int i = 0; i < count; i++)
                {
                    timestamps.Add(fromTime + i * interval);
                }
            }

            List<string> result = new List<string>();
            for (int i = 0; i < timestamps.Count; i++)
            {
                string outPath = Path.Combine(outputDir, $"frame_{i:D4}.png");
                // -ss 放在 -i 前为快速定位；-frames:v 1 取单帧
                string args = string.Format(CultureInfo.InvariantCulture,
                    "-y -ss {0} -i \"{1}\" -frames:v 1 \"{2}\"",
                    timestamps[i].ToString("F3", CultureInfo.InvariantCulture), videoPath, outPath);

                RunFfmpeg(ffmpeg, args);

                if (File.Exists(outPath))
                {
                    result.Add(outPath);
                }
            }

            if (result.Count == 0)
            {
                throw new Exception("ffmpeg 未能提取到任何帧，请检查 ffmpeg 是否可用及视频是否有效");
            }
            return result;
        }

        /// <summary>
        /// 运行 ffmpeg 进程。并发读取 stdout/stderr 避免管道缓冲写满导致的死锁，并加执行超时
        /// </summary>
        private static void RunFfmpeg(string ffmpeg, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            Process p;
            try
            {
                p = Process.Start(psi);
            }
            catch (Exception e)
            {
                throw new Exception("调用 ffmpeg 失败（请确认已安装并配置路径）: " + e.Message);
            }

            using (p)
            {
                // ffmpeg 日志几乎都走 stderr：必须并发读两个流，否则缓冲写满会与 WaitForExit 互相死锁
                Task<string> outTask = p.StandardOutput.ReadToEndAsync();
                Task<string> errTask = p.StandardError.ReadToEndAsync();

                // 单帧抽取很快，给足 120 秒兜底；超时则杀进程避免永久卡死
                if (!p.WaitForExit(120000))
                {
                    try { p.Kill(); } catch { }
                    throw new Exception("ffmpeg 执行超时（超过 120 秒），已强制结束。请检查视频是否过大或 ffmpeg 是否异常。");
                }

                // 等待两个异步读取真正读完（WaitForExit(timeout) 不保证已读尽流尾）
                try { Task.WaitAll(new Task[] { outTask, errTask }, 5000); } catch { }

                if (p.ExitCode != 0)
                {
                    string err = errTask.Status == TaskStatus.RanToCompletion ? errTask.Result : "";
                    if (err != null && err.Length > 600) err = err.Substring(err.Length - 600);
                    throw new Exception($"ffmpeg 退出码 {p.ExitCode}。{err}");
                }
            }
        }

        #endregion

        #region 打包

        /// <summary>
        /// 把帧文件打包成 zip（对应原工具 output_type="zip"）
        /// </summary>
        /// <param name="framePaths">帧文件绝对路径</param>
        /// <param name="name">输出名前缀，文件按 name_0000.png 重命名</param>
        /// <param name="zipPath">输出 zip 完整路径</param>
        public static void ZipFrames(List<string> framePaths, string name, string zipPath)
        {
            if (framePaths == null || framePaths.Count == 0)
            {
                throw new ArgumentException("没有可打包的帧");
            }
            if (File.Exists(zipPath)) File.Delete(zipPath);

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                for (int i = 0; i < framePaths.Count; i++)
                {
                    if (!File.Exists(framePaths[i])) continue;
                    string ext = Path.GetExtension(framePaths[i]);
                    string entryName = $"{name}_{i:D4}{ext}";
                    zip.CreateEntryFromFile(framePaths[i], entryName);
                }
            }
        }

        #endregion
    }
}
