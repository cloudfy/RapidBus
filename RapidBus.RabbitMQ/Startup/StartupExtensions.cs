using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace RapidBus.RabbitMQ;

public static class StartupExtensions
{
    public static RapidBusOptionsBuilder UseRabbitMQ(
        this RapidBusOptionsBuilder builder
        , string connectionString
        , int timeoutBeforeReconnecting = 15)
    {
        var connectionFactory = new ConnectionFactory { 
            Uri = new Uri(connectionString) 
            , DispatchConsumersAsync = true
        };

        builder.Services.AddSingleton<IEventBus>((sp) => {

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var persistentConnection = new RabbitMQPersistentConnection(
                connectionFactory
                , timeoutBeforeReconnecting
                , loggerFactory.CreateLogger<RabbitMQPersistentConnection>());

            return new RabbitMqRapidBus(
                persistentConnection
                , loggerFactory.CreateLogger<RabbitMqRapidBus>());
        });

        //options.AddIntegrationEventBus<RabbitMQIntegrationEventBus>();
        //options.Services.Configure<RabbitMQOptions>(options =>
        //{
        //    options.ConnectionString = connectionString;
        //});
        return builder;
    }
}
