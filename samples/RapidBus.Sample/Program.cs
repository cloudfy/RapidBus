using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RapidBus;
using RapidBus.Middleware;
using RapidBus.RabbitMQ;
using RapidBus.Sample.Event;
using RapidBus.Sample.Middleware;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddRapidBus(builder => {
    builder
        .UseRabbitMQ("amqp://guest:guest@localhost:5672", "exchange", "queue") // use rabbitmq
        .RegisterEventHandlers(typeof(Program).Assembly); // auto register (event processing)
});

var host = builder.Build();

// use middleware for event processing
host.UseEventMiddleware<SampleMiddleware>();

host.Start();
//await host.RunAsync();

// sample

var bus = host.Services.GetRequiredService<IRapidBus>();

for (int i = 0; i < 10; i++)
{
    bus.Publish(new SampleEvent());
}

await Task.Delay(1999);
