using System.Text.Json;
using System.Text.Json.Serialization;
using StealthPane.Models;

namespace StealthPane.Services;

[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext;

public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
        var tempPath = SettingsPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, SettingsPath, true);
    }
}
