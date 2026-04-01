using System.Text.Json;
using System.Text.Json.Serialization;
using StealthCode.Updater.Models;

namespace StealthCode.Updater.Services;

[JsonSerializable(typeof(GitHubRelease))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
// ReSharper disable once PartialTypeWithSinglePart
internal sealed partial class GitHubReleaseJsonContext : JsonSerializerContext;

public sealed class GitHubReleaseClient : IDisposable
{
    private const string GitHubOwner = "suxrobGM";
    private const string GitHubRepo = "stealth-pane";

    private readonly HttpClient http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders =
        {
            { "User-Agent", "StealthCode-Updater" },
            { "Accept", "application/vnd.github+json" }
        }
    };

    public void Dispose()
    {
        http.Dispose();
    }

    public async Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken ct = default)
    {
        try
        {
            const string url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
            using var response = await http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize(json, GitHubReleaseJsonContext.Default.GitHubRelease);
        }
        catch
        {
            return null;
        }
    }
}
