using Microsoft.Extensions.DependencyInjection;

namespace RapidBus;

public static class StartupExtensions
{
    public static IServiceCollection AddRapidBus(this IServiceCollection services, Action<RapidBusOptions> configure)
    {
        //services.Configure(configure);
        //services.AddSingleton<IRapidBus, RapidBus>();
        return services;
    }
}
