using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>将框架随附的 Valve Windows 启动器部署到当前用户的独立 SteamCMD 目录。</summary>
internal static class SteamPipeBundledTool
{
    #region 路径与版本

    // 来自 Valve 官方 CDN 的原始 ZIP，更新随框架分发的启动器时一并更新摘要。
    private const string ArchiveSha256 = "7669B170DEE42DB8EE2273775ED7DFB2D173BDBA1B849F70D2C7B379290BCE13";

    /// <summary>Unity 忽略 Tools~，窗口通过文件系统读取原始分发包。</summary>
    public static string ArchivePath => Path.Combine(Application.dataPath, "FrameWork", "Editor", "SteamPipe", "Tools~", "steamcmd.zip");

    /// <summary>SteamCMD 在仓库外的此目录自行更新并保存本机授权。</summary>
    public static string ExecutablePath => Path.Combine(SteamPipeSettings.UserDirectory, "SteamCMD", "steamcmd.exe");

    #endregion

    #region 首次部署

    /// <summary>在运行器持有安装目录锁时部署缺少的启动器，保留 Valve 自更新后的本机文件。</summary>
    public static void InstallIfMissing(string executablePath)
    {
        if (File.Exists(executablePath)) return;
        using (var source = File.OpenRead(ArchivePath))
        {
            using (var sha = SHA256.Create())
            {
                string digest = BitConverter.ToString(sha.ComputeHash(source)).Replace("-", "");
                if (!string.Equals(digest, ArchiveSha256, StringComparison.Ordinal))
                    throw new InvalidDataException("框架内置的 SteamCMD 压缩包校验失败，请重新同步框架。");
            }
            source.Position = 0;
            using (var archive = new ZipArchive(source, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry("steamcmd.exe");
                if (entry == null) throw new InvalidDataException("框架压缩包中缺少 steamcmd.exe。");
                Directory.CreateDirectory(Path.GetDirectoryName(executablePath));
                string temp = $"{executablePath}.{Guid.NewGuid():N}.tmp";
                try
                {
                    using (var input = entry.Open())
                    using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        input.CopyTo(output);
                    File.Move(temp, executablePath);
                }
                finally
                {
                    if (File.Exists(temp)) File.Delete(temp);
                }
            }
        }
    }

    #endregion
}
