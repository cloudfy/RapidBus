using Microsoft.Extensions.DependencyInjection;
using RabidBus.Abstractions;
using System.Reflection;

namespace RapidBus;

public sealed class RapidBusOptionsBuilder
{
    private readonly IServiceCollection _services;

    internal RapidBusOptionsBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public IServiceCollection Services => _services;

    public void RegisterEventHandlers(Assembly assembly) { }
    public void RegisterEventHandler<T>() where T : IIntegrationEvent { }

    public IEventBus Build(IServiceProvider sp)
    {
        return null;
    }
}