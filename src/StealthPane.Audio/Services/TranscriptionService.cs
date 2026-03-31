using Whisper.net;

namespace StealthPane.Audio.Services;

/// <summary>
/// Transcribes audio files using Whisper.net (local whisper.cpp).
/// Caches the loaded model/processor for reuse across multiple transcriptions.
/// Whisper.net automatically selects the best available runtime (CUDA > Vulkan > CPU).
/// </summary>
public sealed class TranscriptionService : IDisposable
{
    private WhisperFactory? factory;
    private WhisperProcessor? processor;
    private string? loadedModelPath;

    public async Task<string> TranscribeAsync(string wavPath, string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
        {
            return "[Error: Whisper model not found. Configure the model path in Settings > Audio.]";
        }

        if (!File.Exists(wavPath))
        {
            return "[Error: Audio file not found.]";
        }

        EnsureProcessor(modelPath);

        if (processor is null)
        {
            return "[Error: Failed to load Whisper model.]";
        }

        await using var fileStream = File.OpenRead(wavPath);
        var segments = new List<string>();

        await foreach (var segment in processor.ProcessAsync(fileStream))
        {
            if (!string.IsNullOrWhiteSpace(segment.Text))
            {
                segments.Add(segment.Text.Trim());
            }
        }

        return segments.Count > 0 ? string.Join(" ", segments) : "[No speech detected in recording.]";
    }

    public void Dispose()
    {
        processor?.Dispose();
        factory?.Dispose();
    }

    private void EnsureProcessor(string modelPath)
    {
        if (processor is not null && loadedModelPath == modelPath)
        {
            return;
        }

        processor?.Dispose();
        factory?.Dispose();

        try
        {
            factory = WhisperFactory.FromPath(modelPath);
            processor = factory.CreateBuilder()
                .WithLanguage("auto")
                .Build();
            loadedModelPath = modelPath;
        }
        catch
        {
            processor?.Dispose();
            factory?.Dispose();
            processor = null;
            factory = null;
            loadedModelPath = null;
        }
    }
}
