using Microsoft.Extensions.DependencyInjection;

namespace StealthPane.ScreenCapture;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenCapture(this IServiceCollection services)
    {
        return services;
    }
}
