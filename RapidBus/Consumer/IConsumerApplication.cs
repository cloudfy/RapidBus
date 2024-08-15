namespace RapidBus.Consumer;

public interface IConsumerApplication
{
    Task ProcessEvent(string eventName, string message, CancellationToken cancellationToken);
}
