using StealthPane.Models;

namespace StealthPane.Services;

public sealed class CliProviderRegistry
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

    public IReadOnlyList<CliProviderConfig> GetAllProviders()
    {
        var settings = SettingsService.Load();
        return [.. BuiltInProviders, .. settings.CustomProviders];
    }

    public CliProviderConfig? GetProvider(string id) => GetAllProviders().FirstOrDefault(p => p.Id == id);

    public CliProviderConfig GetActiveProvider()
    {
        var settings = SettingsService.Load();
        return GetProvider(settings.ActiveProviderId) ?? BuiltInProviders[0];
    }

    public void AddCustomProvider(CliProviderConfig provider)
    {
        var settings = SettingsService.Load();
        settings.CustomProviders.Add(provider);
        SettingsService.Save(settings);
    }

    public void RemoveCustomProvider(string id)
    {
        var settings = SettingsService.Load();
        settings.CustomProviders.RemoveAll(p => p.Id == id);
        SettingsService.Save(settings);
    }
}
