using System.Text.Json.Serialization;

namespace StealthCode.ScreenCapture.Models;

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

    public string MultiCaptureHotkey { get; set; } = "Ctrl+Shift+X";

    public string SystemPrompt { get; set; } =
        "Analyze the screenshot and answer any visible questions or problems concisely. For interview questions, give direct answers. For coding problems, provide the solution. Do not describe the screenshot itself unless asked.";

    public string MultiCaptureSystemPrompt { get; set; } =
        "The following screenshots show consecutive parts of the same scrollable page. They may contain overlapping regions from scrolling. Treat them as one continuous document, ignore duplicate content, and answer any visible questions or problems concisely";
}
