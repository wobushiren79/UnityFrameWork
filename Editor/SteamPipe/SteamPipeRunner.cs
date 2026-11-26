using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>本次 SteamCMD 操作的本机历史快照。</summary>
[Serializable]
internal sealed class SteamPipeRun
{
    #region 历史字段
    public string utc, appId, account, branch, description, action, result, buildId, directory, profileJson;
    public bool preview, setLive;
    #endregion
}

/// <summary>驱动本机 SteamCMD 并将每次操作的脚本、输出和结果保存在项目专属目录。</summary>
[InitializeOnLoad]
internal static class SteamPipeRunner
{
    #region 任务状态
    private enum Operation { Login, Verify, Preview, Upload }

    private const string Running = "进行中";
    private const int DisplayLimit = 16000;
    private static readonly Regex FinishedBuild = new Regex(
        @"(?:^|\]:\s*)Successfully finished AppID\s+(\d+)\s+build\s+(?:\(BuildID\s+(\d+)\)|(?:\(?preview\)?))", RegexOptions.IgnoreCase);
    private static readonly Regex SteamError = new Regex(
        @"(?:^|\]:\s*|\s)(?:ERROR!|ERROR\s*:|FAILED\s*\(|Failed to (?:commit|build|initialize|upload|set|log|connect)|failed ['""](?:Depot|App)Build)", RegexOptions.IgnoreCase);
    private static readonly Regex SensitiveLine = new Regex(
        @"password|passphrase|token|credential|login[_ -]?key|two[_ -]?factor|(?:guard|auth|verification)[_ -]?code|session[_ -]?id|cookie\s*[:=]|authorization\s*:|bearer\s|[?&]key=", RegexOptions.IgnoreCase);
    private static readonly Regex AuthChallenge = new Regex(
        @"enter (?:your |the )?(?:password|.*guard code)|(?:password|steam guard code):\s*$|waiting for confirmation.*(?:mobile|guard)|check your email.*guard|Invalid Password|Account Logon Denied|Account Login Denied",
        RegexOptions.IgnoreCase);
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
    private static Session current;
    private static string status = "就绪";
    private static string output = "";

    /// <summary>有后台操作或官方登录窗口尚未结束。</summary>
    public static bool IsBusy => current != null;
    /// <summary>当前阶段或最近一次任务结果。</summary>
    public static string Status => status;
    /// <summary>最近一段已脱敏的控制台输出。</summary>
    public static string Output => output;
    /// <summary>任务状态或输出变化，在编辑器主线程触发。</summary>
    public static event Action Changed;

    /// <summary>关闭窗口后仍跟踪任务，域重载或编辑器退出时结束自己启动的进程。</summary>
    static SteamPipeRunner()
    {
        EditorApplication.update += Update;
        AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
        EditorApplication.quitting += Shutdown;
    }
    #endregion

