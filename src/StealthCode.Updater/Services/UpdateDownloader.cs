using System.IO.Compression;
using StealthCode.Updater.Models;

namespace StealthCode.Updater.Services;

public sealed class UpdateDownloader : IDisposable
{
    private const string AssetPattern = "StealthCode-";

    private readonly HttpClient http = new()
    {
        Timeout = TimeSpan.FromMinutes(10),
        DefaultRequestHeaders = { { "User-Agent", "StealthCode-Updater" } }
    };

    private CancellationTokenSource? activeCts;

    public event Action<long, long>? DownloadProgress;

    public async Task<bool> DownloadAndExtractAsync(GitHubRelease release, string outputPath, CancellationToken ct = default)
    {
        activeCts?.Cancel();
        activeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = activeCts.Token;

        try
        {
            var asset = release.Assets.Find(a =>
                a.Name.StartsWith(AssetPattern, StringComparison.OrdinalIgnoreCase)
                && a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            if (asset is null)
            {
                return false;
            }

            var tempZip = Path.Combine(Path.GetTempPath(), asset.Name);

            // Download the zip with progress reporting
            using (var response = await http.SendAsync(
                       new HttpRequestMessage(HttpMethod.Get, asset.BrowserDownloadUrl),
                       HttpCompletionOption.ResponseHeadersRead, token))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1;

                await using var contentStream = await response.Content.ReadAsStreamAsync(token);
                await using var fileStream = File.Create(tempZip);
                var buffer = new byte[81920];
                long downloaded = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, token)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                    downloaded += bytesRead;
                    DownloadProgress?.Invoke(downloaded, totalBytes);
                }
            }

            // Extract the launcher exe from the zip
            using (var archive = ZipFile.OpenRead(tempZip))
            {
                var launcherEntry = archive.Entries.FirstOrDefault(e =>
                    e.Name.Equals("stealthcode.exe", StringComparison.OrdinalIgnoreCase));

                if (launcherEntry is null)
                {
                    return false;
                }

                await using var entryStream = launcherEntry.Open();
                await using var outStream = File.Create(outputPath);
                await entryStream.CopyToAsync(outStream, token);
            }

            File.Delete(tempZip);
            return true;
        }
        catch
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            return false;
        }
    }

    public void Dispose()
    {
        activeCts?.Cancel();
        activeCts?.Dispose();
        http.Dispose();
    }
}
