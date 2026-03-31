using System.Text;
using StealthPane.Audio.Services;
using StealthPane.Terminal;

namespace StealthPane.Services;

/// <summary>
///     Orchestrates audio capture, transcription, and injection into the terminal.
///     Toggle pattern: the first call starts recording, the second call stops → transcribes → injects.
/// </summary>
public sealed class AudioInjectorService(
    SettingsService settingsService,
    AudioCaptureService audioCaptureService,
    TranscriptionService transcriptionService,
    PtyService pty)
{
    private static readonly byte[] Enter = "\r"u8.ToArray();

    public string? LastError => audioCaptureService.LastError;

    /// <summary>
    ///     Toggles audio recording. Returns true if recording started, false if stopped.
    ///     On stop, transcription and injection happen on a background thread.
    /// </summary>
    public bool Toggle(Action<bool>? onRecordingChanged = null)
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

        var audio = settingsService.Settings.Audio;

        Task.Run(async () =>
        {
            var transcript = await transcriptionService.TranscribeAsync(wavPath, audio.ModelPath);
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return;
            }

            var prompt = $"{audio.SystemPrompt.Trim()} \"{transcript}\"";
            pty.Write(Encoding.UTF8.GetBytes(prompt));
            await Task.Delay(500);
            pty.Write(Enter);
        });

        return false;
    }
}
