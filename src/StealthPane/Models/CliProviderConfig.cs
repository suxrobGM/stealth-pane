namespace StealthPane.Models;

public sealed record CliProviderConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string[] Args { get; set; } = [];
    public bool SupportsImageInput { get; set; }
    public ImageInputMode ImageMode { get; set; }
    public string DefaultSystemPrompt { get; set; } = "";
}