    #region 操作入口
    /// <summary>接受普通账号名及旧式邮箱账号，并阻止账号参数被解释为 SteamCMD 指令。</summary>
    public static void ValidateAccount(string account)
    {
        if (!Regex.IsMatch(account ?? "", @"\A[A-Za-z0-9_][A-Za-z0-9_@.\-]*\z") ||
            string.Equals(account, "anonymous", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("请输入获授权的 Steam 登录账号名，不能使用匿名账号、空格或命令参数。");
    }

    /// <summary>检查构建账号和工具来源，返回仓库外的运行路径，内置启动器在首次启动时部署。</summary>
    public static string ValidateSetup(string sdkPath, string account)
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
            throw new PlatformNotSupportedException("此工具使用 Windows 版 SteamCMD。");
        ValidateAccount(account);
        bool bundled = string.IsNullOrWhiteSpace(sdkPath);
        string executable = bundled ? SteamPipeBundledTool.ExecutablePath : sdkPath;
        if (!Path.IsPathRooted(executable) || !string.Equals(Path.GetFileName(executable), "steamcmd.exe", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("请在账号页选择本机的 steamcmd.exe，或恢复使用框架内置版。");
        var file = new FileInfo(executable);
        if (!file.Exists && (!bundled || !File.Exists(SteamPipeBundledTool.ArchivePath)))
            throw new FileNotFoundException(bundled ? "框架中缺少 Tools~/steamcmd.zip，请同步完整的框架版本。" : "自定义 SteamCMD 文件不存在，请重新选择或恢复内置版。");
        if (file.Exists && (file.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new IOException("请选择 SteamCMD 可执行文件的实际位置。");
        for (var parent = file.Directory; parent != null; parent = parent.Parent)
        {
            string git = Path.Combine(parent.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git) || (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0))
                throw new IOException("请将 SteamCMD 放在 Git 仓库外的实际目录，确保登录缓存仅存本机。");
        }
        return file.FullName;
    }

    /// <summary>打开官方交互式登录窗口，由 SteamCMD 自行处理密码和 Steam Guard。</summary>
    public static void Login(string sdkPath, string account)
    {
        Begin(sdkPath, account, null, Operation.Login, false);
    }

    /// <summary>只使用已有授权测试服务器登录，失效时返回需要授权。</summary>
    public static void Verify(string sdkPath, string account)
    {
        Begin(sdkPath, account, null, Operation.Verify, false);
    }

    /// <summary>根据当前配置生成本机脚本并执行预览或上传。</summary>
    public static void Start(string sdkPath, SteamPipeProfile profile, bool preview, bool setLive)
    {
        if (profile == null) throw new ArgumentException("请选择游戏配置。");
        if (preview && setLive) throw new ArgumentException("预览与上线不能同时执行。");
        Begin(sdkPath, profile.account, profile, preview ? Operation.Preview : Operation.Upload, setLive);
    }

    /// <summary>请求停止当前子进程，保留中断前的输出与可能已经分配的 Build ID。</summary>
    public static void Cancel()
    {
        if (current == null) return;
        current.Stop("已取消");
        status = "正在停止 SteamCMD";
        Changed?.Invoke();
    }
    #endregion

    #region 启动与收尾
    /// <summary>取得 SDK 独占锁，创建历史目录并启动所选操作。</summary>
    private static void Begin(string sdkPath, string account, SteamPipeProfile profile, Operation operation, bool setLive)
    {
        if (IsBusy) throw new InvalidOperationException("请先等待当前 SteamCMD 任务结束。");
        string exe = ValidateSetup(sdkPath, account);
        if (profile != null)
        {
            SteamPipeVdf.Validate(profile, setLive);
            SteamPipeVdf.ValidateSdkLocation(profile, exe);
        }
        DateTime now = DateTime.UtcNow;
        var entry = new SteamPipeRun
        {
            utc = now.ToString("o", CultureInfo.InvariantCulture), account = account,
            appId = profile?.appId ?? "", description = profile?.description ?? "",
            branch = setLive ? profile.branch : "", preview = operation == Operation.Preview, setLive = setLive,
            action = operation == Operation.Login ? "登录授权" : operation == Operation.Verify ? "验证登录" :
                operation == Operation.Preview ? "预览" : setLive ? "上传并上线到测试分支" : "仅上传",
            profileJson = profile != null ? JsonUtility.ToJson(profile) : "", result = Running, buildId = "",
            directory = Path.Combine(SteamPipeSettings.ProjectDirectory, "Runs", $"{now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}")
        };
        var session = new Session(operation, entry);
        try
        {
            session.Acquire(exe);
            if (string.IsNullOrWhiteSpace(sdkPath)) SteamPipeBundledTool.InstallIfMissing(exe);
            Directory.CreateDirectory(entry.directory);
            SaveHistory(entry);
            string args = $"+login {account} +quit";
            if (operation != Operation.Login)
            {
                string command = "+info";
                if (profile != null)
                {
                    session.BuildOutput = entry.preview ? Path.Combine(entry.directory, "SteamOutput") :
                        Path.Combine(SteamPipeSettings.ProjectDirectory, "Cache", profile.appId);
                    Directory.CreateDirectory(session.BuildOutput);
                    string script = SteamPipeVdf.Write(profile, entry.directory, session.BuildOutput, entry.preview, setLive);
                    command = $"+run_app_build \"{script}\"";
                }
                args = $"+@ShutdownOnFailedCommand 1 +@NoPromptForPassword 1 +login {account} {command} +quit";
            }
            session.Launch(exe, args);
            current = session;
            output = "";
            status = operation == Operation.Login ? "请在 SteamCMD 窗口完成登录和 Steam Guard 验证" : $"正在{entry.action}";
        }
        catch (Exception ex)
        {
            session.Stop("启动失败");
            session.Dispose();
            entry.result = $"启动失败：{ex.Message}";
            if (Directory.Exists(entry.directory)) SaveHistory(entry);
            throw;
        }
        Changed?.Invoke();
    }

    /// <summary>从主线程读取脱敏日志并处理退出，避免后台线程操作 Unity。</summary>
    private static void Update()
    {
        var session = current;
        if (session == null) return;
        if (session.ConsumeOutput(out string latest))
        {
            output = latest;
            if (!string.IsNullOrEmpty(session.Phase)) status = session.Phase;
            Changed?.Invoke();
        }
        if (session.AuthRequired)
            session.Stop("授权失效，请先在账号页重新登录");
        else if (session.Mode != Operation.Login && !session.PastLogin &&
                 DateTime.UtcNow - session.Started > TimeSpan.FromMinutes(3))
            session.Stop("SteamCMD 登录超时，请检查网络或重新授权");
        if (session.IsFinished()) Complete(session);
    }

    /// <summary>编辑器重载或退出前停止进程并记录中断状态。</summary>
    private static void Shutdown()
    {
        var session = current;
        if (session == null) return;
        if (!session.IsFinished())
        {
            session.Stop("编辑器退出或脚本重载，任务中断");
            session.WaitForStop();
        }
        Complete(session);
    }

    /// <summary>归档官方诊断、保存结果并释放 SDK 锁。</summary>
    private static void Complete(Session session)
    {
        current = null;
        var entry = session.Entry;
        try
        {
            session.ArchiveDiagnostics();
        }
        catch (Exception ex)
        {
            session.Record($"诊断归档提示：{ex.Message}");
        }
        try
        {
            entry.result = session.Result();
            session.Record(entry.result);
            session.ConsumeOutput(out output);
            SaveHistory(entry);
            status = entry.result;
        }
        catch (Exception ex)
        {
            status = $"{entry.result}（历史记录写入失败：{ex.Message}）";
        }
        finally
        {
            session.Dispose();
        }
        Changed?.Invoke();
    }
    #endregion

    #region 本机历史
    /// <summary>从独立的任务目录按新到旧读取历史，损坏的单条记录不影响其他任务。</summary>
    public static List<SteamPipeRun> History()
    {
        var runs = new List<SteamPipeRun>();
        string root = Path.Combine(SteamPipeSettings.ProjectDirectory, "Runs");
        if (!Directory.Exists(root)) return runs;
        foreach (string directory in Directory.GetDirectories(root).OrderByDescending(path => path, StringComparer.Ordinal))
        {
            string file = Path.Combine(directory, "result.json");
            if (!File.Exists(file)) continue;
            try
            {
                SteamPipeRun entry = JsonUtility.FromJson<SteamPipeRun>(File.ReadAllText(file, Utf8));
                if (entry == null || string.IsNullOrEmpty(entry.utc)) continue;
                entry.directory = directory;
                if (entry.result == Running && current?.Entry.directory != directory)
                    entry.result = "上次任务中断，远端状态待核对";
                runs.Add(entry);
            }
            catch (IOException) { }
            catch (ArgumentException) { }
        }
        return runs;
    }

    /// <summary>写完快照后再替换历史文件，确保崩溃后仍可读取前一份结果。</summary>
    private static void SaveHistory(SteamPipeRun entry)
    {
        string target = Path.Combine(entry.directory, "result.json");
        string temp = $"{target}.tmp";
        File.WriteAllText(temp, JsonUtility.ToJson(entry, true), Utf8);
        if (File.Exists(target)) File.Replace(temp, target, null);
        else File.Move(temp, target);
    }
    #endregion

    /// <summary>管理唯一子进程的句柄、日志写入和 SteamCMD 结果信号。</summary>
    private sealed class Session : IDisposable
    {
        #region 进程资源
        public readonly Operation Mode;
        public readonly SteamPipeRun Entry;
        public readonly DateTime Started = DateTime.UtcNow;
        public string BuildOutput;
        public volatile bool AuthRequired;
        public volatile bool PastLogin;
        public volatile string Phase;
        private readonly object sync = new object();
        private readonly StringBuilder tail = new StringBuilder();
        private FileStream sdkLock;
        private Process child;
        private StreamWriter log;
        private bool launched, dirty, loggedIn, previewFinished, errorSeen, branchError;
        private volatile bool stdoutDone, stderrDone;
        private string stopReason;

        /// <summary>保存本次操作的类型与参数快照。</summary>
        public Session(Operation mode, SteamPipeRun entry)
        {
            Mode = mode;
            Entry = entry;
        }

        /// <summary>进程间互斥使用同一安装，防止多个 Unity 实例覆盖其登录缓存。</summary>
        public void Acquire(string exe)
        {
            string key;
            using (var hash = SHA256.Create())
                key = BitConverter.ToString(hash.ComputeHash(Utf8.GetBytes(exe.ToUpperInvariant()))).Replace("-", "");
            string folder = Path.Combine(SteamPipeSettings.UserDirectory, "Locks");
            Directory.CreateDirectory(folder);
            try { sdkLock = new FileStream(Path.Combine(folder, $"{key}.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException ex) { throw new IOException("另一个编辑器可能正在使用这个 SteamCMD 安装，请稍后再试。", ex); }
        }

        /// <summary>登录使用可见的官方终端，自动任务使用无窗口的子进程。</summary>
        public void Launch(string exe, string args)
        {
            bool interactive = Mode == Operation.Login;
            var info = new ProcessStartInfo(exe, args)
            {
                WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = interactive, CreateNoWindow = !interactive,
                RedirectStandardInput = !interactive, RedirectStandardOutput = !interactive, RedirectStandardError = !interactive
            };
            child = new Process { StartInfo = info };
            log = new StreamWriter(Path.Combine(Entry.directory, "steamcmd.log"), false, Utf8) { AutoFlush = true };
            if (!interactive)
            {
                child.OutputDataReceived += (sender, evt) => { if (evt.Data == null) stdoutDone = true; else Record(evt.Data); };
                child.ErrorDataReceived += (sender, evt) => { if (evt.Data == null) stderrDone = true; else Record(evt.Data); };
            }
            launched = child.Start();
            if (!launched) throw new IOException("SteamCMD 启动失败。");
            if (interactive) return;
            child.StandardInput.Close();
            child.BeginOutputReadLine();
            child.BeginErrorReadLine();
        }

        /// <summary>确认进程已退出且两路输出均已读完。</summary>
        public bool IsFinished()
        {
            return launched && child.HasExited && (Mode == Operation.Login || (stdoutDone && stderrDone));
        }

        /// <summary>只结束当前工具启动的进程，保留已经出现的失败原因。</summary>
        public void Stop(string reason)
        {
            if (stopReason == null) stopReason = reason;
            if (!launched || child.HasExited) return;
            try { child.Kill(); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) when (child.HasExited) { }
        }

        /// <summary>编辑器退出前给系统时间关闭子进程与输出句柄。</summary>
        public void WaitForStop()
        {
            if (launched) child.WaitForExit(1500);
        }

        /// <summary>关闭子进程句柄、任务日志和进程间锁。</summary>
        public void Dispose()
        {
            child?.Dispose();
            lock (sync) { log?.Dispose(); log = null; }
            sdkLock?.Dispose();
        }
        #endregion

        #region 输出和结果
        /// <summary>识别 SteamCMD 的任务完成信号并保存已脱敏的输出。</summary>
        public void Record(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            lock (sync)
            {
                if (log == null) return;
                Inspect(line);
                string safe = SensitiveLine.IsMatch(line) ? "（已隐藏认证信息）" : line;
                if (safe.Length > 4096) safe = safe.Substring(0, 4096);
                try { log.WriteLine(safe); }
                catch (IOException) { /* 保留内存中的状态供任务结束时显示。 */ }
                tail.AppendLine(safe);
                if (tail.Length > DisplayLimit) tail.Remove(0, tail.Length - DisplayLimit);
                dirty = true;
            }
        }

        /// <summary>解析登录成功、预览、Build ID 和测试分支失败信号。</summary>
        private void Inspect(string line)
        {
            if (AuthChallenge.IsMatch(line)) AuthRequired = true;
            if (Regex.IsMatch(line, @"Waiting for user info[^\r\n]*\bOK\b|Logged in OK", RegexOptions.IgnoreCase))
            {
                PastLogin = loggedIn = true;
                Phase = Mode == Operation.Verify ? "正在核实登录信息" : "已登录，正在准备构建";
            }
            if (Regex.IsMatch(line, @"(?:Starting (?:build of|AppID|depot)|Building (?:depot|file list)|Scanning content)", RegexOptions.IgnoreCase))
            {
                PastLogin = true;
                Phase = "正在扫描文件并生成 Depot 清单";
            }
            else if (line.IndexOf("Uploading content", StringComparison.OrdinalIgnoreCase) >= 0)
                Phase = "正在上传文件区块";
            if (SteamError.IsMatch(line)) errorSeen = true;
            if (Entry.setLive && Regex.IsMatch(line, @"(?:fail|unable|denied|error).*(?:live|branch|commit)|(?:live|branch|commit).*(?:fail|denied|error)", RegexOptions.IgnoreCase))
                branchError = true;
            Match match = FinishedBuild.Match(line);
            if (match.Success && match.Groups[1].Value == Entry.appId)
            {
                if (match.Groups[2].Success) Entry.buildId = match.Groups[2].Value;
                else previewFinished = true;
            }
        }

        /// <summary>把后台输出快照交给主线程，同时清除变更标记。</summary>
        public bool ConsumeOutput(out string value)
        {
            lock (sync)
            {
                value = dirty ? tail.ToString() : output;
                bool changed = dirty;
                dirty = false;
                return changed;
            }
        }

        /// <summary>保留正式上传本次生成的官方日志供历史记录查看，区块缓存继续复用。</summary>
        public void ArchiveDiagnostics()
        {
            if (Mode != Operation.Upload || !Directory.Exists(BuildOutput)) return;
            string destination = Path.Combine(Entry.directory, "SteamOutput");
            foreach (string file in Directory.EnumerateFiles(BuildOutput))
            {
                string ext = Path.GetExtension(file);
                if (!ext.Equals(".log", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".txt", StringComparison.OrdinalIgnoreCase)) continue;
                if (File.GetLastWriteTimeUtc(file) < Started) continue;
                Directory.CreateDirectory(destination);
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
            }
        }

        /// <summary>将进程退出与 Valve 成功信号合并判断，保留部分成功和无法确认的情况。</summary>
        public string Result()
        {
            lock (sync)
            {
                if (stopReason != null)
                    return string.IsNullOrEmpty(Entry.buildId) ? stopReason : $"{stopReason}，已创建 Build {Entry.buildId}，请核对远端状态";
                bool failed = errorSeen || child.ExitCode != 0;
                if (Mode == Operation.Login)
                    return failed ? "登录窗口异常退出，请重新授权" : "登录窗口已结束，可点击「验证登录」核实授权";
                if (Mode == Operation.Verify) return !failed && loggedIn ? "登录验证成功" : "未确认登录，请检查日志并重新授权";
                if (Mode == Operation.Preview) return !failed && previewFinished ? "预览完成" : "未确认预览成功，请检查 SteamCMD 日志";
                if (Entry.setLive && branchError && string.IsNullOrEmpty(Entry.buildId))
                    return "测试分支上线失败，Build ID 未确认，请核对 Steamworks 版本页";
                if (string.IsNullOrEmpty(Entry.buildId)) return failed ? "上传失败，请检查诊断日志" : "未收到 Build ID，请核对 Steamworks 版本页";
                if (Entry.setLive && (failed || branchError)) return $"已创建 Build {Entry.buildId}，测试分支上线待核对";
                return failed ? $"已创建 Build {Entry.buildId}，SteamCMD 报错，请核对版本" :
                    Entry.setLive ? $"已上传 Build {Entry.buildId}，已请求上线到 {Entry.branch}" : $"上传成功，Build ID {Entry.buildId}";
            }
        }
        #endregion
    }
}
