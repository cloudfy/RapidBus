using RabidBus.Abstractions;

namespace RapidBus;

/// <summary>
/// A function that can process an event.
/// </summary>
/// <param name="context">The <see cref="EventContext"/> for the request.</param>
/// <returns>A task that represents the completion of request processing.</returns>
public delegate Task RequestDelegate(EventContext context);
