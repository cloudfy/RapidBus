using Microsoft.Extensions.DependencyInjection;
using RapidBus.Abstractions;
using RapidBus.Abstractions.Exceptions;
using RapidBus.Consumer;
using RapidBus.Subscriptions;

namespace RapidBus;

public static class StartupExtensions
{
    public static IServiceCollection AddRapidBus(
        this IServiceCollection services
        , Action<RapidBusBuilder> builder)
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
        
        // add hosted services
      //  services.AddSingleton<ISubscriptionManager, InMemorySubscriptionManager>();
       // services.AddSingleton<IConsumerApplication, ConsumerApplication>();

        // build the IRapidBus
        services.AddSingleton<IRapidBus>((sp) => {

            return rapidBusBuilder.Build(sp);
        });

        return services;
    }
    
    public static IServiceCollection AddRapidBus(
        this IServiceCollection services
        , Action<RapidBusBuilder, RapidBusConsumerBuilder> builder)
    { 
        if (services.Any(d => d.ServiceType == typeof(IRapidBus)))
        {
            throw new ConfigurationException(
                "AddRapidBus() was already called and may only be called once per container.");
        }

        //AddHostedService(services);
        //AddInstrumentation(services);

        var rapidBusBuilder = new RapidBusBuilder(services);
        var rapidBusConsumerBuilder = new RapidBusConsumerBuilder(services);
        builder(rapidBusBuilder, rapidBusConsumerBuilder);

        // add hosted services
        services.AddSingleton<ISubscriptionManager, InMemorySubscriptionManager>();
        services.AddSingleton<IConsumerApplication, ConsumerApplication>();

        // build the IRapidBus
        services.AddSingleton<IRapidBus>((sp) => {

            var subscriptionManager = sp.GetRequiredService<ISubscriptionManager>();
            // bind events
            foreach (var subscription in rapidBusConsumerBuilder.DelayedSubscriptions)
            {
                subscriptionManager.AddSubscription(subscription.EventType, subscription.HandlerType);
            }

            var implementation = rapidBusBuilder.Build(sp);

            return implementation;
        });

        return services;
    }
}
