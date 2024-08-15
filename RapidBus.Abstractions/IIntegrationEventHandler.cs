namespace RabidBus.Abstractions;

public interface IIntegrationEventHandler<in TIntegrationEvent> 
    : IIntegrationEventHandler
    where TIntegrationEvent : IIntegrationEvent
{
    Task Handle(TIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

public interface IIntegrationEventHandler
{

}