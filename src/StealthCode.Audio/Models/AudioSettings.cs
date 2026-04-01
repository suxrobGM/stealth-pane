namespace StealthCode.Audio.Models;

public sealed record AudioSettings
{
    public string Hotkey { get; set; } = "Shift+A";
    public string ModelPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StealthCode", "models", "ggml-base.bin");
    public string SystemPrompt { get; set; } =
        "Listen to the transcribed audio and answer any questions or problems concisely. For interview questions, give direct answers. For coding problems, provide the solution. Do not summarize the transcript unless asked.";
}
