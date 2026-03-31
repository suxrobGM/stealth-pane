using System.Text.Json.Serialization;

namespace StealthPane.ScreenCapture.Models;

public enum CaptureMode
{
    FullScreen,
    Region,
    Window
}

public sealed record CaptureSettings
{
    public CaptureMode Mode { get; set; } = CaptureMode.FullScreen;
    public int RegionX { get; set; }
    public int RegionY { get; set; }
    public int RegionWidth { get; set; }
    public int RegionHeight { get; set; }
    public string WindowTitle { get; set; } = "";

    [JsonIgnore]
    public nint WindowHandle { get; set; }

    public string Hotkey { get; set; } = "Ctrl+Shift+C";

    public string SystemPrompt { get; set; } =
        "Analyze the following screenshot. Describe what you see and suggest actions or code changes based on the content.";
}
