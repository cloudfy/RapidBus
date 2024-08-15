using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RapidBus.Consumer;
using RapidBus.Subscriptions;

namespace RapidBus.RabbitMQ;

public static class StartupExtensions
{
    public static RapidBusBuilder UseRabbitMQ(
        this RapidBusBuilder builder
        , string connectionString
        , string exchangeName
        , string queueName
        , int timeoutBeforeReconnecting = 15)
    {
        // assign the factory of the transport
        builder.SetFactory((sp) => {
            var connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(connectionString)
                , DispatchConsumersAsync = true
            };

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var consumer = sp.GetRequiredService<IConsumerApplication>();
            var subMgr = sp.GetRequiredService<ISubscriptionManager>();

            var persistentConnection = new RabbitMQPersistentConnection(
                connectionFactory
                , timeoutBeforeReconnecting
                , loggerFactory.CreateLogger<RabbitMQPersistentConnection>());

            return new RabbitMQRapidBus(
                consumer
                , persistentConnection
                , subMgr
                , loggerFactory.CreateLogger<RabbitMQRapidBus>()
                , exchangeName
                , queueName);
        });

        return builder;
    }
}