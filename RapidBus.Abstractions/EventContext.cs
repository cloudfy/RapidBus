using System.Collections.Concurrent;

namespace RabidBus.Abstractions;

public sealed class EventContext(
    IIntegrationEvent @event
    , string eventName
    , IServiceProvider serviceProvider)
{
    /// <summary>
    /// Gets the current <see cref="IIntegrationEvent"/> event.
    /// </summary>
    public IIntegrationEvent Event { get; } = @event;
    /// <summary>
    /// Gets the name of the current event.
    /// </summary>
    public string EventName { get; } = eventName;
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> for this context.
    /// </summary>
    public IServiceProvider ServiceProvider { get; set; } = serviceProvider;
    /// <summary>
    /// Gets a key/value collection that can be used to share data within the scope of this event.
    /// </summary>
    public IDictionary<object, object?> Items { get; } = new ConcurrentDictionary<object, object?>();
}
