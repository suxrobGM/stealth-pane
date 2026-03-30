namespace StealthPane.Models;

public enum CaptureMode
{
    FullScreen,
    Region,
    Window,
    Interactive
}

public sealed class CaptureSettings
{
    public CaptureMode Mode { get; set; } = CaptureMode.FullScreen;
    public int RegionX { get; set; }
    public int RegionY { get; set; }
    public int RegionWidth { get; set; }
    public int RegionHeight { get; set; }
    public string Hotkey { get; set; } = "Ctrl+Shift+C";
    public string SystemPrompt { get; set; } = """
        Analyze the following screenshot. Describe what you see and suggest actions or code changes based on the content.
    """;
    public string TempDirectory { get; set; } = "";
    public int AutoCleanupMinutes { get; set; } = 30;
}
