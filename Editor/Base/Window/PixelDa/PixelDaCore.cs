using System;
using System.Collections.Concurrent;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelDa
{
    #region 枚举定义

    /// <summary>
    /// AI 提供商类型（对应原工具的 model_type）
    /// </summary>
    public enum PixelDaProvider
    {
        /// <summary>豆包（火山引擎 Ark）</summary>
        Doubao = 0,

        /// <summary>通义（阿里百炼 DashScope）</summary>
        Tongyi = 1,
    }

    #endregion

    /// <summary>
    /// PixelDa 编辑器工具的全局配置，全部通过 EditorPrefs 持久化。
    /// 端点 URL 与模型名做成可改字段，避免官方接口变动后写死失效。
    /// </summary>
    public static class PixelDaConfig
    {
        #region 常量与默认值

        /// <summary>EditorPrefs 键前缀</summary>
        private const string KEY_PREFIX = "PixelDa.";

        /// <summary>豆包默认基础地址</summary>
        public const string DEFAULT_DOUBAO_BASE_URL = "https://ark.cn-beijing.volces.com/api/v3";

        /// <summary>通义默认基础地址</summary>
        public const string DEFAULT_TONGYI_BASE_URL = "https://dashscope.aliyuncs.com/api/v1";

        /// <summary>通义聊天（音乐）兼容模式地址</summary>
        public const string DEFAULT_TONGYI_CHAT_BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1";

        /// <summary>豆包文生图/图编辑模型</summary>
        public const string DEFAULT_DOUBAO_IMAGE_MODEL = "doubao-seedream-4-0-250828";

        /// <summary>豆包图生视频模型</summary>
        public const string DEFAULT_DOUBAO_VIDEO_MODEL = "doubao-seedance-1-0-pro-250528";

        /// <summary>豆包聊天（音乐 ABC）模型</summary>
        public const string DEFAULT_DOUBAO_CHAT_MODEL = "doubao-seed-1-6-251015";

        /// <summary>通义文生图模型</summary>
        public const string DEFAULT_TONGYI_IMAGE_MODEL = "wan2.5-t2i-preview";

        /// <summary>通义图编辑模型</summary>
        public const string DEFAULT_TONGYI_EDIT_MODEL = "wanx2.1-imageedit";

        /// <summary>通义图编辑功能名</summary>
        public const string DEFAULT_TONGYI_EDIT_FUNCTION = "description_edit";

        /// <summary>通义图生视频模型</summary>
        public const string DEFAULT_TONGYI_VIDEO_MODEL = "wan2.5-i2v-preview";

        /// <summary>通义聊天（音乐 ABC）模型</summary>
        public const string DEFAULT_TONGYI_CHAT_MODEL = "qwen-plus";

        /// <summary>默认图片尺寸（通义用 * 分隔，豆包用 x，发送时自动转换）</summary>
        public const string DEFAULT_IMAGE_SIZE = "1024*1024";

        /// <summary>默认视频分辨率</summary>
        public const string DEFAULT_VIDEO_RESOLUTION = "480P";

        /// <summary>聊天最大 token</summary>
        public const int DEFAULT_TEXT_ENGINE_MAX_TOKENS = 2048;

        /// <summary>默认输出目录（相对项目根，放在 Assets 下方便 Unity 自动导入）</summary>
        public const string DEFAULT_OUTPUT_FOLDER = "Assets/Out/PixelDa";

        #endregion

        #region 项目级配置存储（随 git 提交、团队共享）

        /// <summary>
        /// 随项目提交的配置数据：API Key/端点/模型/尺寸/输出目录。
        /// 落盘到 <see cref="PROJECT_CONFIG_PATH"/>，其他成员拉取后即可直接使用。
        /// </summary>
        [Serializable]
        private class ProjectData
        {
            /// <summary>豆包 API Key</summary>
            public string doubaoApiKey = "";

            /// <summary>通义 API Key</summary>
            public string tongyiApiKey = "";

            /// <summary>豆包基础地址</summary>
            public string doubaoBaseUrl = DEFAULT_DOUBAO_BASE_URL;

            /// <summary>通义基础地址</summary>
            public string tongyiBaseUrl = DEFAULT_TONGYI_BASE_URL;

            /// <summary>通义聊天兼容地址</summary>
            public string tongyiChatBaseUrl = DEFAULT_TONGYI_CHAT_BASE_URL;

            /// <summary>豆包文生图/图编辑模型</summary>
            public string doubaoImageModel = DEFAULT_DOUBAO_IMAGE_MODEL;

            /// <summary>豆包图生视频模型</summary>
            public string doubaoVideoModel = DEFAULT_DOUBAO_VIDEO_MODEL;

            /// <summary>豆包聊天模型</summary>
            public string doubaoChatModel = DEFAULT_DOUBAO_CHAT_MODEL;

            /// <summary>通义文生图模型</summary>
            public string tongyiImageModel = DEFAULT_TONGYI_IMAGE_MODEL;

            /// <summary>通义图编辑模型</summary>
            public string tongyiEditModel = DEFAULT_TONGYI_EDIT_MODEL;

            /// <summary>通义图生视频模型</summary>
            public string tongyiVideoModel = DEFAULT_TONGYI_VIDEO_MODEL;

            /// <summary>通义聊天模型</summary>
            public string tongyiChatModel = DEFAULT_TONGYI_CHAT_MODEL;

            /// <summary>默认图片尺寸</summary>
            public string imageSize = DEFAULT_IMAGE_SIZE;

            /// <summary>默认视频分辨率</summary>
            public string videoResolution = DEFAULT_VIDEO_RESOLUTION;

            /// <summary>输出目录（相对项目根）</summary>
            public string outputFolder = DEFAULT_OUTPUT_FOLDER;
        }

        /// <summary>项目配置文件路径（相对项目根，随 git 提交）</summary>
        public const string PROJECT_CONFIG_PATH = "Assets/FrameWork/Editor/Base/Window/PixelDa/PixelDaProjectConfig.json";

        /// <summary>配置内存缓存</summary>
        private static ProjectData _data;

        /// <summary>懒加载的项目配置</summary>
        private static ProjectData Data
        {
            get
            {
                if (_data == null) LoadProjectData();
                return _data;
            }
        }

        /// <summary>配置文件绝对路径</summary>
        private static string GetConfigAbsolutePath()
        {
            return Path.Combine(GetProjectRoot(), PROJECT_CONFIG_PATH.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>从项目 JSON 读取配置；缺失或损坏时回退默认值</summary>
        private static void LoadProjectData()
        {
            string path = GetConfigAbsolutePath();
            if (File.Exists(path))
            {
                try { _data = JsonUtility.FromJson<ProjectData>(File.ReadAllText(path)) ?? new ProjectData(); }
                catch { _data = new ProjectData(); }
            }
            else
            {
                _data = new ProjectData();
            }
        }

        /// <summary>把配置写回项目 JSON（仅值变化时调用，避免频繁写盘）</summary>
        private static void SaveProjectData()
        {
            try
            {
                string path = GetConfigAbsolutePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(_data, true));
            }
            catch (Exception e)
            {
                Debug.LogError("[PixelDa] 保存项目配置失败: " + e);
            }
        }

        /// <summary>手动重新从磁盘加载（如他人更新后）</summary>
        public static void ReloadProjectData()
        {
            LoadProjectData();
        }

        #endregion

        #region 机器本地设置（EditorPrefs，不入库）

        /// <summary>当前选中的提供商（每人各自的 UI 偏好，不共享）</summary>
        public static PixelDaProvider Provider
        {
            get => (PixelDaProvider)EditorPrefs.GetInt(KEY_PREFIX + "Provider", 0);
            set => EditorPrefs.SetInt(KEY_PREFIX + "Provider", (int)value);
        }

        /// <summary>ffmpeg 可执行文件路径（与机器相关，不共享；留空则从系统 PATH 查找）</summary>
        public static string FfmpegPath
        {
            get => EditorPrefs.GetString(KEY_PREFIX + "FfmpegPath", "");
            set => EditorPrefs.SetString(KEY_PREFIX + "FfmpegPath", value);
        }

        #endregion

        #region 项目共享设置（读写均走项目 JSON）

        /// <summary>豆包 API Key</summary>
        public static string DoubaoApiKey
        {
            get => Data.doubaoApiKey;
            set { if (Data.doubaoApiKey != value) { Data.doubaoApiKey = value; SaveProjectData(); } }
        }

        /// <summary>通义 API Key</summary>
        public static string TongyiApiKey
        {
            get => Data.tongyiApiKey;
            set { if (Data.tongyiApiKey != value) { Data.tongyiApiKey = value; SaveProjectData(); } }
        }

        /// <summary>豆包基础地址</summary>
        public static string DoubaoBaseUrl
        {
            get => Data.doubaoBaseUrl;
            set { if (Data.doubaoBaseUrl != value) { Data.doubaoBaseUrl = value; SaveProjectData(); } }
        }

        /// <summary>通义基础地址</summary>
        public static string TongyiBaseUrl
        {
            get => Data.tongyiBaseUrl;
            set { if (Data.tongyiBaseUrl != value) { Data.tongyiBaseUrl = value; SaveProjectData(); } }
        }

        /// <summary>通义聊天兼容地址</summary>
        public static string TongyiChatBaseUrl
        {
            get => Data.tongyiChatBaseUrl;
            set { if (Data.tongyiChatBaseUrl != value) { Data.tongyiChatBaseUrl = value; SaveProjectData(); } }
        }

        /// <summary>豆包文生图/图编辑模型</summary>
        public static string DoubaoImageModel
        {
            get => Data.doubaoImageModel;
            set { if (Data.doubaoImageModel != value) { Data.doubaoImageModel = value; SaveProjectData(); } }
        }

        /// <summary>豆包图生视频模型</summary>
        public static string DoubaoVideoModel
        {
            get => Data.doubaoVideoModel;
            set { if (Data.doubaoVideoModel != value) { Data.doubaoVideoModel = value; SaveProjectData(); } }
        }

        /// <summary>豆包聊天模型</summary>
        public static string DoubaoChatModel
        {
            get => Data.doubaoChatModel;
            set { if (Data.doubaoChatModel != value) { Data.doubaoChatModel = value; SaveProjectData(); } }
        }

        /// <summary>通义文生图模型</summary>
        public static string TongyiImageModel
        {
            get => Data.tongyiImageModel;
            set { if (Data.tongyiImageModel != value) { Data.tongyiImageModel = value; SaveProjectData(); } }
        }

        /// <summary>通义图编辑模型</summary>
        public static string TongyiEditModel
        {
            get => Data.tongyiEditModel;
            set { if (Data.tongyiEditModel != value) { Data.tongyiEditModel = value; SaveProjectData(); } }
        }

        /// <summary>通义图生视频模型</summary>
        public static string TongyiVideoModel
        {
            get => Data.tongyiVideoModel;
            set { if (Data.tongyiVideoModel != value) { Data.tongyiVideoModel = value; SaveProjectData(); } }
        }

        /// <summary>通义聊天模型</summary>
        public static string TongyiChatModel
        {
            get => Data.tongyiChatModel;
            set { if (Data.tongyiChatModel != value) { Data.tongyiChatModel = value; SaveProjectData(); } }
        }

        /// <summary>默认图片尺寸</summary>
        public static string ImageSize
        {
            get => Data.imageSize;
            set { if (Data.imageSize != value) { Data.imageSize = value; SaveProjectData(); } }
        }

        /// <summary>默认视频分辨率</summary>
        public static string VideoResolution
        {
            get => Data.videoResolution;
            set { if (Data.videoResolution != value) { Data.videoResolution = value; SaveProjectData(); } }
        }

        /// <summary>输出目录（相对项目根）</summary>
        public static string OutputFolder
        {
            get => Data.outputFolder;
            set { if (Data.outputFolder != value) { Data.outputFolder = value; SaveProjectData(); } }
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 获取当前提供商的 API Key
        /// </summary>
        public static string GetActiveApiKey(PixelDaProvider provider)
        {
            return provider == PixelDaProvider.Doubao ? DoubaoApiKey : TongyiApiKey;
        }

        /// <summary>
        /// 获取项目根目录绝对路径（Assets 的上一级）
        /// </summary>
        public static string GetProjectRoot()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        /// <summary>
        /// 获取输出目录的绝对路径，必要时创建
        /// </summary>
        public static string GetOutputFolderAbsolute(string subFolder = "")
        {
            string root = GetProjectRoot();
            string folder = Path.Combine(root, OutputFolder.Replace('/', Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(subFolder))
            {
                folder = Path.Combine(folder, subFolder);
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return folder;
        }

        /// <summary>
        /// 把绝对路径转换为相对项目根的 Unity 资源路径（Assets/...），不在 Assets 下则返回 null
        /// </summary>
        public static string ToUnityAssetPath(string absolutePath)
        {
            string root = GetProjectRoot();
            absolutePath = Path.GetFullPath(absolutePath);
            root = Path.GetFullPath(root);
            if (!absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            string rel = absolutePath.Substring(root.Length).TrimStart('\\', '/');
            return rel.Replace('\\', '/');
        }

        #endregion
    }

    /// <summary>
    /// 编辑器主线程派发器：HttpClient 在后台线程跑，结果回调需要在主线程执行 UI / 资源操作时入队。
    /// </summary>
    [InitializeOnLoad]
    public static class PixelDaDispatcher
    {
        #region 字段

        /// <summary>待主线程执行的动作队列</summary>
        private static readonly ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();

        #endregion

        #region 初始化

        /// <summary>
        /// 静态构造：挂接到 EditorApplication.update 持续抽干队列
        /// </summary>
        static PixelDaDispatcher()
        {
            EditorApplication.update += Update;
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 把动作排入主线程执行
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action != null)
            {
                actionQueue.Enqueue(action);
            }
        }

        #endregion

        #region 内部逻辑

        /// <summary>
        /// 每帧抽干队列，在主线程执行
        /// </summary>
        private static void Update()
        {
            while (actionQueue.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PixelDa] 主线程回调异常: {e}");
                }
            }
        }

        #endregion
    }
}
