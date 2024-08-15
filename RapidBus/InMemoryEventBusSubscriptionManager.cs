using RabidBus.Abstractions;
using RapidBus.Abstractions;

namespace RapidBus;

internal sealed class InMemoryEventBusSubscriptionManager
    : IEventBusSubscriptionManager
{
    private readonly Dictionary<string, Type> _eventTypes = [];
    private readonly Dictionary<string, List<Subscription>> _handlers = [];

    public event EventHandler<string>? OnEventRemoved;

    public string GetEventIdentifier<TEvent>()
    {
        return typeof(TEvent).GetCustomAttributes(typeof(IntegrationEventAttribute), false)
            .SingleOrDefault() is IntegrationEventAttribute attribute
            ? attribute.Name
            : typeof(TEvent).Name;
    }

    public Type? GetEventTypeByName(string eventName) => _eventTypes[eventName];
    public IEnumerable<Subscription> GetHandlersForEvent(string eventName) => _handlers[eventName];

    /// <summary>
    /// Returns the dictionary of subscriptiosn in an immutable way.
    /// </summary>
    /// <returns>Dictionary.</returns>
    public Dictionary<string, List<Subscription>> GetAllSubscriptions() => new(_handlers);

    public void AddSubscription<TEvent, TEventHandler>()
        where TEvent : IIntegrationEvent
        where TEventHandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = GetEventIdentifier<TEvent>();

        DoAddSubscription(typeof(TEvent), typeof(TEventHandler), eventName);

        if (!_eventTypes.ContainsKey(eventName))
        {
            _eventTypes.Add(eventName, typeof(TEvent));
        }
    }

    public void RemoveSubscription<TEvent, TEventHandler>()
        where TEventHandler : IIntegrationEventHandler<TEvent>
        where TEvent : IIntegrationEvent
    {
        var handlerToRemove = FindSubscriptionToRemove<TEvent, TEventHandler>();
        if (handlerToRemove is null) return;

        var eventName = GetEventIdentifier<TEvent>();
        DoRemoveHandler(eventName, handlerToRemove);
    }

    public void Clear()
    {
        _handlers.Clear();
        _eventTypes.Clear();
    }

    public bool IsEmpty => !_handlers.Keys.Any();
    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    private void DoAddSubscription(Type eventType, Type handlerType, string eventName)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            _handlers.Add(eventName, []);
        }

        if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
        {
            throw new ArgumentException($"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
        }

        _handlers[eventName].Add(new Subscription(eventType, handlerType));
    }

    private void DoRemoveHandler(string eventName, Subscription subscriptionToRemove)
    {
        if (subscriptionToRemove == null)
        {
            return;
        }

        _handlers[eventName].Remove(subscriptionToRemove);
        if (_handlers[eventName].Any())
        {
            return;
        }

        _handlers.Remove(eventName);
        if (_eventTypes.ContainsKey(eventName))
            _eventTypes.Remove(eventName);

        RaiseOnEventRemoved(eventName);
    }

    private void RaiseOnEventRemoved(string eventName)
    {
        var handler = OnEventRemoved;
        handler?.Invoke(this, eventName);
    }

    private Subscription? FindSubscriptionToRemove<TEvent, TEventHandler>()
         where TEvent : IIntegrationEvent
         where TEventHandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = GetEventIdentifier<TEvent>();
        return DoFindSubscriptionToRemove(eventName, typeof(TEventHandler));
    }

    private Subscription? DoFindSubscriptionToRemove(string eventName, Type handlerType)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            return null;
        }

        return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);

    }
}
