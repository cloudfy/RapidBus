using Microsoft.Extensions.DependencyInjection;

namespace RapidBus;

public static class StartupExtensions
{
    public static IServiceCollection AddRapidBus(this IServiceCollection services, Action<RapidBusOptionsBuilder> builder)
    {
        var rapidBusBuilder = new RapidBusOptionsBuilder(services);
        builder(rapidBusBuilder);
        //services.Configure(configure);
        //services.AddSingleton<IRapidBus, RapidBus>();
        services.AddSingleton<IEventBus>((sp) => rapidBusBuilder.Build(sp));

        return services;
    }
}
