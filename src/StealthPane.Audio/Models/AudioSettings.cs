namespace StealthPane.Audio.Models;

public sealed record AudioSettings
{
    public string Hotkey { get; set; } = "Ctrl+Shift+A";
    public string ModelPath { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "ggml-base.bin");
    public string SystemPrompt { get; set; } =
        "Analyze the following meeting transcript and provide insights, action items, and key decisions:";
}
