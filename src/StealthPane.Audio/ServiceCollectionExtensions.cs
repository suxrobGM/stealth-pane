using Microsoft.Extensions.DependencyInjection;
using StealthPane.Audio.Services;

namespace StealthPane.Audio;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAudioCapture(this IServiceCollection services)
    {
        services.AddSingleton<AudioCaptureService>();
        services.AddSingleton<TranscriptionService>();
        services.AddSingleton<ModelDownloadService>();
        return services;
    }
}
