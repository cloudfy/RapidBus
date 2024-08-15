using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

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
        builder.SetFactory((sp, isvm) => {
            var connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(connectionString)
                , DispatchConsumersAsync = true
            };

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var persistentConnection = new RabbitMQPersistentConnection(
                connectionFactory
                , timeoutBeforeReconnecting
                , loggerFactory.CreateLogger<RabbitMQPersistentConnection>());

            return new RabbitMqRapidBus(
                persistentConnection
                , isvm
                , loggerFactory.CreateLogger<RabbitMqRapidBus>()
                , sp
                , exchangeName
                , queueName);
        });
       
        //options.AddIntegrationEventBus<RabbitMQIntegrationEventBus>();
        //options.Services.Configure<RabbitMQOptions>(options =>
        //{
        //    options.ConnectionString = connectionString;
        //});
        return builder;
    }
}
