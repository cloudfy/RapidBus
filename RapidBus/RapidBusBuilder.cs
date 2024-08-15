using Microsoft.Extensions.DependencyInjection;
using RabidBus.Abstractions;
using RapidBus.Abstractions;
using System.Reflection;

namespace RapidBus;

public sealed class RapidBusBuilder
{
    private readonly IServiceCollection _services;
    private Func<IServiceProvider, IEventBusSubscriptionManager, IRapidBus>? _factory;

    internal RapidBusBuilder(IServiceCollection services) => _services = services;

    public IServiceCollection Services => _services;
    public ServiceLifetime Lifetime { get; } = ServiceLifetime.Scoped;

    public void RegisterEventHandlers(Assembly assembly)
    {
        assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
            .ToList()
            .ForEach(t => _services.AddScoped(t));

        // todo: add to delayed subscription list
    }
    public void RegisterEventHandler<T>() where T : IIntegrationEventHandler { }

    public void SetFactory(Func<IServiceProvider, IEventBusSubscriptionManager, IRapidBus> factory) 
    {
        if (_factory is not null)
            throw new Abstractions.Exceptions.ConfigurationException("Factory already set");

        _factory = factory;
    }

    public IRapidBus Build(IServiceProvider serviceProvider)
    {
        if (_factory is null)
            throw new InvalidOperationException();

        return _factory(serviceProvider, CreateSubscriptionManager());
    }
    private IEventBusSubscriptionManager CreateSubscriptionManager() => new InMemoryEventBusSubscriptionManager();
}