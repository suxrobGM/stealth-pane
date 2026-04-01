using System.Text.Json;
using System.Text.Json.Serialization;
using StealthCode.Audio.Models;
using StealthCode.Models;
using StealthCode.ScreenCapture.Models;

namespace StealthCode.Services;

[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext;

public sealed class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    private Timer? saveTimer;

    public AppSettings Settings { get; } = Load();

    public void Save()
    {
        var json = JsonSerializer.Serialize(Settings, AppSettingsJsonContext.Default.AppSettings);
        var tempPath = SettingsPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, SettingsPath, true);
    }

    public void SaveDebounced()
    {
        saveTimer?.Dispose();
        saveTimer = new Timer(_ =>
        {
            Save();
            saveTimer?.Dispose();
            saveTimer = null;
        }, null, 500, Timeout.Infinite);
    }

    private static AppSettings Load()
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
}
