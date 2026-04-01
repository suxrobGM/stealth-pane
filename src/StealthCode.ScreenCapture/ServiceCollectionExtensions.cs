using Microsoft.Extensions.DependencyInjection;
using StealthCode.ScreenCapture.Services;

namespace StealthCode.ScreenCapture;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenCapture(this IServiceCollection services)
    {
        services.AddSingleton<ScreenCaptureService>();
        return services;
    }
}
