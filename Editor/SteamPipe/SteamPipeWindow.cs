using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>管理本机 SteamPipe 账号、游戏发布配置和构建记录的独立编辑器窗口。</summary>
public sealed partial class SteamPipeWindow : EditorWindow
{
    #region 状态与项目接口

    /// <summary>游戏项目可注册构建回调，接收全新输出目录，完成所有后处理后返回游戏目录。</summary>
    public static Func<string, string> BuildContent { get; set; }

    private enum Tab { Upload, Profiles, Accounts, History }
    private static readonly string[] Tabs = { "上传", "游戏与 Depot", "账号与工具", "历史与日志" };
    [NonSerialized] private Tab tab;
    [NonSerialized] private SteamPipeSettings settings;
    [NonSerialized] private string error;
    [NonSerialized] private bool dirty;
    [NonSerialized] private double saveAt;
    [NonSerialized] private Vector2 scroll;
    [NonSerialized] private string newAccount;
    [NonSerialized] private string modSummary;
    [NonSerialized] private string incompleteBuild;
    [NonSerialized] private List<SteamPipeRun> history;
    [NonSerialized] private bool wasBusy;

    /// <summary>返回当前项目选中的发布配置。</summary>
    private SteamPipeProfile Profile => settings.profiles[settings.selectedProfile];

    #endregion

    #region 窗口生命周期

    /// <summary>从游戏菜单打开 SteamPipe。</summary>
    [MenuItem("游戏/SteamPipe")]
    public static void ShowWindow()
    {
        GetWindow<SteamPipeWindow>("SteamPipe").minSize = new Vector2(580, 430);
    }

    /// <summary>载入本机配置并监听正在运行的后台任务。</summary>
    private void OnEnable()
    {
        minSize = new Vector2(580, 430);
        TryAction(LoadSettings);
        wasBusy = SteamPipeRunner.IsBusy;
        SteamPipeRunner.Changed += OnRunnerChanged;
        EditorApplication.update += FlushAfterEdit;
    }

    /// <summary>关闭窗口时保存尚未写入的编辑，进行中的上传由运行器继续管理。</summary>
    private void OnDisable()
    {
        EditorApplication.update -= FlushAfterEdit;
        SteamPipeRunner.Changed -= OnRunnerChanged;
        if (dirty)
        {
            try { SaveChangesToDisk(); }
            catch (Exception ex) { Debug.LogError($"SteamPipe 本机配置保存失败：{ex.Message}"); }
        }
    }

    /// <summary>在主线程更新界面，任务结束后重新加载历史。</summary>
    private void OnRunnerChanged()
    {
        if (wasBusy && !SteamPipeRunner.IsBusy)
        {
            history = null;
            selectedRun = 0;
            ResetPreview();
        }
        wasBusy = SteamPipeRunner.IsBusy;
        Repaint();
    }

    /// <summary>还原编辑环境，读失败时保留原文件供用户检查。</summary>
    private void LoadSettings()
    {
        settings = SteamPipeSettings.Load();
        if (settings.profiles.Count == 0)
            settings.profiles.Add(new SteamPipeProfile { name = "新游戏" });
        settings.selectedProfile = Mathf.Clamp(settings.selectedProfile, 0, settings.profiles.Count - 1);
        InspectMods();
    }

    /// <summary>延迟写盘以合并连续键盘输入。</summary>
    private void MarkEdited()
    {
        dirty = true;
        saveAt = EditorApplication.timeSinceStartup + 0.6;
    }

    /// <summary>持久化已变更的配置。</summary>
    private void SaveChangesToDisk()
    {
        if (!dirty) return;
        settings.Save();
        dirty = false;
    }

    /// <summary>在用户结束编辑后保存，失败时可从窗口手动重试。</summary>
    private void FlushAfterEdit()
    {
        if (!dirty || EditorApplication.timeSinceStartup < saveAt) return;
        saveAt = double.PositiveInfinity;
        TryAction(SaveChangesToDisk);
    }

    /// <summary>将可恢复错误显示在当前窗口。</summary>
    private void TryAction(Action action)
    {
        try { error = null; action(); }
        catch (ExitGUIException) { throw; }
        catch (Exception ex) { error = ex.Message; }
    }

