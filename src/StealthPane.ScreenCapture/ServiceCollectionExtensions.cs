using Microsoft.Extensions.DependencyInjection;
using StealthPane.ScreenCapture.Services;

namespace StealthPane.ScreenCapture;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenCapture(this IServiceCollection services)
    {
        services.AddSingleton<ScreenCaptureService>();
        return services;
    }
}
