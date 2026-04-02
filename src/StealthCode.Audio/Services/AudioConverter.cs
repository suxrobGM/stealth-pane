namespace StealthCode.Audio.Services;

/// <summary>
/// Converts raw PCM audio to Whisper-compatible format (16kHz mono float32)
/// and writes WAV files.
/// </summary>
internal static class AudioConverter
{
    private const int WhisperSampleRate = 16000;

    /// <summary>
    /// Converts raw PCM bytes to 16kHz mono float32 samples for Whisper.
    /// </summary>
    public static float[] ToWhisperFormat(byte[] pcmData, CaptureFormat format)
    {
        var rawSamples = DecodeSamples(pcmData, format.BitsPerSample, format.IsFloat);
        if (rawSamples.Length == 0)
        {
            return [];
        }

        var mono = MixToMono(rawSamples, format.Channels);
        return Resample(mono, format.SampleRate, WhisperSampleRate);
    }

    /// <summary>
    /// Writes float32 mono samples as a 16-bit PCM WAV file.
    /// </summary>
    public static void WriteWav(string path, float[] samples, int sampleRate)
    {
        const int bitsPerSample = 16;
        const int channels = 1;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var dataSize = samples.Length * blockAlign;

        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        bw.Write("RIFF"u8);
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8);

        bw.Write("fmt "u8);
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)blockAlign);
        bw.Write((short)bitsPerSample);

        bw.Write("data"u8);
        bw.Write(dataSize);

        foreach (var sample in samples)
        {
            bw.Write((short)(Math.Clamp(sample, -1f, 1f) * 32767));
        }
    }

    private static float[] DecodeSamples(byte[] pcmData, int bitsPerSample, bool isFloat)
    {
        if (isFloat || bitsPerSample == 32)
        {
            var samples = new float[pcmData.Length / 4];
            Buffer.BlockCopy(pcmData, 0, samples, 0, pcmData.Length);
            return samples;
        }

        if (bitsPerSample == 16)
        {
            var samples = new float[pcmData.Length / 2];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = BitConverter.ToInt16(pcmData, i * 2) / 32768f;
            }

            return samples;
        }

        return [];
    }

    private static float[] MixToMono(float[] samples, int channels)
    {
        if (channels < 2)
        {
            return samples;
        }

        var mono = new float[samples.Length / channels];
        for (var i = 0; i < mono.Length; i++)
        {
            float sum = 0;
            for (var ch = 0; ch < channels; ch++)
            {
                sum += samples[i * channels + ch];
            }

            mono[i] = sum / channels;
        }

        return mono;
    }

    private static float[] Resample(float[] samples, int fromRate, int toRate)
    {
        if (fromRate == toRate)
        {
            return samples;
        }

        var ratio = (double)fromRate / toRate;
        var outputLength = (int)(samples.Length / ratio);
        var resampled = new float[outputLength];

        for (var i = 0; i < outputLength; i++)
        {
            var srcIndex = i * ratio;
            var index = (int)srcIndex;
            var frac = (float)(srcIndex - index);

            if (index + 1 < samples.Length)
            {
                resampled[i] = samples[index] * (1 - frac) + samples[index + 1] * frac;
            }
            else if (index < samples.Length)
            {
                resampled[i] = samples[index];
            }
        }

        return resampled;
    }
}
