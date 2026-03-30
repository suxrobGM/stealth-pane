namespace StealthPane.Models;

public sealed class AppSettings
{
    public string ActiveProviderId { get; set; } = "claude-code";
    public double WindowOpacity { get; set; } = 1.0;
    public bool AlwaysOnTop { get; set; }
    public CaptureSettings Capture { get; set; } = new();
    public List<CliProviderConfig> CustomProviders { get; set; } = [];
}
