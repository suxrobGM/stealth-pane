namespace StealthCode.Audio.Models;

public sealed record AudioSettings
{
    public string Hotkey { get; set; } = "Shift+A";
    public string ModelPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StealthCode", "models", "ggml-base.bin");
    public string SystemPrompt { get; set; } =
        "Analyze the following meeting transcript and provide insights, action items, and key decisions:";
}
