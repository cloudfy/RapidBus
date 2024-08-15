using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RapidBus;
using RapidBus.RabbitMQ;
using RapidBus.Sample.Event;
using RapidBus.Sample.Middleware;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddRapidBus(builder => {

    // use middleware
    builder.UseMiddleware<SampleMiddleware>();

    // use rabbitmq
    builder.UseRabbitMQ("amqp://guest:guest@localhost:5672");

    // auto register
    builder.RegisterEventHandlers(typeof(Program).Assembly);
});

var host = builder.Build();
await host.RunAsync();

var bus = host.Services.GetRequiredService<IEventBus>();
bus.Publish(new SampleEvent());
