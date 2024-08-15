using Microsoft.Extensions.DependencyInjection;
using RabidBus.Abstractions;
using RapidBus.Abstractions.Exceptions;
using RapidBus.Consumer;
using System.Reflection;

namespace RapidBus;

public class RapidBusConsumerBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Subscription> _delayedSubscriptions = [];

    internal RapidBusConsumerBuilder(IServiceCollection services) => _services = services;

    internal List<Subscription> DelayedSubscriptions => _delayedSubscriptions;

    public RapidBusConsumerBuilder RegisterEventHandlers(Assembly assembly)
    {
        assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
            .ToList()
            .ForEach(handler => {
                _services.AddScoped(handler);

                var eventTypeDeclaration = handler.GetInterfaces()
                .First(_ => _.IsGenericType == true)
                    .GetGenericArguments()
                        .First();

                _delayedSubscriptions.Add(new Subscription(eventTypeDeclaration, handler));
            });

        return this;
        // todo: add to delayed subscription list
    }
 //   public void RegisterEventHandler<T>() where T : IIntegrationEventHandler { }

    public void SubscribeEventHandlers() 
    {
        if (_services.Any(d => d.ServiceType == typeof(RapidBusConsumer)))
        {
            throw new ConfigurationException(
                "SubscribeEventHandlers() was already called and may only be called once per container.");
        }

        //_services.AddHostedService<RapidBusConsumer>();
    }
}