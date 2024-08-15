using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabidBus.Abstractions;
using RapidBus.Middleware;
using RapidBus.Subscriptions;
using System.Text.Json;

namespace RapidBus.Consumer;

public sealed class ConsumerApplication : IConsumerApplication
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsumerApplication> _logger;
    private readonly ISubscriptionManager _subscriptionsManager;
    private readonly SemaphoreSlim _concurrencyLimit = new(2);

    public ConsumerApplication(
        ILogger<ConsumerApplication> logger
        , IServiceProvider serviceProvider
        , ISubscriptionManager subscriptionManager)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _subscriptionsManager = subscriptionManager;
    }

    public Task ProcessEvent(string eventName, string message, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Processing RabbitMQ event: {EventName}...", eventName);

        if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName))
        {
            _logger.LogTrace("There are no subscriptions for this event.");
            return Task.CompletedTask;
        }

        var subscriptions = _subscriptionsManager.GetHandlersForEvent(eventName);
        if (subscriptions.Any() == false)
        {
            _logger.LogWarning("There are no handlers for the following event: {EventName}", eventName);
            return Task.CompletedTask;
        }

        foreach (var subscription in subscriptions)
        {
            var eventType = _subscriptionsManager.GetEventTypeByName(eventName);
            if (eventType is null)
            {
                _logger.LogWarning("There are no handlers for the following event: {EventName}", eventName);
                continue;
            }
            var @event = JsonSerializer.Deserialize(message, eventType) as IIntegrationEvent;
            var eventHandlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

            ThreadPool.QueueUserWorkItem(async state => {
                await _concurrencyLimit.WaitAsync((CancellationToken)state!);

                _logger.LogTrace($"Processing event {eventName} on thread {Environment.CurrentManagedThreadId}...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    // build event middleware pipeline (UseMiddleware) and execute the main handler
                    var del = UseEventMiddlewareExtensions.BuildByRequestDelegate(
                        async () => {
                            var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                            if (handler == null)
                            {
                                _logger.LogWarning("There are no handlers for the following event: {EventName}", eventName);
                                return;
                            }
                            await (Task)eventHandlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.Handle))!.Invoke(handler, [@event!, state!])!;
                        });
                    // begin pipeline
                    await del(new EventContext(@event!, eventName, scope.ServiceProvider));
                }
                _concurrencyLimit.Release();

            }, cancellationToken);
        }

        _logger.LogTrace("Processed event {EventName}.", eventName);
        return Task.CompletedTask;
    }
}
