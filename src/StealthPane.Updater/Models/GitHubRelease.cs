namespace StealthPane.Updater.Models;

public sealed record GitHubRelease
{
    public string TagName { get; init; } = "";
    public string HtmlUrl { get; init; } = "";
    public List<GitHubAsset> Assets { get; init; } = [];
}

public sealed record GitHubAsset
{
    public string Name { get; init; } = "";
    public string BrowserDownloadUrl { get; init; } = "";
}
