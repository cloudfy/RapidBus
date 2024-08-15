namespace RabidBus.Abstractions;

public interface IIntegrationEvent
{
}

public delegate Task RequestDelegate(EventContext context);
