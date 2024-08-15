using RabidBus.Abstractions;

namespace RapidBus.Subscriptions;

public interface ISubscriptionManager
{
    event EventHandler<string> OnEventRemoved;

    bool IsEmpty { get; }
    bool HasSubscriptionsForEvent(string eventName);
    string GetEventIdentifier<TEvent>();
    Type? GetEventTypeByName(string eventName);
    IEnumerable<Subscription> GetHandlersForEvent(string eventName);
    Dictionary<string, List<Subscription>> GetAllSubscriptions();

    void AddSubscription<TEvent, TEventHandler>()
        where TEvent : IIntegrationEvent
        where TEventHandler : IIntegrationEventHandler<TEvent>;
    
    void AddSubscription(Type @eventType, Type eventHandlerType);

    void RemoveSubscription<TEvent, TEventHandler>()
        where TEvent : IIntegrationEvent
        where TEventHandler : IIntegrationEventHandler<TEvent>;

    void Clear();
}
