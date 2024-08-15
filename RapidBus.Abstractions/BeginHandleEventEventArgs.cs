namespace RabidBus.Abstractions;

public class BeginHandleEventEventArgs(
    IServiceProvider serviceProvider
        , string eventName
        , IIntegrationEvent @event) : EventArgs
{
    public IIntegrationEvent Event { get; } = @event;
    public string EventName { get; } = eventName;
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
}