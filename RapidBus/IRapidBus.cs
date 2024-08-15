using RabidBus.Abstractions;

namespace RapidBus;

public interface IRapidBus
{
    void Publish<TEvent>(TEvent @event) where TEvent : IIntegrationEvent;
}
