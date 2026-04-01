using System.Diagnostics;
using System.Reflection;
using StealthCode.Updater.Models;

namespace StealthCode.Updater.Services;

public sealed class UpdateService : IDisposable
{
    private static readonly string LauncherDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
    private static readonly string UpdateFilePath = Path.Combine(LauncherDir, "stealthcode_update.exe");
    private static readonly string UpdateScriptPath = Path.Combine(LauncherDir, "update.cmd");

    private readonly GitHubReleaseClient releaseClient = new();
    private readonly UpdateDownloader downloader = new();

    public static string CurrentVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version?.ToString(3) ?? "0.0.0";
        }
    }

    public event Action<long, long>? DownloadProgress
    {
        add => downloader.DownloadProgress += value;
        remove => downloader.DownloadProgress -= value;
    }

    public async Task<GitHubRelease?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        var release = await releaseClient.GetLatestReleaseAsync(ct);

        if (release is null || string.IsNullOrEmpty(release.TagName))
        {
            return null;
        }

        var latestVersion = ParseVersion(release.TagName);
        var currentVersion = ParseVersion($"v{CurrentVersion}");

        if (latestVersion is null || currentVersion is null)
        {
            return null;
        }

        return latestVersion > currentVersion ? release : null;
    }

    public async Task<bool> DownloadAndApplyAsync(GitHubRelease release, CancellationToken ct = default)
    {
        var success = await downloader.DownloadAndExtractAsync(release, UpdateFilePath, ct);

        if (success)
        {
            WriteUpdateScript();
        }

        return success;
    }

    public static void LaunchUpdateAndExit()
    {
        if (!File.Exists(UpdateScriptPath))
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{UpdateScriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = LauncherDir
        };

        Process.Start(startInfo);
        Environment.Exit(0);
    }

    public void Dispose()
    {
        releaseClient.Dispose();
        downloader.Dispose();
    }

    private static void WriteUpdateScript()
    {
        var launcherPath = Path.Combine(LauncherDir, "stealthcode.exe");
        var binDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

        var script = $"""
                      @echo off
                      timeout /t 2 /nobreak >nul
                      del "{launcherPath}"
                      move "{UpdateFilePath}" "{launcherPath}"
                      rmdir /s /q "{binDir}"
                      start "" "{launcherPath}"
                      del "%~f0"
                      """;

        File.WriteAllText(UpdateScriptPath, script);
    }

    private static Version? ParseVersion(string tag)
    {
        var versionStr = tag.TrimStart('v');
        return Version.TryParse(versionStr, out var version) ? version : null;
    }
}
