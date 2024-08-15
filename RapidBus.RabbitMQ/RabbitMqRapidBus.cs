using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabidBus.Abstractions;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace RapidBus.RabbitMQ;

internal class RabbitMqRapidBus : IEventBus
{
    private readonly RabbitMQPersistentConnection _persistentConnection;
    private readonly IEventBusSubscriptionManager _subscriptionsManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqRapidBus> _logger;
    
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly int _publishRetryCount = 5;
    private readonly TimeSpan _subscribeRetryTime = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim _concurrencyLimit = new(2);

    private IModel? _consumerChannel;
    private CancellationTokenSource? _cancellationTokenSource;

    public event EventHandler<BeginHandleEventEventArgs>? BeginHandleEvent;

    internal RabbitMqRapidBus(
        RabbitMQPersistentConnection persistentConnection
        , IEventBusSubscriptionManager subscriptionsManager
        , ILogger<RabbitMqRapidBus> logger
        , IServiceProvider serviceProvider)
    {
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _subscriptionsManager = subscriptionsManager ?? throw new ArgumentNullException(nameof(subscriptionsManager));
        _serviceProvider = serviceProvider;
        _logger = logger;
        //_exchangeName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
        //_queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

        ConfigureMessageBroker();
    }

    private void ConfigureMessageBroker()
    {
        _consumerChannel = CreateConsumerChannel();
        _subscriptionsManager.OnEventRemoved += SubscriptionManager_OnEventRemoved;
        _persistentConnection.ReconnectedAfterConnectionFailure += PersistentConnection_OnReconnectedAfterConnectionFailure;
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : IIntegrationEvent
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var policy = Policy
            .Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_publishRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (exception, timeSpan) => {
                // _logger.LogWarning(exception, "Could not publish event #{EventId} after {Timeout} seconds: {ExceptionMessage}.", @event.Id, $"{timeSpan.TotalSeconds:n1}", exception.Message);
            });

        var eventName = _subscriptionsManager.GetEventIdentifier<TEvent>(); // @event.GetType()dd.Name;

        //_logger.LogTrace("Creating RabbitMQ channel to publish event #{EventId} ({EventName})...", @event.Id, eventName);

