using Microsoft.Extensions.DependencyInjection;

namespace StealthCode.Terminal;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTerminal(this IServiceCollection services)
    {
        services.AddSingleton<PtyService>();
        return services;
    }
}