    #endregion

    #region 导航与上传

    /// <summary>绘制配置选择、页签、状态和任务操作。</summary>
    private void OnGUI()
    {
        if (settings == null)
        {
            EditorGUILayout.HelpBox(error ?? "无法载入本机配置。", MessageType.Error);
            if (GUILayout.Button("重新读取本机配置")) TryAction(LoadSettings);
            return;
        }

        var profile = Profile;
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(SteamPipeRunner.IsBusy))
        {
            int index = EditorGUILayout.Popup("当前游戏配置", settings.selectedProfile,
                settings.profiles.Select(p => $"{p.name}   ·   {p.appId}").ToArray());
            if (index != settings.selectedProfile)
            {
                settings.selectedProfile = index;
                profile = Profile;
                MarkEdited();
                InspectMods();
            }
        }
        string account = settings.accounts.Any(n => string.Equals(n, profile.account, StringComparison.OrdinalIgnoreCase))
            ? profile.account : "未选择";
        string destination = profile.useBeta ? $"测试分支 {profile.branch}" : "仅创建 Build";
        EditorGUILayout.LabelField($"账号  {account}      目标  {destination}", EditorStyles.miniLabel);
        var nextTab = (Tab)GUILayout.Toolbar((int)tab, Tabs);
        if (nextTab != tab) { tab = nextTab; scroll = Vector2.zero; }
        if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        if (dirty && double.IsPositiveInfinity(saveAt) && GUILayout.Button("重试保存本机配置"))
            TryAction(SaveChangesToDisk);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        using (new EditorGUI.DisabledScope(SteamPipeRunner.IsBusy && tab != Tab.History))
        {
            switch (tab)
            {
                case Tab.Upload: DrawUpload(); break;
                case Tab.Profiles: DrawProfiles(); break;
                case Tab.Accounts: DrawAccounts(); break;
                case Tab.History: DrawHistory(); break;
            }
        }
        EditorGUILayout.EndScrollView();
        DrawStatus();
    }

    /// <summary>绘制已构建目录、版本备注、目标和三种执行方式。</summary>
    private void DrawUpload()
    {
        var profile = Profile;
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        string root = PathField("游戏包目录", profile.contentRoot, false);
        if (root != profile.contentRoot) { profile.contentRoot = root; InspectMods(); }
        profile.description = EditorGUILayout.TextField("版本备注", profile.description ?? "");
        profile.useBeta = EditorGUILayout.Toggle("上传后上线测试分支", profile.useBeta);
        if (profile.useBeta)
            profile.branch = EditorGUILayout.TextField("已有测试分支名称", profile.branch ?? "");
        if (EditorGUI.EndChangeCheck()) MarkEdited();
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"目录内的 Mods   {modSummary}", EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("刷新", GUILayout.Width(50))) InspectMods();
        }
        EditorGUILayout.Space();
        string upload = profile.useBeta ? "上传并上线到测试分支" : "仅上传";
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("预览上传内容", GUILayout.Height(32))) TryAction(() => Submit(true, false));
            if (GUILayout.Button(upload, GUILayout.Height(32))) TryAction(() => Submit(false, false));
        }
        if (BuildContent != null && GUILayout.Button("打包并上传", GUILayout.Height(26)))
            TryAction(() => Submit(false, true));
        if (!string.IsNullOrEmpty(incompleteBuild) && Directory.Exists(incompleteBuild) && GUILayout.Button("定位上次未完成的构建"))
            EditorUtility.RevealInFinder(incompleteBuild);
        if (string.IsNullOrWhiteSpace(profile.appId) || !settings.accounts.Contains(profile.account, StringComparer.OrdinalIgnoreCase))
            EditorGUILayout.HelpBox("首次使用请在「账号与工具」添加构建账号并登录，然后在「游戏与 Depot」填写 App ID 和 Depot ID。", MessageType.Info);
        if (uint.TryParse(profile.appId, out uint id) && id > 0 && GUILayout.Button("打开 Steamworks 版本页"))
            Application.OpenURL($"https://partner.steamgames.com/apps/builds/{id}");
    }

    /// <summary>核对发布目标并启动任务，新打包任务使用独立的空目录。</summary>
    private void Submit(bool preview, bool build)
    {
        SaveChangesToDisk();
        var profile = Profile;
        bool live = !preview && profile.useBeta;
        EnsureAccount();
        SteamPipeRunner.ValidateSetup(settings.sdkPath, profile.account);
        if (build) SteamPipeVdf.ValidateRules(profile, live);
        if (build && profile.depots.Any(d => !string.IsNullOrWhiteSpace(d.contentRoot) && Path.IsPathRooted(d.contentRoot)))
            throw new InvalidOperationException("打包并上传时 Depot 目录请留空或使用相对于新游戏包的子目录。上传固定目录的现有包可使用「仅上传」。");
        if (!build) SteamPipeVdf.Validate(profile, live);
        string target = live ? $"上线到测试分支 {profile.branch}" : "仅创建 Steam Build";
        if (!preview && !EditorUtility.DisplayDialog("确认 SteamPipe 上传",
            $"游戏：{profile.name}  ·  App {profile.appId}\n账号：{profile.account}\n目标：{target}\n内容：{(build ? "按本项目打包设置构建到全新目录" : profile.contentRoot)}",
            build ? "打包并上传" : "上传", "取消")) return;
        if (build)
        {
            string buildRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UnitySteamPipeBuilds", Path.GetFileName(SteamPipeSettings.ProjectDirectory),
                $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(buildRoot);
            try
            {
                string built = BuildContent(buildRoot);
                if (string.IsNullOrEmpty(built) || !Directory.Exists(built))
                    throw new IOException("构建未返回有效的游戏包目录。");
                profile.contentRoot = built;
                incompleteBuild = null;
            }
            catch (Exception ex) when (!(ex is ExitGUIException))
            {
                incompleteBuild = buildRoot;
                throw new IOException($"{ex.Message}\n本次构建目录：{buildRoot}", ex);
            }
            MarkEdited();
            InspectMods();
            SaveChangesToDisk();
        }
        SteamPipeRunner.Start(settings.sdkPath, profile, preview, live);
        wasBusy = SteamPipeRunner.IsBusy;
    }

    /// <summary>列出包中实际存在的一级 Mod 目录，最终 Depot 内容见官方预览。</summary>
    private void InspectMods()
    {
        modSummary = "无";
        if (string.IsNullOrWhiteSpace(Profile.contentRoot)) return;
        try
        {
            string path = Path.Combine(Profile.contentRoot, "Mods");
            if (!Directory.Exists(path)) return;
            var names = Directory.GetDirectories(path).Select(Path.GetFileName).OrderBy(n => n).ToArray();
            if (names.Length > 0) modSummary = string.Join("、", names);
        }
        catch (Exception ex) { modSummary = ex.Message; }
    }

    /// <summary>显示后台任务结果和实时输出，窗口关闭后任务仍可完成。</summary>
    private void DrawStatus()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(SteamPipeRunner.Status ?? "就绪", EditorStyles.wordWrappedLabel);
            if (SteamPipeRunner.IsBusy && GUILayout.Button("取消任务", GUILayout.Width(76)))
                TryAction(SteamPipeRunner.Cancel);
            if (!SteamPipeRunner.IsBusy && tab != Tab.History && !string.IsNullOrEmpty(SteamPipeRunner.Output)
                && GUILayout.Button("查看清单与日志", GUILayout.Width(114)))
            {
                tab = Tab.History;
                history = null;
                selectedRun = 0;
                ResetPreview();
                scroll = Vector2.zero;
            }
        }
        if (tab == Tab.Upload && !string.IsNullOrEmpty(SteamPipeRunner.Output))
        {
            string output = SteamPipeRunner.Output;
            int start = Math.Max(0, output.Length - 520);
            EditorGUILayout.SelectableLabel(output.Substring(start).Trim(), EditorStyles.wordWrappedMiniLabel, GUILayout.Height(62));
        }
    }

    #endregion
}
