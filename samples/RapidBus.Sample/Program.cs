using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RapidBus;
using RapidBus.AzureStorageQueues;
using RapidBus.Middleware;
using RapidBus.Sample.Event;
using RapidBus.Sample.Middleware;

var builder = Host.CreateApplicationBuilder();

if (false) // produce only 
{
    builder.Services.AddRapidBus(busBuilder =>
    {
        //busBuilder
        //    .UseRabbitMQ(builder.Configuration.GetConnectionString("RabbitMQ")!, "exchange", "queue"); // use rabbitmq

        busBuilder
            .UseAzureStorageQueues(builder.Configuration.GetConnectionString("AzureStorage")!);
    });
}
if (true) // produce and consume
{
    builder.Services.AddRapidBus((busBuilder, consumer) =>
    {
        //busBuilder
        //    .UseRabbitMQ(builder.Configuration.GetConnectionString("RabbitMQ")!, "exchange", "queue"); // use rabbitmq
        busBuilder
            .UseAzureStorageQueues(builder.Configuration.GetConnectionString("AzureStorage")!);
        consumer
            .RegisterEventHandlers(typeof(Program).Assembly) // auto register (event processing)
            .SubscribeEventHandlers();
    });
}

var host = builder.Build();

// use middleware for event processing
host.UseEventMiddleware<SampleMiddleware>();

host.Start();
//await host.RunAsync();

// sample publish

var bus = host.Services.GetRequiredService<IRapidBus>();

for (int i = 0; i < 10; i++)
{
    bus.Publish(new SampleEvent());
}

await Task.Delay(1999);
