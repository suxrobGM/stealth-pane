using Microsoft.Extensions.DependencyInjection;
using StealthPane.Updater.Services;

namespace StealthPane.Updater;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUpdater(this IServiceCollection services)
    {
        services.AddSingleton<UpdateService>();
        return services;
    }
}
