using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>校验发布配置，将可编辑的 Depot 规则与 SteamPipe KeyValues 脚本互转。</summary>
internal static class SteamPipeVdf
{
    #region 配置校验

    /// <summary>启动 SteamCMD 前校验标识、分支、源目录及所有文件规则。</summary>
    public static void Validate(SteamPipeProfile profile, bool setLive)
    {
        ValidateRules(profile, setLive);
        ExistingRoot(profile.contentRoot);
        var project = Directory.GetParent(Application.dataPath).FullName;
        foreach (var root in profile.depots.Select(depot => ExistingRoot(DepotRoot(profile, depot))).Distinct(PathComparer))
        {
            if (Contains(root, project) || Overlaps(root, SteamPipeSettings.UserDirectory) ||
                new[] { "Assets", "Library", "Packages", "ProjectSettings", "UserSettings", ".git" }
                    .Any(folder => Overlaps(root, Path.Combine(project, folder))) ||
                Directory.Exists(Path.Combine(root, ".git")) || File.Exists(Path.Combine(root, ".git")) ||
                Directory.Exists(Path.Combine(root, "ProjectSettings")) || File.Exists(Path.Combine(root, "steamcmd.exe")) ||
                File.Exists(Path.Combine(root, "config", "config.vdf")) || File.Exists(Path.Combine(root, "loginusers.vdf")) ||
                File.Exists(Path.Combine(root, "config.vdf")) || PathParts(root).Any(part => part.Equals(".git", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException($"内容目录包含工程、账户或工具数据，请改选游戏包目录。{root}");
        }
    }

    /// <summary>只核对发布规则，供尚未生成包目录的一键构建在执行前使用。</summary>
    public static void ValidateRules(SteamPipeProfile profile, bool setLive)
    {
        if (profile == null)
            throw new InvalidDataException("请选择游戏配置。");
        CheckId(profile.appId, "App ID");
        CheckText(profile.description ?? "", "版本备注");
        if (setLive)
        {
            CheckText(profile.branch ?? "", "测试分支");
            if (string.IsNullOrWhiteSpace(profile.branch) || profile.branch != profile.branch.Trim() ||
                string.Equals(profile.branch, "default", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(profile.branch, "public", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("请选择测试分支；默认公开分支请在 Steamworks 后台上线。");
        }
        if (profile.depots == null || profile.depots.Count == 0)
            throw new InvalidDataException("请至少配置一个 Depot。");
        var ids = new HashSet<uint>();
        foreach (var depot in profile.depots)
        {
            if (depot == null || !ids.Add(CheckId(depot.id, "Depot ID")))
                throw new InvalidDataException("存在空白或重复的 Depot。");
            if (!string.IsNullOrWhiteSpace(depot.contentRoot) && !Path.IsPathRooted(depot.contentRoot))
                CheckRule(depot.contentRoot, "Depot 相对内容目录", false);
            if (depot.mappings == null || depot.mappings.Count == 0)
                throw new InvalidDataException($"Depot {depot.id} 至少需要一条文件映射。");
            foreach (var map in depot.mappings)
            {
                if (map == null)
                    throw new InvalidDataException($"Depot {depot.id} 包含空白映射。");
                CheckRule(map.localPath, "本地映射", true);
                CheckRule(map.depotPath, "安装目录", false);
            }
            foreach (var rule in depot.exclusions ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(rule)) CheckRule(rule, "排除规则", true);
            foreach (var script in depot.installScripts ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(script)) CheckRule(script, "安装脚本", false);
            foreach (var property in depot.properties ?? new List<SteamPipeFileProperty>())
                CheckProperty(property);
        }
    }

    /// <summary>以游戏包为基准展开可选的 Depot 内容目录覆盖。</summary>
    public static string DepotRoot(SteamPipeProfile profile, SteamPipeDepot depot)
    {
        if (string.IsNullOrWhiteSpace(depot.contentRoot))
            return Path.GetFullPath(profile.contentRoot);
        if (Path.IsPathRooted(depot.contentRoot))
            return Path.GetFullPath(depot.contentRoot);
        CheckRule(depot.contentRoot, "Depot 相对内容目录", false);
        return Path.GetFullPath(Path.Combine(profile.contentRoot, depot.contentRoot));
    }

    /// <summary>防止 SDK 的账号配置、工具及缓存出现在上传目录内。</summary>
    public static void ValidateSdkLocation(SteamPipeProfile profile, string sdkPath)
    {
        string sdkDirectory = Path.GetDirectoryName(Path.GetFullPath(sdkPath));
        CheckOutputSeparation(profile, sdkDirectory);
    }

    /// <summary>检查每个实际参与发布的目录均与输出目录隔离。</summary>
    private static void CheckOutputSeparation(SteamPipeProfile profile, string output)
    {
        foreach (var depot in profile.depots)
        {
            string root = DepotRoot(profile, depot);
            if (Overlaps(root, output))
                throw new InvalidDataException($"内容目录与 SDK、任务目录或缓存重叠。请分开存放。{root}");
        }
    }

    /// <summary>路径比较随宿主平台处理大小写。</summary>
    private static StringComparer PathComparer => Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>按目录边界判断路径是否位于指定目录内，包含目录自身。</summary>
    private static bool Contains(string directory, string path)
    {
        string parent = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string child = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return PathComparer.Equals(parent, child) || child.StartsWith($"{parent}{Path.DirectorySeparatorChar}",
            Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    /// <summary>检查两个目录是否相同或互为祖先。</summary>
    private static bool Overlaps(string first, string second) => Contains(first, second) || Contains(second, first);

    /// <summary>检查内容目录存在且为明确的绝对路径。</summary>
    private static string ExistingRoot(string value)
    {
        CheckText(value, "内容目录");
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathRooted(value) || !Directory.Exists(value))
            throw new InvalidDataException($"内容目录不存在，请选择游戏包目录。{value}");
        string full = Path.GetFullPath(value);
        if (PathComparer.Equals(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetPathRoot(full).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
            throw new InvalidDataException($"内容目录不能是整个磁盘或共享目录的根。{full}");
        return full;
    }

    /// <summary>Steam 标识使用无符号十进制正整数。</summary>
    private static uint CheckId(string id, string label)
    {
        if (!uint.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out uint number) || number == 0 ||
            number.ToString(CultureInfo.InvariantCulture) != id)
            throw new InvalidDataException($"{label} 应为十进制正整数。");
        return number;
    }

    /// <summary>拒绝可能退出 KeyValues 引号的控制符。</summary>
    private static void CheckText(string value, string label)
    {
        if (value == null || value.Any(ch => ch == '"' || char.IsControl(ch)))
            throw new InvalidDataException($"{label} 不能包含引号、换行或控制字符。");
    }

    /// <summary>把两种分隔符统一为可检查的路径段。</summary>
    private static string[] PathParts(string value) => value.Replace('\\', '/').Split('/');

    /// <summary>限定本地匹配和 Depot 目标均位于各自根目录内。</summary>
    private static void CheckRule(string value, string label, bool wildcards)
    {
        CheckText(value, label);
        var parts = PathParts(value);
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("/") || value.StartsWith("\\") ||
            value.IndexOf(':') >= 0 || parts.Any(part => part != "." && (part.EndsWith(".", StringComparison.Ordinal) ||
            part.EndsWith(" ", StringComparison.Ordinal) || part.Equals(".git", StringComparison.OrdinalIgnoreCase))) ||
            (!wildcards && value.IndexOfAny(new[] { '*', '?' }) >= 0))
            throw new InvalidDataException($"{label} 必须是根目录内的相对路径。{value}");
    }

    /// <summary>核对属性的相对路径及 Steam 的文件更新标志。</summary>
    private static void CheckProperty(SteamPipeFileProperty property)
    {
        if (property == null)
            throw new InvalidDataException("文件属性中包含空白条目。");
        CheckRule(property.localPath, "文件属性路径", true);
        if (!string.Equals(property.attributes, "userconfig", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(property.attributes, "versionedconfig", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("文件属性应使用 userconfig 或 versionedconfig。");
    }

    #endregion

    #region 写入脚本

    /// <summary>在本次任务目录生成包含内联 Depot 的应用脚本，返回 SteamCMD 可使用的绝对路径。</summary>
    public static string Write(SteamPipeProfile profile, string runDirectory, string cacheDirectory, bool preview, bool setLive)
    {
        Validate(profile, setLive);
        if (preview && setLive)
            throw new InvalidDataException("预览任务不能设置上线分支。");
        CheckOutputSeparation(profile, runDirectory);
        CheckOutputSeparation(profile, cacheDirectory);
        var text = new StringBuilder();
        text.AppendLine("\"AppBuild\"").AppendLine("{");
        Field(text, 1, "AppID", profile.appId);
        Field(text, 1, "Desc", profile.description ?? "");
        Field(text, 1, "ContentRoot", ScriptPath(profile.contentRoot));
        Field(text, 1, "BuildOutput", ScriptPath(Path.GetFullPath(cacheDirectory)));
        if (preview)
            Field(text, 1, "Preview", "1");
        if (setLive)
            Field(text, 1, "SetLive", profile.branch);
        text.AppendLine("\t\"Depots\"").AppendLine("\t{");
        foreach (var depot in profile.depots)
        {
            text.AppendLine($"\t\t\"{depot.id}\"").AppendLine("\t\t{");
            if (!string.IsNullOrWhiteSpace(depot.contentRoot))
                Field(text, 3, "ContentRoot", ScriptPath(DepotRoot(profile, depot)));
            foreach (var map in depot.mappings)
            {
                text.AppendLine("\t\t\t\"FileMapping\"").AppendLine("\t\t\t{");
                Field(text, 4, "LocalPath", ScriptPath(map.localPath));
                Field(text, 4, "DepotPath", ScriptPath(map.depotPath));
                Field(text, 4, "Recursive", map.recursive ? "1" : "0");
                text.AppendLine("\t\t\t}");
            }
            foreach (var filter in depot.exclusions ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(filter)) Field(text, 3, "FileExclusion", ScriptPath(filter));
            foreach (var scriptFile in depot.installScripts ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(scriptFile)) Field(text, 3, "InstallScript", ScriptPath(scriptFile));
            foreach (var property in depot.properties ?? new List<SteamPipeFileProperty>())
            {
                text.AppendLine("\t\t\t\"FileProperties\"").AppendLine("\t\t\t{");
                Field(text, 4, "LocalPath", ScriptPath(property.localPath));
                Field(text, 4, "Attributes", property.attributes);
                text.AppendLine("\t\t\t}");
            }
            text.AppendLine("\t\t}");
        }
        text.AppendLine("\t}").AppendLine("}");
        Directory.CreateDirectory(runDirectory);
        Directory.CreateDirectory(cacheDirectory);
        string script = Path.GetFullPath(Path.Combine(runDirectory, $"app_build_{profile.appId}.vdf"));
        File.WriteAllText(script, text.ToString(), new UTF8Encoding(false));
        return script;
    }

    /// <summary>使用两端通用的正斜杠写出路径。</summary>
    private static string ScriptPath(string path) => path.Replace('\\', '/');

    /// <summary>将一个已检查的键值写入当前作用域。</summary>
    private static void Field(StringBuilder text, int indentation, string key, string value)
    {
        CheckText(value, key);
        text.Append('\t', indentation).AppendLine($"\"{key}\" \"{value}\"");
    }

    #endregion

    #region 读取脚本

    /// <summary>读取已有应用脚本及其引用的 Depot 脚本，保留映射顺序和重复的排除项。</summary>
    public static SteamPipeProfile Import(string path)
    {
        path = Path.GetFullPath(path);
        Node app = ReadDocument(path, "AppBuild");
        app.Allow("AppID", "Desc", "ContentRoot", "BuildOutput", "Preview", "SetLive", "Local", "Depots");
        if (!string.IsNullOrEmpty(app.Text("Local")))
            throw new InvalidDataException("当前窗口仅支持 Steam 发布，无法导入使用 Local 内容服务器的脚本。");
        ReadBool(app.Text("Preview", "0"));
        app.Text("BuildOutput");
        string appId = app.Text("AppID");
        CheckId(appId, "App ID");
        var profile = new SteamPipeProfile
        {
            name = Path.GetFileNameWithoutExtension(path),
            appId = appId,
            contentRoot = Resolve(path, app.Text("ContentRoot", ".")),
            branch = app.Text("SetLive"),
            description = app.Text("Desc")
        };
        profile.useBeta = !string.IsNullOrWhiteSpace(profile.branch);
        profile.depots.Clear();
        var depots = app.Single("Depots");
        if (depots == null || depots.children == null || depots.children.Count == 0)
            throw new InvalidDataException("应用脚本必须包含 Depots 列表。");
        var ids = new HashSet<uint>();
        foreach (var entry in depots.children)
        {
            if (!ids.Add(CheckId(entry.key, "Depot ID")))
                throw new InvalidDataException($"Depot {entry.key} 重复。");
            string source = path;
            Node block = entry;
            if (entry.children == null)
            {
                source = Resolve(path, entry.value);
                block = ReadDocument(source, "DepotBuild");
            }
            profile.depots.Add(ImportDepot(block, entry.key, source));
        }
        return profile;
    }

    /// <summary>根据单个 Depot 的脚本位置解析目录覆盖及全部规则。</summary>
    private static SteamPipeDepot ImportDepot(Node block, string id, string source)
    {
        block.Allow("DepotID", "ContentRoot", "FileMapping", "FileExclusion", "InstallScript", "FileProperties");
        if (block.Text("DepotID", id) != id)
            throw new InvalidDataException($"引用脚本的 Depot ID 与应用脚本的 {id} 不一致。");
        string content = block.Text("ContentRoot");
        var depot = new SteamPipeDepot { id = id, contentRoot = string.IsNullOrEmpty(content) ? "" : Resolve(source, content) };
        depot.mappings.Clear();
        depot.exclusions.Clear();
        foreach (var rule in block.children)
        {
            if (rule.Is("FileExclusion"))
            {
                CheckRule(rule.value, "排除规则", true);
                depot.exclusions.Add(rule.value);
            }
            if (rule.Is("InstallScript"))
            {
                CheckRule(rule.value, "安装脚本", false);
                depot.installScripts.Add(rule.value);
            }
            if (rule.Is("FileProperties"))
            {
                rule.Allow("LocalPath", "Attributes");
                var property = new SteamPipeFileProperty { localPath = rule.Text("LocalPath"), attributes = rule.Text("Attributes") };
                CheckProperty(property);
                depot.properties.Add(property);
            }
            if (rule.Is("FileMapping"))
            {
                rule.Allow("LocalPath", "DepotPath", "Recursive");
                var mapping = new SteamPipeMapping
                {
                    localPath = rule.Text("LocalPath"),
                    depotPath = rule.Text("DepotPath"),
                    recursive = ReadBool(rule.Text("Recursive", "0"))
                };
                CheckRule(mapping.localPath, "本地映射", true);
                CheckRule(mapping.depotPath, "安装目录", false);
                depot.mappings.Add(mapping);
            }
        }
        if (depot.mappings.Count == 0)
            throw new InvalidDataException($"Depot {id} 缺少文件映射。");
        return depot;
    }

    /// <summary>按脚本所在目录解析绝对或相对的文件系统路径。</summary>
    private static string Resolve(string source, string value)
    {
        CheckText(value, "脚本路径");
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source), value.Replace('\\', Path.DirectorySeparatorChar)));
    }

    /// <summary>解析 Valve 脚本的布尔字段，无法识别的值提示用户核实。</summary>
    private static bool ReadBool(string value)
    {
        if (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
        if (value == "0" || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) return false;
        throw new InvalidDataException($"无法识别 VDF 的布尔值 {value}。");
    }

    #endregion

    #region KeyValues 语法

    private sealed class Node
    {
        public string key;
        public string value;
        public List<Node> children;

        /// <summary>KeyValues 键不区分大小写。</summary>
        public bool Is(string name) => string.Equals(name, key, StringComparison.OrdinalIgnoreCase);

        /// <summary>检测未知规则，避免导入后静默改变上传内容。</summary>
        public void Allow(params string[] keys)
        {
            if (children == null) throw new InvalidDataException($"{key} 应包含一个 VDF 块。");
            foreach (var child in children)
                if (!keys.Any(child.Is)) throw new InvalidDataException($"暂不支持导入 {key} 下的 {child.key}，请在原脚本中核对该规则。");
        }

        /// <summary>取得最多出现一次的命名项。</summary>
        public Node Single(string name)
        {
            var items = children.Where(child => child.Is(name)).Take(2).ToArray();
            if (items.Length > 1) throw new InvalidDataException($"VDF 字段 {name} 重复。");
            return items.FirstOrDefault();
        }

        /// <summary>获取简单字段，未定义时返回调用者指定的默认值。</summary>
        public string Text(string name, string fallback = "")
        {
            var item = Single(name);
            if (item == null) return fallback;
            if (item.children != null) throw new InvalidDataException($"VDF 字段 {name} 应为文本。");
            return item.value;
        }
    }

    // SteamPipe 示例中引号前的反斜杠是 Windows 目录分隔符，按原文保留。
    private static readonly Regex Tokens = new Regex(@"\G(?:\s+|//[^\r\n]*|(?<quoted>""[^""]*"")|(?<brace>[{}])|(?<word>[^\s{}""]+))");

    /// <summary>读取单个有界 VDF 配置文件，不执行任何脚本指令。</summary>
    private static Node ReadDocument(string path, string expected)
    {
        var info = new FileInfo(path);
        if (!string.Equals(info.Extension, ".vdf", StringComparison.OrdinalIgnoreCase) || !info.Exists || info.Length > 4 * 1024 * 1024)
            throw new InvalidDataException($"请选择存在的 SteamPipe VDF 文件（最多 4 MB）。{path}");
        string input = File.ReadAllText(path, Encoding.UTF8);
        var match = Tokens.Match(input);
        int offset = 0;
        var stack = new Stack<Node>();
        stack.Push(new Node { children = new List<Node>() });
        string key = null;
        while (match.Success)
        {
            offset = match.Index + match.Length;
            if (match.Groups["brace"].Success)
            {
                if (match.Value == "{")
                {
                    if (key == null || stack.Count > 12) throw new InvalidDataException("VDF 块起始位置或嵌套层数不正确。");
                    var node = new Node { key = key, children = new List<Node>() };
                    stack.Peek().children.Add(node);
                    stack.Push(node);
                    key = null;
                }
                else
                {
                    if (key != null || stack.Count <= 1) throw new InvalidDataException("VDF 块缺少字段值或结束位置不正确。");
                    stack.Pop();
                }
            }
            else if (match.Groups["quoted"].Success || match.Groups["word"].Success)
            {
                string token = match.Groups["quoted"].Success ? match.Value.Substring(1, match.Length - 2) : match.Value;
                CheckText(token, "VDF 字段");
                if (key == null)
                    key = token;
                else
                {
                    stack.Peek().children.Add(new Node { key = key, value = token });
                    key = null;
                }
            }
            match = match.NextMatch();
        }
        var root = stack.Peek();
        if (offset != input.Length || key != null || stack.Count != 1 || root.children.Count != 1 ||
            !root.children[0].Is(expected) || root.children[0].children == null)
            throw new InvalidDataException($"VDF 结构不完整或根块不是 {expected}。{path}");
        return root.children[0];
    }

    #endregion
}