        using (var channel = _persistentConnection.CreateModel())
        {
  //          using var activity = ActivitySource.StartActivity("PUBLISH " + eventName, ActivityKind.Producer);

            channel.ExchangeDeclare(exchange: _exchangeName, type: "direct", durable: false);

            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() => {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = (byte)DeliveryMode.Persistent;
                properties.AppId = "Plude";

//                _logger.LogTrace($"Publishing event {@eventName} to RabbitMQ with ID #{@event.Id}");

//                AddActivityToHeader(activity!, properties, eventName);

                channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                body: body);

//                _logger.LogTrace("Published event with ID #{EventId}.", @event.Id);
            });
        }
    }

    public void Subscribe<TEvent, TEventHandler>()
        where TEvent : IIntegrationEvent
        where TEventHandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = _subscriptionsManager.GetEventIdentifier<TEvent>();
        var eventHandlerName = typeof(TEventHandler).Name;

        AddQueueBindForEventSubscription(eventName);

        _logger.LogTrace("Subscribing to event {EventName} with {EventHandler}...", eventName, eventHandlerName);

        _subscriptionsManager.AddSubscription<TEvent, TEventHandler>();
        StartBasicConsume();

        _logger.LogTrace("Subscribed to event {EventName} with {EvenHandler}.", eventName, eventHandlerName);
    }

    public void Unsubscribe<TEvent, TEventHandler>()
        where TEvent : IIntegrationEvent
        where TEventHandler : IIntegrationEventHandler<TEvent>
    {
        var eventName = _subscriptionsManager.GetEventIdentifier<TEvent>();

        _logger.LogTrace("Unsubscribing from event {EventName}...", eventName);

        _subscriptionsManager.RemoveSubscription<TEvent, TEventHandler>();

        _logger.LogTrace("Unsubscribed from event {EventName}.", eventName);
    }

    public void Dispose()
    {
        _consumerChannel?.Dispose();
        //_persistentConnection.Dispose();
        _subscriptionsManager.Clear();
    }

    //private void AddActivityToHeader(Activity activity, IBasicProperties props, string eventName)
    //{
    //    Propagator.Inject(
    //        new PropagationContext(activity.Context, Baggage.Current)
    //        , props
    //        , this.OpenTelemetryContextSetter);
    //    activity?.SetTag("messaging.system", "rabbitmq");
    //    activity?.SetTag("messaging.destination_kind", "queue");
    //    activity?.SetTag("messaging.destination", _exchangeName);
    //    activity?.SetTag("messaging.event_name", eventName);
    //}

    private IEnumerable<string> OpenTelemetryContextGetter(IBasicProperties props, string key)
    {
        try
        {
            if (props.Headers.TryGetValue(key, out var value) && value is byte[] bytes)
            {
                return [Encoding.UTF8.GetString(bytes)];
            }
        }
        catch (Exception)
        {
            //this.logger.LogError(ex, "Failed to extract trace context.");
        }

        return [];
    }
    private void OpenTelemetryContextSetter(IBasicProperties props, string key, string value)
    {
        props.Headers ??= new Dictionary<string, object>();

        props.Headers[key] = Encoding.UTF8.GetBytes(value);
        if (key == "traceparent")
            props.Headers["baggage"] = Encoding.UTF8.GetBytes(value);
    }

    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        _logger.LogTrace("Creating RabbitMQ consumer channel...");

        var channel = _persistentConnection.CreateModel();

        channel.ExchangeDeclare(exchange: _exchangeName, type: "direct");
        channel.QueueDeclare
        (
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        channel.CallbackException += (sender, ea) => {
            _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel...");
            DoCreateConsumerChannel();
        };

        _logger.LogTrace("Created RabbitMQ consumer channel.");


        return channel;
    }

    private void StartBasicConsume()
    {
        _logger.LogTrace("Starting RabbitMQ basic consume...");

        if (_consumerChannel is null)
        {
            _logger.LogError("Could not start basic consume because consumer channel is null.");
            return;
        }

        _cancellationTokenSource = new();

        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
        consumer.Received += Consumer_Received;

        _consumerChannel.BasicConsume
        (
            queue: _queueName,
            autoAck: false,
            consumer: consumer
        );

        _logger.LogTrace("Started RabbitMQ basic consume.");
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_consumerChannel is null)
        {
            throw new InvalidOperationException("Consumer channel is null.");
        }

        //var parentContext = Propagator.Extract(
        //    default
        //    , eventArgs.BasicProperties
        //    , OpenTelemetryContextGetter);

        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);
        var isAcknowledged = false;
        //using var activity = ActivitySource
        //    .StartActivity("CONSUME " + eventName, ActivityKind.Consumer, parentContext.ActivityContext);
        //Baggage.Current = parentContext.Baggage;

        try
        {
            await ProcessEvent(eventName, message, _cancellationTokenSource?.Token ?? default);

            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            isAcknowledged = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing the following message: {Message}.", message);
        }
        finally
        {
            if (!isAcknowledged)
            {
                await TryEnqueueMessageAgainAsync(eventArgs);
            }
        }
    }

    private async Task TryEnqueueMessageAgainAsync(BasicDeliverEventArgs eventArgs)
    {
        try
        {
            _logger.LogWarning("Adding message to queue again with {Time} seconds delay...", $"{_subscribeRetryTime.TotalSeconds:n1}");

            await Task.Delay(_subscribeRetryTime);
            _consumerChannel!.BasicNack(eventArgs.DeliveryTag, false, true);

            _logger.LogTrace("Message added to queue again.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Could not enqueue message again: {Error}.", ex.Message);
        }
    }

    private Task ProcessEvent(string eventName, string message, CancellationToken cancellationToken)
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
            //var @event = (IIntegrationEvent)JsonSerializer.Deserialize(message, eventType);
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

    private void SubscriptionManager_OnEventRemoved(object? sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        using (var channel = _persistentConnection.CreateModel())
        {
            channel.QueueUnbind(queue: _queueName, exchange: _exchangeName, routingKey: eventName);

            if (_subscriptionsManager.IsEmpty)
            {
                _consumerChannel?.Close();
            }
        }
    }

    private void AddQueueBindForEventSubscription(string eventName)
    {
        var containsKey = _subscriptionsManager.HasSubscriptionsForEvent(eventName);
        if (containsKey)
        {
            return;
        }

        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        using (var channel = _persistentConnection.CreateModel())
        {
            channel.QueueBind(queue: _queueName, exchange: _exchangeName, routingKey: eventName);
        }
    }

    private void PersistentConnection_OnReconnectedAfterConnectionFailure(object? sender, bool e)
    {
        DoCreateConsumerChannel();
        RecreateSubscriptions();
    }

    private void DoCreateConsumerChannel()
    {
        _consumerChannel?.Dispose();
        _consumerChannel = CreateConsumerChannel();
        StartBasicConsume();
    }

    private void RecreateSubscriptions()
    {
        var subscriptions = _subscriptionsManager.GetAllSubscriptions();
        _subscriptionsManager.Clear();

        var eventBusType = this.GetType();
        MethodInfo? genericSubscribe;

        foreach (var entry in subscriptions)
        {
            foreach (var subscription in entry.Value)
            {
                genericSubscribe = eventBusType?
                    .GetMethod("Subscribe")?
                    .MakeGenericMethod(subscription.EventType, subscription.HandlerType);
                genericSubscribe?
                    .Invoke(this, null);
            }
        }
    }

    //internal void Start()
    //{
    //    using var connection = factory.CreateConnection();
    //    using var channel = connection.CreateModel();

    //    channel.QueueDeclare(queue: "hello",
    //                 durable: false,
    //                 exclusive: false,
    //                 autoDelete: false,
    //                 arguments: null);

    //    if (false) 
    //    { 
    //        const string message = "Hello World!";
    //        var body = Encoding.UTF8.GetBytes(message);

    //        channel.BasicPublish(exchange: string.Empty,
    //                             routingKey: "hello",
    //                             basicProperties: null,
    //                             body: body);
    //    }
    //    if (true)
    //    {
    //        var consumer = new EventingBasicConsumer(channel);
    //        consumer.Received += (model, ea) =>
    //        {
    //            var body = ea.Body.ToArray();
    //            var message = Encoding.UTF8.GetString(body);
    //            Console.WriteLine($" [x] Received {message}");
    //        };
    //        channel.BasicConsume(queue: "hello",
    //                             autoAck: true,
    //                             consumer: consumer);
    //    }
    //}
}
