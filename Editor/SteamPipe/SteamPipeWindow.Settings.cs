using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed partial class SteamPipeWindow
{
    #region 账号和工具

    /// <summary>管理工具路径、注册在本机的账号以及官方交互登录。</summary>
    private void DrawAccounts()
    {
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        settings.sdkPath = PathField("SteamCMD", settings.sdkPath, true);
        DrawAccountPicker();
        if (EditorGUI.EndChangeCheck()) MarkEdited();
        using (new EditorGUILayout.HorizontalScope())
        {
            newAccount = EditorGUILayout.TextField("添加构建账号", newAccount ?? "");
            if (GUILayout.Button("添加", GUILayout.Width(62))) TryAction(AddAccount);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("登录 / 重新授权")) TryAction(() => AccountAction(true));
            if (GUILayout.Button("验证登录状态")) TryAction(() => AccountAction(false));
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(Profile.account)))
                if (GUILayout.Button("移除账号记录")) TryAction(RemoveAccount);
        }
        EditorGUILayout.HelpBox("SteamCMD 首次运行时会联网更新。点击「登录 / 重新授权」后，请在 Valve 的窗口中完成 Steam Guard 验证。", MessageType.Info);
        if (GUILayout.Button("打开本机配置目录"))
            TryAction(() => { Directory.CreateDirectory(SteamPipeSettings.ProjectDirectory); EditorUtility.RevealInFinder(SteamPipeSettings.ProjectDirectory); });
    }

    /// <summary>选择当前发布配置对应的本机账号。</summary>
    private void DrawAccountPicker()
    {
        var names = new List<string> { "选择账号…" };
        names.AddRange(settings.accounts);
        int index = settings.accounts.FindIndex(n => string.Equals(n, Profile.account, StringComparison.OrdinalIgnoreCase)) + 1;
        int next = EditorGUILayout.Popup("构建账号", index, names.ToArray());
        if (next != index) Profile.account = next > 0 ? settings.accounts[next - 1] : "";
    }

    /// <summary>登记账号并选作当前游戏的构建账号。</summary>
    private void AddAccount()
    {
        string account = (newAccount ?? "").Trim();
        SteamPipeRunner.ValidateAccount(account);
        string existing = settings.accounts.FirstOrDefault(n => string.Equals(n, account, StringComparison.OrdinalIgnoreCase));
        if (existing == null) settings.accounts.Add(account);
        Profile.account = existing ?? account;
        newAccount = "";
        MarkEdited();
        SaveChangesToDisk();
    }

    /// <summary>从工具的账号列表中移除记录并解除本项目的关联。</summary>
    private void RemoveAccount()
    {
        string account = Profile.account;
        if (!EditorUtility.DisplayDialog("移除本机账号记录", $"从工具的账号列表移除 {account}？SteamCMD 的设备授权由 Steam 管理。", "移除", "取消")) return;
        settings.accounts.RemoveAll(n => string.Equals(n, account, StringComparison.OrdinalIgnoreCase));
        foreach (var profile in settings.profiles)
            if (string.Equals(profile.account, account, StringComparison.OrdinalIgnoreCase)) profile.account = "";
        MarkEdited();
        SaveChangesToDisk();
    }

    /// <summary>检查关联账号仍在本机可选列表中。</summary>
    private void EnsureAccount()
    {
        if (!settings.accounts.Any(n => string.Equals(n, Profile.account, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("请先在「账号与工具」添加并选择构建账号。");
    }

    /// <summary>向官方工具发起交互登录或独立的会话验证。</summary>
    private void AccountAction(bool interactive)
    {
        SaveChangesToDisk();
        EnsureAccount();
        if (interactive) SteamPipeRunner.Login(settings.sdkPath, Profile.account);
        else SteamPipeRunner.Verify(settings.sdkPath, Profile.account);
        wasBusy = SteamPipeRunner.IsBusy;
    }

    /// <summary>选择内容目录或本机 SteamCMD；工具路径留空时显示框架内置来源。</summary>
    private static string PathField(string label, string path, bool executable)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            string value = path ?? "";
            if (executable && string.IsNullOrWhiteSpace(value))
                EditorGUILayout.LabelField(label, "框架内置（Windows）");
            else
                value = EditorGUILayout.TextField(label, value);
            if (executable && !string.IsNullOrWhiteSpace(value) && GUILayout.Button("恢复内置", GUILayout.Width(72)))
            {
                GUI.changed = true;
                return "";
            }
            if (!GUILayout.Button(executable ? "选择本机文件…" : "浏览…", GUILayout.Width(executable ? 108 : 60))) return value;
            string initial = "";
            if (Directory.Exists(value)) initial = value;
            else if (File.Exists(value)) initial = Path.GetDirectoryName(value);
            string selected = executable ? EditorUtility.OpenFilePanel(label, initial, "exe") : EditorUtility.OpenFolderPanel(label, initial, "");
            if (string.IsNullOrEmpty(selected)) return value;
            GUI.changed = true;
            return selected;
        }
    }

    #endregion

    #region 发布配置和文件映射

    /// <summary>编辑游戏配置及组成 Steam 构建的各个 Depot。</summary>
    private void DrawProfiles()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("新建配置")) SelectNewProfile(new SteamPipeProfile { name = "新游戏" });
            if (GUILayout.Button("复制配置"))
            {
                var copy = JsonUtility.FromJson<SteamPipeProfile>(JsonUtility.ToJson(Profile));
                copy.name = $"{Profile.name} 副本";
                SelectNewProfile(copy);
            }
            if (GUILayout.Button("导入 App VDF…")) TryAction(ImportProfile);
            using (new EditorGUI.DisabledScope(settings.profiles.Count < 2))
                if (GUILayout.Button("删除当前配置")) RemoveProfile();
        }
        var profile = Profile;
        EditorGUI.BeginChangeCheck();
        profile.name = EditorGUILayout.TextField("配置名称", profile.name ?? "");
        profile.appId = EditorGUILayout.TextField("Steam App ID", profile.appId ?? "");
        DrawAccountPicker();
        profile.branch = EditorGUILayout.TextField("测试分支名称", profile.branch ?? "");
        EditorGUILayout.Space();
        for (int i = 0; i < profile.depots.Count; i++)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Depot {i + 1}", EditorStyles.boldLabel);
                    if (GUILayout.Button("移除 Depot", GUILayout.Width(90)))
                    {
                        profile.depots.RemoveAt(i);
                        MarkEdited();
                        break;
                    }
                }
                DrawDepot(profile.depots[i]);
            }
        }
        if (GUILayout.Button("添加 Depot")) { profile.depots.Add(new SteamPipeDepot()); MarkEdited(); }
        if (EditorGUI.EndChangeCheck()) MarkEdited();
    }

    /// <summary>绘制一个 Depot 的目录覆盖、文件映射和逐行排除规则。</summary>
    private void DrawDepot(SteamPipeDepot depot)
    {
        depot.id = EditorGUILayout.TextField("Depot ID", depot.id ?? "");
        depot.contentRoot = PathField("内容目录覆盖（可留空）", depot.contentRoot, false);
        EditorGUILayout.LabelField("文件映射", EditorStyles.boldLabel);
        for (int i = 0; i < depot.mappings.Count; i++)
        {
            var mapping = depot.mappings[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                mapping.localPath = EditorGUILayout.TextField(new GUIContent("", "本地文件或通配符"), mapping.localPath ?? "");
                GUILayout.Label("→", GUILayout.Width(16));
                mapping.depotPath = EditorGUILayout.TextField(new GUIContent("", "Depot 目标目录"), mapping.depotPath ?? "");
                mapping.recursive = GUILayout.Toggle(mapping.recursive, "递归", GUILayout.Width(54));
                if (GUILayout.Button("移除", GUILayout.Width(44))) { depot.mappings.RemoveAt(i); MarkEdited(); break; }
            }
        }
        if (GUILayout.Button("添加映射", GUILayout.Width(94))) { depot.mappings.Add(new SteamPipeMapping()); MarkEdited(); }
        EditorGUILayout.LabelField("排除规则（每行一条）");
        string old = string.Join("\n", depot.exclusions);
        string edited = EditorGUILayout.TextArea(old, GUILayout.MinHeight(50));
        if (old != edited) depot.exclusions = edited.Replace("\r", "").Split('\n').ToList();
        DrawAdvancedDepot(depot);
    }

    [NonSerialized] private HashSet<SteamPipeDepot> expandedDepots;

    /// <summary>按需编辑 Depot 的安装脚本及 Steam 对用户配置文件的更新策略。</summary>
    private void DrawAdvancedDepot(SteamPipeDepot depot)
    {
        if (expandedDepots == null) expandedDepots = new HashSet<SteamPipeDepot>();
        string title = $"安装脚本与文件属性（{depot.installScripts.Count} / {depot.properties.Count}）";
        if (!EditorGUILayout.Foldout(expandedDepots.Contains(depot), title, true)) { expandedDepots.Remove(depot); return; }
        expandedDepots.Add(depot);
        EditorGUILayout.LabelField("安装脚本（相对路径）", EditorStyles.boldLabel);
        for (int i = 0; i < depot.installScripts.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                depot.installScripts[i] = EditorGUILayout.TextField(depot.installScripts[i]);
                if (GUILayout.Button("移除", GUILayout.Width(44))) { depot.installScripts.RemoveAt(i); MarkEdited(); break; }
            }
        }
        if (GUILayout.Button("添加安装脚本", GUILayout.Width(108))) { depot.installScripts.Add(""); MarkEdited(); }
        EditorGUILayout.LabelField("文件属性（本地路径 / 更新策略）", EditorStyles.boldLabel);
        string[] attributes = { "userconfig", "versionedconfig" };
        for (int i = 0; i < depot.properties.Count; i++)
        {
            var property = depot.properties[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                property.localPath = EditorGUILayout.TextField(property.localPath ?? "");
                int choice = string.Equals(property.attributes, attributes[1], StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                int updated = EditorGUILayout.Popup(choice, attributes, GUILayout.Width(140));
                if (updated != choice) property.attributes = attributes[updated];
                if (GUILayout.Button("移除", GUILayout.Width(44))) { depot.properties.RemoveAt(i); MarkEdited(); break; }
            }
        }
        if (GUILayout.Button("添加文件属性", GUILayout.Width(108)))
        {
            depot.properties.Add(new SteamPipeFileProperty { localPath = "", attributes = "userconfig" });
            MarkEdited();
        }
    }

    /// <summary>选中新建或导入的配置并更新界面和本机存储。</summary>
    private void SelectNewProfile(SteamPipeProfile profile)
    {
        settings.profiles.Add(profile);
        settings.selectedProfile = settings.profiles.Count - 1;
        InspectMods();
        MarkEdited();
    }

    /// <summary>导入本机 AppBuild VDF 并保留当前账号选择。</summary>
    private void ImportProfile()
    {
        string path = EditorUtility.OpenFilePanel("导入 Steam AppBuild VDF", "", "vdf");
        if (string.IsNullOrEmpty(path)) return;
        var imported = SteamPipeVdf.Import(path);
        imported.account = Profile.account;
        SelectNewProfile(imported);
        SaveChangesToDisk();
    }

    /// <summary>删除当前项目的一套命名配置。</summary>
    private void RemoveProfile()
    {
        if (!EditorUtility.DisplayDialog("删除本机配置", $"删除「{Profile.name}」？", "删除", "取消")) return;
        settings.profiles.RemoveAt(settings.selectedProfile);
        settings.selectedProfile = Mathf.Min(settings.selectedProfile, settings.profiles.Count - 1);
        InspectMods();
        MarkEdited();
    }

    #endregion
}
