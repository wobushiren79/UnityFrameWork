using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>SteamPipe 的一条本地文件到安装目录的映射。</summary>
[Serializable]
internal sealed class SteamPipeMapping
{
    public string localPath = "*";
    public string depotPath = ".";
    public bool recursive = true;
}

/// <summary>为 Depot 中的文件指定 Steam 更新与校验属性。</summary>
[Serializable]
internal sealed class SteamPipeFileProperty
{
    public string localPath = "";
    public string attributes = "userconfig";
}

/// <summary>一个 Steam Depot 的内容来源、映射和筛选条件。</summary>
[Serializable]
internal sealed class SteamPipeDepot
{
    public string id = "";
    public string contentRoot = "";
    public List<SteamPipeMapping> mappings = new List<SteamPipeMapping> { new SteamPipeMapping() };
    public List<string> exclusions = new List<string> { "steam_appid.txt", "*.pdb", "*_BackUpThisFolder_ButDontShipItWithYourGame/*", "*_BurstDebugInformation_DoNotShip/*" };
    public List<string> installScripts = new List<string>();
    public List<SteamPipeFileProperty> properties = new List<SteamPipeFileProperty>();
}

/// <summary>此 Unity 项目中可重复选择的一套上传配置。</summary>
[Serializable]
internal sealed class SteamPipeProfile
{
    public string name = "新配置";
    public string appId = "";
    public string account = "";
    public string contentRoot = "";
    public string branch = "";
    public string description = "";
    public bool useBeta;
    public List<SteamPipeDepot> depots = new List<SteamPipeDepot> { new SteamPipeDepot() };
}

/// <summary>按操作系统用户保存工具位置与账号，按 Unity 项目路径隔离发布配置。</summary>
internal sealed class SteamPipeSettings
{
    #region 本机数据

    public string sdkPath = "";
    public List<string> accounts = new List<string>();
    public List<SteamPipeProfile> profiles = new List<SteamPipeProfile>();
    public int selectedProfile;

    private string userSnapshot;
    private string projectSnapshot;
    private static readonly JsonSerializerSettings JsonOptions = new JsonSerializerSettings
    {
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        MissingMemberHandling = MissingMemberHandling.Error
    };

    private sealed class UserFile
    {
        public string sdkPath = "";
        public List<string> accounts = new List<string>();
    }

    private sealed class ProjectFile
    {
        public List<SteamPipeProfile> profiles = new List<SteamPipeProfile> { new SteamPipeProfile() };
        public int selectedProfile;
    }

    /// <summary>系统用户的独立数据目录，无需将配置写入 Unity 或 Git 工作树。</summary>
    public static string UserDirectory
    {
        get
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(local) || !Path.IsPathRooted(local))
                throw new IOException("系统未提供可用的本机用户数据目录。");
            return Path.Combine(local, "UnitySteamPipe");
        }
    }

    /// <summary>使用绝对项目路径的摘要区分同名项目及不同工作副本。</summary>
    public static string ProjectDirectory
    {
        get
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            if (Path.DirectorySeparatorChar == '\\')
                project = project.ToUpperInvariant();
            using (var sha = SHA256.Create())
            {
                string id = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(project))).Replace("-", "");
                return Path.Combine(UserDirectory, "projects", id.Substring(0, 24));
            }
        }
    }

    #endregion

    #region 加载与保存

    /// <summary>加载两份配置；损坏时向调用者报告原路径，保留原文件。</summary>
    public static SteamPipeSettings Load()
    {
        var user = Read<UserFile>(Path.Combine(UserDirectory, "accounts.json"));
        var project = Read<ProjectFile>(Path.Combine(ProjectDirectory, "profiles.json"));
        if (user.accounts == null || project.profiles == null || project.profiles.Exists(profile => profile == null ||
            profile.depots == null || profile.depots.Exists(depot => depot == null || depot.mappings == null ||
            depot.exclusions == null || depot.installScripts == null || depot.properties == null ||
            depot.properties.Exists(property => property == null) || depot.mappings.Exists(mapping => mapping == null))))
            throw new InvalidDataException($"SteamPipe 本机配置包含空条目，请检查 {UserDirectory}。");
        return new SteamPipeSettings
        {
            sdkPath = user.sdkPath ?? "",
            accounts = user.accounts,
            profiles = project.profiles,
            selectedProfile = Math.Max(0, Math.Min(project.selectedProfile, project.profiles.Count - 1)),
            userSnapshot = Encode(user),
            projectSnapshot = Encode(project)
        };
    }

    /// <summary>只写有修改的配置，防止另一个项目的写入被旧窗口覆盖。</summary>
    public void Save()
    {
        SaveChanged(Path.Combine(UserDirectory, "accounts.json"), new UserFile { sdkPath = sdkPath, accounts = accounts }, ref userSnapshot);
        SaveChanged(Path.Combine(ProjectDirectory, "profiles.json"), new ProjectFile { profiles = profiles, selectedProfile = selectedProfile }, ref projectSnapshot);
    }

    /// <summary>将配置转换为格式稳定的 JSON 快照。</summary>
    private static string Encode<T>(T value) => JsonConvert.SerializeObject(value, Formatting.Indented, JsonOptions);

    /// <summary>读取存在的配置，只在首次使用时使用默认值。</summary>
    private static T Read<T>(string path) where T : new()
    {
        if (!File.Exists(path))
            return new T();
        try
        {
            var result = JsonConvert.DeserializeObject<T>(File.ReadAllText(path, Encoding.UTF8), JsonOptions);
            if (result == null)
                throw new InvalidDataException("文件内缺少配置对象。");
            return result;
        }
        catch (Exception e) when (e is JsonException || e is InvalidDataException)
        {
            throw new InvalidDataException($"无法读取 SteamPipe 配置 {path}。{e.Message}", e);
        }
    }

    /// <summary>锁内比较读取时的快照，再用同目录临时文件原子替换。</summary>
    private static void SaveChanged<T>(string path, T file, ref string snapshot) where T : new()
    {
        string next = Encode(file);
        if (next == snapshot)
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (new FileStream($"{path}.lock", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        {
            if (Encode(Read<T>(path)) != snapshot)
                throw new IOException($"配置已由其他窗口修改，请重新打开 SteamPipe 后再编辑。文件为 {path}");
            string temp = $"{path}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(temp, next, new UTF8Encoding(false));
                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
                snapshot = next;
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }
    }

    #endregion
}
