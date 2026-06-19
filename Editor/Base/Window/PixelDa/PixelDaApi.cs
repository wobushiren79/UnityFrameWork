using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PixelDa
{
    #region 回调与数据结构

    /// <summary>
    /// 单 URL 结果回调（文生图 / 图编辑 / 图生视频）
    /// </summary>
    public delegate void UrlResultCallback(bool success, string url, string error);

    /// <summary>
    /// 音乐 ABC 结果回调
    /// </summary>
    public delegate void MusicResultCallback(bool success, string notation, string comments, string error);

    #endregion

    /// <summary>
    /// PixelDa 的纯 C# REST 客户端：用 HttpClient 直连豆包(Ark) / 通义(DashScope)，
    /// 复刻原 Python 后端 model_wrapper 的全部 AI 调用。所有方法在后台线程执行，
    /// 结果通过 <see cref="PixelDaDispatcher"/> 回到主线程。
    /// </summary>
    public static class PixelDaApi
    {
        #region HttpClient

        /// <summary>共享 HttpClient（单请求超时 5 分钟，轮询由循环控制）</summary>
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5),
        };

        #endregion

        #region 音乐提示词模板

        /// <summary>音乐生成系统提示词（精简自原工具 abc_notation 指令）</summary>
        private const string MUSIC_SYSTEM_PROMPT =
            "You are a helpful assistant. You can generate creative and original music based on the " +
            "input requirements given to you, and respond strictly in valid ABC notation. " +
            "Use standard ABC headers (X:, T:, M:, L:, Q:, K:) followed by the tune body. " +
            "Keep the notation parseable and self-consistent.";

        /// <summary>音乐生成用户提示词模板</summary>
        private const string MUSIC_USER_PROMPT =
            "Generate ABC notation of a piano song with ABC format, following below requirements, " +
            "and double check the format correctness:\n" +
            "duration: around {0} seconds.\n" +
            "genre: {1}.\n" +
            "tempo: {2}.\n" +
            "description: {3}.\n" +
            "return json object with keys:\n" +
            "- notation(pure ABC notation)\n" +
            "- comments(any comments)";

        #endregion

        #region 文生图

        /// <summary>
        /// 文生图，按提供商路由到豆包或通义
        /// </summary>
        public static void GenerateImage(PixelDaProvider provider, string apiKey, string prompt,
            string negativePrompt, int seed, string size,
            UrlResultCallback callback, Action<string> onProgress)
        {
            RunTask(async () =>
            {
                if (provider == PixelDaProvider.Doubao)
                {
                    return await DoubaoGenerateImageAsync(apiKey, prompt, negativePrompt, seed, size, null, onProgress);
                }
                return await TongyiImageAsync(apiKey, prompt, negativePrompt, seed, size, null, onProgress);
            }, callback);
        }

        #endregion

        #region 图编辑

        /// <summary>
        /// 图编辑（图+提示词→新图）
        /// </summary>
        public static void EditImage(PixelDaProvider provider, string apiKey, string prompt,
            string imageUrl, string negativePrompt, int seed, string size,
            UrlResultCallback callback, Action<string> onProgress)
        {
            RunTask(async () =>
            {
                if (provider == PixelDaProvider.Doubao)
                {
                    return await DoubaoGenerateImageAsync(apiKey, prompt, negativePrompt, seed, size, imageUrl, onProgress);
                }
                return await TongyiImageAsync(apiKey, prompt, negativePrompt, seed, size, imageUrl, onProgress);
            }, callback);
        }

        #endregion

        #region 图生视频

        /// <summary>
        /// 图生视频（首帧图+提示词→约5秒动画），异步轮询直至完成
        /// </summary>
        public static void GenerateVideo(PixelDaProvider provider, string apiKey, string baseImageUrl,
            string prompt, string negativePrompt, string resolution,
            UrlResultCallback callback, Action<string> onProgress, Func<bool> isCancelled)
        {
            RunTask(async () =>
            {
                if (provider == PixelDaProvider.Doubao)
                {
                    return await DoubaoVideoAsync(apiKey, baseImageUrl, prompt, resolution, onProgress, isCancelled);
                }
                return await TongyiVideoAsync(apiKey, baseImageUrl, prompt, negativePrompt, resolution, onProgress, isCancelled);
            }, callback);
        }

        #endregion

        #region 音乐 ABC

        /// <summary>
        /// 生成音乐（聊天模型输出 ABC 记谱），返回 ABC 文本与注释
        /// </summary>
        public static void GenerateMusic(PixelDaProvider provider, string apiKey, string prompt,
            int duration, string genre, string tempo, int seed,
            MusicResultCallback callback, Action<string> onProgress)
        {
            Task.Run(async () =>
            {
                try
                {
                    string content = provider == PixelDaProvider.Doubao
                        ? await DoubaoChatAsync(apiKey, prompt, duration, genre, tempo, onProgress)
                        : await TongyiChatAsync(apiKey, prompt, duration, genre, tempo, seed, onProgress);

                    string notation = null;
                    string comments = "";
                    try
                    {
                        JObject obj = JObject.Parse(content);
                        notation = (string)obj["notation"];
                        comments = (string)obj["comments"] ?? "";
                    }
                    catch
                    {
                        // 模型未严格返回 JSON 时，直接把原始文本当作 ABC
                        notation = content;
                    }

                    if (string.IsNullOrWhiteSpace(notation))
                    {
                        throw new Exception("模型返回的 ABC 记谱为空");
                    }

                    // 去掉空行
                    notation = string.Join("\n", notation.Replace("\r\n", "\n").Split('\n'));
                    string finalNotation = notation;
                    string finalComments = comments;
                    PixelDaDispatcher.Enqueue(() => callback?.Invoke(true, finalNotation, finalComments, null));
                }
                catch (Exception e)
                {
                    string err = e.Message;
                    PixelDaDispatcher.Enqueue(() => callback?.Invoke(false, null, null, err));
                }
            });
        }

        #endregion

        #region 文件下载

        /// <summary>
        /// 下载远程文件到本地路径（后台线程），完成后回调（主线程）
        /// </summary>
        public static void DownloadFile(string url, string savePath, Action<bool, string> callback)
        {
            Task.Run(async () =>
            {
                try
                {
                    byte[] data = await httpClient.GetByteArrayAsync(url);
                    File.WriteAllBytes(savePath, data);
                    PixelDaDispatcher.Enqueue(() => callback?.Invoke(true, savePath));
                }
                catch (Exception e)
                {
                    string err = e.Message;
                    PixelDaDispatcher.Enqueue(() => callback?.Invoke(false, err));
                }
            });
        }

        /// <summary>
        /// 同步下载字节（供工具类内部调用）
        /// </summary>
        public static byte[] DownloadBytes(string url)
        {
            return httpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
        }

        #endregion

        #region 豆包实现

        /// <summary>
        /// 豆包文生图 / 图编辑（同一 images/generations 接口，传 image 即为编辑）
        /// </summary>
        private static async Task<string> DoubaoGenerateImageAsync(string apiKey, string prompt,
            string negativePrompt, int seed, string size, string imageUrl, Action<string> onProgress)
        {
            Report(onProgress, "豆包：提交图片生成请求...");
            string url = PixelDaConfig.DoubaoBaseUrl.TrimEnd('/') + "/images/generations";

            JObject body = new JObject
            {
                ["model"] = PixelDaConfig.DoubaoImageModel,
                ["prompt"] = prompt,
                ["size"] = size.Replace("*", "x"),
                ["watermark"] = false,
                ["response_format"] = "url",
            };
            if (seed > 0) body["seed"] = seed;
            if (!string.IsNullOrEmpty(imageUrl)) body["image"] = imageUrl;

            JObject resp = await PostJsonAsync(url, apiKey, body);
            JToken data = resp["data"];
            if (data == null || !data.HasValues)
            {
                throw new Exception("豆包未返回图片结果");
            }
            return (string)data[0]["url"];
        }

        /// <summary>
        /// 豆包图生视频：创建任务 + 轮询
        /// </summary>
        private static async Task<string> DoubaoVideoAsync(string apiKey, string baseImageUrl,
            string prompt, string resolution, Action<string> onProgress, Func<bool> isCancelled)
        {
            Report(onProgress, "豆包：提交视频生成任务...");
            string createUrl = PixelDaConfig.DoubaoBaseUrl.TrimEnd('/') + "/contents/generations/tasks";

            JObject body = new JObject
            {
                ["model"] = PixelDaConfig.DoubaoVideoModel,
                ["content"] = new JArray
                {
                    new JObject { ["type"] = "text", ["text"] = $"{prompt} --rs {resolution}" },
                    new JObject { ["type"] = "image_url", ["image_url"] = new JObject { ["url"] = baseImageUrl } },
                },
            };

            JObject createResp = await PostJsonAsync(createUrl, apiKey, body);
            string taskId = (string)createResp["id"];
            if (string.IsNullOrEmpty(taskId))
            {
                throw new Exception("豆包未返回视频任务 ID");
            }

            string getUrl = createUrl + "/" + taskId;
            while (true)
            {
                if (isCancelled != null && isCancelled()) throw new Exception("已取消");
                await Task.Delay(5000);
                if (isCancelled != null && isCancelled()) throw new Exception("已取消");

                JObject poll = await GetJsonAsync(getUrl, apiKey);
                string status = (string)poll["status"];
                Report(onProgress, $"豆包：视频生成中... ({status})");
                if (status == "succeeded")
                {
                    string videoUrl = (string)poll["content"]?["video_url"];
                    if (string.IsNullOrEmpty(videoUrl)) throw new Exception("豆包任务完成但无视频地址");
                    return videoUrl;
                }
                if (status == "failed")
                {
                    string msg = (string)poll["error"]?["message"] ?? "未知错误";
                    throw new Exception("豆包视频任务失败: " + msg);
                }
            }
        }

        /// <summary>
        /// 豆包聊天（音乐 ABC）
        /// </summary>
        private static async Task<string> DoubaoChatAsync(string apiKey, string prompt,
            int duration, string genre, string tempo, Action<string> onProgress)
        {
            Report(onProgress, "豆包：生成音乐 ABC 记谱...");
            string url = PixelDaConfig.DoubaoBaseUrl.TrimEnd('/') + "/chat/completions";

            JObject body = new JObject
            {
                ["model"] = PixelDaConfig.DoubaoChatModel,
                ["max_tokens"] = PixelDaConfig.DEFAULT_TEXT_ENGINE_MAX_TOKENS,
                ["temperature"] = 2,
                ["presence_penalty"] = 2,
                ["response_format"] = new JObject { ["type"] = "json_object" },
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = MUSIC_SYSTEM_PROMPT },
                    new JObject { ["role"] = "user", ["content"] = string.Format(MUSIC_USER_PROMPT, duration, genre, tempo, prompt) },
                },
            };

            JObject resp = await PostJsonAsync(url, apiKey, body);
            return (string)resp["choices"]?[0]?["message"]?["content"];
        }

        #endregion

        #region 通义实现

        /// <summary>
        /// 通义文生图 / 图编辑：异步任务创建 + 轮询
        /// </summary>
        private static async Task<string> TongyiImageAsync(string apiKey, string prompt,
            string negativePrompt, int seed, string size, string imageUrl, Action<string> onProgress)
        {
            bool isEdit = !string.IsNullOrEmpty(imageUrl);
            Report(onProgress, isEdit ? "通义：提交图编辑任务..." : "通义：提交文生图任务...");

            string baseUrl = PixelDaConfig.TongyiBaseUrl.TrimEnd('/');
            string url = baseUrl + (isEdit
                ? "/services/aigc/image2image/image-synthesis"
                : "/services/aigc/text2image/image-synthesis");

            JObject input = new JObject
            {
                ["prompt"] = prompt,
                ["negative_prompt"] = negativePrompt ?? "",
            };
            if (isEdit)
            {
                input["function"] = PixelDaConfig.DEFAULT_TONGYI_EDIT_FUNCTION;
                input["base_image_url"] = imageUrl;
            }

            JObject parameters = new JObject
            {
                ["n"] = 1,
                ["size"] = size,
                ["prompt_extend"] = false,
            };
            if (seed > 0) parameters["seed"] = seed;

            JObject body = new JObject
            {
                ["model"] = isEdit ? PixelDaConfig.TongyiEditModel : PixelDaConfig.TongyiImageModel,
                ["input"] = input,
                ["parameters"] = parameters,
            };

            JObject createResp = await PostJsonAsync(url, apiKey, body, async: true);
            string taskId = (string)createResp["output"]?["task_id"];
            if (string.IsNullOrEmpty(taskId)) throw new Exception("通义未返回任务 ID");

            JObject final = await TongyiPollTaskAsync(apiKey, taskId, onProgress, null);
            JToken results = final["output"]?["results"];
            if (results == null || !results.HasValues) throw new Exception("通义任务完成但无图片结果");
            return (string)results[0]["url"];
        }

        /// <summary>
        /// 通义图生视频：异步任务创建 + 轮询
        /// </summary>
        private static async Task<string> TongyiVideoAsync(string apiKey, string baseImageUrl,
            string prompt, string negativePrompt, string resolution, Action<string> onProgress, Func<bool> isCancelled)
        {
            Report(onProgress, "通义：提交视频生成任务...");
            string baseUrl = PixelDaConfig.TongyiBaseUrl.TrimEnd('/');
            string url = baseUrl + "/services/aigc/image2video/video-synthesis";

            JObject body = new JObject
            {
                ["model"] = PixelDaConfig.TongyiVideoModel,
                ["input"] = new JObject
                {
                    ["prompt"] = prompt,
                    ["negative_prompt"] = negativePrompt ?? "",
                    ["img_url"] = baseImageUrl,
                },
                ["parameters"] = new JObject
                {
                    ["resolution"] = resolution,
                    ["prompt_extend"] = false,
                },
            };

            JObject createResp = await PostJsonAsync(url, apiKey, body, async: true);
            string taskId = (string)createResp["output"]?["task_id"];
            if (string.IsNullOrEmpty(taskId)) throw new Exception("通义未返回视频任务 ID");

            JObject final = await TongyiPollTaskAsync(apiKey, taskId, onProgress, isCancelled);
            string videoUrl = (string)final["output"]?["video_url"];
            if (string.IsNullOrEmpty(videoUrl)) throw new Exception("通义任务完成但无视频地址");
            return videoUrl;
        }

        /// <summary>
        /// 通义任务轮询（文生图 / 视频共用 /tasks/{id} 端点）
        /// </summary>
        private static async Task<JObject> TongyiPollTaskAsync(string apiKey, string taskId,
            Action<string> onProgress, Func<bool> isCancelled)
        {
            string getUrl = PixelDaConfig.TongyiBaseUrl.TrimEnd('/') + "/tasks/" + taskId;
            while (true)
            {
                if (isCancelled != null && isCancelled()) throw new Exception("已取消");
                await Task.Delay(5000);
                if (isCancelled != null && isCancelled()) throw new Exception("已取消");

                JObject poll = await GetJsonAsync(getUrl, apiKey);
                string status = (string)poll["output"]?["task_status"];
                Report(onProgress, $"通义：生成中... ({status})");
                if (status == "SUCCEEDED") return poll;
                if (status == "FAILED" || status == "CANCELED" || status == "UNKNOWN")
                {
                    string msg = (string)poll["output"]?["message"] ?? "未知错误";
                    throw new Exception("通义任务失败: " + msg);
                }
            }
        }

        /// <summary>
        /// 通义聊天（音乐 ABC），OpenAI 兼容模式
        /// </summary>
        private static async Task<string> TongyiChatAsync(string apiKey, string prompt,
            int duration, string genre, string tempo, int seed, Action<string> onProgress)
        {
            Report(onProgress, "通义：生成音乐 ABC 记谱...");
            string url = PixelDaConfig.TongyiChatBaseUrl.TrimEnd('/') + "/chat/completions";

            JObject body = new JObject
            {
                ["model"] = PixelDaConfig.TongyiChatModel,
                ["max_tokens"] = PixelDaConfig.DEFAULT_TEXT_ENGINE_MAX_TOKENS,
                ["temperature"] = 1.9,
                ["presence_penalty"] = 2,
                ["response_format"] = new JObject { ["type"] = "json_object" },
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = MUSIC_SYSTEM_PROMPT },
                    new JObject { ["role"] = "user", ["content"] = string.Format(MUSIC_USER_PROMPT, duration, genre, tempo, prompt) },
                },
            };
            if (seed > 0) body["seed"] = seed;

            JObject resp = await PostJsonAsync(url, apiKey, body);
            return (string)resp["choices"]?[0]?["message"]?["content"];
        }

        #endregion

        #region HTTP 基础方法

        /// <summary>
        /// 发送 POST JSON 请求，返回解析后的 JObject；非 2xx 抛异常含服务端报错
        /// </summary>
        private static async Task<JObject> PostJsonAsync(string url, string apiKey, JObject body, bool async = false)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                if (async)
                {
                    request.Headers.Add("X-DashScope-Async", "enable");
                }
                request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.SendAsync(request);
                string text = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HTTP {(int)response.StatusCode}: {ExtractError(text)}");
                }
                return JObject.Parse(text);
            }
        }

        /// <summary>
        /// 发送 GET JSON 请求
        /// </summary>
        private static async Task<JObject> GetJsonAsync(string url, string apiKey)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                HttpResponseMessage response = await httpClient.SendAsync(request);
                string text = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HTTP {(int)response.StatusCode}: {ExtractError(text)}");
                }
                return JObject.Parse(text);
            }
        }

        /// <summary>
        /// 从服务端返回体中提取可读错误信息
        /// </summary>
        private static string ExtractError(string text)
        {
            try
            {
                JObject obj = JObject.Parse(text);
                string msg = (string)obj["error"]?["message"]
                             ?? (string)obj["message"]
                             ?? (string)obj["error"];
                return string.IsNullOrEmpty(msg) ? text : msg;
            }
            catch
            {
                return text;
            }
        }

        /// <summary>
        /// 统一跑后台任务，结果回主线程的 <see cref="UrlResultCallback"/>
        /// </summary>
        private static void RunTask(Func<Task<string>> work, UrlResultCallback callback)
        {
            Task.Run(async () =>
            {
                try
                {
                    string url = await work();
                    PixelDaDispatcher.Enqueue(() => callback?.Invoke(true, url, null));
                }
                catch (Exception e)
                {
                    string err = e.Message;
                    PixelDaDispatcher.Enqueue(() => callback?.Invoke(false, null, err));
                }
            });
        }

        /// <summary>
        /// 把进度文本回报到主线程
        /// </summary>
        private static void Report(Action<string> onProgress, string msg)
        {
            if (onProgress != null)
            {
                PixelDaDispatcher.Enqueue(() => onProgress(msg));
            }
        }

        #endregion
    }
}
