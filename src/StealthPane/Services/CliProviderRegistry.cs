using StealthPane.Models;
using StealthPane.ScreenCapture.Models;

namespace StealthPane.Services;

/// <summary>
/// Manages the registry of CLI providers, including built-in and user-defined ones.
/// It allows retrieval of all providers, fetching a specific provider by ID, and managing the active provider based on user settings.
/// Custom providers can be added or removed, with changes persisted to the application settings.
/// </summary>
public static class CliProviderRegistry
{
    private static readonly List<CliProviderConfig> BuiltInProviders =
    [
        new()
        {
            Id = "claude-code",
            Name = "Claude Code",
            Command = "claude",
            Args = [],
            SupportsImageInput = true,
            ImageMode = ImageInputMode.FilePath,
            DefaultSystemPrompt = "Analyze the following screenshot. Describe what you see and suggest actions or code changes based on the content."
        },
        new()
        {
            Id = "codex",
            Name = "Codex",
            Command = "codex",
            Args = [],
            SupportsImageInput = false,
            ImageMode = ImageInputMode.FilePath,
            DefaultSystemPrompt = "Analyze the following screenshot and describe what you see."
        },
        new()
        {
            Id = "gemini-cli",
            Name = "Gemini CLI",
            Command = "gemini",
            Args = [],
            SupportsImageInput = false,
            ImageMode = ImageInputMode.FilePath,
            DefaultSystemPrompt = "Analyze the following screenshot and describe what you see."
        }
    ];

    public static IReadOnlyList<CliProviderConfig> GetAllProviders()
    {
        var settings = SettingsService.Load();
        return [.. BuiltInProviders, .. settings.CustomProviders];
    }

    public static CliProviderConfig? GetProvider(string id) => GetAllProviders().FirstOrDefault(p => p.Id == id);

    public static CliProviderConfig GetActiveProvider()
    {
        var settings = SettingsService.Load();
        return GetProvider(settings.ActiveProviderId) ?? BuiltInProviders[0];
    }

    public static void AddCustomProvider(CliProviderConfig provider)
    {
        var settings = SettingsService.Load();
        settings.CustomProviders.Add(provider);
        SettingsService.Save(settings);
    }

    public static void RemoveCustomProvider(string id)
    {
        var settings = SettingsService.Load();
        settings.CustomProviders.RemoveAll(p => p.Id == id);
        SettingsService.Save(settings);
    }
}
