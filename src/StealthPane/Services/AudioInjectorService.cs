using System.Text;
using StealthPane.Audio.Models;
using StealthPane.Terminal;

using StealthPane.Audio.Services;

namespace StealthPane.Services;

/// <summary>
/// Orchestrates audio capture, transcription, and injection into the terminal.
/// Toggle pattern: first call starts recording, second call stops → transcribes → injects.
/// </summary>
public sealed class AudioInjectorService(AudioCaptureService audioCaptureService, TranscriptionService transcriptionService, PtyService pty)
{
    private static readonly byte[] Enter = "\r"u8.ToArray();

    public bool IsRecording => audioCaptureService.IsRecording;
    public string? LastError => audioCaptureService.LastError;

    /// <summary>
    /// Toggles audio recording. Returns true if recording started, false if stopped.
    /// On stop, transcription and injection happen on a background thread.
    /// </summary>
    public bool Toggle(AudioSettings settings, Action<bool>? onRecordingChanged = null)
    {
        if (!audioCaptureService.IsRecording)
        {
            if (audioCaptureService.StartCapture())
            {
                onRecordingChanged?.Invoke(true);
                return true;
            }

            return false;
        }

        // Stop recording and process on background thread
        var wavPath = audioCaptureService.StopCapture();
        onRecordingChanged?.Invoke(false);

        if (wavPath is null)
        {
            return false;
        }

        Task.Run(async () =>
        {
            var transcript = await transcriptionService.TranscribeAsync(wavPath, settings.ModelPath);
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return;
            }

            var prompt = $"{settings.SystemPrompt.Trim()} \"{transcript}\"";
            pty.Write(Encoding.UTF8.GetBytes(prompt));
            await Task.Delay(500);
            pty.Write(Enter);
        });

        return false;
    }
}
