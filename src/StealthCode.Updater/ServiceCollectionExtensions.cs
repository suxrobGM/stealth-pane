using Microsoft.Extensions.DependencyInjection;
using StealthCode.Updater.Services;

namespace StealthCode.Updater;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUpdater(this IServiceCollection services)
    {
        services.AddSingleton<UpdateService>();
        return services;
    }
}
