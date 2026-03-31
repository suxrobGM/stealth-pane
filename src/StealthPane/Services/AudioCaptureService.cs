using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace StealthPane.Services;

/// <summary>
/// Captures system audio (loopback) via NAudio's WasapiLoopbackCapture.
/// Records what's playing through speakers — ideal for capturing meeting audio.
/// </summary>
public sealed class AudioCaptureService : IDisposable
{
    private WasapiLoopbackCapture? capture;
    private MemoryStream? capturedPcm;
    private WaveFormat? captureFormat;

    public bool IsRecording => capture is not null;
    public string? LastError { get; private set; }

    public bool StartCapture()
    {
        if (capture is not null)
        {
            return false;
        }

        LastError = null;

        try
        {
            capturedPcm = new MemoryStream();
            capture = new WasapiLoopbackCapture();
            captureFormat = capture.WaveFormat;

            capture.DataAvailable += OnDataAvailable;
            capture.RecordingStopped += OnRecordingStopped;
            capture.StartRecording();

            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Failed to start audio capture: {ex.Message}";
            Cleanup();
            return false;
        }
    }

    /// <summary>
    /// Stops recording and returns the captured audio as a WAV file path, or null if no data.
    /// </summary>
    public string? StopCapture()
    {
        if (capture is null)
        {
            return null;
        }

        try
        {
            capture.StopRecording();
        }
        catch
        {
            // StopRecording can throw if already stopped
        }

        Cleanup();

        if (capturedPcm is null || capturedPcm.Length == 0 || captureFormat is null)
        {
            LastError = "No audio data captured";
            capturedPcm?.Dispose();
            capturedPcm = null;
            return null;
        }

        var pcmData = capturedPcm.ToArray();
        capturedPcm.Dispose();
        capturedPcm = null;

        // Convert to 16kHz mono float32 for Whisper
        var samples = ConvertToWhisperFormat(pcmData, captureFormat);
        if (samples.Length == 0)
        {
            LastError = "Audio conversion failed";
            return null;
        }

        // Save as WAV file
        var capturesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captures");
        Directory.CreateDirectory(capturesDir);
        var wavPath = Path.Combine(capturesDir, $"audio_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.wav");
        WriteWav(wavPath, samples, 16000);

        return wavPath;
    }

    public void Dispose()
    {
        Cleanup();
        capturedPcm?.Dispose();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded > 0)
        {
            capturedPcm?.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
        {
            LastError = $"Recording error: {e.Exception.Message}";
        }
    }

    private void Cleanup()
    {
        if (capture is not null)
        {
            capture.DataAvailable -= OnDataAvailable;
            capture.RecordingStopped -= OnRecordingStopped;
            capture.Dispose();
            capture = null;
        }
    }

    /// <summary>
    /// Converts raw PCM audio to 16kHz mono float32 samples for Whisper.
    /// </summary>
    private static float[] ConvertToWhisperFormat(byte[] pcmData, WaveFormat format)
    {
        var sampleRate = format.SampleRate;
        var channels = format.Channels;
        var bitsPerSample = format.BitsPerSample;

        // Parse raw bytes into float samples
        float[] rawSamples;

        if (format.Encoding == WaveFormatEncoding.IeeeFloat || bitsPerSample == 32)
        {
            rawSamples = new float[pcmData.Length / 4];
            Buffer.BlockCopy(pcmData, 0, rawSamples, 0, pcmData.Length);
        }
        else if (bitsPerSample == 16)
        {
            rawSamples = new float[pcmData.Length / 2];
            for (var i = 0; i < rawSamples.Length; i++)
            {
                var sample = BitConverter.ToInt16(pcmData, i * 2);
                rawSamples[i] = sample / 32768f;
            }
        }
        else
        {
            return [];
        }

        // Convert to mono
        float[] monoSamples;
        if (channels >= 2)
        {
            monoSamples = new float[rawSamples.Length / channels];
            for (var i = 0; i < monoSamples.Length; i++)
            {
                float sum = 0;
                for (var ch = 0; ch < channels; ch++)
                {
                    sum += rawSamples[i * channels + ch];
                }

                monoSamples[i] = sum / channels;
            }
        }
        else
        {
            monoSamples = rawSamples;
        }

        // Resample to 16kHz
        if (sampleRate == 16000)
        {
            return monoSamples;
        }

        var ratio = (double)sampleRate / 16000;
        var outputLength = (int)(monoSamples.Length / ratio);
        var resampled = new float[outputLength];

        for (var i = 0; i < outputLength; i++)
        {
            var srcIndex = i * ratio;
            var index = (int)srcIndex;
            var frac = (float)(srcIndex - index);

            if (index + 1 < monoSamples.Length)
            {
                resampled[i] = monoSamples[index] * (1 - frac) + monoSamples[index + 1] * frac;
            }
            else if (index < monoSamples.Length)
            {
                resampled[i] = monoSamples[index];
            }
        }

        return resampled;
    }

    /// <summary>
    /// Writes float32 mono samples as a 16-bit PCM WAV file.
    /// </summary>
    private static void WriteWav(string path, float[] samples, int sampleRate)
    {
        const int bitsPerSample = 16;
        const int channels = 1;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var dataSize = samples.Length * blockAlign;

        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        // RIFF header
        bw.Write("RIFF"u8);
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8);

        // fmt chunk
        bw.Write("fmt "u8);
        bw.Write(16);
        bw.Write((short)1); // PCM
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)blockAlign);
        bw.Write((short)bitsPerSample);

        // data chunk
        bw.Write("data"u8);
        bw.Write(dataSize);

        foreach (var sample in samples)
        {
            var clamped = Math.Clamp(sample, -1f, 1f);
            bw.Write((short)(clamped * 32767));
        }
    }
}
