using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed partial class SteamPipeWindow
{
    #region 本机历史

    [NonSerialized] private int selectedRun;
    [NonSerialized] private string[] previewFiles;
    [NonSerialized] private int selectedFile;
    [NonSerialized] private int page;
    [NonSerialized] private string fileText;
    [NonSerialized] private bool hasNextPage;
    private const int LinesPerPage = 35;

    /// <summary>显示本项目的本机任务、执行目标、版本号和可重用的原始配置。</summary>
    private void DrawHistory()
    {
        if (history == null) TryAction(() => history = SteamPipeRunner.History());
        if (GUILayout.Button("刷新任务记录", GUILayout.Width(120)))
        {
            TryAction(() => history = SteamPipeRunner.History());
            ResetPreview();
        }
        if (history == null || history.Count == 0)
        {
            EditorGUILayout.LabelField("暂无本机任务记录。");
            return;
        }
        selectedRun = Mathf.Clamp(selectedRun, 0, history.Count - 1);
        int index = EditorGUILayout.Popup("本机任务", selectedRun, history.Select(run =>
            $"{LocalTime(run.utc)}   {run.action}   {run.appId}").ToArray());
        if (index != selectedRun) { selectedRun = index; ResetPreview(); }
        var entry = history[selectedRun];
        EditorGUILayout.LabelField("结果", entry.result, EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("构建账号", entry.account);
        if (!string.IsNullOrEmpty(entry.appId))
        {
            EditorGUILayout.LabelField("App ID / 分支", $"{entry.appId}  ·  {(entry.setLive ? entry.branch : "未设置上线")}");
            EditorGUILayout.LabelField("版本备注", entry.description, EditorStyles.wordWrappedLabel);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (!string.IsNullOrEmpty(entry.buildId))
            {
                EditorGUILayout.LabelField($"Build ID   {entry.buildId}", EditorStyles.boldLabel);
                if (GUILayout.Button("复制 Build ID", GUILayout.Width(104))) EditorGUIUtility.systemCopyBuffer = entry.buildId;
            }
            if (GUILayout.Button("打开任务目录", GUILayout.Width(110))) EditorUtility.RevealInFinder(entry.directory);
        }
        if (uint.TryParse(entry.appId, out uint id) && id > 0 && GUILayout.Button("查看 Steamworks 上的版本"))
            Application.OpenURL($"https://partner.steamgames.com/apps/builds/{id}");
        if (!string.IsNullOrEmpty(entry.profileJson))
        {
            using (new EditorGUI.DisabledScope(SteamPipeRunner.IsBusy))
                if (GUILayout.Button("沿用本次配置，返回上传页")) TryAction(() => RestoreRun(entry));
        }
        EditorGUILayout.Space();
        DrawFiles(entry);
    }

    /// <summary>将 UTC 时间转换为本机短日期。</summary>
    private static string LocalTime(string value)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time)
            ? time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : value;
    }

    /// <summary>从历史快照恢复配置，切回上传页供用户检查和重试。</summary>
    private void RestoreRun(SteamPipeRun entry)
    {
        var snapshot = JsonUtility.FromJson<SteamPipeProfile>(entry.profileJson);
        if (snapshot == null || snapshot.depots == null) throw new InvalidDataException("此记录没有可恢复的发布配置。");
        string original = JsonUtility.ToJson(snapshot);
        if (!(snapshot.name ?? "").EndsWith("（历史）", StringComparison.Ordinal)) snapshot.name = $"{snapshot.name}（历史）";
        string restored = JsonUtility.ToJson(snapshot);
        int existing = settings.profiles.FindIndex(p =>
        {
            string candidate = JsonUtility.ToJson(p);
            return candidate == original || candidate == restored;
        });
        if (existing >= 0) { settings.selectedProfile = existing; InspectMods(); MarkEdited(); }
        else SelectNewProfile(snapshot);
        tab = Tab.Upload;
        scroll = Vector2.zero;
        SaveChangesToDisk();
    }

    #endregion

    #region 官方文件清单与日志

    /// <summary>切换历史任务后丢弃文件列表与当前页。</summary>
    private void ResetPreview()
    {
        previewFiles = null;
        selectedFile = 0;
        page = 0;
        fileText = null;
        hasNextPage = false;
    }

    /// <summary>列出本次任务中的官方清单、诊断输出和本机控制台日志。</summary>
    private void DrawFiles(SteamPipeRun entry)
    {
        if (previewFiles == null)
        {
            try
            {
                previewFiles = Directory.GetFiles(entry.directory, "*", SearchOption.AllDirectories)
                    .Where(p => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(p => Path.GetFileName(p).IndexOf("manifest", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ThenBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
            }
            catch (IOException ex) { error = ex.Message; previewFiles = Array.Empty<string>(); }
            catch (UnauthorizedAccessException ex) { error = ex.Message; previewFiles = Array.Empty<string>(); }
        }
        if (previewFiles.Length == 0) { EditorGUILayout.LabelField("此任务暂无文本清单或日志。"); return; }
        string[] labels = previewFiles.Select(path => path.Substring(entry.directory.Length + 1)).ToArray();
        int nextFile = EditorGUILayout.Popup("清单 / 日志", selectedFile, labels);
        if (selectedFile != nextFile) { selectedFile = nextFile; page = 0; fileText = null; }
        if (fileText == null) ReadPage();
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(page == 0))
                if (GUILayout.Button("上一页", GUILayout.Width(65))) { page--; ReadPage(); }
            GUILayout.Label($"第 {page + 1} 页", GUILayout.Width(78));
            using (new EditorGUI.DisabledScope(!hasNextPage))
                if (GUILayout.Button("下一页", GUILayout.Width(65))) { page++; ReadPage(); }
            if (GUILayout.Button("定位原文件", GUILayout.Width(90))) EditorUtility.RevealInFinder(previewFiles[selectedFile]);
        }
        EditorGUILayout.TextArea(fileText ?? "", EditorStyles.textArea, GUILayout.MinHeight(100), GUILayout.ExpandHeight(true));
    }

    /// <summary>分页读取官方输出，限制单页文本量以适配 IMGUI。</summary>
    private void ReadPage()
    {
        using (var lines = ReadLogLines(previewFiles[selectedFile]).GetEnumerator())
        {
            var visible = new List<string>(LinesPerPage);
            try
            {
                for (int i = 0; i < page * LinesPerPage && lines.MoveNext(); i++) { }
                for (int i = 0; i < LinesPerPage && lines.MoveNext(); i++)
                {
                    string line = lines.Current;
                    visible.Add(line.Length > 320 ? $"{line.Substring(0, 320)}…" : line);
                }
                hasNextPage = lines.MoveNext();
                fileText = string.Join("\n", visible);
            }
            catch (Exception ex) { fileText = ex.Message; hasNextPage = false; }
        }
    }

    /// <summary>允许在 SteamCMD 仍在写入时浏览历史日志。</summary>
    private static IEnumerable<string> ReadLogLines(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var reader = new StreamReader(stream, true))
            while (reader.ReadLine() is string line) yield return line;
    }

    #endregion
}
