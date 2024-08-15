namespace RapidBus;

public sealed class Subscription(
    Type eventType
    , Type handlerType)
{
    public Type EventType { get; private set; } = eventType;
    public Type HandlerType { get; private set; } = handlerType;
}