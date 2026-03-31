using Microsoft.Extensions.DependencyInjection;

namespace StealthPane.Terminal;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTerminal(this IServiceCollection services)
    {
        services.AddSingleton<PtyService>();
        return services;
    }
}
