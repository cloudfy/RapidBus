using Microsoft.Extensions.DependencyInjection;
using RapidBus.Abstractions.Exceptions;

namespace RapidBus;

public static class StartupExtensions
{
    public static IServiceCollection AddRapidBus(this IServiceCollection services, Action<RapidBusBuilder> builder)
    {
        if (services.Any(d => d.ServiceType == typeof(IRapidBus)))
        {
            throw new ConfigurationException(
                "AddRapidBus() was already called and may only be called once per container.");
        }

        //AddHostedService(services);
        //AddInstrumentation(services);

        var rapidBusBuilder = new RapidBusBuilder(services);
        builder(rapidBusBuilder);
        //services.Configure(configure);
        //services.AddSingleton<IRapidBus, RapidBus>();
        services.AddSingleton<IRapidBus>((sp) => rapidBusBuilder.Build(sp));

        return services;
    }
}
