using Microsoft.Extensions.DependencyInjection;
using RapidBus.Subscriptions;

namespace RapidBus;

public sealed class RapidBusBuilder
{
    private readonly IServiceCollection _services;
    private Func<IServiceProvider, IRapidBus>? _factory;
    private string? _name;

    internal RapidBusBuilder(IServiceCollection services) => _services = services;

    public IServiceCollection Services => _services;
    public ServiceLifetime Lifetime { get; } = ServiceLifetime.Scoped;

    public RapidBusBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public void SetFactory(Func<IServiceProvider, IRapidBus> factory) 
    {
        if (_factory is not null)
            throw new Abstractions.Exceptions.ConfigurationException("Factory already set");

        _factory = factory;
    }

    public IRapidBus Build(IServiceProvider serviceProvider)
    {
        if (_factory is null)
            throw new InvalidOperationException();

        return _factory(serviceProvider);
    }
}